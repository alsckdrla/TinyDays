using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;

public static class AdultSandplayChecks {
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Execute(){try{Run();Debug.Log("ADULT_SANDPLAY_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        Check(r.clips.Length==17&&Mathf.Abs(r.clips[16].length-8)<.001&&!r.clips[16].isLooping,"Roster / timing");
        var bones=r.resident.GetComponentsInChildren<Transform>();var hand=bones.Single(b=>b.name=="Hand_R");var head=bones.First(b=>b.name=="Head"&&!b.GetComponent<Renderer>());var pelvis=bones.Single(b=>b.name=="Pelvis");
        var mesh=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="BodyHands");int bi=Array.IndexOf(mesh.bones,hand);var weights=mesh.sharedMesh.boneWeights;
        var indices=Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==bi&&weights[i].weight0>.99f).ToArray();
        Func<float> floor=()=>{var pts=AdultRabbitSitCoat.World(mesh);return indices.Min(i=>pts[i].y)-r.resident.transform.position.y;};
        r.Select(16);r.Sample(0);AdultIdleVariations.ConfigureHeels(r.resident,r.idleProfile.Supports);
        var supports=r.idleProfile.Supports;var heels=supports.Select(l=>l.End.TransformPoint(l.HeelLocal)).ToArray();var pelvis0=pelvis.position;
        var joints=new[]{"L","R"}.SelectMany(side=>new[]{("UpperArm_"+side,"Forearm_"+side),("Forearm_"+side,"Hand_"+side),("Thigh_"+side,"Shin_"+side),("Shin_"+side,"Foot_"+side)}).Select(pair=>new[]{bones.Single(b=>b.name==pair.Item1),bones.Single(b=>b.name==pair.Item2)}).ToArray();
        var lengths=joints.Select(j=>Vector3.Distance(j[0].position,j[1].position)).ToArray();
        var rest=bones.Select(b=>b.localPosition).ToArray();var rot=bones.Select(b=>b.localRotation).ToArray();var scales=bones.Select(b=>b.localScale).ToArray();
        float drift=0,min=999,gap=0,len=0;var csv=new List<string>{"time,hand_x,hand_y,hand_z,hand_floor,head_y,heel_drift"};var contact=new List<Vector3>();
        for(int i=0;i<=1920;i++){
            float t=i/240f;r.Sample(t);float f=floor();min=Mathf.Min(min,f);
            drift=Mathf.Max(drift,Vector3.Distance(pelvis0,pelvis.position),supports.Select((l,j)=>Vector3.Distance(heels[j],l.End.TransformPoint(l.HeelLocal))).Max());
            for(int j=0;j<joints.Length;j++)len=Mathf.Max(len,Mathf.Abs(lengths[j]-Vector3.Distance(joints[j][0].position,joints[j][1].position)));
            for(int j=0;j<bones.Length;j++)Check(Vector3.Distance(scales[j],bones[j].localScale)<.00001,"Scale changed");
            if(t>=1.2f&&t<=6.5f){gap=Mathf.Max(gap,f);contact.Add(hand.position);}
            csv.Add(FormattableString.Invariant($"{t:F6},{hand.position.x:F7},{hand.position.y:F7},{hand.position.z:F7},{f:F7},{head.position.y:F7},{drift:F7}"));
        }
        File.WriteAllLines("Docs/AdultSandplayMotion.csv",csv);
        var report=new List<string>{$"240Hz: support drift {drift*1000:F4}mm; hand minimum {min*1000:F4}mm; drawing gap {gap*1000:F4}mm; limb error {len*1000:F4}mm."};
        File.WriteAllLines("Docs/AdultSandplayVerification.txt",report);
        Check(drift<=.0035&&min>=-.0005&&gap<=.005&&len<=.0001,$"Contact / reach: {string.Join(";",report)}");
        float range=contact.Max(p=>p.x)-contact.Min(p=>p.x);Check(range>=.039&&range<=.061,"Drawing width");
        for(int i=0;i<bones.Length;i++)Check(Vector3.Distance(rest[i],bones[i].localPosition)<.0001&&Quaternion.Angle(rot[i],bones[i].localRotation)<.1,"End pose mismatch "+bones[i].name);
        int cases=0;float recoveryFloor=999,recoveryDrift=0;
        foreach(float phase in new[]{0,.5f,1.2f,2.2f,3.2f,4.3f,5.8f,6.45f,6.6f,7.4f})foreach(bool style in new[]{false,true})foreach(bool run in new[]{false,true})foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true}){
            r.Select(0);r.SetSitStyle(style);r.StartBreathing(true);r.slow=slow;r.Idle.Play("앉아서 낙서하기");r.Advance((.25f+phase)/(slow?.5f:1));
            var h=head.position;var hh=supports.Select(l=>l.End.TransformPoint(l.HeelLocal)).ToArray();r.RequestRun(!run);r.RequestRun(run);r.Advance(0);Check(Vector3.Distance(h,head.position)<.0001,"Interrupted pose jumped");
            double time=r.Idle.Time;r.paused=true;r.Advance(.2f);Check(r.Idle.Time==time,"Pause");r.paused=false;
            for(int n=0;n<fps*7&&!r.MovingReview;n++){
                r.Advance(1f/fps);
                if(r.Idle!=null&&r.Idle.State==CommonIdleDirector.Stage.Tidying&&r.Idle.Seated){recoveryFloor=Mathf.Min(recoveryFloor,floor());recoveryDrift=Mathf.Max(recoveryDrift,supports.Select((l,j)=>Vector3.Distance(hh[j],l.End.TransformPoint(l.HeelLocal))).Max());}
            }
            Check(r.MovingReview&&r.FootTransition.WantsRun==run,"Departure stalled");cases++;
        }
        Check(recoveryFloor>=-.0005&&recoveryDrift<=.0035,$"Recovery floor/drift {recoveryFloor} / {recoveryDrift}");
        int cancels=0;
        foreach(float phase in new[]{.5f,1.2f,3.3f,6.4f,6.7f})foreach(bool renew in new[]{false,true})foreach(bool style in new[]{false,true})foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true}){
            r.Select(0);r.SetSitStyle(style);r.StartBreathing(true);r.slow=slow;r.Idle.Play("앉아서 낙서하기");r.Advance((.25f+phase)/(slow?.5f:1));r.RequestRun(false);r.Advance(.1f/(slow?.5f:1));r.RequestWalk(false);if(renew)r.RequestRun(true);
            for(int i=0;i<fps*6&&!r.MovingReview;i++){r.Advance(1f/fps);if(!renew&&r.Idle.State==CommonIdleDirector.Stage.Breathing)break;}
            Check(renew?r.MovingReview:r.Idle!=null&&r.Idle.State==CommonIdleDirector.Stage.Breathing,"Cancel/re-request");cancels++;
        }
        r.StartBreathing(true);Check(r.Idle.CandidateCount==3,"Seated roster");r.Select(0);Check(r.Idle==null,"Reset");
        report.Add($"Drawing width {range*1000:F3}mm; {cases} departures, {cancels} cancellation/re-request cases PASS; recovery hand floor {recoveryFloor*1000:F4}mm, heel drift {recoveryDrift*1000:F4}mm.");
        report.Add("Automatic samples/callbacks only. Physical input and user motion quality pending. Fine coat collision excluded.");File.WriteAllLines("Docs/AdultSandplayVerification.txt",report);
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        var capture=typeof(AdultRabbitMotionBuilder).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Camera),typeof(SkinnedMeshRenderer[]),typeof(string),typeof(string)},null);
        Directory.CreateDirectory("Logs/SandplaySequence");Directory.CreateDirectory("Docs/Captures/Sandplay");
        foreach(int view in new[]{0,45,90}){
            r.StartBreathing(true);r.slow=false;
            for(int frame=0;frame<300;frame++){if(frame>0)r.Advance(1f/30);if(frame==23)r.Idle.Play("앉아서 낙서하기");r.View(view);capture.Invoke(null,new object[]{r.reviewCamera,skins,$"{view}_{frame:D3}","Logs/SandplaySequence"});}
            for(int pose=0;pose<8;pose++){r.Select(16);r.Pose(pose);r.View(view);capture.Invoke(null,new object[]{r.reviewCamera,skins,$"{view}_Pose{pose}","Docs/Captures/Sandplay"});}
            r.StartBreathing(true);r.slow=false;r.Idle.Play("앉아서 낙서하기");r.Advance(2.5f);
            for(int frame=0;frame<150;frame++){if(frame>0)r.Advance(1f/30);if(frame==15)r.RequestRun(false);r.View(view);capture.Invoke(null,new object[]{r.reviewCamera,skins,$"Interrupt_{view}_{frame:D3}","Logs/SandplaySequence"});}
        }
        r.Select(0);Debug.Log("ADULT_SANDPLAY_SEQUENCE_OK");
    }
}
