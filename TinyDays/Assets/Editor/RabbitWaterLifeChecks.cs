using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class RabbitWaterLifeChecks {
    static void Check(bool ok,string text){if(!ok)throw new Exception("Water life: "+text);}
    static RabbitHomeLifeReview Open(){EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();Check(h&&h.lifeFlow,"missing flow scene");return h;}
    public static void Execute(){try{
        AssetDatabase.Refresh();RabbitHomeLifeBuilder.BuildOnly();Verify();
        RabbitHomeGroundChecks.Execute();RabbitHomeLifeBuilder.Stress();RabbitWaterChecks.Verify();RabbitBenchChecks.Verify();
        Render();RabbitHomeLifeBuilder.BuildPlayer();Debug.Log("WATER_LIFE_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void FinalizeReview(){try{AssetDatabase.Refresh();RabbitHomeLifeBuilder.BuildOnly();Verify();Render();ClosePoses();RabbitHomeLifeBuilder.BuildPlayer();Debug.Log("WATER_LIFE_FINALIZE_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void Verify(){
        var h=Open();var flow=h.lifeFlow;var w=h.watering;var rows=new List<string>{"v0.126 water routine: editor deterministic tests, not OS input or quality approval"};
        float grip=0,drift=0,gap=0,reach=0,sole=100,storageError=0;int count=0;
        Action measure=()=>{if(!w.Active)return;if(w.MaxGripError>grip+.001f)rows.Add($"grip peak {w.MaxGripError:F6} at {flow.State}/{w.State} t={w.TimeInState:F3} root={h.resident.transform.position:F4} approach={flow.approach.position:F4}");grip=Mathf.Max(grip,w.MaxGripError);drift=Mathf.Max(drift,w.MaxDrift);gap=Mathf.Max(gap,w.MaxSupportGap);reach=Mathf.Max(reach,w.MaxReach);sole=Mathf.Min(sole,w.MinSole);};
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true}){
            h.SelectLifeFlow();h.slow=slow;flow.StartFlow();var previous=h.resident.transform.position;float maxRootJump=0;
            for(int i=0;i<fps*100&&flow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
                h.Advance(1f/fps);measure();maxRootJump=Mathf.Max(maxRootJump,Vector3.Distance(previous,h.resident.transform.position));previous=h.resident.transform.position;
            }
            Check(flow.State==RabbitWaterLifeFlow.Phase.Finished,$"not finished {fps}/{slow}: {flow.State}/{w.State} root={h.resident.transform.position}");
            Check(h.DoorClosed&&w.DropletsLanded>0&&w.DropletsOutsideBed==0,"door/water completion");
            storageError=Mathf.Max(storageError,Vector3.Distance(w.can.position,flow.storage.position));
            Check(storageError<.001f&&Quaternion.Angle(w.can.rotation,flow.storage.rotation)<.1f,"storage drift");
            Check(maxRootJump<.08f,"resident teleported between owners");
            rows.Add($"fps={fps} slow={slow} completed={flow.Completed} grip={w.MaxGripError:F6} rootStep={maxRootJump:F6}");count++;
        }
        h.SelectLifeFlow();
        for(int cycle=0;cycle<20;cycle++){
            flow.StartFlow();for(int i=0;i<60*80&&flow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){h.Advance(1f/60);measure();}
            Check(flow.State==RabbitWaterLifeFlow.Phase.Finished,"20 cycle stalled "+cycle);Check(Vector3.Distance(w.can.position,flow.storage.position)<.001f,"cycle can drift");
        }
        rows.Add("20 routines: completed="+flow.Completed);
        for(int cycle=0;cycle<20;cycle++){
            h.ResetLifeFlow();flow.StartFlow();for(int i=0;i<60*80&&flow.State!=RabbitWaterLifeFlow.Phase.Finished;i++)h.Advance(1f/60);
            Check(flow.State==RabbitWaterLifeFlow.Phase.Finished&&h.Trips==1&&h.DoorClosed,"full exit/water repeat failed "+cycle);
        }
        rows.Add("20 full routines from explicit indoor reset: exit, close wait, water, storage completed");
        foreach(var phase in new[]{RabbitWaterLifeFlow.Phase.Exiting,RabbitWaterLifeFlow.Phase.ToCan,RabbitWaterLifeFlow.Phase.Picking,RabbitWaterLifeFlow.Phase.ToFlowers,RabbitWaterLifeFlow.Phase.Watering,RabbitWaterLifeFlow.Phase.ToStorage,RabbitWaterLifeFlow.Phase.Putting}){
            h.SelectLifeFlow();flow.StartFlow();
            for(int i=0;i<60*80&&flow.State!=phase;i++)h.Advance(1f/60);
            Check(flow.State==phase,"phase unavailable "+phase);
            h.Advance(.12f);double blink=h.AutomaticBlinkClock;var p=h.resident.transform.position;var can=w.can.position;
            h.paused=true;h.Advance(1);Check(h.resident.transform.position==p&&w.can.position==can&&h.AutomaticBlinkClock==blink,"pause changed routine");h.paused=false;
            flow.StartFlow();Check(flow.State==phase,"repeat restarted routine");flow.Stop();flow.Stop();
            for(int i=0;i<60*60&&flow.State!=RabbitWaterLifeFlow.Phase.Held;i++)h.Advance(1f/60);
            Check(flow.State==RabbitWaterLifeFlow.Phase.Held,"stop not settled "+phase);p=h.resident.transform.position;h.Advance(1);Check(Vector3.Distance(p,h.resident.transform.position)<.001f,"held routine moved");
            flow.StartFlow();for(int i=0;i<60*80&&flow.State!=RabbitWaterLifeFlow.Phase.Finished;i++)h.Advance(1f/60);
            Check(flow.State==RabbitWaterLifeFlow.Phase.Finished,"resume failed "+phase);
        }
        h.SelectLifeFlow();h.Advance(.5f);double clock=h.AutomaticBlinkClock;h.SelectWaterMode(true);Check(!h.FlowMode&&h.WaterMode,"mode owner cleanup");Check(h.AutomaticBlinkClock==clock,"mode reset blink");h.SelectBenchMode();h.SelectLifeFlow();
        rows.Add($"all sampled stages: grip={grip:F6} reach={reach:F6} sole={sole:F6} gap={gap:F6} drift={drift:F6} storage={storageError:F6} propBoundary={w.MaxCanBoundaryJump:F6}");
        File.WriteAllLines("Docs/RabbitWaterLifeVerification.txt",rows);Debug.Log(string.Join("\n",rows));
        Check(grip<=.01f&&reach<=.001f&&sole>=-.0005f&&gap<=.005f&&drift<=.0035f,"contact criteria; see report");
        Debug.Log("WATER_LIFE_VERIFY_OK");
    }
    static void Capture(Camera camera,string path){
        Directory.CreateDirectory(Path.GetDirectoryName(path));var rt=new RenderTexture(960,720,24);var old=camera.targetTexture;var active=RenderTexture.active;var tex=new Texture2D(960,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.aspect=4f/3;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();ReviewCaptureFile.Write(path,tex.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    public static void Render(){
        var h=Open();var flow=h.lifeFlow;
        foreach(float view in new[]{90f,130f,180f})foreach(bool pick in new[]{true,false})for(int i=0;i<8;i++){
            flow.Pose(pick,i);h.ViewWater(view);Capture(h.reviewCamera,$"Docs/Captures/RabbitWaterLife/{(pick?"Pickup":"Putdown")}{view}_Pose{i}.png");
        }
        foreach(float view in new[]{90f,130f,180f}){
            h.SelectLifeFlow();h.ViewWater(view);flow.StartFlow();int frame=0;
            do{Capture(h.reviewCamera,$"Logs/RabbitWaterLifeFrames/{view}/{frame:D4}.png");h.Advance(1f/30);frame++;}while(frame<30*65&&flow.State!=RabbitWaterLifeFlow.Phase.Finished);
            Capture(h.reviewCamera,$"Docs/Captures/RabbitWaterLife/Finished{view}.png");
            Debug.Log("WATER_LIFE_RENDER view="+view+" frames="+frame);
        }
        h.SelectLifeFlow();Debug.Log("WATER_LIFE_RENDER_OK");
    }
    public static void ClosePoses(){
        var h=Open();foreach(bool pick in new[]{true,false})for(int i=0;i<8;i++){
            h.lifeFlow.Pose(pick,i);var root=h.resident.transform.position;
            h.reviewCamera.fieldOfView=45;h.reviewCamera.transform.position=root+new Vector3(-2.6f,1.8f,1.0f);h.reviewCamera.transform.LookAt(root+Vector3.up*.9f);
            Capture(h.reviewCamera,$"Docs/Captures/RabbitWaterLife/Face{(pick?"Pickup":"Putdown")}_Pose{i}.png");
        }
        Debug.Log("WATER_LIFE_CLOSE_POSES_OK");
    }
}
