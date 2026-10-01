using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;
public static class AdultSideRiseChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Execute(){try{Run();Debug.Log("DIRECT_SIDE_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Diagnose(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();var skin=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name=="Head");
        r.Select(32);
        foreach(float sampleTime in new[]{2.2f,2.3f,2.32f,2.4f}){
            r.clips[32].SampleAnimation(r.resident,sampleTime);
            Debug.Log("FOOT_RAW "+sampleTime+" "+skin.bones.First(b=>b.name=="Foot_L").position.ToString("F6"));
            r.Sample(sampleTime);
            Debug.Log("FOOT_LIVE "+sampleTime+" "+skin.bones.First(b=>b.name=="Foot_L").position.ToString("F6"));
        }
        r.Select(33);
        foreach(float sampleTime in new[]{2.8f,2.9f,2.91f,3.2f}){
            r.clips[33].SampleAnimation(r.resident,sampleTime);
            Debug.Log("STAND_FOOT_RAW "+sampleTime+" "+skin.bones.First(b=>b.name=="Foot_L").position.ToString("F6"));
            r.Sample(sampleTime);
            Debug.Log("STAND_FOOT_LIVE "+sampleTime+" "+skin.bones.First(b=>b.name=="Foot_L").position.ToString("F6"));
        }
        foreach(int clip in new[]{32,33,8,9}){
            r.Select(clip);double t=clip==32?1.35:clip==33?1.75:3.2;
            r.clips[clip].SampleAnimation(r.resident,(float)t);
            Debug.Log("RAW "+clip+" actor "+r.resident.transform.localPosition+" / "+r.resident.transform.localScale+" "+string.Join(";",skin.bones.Where(b=>new[]{"Root","Pelvis","Spine","Head","Hand_L","Hand_R","Foot_L","Thigh_R","Shin_R","Foot_R"}.Contains(b.name)).Select(b=>b.name+"="+b.position.ToString("F4")+" scale"+b.localScale)));
            r.Sample(t);
            Debug.Log("LIVE "+clip+" actor "+r.resident.transform.localPosition+" / "+r.resident.transform.localScale+" "+string.Join(";",skin.bones.Where(b=>new[]{"Root","Pelvis","Spine","Head","Hand_L","Hand_R","Foot_L"}.Contains(b.name)).Select(b=>b.name+"="+b.position.ToString("F4")+" scale"+b.localScale)));
        }
        foreach(int reference in new[]{28,8,9}){
            r.Select(33);r.clips[reference].SampleAnimation(r.resident,0);
            Debug.Log("NEUTRAL "+reference+" "+string.Join(";",skin.bones.Where(b=>new[]{"Root","Spine","Head"}.Contains(b.name)).Select(b=>b.name+" local="+b.localPosition.ToString("F6"))));
            r.clips[reference].SampleAnimation(r.resident,1.75f);
            Debug.Log("PHASE "+reference+" "+string.Join(";",skin.bones.Where(b=>new[]{"Root","Spine","Head"}.Contains(b.name)).Select(b=>b.name+" local="+b.localPosition.ToString("F6"))));
        }
    }
    public static void FinalizeReview(){try{Run();Sequence();AdultRabbitMotionBuilder.BuildPlayer();Debug.Log("DIRECT_SIDE_FINAL_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Tick(AdultRabbitMotionReview r,int fps,float seconds){for(int f=0;f<fps*seconds;f++)r.Advance(1f/fps);}
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        Check(r.clips.Length>=34&&r.clips[32].name.EndsWith("Adult_Left_To_Sit")&&r.clips[33].name.EndsWith("Adult_Left_To_Stand"),"Stable34slots");
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();var bones=skins.First(s=>s.name=="Head").bones;
        var pairs=new[]{"L","R"}.SelectMany(s=>new[]{new[]{"UpperArm_"+s,"Forearm_"+s},new[]{"Forearm_"+s,"Hand_"+s},new[]{"Thigh_"+s,"Shin_"+s},new[]{"Shin_"+s,"Foot_"+s}}).Select(a=>a.Select(n=>bones.First(b=>b.name==n)).ToArray()).ToArray();
        var pelvis=bones.First(b=>b.name=="Pelvis");var tail=bones.First(b=>b.name=="Tail");r.Select(28);
        var lengths=pairs.Select(a=>Vector3.Distance(a[0].position,a[1].position)).ToArray();var attachment=pelvis.InverseTransformPoint(tail.position);var tq=Quaternion.Inverse(pelvis.rotation)*tail.rotation;
        float floor=99,limb=0,tailError=0,tailAngle=0,handDrift=0,handGap=0,individualHandGap=0,footDrift=0,endpoint=0,headStep=0,maxAcceleration=0;
        string floorAt="";float kneeFloor=float.PositiveInfinity,kneeFloorAt=0,kneeDrift=0,kneeContactGap=0;
        var csv=new List<string>{"clip,time,spine_y,head_y,head_speed,head_acceleration,left_knee_y"};
        foreach(int clip in new[]{32,33}){
            r.Select(clip);Check(!r.clips[clip].isLooping&&Math.Abs(r.clips[clip].length-(clip==32?2.4:3.2))<.002,"Duration/loop");
            float contact=clip==32?1.35f:1.1f;r.Sample(contact);var hands=(clip==32?new[]{"Hand_R"}:new[]{"Hand_L","Hand_R"}).Select(n=>bones.First(b=>b.name==n)).ToArray();var fixedHands=hands.Select(b=>b.position).ToArray();
            var foot=bones.First(b=>b.name=="Foot_L");r.Sample(clip==32?2.3:2.9);var fixedFoot=foot.position;
            r.Sample(1.7);var fixedKnee=bones.First(b=>b.name=="Shin_R").position;
            var head=bones.First(b=>b.name=="Head");var spine=bones.First(b=>b.name=="Spine");Vector3 last=Vector3.zero,velocity=Vector3.zero;
            for(int f=0;f<=Mathf.RoundToInt(r.clips[clip].length*240);f++){
                float t=f/240f;r.Sample(t);
                limb=Mathf.Max(limb,pairs.Select((a,i)=>Mathf.Abs(Vector3.Distance(a[0].position,a[1].position)-lengths[i])).Max());
                tailError=Mathf.Max(tailError,Vector3.Distance(pelvis.InverseTransformPoint(tail.position),attachment));tailAngle=Mathf.Max(tailAngle,Quaternion.Angle(Quaternion.Inverse(pelvis.rotation)*tail.rotation,tq));
                if(t>=(clip==32?1.315f:1.075f)&&t<=(clip==32?1.395f:1.135f)){
                    handDrift=Mathf.Max(handDrift,hands.Select((b,i)=>Vector3.Distance(b.position,fixedHands[i])).Max());
                    var handSkin=skins.First(s=>s.name=="BodyHands");
                    handGap=Mathf.Max(handGap,Math.Abs(HandFloor(handSkin)));
                    foreach(var hand in hands)individualHandGap=Mathf.Max(individualHandGap,Math.Abs(SupportFloor(handSkin,hand.name)));
                }
                if(t>=(clip==32?2.32f:2.91f))footDrift=Mathf.Max(footDrift,Vector3.Distance(foot.position,fixedFoot));
                if(f%4==0)foreach(var skin in skins.Where(s=>new[]{"Head","BodyTorso","BodyLegs","BodyHands","Shoes"}.Contains(s.name))){float sampleFloor=AdultRabbitSitCoat.World(skin).Min(p=>p.y);if(sampleFloor<floor){floor=sampleFloor;floorAt=clip+" / "+t.ToString("F4")+" / "+skin.name;}}
                if(clip==33&&t>=1.35f&&t<=2.25f&&f%4==0){float sampleKnee=KneeFloor(skins.First(s=>s.name=="BodyLegs"),bones.First(b=>b.name=="Shin_R"));if(sampleKnee<kneeFloor){kneeFloor=sampleKnee;kneeFloorAt=t;}}
                if(clip==33&&t>=1.66f&&t<=1.74f){kneeDrift=Mathf.Max(kneeDrift,Vector3.Distance(fixedKnee,bones.First(b=>b.name=="Shin_R").position));kneeContactGap=Mathf.Max(kneeContactGap,Math.Abs(KneeFloor(skins.First(s=>s.name=="BodyLegs"),bones.First(b=>b.name=="Shin_R"))));}
                float acceleration=0;if(f>0){var v=(head.position-last)*240;headStep=Mathf.Max(headStep,Vector3.Distance(head.position,last));if(f>1){acceleration=(v-velocity).magnitude*240;maxAcceleration=Mathf.Max(maxAcceleration,acceleration);}velocity=v;}last=head.position;
                csv.Add(FormattableString.Invariant($"{clip},{t:F6},{spine.position.y:F6},{head.position.y:F6},{velocity.magnitude:F6},{acceleration:F6},{bones.First(b=>b.name=="Shin_R").position.y:F6}"));
            }
            r.Sample(r.clips[clip].length);var final=bones.Select(b=>b.position).ToArray();r.Select(clip==32?9:8);r.Sample((clip==32?2.4:3.2)/4*4);endpoint=Mathf.Max(endpoint,bones.Select((b,i)=>Vector3.Distance(b.position,final[i])).Max());
        }
        var report=new List<string>{"v0.112 direct side returns. Automatic method/geometry samples, not OS input or user quality approval.",$"240Hz floor {floor*1000:F4}mm at {floorAt}; limb length difference {limb*1000:F4}mm; tail attachment {tailError*1000:F4}mm/{tailAngle:F5}deg.",$"Hands contact drift {handDrift*1000:F4}mm; contact floor gap {handGap*1000:F4}mm; individual hand gap {individualHandGap*1000:F4}mm; kneeling leg minimum surface gap {kneeFloor*1000:F4}mm at {kneeFloorAt:F4}s; final planted-foot drift {footDrift*1000:F4}mm; endpoint joint difference {endpoint*1000:F4}mm.",$"Head step {headStep*1000:F4}mm at240Hz; max finite-difference acceleration {maxAcceleration:F4}m/s2."};
        report.Add($"v0.114 designated knee support1.66-1.74s: drift {kneeDrift*1000:F4}mm; max surface gap {kneeContactGap*1000:F4}mm.");
        File.WriteAllLines("Docs/AdultSideRiseVerification.txt",report);File.WriteAllLines("Docs/AdultSideRiseMotion.csv",csv);
        Check(floor>=-.0005f&&limb<=.0005f&&tailError<.0001f&&tailAngle<.1f,"Floor/length/tail");
        Check(handDrift<=.0035f&&handGap<=.005f&&individualHandGap<=.005f&&footDrift<=.0035f,"Measured support contacts");
        Check(kneeFloor<=.005f&&kneeDrift<=.0035f&&kneeContactGap<=.005f,"Kneeling leg support gap/drift");
        Check(endpoint<=.0035f,"End pose connects to idle");
        int cases=0;
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(int start in new[]{28,29,30})foreach(int phase in Enumerable.Range(0,8))foreach(int dest in Enumerable.Range(0,4)){
            r.Select(start);r.slow=slow;r.paused=false;r.Advance(r.clips[start].length*phase/8/(slow?.5f:1));
            if(dest<2)r.RequestLyingReturn(dest==0);else r.RequestRun(dest==3);
            if(start==28)Check(r.selected==(dest==0?32:33),"Direct entry route");else Check(r.selected==31,"Wake first on side");
            bool direct=false;
            for(int f=0;f<fps*22;f++){
                Check(r.selected!=27&&r.selected!=18&&r.selected!=20,"Unexpected supine detour");direct|=AdultRabbitMotionReview.IsDirectSideReturn(r.selected);r.Advance(1f/fps);
                if(dest<2?r.Idle!=null:r.MovingReview)break;
            }
            Check(direct&&(dest<2?r.Idle!=null&&r.Idle.Seated==(dest==0):r.MovingReview&&r.FootTransition.WantsRun==(dest==3)),"Final destination");cases++;
        }
        foreach(int clip in new[]{32,33})foreach(int phase in Enumerable.Range(0,8)){
            r.Select(28);r.slow=false;r.RequestLyingReturn(clip==32);r.Advance(r.clips[clip].length*phase/8);int active=r.selected;
            r.RequestRun(false);r.RequestRun(true);r.RequestWalk(false);Check(r.selected==active,"Cancel did not rewind body");Tick(r,60,6);Check(r.Idle!=null&&r.Idle.Seated==(clip==32),"Cancel ends safely");
            r.Select(28);r.RequestLyingReturn(clip==32);r.Advance(r.clips[clip].length*phase/8);r.RequestLyingReturn(clip==33);Tick(r,60,8);Check(r.Idle!=null&&r.Idle.Seated==(clip==33),"Latest posture after current transition");
        }
        r.Select(28);r.RequestRun(false);r.Advance(.5f);var elapsed=r.Elapsed;var breath=r.RestBreathPhase;r.paused=true;r.Advance(1);Check(elapsed==r.Elapsed&&breath==r.RestBreathPhase,"Pause both clocks");r.paused=false;Tick(r,60,8);Check(r.MovingReview,"Resume departure");r.ExitLyingReview();Check(r.SleepEyeWeight==0,"Eye cleanup");
        report.Add($"{cases} phase/fps/speed awake/sleep direct destinations PASS;32 cancel/latest-posture checks; pause/resume/exit PASS.");File.WriteAllLines("Docs/AdultSideRiseVerification.txt",report);
    }
    static float HandFloor(SkinnedMeshRenderer skin)=>AdultRabbitSitCoat.World(skin).Min(p=>p.y);
    static float SupportFloor(SkinnedMeshRenderer skin,string boneName){
        var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var vertices=AdultRabbitSitCoat.World(skin);
        int bone=Array.FindIndex(skin.bones,b=>b.name==boneName);Check(bone>=0,"Support bone present");
        float lowest=float.PositiveInfinity;
        for(int i=0;i<weights.Length;i++){
            var w=weights[i];
            float influence=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)+(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
            if(influence>.9f)lowest=Mathf.Min(lowest,vertices[i].y);
        }
        Check(!float.IsInfinity(lowest),"Weighted support vertices");return lowest;
    }
    static float KneeFloor(SkinnedMeshRenderer skin,Transform knee){
        var vertices=AdultRabbitSitCoat.World(skin);float lowest=float.PositiveInfinity;
        for(int i=0;i<vertices.Length;i++){
            var p=vertices[i];var delta=p-knee.position;
            if(delta.x*delta.x+delta.z*delta.z<.08f*.08f&&p.y<knee.position.y+.05f)lowest=Mathf.Min(lowest,p.y);
        }
        Check(!float.IsInfinity(lowest),"Kneeling leg surface present");return lowest;
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();Directory.CreateDirectory("Docs/Captures/SideRise");Directory.CreateDirectory("Logs/SideRiseSequence");
        Action<int> camera=view=>{float a=view*Mathf.Deg2Rad;var focus=new Vector3(0,.62f,-.12f);r.reviewCamera.transform.position=focus+new Vector3(4.2f*Mathf.Sin(a),1.3f,4.2f*Mathf.Cos(a));r.reviewCamera.transform.LookAt(focus);};
        foreach(int clip in new[]{32,33})foreach(int view in new[]{0,45,90}){
            r.Select(clip);for(int p=0;p<8;p++){r.Sample(r.clips[clip].length*AdultRabbitMotionReview.LyingPosePhases[p]);camera(view);AdultSleepChecks.Capture(r,$"Docs/Captures/SideRise/{clip}_{view}_Pose{p}.png");}
            for(int f=0;f<=Mathf.RoundToInt(r.clips[clip].length*30);f++){r.Sample(f/30.0);camera(view);AdultSleepChecks.Capture(r,$"Logs/SideRiseSequence/{clip}_{view}_{f:D3}.png");}
        }
        r.Select(30);r.paused=false;r.slow=false;for(int f=0;f<=420;f++){if(f==30)r.RequestRun(false);if(f>0)r.Advance(1f/30);camera(45);AdultSleepChecks.Capture(r,$"Logs/SideRiseSequence/Cycle_{f:D3}.png");}r.Select(0);
    }
}
