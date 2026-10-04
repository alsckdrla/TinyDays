using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class RabbitBenchArrivalChecks {
    static void Check(bool value,string message){if(!value)throw new Exception("Bench arrival134: "+message);}
    static RabbitHomeLifeReview Open(){EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");return UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();}
    public static void Before(){try{Capture(false);Debug.Log("BENCH134_BEFORE_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Capture(bool after){
        foreach(float view in after?new[]{0f,45f,90f}:new[]{45f}){
            var h=Open();h.SelectLifeFlow();h.lifeFlow.StartFlow(true);bool started=false;int frame=0;
            for(int i=0;i<60*100;i++){
                h.Advance(1f/60);var b=h.benchRest;
                if(h.lifeFlow.BenchOwned&&b.State==RabbitBenchReview.TaskState.Turning)started=true;
                if(!started)continue;
                if(i%4==0){h.ViewLifeFlow(view);RabbitWaterBenchLifeChecks.Capture(h.reviewCamera,$"Logs/BenchArrival134/{(after?"After":"Before")}{view}/{frame++:D4}.png");}
                if(frame>=120)break;
            }
        }
    }
    public static void Draft(){try{Verify(false);Debug.Log("BENCH134_DRAFT_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Verify(bool matrix){
        var rows=new List<string>{"v0.134 bench arrival; automatic callbacks, not OS input or quality approval."};
        foreach(bool connected in new[]{false,true})foreach(int fps in matrix?new[]{240,30,60,120}:new[]{240})foreach(bool slow in matrix?new[]{false,true}:new[]{false}){
            var h=Open();if(connected){h.SelectLifeFlow();h.lifeFlow.StartFlow(true);}else{h.SelectBenchMode();h.benchRest.StartFlow();}h.slow=slow;
            var b=h.benchRest;int entries=0;float time=0,previousTime=-1;bool rest=false;var old=b.State;
            for(int i=0;i<fps*140;i++){
                h.Advance(1f/fps);if(connected&&!h.lifeFlow.BenchOwned)continue;
                if(b.State==RabbitBenchReview.TaskState.Backstepping){
                    if(old!=b.State){entries++;Debug.Log($"BENCH134_ENTRY connected={connected} from={h.resident.transform.position} to={b.SeatPreparation}");}
                    else Check(b.TimeInState>=previousTime,"backstep restarted");
                    previousTime=b.TimeInState;time=Mathf.Max(time,b.TimeInState);
                }
                old=b.State;rest|=b.State==RabbitBenchReview.TaskState.Resting;
                Check(b.MinSole>=-.0005f&&b.MaxGap<=.005f&&b.MaxDrift<=.0035f&&b.MaxReach<=.001f,$"contacts connected={connected} state={b.State} sole={b.MinSole} gap={b.MaxGap} drift={b.MaxDrift} reach={b.MaxReach} {b.ReachDetail}");
                if(fps==240&&i%4==0){RabbitBenchChecks.CheckConnectedPose(h);if(connected)RabbitBenchChecks.CheckStoredCanClearance(h);}
                if(rest&&b.State==RabbitBenchReview.TaskState.Standing)break;
                Check(i<fps*140-1,"flow timeout");
            }
            Check(rest&&entries==1&&time<=1.21f,$"arrival count={entries} time={time}");
            rows.Add($"connected={connected} {fps}fps {(slow?.5f:1)}x: current v0.135 preparation <=3 footfalls/1.2s; drift={b.MaxDrift:F6} gap={b.MaxGap:F6} sole={b.MinSole:F6} reach={b.MaxReach:F6}");
        }
        foreach(float phase in new[]{.1f,.3f,.5f,.7f}){
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
        rows.Add("Four preparation phases: pause/repeated request/cancel at safe pair completion/re-seat PASS.");
        File.WriteAllLines("Docs/RabbitBenchArrival134Verification.txt",rows);
    }
    public static void Finish(){try{
        var hashes=RabbitWaterBenchLifeChecks.ArtHashes();var scene=File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity");
        Verify(true);RabbitBenchChecks.Verify();RabbitCanAvoidanceChecks.Draft();
        RabbitWaterChecks.Verify();RabbitHomeGroundChecks.FinalizeReview();
        Capture(true);RabbitHomeLifeBuilder.BuildPlayer();
        var after=RabbitWaterBenchLifeChecks.ArtHashes();Check(hashes.All(p=>after[p.Key]==p.Value)&&scene.SequenceEqual(File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity")),"art or saved scene changed");
        File.AppendAllText("Docs/RabbitBenchArrival134Verification.txt","Existing bench/water/home regression, three-view render and Windows build PASS. Art/source FBX and saved scene hashes preserved. OS input not performed.\n");
        Debug.Log("BENCH134_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
