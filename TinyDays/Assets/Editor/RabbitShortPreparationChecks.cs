using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class RabbitShortPreparationChecks {
    static void Check(bool value,string message){if(!value)throw new Exception("Bench arrival135: "+message);}
    static RabbitHomeLifeReview Open(){EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");return UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();}
    public static void Before(){try{Capture(false);Debug.Log("BENCH135_BEFORE_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Capture(bool after){
        foreach(bool canSection in new[]{false,true})foreach(float view in after?new[]{0f,45f,90f}:new[]{45f}){
            var h=Open();h.SelectLifeFlow();h.lifeFlow.StartFlow(true);bool started=false;int frame=0;
            for(int i=0;i<60*100;i++){
                h.Advance(1f/60);var b=h.benchRest;
                if(h.lifeFlow.BenchOwned&&b.State==(canSection?RabbitBenchReview.TaskState.ClearingCan:RabbitBenchReview.TaskState.Turning))started=true;
                if(!started)continue;
                if(i%4==0){h.ViewLifeFlow(view);RabbitWaterBenchLifeChecks.Capture(h.reviewCamera,$"Logs/BenchArrival135/{(canSection?"Can":"Bench")}{(after?"After":"Before")}{view}/{frame++:D4}.png");}
                if(frame>=120)break;
            }
        }
    }
    public static void Draft(){try{Verify(false);Debug.Log("BENCH135_DRAFT_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Metrics(){try{
        var report=new List<string>{"v0.135 matched original full-target versus staged preparation, 240Hz; automatic, not quality approval."};
        var csv=new List<string>{"section,staged,time,pelvisY,headY,leftKneeBend,rightKneeBend,firstFootTravel"};
        foreach(bool canSection in new[]{false,true}){
            var h=Open();h.SelectLifeFlow();h.lifeFlow.StartFlow(true);var b=h.benchRest;
            var state=canSection?RabbitBenchReview.TaskState.ClearingCan:RabbitBenchReview.TaskState.Backstepping;
            for(int i=0;i<60*100&&(!h.lifeFlow.BenchOwned||b.State!=state);i++)h.Advance(1f/60);
            Check(b.State==state,"metrics preparation missing");
            var original=(RabbitHomeFootwork)typeof(RabbitBenchReview).GetField("support",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(b);
            var path=(Func<float,Pose>)typeof(RabbitHomeFootwork).GetField("route",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(original);
            float before=0,after=0;
            foreach(bool staged in new[]{false,true}){
                var actor=h.resident.transform;actor.SetPositionAndRotation(path(0).position,path(0).rotation);h.idleClip.SampleAnimation(h.resident,0);
                var joints=actor.GetComponentsInChildren<Transform>();var baseP=joints.Select(t=>t.localPosition).ToArray();var baseQ=joints.Select(t=>t.localRotation).ToArray();
                var support=new RabbitHomeFootwork(h.resident,h.groundHeight){FitStandingReach=true};
                typeof(RabbitHomeFootwork).GetProperty("ExactStepLowering",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(support,canSection?0:.035f);
                support.Hold();
                if(staged)support.BeginPreparation(path);else support.BeginExactPair(path);
                var foot=support.FootPosition(0);float first=0;
                for(int tick=0;tick<=Mathf.RoundToInt(support.Duration*240);tick++){
                    for(int j=1;j<joints.Length;j++){joints[j].localPosition=baseP[j];joints[j].localRotation=baseQ[j];}
                    float time=tick/240f;support.PlaceRoot(time);h.walkClip.SampleAnimation(h.resident,canSection?time%.8f:(.8f-time%.8f)%.8f);support.Apply(time);
                    float travel=Vector3.ProjectOnPlane(support.FootPosition(0)-foot,Vector3.up).magnitude;if(tick<=96)first=Mathf.Max(first,travel);
                    Func<string,float> bend=suffix=>{var hip=joints.First(t=>t.name=="Thigh_"+suffix).position;var knee=joints.First(t=>t.name=="Shin_"+suffix).position;var ankle=joints.First(t=>t.name=="Foot_"+suffix).position;return 180-Vector3.Angle(hip-knee,ankle-knee);};
                    csv.Add(FormattableString.Invariant($"{(canSection?"can":"bench")},{staged},{time:F6},{joints.First(t=>t.name=="Pelvis").position.y:F6},{joints.First(t=>t.name=="Head").position.y:F6},{bend("L"):F6},{bend("R"):F6},{travel:F6}"));
                }
                if(staged)after=first;else before=first;
            }
            Check(after<before*.65f,"first foot displacement was not reduced");report.Add($"{(canSection?"can":"bench")} first foot horizontal travel {before:F6} -> {after:F6}m; both knee angles and pelvis/head heights recorded at240Hz.");
        }
        File.WriteAllLines("Docs/RabbitShortPreparation135Metrics.txt",report);File.WriteAllLines("Docs/RabbitShortPreparation135Comparison240.csv",csv);Debug.Log("SHORT135_METRICS_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Verify(bool matrix){
        var rows=new List<string>{"v0.135 bench arrival; automatic callbacks, not OS input or quality approval."};
        var trace=new List<string>{"time,state,step,landingFraction,bone,x,y,z"};
        Check(RabbitHomeFootwork.PreparationFootfalls(new Pose(Vector3.zero,Quaternion.identity),new Pose(Vector3.forward*.15f,Quaternion.Euler(0,45,0)))==2,"short two-step rule");
        Check(RabbitHomeFootwork.PreparationFootfalls(new Pose(Vector3.zero,Quaternion.identity),new Pose(Vector3.forward*.151f,Quaternion.identity))==3,"distance three-step rule");
        Check(RabbitHomeFootwork.PreparationFootfalls(new Pose(Vector3.zero,Quaternion.identity),new Pose(Vector3.forward*.1f,Quaternion.Euler(0,46,0)))==3,"yaw three-step rule");
        foreach(bool connected in new[]{false,true})foreach(int fps in matrix?new[]{240,30,60,120}:new[]{240})foreach(bool slow in matrix?new[]{false,true}:new[]{false}){
            var h=Open();if(connected){h.SelectLifeFlow();h.lifeFlow.StartFlow(true);}else{h.SelectBenchMode();h.benchRest.StartFlow();}h.slow=slow;
            var b=h.benchRest;int entries=0;float time=0,previousTime=-1;bool rest=false;var old=b.State;
            for(int i=0;i<fps*140;i++){
                h.Advance(1f/fps);if(connected&&!h.lifeFlow.BenchOwned)continue;
                if(b.State==RabbitBenchReview.TaskState.Backstepping){
                    if(old!=b.State){entries++;Debug.Log($"BENCH135_ENTRY connected={connected} from={h.resident.transform.position} to={b.SeatPreparation}");}
                    else Check(b.TimeInState>=previousTime,"backstep restarted");
                    previousTime=b.TimeInState;time=Mathf.Max(time,b.TimeInState);
                }
                old=b.State;rest|=b.State==RabbitBenchReview.TaskState.Resting;
                if(fps==240&&!slow&&(b.State==RabbitBenchReview.TaskState.Backstepping||b.State==RabbitBenchReview.TaskState.ClearingCan)){
                    var support=(RabbitHomeFootwork)typeof(RabbitBenchReview).GetField("support",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(b);
                    Check(support.Footfalls<=3&&support.Duration<=1.201f,"too many preparation steps");
                    if(support.StepIndex==0&&support.Footfalls==3)Check(support.LandingFraction==.5f,"first foot still targets full endpoint");
                    foreach(var bone in h.resident.GetComponentsInChildren<Transform>().Where(t=>new[]{"Pelvis","Spine","Head","Foot_L","Foot_R","Shin_L","Shin_R"}.Contains(t.name)))
                        trace.Add(FormattableString.Invariant($"{b.TimeInState:F6},{b.State},{support.StepIndex},{support.LandingFraction},{bone.name},{bone.position.x:F6},{bone.position.y:F6},{bone.position.z:F6}"));
                }
                Check(b.MinSole>=-.0005f&&b.MaxGap<=.005f&&b.MaxDrift<=.0035f&&b.MaxReach<=.001f,$"contacts connected={connected} state={b.State} sole={b.MinSole} gap={b.MaxGap} drift={b.MaxDrift} reach={b.MaxReach} {b.ReachDetail}");
                if(fps==240&&i%4==0){RabbitBenchChecks.CheckConnectedPose(h);if(connected)RabbitBenchChecks.CheckStoredCanClearance(h);}
                if(rest&&b.State==RabbitBenchReview.TaskState.Standing)break;
                Check(i<fps*140-1,"flow timeout");
            }
            Check(rest&&entries==1&&time<=1.21f,$"arrival count={entries} time={time}");
            rows.Add($"connected={connected} {fps}fps {(slow?.5f:1)}x: one preparation <=3 footfalls /1.2s; drift={b.MaxDrift:F6} gap={b.MaxGap:F6} sole={b.MinSole:F6} reach={b.MaxReach:F6}");
        }
        foreach(float phase in new[]{.1f,.3f,.5f,.7f,.9f,1.1f}){
            var h=Open();h.SelectBenchMode();var b=h.benchRest;b.StartFlow();
            for(int i=0;i<60*20&&b.State!=RabbitBenchReview.TaskState.Backstepping;i++)h.Advance(1f/60);
            Check(b.State==RabbitBenchReview.TaskState.Backstepping,"preparation not reached");h.Advance(phase);
            var position=h.resident.transform.position;float time=b.TimeInState;h.paused=true;h.Advance(1);
            Check(position==h.resident.transform.position&&time==b.TimeInState,"pause changed preparation");h.paused=false;
            b.StopTask();h.Advance(2);Check(b.State==RabbitBenchReview.TaskState.Standing,"cancel did not finish safe foot pair");
            RabbitBenchChecks.CheckConnectedPose(h);b.RequestSit();b.RequestSit();h.Advance(4);
            Check(b.State==RabbitBenchReview.TaskState.Resting,"cancelled preparation could not seat again");
            Check(b.MaxDrift<=.0035f&&b.MaxGap<=.005f&&b.MinSole>=-.0005f&&b.MaxReach<=.001f,"resume contacts");
        }
        rows.Add("Six preparation phases: pause/repeated request/cancel at safe completion/re-seat PASS.");
        File.WriteAllLines("Docs/RabbitShortPreparation135Motion240.csv",trace);
        File.WriteAllLines("Docs/RabbitBenchArrival135Verification.txt",rows);
    }
    public static void Finish(){try{
        var hashes=RabbitWaterBenchLifeChecks.ArtHashes();var scene=File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity");
        Verify(true);RabbitBenchChecks.Verify();RabbitCanAvoidanceChecks.Verify();
        RabbitWaterChecks.Verify();RabbitHomeGroundChecks.FinalizeReview();
        Capture(true);RabbitHomeLifeBuilder.BuildPlayer();
        var after=RabbitWaterBenchLifeChecks.ArtHashes();Check(hashes.All(p=>after[p.Key]==p.Value)&&scene.SequenceEqual(File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity")),"art or saved scene changed");
        File.AppendAllText("Docs/RabbitBenchArrival135Verification.txt","Existing bench/water/home regression, three-view render and Windows build PASS. Art/source FBX and saved scene hashes preserved. OS input not performed.\n");
        Debug.Log("BENCH135_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
