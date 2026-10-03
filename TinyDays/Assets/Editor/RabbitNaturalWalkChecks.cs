using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class RabbitNaturalWalkChecks {
    static void Check(bool value,string message){if(!value)throw new Exception("Natural walk: "+message);}
    static AdultRabbitFootTransition Feet(object owner)=>(AdultRabbitFootTransition)owner.GetType().GetField("feet",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(owner);
    public static void Continuity131(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();
        var lines=new List<string>{"v0.131 same-controller pelvis correction at240Hz; direct checks, not OS input or quality approval."};
        foreach(bool slow in new[]{false,true}){
            h.SelectLifeFlow();h.slow=slow;h.lifeFlow.StartFlow(true);AdultRabbitFootTransition previous=null;float old=0,maximum=0,drop=0,bend=0;string detail="";
            for(int i=0;i<240*180&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
                h.Advance(1f/240);object owner=h.lifeFlow.BenchOwned?(object)h.benchRest:h.watering.Active?(object)h.watering:h;
                var f=Feet(owner);
                if(f==null||!f.FollowWalkingHeading||f.State==AdultRabbitFootTransition.Stage.Idle){previous=null;continue;}
                drop=Mathf.Max(drop,f.AdditionalDrop);bend=Mathf.Max(bend,f.AdditionalSupportBend);
                if(previous==f){float delta=Mathf.Abs(f.AdditionalDrop-old);if(delta>maximum){maximum=delta;detail=$"t={i/240f:F4} {h.lifeFlow.State}/{f.State} safety={f.SafetyDrop:F7} reach={f.ReachDrop:F7}";}}
                previous=f;old=f.AdditionalDrop;
            }
            lines.Add($"{(slow?.5f:1)}x maximum correction step {maximum:F7}m; drop {drop:F7}m; additional knee {bend:F4}deg; {detail}");
            File.WriteAllLines("Docs/RabbitNaturalContinuity131.txt",lines);
            Check(h.lifeFlow.State==RabbitWaterLifeFlow.Phase.Finished,"continuity routine stalled");
            Check(maximum<=.002001f&&drop<=.010001f&&bend<=10.001f,"continuity/posture failed: "+lines.Last());
        }
        // Solid obstacle blocks the clearing segment; do not move through it.
        h.SelectLifeFlow();var origin=h.lifeFlow.storage.position+Vector3.right*.58f;origin.y=h.groundHeight;
        h.resident.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,-90,0));h.benchRest.EnterCurrent();
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Bench path blocker";blocker.transform.SetParent(h.transform);blocker.transform.position=origin+Vector3.up*.4f;blocker.transform.localScale=new Vector3(6,1,6);
        try{h.benchRest.StartConnectedFlow(h.watering.can);for(int i=0;i<240;i++)h.benchRest.AdvanceFlow(1f/240);Check(h.benchRest.State==RabbitBenchReview.TaskState.Standing&&Vector3.Distance(origin,h.resident.transform.position)<.001f,"blocked departure moved");}finally{UnityEngine.Object.DestroyImmediate(blocker);}
        lines.Add("Blocked outward path remains standing: PASS (injected solid)");File.WriteAllLines("Docs/RabbitNaturalContinuity131.txt",lines);Debug.Log("NATURAL131_CONTINUITY_OK");
    }
    public static void BenchContacts131(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();
        h.SelectLifeFlow();h.lifeFlow.StartFlow(true);
        for(int i=0;i<240*120&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
            h.Advance(1f/240);if(!h.benchRest.Active)continue;var b=h.benchRest;
            RabbitBenchChecks.CheckStoredCanClearance(h);RabbitBenchChecks.CheckConnectedPose(h);
            Check(b.MinSole>=-.0005f&&b.MaxGap<=.005f&&b.MaxDrift<=.0035f&&b.MaxReach<=.001f,$"bench contacts {b.State} t={b.TimeInState:F4} sole={b.MinSole} gap={b.MaxGap} drift={b.MaxDrift} reach={b.MaxReach} {b.ReachDetail}");
        }
        Check(h.lifeFlow.State==RabbitWaterLifeFlow.Phase.Finished,"bench contact timeout");Debug.Log("NATURAL131_BENCH_CONTACTS_OK");
    }
    public static void BenchDepartures131(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();var lines=new List<string>();
        foreach(int side in Enumerable.Range(0,8))foreach(float yaw in new[]{0f,90f,180f,270f}){
            h.SelectLifeFlow();var can=h.watering.can;can.SetPositionAndRotation(h.lifeFlow.storage.position,Quaternion.Euler(0,yaw,0));
            var origin=can.position+Quaternion.Euler(0,side*45,0)*Vector3.back*.58f;origin.y=h.groundHeight;
            h.resident.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,side*45,0));h.idleClip.SampleAnimation(h.resident,0);new RabbitHomeFootwork(h.resident,h.groundHeight){FitStandingReach=true}.Hold();
            var b=h.benchRest;b.EnterCurrent();b.StartConnectedFlow(can);bool held=b.State==RabbitBenchReview.TaskState.Standing;
            for(int i=0;i<240*90;i++){
                b.AdvanceFlow(1f/240);
                Check(b.MinSole>=-.0005f&&b.MaxGap<=.005f&&b.MaxDrift<=.0035f&&b.MaxReach<=.001f,$"departure contacts {side}/{yaw} {b.State} gap={b.MaxGap} drift={b.MaxDrift} reach={b.MaxReach} {b.ReachDetail}");
                if(i%8==0){try{RabbitBenchChecks.CheckStoredCanClearance(h);RabbitBenchChecks.CheckConnectedPose(h);}catch(Exception){Debug.Log($"DEPARTURE_GEOMETRY side={side*45} canYaw={yaw} state={b.State} t={b.TimeInState} root={h.resident.transform.position:F4} approach={b.approach:F4} bench={b.BenchBounds}");throw;}}
                if(b.State==RabbitBenchReview.TaskState.Standing&&i>240){if(held)Check(Vector3.Distance(origin,h.resident.transform.position)<.001f,"blocked angular path moved");break;}
                Check(i<240*90-1,"angular departure timeout");
            }
            lines.Add($"approach={side*45} canYaw={yaw}: {(held?"blocked, safely held":"completed")}; sole={b.MinSole:F6} gap={b.MaxGap:F6} drift={b.MaxDrift:F6} reach={b.MaxReach:F6}");
            File.WriteAllLines("Docs/RabbitBenchDeparture131.txt",lines);
        }
        Debug.Log("NATURAL131_BENCH_DEPARTURES_OK cases="+lines.Count);
    }
    public static void Final131(){try{
        Continuity131();Pickup();RabbitWalkingTurnChecks.Verify();BenchDepartures131();
        RabbitWaterBenchLifeChecks.Verify();RabbitWaterBenchLifeChecks.RepeatPositions();RabbitWaterLifeChecks.Verify();
        RabbitHomeLifeBuilder.AutomaticClosingVerify();RabbitHomeLifeBuilder.Stress();RabbitWaterChecks.Verify();RabbitBenchChecks.Verify();RabbitHomeGroundChecks.Execute();
        Debug.Log("NATURAL131_FINAL_REGRESSION_OK");
    }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void Remaining131(){try{
        RabbitWaterLifeChecks.Verify();RabbitHomeLifeBuilder.AutomaticClosingVerify();RabbitHomeLifeBuilder.Stress();
        RabbitWaterChecks.Verify();RabbitBenchChecks.Verify();RabbitHomeGroundChecks.Execute();Debug.Log("NATURAL131_REMAINING_REGRESSION_OK");
    }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void BenchFinal131(){try{RabbitBenchChecks.Verify();RabbitHomeGroundChecks.Execute();Debug.Log("NATURAL131_BENCH_FINAL_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void ReviewAndBuild131(){try{Continuity131();After131();RabbitHomeLifeBuilder.Open();RabbitHomeLifeBuilder.BuildPlayer();Debug.Log("NATURAL131_REVIEW_BUILD_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void Before131(){Render131("Before");Diagnose();}
    public static void After131(){Render131("After");}
    static void Render131(string label){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();
        foreach(float angle in new[]{0f,45f,90f}){
            h.SelectLifeFlow();h.ViewLifeFlow(angle);h.lifeFlow.StartFlow(true);int frame=0;
            do{RabbitWaterBenchLifeChecks.Capture(h.reviewCamera,$"Logs/Natural131/{label}/{angle}/{frame:D4}.png");h.Advance(1f/15);frame++;}while(frame<15*90&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished);
            Check(h.lifeFlow.State==RabbitWaterLifeFlow.Phase.Finished,"comparison render timeout");
        }
        Debug.Log("NATURAL131_RENDER_"+label+"_OK");
    }
    public static void Draft(){try{Diagnose();Pickup();RabbitWalkingTurnChecks.Verify();RabbitWaterBenchLifeChecks.Verify();Debug.Log("NATURAL_WALK_DRAFT_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Pickup(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();var rows=new List<string>();
        foreach(int side in Enumerable.Range(0,8))foreach(float initial in new[]{0f,90f,180f,270f}){
            h.SelectWaterMode(true);var w=h.watering;var root=h.resident.transform;var stored=new Vector3(0,h.groundHeight+.0025f,-6);var rotation=Quaternion.Euler(0,initial,0);
            root.SetPositionAndRotation(stored+Quaternion.Euler(0,side*45,0)*Vector3.back*.58f,Quaternion.Euler(0,side*45,0));root.position=new Vector3(root.position.x,h.groundHeight,root.position.z);
            w.EnterCurrent(false);w.can.SetPositionAndRotation(stored,rotation);w.BeginPickup(stored,rotation,.22f);
            for(int i=0;i<432;i++){h.Advance(1f/240);Check(w.MaxGripError<=.01f&&w.MaxReach<=.001f,$"pickup {side}/{initial} t={w.TimeInState} grip={w.MaxGripError} reach={w.MaxReach}");if(i<180)Check(Vector3.Distance(w.can.position,stored)<.00001f,"can moved before lift");}
            Check(w.Carrying,"pickup did not finish");var heldRotation=w.can.rotation;w.BeginPutdown(stored,rotation,.22f);
            h.Advance(1.05f);Check(Vector3.Distance(w.can.position,stored)<.001f&&Quaternion.Angle(w.can.rotation,heldRotation)<.1f,"putdown must land before rotating");
            h.Advance(.30f);Check(Quaternion.Angle(w.can.rotation,heldRotation)<.1f,"can rotated before hands released");
            h.Advance(.1f);
            var hands=h.resident.GetComponentsInChildren<Transform>().Where(t=>t.name=="Hand_L"||t.name=="Hand_R").ToArray();var points=hands.Select(t=>t.position).ToArray();
            var field=typeof(RabbitWaterReview).GetField("storedRotation",BindingFlags.Instance|BindingFlags.NonPublic);field.SetValue(w,rotation*Quaternion.Euler(0,180,0));
            typeof(RabbitWaterReview).GetMethod("TickWork",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            Check(hands.Select((t,j)=>Vector3.Distance(t.position,points[j])).Max()<.00001f,"released hands followed can rotation");field.SetValue(w,rotation);h.Advance(.36f);
            Check(!w.Carrying&&Vector3.Distance(w.can.position,stored)<.001f&&Quaternion.Angle(w.can.rotation,rotation)<.1f,"putdown original pose");
            Check(w.MaxGripError<=.01f&&w.MaxReach<=.001f&&w.MaxDrift<=.0035f&&w.MinSole>=-.0005f,$"putdown {side}/{initial} grip={w.MaxGripError} reach={w.MaxReach} drift={w.MaxDrift}");rows.Add($"side={side*45} canYaw={initial} grip={w.MaxGripError:F6} drift={w.MaxDrift:F6}");
        }
        File.WriteAllLines("Docs/RabbitNaturalPickupVerification.txt",rows);Debug.Log("NATURAL_PICKUP_OK cases="+rows.Count);
    }
    public static void Diagnose(){try{
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();
        var bones=h.resident.GetComponentsInChildren<Transform>();
        foreach(float t in new[]{0,.1f,.2f,.3f,.4f,.5f,.6f,.7f}){h.walkClip.SampleAnimation(h.resident,t);var a=bones.First(b=>b.name=="Thigh_L");var b=bones.First(x=>x.name=="Shin_L");var c=bones.First(x=>x.name=="Foot_L");Debug.Log($"SOURCE t={t} hip={h.resident.transform.InverseTransformPoint(a.position):F4} foot={h.resident.transform.InverseTransformPoint(c.position):F4} lengths={Vector3.Distance(a.position,b.position):F4}/{Vector3.Distance(b.position,c.position):F4}");}
        h.SelectLifeFlow();h.lifeFlow.StartFlow(true);
        var rows=new List<string>{"time,phase,footState,phaseTime,drop,bend,reach,safety,sourceY,detail"};float drop=0,bend=0;
        for(int i=0;i<240*90&&h.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
            h.Advance(1f/240);object owner=h.lifeFlow.BenchOwned?(object)h.benchRest:h.watering.Active?(object)h.watering:h;
            var f=Feet(owner);if(f==null||!f.FollowWalkingHeading||f.State==AdultRabbitFootTransition.Stage.Idle)continue;
            drop=Mathf.Max(drop,f.AdditionalDrop);bend=Mathf.Max(bend,f.AdditionalSupportBend);
            rows.Add($"{i/240f:F4},{h.lifeFlow.State},{f.State},{f.WalkTime:F4},{f.AdditionalDrop:F6},{f.AdditionalSupportBend:F3},{f.ReachDrop:F6},{f.SafetyDrop:F6},{f.SourcePelvisHeight:F6},\"{f.ContactDetail}\"");
        }
        File.WriteAllLines("Logs/natural-walk-diagnostic.csv",rows);Debug.Log($"NATURAL_WALK_DIAGNOSTIC state={h.lifeFlow.State} drop={drop:F6} bend={bend:F3}");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
