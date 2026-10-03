using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class RabbitWaterBenchLifeChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception("Water/bench life: "+message);}
    static RabbitHomeLifeReview Open(){EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");return UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();}
    public static void Execute(){try{
        AssetDatabase.Refresh();var hashes=ArtHashes();RabbitHomeLifeBuilder.BuildOnly();Verify();RepeatPositions();RabbitWaterLifeChecks.Verify();
        RabbitHomeGroundChecks.Execute();RabbitHomeLifeBuilder.Stress();RabbitWaterChecks.Verify();RabbitBenchChecks.Verify();
        Render();RabbitHomeLifeBuilder.BuildPlayer();var after=ArtHashes();Check(hashes.All(p=>after[p.Key]==p.Value),"existing art bytes changed");File.WriteAllLines("Docs/RabbitWaterBenchLifePreservation.txt",hashes.Select(p=>p.Key+" SHA256 "+p.Value+" unchanged"));Debug.Log("WATER_BENCH_LIFE_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void BuildAndVerify(){try{AssetDatabase.Refresh();RabbitHomeLifeBuilder.BuildOnly();Verify();Debug.Log("WATER_BENCH_LIFE_DRAFT_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void Inspect(){
        AssetDatabase.Refresh();RabbitHomeLifeBuilder.BuildOnly();var h=Open();h.SelectLifeFlow();h.lifeFlow.StartFlow(true);float peak=0;
        for(int i=0;i<240*65&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
            h.Advance(1f/240);var b=h.benchRest;if(b.Active&&b.MaxReach>peak+.00001f){peak=b.MaxReach;Debug.Log($"BENCH_REACH_PEAK {b.State} t={b.TimeInState:F4} reach={peak:F6} {b.ReachDetail}");}
            if(h.watering.Active&&h.watering.MaxReach>.001f){Debug.LogError("WATER_REACH "+h.lifeFlow.State+"/"+h.watering.State+" t="+h.watering.TimeInState+" reach="+h.watering.MaxReach);break;}
        }
        Debug.Log("BENCH_INSPECT_FINAL "+h.lifeFlow.State+" reach="+peak);
    }
    public static void RepeatPositions(){
        var h=Open();h.SelectLifeFlow();var f=h.lifeFlow;Vector3 terminal=Vector3.zero;Quaternion facing=Quaternion.identity;float maxPosition=0,maxFacing=0,maxCan=0;
        foreach(bool indoor in new[]{false,true})for(int cycle=0;cycle<20;cycle++){
            if(indoor)h.ResetLifeFlow();f.StartFlow(true);
            for(int i=0;i<60*100&&f.State!=RabbitWaterLifeFlow.Phase.Finished;i++)h.Advance(1f/60);
            Check(f.State==RabbitWaterLifeFlow.Phase.Finished,"repeat position timeout");
            if(!indoor&&cycle==0){terminal=h.resident.transform.position;facing=h.resident.transform.rotation;}
            maxPosition=Mathf.Max(maxPosition,Vector3.Distance(terminal,h.resident.transform.position));maxFacing=Mathf.Max(maxFacing,Quaternion.Angle(facing,h.resident.transform.rotation));maxCan=Mathf.Max(maxCan,Vector3.Distance(f.storage.position,h.watering.can.position));
        }
        Check(maxPosition<=.01f&&maxFacing<=.1f&&maxCan<.001f,"resident/can repeat accumulation");
        string result=$"20 outdoor +20 indoor: resident terminal variation={maxPosition:F6}m facing={maxFacing:F6}deg can={maxCan:F6}m; terminal={terminal:F6}. Automatic, not OS input.";
        File.WriteAllText("Docs/RabbitWaterBenchLifeRepeat.txt",result);Debug.Log("WATER_BENCH_REPEAT_POSITIONS_OK "+result);
    }
    public static void BuildCurrentPlayer(){
        AssetDatabase.Refresh();var h=Open();h.SelectLifeFlow();
        foreach(float angle in new[]{0f,45f,90f}){
            h.ViewLifeFlow(angle);var expected=new Vector3(-.6f,1.05f,-3.8f)+Quaternion.Euler(15,angle,0)*new Vector3(0,0,-10);
            Check(Vector3.Distance(expected,h.reviewCamera.transform.position)<.001f,"runtime/render camera mismatch "+angle);
        }
        h.ResetStudy();RabbitHomeLifeBuilder.BuildPlayer();Debug.Log("WATER_BENCH_CAMERA_PLAYER_BUILD_OK");
    }
    public static void FinalizeReview(){try{AssetDatabase.Refresh();var hashes=ArtHashes();RabbitHomeLifeBuilder.BuildOnly();Verify();Render();RabbitHomeLifeBuilder.BuildPlayer();
        var after=ArtHashes();Check(hashes.All(p=>after[p.Key]==p.Value),"existing art bytes changed");File.WriteAllLines("Docs/RabbitWaterBenchLifePreservation.txt",hashes.Select(p=>p.Key+" SHA256 "+p.Value+" unchanged"));Debug.Log("WATER_BENCH_LIFE_FINALIZE_OK");
    }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static Dictionary<string,string> ArtHashes(){
        var paths=new[]{"ArtSource/AdultRabbit - 01.blend","ArtSource/AdultRabbitMotion.blend","ArtSource/AdultRabbitHome.blend","ArtSource/AdultRabbitWater.blend","ArtSource/AdultRabbitBench.blend","Assets/Art/Generated/AdultRabbit/AdultRabbitMotion.fbx","Assets/Art/Generated/RabbitHome/AdultRabbitHome.fbx","Assets/Art/Generated/RabbitWater/AdultRabbitWater.fbx","Assets/Art/Generated/RabbitBench/AdultRabbitBench.fbx"};
        using(var sha=System.Security.Cryptography.SHA256.Create())return paths.ToDictionary(p=>p,p=>BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-",""));
    }
    public static void Verify(){
        var h=Open();var f=h.lifeFlow;var b=h.benchRest;var w=h.watering;
        var rows=new List<string>{"v0.128 water-to-bench routine; automated functions, not OS input or user quality approval."};
        float drift=0,gap=0,sole=100,reach=0,rootJump=0,handoff=0,ownerJump=0;float completion=0;
        Action contacts=()=>{
            Check(!(w.Active&&b.Active),"two resident pose owners");
            ownerJump=Mathf.Max(ownerJump,f.MaxOwnerPositionJump);
            if(w.Active)Check(w.MaxGripError<=.01f&&w.MaxReach<=.001f&&w.MaxDrift<=.0035f&&w.MaxSupportGap<=.005f&&w.MinSole>=-.0005f,$"connected water contacts {f.State}/{w.State}: grip={w.MaxGripError:F6} reach={w.MaxReach:F6} drift={w.MaxDrift:F6} gap={w.MaxSupportGap:F6} sole={w.MinSole:F6} {w.WalkingDetail}");
            if(b.Active){if(b.MaxReach>reach+.00001f)rows.Add($"reach peak {f.State}/{b.State} t={b.TimeInState:F4} {b.ReachDetail}");drift=Mathf.Max(drift,b.MaxDrift);gap=Mathf.Max(gap,b.MaxGap);sole=Mathf.Min(sole,b.MinSole);reach=Mathf.Max(reach,b.MaxReach);}
        };
        Action<int,bool,bool> run=(fps,slow,geometry)=>{
            h.slow=slow;f.StartFlow(true);bool rested=false;var previous=h.resident.transform.position;var oldOwner=f.BenchOwned;
            var bones=h.resident.GetComponentsInChildren<Transform>().Where(t=>new[]{"Pelvis","Spine","Head","Hand_L","Hand_R"}.Contains(t.name)).ToArray();var oldPoints=bones.Select(t=>t.position).ToArray();
            int n=0;for(;n<fps*160&&f.State!=RabbitWaterLifeFlow.Phase.Finished;n++){
                h.Advance(1f/fps);contacts();rootJump=Mathf.Max(rootJump,Vector3.Distance(previous,h.resident.transform.position));previous=h.resident.transform.position;
                if(oldOwner!=f.BenchOwned){for(int j=0;j<bones.Length;j++)handoff=Mathf.Max(handoff,Vector3.Distance(oldPoints[j],bones[j].position));}
                oldOwner=f.BenchOwned;oldPoints=bones.Select(t=>t.position).ToArray();
                if(f.BenchOwned){rested|=b.State==RabbitBenchReview.TaskState.Resting;if(geometry){RabbitBenchChecks.CheckConnectedPose(h);RabbitBenchChecks.CheckStoredCanClearance(h);Check(h.DoorClosed&&h.resident.transform.position.z<h.hinge.position.z-.6f,"bench route entered doorway swing area");}Check(w.can.gameObject.activeSelf&&Vector3.Distance(w.can.position,f.storage.position)<.001f,"can moved/hidden during bench");}
            }
            completion=n/(float)fps;
            Check(rested&&f.State==RabbitWaterLifeFlow.Phase.Finished&&b.State==RabbitBenchReview.TaskState.Standing,"routine timeout "+f.State+"/"+b.State);
            Check(h.DoorClosed&&b.TurnSteps<=3,"door/rotation completion");
            Check(Vector3.Distance(w.can.position,f.storage.position)<.001f&&Quaternion.Angle(w.can.rotation,f.storage.rotation)<.1f,"can original transform");
        };
        h.SelectLifeFlow();run(240,false,true);rows.Add($"240Hz complete={completion:F4}s rootStep={rootJump:F6} handoffBoneStep={handoff:F6}");
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true}){h.ResetLifeFlow();run(fps,slow,false);rows.Add($"fps={fps} slow={slow} complete={completion:F4} sole={b.MinSole:F6} gap={b.MaxGap:F6} drift={b.MaxDrift:F6} reach={b.MaxReach:F6}");}
        h.ResetLifeFlow();for(int i=0;i<20;i++){run(60,false,false);Check(f.Completed==i+1,"outdoor repeat counter");}rows.Add("20 outdoor routines: PASS, no actor/can reset between runs");
        for(int i=0;i<20;i++){h.ResetLifeFlow();run(60,false,false);Check(h.Trips==1,"indoor exit missing");}rows.Add("20 indoor-to-end routines: PASS");
        foreach(var phase in new[]{RabbitWaterLifeFlow.Phase.Exiting,RabbitWaterLifeFlow.Phase.ToCan,RabbitWaterLifeFlow.Phase.Picking,RabbitWaterLifeFlow.Phase.ToFlowers,RabbitWaterLifeFlow.Phase.Watering,RabbitWaterLifeFlow.Phase.ToStorage,RabbitWaterLifeFlow.Phase.Putting,RabbitWaterLifeFlow.Phase.ToBench,RabbitWaterLifeFlow.Phase.BenchSitting,RabbitWaterLifeFlow.Phase.BenchResting,RabbitWaterLifeFlow.Phase.BenchRising,RabbitWaterLifeFlow.Phase.BenchDeparting}){
            h.ResetLifeFlow();f.StartFlow(true);for(int n=0;n<60*100&&f.State!=phase;n++)h.Advance(1f/60);Check(f.State==phase,"unavailable phase "+phase);
            h.Advance(.12f);var p=h.resident.transform.position;var can=w.can.position;double blink=h.AutomaticBlinkClock;h.paused=true;h.Advance(1);Check(p==h.resident.transform.position&&can==w.can.position&&blink==h.AutomaticBlinkClock,"paused routine changed");h.paused=false;
            f.StartFlow(true);Check(f.State==phase,"repeat restarted "+phase);f.Stop();f.Stop();for(int n=0;n<60*30&&f.State!=RabbitWaterLifeFlow.Phase.Held;n++)h.Advance(1f/60);Check(f.State==RabbitWaterLifeFlow.Phase.Held,"stop not safe "+phase);
            p=h.resident.transform.position;h.Advance(1);Check(Vector3.Distance(p,h.resident.transform.position)<.001f,"held drift");
            if(phase==RabbitWaterLifeFlow.Phase.BenchResting||phase==RabbitWaterLifeFlow.Phase.BenchSitting)Check(b.State==RabbitBenchReview.TaskState.Resting,"stop should stay seated");
            f.StartFlow(true);for(int n=0;n<60*120&&f.State!=RabbitWaterLifeFlow.Phase.Finished;n++){h.Advance(1f/60);contacts();}Check(f.State==RabbitWaterLifeFlow.Phase.Finished,"resume stalled "+phase);rows.Add("stop/pause/repeat/resume "+phase+": PASS");
        }
        h.ResetLifeFlow();f.StartFlow(true);h.Advance(32);double saved=h.AutomaticBlinkClock;h.SelectBenchMode();Check(!h.FlowMode&&!w.Active&&b.Active&&saved==h.AutomaticBlinkClock,"mode cleanup/blink clock");h.ResetLifeFlow();Check(!w.Active&&!b.Active&&h.Inside&&h.DoorClosed&&w.can.gameObject.activeSelf,"reset cleanup");
        rows.Add($"contacts sole={sole:F6} gap={gap:F6} drift={drift:F6} reach={reach:F6}; rootStep={rootJump:F6}; handoffFrameStep={handoff:F6} (includes intentional movement); exactOwnerJump={ownerJump:F6}; ownership/can/face/reset: PASS");
        File.WriteAllLines("Docs/RabbitWaterBenchLifeVerification.txt",rows);Debug.Log(string.Join("\n",rows));
        Check(sole>=-.0005f&&gap<=.005f&&drift<=.0035f&&reach<=.001f&&rootJump<.08f&&ownerJump<.001f,"contact or exact owner boundary criteria");Debug.Log("WATER_BENCH_LIFE_VERIFY_OK");
    }
    public static void Capture(Camera c,string path){
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var skins=UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>().Where(s=>s.enabled&&s.gameObject.activeInHierarchy).ToArray();var previews=new List<GameObject>();var meshes=new List<Mesh>();
        foreach(var s in skins){var m=UnityEngine.Object.Instantiate(s.sharedMesh);m.vertices=AdultRabbitSitCoat.World(s).Select(s.transform.InverseTransformPoint).ToArray();m.RecalculateNormals();m.RecalculateBounds();var p=new GameObject("Evaluated routine pose");p.transform.SetParent(s.transform,false);p.AddComponent<MeshFilter>().sharedMesh=m;p.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;s.enabled=false;previews.Add(p);meshes.Add(m);}
        var rt=new RenderTexture(960,720,24);var old=c.targetTexture;var active=RenderTexture.active;var tex=new Texture2D(960,720,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.aspect=4f/3;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();ReviewCaptureFile.Write(path,tex.EncodeToPNG());}
        finally{foreach(var p in previews)UnityEngine.Object.DestroyImmediate(p);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);foreach(var s in skins)s.enabled=true;c.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    public static void Render(){
        var h=Open();foreach(float angle in new[]{0f,45f,90f}){
            h.SelectLifeFlow();h.ViewLifeFlow(angle);var camera=h.reviewCamera;h.lifeFlow.StartFlow(true);
            var seen=new HashSet<RabbitWaterLifeFlow.Phase>();int frame=0;
            do{Capture(camera,$"Logs/RabbitWaterBenchLifeFrames/{angle}/{frame:D4}.png");if(seen.Add(h.lifeFlow.State))Capture(camera,$"Docs/Captures/RabbitWaterBenchLife/{angle}_{h.lifeFlow.State}.png");h.Advance(1f/30);frame++;}while(frame<30*90&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished);
            Capture(camera,$"Docs/Captures/RabbitWaterBenchLife/{angle}_Finished.png");Check(h.lifeFlow.State==RabbitWaterLifeFlow.Phase.Finished,"render timeout");Debug.Log("WATER_BENCH_LIFE_RENDER "+angle+" frames="+frame);
        }
        h.SelectLifeFlow();Debug.Log("WATER_BENCH_LIFE_RENDER_OK");
    }
}
