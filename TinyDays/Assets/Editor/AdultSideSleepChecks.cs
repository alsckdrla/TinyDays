using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;
public static class AdultSideSleepChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Execute(){try{Run();Debug.Log("SIDE_SLEEP_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void FinalizeReview(){try{Run();Continuous();AdultSideChecks.Execute();AdultSideChecks.Pillow();AdultSideChecks.Continuity();AdultSideChecks.Sequence();Sequence();AdultRabbitMotionBuilder.BuildPlayer();Debug.Log("SIDE_SLEEP_FINAL_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Continuous(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();var bones=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Head").bones;var joints=new[]{"Spine","Head","Hand_R"}.Select(n=>bones.Single(b=>b.name==n)).ToArray();
        float maxStep=0,maxAcceleration=0;var rows=new System.Collections.Generic.List<string>{"initial_phase,time,clip,breath_phase,chest_y,head_y"};
        for(int phase=0;phase<8;phase++){
            r.Select(28);r.slow=false;r.paused=false;r.Advance(phase*.5f);var last=joints.Select(b=>b.position).ToArray();var velocity=new Vector3[joints.Length];
            for(int f=0;f<=3360;f++){
                if(f==240)r.RequestSleep();if(f==2400)r.RequestWake();double clock=r.RestBreathPhase;r.Advance(1f/240);Check(r.RestBreathPhase>clock,"Continuous side sleep phase");
                for(int i=0;i<joints.Length;i++){var v=(joints[i].position-last[i])*240;maxStep=Mathf.Max(maxStep,Vector3.Distance(joints[i].position,last[i]));if(f>1)maxAcceleration=Mathf.Max(maxAcceleration,(v-velocity[i]).magnitude*240);last[i]=joints[i].position;velocity[i]=v;}
                rows.Add(FormattableString.Invariant($"{phase},{f/240f:F6},{r.selected},{r.RestBreathPhase:F8},{joints[0].position.y:F7},{joints[1].position.y:F7}"));
            }
            Check(r.selected==28&&r.SleepEyeWeight==0,"Awake on same side");
        }
        File.WriteAllLines("Docs/AdultSideSleepContinuity.csv",rows);File.WriteAllText("Docs/AdultSideSleepContinuity.txt",$"240Hz eight initial phases: max joint step {maxStep*1000:F4}mm; finite-difference acceleration {maxAcceleration:F4}m/s2. Breath phase strictly advances. Automated curve check, not visual quality approval.\n");
        Check(maxStep<.0035f&&maxAcceleration<10,"Side sleep discontinuity");
    }
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        Check(r.clips.Length>=32,"32 preserved slots");var bones=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Head").bones;var pelvis=bones.Single(b=>b.name=="Pelvis");var tail=bones.Single(b=>b.name=="Tail");
        r.Select(21);var tailPosition=pelvis.InverseTransformPoint(tail.position);var tailRotation=Quaternion.Inverse(pelvis.rotation)*tail.rotation;float drift=0,angle=0;
        for(int clip=17;clip<32;clip++){
            r.Select(clip);for(int f=0;f<=Mathf.RoundToInt(r.clips[clip].length*240);f++){
                r.Sample(f/240.0);drift=Mathf.Max(drift,Vector3.Distance(pelvis.InverseTransformPoint(tail.position),tailPosition));angle=Mathf.Max(angle,Quaternion.Angle(Quaternion.Inverse(pelvis.rotation)*tail.rotation,tailRotation));
            }
        }
        Check(drift<.0001f&&angle<.1f,$"Tail attachment {drift}/{angle}");
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();var head=skins.Single(s=>s.name=="Head");var spine=bones.Single(b=>b.name=="Spine");var hb=bones.Single(b=>b.name=="Head");
        r.Select(28);var marker=spine.InverseTransformPoint(Vector3.Lerp(spine.position,hb.position,.49f)+Vector3.right*.17f);var support=new[]{hb,pelvis,bones.Single(b=>b.name=="Hand_R"),bones.Single(b=>b.name=="Foot_R")};var initial=support.Select(b=>b.position).ToArray();
        float supportDrift=0,floor=99;var ranges=new float[2];
        for(int variant=0;variant<2;variant++){
            int clip=variant==0?28:30;r.Select(clip);float lo=99,hi=-99;
            for(int f=0;f<=Mathf.RoundToInt(r.clips[clip].length*240);f++){
                r.Sample(f/240.0);float x=spine.TransformPoint(marker).x;lo=Mathf.Min(lo,x);hi=Mathf.Max(hi,x);
                supportDrift=Mathf.Max(supportDrift,support.Select((b,i)=>Vector3.Distance(b.position,initial[i])).Max());
                if(f%8==0)floor=Mathf.Min(floor,skins.Where(s=>s.name=="Head"||s.name=="Shoes"||s.name=="BodyHands").Min(s=>AdultRabbitSitCoat.World(s).Min(p=>p.y)));
                Check(variant==0?r.SleepEyeWeight==0:r.SleepEyeWeight>.999f,"Side eye state");
            }
            ranges[variant]=hi-lo;
        }
        Check(supportDrift<=.0035f&&floor>=-.0005f,"Side sleep support");
        Check(Mathf.Abs(ranges[0]-.0078f)<.001f&&Mathf.Abs(ranges[1]-.0104f)<.001f,$"Side breathing ranges {ranges[0]}/{ranges[1]}");
        int cases=0;
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(int clip in new[]{29,30})foreach(int phase in Enumerable.Range(0,8))foreach(int dest in Enumerable.Range(0,5)){
            r.Select(clip);r.paused=false;r.slow=slow;r.Advance(r.clips[clip].length*phase/8/(slow?.5f:1));
            if(dest==0)r.RequestSupineReturn();else if(dest<3)r.RequestLyingReturn(dest==1);else r.RequestRun(dest==4);
            Check(r.selected==31,"Wake on the side before departure");
            for(int f=0;f<fps*32;f++){
                r.Advance(1f/fps);
                if(dest==0?r.selected==22:dest<3?r.Idle!=null:r.MovingReview)break;
            }
            Check(dest==0?r.selected==22:dest<3?r.Idle!=null&&r.Idle.Seated==(dest==1):r.MovingReview&&r.FootTransition.WantsRun==(dest==4),$"Side sleeping destination {fps}/{slow}/{clip}/{phase}/{dest}");cases++;
        }
        float entryStep=0;int cancellations=0;
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(int phase in Enumerable.Range(0,8)){
            r.Select(28);r.slow=slow;r.paused=false;r.Advance(phase*.5f/(slow?.5f:1));double clock=r.RestBreathPhase;var before=hb.position;r.RequestSleep();
            entryStep=Mathf.Max(entryStep,Vector3.Distance(before,hb.position));Check(Math.Abs(clock-r.RestBreathPhase)<1e-8,"Side breath clock reset");
            for(int f=0;f<fps*9;f++){double old=r.RestBreathPhase;r.Advance(1f/fps);Check(r.RestBreathPhase>old,"Stopped side breathing");}
            Check(r.selected==30&&r.Posture==AdultRabbitMotionReview.RestPosture.LeftSide,"Side sleep loop / posture");
            r.RequestRun(false);r.RequestRun(true);r.RequestWalk(false);for(int f=0;f<fps*5;f++)r.Advance(1f/fps);
            Check(r.selected==28&&!r.MovingReview&&r.SleepEyeWeight==0,"Cancel stays awake on side");
            r.paused=true;clock=r.RestBreathPhase;r.Advance(1);Check(clock==r.RestBreathPhase,"Pause");r.paused=false;cancellations++;
        }
        r.Select(30);r.SendMessage("OnDisable");Check(r.SleepEyeWeight==0&&r.RestBreathPhase==0,"Sleep reset");r.Select(28);r.slow=false;r.paused=false;r.RequestSleep();r.Advance(4);Check(r.selected==30,"Reactivation");r.Select(0);
        File.WriteAllText("Docs/AdultSideSleepVerification.txt",$"v0.111, direct calls/imported geometry, not OS input/user approval.\n240Hz 17-31 tail pelvis-local drift {drift*1000:F5}mm, angle {angle:F5}deg. Short-tail ground overlap allowed.\nAwake/sleep chest {ranges[0]*1000:F4}/{ranges[1]*1000:F4}mm; support {supportDrift*1000:F4}mm; measured head/hands/shoes floor {floor*1000:F4}mm.\n{cases} side sleep departure cases, {cancellations} phase/pause/cancel cases PASS. Entry head step {entryStep*1000:F4}mm. Tail-only preservation audited separately.\n");
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();Directory.CreateDirectory("Docs/Captures/SideSleep");Directory.CreateDirectory("Logs/SideSleepSequence");
        Action<int> camera=view=>{float a=view*Mathf.Deg2Rad;var focus=new Vector3(0,.22f,-.72f);r.reviewCamera.transform.position=focus+new Vector3(4.2f*Mathf.Sin(a),2.2f,4.2f*Mathf.Cos(a));r.reviewCamera.transform.LookAt(focus);};
        foreach(int clip in new[]{29,30,31})foreach(int view in new[]{0,45,90}){
            r.Select(clip);for(int pose=0;pose<8;pose++){r.Sample(r.clips[clip].length*AdultRabbitMotionReview.SleepPosePhases[pose]);camera(view);AdultSleepChecks.Capture(r,$"Docs/Captures/SideSleep/{clip}_{view}_Pose{pose}.png");}
            for(int f=0;f<=Mathf.RoundToInt(r.clips[clip].length*30);f++){r.Sample(f/30.0);camera(view);AdultSleepChecks.Capture(r,$"Logs/SideSleepSequence/{clip}_{view}_{f:D3}.png");}
        }
        r.Select(28);r.paused=false;r.slow=false;
        for(int f=0;f<=660;f++){if(f==30)r.RequestSleep();if(f==330)r.RequestRun(false);if(f>0)r.Advance(1f/30);camera(90);AdultSleepChecks.Capture(r,$"Logs/SideSleepSequence/Cycle_{f:D3}.png");}r.Select(0);
    }
}
