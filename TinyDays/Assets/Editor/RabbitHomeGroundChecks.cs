using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TinyDays.Review;
using UnityEditor.SceneManagement;
using UnityEngine;

// Read-only scene evaluation: never regenerates the original motion assets.
public static class RabbitHomeGroundChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Execute()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");
        var r=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();r.ResetStudy();
        var bones=r.resident.GetComponentsInChildren<Transform>();
        var watched=new[]{"Pelvis","Head","Hand_L","Hand_R","Foot_L","Foot_R"}.Select(n=>bones.First(b=>b.name==n&&!b.GetComponent<Renderer>())).ToArray();
        var rows=new List<string>{"time,stage,pelvisY,headY,leftHandSpeed,rightHandSpeed,leftFootSpeed,rightFootSpeed,pelvisVerticalSpeed,headVerticalSpeed"};
        var previous=watched.Select(b=>b.position).ToArray();float time=0,maxBoundary=0;var stage=r.Current;
        foreach(bool exit in new[]{true,false}){
            if(exit)r.RequestExit();else r.RequestEnter();
            for(int i=0;i<240*120&&!(exit?r.Outside:r.Inside);i++){
                r.Advance(1f/240);time+=1f/240;var now=watched.Select(b=>b.position).ToArray();
                if(stage!=r.Current)maxBoundary=Mathf.Max(maxBoundary,Mathf.Abs(now[0].y-previous[0].y),Mathf.Abs(now[1].y-previous[1].y));
                rows.Add(FormattableString.Invariant($"{time:F6},{r.Current},{now[0].y:F6},{now[1].y:F6},{Vector3.Distance(now[2],previous[2])*240:F6},{Vector3.Distance(now[3],previous[3])*240:F6},{Vector3.Distance(now[4],previous[4])*240:F6},{Vector3.Distance(now[5],previous[5])*240:F6},{(now[0].y-previous[0].y)*240:F6},{(now[1].y-previous[1].y)*240:F6}"));
                previous=now;stage=r.Current;
            }
            Require(exit?r.Outside:r.Inside,"240Hz trip did not complete");
        }
        File.WriteAllLines("Docs/RabbitHomeMotion240.csv",rows);
        var log=new List<string>{"v0.131 automatic door closing direct-call 240Hz / actual floor .275m; not OS input or aesthetic approval.",
            $"2 trips in {time:F3}s; minimum sole above actual floor {r.MinimumSole:F6}m; maximum support gap {r.MaximumSupportGap:F6}m; maximum planted drift {r.MaximumSupportDrift:F6}m; step reach error {r.MaxStepReach:F6}m",
            $"Unsafe door samples {r.UnsafeDoorSamples}; maximum turn footfalls {r.MaxTurnSteps}; reverse travel {r.ReverseTravel:F6}m; lateral deviation {r.MaximumLateralDeviation:F6}m; manual hand contact removed.",
            $"Maximum pelvis/head height delta at a stage boundary {maxBoundary:F6}m per 1/240s; full limb speed series in RabbitHomeMotion240.csv"};
        File.WriteAllLines("Docs/RabbitHomeGroundVerification.txt",log);
        Require(r.MinimumSole>=-.0005f&&r.MaximumSupportDrift<=.0035f&&r.MaxStepReach<=.0035f,"home foot criteria");
        Require(r.MaximumSupportGap<=.005f,"home support clearance");
        Require(r.UnsafeDoorSamples==0&&r.MaxTurnSteps<=3&&r.ReverseTravel<.001f,"automatic door/route");
        r.ResetStudy();
        EditorSceneManager.OpenScene("Assets/Scenes/AdultRabbitMotionStudy.unity");
        var motion=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        float drift=0,minSole=100,gap=0;int runs=0;
        foreach(bool run in new[]{false,true})foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(int phase in Enumerable.Range(0,8)){
            motion.BeginMovement();motion.slow=slow;if(run)motion.RequestRun(true);else motion.RequestWalk(true);
            var feet=motion.resident.GetComponentsInChildren<Transform>().Where(b=>b.name=="Foot_L"||b.name=="Foot_R").OrderBy(b=>b.name).ToArray();
            float elapsed=0,scale=slow?.5f:1,stopAt=1.2f+phase*(run?AdultRunTiming.Data.duration:.8f)/8;bool stopped=false;
            while(elapsed<(stopAt+2.5f)/scale){
                if(!stopped&&elapsed*scale>=stopAt){motion.RequestWalk(false);stopped=true;}
                var old=Enumerable.Range(0,2).Select(j=>motion.FootTransition.ContactPosition(j)).ToArray();var ids=Enumerable.Range(0,2).Select(j=>motion.FootTransition.ContactIndex(j)).ToArray();var planted=new[]{motion.FootTransition.LeftPlanted,motion.FootTransition.RightPlanted};
                motion.Advance(1f/fps);elapsed+=1f/fps;var f=motion.FootTransition;var after=new[]{f.LeftPlanted,f.RightPlanted};
                // A running shoe rolls around its toe; ankle travel is not slip.
                for(int j=0;j<2;j++)if(planted[j]&&after[j]&&ids[j]==f.ContactIndex(j))drift=Mathf.Max(drift,Vector3.Distance(old[j],f.ContactPosition(j)));
                minSole=Mathf.Min(minSole,f.LeftSoleHeight,f.RightSoleHeight);
            }
            Require(motion.FootTransition.State==AdultRabbitFootTransition.Stage.Idle&&motion.FootTransition.BothFeetGrounded,"original movement did not settle");
            gap=Mathf.Max(gap,motion.FootTransition.FootForwardGap);runs++;
        }
        motion.Select(0);
        log.Add($"Original floor=0 walking/running: {runs} stop combinations; drift {drift:F6}m; minimum sole {minSole:F6}m; final foot forward gap {gap:F6}m.");
        File.WriteAllLines("Docs/RabbitHomeGroundVerification.txt",log);
        Require(drift<=.0035f&&minSole>=-.0005f&&gap<=.01f,"original floor=0 regression");
        Debug.Log("RABBIT_HOME_GROUND_REGRESSION_OK");
    }
    public static void FinalizeReview(){Execute();RabbitHomeLifeBuilder.Stress();RabbitHomeLifeBuilder.BuildPlayer();}
    public static void RenderReview(){RabbitHomeLifeBuilder.Execute();RabbitHomeLifeBuilder.CaptureSequence();RabbitHomeLifeBuilder.BuildPlayer();}
    public static void InspectSampling(){
        EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");
        var r=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();r.ResetStudy();
        var bones=r.resident.GetComponentsInChildren<Transform>();var report=new List<string>();
        var sample=typeof(RabbitHomeLifeReview).GetMethod("SampleBase",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var poses=new List<Quaternion[]>();
        for(int n=0;n<8;n++){sample.Invoke(r,new object[]{1f,n*.1f});poses.Add(bones.Select(b=>b.localRotation).ToArray());}
        for(int n=0;n<8;n++){
            float t=n*.1f;
            var graphPose=poses[n];r.walkClip.SampleAnimation(r.resident,t);
            report.Add($"{t:F3} max mixer/sample rotation difference {bones.Select((b,i)=>Quaternion.Angle(graphPose[i],b.localRotation)).Max():F4} degrees");
            foreach(string bone in new[]{"UpperArm_L","Forearm_L","UpperArm_R","Forearm_R"}){int i=Array.FindIndex(bones,b=>b.name==bone);report.Add($"{bone}: mixer {graphPose[i].eulerAngles}, source {bones[i].localEulerAngles}");}
        }
        File.WriteAllLines("Logs/home117-sampling.txt",report);r.ResetStudy();Debug.Log("HOME_SAMPLING_INSPECTED");
    }
}
