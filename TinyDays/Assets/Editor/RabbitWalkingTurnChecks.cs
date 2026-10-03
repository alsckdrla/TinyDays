using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class RabbitWalkingTurnChecks {
    static void Check(bool value,string text){if(!value)throw new Exception("Moving turn: "+text);}
    static void Relocate(RabbitHomeLifeReview h,Vector3 origin){
        var root=h.resident.transform;var can=h.watering.can;var local=root.InverseTransformPoint(can.position);var rotation=Quaternion.Inverse(root.rotation)*can.rotation;
        root.SetPositionAndRotation(origin,Quaternion.identity);can.SetPositionAndRotation(root.TransformPoint(local),root.rotation*rotation);
    }
    public static void Draft(){try{AssetDatabase.Refresh();RabbitHomeLifeBuilder.BuildOnly();Verify();RabbitWaterBenchLifeChecks.Verify();Debug.Log("MOVING_TURN_DRAFT_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void StopDraft(){try{AssetDatabase.Refresh();RabbitHomeLifeBuilder.BuildOnly();VerifyStops();RabbitWaterBenchLifeChecks.Verify();Debug.Log("MOVING_TURN_STOP_DRAFT_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void InspectFlow(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();h.SelectLifeFlow();h.lifeFlow.StartFlow(true);var state=h.lifeFlow.State;
        for(int i=0;i<240*70;i++){
            h.Advance(1f/240);if(h.lifeFlow.State!=state){state=h.lifeFlow.State;Debug.Log("TURN_FLOW "+state+" "+h.watering.WalkingDetail);}
            if(h.watering.Active&&h.watering.MaxDrift>.0035f){
                Debug.LogError("TURN_FLOW_FAILURE "+state+" "+h.watering.WalkingDetail);
                var root=h.resident.transform.position;h.reviewCamera.transform.position=root+new Vector3(-2,1.5f,-2);h.reviewCamera.transform.LookAt(root+Vector3.up*.8f);
                RabbitWaterBenchLifeChecks.Capture(h.reviewCamera,"Docs/Captures/RabbitWalkingTurn/DraftFailure.png");return;
            }
        }
    }
    public static void Execute(){try{
        AssetDatabase.Refresh();var hashes=RabbitWaterBenchLifeChecks.ArtHashes();RabbitHomeLifeBuilder.BuildOnly();Verify();RabbitWaterBenchLifeChecks.Verify();RabbitWaterBenchLifeChecks.RepeatPositions();
        RabbitWaterLifeChecks.Verify();RabbitHomeGroundChecks.Execute();RabbitHomeLifeBuilder.Stress();RabbitWaterChecks.Verify();RabbitBenchChecks.Verify();
        Preview();RabbitWaterBenchLifeChecks.Render();RabbitHomeLifeBuilder.BuildPlayer();var after=RabbitWaterBenchLifeChecks.ArtHashes();
        Check(hashes.All(p=>after[p.Key]==p.Value),"source art modified");File.WriteAllLines("Docs/RabbitWalkingTurnPreservation.txt",hashes.Select(p=>p.Key+" SHA256 "+p.Value+" unchanged"));Debug.Log("MOVING_TURN_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Verify(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();var w=h.watering;
        var rows=new List<string>{"v0.131 natural moving turn; automatic evaluated poses, not OS input or quality approval"};
        foreach(bool carrying in new[]{false,true})foreach(float angle in new[]{30f,60f,90f,135f,180f})foreach(int side in new[]{-1,1})foreach(int fps in new[]{240,30,60,120})foreach(bool slow in new[]{false,true}){
            h.SelectWaterMode(true);var origin=new Vector3(0,h.groundHeight,-7);Relocate(h,origin);
            w.EnterCurrentFromPose(carrying);float yaw=angle*side;var target=origin+Quaternion.Euler(0,yaw,0)*Vector3.forward*2;
            h.slow=slow;w.BeginTravel(target,yaw);float maxYawSpeed=0,minMovingSpeed=100,maxDrop=0,maxBend=0,maxCorrectionStep=0,previousCorrection=float.NaN;var old=origin;var q=h.resident.transform.rotation;int n=0;
            var feet=(AdultRabbitFootTransition)typeof(RabbitWaterReview).GetField("feet",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(w);
            for(;n<fps*30&&w.State!=RabbitWaterReview.TaskState.Ready;n++){
                float oldTime=w.TimeInState;var footStateBefore=feet.State;h.Advance(1f/fps);float scale=slow?.5f:1;
                if(w.TimeInState>oldTime+.00001f)maxYawSpeed=Mathf.Max(maxYawSpeed,Mathf.Abs(Mathf.DeltaAngle(q.eulerAngles.y,h.resident.transform.eulerAngles.y))/(w.TimeInState-oldTime));
                if(w.State==RabbitWaterReview.TaskState.Walking&&footStateBefore==AdultRabbitFootTransition.Stage.Walking&&feet.State==AdultRabbitFootTransition.Stage.Walking&&feet.Speed>.1f&&w.TimeInState>oldTime+.00001f)minMovingSpeed=Mathf.Min(minMovingSpeed,Vector3.Distance(old,h.resident.transform.position)*fps/scale);
                old=h.resident.transform.position;q=h.resident.transform.rotation;
                if(fps==240&&feet.State!=AdultRabbitFootTransition.Stage.Idle){if(!float.IsNaN(previousCorrection))maxCorrectionStep=Mathf.Max(maxCorrectionStep,Mathf.Abs(feet.AdditionalDrop-previousCorrection));previousCorrection=feet.AdditionalDrop;}
                maxDrop=Mathf.Max(maxDrop,feet.AdditionalDrop);maxBend=Mathf.Max(maxBend,feet.AdditionalSupportBend);
                Check(w.MaxDrift<=.0035f&&w.MinSole>=-.0005f&&w.MaxSupportGap<=.005f,$"contacts carry={carrying} yaw={yaw} fps={fps} slow={slow} t={w.TimeInState:F3} drift={w.MaxDrift:F6} {w.WalkingDetail}");
            }
            Check(w.State==RabbitWaterReview.TaskState.Ready,"timeout "+w.WalkingDetail);
            Check(Mathf.Abs(Mathf.DeltaAngle(h.resident.transform.eulerAngles.y,yaw))<1,"arrival heading "+w.WalkingDetail);
            Check(w.MaxGripError<=.01f&&w.MaxReach<=.001f,$"grip/reach carry={carrying} yaw={yaw} fps={fps} slow={slow} grip={w.MaxGripError:F6} reach={w.MaxReach:F6} {w.WalkingDetail}");
            Check(maxYawSpeed<=181&&minMovingSpeed>.01f,$"stationary turn/angular speed yaw={yaw} fps={fps} minSpeed={minMovingSpeed:F6} yawSpeed={maxYawSpeed:F3}");
            Check(maxCorrectionStep<=.002001f,$"correction step yaw={yaw} fps={fps} slow={slow} step={maxCorrectionStep:F7}");
            Check(maxDrop<=.010001f&&maxBend<=10.001f,$"posture yaw={yaw} fps={fps} drop={maxDrop:F6} bend={maxBend:F3}");
            rows.Add($"carry={carrying} yaw={yaw} fps={fps} slow={slow} duration={n/(float)fps:F4} drift={w.MaxDrift:F6} sole={w.MinSole:F6} yawSpeed={maxYawSpeed:F3} minMovingSpeed={minMovingSpeed:F4} extraDrop={maxDrop:F6} extraBend={maxBend:F3} max240HzCorrectionStep={maxCorrectionStep:F7}");
        }
        File.WriteAllLines("Docs/RabbitWalkingTurnVerification.txt",rows);Debug.Log("MOVING_TURN_ANGLES_OK cases="+(rows.Count-1));
        VerifyStops();Debug.Log("MOVING_TURN_VERIFY_OK cases="+(rows.Count-1));
    }
    static void VerifyStops(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();var w=h.watering;
        // Stop during the actual curved portion, resume from the landed pose.
        foreach(bool carry in new[]{false,true})foreach(float time in new[]{.2f,.5f,.8f,1.2f}){
            h.SelectWaterMode(true);var origin=new Vector3(0,h.groundHeight,-7);Relocate(h,origin);w.EnterCurrentFromPose(carry);
            var target=origin+Vector3.left*2;w.BeginTravel(target,-90);h.Advance(time);w.StopTask();w.StopTask();h.Advance(3);
            Check(w.State==RabbitWaterReview.TaskState.Ready,"curve stop "+w.WalkingDetail);var p=h.resident.transform.position;h.paused=true;h.Advance(1);Check(p==h.resident.transform.position,"paused curve");h.paused=false;
            w.BeginTravel(target,-90);h.Advance(8);Check(w.State==RabbitWaterReview.TaskState.Ready&&w.MaxDrift<=.0035f,$"curve resume carry={carry} time={time} state={w.State} drift={w.MaxDrift:F6} {w.WalkingDetail}");
        }
        Debug.Log("MOVING_TURN_STOPS_OK");
    }
    public static void Preview(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();
        foreach(bool carry in new[]{false,true}){
            h.SelectWaterMode(true);var origin=new Vector3(0,h.groundHeight,-7);Relocate(h,origin);var w=h.watering;w.EnterCurrentFromPose(carry);
            // Ordinary walking leaves the prop at storage, not at the relocated test actor.
            if(!carry)w.can.SetPositionAndRotation(h.lifeFlow.storage.position,h.lifeFlow.storage.rotation);
            w.BeginTravel(origin+Vector3.left*2,-90);
            h.reviewCamera.transform.position=origin+new Vector3(-3,1.9f,3);h.reviewCamera.transform.LookAt(origin+new Vector3(-.5f,.9f,0));
            float old=0;foreach(float time in new[]{.2f,.4f,.6f,.8f,1.0f,1.4f,2f}){h.Advance(time-old);old=time;RabbitWaterBenchLifeChecks.Capture(h.reviewCamera,$"Docs/Captures/RabbitWalkingTurn/{(carry?"Carry":"Walk")}_{time:F1}.png");}
        }
        Debug.Log("MOVING_TURN_PREVIEW_OK");
    }
}
