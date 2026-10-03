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
    public static void Draft(){try{Diagnose();Pickup();RabbitWalkingTurnChecks.Verify();RabbitWaterBenchLifeChecks.Verify();Debug.Log("NATURAL_WALK_DRAFT_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Pickup(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();var rows=new List<string>();
        foreach(int side in Enumerable.Range(0,8))foreach(float initial in new[]{0f,90f,180f,270f}){
            h.SelectWaterMode(true);var w=h.watering;var root=h.resident.transform;var stored=new Vector3(0,h.groundHeight+.0025f,-6);var rotation=Quaternion.Euler(0,initial,0);
            root.SetPositionAndRotation(stored+Quaternion.Euler(0,side*45,0)*Vector3.back*.58f,Quaternion.Euler(0,side*45,0));root.position=new Vector3(root.position.x,h.groundHeight,root.position.z);
            w.EnterCurrent(false);w.can.SetPositionAndRotation(stored,rotation);w.BeginPickup(stored,rotation,.22f);
            for(int i=0;i<432;i++){h.Advance(1f/240);Check(w.MaxGripError<=.01f&&w.MaxReach<=.001f,$"pickup {side}/{initial} t={w.TimeInState} grip={w.MaxGripError} reach={w.MaxReach}");if(i<180)Check(Vector3.Distance(w.can.position,stored)<.00001f,"can moved before lift");}
            Check(w.Carrying,"pickup did not finish");w.BeginPutdown(stored,rotation,.22f);h.Advance(1.81f);
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
