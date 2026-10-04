using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class RabbitCanAvoidanceChecks {
    static readonly List<string> report=new List<string>();
    static void Check(bool ok,string text){if(!ok)throw new Exception("Can avoidance133: "+text);}
    static void Contacts(RabbitBenchReview b){Check(b.MinSole>=-.0005f&&b.MaxGap<=.005f&&b.MaxDrift<=.0035f&&b.MaxReach<=.001f,$"contacts {b.State} sole={b.MinSole:F6} gap={b.MaxGap:F6} drift={b.MaxDrift:F6} reach={b.MaxReach:F6}");}
    static AdultRabbitFootTransition Feet(RabbitBenchReview b)=>(AdultRabbitFootTransition)typeof(RabbitBenchReview).GetField("feet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b);
    static RabbitHomeLifeReview Open(){EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");return UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();}
    public static void Draft(){try{
        var h=Open();h.SelectLifeFlow();h.lifeFlow.StartFlow(true);var old=RabbitBenchReview.TaskState.Standing;
        for(int i=0;i<240*100&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
            h.Advance(1f/240);if(!h.lifeFlow.BenchOwned)continue;var b=h.benchRest;
            if(b.State!=old){Debug.Log($"CAN133_STATE {b.State} at={i/240f:F3} root={h.resident.transform.position:F4} blocked={b.PathBlocked} drift={b.MaxDrift:F6} reach={b.MaxReach:F6}");old=b.State;}
            if(b.PathBlocked)throw new Exception("Default local route blocked at "+h.resident.transform.position);
            if(i%8==0){RabbitBenchChecks.CheckStoredCanClearance(h);RabbitBenchChecks.CheckConnectedPose(h);}
            if(b.MaxDrift>.0035f||b.MaxGap>.005f||b.MinSole<-.0005f||b.MaxReach>.001f)throw new Exception($"Contacts {b.State} drift={b.MaxDrift} gap={b.MaxGap} reach={b.MaxReach} {b.ReachDetail}");
        }
        if(h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished)throw new Exception("Default flow timeout");
        Debug.Log("CAN133_DRAFT_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Before(){try{
        var h=Open();h.SelectLifeFlow();h.lifeFlow.StartFlow(true);int frame=0;
        for(int i=0;i<60*100&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
            h.Advance(1f/60);
            if(h.lifeFlow.BenchOwned&&frame<160&&i%4==0){h.ViewLifeFlow(45);RabbitWaterBenchLifeChecks.Capture(h.reviewCamera,$"Logs/CanAvoidance133/Before/{frame++:D4}.png");}
            if(h.lifeFlow.BenchOwned&&i%120==0)Debug.Log($"CAN133_BEFORE {h.benchRest.State} root={h.resident.transform.position:F4} can={h.watering.can.position:F4} bench={h.benchRest.BenchBounds}");
        }
        foreach(var t in h.GetComponentsInChildren<Transform>())if(t.name.Contains("bed")||t.name.Contains("Soil"))Debug.Log("CAN133_OBJECT "+t.name+" parent="+t.parent.name+" position="+t.position);
        Debug.Log("CAN133_BEFORE_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Verify(){try{
        report.Clear();report.Add("v0.133 local can avoidance; automatic checks, not OS mouse input or quality approval.");
        var h=Open();var b=h.benchRest;
        foreach(int fps in new[]{240,30,60,120})foreach(bool slow in new[]{false,true}){
            h.SelectLifeFlow();h.slow=slow;h.lifeFlow.StartFlow(true);int walkingEntries=0;var old=b.State;float drop=0,bend=0,change=0,last=0;AdultRabbitFootTransition previous=null;
            for(int i=0;i<fps*130&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
                h.Advance(1f/fps);if(!h.lifeFlow.BenchOwned)continue;
                Check(!b.PathBlocked,"default flow blocked");Contacts(b);
                if(b.State==RabbitBenchReview.TaskState.Walking&&old!=b.State)walkingEntries++;old=b.State;
                var feet=Feet(b);if(feet!=null&&b.State==RabbitBenchReview.TaskState.Walking){drop=Mathf.Max(drop,feet.AdditionalDrop);bend=Mathf.Max(bend,feet.AdditionalSupportBend);if(previous==feet)change=Mathf.Max(change,Mathf.Abs(feet.AdditionalDrop-last));previous=feet;last=feet.AdditionalDrop;}else previous=null;
                if(fps==240){RabbitBenchChecks.CheckStoredCanClearance(h);if(i%8==0)RabbitBenchChecks.CheckConnectedPose(h);CheckFlower(h);}
            }
            Check(h.lifeFlow.State==RabbitWaterLifeFlow.Phase.Finished,"flow timeout");Check(walkingEntries==1,"avoidance waypoint stopped/restarted walk");Check(b.AvoidanceTurnSteps<=3,"too many clearing steps");
            Check(drop<=.010001f&&bend<=10.001f&& (fps!=240||change<=.002001f),$"posture drop={drop} bend={bend} delta={change}");
            report.Add($"{fps}fps {(slow?.5f:1)}x finished; one continuous approach; prep steps={b.AvoidanceTurnSteps}; drop={drop:F6} knee={bend:F4} correctionStep={change:F6}; drift={b.MaxDrift:F6}");
        }
        Debug.Log("CAN133_DEFAULT_MATRIX_OK");
        int held=0,completed=0;
        foreach(int side in Enumerable.Range(0,8))foreach(float yaw in new[]{0f,90f,180f,270f}){
            h.SelectLifeFlow();h.slow=false;var can=h.watering.can;can.SetPositionAndRotation(h.lifeFlow.storage.position,Quaternion.Euler(0,yaw,0));
            var origin=can.position+Quaternion.Euler(0,side*45,0)*Vector3.back*.58f;origin.y=h.groundHeight;
            h.resident.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,side*45,0));h.idleClip.SampleAnimation(h.resident,0);new RabbitHomeFootwork(h.resident,h.groundHeight){FitStandingReach=true}.Hold();b.EnterCurrent();b.StartConnectedFlow(can);
            if(b.PathBlocked){held++;Check(Vector3.Distance(origin,h.resident.transform.position)<.00001f,"blocked angular route moved");continue;}
            bool rest=false;for(int i=0;i<240*65;i++){b.AdvanceFlow(1f/240);Contacts(b);CheckFlower(h);rest|=b.State==RabbitBenchReview.TaskState.Resting;if(i%8==0){RabbitBenchChecks.CheckStoredCanClearance(h);RabbitBenchChecks.CheckConnectedPose(h);}if(rest&&b.State==RabbitBenchReview.TaskState.Standing)break;Check(i<240*65-1,"angular timeout");}
            completed++;
        }
        report.Add($"8 approach directions x4 can directions: {completed} completed, {held} safely blocked; no root relocation when blocked.");Debug.Log("CAN133_ANGULAR_OK "+report.Last());
        foreach(float after in new[]{.15f,.6f,1.1f,2f,3f}){
            h.SelectLifeFlow();h.lifeFlow.StartFlow(true);for(int i=0;i<60*50&&!h.lifeFlow.BenchOwned;i++)h.Advance(1f/60);Check(h.lifeFlow.BenchOwned,"handoff missing");h.Advance(after);
            h.lifeFlow.Stop();for(int i=0;i<60*10&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Held;i++){h.Advance(1f/60);Contacts(b);CheckFlower(h);RabbitBenchChecks.CheckStoredCanClearance(h);}
            Check(h.lifeFlow.State==RabbitWaterLifeFlow.Phase.Held,"stop unsafe");var position=h.resident.transform.position;h.Advance(1);Check(Vector3.Distance(position,h.resident.transform.position)<.001f,"held drift");
            h.lifeFlow.StartFlow(true);for(int i=0;i<60*80&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){h.Advance(1f/60);CheckFlower(h);RabbitBenchChecks.CheckStoredCanClearance(h);}Check(h.lifeFlow.State==RabbitWaterLifeFlow.Phase.Finished,"resume failed");Contacts(b);
        }
        report.Add("Preparation/walking stop and current-pose replan at5 offsets: PASS");
        OneSide(h);Blocked(h);
        File.WriteAllLines("Docs/RabbitCanAvoidance133Verification.txt",report);Debug.Log("CAN133_VERIFY_OK");
    }catch(Exception e){report.Add("FAILED: "+e);File.WriteAllLines("Docs/RabbitCanAvoidance133Verification.txt",report);Debug.LogException(e);EditorApplication.Exit(1);}}
    static void CheckFlower(RabbitHomeLifeReview h){
        var bed=h.GetComponentsInChildren<Transform>().First(t=>t.name=="Watering flower bed");Bounds bounds=bed.GetComponentsInChildren<Renderer>().First().bounds;foreach(var r in bed.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
        foreach(var skin in h.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name=="Shoes"))foreach(var p in AdultRabbitSitCoat.World(skin)){
            float dx=Mathf.Max(bounds.min.x-p.x,0,p.x-bounds.max.x),dz=Mathf.Max(bounds.min.z-p.z,0,p.z-bounds.max.z);
            Check(Mathf.Sqrt(dx*dx+dz*dz)>=.04999f,"shoe enters flower bed margin");
        }
    }
    static void Blocked(RabbitHomeLifeReview h){
        h.SelectLifeFlow();var can=h.watering.can;var origin=h.lifeFlow.storage.position+Vector3.right*.58f;origin.y=h.groundHeight;
        h.resident.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,-90,0));h.benchRest.EnterCurrent();
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetParent(h.transform);blocker.transform.position=origin+Vector3.up*.4f;blocker.transform.localScale=new Vector3(6,1,6);
        try{h.benchRest.StartConnectedFlow(can);h.benchRest.AdvanceFlow(1);Check(h.benchRest.PathBlocked&&h.benchRest.Label=="이동 경로 막힘"&&Vector3.Distance(origin,h.resident.transform.position)<.001f,"blocked path did not hold");}finally{UnityEngine.Object.DestroyImmediate(blocker);}
        h.benchRest.ResumeFlow();Check(!h.benchRest.PathBlocked,"unblocked resume did not replan");report.Add("Both exits obstructed: standing/block label; blocker removed: replan PASS");
    }
    static void OneSide(RabbitHomeLifeReview h){
        foreach(float side in new[]{-1f,1f}){
            h.SelectLifeFlow();var can=h.watering.can;can.SetPositionAndRotation(new Vector3(0,h.groundHeight+.1525f,-6),Quaternion.identity);
            var origin=new Vector3(.58f,h.groundHeight,-6);h.resident.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,-90,0));h.idleClip.SampleAnimation(h.resident,0);new RabbitHomeFootwork(h.resident,h.groundHeight){FitStandingReach=true}.Hold();h.benchRest.EnterCurrent();
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="One side local blocker";blocker.transform.SetParent(h.transform);blocker.transform.position=origin+Vector3.forward*(side*.75f)+Vector3.up*.45f;blocker.transform.localScale=new Vector3(.6f,.9f,.35f);
            try{
                h.benchRest.StartConnectedFlow(can);Check(!h.benchRest.PathBlocked,"one side blocked, other side should be available");bool rested=false;
                for(int i=0;i<240*60;i++){
                    h.benchRest.AdvanceFlow(1f/240);Contacts(h.benchRest);rested|=h.benchRest.State==RabbitBenchReview.TaskState.Resting;
                    if(i%8==0){RabbitBenchChecks.CheckStoredCanClearance(h);var bounds=blocker.GetComponent<Renderer>().bounds;foreach(var skin in h.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name=="Shoes"||s.name=="Bottom"))foreach(var p in AdultRabbitSitCoat.World(skin)){float dx=Mathf.Max(bounds.min.x-p.x,0,p.x-bounds.max.x),dz=Mathf.Max(bounds.min.z-p.z,0,p.z-bounds.max.z);Check(Mathf.Sqrt(dx*dx+dz*dz)>=.04999f,"one side blocker clearance");}}
                    if(rested&&h.benchRest.State==RabbitBenchReview.TaskState.Standing)break;Check(i<240*60-1,"one side exit timeout");
                }
            }finally{UnityEngine.Object.DestroyImmediate(blocker);}
        }
        report.Add("Left/right local exit obstructed individually: alternative route completes with5cm shoe/leg clearance PASS");
    }
    public static void After(){try{
        var h=Open();foreach(float angle in new[]{0f,45f,90f}){h.SelectLifeFlow();h.ViewLifeFlow(angle);h.lifeFlow.StartFlow(true);int frame=0;float time=0;bool started=false;
            for(int i=0;i<15*90&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
                h.Advance(1f/15);if(!h.lifeFlow.BenchOwned)continue;
                if(!started){started=true;RabbitWaterBenchLifeChecks.Capture(h.reviewCamera,$"Docs/Captures/RabbitCanAvoidance133/{angle}_Start.png");}
                if(time<10.7f)RabbitWaterBenchLifeChecks.Capture(h.reviewCamera,$"Logs/CanAvoidance133/After/{angle}/{frame++:D4}.png");time+=1f/15;
            }
            Check(h.lifeFlow.State==RabbitWaterLifeFlow.Phase.Finished,"render timeout");
        }Debug.Log("CAN133_RENDER_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Final(){try{
        var hashes=RabbitWaterBenchLifeChecks.ArtHashes();var sceneHash=File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity");
        Verify();RabbitWaterBenchLifeChecks.Verify();RabbitWaterBenchLifeChecks.RepeatPositions();RabbitWalkingTurnChecks.Verify();RabbitHomeGroundChecks.Execute();RabbitHomeLifeBuilder.AutomaticClosingVerify();RabbitWaterChecks.Verify();RabbitBenchChecks.Verify();
        After();RabbitHomeLifeBuilder.BuildPlayer();
        var after=RabbitWaterBenchLifeChecks.ArtHashes();Check(hashes.All(p=>after[p.Key]==p.Value),"model/clip source changed");Check(sceneHash.SequenceEqual(File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity")),"saved scene changed");
        report.Add("Existing art/source FBX and saved scene hashes preserved; walking/door/water/bench/full-flow repeat regressions and Windows build PASS. OS input not performed.");File.WriteAllLines("Docs/RabbitCanAvoidance133Verification.txt",report);Debug.Log("CAN133_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Finish(){try{
        // Full regressions were run in Final; repeat the focused suite after the
        // exact-pose cache optimization and additional one-side blockers.
        var hashes=RabbitWaterBenchLifeChecks.ArtHashes();var scene=File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity");
        Verify();After();RabbitHomeLifeBuilder.BuildPlayer();
        var after=RabbitWaterBenchLifeChecks.ArtHashes();Check(hashes.All(p=>after[p.Key]==p.Value)&&scene.SequenceEqual(File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity")),"preserved art/scene changed");
        report.Add("Focused final suite, three-view render and Windows build PASS; art/source FBX/saved scene preserved. Full regressions: Logs/can133-final2.log. OS input not performed.");File.WriteAllLines("Docs/RabbitCanAvoidance133Verification.txt",report);Debug.Log("CAN133_FINISH_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
