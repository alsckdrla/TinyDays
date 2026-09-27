using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;

public static class AdultFidgetChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Execute(){try{Run();Debug.Log("ADULT_FIDGET_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        var report=new List<string>{"v0.104 upright contrapposto: automatic checks, not physical input or user quality approval."};
        var csv=new List<string>{"clip,time,pelvis_x,pelvis_y,head_y,head_speed,heel_drift,sole,chest_y"};
        var bones=r.resident.GetComponentsInChildren<Transform>();var pelvis=bones.First(b=>b.name=="Pelvis");var head=bones.First(b=>b.name=="Head");
        var shoes=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Shoes");var legs=r.idleProfile.Supports;
        foreach(var d in AdultIdleVariations.All.Where(d=>d.Index==14||d.Index==15)){
            var clip=r.clips[d.Index];Check(Math.Abs(clip.length-(d.Seated?4:13))<.001&&!clip.isLooping,"Duration/loop");
            r.Select(d.Index);r.Sample(0);AdultIdleVariations.ConfigureHeels(r.resident,legs);
            var heels=legs.Select(l=>l.End.TransformPoint(l.HeelLocal)).ToArray();var rest=bones.Select(b=>b.localPosition).ToArray();var rotations=bones.Select(b=>b.localRotation).ToArray();var scales=bones.Select(b=>b.localScale).ToArray();
            var lengths=legs.Select(l=>new[]{Vector3.Distance(l.Upper.position,l.Lower.position),Vector3.Distance(l.Lower.position,l.End.position)}).ToArray();
            var arms=new[]{"L","R"}.Select(side=>new[]{"UpperArm_","Forearm_","Hand_"}.Select(prefix=>bones.Single(b=>b.name==prefix+side)).ToArray()).ToArray();
            var armLengths=arms.Select(a=>new[]{Vector3.Distance(a[0].position,a[1].position),Vector3.Distance(a[1].position,a[2].position)}).ToArray();
            var chest=bones.Single(b=>b.name=="Spine");var origin=pelvis.position;float minX=origin.x,maxX=origin.x,drift=0,sole=999,lengthError=0,maxSpeedStep=0;var previous=head.position;float previousSpeed=0;
            float headOrigin=head.position.y,chestOrigin=chest.position.y,pelvisDrop=0,headDrop=0,chestDrop=0;
            var footRot=legs.Select(l=>l.End.rotation).ToArray();var peaks=new int[2];var raised=new bool[2];var maxAngle=new float[2];var maxLift=new float[2];
            var order=new List<int>();float worstContactTime=0,worstSoleTime=0;
            var lateral=new float[2];var maxYaw=new float[2];var maxPitch=new float[2];var peakTimes=new[]{new List<float>(),new List<float>()};
            var shoeLength=legs.Select(l=>{var pts=l.SoleLocal.Select(p=>Vector3.Dot(l.End.TransformPoint(p),r.resident.transform.forward));return pts.Max()-pts.Min();}).ToArray();
            for(int i=0;i<=(int)(clip.length*240);i++){
                float time=i/240f;r.Sample(time);float stepDrift=0;
                for(int j=0;j<2;j++){
                    float h=legs[j].End.TransformPoint(legs[j].HeelLocal).y-heels[j].y;maxLift[j]=Mathf.Max(maxLift[j],h);
                    float angle=Quaternion.Angle(legs[j].End.rotation,footRot[j]);maxAngle[j]=Mathf.Max(maxAngle[j],angle);
                    var direction=(legs[j].End.rotation*Quaternion.Inverse(footRot[j]))*r.resident.transform.forward;
                    float pitch=Mathf.Abs(Mathf.Asin(Mathf.Clamp(direction.y,-1,1))*Mathf.Rad2Deg),yaw=Mathf.Abs(Vector3.SignedAngle(r.resident.transform.forward,Vector3.ProjectOnPlane(direction,Vector3.up),Vector3.up));
                    maxYaw[j]=Mathf.Max(maxYaw[j],yaw);maxPitch[j]=Mathf.Max(maxPitch[j],pitch);
                    float measure=d.Seated?angle:pitch,threshold=d.Seated?11:9;
                    if(measure>threshold&&!raised[j]){peaks[j]++;order.Add(j);peakTimes[j].Add(time);raised[j]=true;}if(measure<1)raised[j]=false;
                    float outward=j==0?.4f:6.5f,restore=j==0?5.75f:11.85f;
                    bool air=!d.Seated&&((time>outward&&time<outward+.35f)||(time>restore&&time<restore+.35f));
                    float side=Mathf.Sign(Vector3.Dot(heels[j]-heels[1-j],r.resident.transform.right));
                    var offset=legs[j].End.TransformPoint(legs[j].HeelLocal)-heels[j];lateral[j]=Mathf.Max(lateral[j],Mathf.Abs(Vector3.Dot(offset,r.resident.transform.right)));
                    var expected=heels[j]+(!d.Seated&&time>=outward+.35f&&time<=restore?r.resident.transform.right*(side*.020f):Vector3.zero);
                    if(!d.Seated&&time>=outward+.35f&&time<=restore)expected.y+=.0015f;
                    if(!air){var actualHeel=legs[j].End.TransformPoint(legs[j].HeelLocal);stepDrift=Mathf.Max(stepDrift,Vector3.Distance(expected,actualHeel));Check(actualHeel.y>=-.0005f&&actualHeel.y<=.005f,"Planted heel height");}
                }if(stepDrift>drift)worstContactTime=time;drift=Mathf.Max(drift,stepDrift);
                float bottom=AdultRabbitSitCoat.World(shoes).Min(p=>p.y);if(bottom<sole)worstSoleTime=time;sole=Mathf.Min(sole,bottom);
                for(int j=0;j<2;j++){lengthError=Mathf.Max(lengthError,Mathf.Abs(Vector3.Distance(legs[j].Upper.position,legs[j].Lower.position)-lengths[j][0]),Mathf.Abs(Vector3.Distance(legs[j].Lower.position,legs[j].End.position)-lengths[j][1]));}
                for(int j=0;j<2;j++)for(int k=0;k<2;k++)Check(Mathf.Abs(Vector3.Distance(arms[j][k].position,arms[j][k+1].position)-armLengths[j][k])<.0001,"Arm length changed");
                float speed=(head.position.y-previous.y)*240;if(i>1)maxSpeedStep=Mathf.Max(maxSpeedStep,Mathf.Abs(speed-previousSpeed));previous=head.position;previousSpeed=speed;
                minX=Mathf.Min(minX,pelvis.position.x);maxX=Mathf.Max(maxX,pelvis.position.x);
                pelvisDrop=Mathf.Max(pelvisDrop,origin.y-pelvis.position.y);headDrop=Mathf.Max(headDrop,headOrigin-head.position.y);chestDrop=Mathf.Max(chestDrop,chestOrigin-chest.position.y);
                if(d.Seated)Check(Vector3.Distance(pelvis.position,origin)<.0001,"Seated support moved");
                Check(bones.Select((b,j)=>Vector3.Distance(b.localScale,scales[j])).Max()<.00001,"Scale changed");
                // Each knee stays on its own side of the body; no crossed legs.
                Check(Vector3.Dot(legs[0].Lower.position-legs[1].Lower.position,legs[0].Upper.position-legs[1].Upper.position)>0,"Crossed knees");
                csv.Add(FormattableString.Invariant($"{d.Index},{i/240f:F6},{pelvis.position.x:F7},{pelvis.position.y:F7},{head.position.y:F7},{speed:F6},{stepDrift:F7},{bottom:F7},{chest.position.y:F7}"));
            }
            Check(drift<=.0035&&sole>=-.0005&&heels.All(p=>p.y<=.005),$"Contact {d.Label}: drift {drift} at {worstContactTime}, sole {sole} at {worstSoleTime}");
            Check(lengthError<.0001&&maxSpeedStep<.04,$"Length/smoothness {lengthError} {maxSpeedStep}");
            r.Sample(clip.length);
            Check(bones.Select((b,j)=>Vector3.Distance(b.localPosition,rest[j])).Max()<.0001&&bones.Select((b,j)=>Quaternion.Angle(b.localRotation,rotations[j])).Max()<.1,"Endpoint mismatch "+d.Label+" "+string.Join(";",bones.Select((b,j)=>$"{b.name}:{Vector3.Distance(b.localPosition,rest[j])}/{Quaternion.Angle(b.localRotation,rotations[j])}")));
            if(!d.Seated){Check(maxX-minX>.075&&maxX-minX<.09,"Weight shift amplitude");Check(pelvisDrop<=.01f&&headDrop<=.016f&&chestDrop<=.016f,$"Upright drop pelvis/chest/head: {pelvisDrop}/{chestDrop}/{headDrop}");report.Add($"Upright max drop: pelvis {pelvisDrop*1000:F3}mm, chest {chestDrop*1000:F3}mm, head {headDrop*1000:F3}mm; pelvis <= 10mm PASS.");}
            if(d.Seated)Check(peaks.All(n=>n==3)&&maxAngle.All(a=>Mathf.Abs(a-12)<.2),"Three 12-degree ankle pulses per foot");
            if(d.Seated)Check(order.SequenceEqual(new[]{0,1,0,1,0,1}),"Alternating pulse order");
            else {
                Check(maxLift.All(h=>Mathf.Abs(h-.003f)<.001)&&maxYaw.All(a=>Mathf.Abs(a-8)<.2)&&maxPitch.All(a=>Mathf.Abs(a-10)<.2),$"Low lift / angles: {string.Join(",",maxLift)} {string.Join(",",maxYaw)} {string.Join(",",maxPitch)}");
                Check(peaks.All(n=>n==5)&&order.SequenceEqual(new[]{0,0,0,0,0,1,1,1,1,1}),"Standing five toe taps per side");
                for(int j=0;j<2;j++){Check(Mathf.Abs(lateral[j]-.020f)<.001,"Height-first 20mm offset");for(int k=1;k<5;k++)Check(Mathf.Abs(peakTimes[j][k]-peakTimes[j][k-1]-.5f)<.01,"Two taps per second");}
                r.Sample(6.1f);Check(legs.Select((l,j)=>Vector3.Distance(l.End.TransformPoint(l.HeelLocal),heels[j])).Max()<.001,"Both feet must return before support switch");
                report.Add($"Standing offsets {lateral[0]*1000:F3}/{lateral[1]*1000:F3}mm (height priority); toe 10deg x5 each at 2Hz; yaw 8deg; neutral switch PASS.");
                foreach(var sample in new[]{(time:1.2f,support:1),(time:7.3f,support:0)}){
                    r.Sample(sample.time);int s=sample.support,f=1-s;
                    Func<int,float> knee=j=>Vector3.Angle(legs[j].Upper.position-legs[j].Lower.position,legs[j].End.position-legs[j].Lower.position);
                    float loadOffset=Mathf.Abs(Vector3.Dot(pelvis.position-legs[s].End.TransformPoint(legs[s].HeelLocal),r.resident.transform.right));
                    Check(loadOffset<.14f,$"Pelvis not loaded toward support foot: {loadOffset}");
                    Check(knee(s)>knee(f)+5&&knee(s)<179,$"Support / relaxed knee contrast at {sample.time}: {knee(s)} / {knee(f)}");
                    var hand=bones.Single(b=>b.name=="Hand_"+(s==0?"L":"R"));var spine=bones.Single(b=>b.name=="Spine");
                    Check(Mathf.Abs(hand.position.y-spine.position.y)<.09,"Support hand not at waist height");
                    report.Add($"Loaded pose {sample.time:F2}s: pelvis-to-support lateral {loadOffset*1000:F2}mm, knee angles support/free {knee(s):F2}/{knee(f):F2}, waist hand height difference {(hand.position.y-spine.position.y)*1000:F2}mm (visual quality remains unapproved).");
                }
            }
            report.Add($"{d.Label}: max angles {maxAngle[0]:F3}/{maxAngle[1]:F3}deg, heel lifts {maxLift[0]*1000:F3}/{maxLift[1]*1000:F3}mm, ankle pulse counts {peaks[0]}/{peaks[1]}.");
            report.Add($"{d.Label}: heel drift {drift*1000:F4}mm; sole {sole*1000:F4}mm; leg length error {lengthError*1000:F4}mm; pelvis width {(maxX-minX)*1000:F3}mm; adjacent vertical head velocity change {maxSpeedStep:F6}m/s; endpoints PASS.");
        }
        int count=0;float recoverySole=999,recoveryDrift=0,standingDrift=0;
        foreach(var d in AdultIdleVariations.All.Where(d=>d.Index==14||d.Index==15))foreach(bool style in new[]{false,true})foreach(int phase in Enumerable.Range(0,8))foreach(bool run in new[]{false,true})foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true}){
            r.Select(0);r.SetSitStyle(style);r.StartBreathing(d.Seated);r.slow=slow;Check(r.Idle.Play(d.Label),"Play refused");Check(!r.Idle.Play(d.Label),"Repeated start accepted");r.Advance((.25f+phase*r.clips[d.Index].length/8)/(slow?.5f:1));
            var before=head.position;var heelBefore=legs.Select(l=>l.End.TransformPoint(l.HeelLocal)).ToArray();r.RequestRun(!run);r.RequestRun(run);r.Advance(0);Check(Vector3.Distance(before,head.position)<.0001,"Request snapped");
            double time=r.Idle.Time;r.paused=true;r.Advance(.2f);Check(r.Idle.Time==time,"Pause advanced");r.paused=false;
            for(int n=0;n<fps*7&&!r.MovingReview;n++){
                bool tidying=r.Idle.State==CommonIdleDirector.Stage.Tidying;r.Advance(1f/fps);
                if(tidying&&r.Idle!=null&&r.Idle.State==CommonIdleDirector.Stage.Tidying){recoverySole=Mathf.Min(recoverySole,AdultRabbitSitCoat.World(shoes).Min(p=>p.y));if(d.Seated&&r.Idle.Seated)recoveryDrift=Mathf.Max(recoveryDrift,legs.Select((l,j)=>Vector3.Distance(heelBefore[j],l.End.TransformPoint(l.HeelLocal))).Max());if(!d.Seated)for(int j=0;j<2;j++)if(r.LiftRecovery.Fixed(j,(float)r.Idle.Time)&&heelBefore[j].y<.005f)standingDrift=Mathf.Max(standingDrift,Vector3.Distance(heelBefore[j],legs[j].End.TransformPoint(legs[j].HeelLocal)));}
            }
            Check(r.MovingReview&&r.FootTransition.WantsRun==run,$"Departure stalled {d.Label}/{phase}/{fps}/{slow}");count++;
        }
        Check(recoverySole>=-.0005&&recoveryDrift<=.0035,$"Recovery contact {recoverySole} {recoveryDrift}");
        report.Add($"{count} departure combinations PASS; recovery sole {recoverySole*1000:F4}mm; seated heel drift {recoveryDrift*1000:F4}mm; standing heel drift {standingDrift*1000:F4}mm.");
        Check(standingDrift<=.0035,$"Standing recovery heel drift {standingDrift}");
        int airborneCases=0;float airSole=999,airDrift=0;
        foreach(float phase in new[]{.5f,.65f,1.2f,1.85f,2.15f,4.5f,5.85f,6.3f,6.65f,7.95f,8.25f,11.95f})foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(int mode in new[]{0,1,2}){
            r.Select(0);r.StartBreathing(false);r.slow=slow;r.AutomaticIdle=false;
            r.Idle.Play(AdultIdleVariations.All.Single(d=>d.Index==15).Label);r.Advance((.25f+phase)/(slow?.5f:1));
            var recovery=r.LiftRecovery;var before=head.position;r.RequestRun(true);r.Advance(0);Check(Vector3.Distance(before,head.position)<.0001,"Air request snapped");
            bool cancelled=false,resumed=false;float elapsed=0;
            for(int n=0;n<fps*8&&!r.MovingReview;n++){
                if(mode>0&&!cancelled&&elapsed>=.1f){r.RequestWalk(false);cancelled=true;}
                if(mode==2&&!resumed&&elapsed>=.2f){r.RequestRun(false);r.RequestRun(true);resumed=true;}
                r.Advance(1f/fps);elapsed+=(slow?.5f:1)/fps;
                if(r.Idle!=null&&r.Idle.State==CommonIdleDirector.Stage.Tidying){
                    airSole=Mathf.Min(airSole,AdultRabbitSitCoat.World(shoes).Min(p=>p.y));
                    for(int j=0;j<2;j++)if(recovery.Fixed(j,(float)r.Idle.Time))airDrift=Mathf.Max(airDrift,Vector3.Distance(recovery.GroundPoint(j),legs[j].End.TransformPoint(legs[j].HeelLocal)));
                    Check(Vector3.Dot(legs[0].Lower.position-legs[1].Lower.position,legs[0].Upper.position-legs[1].Upper.position)>0,"Recovery crossed knees");
                }
                if(mode==1&&cancelled&&r.Idle!=null&&r.Idle.State==CommonIdleDirector.Stage.Breathing)break;
            }
            if(mode==1)Check(!r.MovingReview&&r.Idle.State==CommonIdleDirector.Stage.Breathing&&recovery.Supported(),$"Air cancel did not restore {phase}/{fps}/{slow}");
            else Check(r.MovingReview&&r.FootTransition.WantsRun,$"Air departure stalled {phase}/{fps}/{slow}/{mode}");
            Check(elapsed<=.45f+1f/fps,$"Recovery too slow {elapsed}");
            airborneCases++;
        }
        Check(airSole>=-.0005&&airDrift<=.0035,$"Air recovery contact: sole {airSole}, fixed drift {airDrift}");
        report.Add($"{airborneCases} airborne lift/landing/return interruption, cancel and renewed-request cases PASS; sole {airSole*1000:F4}mm; fixed heel drift {airDrift*1000:F4}mm.");
        int seatedStops=0;float seatedStopSole=999;
        foreach(float phase in new[]{.4f,.6f,.8f,1.1f,1.3f,1.6f,1.8f,2.1f,2.3f,2.6f,2.8f,3.1f,3.3f})foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(bool style in new[]{false,true}){
            r.Select(0);r.SetSitStyle(style);r.StartBreathing(true);r.slow=slow;r.Idle.Play(AdultIdleVariations.All.Single(d=>d.Index==14).Label);r.Advance((.25f+phase)/(slow?.5f:1));r.RequestRun(false);float tidy=0;
            while(r.Idle.State==CommonIdleDirector.Stage.Tidying&&tidy<1){r.Advance(1f/fps);tidy+=(slow?.5f:1)/fps;seatedStopSole=Mathf.Min(seatedStopSole,AdultRabbitSitCoat.World(shoes).Min(p=>p.y));}
            Check(r.Idle.State==CommonIdleDirector.Stage.Rising&&tidy<=.25f+1f/fps+.001f,"Seated toe stop must enter existing stand-up in .25s");seatedStops++;
        }
        Check(seatedStopSole>=-.0005,"Seated interruption floor penetration");report.Add($"{seatedStops} seated tap interruptions PASS: tidy <= .25s + frame, then existing selected rise; sole {seatedStopSole*1000:F4}mm.");
        foreach(var d in AdultIdleVariations.All.Where(d=>d.Index==14||d.Index==15)){
            r.Select(0);r.StartBreathing(d.Seated);r.slow=false;r.Idle.Play(d.Label);r.Advance(1.2f);r.RequestRun(true);r.Advance(.1f);r.RequestWalk(false);r.Advance(3);Check(!r.MovingReview&&r.Idle.Pending==CommonIdleDirector.Departure.None,"Cancellation");
            r.StartBreathing(d.Seated);r.Idle.Play(d.Label);r.Advance(1);r.Pose(2);Check(r.selected==d.Index,"Wrong variation pose");r.paused=false;r.RequestRun(false);for(int i=0;i<240&&!r.MovingReview;i++)r.Advance(1f/30);Check(r.MovingReview,"Pose departure");
            var sequences=new List<string>();for(int repeat=0;repeat<2;repeat++){
                r.StartBreathing(d.Seated);r.AutomaticIdle=true;r.slow=false;string prior=null;var chosen=new List<string>();
                for(int cycle=0;cycle<4;cycle++){r.Advance((float)r.Idle.Wait+.001f);string id=r.Idle.CurrentId;Check(id!=prior&&r.Idle.CandidateCount==(d.Seated?3:2),"Automatic repetition/count");chosen.Add(id);prior=id;r.Advance(.25f+r.Idle.VariationDuration+.25f);Check(r.Idle.State==CommonIdleDirector.Stage.Breathing,"Automatic finish");}
                sequences.Add(string.Join("/",chosen));
            }Check(sequences[0]==sequences[1],"Seed not reproducible");r.AutomaticIdle=false;
        }
        r.Select(0);Check(r.Idle==null,"Reset");report.Add("Pose selection/departure, cancellation, pause, fixed-seed two-candidate alternation, reset PASS. Fine coat collision excluded.");
        File.WriteAllLines("Docs/AdultFidgetVerification.txt",report);File.WriteAllLines("Docs/AdultFidgetMotion.csv",csv);
    }
    public static void InterruptSequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        var capture=typeof(AdultRabbitMotionBuilder).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Camera),typeof(SkinnedMeshRenderer[]),typeof(string),typeof(string)},null);
        Directory.CreateDirectory("Logs/FidgetInterruptSequence");
        foreach(bool seated in new[]{false,true})foreach(int view in new[]{0,45,90}){
            r.Select(0);r.StartBreathing(seated);r.slow=false;r.Idle.Play(AdultIdleVariations.All.Single(d=>d.Index==(seated?14:15)).Label);r.Advance(.25f+(seated?1.1f:2f));
            for(int frame=0;frame<(seated?6:4)*30;frame++){
                if(frame>0)r.Advance(1f/30);if(frame==15)r.RequestRun(false);r.View(view);
                capture.Invoke(null,new object[]{r.reviewCamera,skins,$"{(seated?14:15)}_{view}_{frame:D3}","Logs/FidgetInterruptSequence"});
            }
        }
        r.Select(0);Debug.Log("ADULT_FIDGET_INTERRUPTION_SEQUENCE_OK");
    }
    public static void Poses(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        var capture=typeof(AdultRabbitMotionBuilder).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Camera),typeof(SkinnedMeshRenderer[]),typeof(string),typeof(string)},null);
        Directory.CreateDirectory("Logs/FidgetV0104Poses");
        foreach(float time in new[]{1.2f,7.3f})foreach(int view in new[]{0,45,90}){
            r.Select(15);r.Sample(time);r.View(view);capture.Invoke(null,new object[]{r.reviewCamera,skins,$"Stand_{time:F1}_{view}","Logs/FidgetV0104Poses"});
        }
        Debug.Log("FIDGET_POSES_OK");
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        var capture=typeof(AdultRabbitMotionBuilder).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Camera),typeof(SkinnedMeshRenderer[]),typeof(string),typeof(string)},null);
        Directory.CreateDirectory("Logs/FidgetSequence");Directory.CreateDirectory("Docs/Captures/Fidgets");
        foreach(var d in AdultIdleVariations.All.Where(d=>d.Index==14||d.Index==15))foreach(int view in new[]{0,45,90}){
            r.StartBreathing(d.Seated);r.slow=false;
            for(int frame=0;frame<(r.clips[d.Index].length+2)*30;frame++){
                if(frame>0)r.Advance(1f/30);if(frame==23)r.Idle.Play(d.Label);r.View(view);
                capture.Invoke(null,new object[]{r.reviewCamera,skins,$"{d.Index}_{view}_{frame:D3}","Logs/FidgetSequence"});
            }
            for(int pose=0;pose<8;pose++){r.Select(d.Index);r.Pose(pose);r.View(view);capture.Invoke(null,new object[]{r.reviewCamera,skins,$"{d.Index}_{view}_Pose{pose}","Docs/Captures/Fidgets"});}
        }
        r.Select(0);Debug.Log("ADULT_FIDGET_SEQUENCE_OK");
    }
}
