using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;

public static class AdultSideDownChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Execute(){try{Run();Debug.Log("SIDE_DOWN_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Extras(){try{
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        int phases=0,variations=0;
        foreach(bool seated in new[]{false,true})foreach(int phase in Enumerable.Range(0,8)){
            r.StartBreathing(seated);r.slow=false;r.Advance(phase*.5f);double before=r.Idle.Time/4;
            r.RequestDirectSideLie();r.Advance(.25f);Check(r.selected==(seated?35:34),"Breath posture entry");
            Check(Math.Abs(r.RestBreathPhase-before-.25/4)<.0001,"Breath phase reset during tidying");
            Tick(r,4);Check(r.selected==28&&r.SleepEyeWeight==0,"Awake side rest");phases++;
        }
        foreach(bool seated in new[]{false,true}){
            r.StartBreathing(seated);var candidates=r.Idle.Candidates.ToArray();
            foreach(string candidate in candidates)foreach(int phase in new[]{1,4,7}){
                r.StartBreathing(seated);r.Idle.Play(candidate);r.Advance(.25f);r.Advance(r.Idle.CurrentClip.length*phase/8);
                r.RequestDirectSideLie();Tick(r,6);Check(r.selected==28,"Variation tidy to side: "+candidate);variations++;
            }
        }
        // Pose preview, disable/re-enable and reset are deliberately separate
        // from the completed-transition state-machine checks.
        r.StartBreathing(false);r.RequestDirectSideLie();Tick(r,.4f);r.Pose(4);Check(r.paused&&r.selected==34,"Eight-pose preview");r.Select(0);Check(!r.LyingReview&&r.SleepEyeWeight==0,"Clip cleanup");
        r.StartBreathing(true);r.RequestDirectSideLie();Tick(r,.4f);r.SendMessage("OnDisable");Check(!r.PendingLie&&!r.LyingReview&&r.SleepEyeWeight==0,"Disable cleanup");r.StartBreathing(false);Check(r.Idle!=null,"Reactivation");
        File.WriteAllText("Docs/AdultSideDownExtras.txt",$"{phases} breathing-phase entries, {variations} variation interruptions, pose selection, clip/disable cleanup PASS. Automatic calls; not OS input.\n");Debug.Log("SIDE_DOWN_EXTRAS_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Diagnose(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();var skin=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name=="BodyHands");
        r.Select(34);foreach(float t in new[]{1.65f,1.7125f,1.7208333f,1.75f,1.2875f})foreach(bool live in new[]{false,true}){
            if(live)r.Sample(t);else r.clips[34].SampleAnimation(r.resident,t);
            Debug.Log("SIDE_DOWN_DIAG "+t+" "+live+" handL "+Surface(skin,"Hand_L")+" handR "+Surface(skin,"Hand_R"));
        }
    }
    static void Tick(AdultRabbitMotionReview r,float seconds,int fps=60){for(int i=0;i<seconds*fps;i++)r.Advance(1f/fps);}
    static float Surface(SkinnedMeshRenderer skin,string bone){
        int index=Array.FindIndex(skin.bones,b=>b.name==bone);var pts=AdultRabbitSitCoat.World(skin);var w=skin.sharedMesh.boneWeights;float floor=99;
        for(int i=0;i<w.Length;i++){
            var b=w[i];float weight=(b.boneIndex0==index?b.weight0:0)+(b.boneIndex1==index?b.weight1:0)+(b.boneIndex2==index?b.weight2:0)+(b.boneIndex3==index?b.weight3:0);
            if(weight>.9f)floor=Mathf.Min(floor,pts[i].y);
        }
        return floor;
    }
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        Check(r.clips.Length==36&&r.clips[34].name.EndsWith("Adult_Stand_To_Left")&&r.clips[35].name.EndsWith("Adult_Sit_To_Left"),"36 stable slots");
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();var bones=skins.First(s=>s.name=="Head").bones;
        Func<string,Transform> bone=n=>bones.First(b=>b.name==n);
        var pairs=new[]{"L","R"}.SelectMany(s=>new[]{new[]{"Thigh_"+s,"Shin_"+s},new[]{"Shin_"+s,"Foot_"+s},new[]{"UpperArm_"+s,"Forearm_"+s},new[]{"Forearm_"+s,"Hand_"+s}}).Select(a=>a.Select(bone).ToArray()).ToArray();
        r.Select(28);var lengths=pairs.Select(a=>Vector3.Distance(a[0].position,a[1].position)).ToArray();
        var pelvis=bone("Pelvis");var tail=bone("Tail");var tailP=pelvis.InverseTransformPoint(tail.position);var tailQ=Quaternion.Inverse(pelvis.rotation)*tail.rotation;
        var leg=bones.Where(b=>b.name.StartsWith("Thigh_")||b.name.StartsWith("Shin_")||b.name.StartsWith("Foot_")).ToArray();
        float floor=99,limb=0,tailError=0,tailAngle=0,drift=0,gap=0,endpoint=0,maxStep=0,kneeDrift=0,kneeGap=0,footDrift=0;string worst="",floorAt="";
        var csv=new List<string>{"clip,time,head_y,chest_y,left_knee_y,max_leg_step_deg"};
        foreach(int clip in new[]{34,35}){
            r.Select(clip);float duration=clip==34?3.2f:2.4f;Check(!r.clips[clip].isLooping&&Math.Abs(r.clips[clip].length-duration)<.002,"Duration/loop");
            r.Sample(clip==34?1.76:.55);var hand=bone(clip==34?"Hand_L":"Hand_R");var fixedHand=hand.position;
            r.Sample(1.05);var fixedKnee=bone("Shin_R").position;
            r.Sample(0);var fixedFoot=bone("Foot_L").position;Quaternion[] previous=null;
            for(int f=0;f<=Mathf.RoundToInt(duration*240);f++){
                float t=f/240f;r.Sample(t);
                limb=Mathf.Max(limb,pairs.Select((a,i)=>Mathf.Abs(Vector3.Distance(a[0].position,a[1].position)-lengths[i])).Max());
                tailError=Mathf.Max(tailError,Vector3.Distance(tailP,pelvis.InverseTransformPoint(tail.position)));tailAngle=Mathf.Max(tailAngle,Quaternion.Angle(tailQ,Quaternion.Inverse(pelvis.rotation)*tail.rotation));
                float step=previous==null?0:leg.Select((b,i)=>Quaternion.Angle(previous[i],b.rotation)).Max();previous=leg.Select(b=>b.rotation).ToArray();
                if(step>maxStep){maxStep=step;worst=clip+" / "+t;}
                if(t>=(clip==34?1.72f:.49f)&&t<=(clip==34?1.80f:.61f)){
                    drift=Mathf.Max(drift,Vector3.Distance(fixedHand,hand.position));gap=Mathf.Max(gap,Math.Abs(Surface(skins.First(s=>s.name=="BodyHands"),hand.name)));
                }
                if(clip==34&&t<=1.1f)footDrift=Mathf.Max(footDrift,Vector3.Distance(fixedFoot,bone("Foot_L").position));
                if(clip==34&&t>=1.01f&&t<=1.09f){
                    kneeDrift=Mathf.Max(kneeDrift,Vector3.Distance(fixedKnee,bone("Shin_R").position));
                    var k=bone("Shin_R").position;float min=AdultRabbitSitCoat.World(skins.First(s=>s.name=="BodyLegs")).Where(p=>(new Vector2(p.x-k.x,p.z-k.z)).sqrMagnitude<.08f*.08f&&p.y<k.y+.05f).Min(p=>p.y);
                    kneeGap=Mathf.Max(kneeGap,Math.Abs(min));
                }
                foreach(var skin in skins.Where(s=>new[]{"Head","BodyTorso","BodyLegs","BodyHands","Shoes"}.Contains(s.name))){float y=AdultRabbitSitCoat.World(skin).Min(p=>p.y);if(y<floor){floor=y;floorAt=clip+" / "+t+" / "+skin.name;}}
                csv.Add(FormattableString.Invariant($"{clip},{t:F6},{bone("Head").position.y:F6},{bone("Spine").position.y:F6},{bone("Shin_R").position.y:F6},{step:F6}"));
            }
            var end=bones.Select(b=>b.position).ToArray();r.Select(28);r.Sample(duration);endpoint=Mathf.Max(endpoint,bones.Select((b,i)=>Vector3.Distance(b.position,end[i])).Max());
        }
        var report=new List<string>{"v0.115 direct left-side entry: automatic geometry/callback checks; not physical OS input or user motion approval.",$"240Hz floor {floor*1000:F4}mm; limb error {limb*1000:F4}mm; tail {tailError*1000:F4}mm / {tailAngle:F5}deg.",$"Hand drift {drift*1000:F4}mm / gap {gap*1000:F4}mm; knee drift {kneeDrift*1000:F4}mm / gap {kneeGap*1000:F4}mm; supporting right foot drift {footDrift*1000:F4}mm.",$"Endpoint {endpoint*1000:F4}mm; leg maximum step {maxStep:F4}deg/240Hz at {worst}."};
        report.Add("Lowest surface at "+floorAt);
        File.WriteAllLines("Docs/AdultSideDownVerification.txt",report);File.WriteAllLines("Docs/AdultSideDownMotion.csv",csv);
        Check(floor>=-.0005f&&limb<=.0005f&&tailError<=.0001f&&tailAngle<=.1f,"Floor/length/tail");
        Check(drift<=.0035f&&gap<=.005f&&kneeDrift<=.0035f&&kneeGap<=.005f&&footDrift<=.0035f,"Contact drift/gap");
        Check(endpoint<=.0035f&&maxStep<=4,"Endpoint/rotation continuity");
        int cases=0;
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(bool seated in new[]{false,true})foreach(int phase in Enumerable.Range(0,8))foreach(int dest in Enumerable.Range(0,4)){
            r.StartBreathing(seated);r.slow=slow;r.paused=false;r.RequestDirectSideLie();Tick(r,.3f/(slow?.5f:1),fps);Check(r.selected==(seated?35:34),"Direct entry selected");
            r.Advance(r.clips[r.selected].length*phase/8/(slow?.5f:1));
            if(dest<2)r.RequestLyingReturn(dest==0);else r.RequestRun(dest==3);
            for(int f=0;f<fps*25;f++){
                Check(r.selected!=17&&r.selected!=19&&r.selected!=26&&r.selected!=27,"Supine detour");r.Advance(1f/fps);
                if(dest<2?r.Idle!=null:r.MovingReview)break;
            }
            Check(dest<2?r.Idle!=null&&r.Idle.Seated==(dest==0):r.MovingReview&&r.FootTransition.WantsRun==(dest==3),"Final request route");cases++;
        }
        foreach(bool seated in new[]{false,true})foreach(int phase in Enumerable.Range(0,8)){
            r.StartBreathing(seated);r.slow=false;r.RequestDirectSideLie();Tick(r,.3f);r.Advance(r.clips[r.selected].length*phase/8);
            int active=r.selected;r.RequestRun(false);r.RequestRun(true);r.RequestWalk(false);Check(r.selected==active,"Cancel rewound transition");Tick(r,5);Check(r.selected==28,"Cancel ends at awake side rest");
            r.RequestSleep();Tick(r,3.5f);Check(r.selected==30,"Sleep after direct entry");r.RequestRun(true);Tick(r,9);Check(r.MovingReview,"Side sleep direct rise and move");
        }
        r.StartBreathing(false);r.RequestRun(false);Tick(r,1);r.RequestDirectSideLie();Tick(r,6);Check(r.selected==28,"Moving stops then direct side entry");
        r.StartBreathing(true);r.RequestDirectSideLie();Tick(r,.4f);var t0=r.Elapsed;var p0=r.RestBreathPhase;r.paused=true;r.Advance(1);Check(t0==r.Elapsed&&p0==r.RestBreathPhase,"Pause clocks");r.paused=false;Tick(r,4);Check(r.selected==28,"Resume");r.ExitLyingReview();Check(!r.LyingReview&&r.SleepEyeWeight==0,"Cleanup");
        report.Add($"{cases} phase/fps/speed request combinations;16 cancel/latest-request/sleep paths; moving stop, pause/resume and cleanup PASS.");File.WriteAllLines("Docs/AdultSideDownVerification.txt",report);
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();Directory.CreateDirectory("Docs/Captures/SideDown");Directory.CreateDirectory("Logs/SideDownSequence");
        Action<int> camera=view=>{float a=view*Mathf.Deg2Rad;var focus=new Vector3(0,.72f,-.12f);r.reviewCamera.transform.position=focus+new Vector3(4.8f*Mathf.Sin(a),1.4f,4.8f*Mathf.Cos(a));r.reviewCamera.transform.LookAt(focus);};
        foreach(int clip in new[]{34,35})foreach(int view in new[]{0,45,90}){
            r.Select(clip);for(int p=0;p<8;p++){r.Sample(r.clips[clip].length*AdultRabbitMotionReview.LyingPosePhases[p]);camera(view);AdultSleepChecks.Capture(r,$"Docs/Captures/SideDown/{clip}_{view}_Pose{p}.png");}
            for(int f=0;f<=Mathf.RoundToInt(r.clips[clip].length*30);f++){r.Sample(f/30.0);camera(view);AdultSleepChecks.Capture(r,$"Logs/SideDownSequence/{clip}_{view}_{f:D3}.png");}
        }
        r.Select(0);
    }
}
