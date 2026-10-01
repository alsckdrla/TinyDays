using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;
public static class RabbitWaterChecks {
    const string Scene="Assets/Scenes/RabbitHomeLifeStudy.unity",Out="Docs/Captures/RabbitWater";
    static void Check(bool ok,string text){if(!ok)throw new Exception("Water review: "+text);}
    static RabbitHomeLifeReview Open(){EditorSceneManager.OpenScene(Scene);var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();Check(h&&h.watering,"missing water scene");h.SelectWaterMode(true);return h;}
    static void Capture(Camera c,string name){
        Directory.CreateDirectory(Out);string path=(name.StartsWith("Sequence")?"Logs/RabbitWaterV0120":Out)+"/"+name+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var rt=new RenderTexture(960,720,24);var old=c.targetTexture;var active=RenderTexture.active;var tex=new Texture2D(960,720,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.aspect=4f/3;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{c.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    static void Contacts(RabbitWaterReview w){Check(w.MaxGripError<=.01f&&w.MaxReach<=.001f&&w.MinSole>=-.0005f&&w.MaxDrift<=.0035f&&w.MaxSupportGap<=.005f,$"contacts grip={w.MaxGripError} reach={w.MaxReach} sole={w.MinSole} drift={w.MaxDrift} gap={w.MaxSupportGap}");}
    public static void Draft(){
        var h=Open();var w=h.watering;var rows=new List<string>();
        foreach(float angle in new[]{90f,130f,180f}){h.ViewWater(angle);Capture(h.reviewCamera,"Carry"+angle);}
        w.StartWalk(true);bool poured=false;
        var watches=h.resident.GetComponentsInChildren<Transform>().Where(t=>new[]{"Spine","Head","Hand_L","Hand_R"}.Contains(t.name)).ToArray();
        var previous=watches.Select(t=>t.position).ToArray();var state=w.State;float maxBoundary=0,maxHandSpeed=0;
        var csv=new List<string>{"time,state,bone,x,y,z,speed"};
        for(int i=0;i<240*18;i++){
            h.Advance(1f/240);
            for(int j=0;j<watches.Length;j++){
                var p=watches[j].position;float speed=Vector3.Distance(p,previous[j])*240;
                if(watches[j].name.StartsWith("Hand"))maxHandSpeed=Mathf.Max(maxHandSpeed,speed);
                if(state!=w.State)maxBoundary=Mathf.Max(maxBoundary,Vector3.Distance(p,previous[j]));
                csv.Add(FormattableString.Invariant($"{i/240f:F6},{w.State},{watches[j].name},{p.x:F6},{p.y:F6},{p.z:F6},{speed:F6}"));previous[j]=p;
            }state=w.State;
            if(!poured&&w.State==RabbitWaterReview.TaskState.Pouring&&w.TimeInState>=2){poured=true;foreach(float angle in new[]{90f,130f,180f}){h.ViewWater(angle);Capture(h.reviewCamera,"Pour"+angle);}}
        }
        rows.Add($"state {w.State}; poured {poured}; distance {w.DistanceTravelled:F6}; grip {w.MaxGripError:F6}; reach {w.MaxReach:F6}; sole {w.MinSole:F6}; support gap {w.MaxSupportGap:F6}; drift {w.MaxDrift:F6}");
        foreach(var t in h.resident.GetComponentsInChildren<Transform>().Where(t=>new[]{"Spine","UpperArm_L","Hand_L","UpperArm_R","Hand_R"}.Contains(t.name)))rows.Add(t.name+" "+t.position.ToString("F4"));
        rows.Add("Can "+w.can.position.ToString("F4")+" scales "+w.can.GetChild(0).lossyScale);File.WriteAllLines("Docs/RabbitWaterVerification.txt",rows);Debug.Log(string.Join("\n",rows));
        rows.Add($"Droplets {w.DropletsLanded}, outside soil {w.DropletsOutsideBed}; max boundary position delta {maxBoundary:F6}m/240Hz; max hand speed {maxHandSpeed:F6}m/s");File.WriteAllLines("Docs/RabbitWaterVerification.txt",rows);
        File.WriteAllLines("Docs/RabbitWaterMotion240.csv",csv);
        Check(poured&&w.State==RabbitWaterReview.TaskState.Ready,"flow did not finish");Contacts(w);h.SelectWaterMode(false);Debug.Log("WATER_DRAFT_OK");
    }
    public static void Verify(){
        var h=Open();var w=h.watering;var rows=new List<string>{"v0.120 water carry/pour deterministic checks. Not OS input or quality approval."};
        var probe=UnityEngine.Object.Instantiate(h.resident);probe.GetComponent<Animator>().enabled=false;
        var joints=probe.GetComponentsInChildren<Transform>();
        foreach(var clip in new[]{w.carryIdle,w.carryWalk}){
            clip.SampleAnimation(probe,0);var p0=joints.Select(t=>t.localPosition).ToArray();var q0=joints.Select(t=>t.localRotation).ToArray();
            clip.SampleAnimation(probe,1f/240);var p1=joints.Select(t=>t.localPosition).ToArray();
            clip.SampleAnimation(probe,clip.length-1f/240);var pN=joints.Select(t=>t.localPosition).ToArray();
            clip.SampleAnimation(probe,clip.length);float pos=0,rot=0,velocity=0;
            for(int i=0;i<joints.Length;i++){pos=Mathf.Max(pos,Vector3.Distance(joints[i].localPosition,p0[i]));rot=Mathf.Max(rot,Quaternion.Angle(joints[i].localRotation,q0[i]));velocity=Mathf.Max(velocity,Vector3.Distance(p1[i]-p0[i],joints[i].localPosition-pN[i])*240);}
            // Local positions include the FBX cm scale; convert using root hierarchy.
            rows.Add($"{clip.name}: {clip.length:F3}s; local boundary position {pos:F6}; angle {rot:F4}deg; local velocity delta {velocity:F6}");
            Check(pos<.001f&&rot<.1f,"carry loop discontinuity");
        }
        UnityEngine.Object.DestroyImmediate(probe);
        foreach(bool slow in new[]{false,true})foreach(int fps in new[]{30,60,120}){
            h.SelectWaterMode(true);h.slow=slow;w.StartWalk(true);bool pouring=false;
            int n=0;for(;n<fps*32;n++){h.Advance(1f/fps);pouring|=w.State==RabbitWaterReview.TaskState.Pouring;if(pouring&&w.State==RabbitWaterReview.TaskState.Ready)break;}
            Check(pouring&&w.State==RabbitWaterReview.TaskState.Ready,"flow timeout");Contacts(w);
            Check(w.DropletsLanded>0&&w.DropletsOutsideBed==0,$"water missed flower bed: {w.DropletsOutsideBed}/{w.DropletsLanded}");
            rows.Add($"{fps}fps {(slow?.5f:1)}x: {n/(float)fps:F3}s; distance {w.DistanceTravelled:F4}; grip {w.MaxGripError:F6}; reach {w.MaxReach:F6}; sole {w.MinSole:F6}; planted drift {w.MaxDrift:F6}");
        }
        foreach(int phase in Enumerable.Range(0,8)){
            h.SelectWaterMode(true);w.StartPour();h.Advance(phase*6f/8);float before=w.TimeInState;var position=w.can.position;var q=w.can.rotation;
            h.paused=true;h.Advance(2);Check(w.TimeInState==before&&w.can.position==position&&w.can.rotation==q,"pause changed pose");h.paused=false;
            w.StopTask();w.StopTask();h.Advance(.55f);Check(w.State==RabbitWaterReview.TaskState.Ready&&w.PourAmount==0,"cancel failed");Contacts(w);
        }
        rows.Add("8 pour phases: pause, resume, repeated stop, upright within .55s: PASS");
        foreach(float time in new[]{.1f,.3f,.7f,1.1f}){
            h.SelectWaterMode(true);w.StartWalk(true);h.Advance(time);w.StopTask();h.Advance(3);Check(w.State==RabbitWaterReview.TaskState.Ready,"walking cancel failed");Contacts(w);
        }
        for(int i=0;i<3;i++){h.SelectWaterMode(true);w.StartPour();h.Advance(2);h.SelectWaterMode(false);Check(!w.Active&&!w.can.gameObject.activeSelf&&h.Inside&&h.DoorClosed,"mode cleanup failed");}
        rows.Add("walking stops x4, mode restoration x3: PASS");
        File.WriteAllLines("Docs/RabbitWaterRegression.txt",rows);h.ResetStudy();Debug.Log("WATER_REGRESSION_OK");
    }
    public static void Render(){
        var h=Open();var w=h.watering;
        foreach(float angle in new[]{90f,130f,180f}){
            h.SelectWaterMode(true);h.ViewWater(angle);w.StartWalk(true);
            for(int frame=0;frame<30*12;frame++){h.Advance(1f/30);Capture(h.reviewCamera,$"Sequence{angle}/frame{frame:D4}");}
            for(int a=0;a<3;a++)for(int i=0;i<8;i++){w.Pose(a,i);h.ViewWater(angle);Capture(h.reviewCamera,$"Pose{a}_{i}_{angle}");}
        }
        h.SelectWaterMode(false);Debug.Log("WATER_RENDER_OK");
    }
    public static void Finish(){RabbitHomeLifeBuilder.Execute();Draft();Verify();RabbitHomeGroundChecks.FinalizeReview();Debug.Log("WATER_FINISH_OK");}
    public static void BuildAndCheck(){RabbitWaterBuilder.Build();Draft();Verify();}
    public static void Complete(){RabbitHomeLifeBuilder.Execute();Draft();Verify();Render();RabbitHomeGroundChecks.FinalizeReview();Debug.Log("WATER_COMPLETE_OK");}
}
