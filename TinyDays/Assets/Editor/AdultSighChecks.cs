using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using TinyDays.Review;

public static class AdultSighChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Execute(){try{Run();Debug.Log("ADULT_SIGH_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        var report=new List<string>{"v0.99 one-second inhale/exhale: automatic function/clip tests; not actual player input or user quality approval."};
        var bones=r.resident.GetComponentsInChildren<Transform>();var spine=bones.First(b=>b.name=="Spine");var head=bones.First(b=>b.name=="Head");
        var supports=new[]{"Pelvis","Foot_L","Foot_R"}.Select(n=>bones.First(b=>b.name==n)).ToArray();
        var csv=new List<string>{"posture,time,chest_y,head_y,chest_velocity,head_velocity,head_forward,head_pitch"};
        foreach(bool seated in new[]{false,true}){
            int clip=seated?13:12;Check(r.clips.Length>=14&&Math.Abs(r.clips[clip].length-5)<.001&&!r.clips[clip].isLooping,"Sigh duration/count/loop");
            r.Select(clip);r.Sample(0);var rest=supports.Select(b=>b.position).ToArray();var scales=bones.Select(b=>b.localScale).ToArray();float neck=Vector3.Distance(head.position,head.parent.position),drift=0,neckDelta=0,low=999,high=-999;
            var start=bones.Select(b=>b.localPosition).ToArray();var startRot=bones.Select(b=>b.localRotation).ToArray();float priorChest=spine.position.y,priorHead=head.position.y,baseChest=spine.position.y;var originChest=spine.position;float maxForward=0;
            var baseHeadRotation=head.rotation;var baseHead=head.position;float maxPitch=-999,pitchTime=0,minChestTime=0;Vector3 crestHead=Vector3.zero,exhaleHead=Vector3.zero;
            var shoes=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Shoes");float minSole=999;
            var shoePoints=AdultRabbitSitCoat.World(shoes);var weights=shoes.sharedMesh.boneWeights;
            foreach(var foot in supports.Skip(1)){int index=Array.IndexOf(shoes.bones,foot);float sole=Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==index&&weights[i].weight0>.99f).Min(i=>shoePoints[i].y);Check(sole>=-.0005&&sole<=.005,"Individual sole gap");}
            for(int i=0;i<=1200;i++){
                r.Sample(i/240.0);for(int j=0;j<supports.Length;j++)drift=Mathf.Max(drift,Vector3.Distance(rest[j],supports[j].position));
                neckDelta=Mathf.Max(neckDelta,Mathf.Abs(neck-Vector3.Distance(head.position,head.parent.position)));if(spine.position.y<low){low=spine.position.y;minChestTime=i/240f;}high=Mathf.Max(high,spine.position.y);
                float pitch=Vector3.SignedAngle(r.resident.transform.forward,(head.rotation*Quaternion.Inverse(baseHeadRotation))*r.resident.transform.forward,r.resident.transform.right);
                if(pitch>maxPitch){maxPitch=pitch;pitchTime=i/240f;}if(i==240){crestHead=head.position;Check(Mathf.Abs(spine.position.y-baseChest-.024f)<.001,"Inhale crest timing");}if(i==496)exhaleHead=head.position;
                maxForward=Mathf.Max(maxForward,Vector3.Dot(spine.position-originChest,r.resident.transform.forward));
                Check(bones.Select((b,j)=>Vector3.Distance(b.localScale,scales[j])).Max()<1e-5,"Scale animation");
                if(i>240&&i<480)Check(head.position.y<=priorHead+.000002f,"Unexpected upward rebound during exhale");
                minSole=Mathf.Min(minSole,AdultRabbitSitCoat.World(shoes).Min(p=>p.y));
                csv.Add(FormattableString.Invariant($"{seated},{i/240.0:F6},{spine.position.y:F7},{head.position.y:F7},{(spine.position.y-priorChest)*240:F6},{(head.position.y-priorHead)*240:F6},{Vector3.Dot(head.position-baseHead,r.resident.transform.forward):F7},{pitch:F5}"));priorChest=spine.position.y;priorHead=head.position.y;
            }
            Check(drift<=.0035&&neckDelta<.0001&&minSole>=-.0005,$"Supports/neck/sole {drift} {neckDelta} {minSole}");
            Check(Mathf.Abs(high-baseChest-.024f)<.001&&Mathf.Abs(low-baseChest+.020f)<.001&&Mathf.Abs(maxForward-.020f)<.001,$"Chest targets {high-baseChest} {low-baseChest} {maxForward}");
            Check(Mathf.Abs(minChestTime-2f)<.04&&Mathf.Abs(maxPitch-(seated?8:6))<.2&&Mathf.Abs(pitchTime-(2f+2f/30))<.04,$"Exhale/head timing {minChestTime} {maxPitch} {pitchTime}");
            Check(exhaleHead.y<crestHead.y&&Vector3.Dot(exhaleHead-crestHead,r.resident.transform.forward)>.01,"Head did not travel down and forward");
            report.Add($"{clip}: exhale trough {minChestTime:F4}s; head pitch {maxPitch:F3}deg at {pitchTime:F4}s; head descent {(crestHead.y-exhaleHead.y)*1000:F3}mm, advance {Vector3.Dot(exhaleHead-crestHead,r.resident.transform.forward)*1000:F3}mm from crest.");
            report.Add($"{clip}: lift {(high-baseChest)*1000:F3}mm; lower {(low-baseChest)*1000:F3}mm; forward {maxForward*1000:F3}mm.");
            Check(bones.Select((b,j)=>Vector3.Distance(b.localPosition,start[j])).Max()<.0001&&bones.Select((b,j)=>Quaternion.Angle(b.localRotation,startRot[j])).Max()<.1,"Endpoints differ");
            report.Add($"{clip}: 240Hz range {(high-low)*1000:F3}mm; supports {drift*1000:F4}mm; neck {neckDelta*1000:F4}mm; min sole {minSole*1000:F4}mm; endpoints PASS.");
            for(int phase=0;phase<8;phase++){
                r.StartBreathing(seated);r.slow=false;r.Advance(phase*.5f);var before=head.position;Check(r.PlaySigh(),"Play refused");Check(Vector3.Distance(before,head.position)<.0001,"Entry snap");Check(!r.PlaySigh(),"Repeated start accepted");
                r.Advance(.25f);Check(r.Idle.State==CommonIdleDirector.Stage.Variation,"Entry duration");r.Advance(r.clips[clip].length);Check(r.Idle.State==CommonIdleDirector.Stage.Leaving,"Clip duration");r.Advance(.25f);Check(r.Idle.State==CommonIdleDirector.Stage.Breathing&&r.Idle.Wait>=6&&r.Idle.Wait<=12,"Exit / scheduling");
            }
        }
        int count=0;
        float maxBridgeSpeedStep=0,maxBridgeSupport=0;
        foreach(bool seated in new[]{false,true})foreach(int phase in Enumerable.Range(0,8)){
            r.StartBreathing(seated);r.slow=false;r.Advance(phase*.5f);r.PlaySigh();var planted=supports.Select(b=>b.position).ToArray();var p=head.position;Vector3 previousVelocity=Vector3.zero;
            for(int frame=1;frame<=1320;frame++){
                r.Advance(1f/240);var velocity=(head.position-p)*240;p=head.position;
                if(frame>1)maxBridgeSpeedStep=Mathf.Max(maxBridgeSpeedStep,(velocity-previousVelocity).magnitude);previousVelocity=velocity;
                maxBridgeSupport=Mathf.Max(maxBridgeSupport,supports.Select((b,j)=>Vector3.Distance(b.position,planted[j])).Max());
            }
        }
        Check(maxBridgeSupport<=.0035&&maxBridgeSpeedStep<.08f,$"Blend continuity {maxBridgeSupport} {maxBridgeSpeedStep}");
        report.Add($"240Hz complete entry/clip/exit: support {maxBridgeSupport*1000:F4}mm; maximum adjacent head velocity difference {maxBridgeSpeedStep:F6}m/s (diagnostic bound .08). This is not a visual smoothness approval.");
        foreach(bool style in new[]{false,true})foreach(bool seated in new[]{false,true})foreach(int phase in Enumerable.Range(0,8))foreach(bool run in new[]{false,true})foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true}){
            r.Select(0);r.SetSitStyle(style);r.StartBreathing(seated);r.slow=slow;r.PlaySigh();r.Advance((.25f+phase*5f/8)/(slow?.5f:1));
            r.RequestRun(!run);r.RequestRun(run);var headBefore=head.position;r.Advance(0);Check(Vector3.Distance(headBefore,head.position)<.0001,"Interruption snap");
            var clock=r.Idle.Time;r.paused=true;r.Advance(1);Check(r.Idle.Time==clock,"Pause advanced");r.paused=false;
            for(int n=0;n<fps*6&&!r.MovingReview;n++)r.Advance(1f/fps);
            Check(r.MovingReview&&r.FootTransition.WantsRun==run,"Departure request lost");count++;
        }
        report.Add($"{count} departure combinations: both rise styles/postures, 8 phases, walk/run, 30/60/120fps, .5/1x, pause and latest request PASS.");
        foreach(bool seated in new[]{false,true}){r.StartBreathing(seated);r.slow=false;r.PlaySigh();r.Advance(2);r.RequestRun(true);r.Advance(.1f);r.RequestWalk(false);r.Advance(3);Check(!r.MovingReview&&r.Idle.Pending==CommonIdleDirector.Departure.None,"Cancel failed");}
        r.AutomaticIdle=true;r.StartBreathing(false);r.Advance(12);Check(r.Idle.IsVariation,"Automatic variation absent");r.Select(0);Check(r.Idle==null,"Reset failed");
        report.Add("Cancellation, automatic selection and reset PASS. Fine coat/leg collision intentionally excluded.");
        File.WriteAllLines("Docs/AdultSighVerification.txt",report);File.WriteAllLines("Docs/AdultSighMotion.csv",csv);
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        var capture=typeof(AdultRabbitMotionBuilder).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Camera),typeof(SkinnedMeshRenderer[]),typeof(string),typeof(string)},null);
        Directory.CreateDirectory("Logs/SighSequence");Directory.CreateDirectory("Docs/Captures/Sigh");
        foreach(bool seated in new[]{false,true})foreach(int view in new[]{0,45,90}){
            r.StartBreathing(seated);r.slow=false;
            for(int frame=0;frame<210;frame++){
                if(frame>0)r.Advance(1f/30);if(frame==23)r.PlaySigh();r.View(view);
                string name=(seated?"Sit":"Stand")+view+"_"+frame.ToString("D3");capture.Invoke(null,new object[]{r.reviewCamera,skins,name,"Logs/SighSequence"});
                if(frame==75||frame==135)capture.Invoke(null,new object[]{r.reviewCamera,skins,name,"Docs/Captures/Sigh"});
            }
        }
        foreach(bool seated in new[]{false,true})foreach(int view in new[]{0,45,90})for(int pose=0;pose<8;pose++){
            r.Select(seated?13:12);r.Pose(pose);r.View(view);capture.Invoke(null,new object[]{r.reviewCamera,skins,(seated?"Sit":"Stand")+view+"_Pose"+pose,"Docs/Captures/Sigh"});
        }
        r.Select(0);Debug.Log("ADULT_SIGH_SEQUENCE_OK");
    }
}
