using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;
public static class AdultSideChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Execute(){try{Run();Debug.Log("ADULT_SIDE_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void Review(){
        try{AdultSleepChecks.Execute();AdultSleepChecks.PreservedMovementRegression();Sequence();AdultRabbitMotionBuilder.BuildPlayer();Debug.Log("ADULT_SIDE_REVIEW_OK");}
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    public static void FinalizeReview(){
        try{Run();Pillow();Continuity();Sequence();AdultRabbitMotionBuilder.BuildPlayer();Debug.Log("ADULT_SIDE_FINAL_OK");}
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    public static void Pillow(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        r.Select(28);r.paused=false;r.slow=false;
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();var head=skins.Single(s=>s.name=="Head");var top=skins.Single(s=>s.name=="Top");
        var hw=AdultRabbitSitCoat.World(head);var tw=AdultRabbitSitCoat.World(top);var triangles=head.sharedMesh.triangles;
        var forearms=top.bones.Select((b,i)=>(b,i)).Where(x=>x.b.name=="Forearm_R"||x.b.name=="Hand_R").Select(x=>x.i).ToArray();
        var weights=top.sharedMesh.boneWeights;var candidates=Enumerable.Range(0,tw.Length).Where(i=>{
            var w=weights[i];return (forearms.Contains(w.boneIndex0)?w.weight0:0)+(forearms.Contains(w.boneIndex1)?w.weight1:0)+(forearms.Contains(w.boneIndex2)?w.weight2:0)+(forearms.Contains(w.boneIndex3)?w.weight3:0)>.5f;
        }).ToArray();
        int vertex=-1,triangle=-1;float distance=99;
        foreach(int v in candidates)for(int t=0;t<triangles.Length;t+=3){
            float d=Vector3.Distance(tw[v],Closest(tw[v],hw[triangles[t]],hw[triangles[t+1]],hw[triangles[t+2]]));
            if(d<distance){distance=d;vertex=v;triangle=t;}
        }
        Check(vertex>=0,"Missing anatomical-left forearm contact patch");var start=tw[vertex];float min=99,max=0,drift=0,floor=99;
        for(int f=0;f<=960;f++){
            r.Sample(f/240.0);tw=AdultRabbitSitCoat.World(top);hw=AdultRabbitSitCoat.World(head);
            var a=hw[triangles[triangle]];var b=hw[triangles[triangle+1]];var c=hw[triangles[triangle+2]];
            var nearest=Closest(tw[vertex],a,b,c);float d=Vector3.Distance(tw[vertex],nearest);
            min=Mathf.Min(min,d);max=Mathf.Max(max,d);drift=Mathf.Max(drift,Vector3.Distance(tw[vertex],start));floor=Mathf.Min(floor,candidates.Min(v=>tw[v].y));
        }
        File.WriteAllText("Docs/AdultSidePillowRuntime.txt",$"v0.110 Unity imported mesh, 240Hz side breathing. Actual left=legacy R. Fixed forearm support vertex {vertex}, head triangle {triangle/3}.\nHead/forearm gap {min*1000:F4}..{max*1000:F4}mm; support patch drift {drift*1000:F4}mm; visible lower sleeve floor {floor*1000:F4}mm. Head floor contact is not required. Full transition signed mesh-distance audit is in AdultSidePillowVerification.json. Not OS input or visual approval.\n");
        Check(max<=.005f&&drift<=.0035f&&floor>=-.0005f,"Head/forearm support or sleeve floor");Debug.Log("SIDE_PILLOW_OK");
    }
    static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b,Vector3 c){
        var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return a;
        var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
        float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+d1/(d1-d3)*ab;
        var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
        float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+d2/(d2-d6)*ac;
        float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0)return b+(d4-d3)/((d4-d3)+(d5-d6))*(c-b);
        float denom=1/(va+vb+vc);return a+ab*(vb*denom)+ac*(vc*denom);
    }
    public static void Continuity(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        var skin=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Head");
        var joints=new[]{"Pelvis","Spine","Head"}.Select(n=>skin.bones.Single(j=>j.name==n)).ToArray();
        float step=0,acceleration=0,boundaryVelocity=0;string worst="",worstAcceleration="";var rows=new List<string>{"phase,time,clip,breath_phase,pelvis_y,chest_y,head_y"};
        for(int phase=0;phase<8;phase++){
            r.Select(22);r.paused=false;r.slow=false;r.Advance(phase*.5f);r.RequestLeftSide();
            var before=joints.Select(j=>j.position).ToArray();var velocity=new Vector3[joints.Length];int previousClip=r.selected;
            for(int f=0;f<1200;f++){
                double clock=r.RestBreathPhase;if(f==600)r.RequestSupineReturn();r.Advance(1f/240);
                Check(r.RestBreathPhase>clock,"Side breath clock stalled/reset");
                for(int i=0;i<joints.Length;i++){
                    var position=joints[i].position;var v=(position-before[i])*240;
                    step=Mathf.Max(step,Vector3.Distance(position,before[i]));
                    if(f>1){if((v-velocity[i]).magnitude*240>acceleration){acceleration=(v-velocity[i]).magnitude*240;worstAcceleration=$"phase {phase}, frame {f}, clip {r.selected}, {joints[i].name}";}if(r.selected!=previousClip&&(v-velocity[i]).magnitude>boundaryVelocity){boundaryVelocity=(v-velocity[i]).magnitude;worst=$"phase {phase}, frame {f}, {previousClip}->{r.selected}, {joints[i].name}, velocity {velocity[i]}->{v}";}}
                    before[i]=position;velocity[i]=v;
                }
                previousClip=r.selected;rows.Add(FormattableString.Invariant($"{phase},{f/240f:F6},{r.selected},{r.RestBreathPhase:F8},{joints[0].position.y:F7},{joints[1].position.y:F7},{joints[2].position.y:F7}"));
            }
        }
        File.WriteAllLines("Docs/AdultSideContinuity.csv",rows);
        File.WriteAllText("Docs/AdultSideContinuity.txt",$"8 phase continuous roll traces: max joint step {step*1000:F4}mm at 240Hz, max finite-difference acceleration {acceleration:F4}m/s2, clip-boundary velocity difference {boundaryVelocity:F6}m/s. Phase strictly advances. These are measurements, not visual approval.\n{worst}\nMax acceleration: {worstAcceleration}\n");
        Check(!float.IsNaN(acceleration)&&boundaryVelocity<.1f&&acceleration<10,"Side transition discontinuity / abrupt normalization switch");
        r.Select(28);r.SendMessage("OnDisable");Check(r.selected==0&&r.RestBreathPhase==0&&r.SidePending==AdultRabbitMotionReview.SideDestination.None,"Side disable reset");r.Select(22);r.RequestLeftSide();r.Advance(2.5f);Check(r.selected==28,"Side resume after reset");r.Select(0);
        Debug.Log("SIDE_CONTINUITY_OK");
    }
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        Check(r.clips.Length>=29,"29 stable clip slots");
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();var head=skins.Single(s=>s.name=="Head");
        var joints=head.bones;var spine=joints.Single(j=>j.name=="Spine");var headBone=joints.Single(j=>j.name=="Head");
        var pairs=new[]{"L","R"}.SelectMany(s=>new[]{("UpperArm_"+s,"Forearm_"+s),("Forearm_"+s,"Hand_"+s),("Thigh_"+s,"Shin_"+s),("Shin_"+s,"Foot_"+s)}).Select(p=>new[]{joints.Single(j=>j.name==p.Item1),joints.Single(j=>j.name==p.Item2)}).ToArray();
        r.Select(28);var lengths=pairs.Select(p=>Vector3.Distance(p[0].position,p[1].position)).ToArray();float neckLength=Vector3.Distance(spine.position,headBone.position);
        Check(joints.Single(j=>j.name=="UpperArm_R").position.y<joints.Single(j=>j.name=="UpperArm_L").position.y-.1f,"Anatomical left / legacy R must face the floor");
        var support=new[]{"Head","Pelvis","Foot_R"}.Select(n=>joints.Single(j=>j.name==n)).ToArray();var supportPositions=support.Select(j=>j.position).ToArray();
        var geometry=skins.Where(s=>new[]{"Head","BodyTorso","BodyHands","BodyLegs","BodyArms","Shoes"}.Contains(s.name)).ToArray();
        Vector3 marker=spine.InverseTransformPoint(Vector3.Lerp(spine.position,headBone.position,.49f)+Vector3.right*.17f);
        float minFloor=99,limb=0,neck=0,drift=0,low=99,high=-99,gap=0;var rows=new List<string>{"clip,time,chest_x,head_y,pelvis_y,floor_min"};
        foreach(int clip in new[]{26,27,28}){
            r.Select(clip);float duration=clip==28?4:2.4f;Check(Mathf.Abs(r.clips[clip].length-duration)<.002&&r.clips[clip].isLooping==(clip==28),"Side timing/loop");
            for(int f=0;f<=Mathf.RoundToInt(duration*240);f++){
                float time=f/240f;r.Sample(time);float floor=geometry.Min(s=>AdultRabbitSitCoat.World(s).Min(p=>p.y));minFloor=Mathf.Min(minFloor,floor);
                limb=Mathf.Max(limb,pairs.Select((p,i)=>Mathf.Abs(Vector3.Distance(p[0].position,p[1].position)-lengths[i])).Max());
                neck=Mathf.Max(neck,Mathf.Abs(Vector3.Distance(spine.position,headBone.position)-neckLength));
                if(clip==28){
                    float x=spine.TransformPoint(marker).x;low=Mathf.Min(low,x);high=Mathf.Max(high,x);
                    drift=Mathf.Max(drift,support.Select((j,i)=>Vector3.Distance(j.position,supportPositions[i])).Max());
                    gap=Mathf.Max(gap,new[]{"BodyTorso","Shoes"}.Max(n=>AdultRabbitSitCoat.World(skins.Single(s=>s.name==n)).Min(p=>p.y)));
                }
                rows.Add(FormattableString.Invariant($"{clip},{time:F6},{spine.TransformPoint(marker).x:F7},{headBone.position.y:F7},{support[1].position.y:F7},{floor:F7}"));
            }
        }
        var report=new List<string>{"v0.110 side rest: actual left=legacy R, head rests on forearm, not floor. Automated methods and skin samples, not OS input or user approval.",$"240Hz floor minimum {minFloor*1000:F4}mm; limb difference {limb*1000:F4}mm; neck difference {neck*1000:F4}mm.",$"Side resting support drift {drift*1000:F4}mm, contact gap {gap*1000:F4}mm; chest outward range {(high-low)*1000:F4}mm."};
        File.WriteAllLines("Docs/AdultSideVerification.txt",report);File.WriteAllLines("Docs/AdultSideMotion.csv",rows);
        Check(minFloor>=-.0005&&limb<=.0005&&neck<=.0005,"Side floor / lengths: "+report[1]);
        Check(drift<=.0035&&gap<=.005,"Side resting support: "+report[2]);
        Check(Mathf.Abs(high-low-.0078f)<=.0015f,"Side breathing range: "+report[2]);
        float phaseFloor=99,boundary=0;
        foreach(int phase in Enumerable.Range(0,8)){
            r.Select(22);r.slow=false;r.paused=false;r.Advance(phase*.5f);var before=joints.Select(j=>j.position).ToArray();double clock=r.RestBreathPhase;r.RequestLeftSide();
            Check(Math.Abs(clock-r.RestBreathPhase)<1e-9,"Roll request breath phase reset");boundary=Mathf.Max(boundary,joints.Select((j,i)=>Vector3.Distance(j.position,before[i])).Max());
            for(int f=0;f<1200;f++){
                if(f==600)r.RequestSupineReturn();r.Advance(1f/240);
                phaseFloor=Mathf.Min(phaseFloor,geometry.Min(s=>AdultRabbitSitCoat.World(s).Min(p=>p.y)));
            }
            Check(r.selected==22,"Roll return");
        }
        report.Add($"8 live breath phases both roll directions at 240Hz: minimum floor {phaseFloor*1000:F4}mm, entry joint difference {boundary*1000:F4}mm.");File.WriteAllLines("Docs/AdultSideVerification.txt",report);
        Check(phaseFloor>=-.0005&&boundary<=.001,"Live roll breath floor / boundary");
        int cases=0;
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(int clip in new[]{26,28})foreach(int phase in Enumerable.Range(0,8))foreach(int destination in Enumerable.Range(0,4)){
            r.Select(clip);r.paused=false;r.slow=slow;r.Advance((clip==28?4:2.4f)*phase/8/(slow?.5f:1));
            if(destination<2)r.RequestLyingReturn(destination==0);else r.RequestRun(destination==3);
            for(int f=0;f<fps*26;f++){
                r.Advance(1f/fps);
                if(destination<2?r.Idle!=null:r.MovingReview)break;
            }
            Check(destination<2?r.Idle!=null&&r.Idle.Seated==(destination==0):r.MovingReview&&r.FootTransition.WantsRun==(destination==3),$"Side destination {fps}/{slow}/{clip}/{phase}/{destination}");cases++;
        }
        r.slow=false;r.Select(22);r.RequestLeftSide();r.Advance(.4f);double elapsed=r.Elapsed;r.RequestLeftSide();Check(r.Elapsed==elapsed,"Repeat roll restart");r.RequestRun(true);r.RequestWalk(false);
        for(int i=0;i<150;i++)r.Advance(1f/60);Check(r.selected==28&&!r.MovingReview,"Cancel pending roll departure");
        r.paused=true;double held=r.RestBreathPhase;r.Advance(2);Check(r.RestBreathPhase==held,"Side breath pause");r.paused=false;r.RequestRun(false);r.RequestRun(true);for(int i=0;i<900&&!r.MovingReview;i++)r.Advance(1f/60);Check(r.MovingReview&&r.FootTransition.WantsRun,"Latest side movement");
        r.Select(24);r.RequestLeftSide();Check(r.selected==25,"Wake before roll");for(int i=0;i<300;i++)r.Advance(1f/60);Check(r.selected==28&&r.SleepEyeWeight==0,"Awake side rest");
        r.ExitLyingReview();Check(r.Idle!=null&&r.Posture==AdultRabbitMotionReview.RestPosture.Standing,"Exit side reset");
        report.Add($"{cases} phase/destination/fps/speed cases PASS. Repeat, cancellation, latest request, pause, wake-before-roll and exit PASS. Fine coat/leg checks excluded.");File.WriteAllLines("Docs/AdultSideVerification.txt",report);
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        Directory.CreateDirectory("Docs/Captures/SideRest");Directory.CreateDirectory("Logs/SideSequence");
        foreach(int clip in new[]{26,27,28})foreach(int view in new[]{0,45,90}){
            r.Select(clip);
            Action camera=()=>{float a=view*Mathf.Deg2Rad;var focus=new Vector3(0,.22f,-.72f);r.reviewCamera.transform.position=focus+new Vector3(4.2f*Mathf.Sin(a),2.6f,4.2f*Mathf.Cos(a));r.reviewCamera.transform.LookAt(focus);};
            for(int pose=0;pose<8;pose++){r.Sample(r.clips[clip].length*AdultRabbitMotionReview.LyingPosePhases[pose]);camera();AdultSleepChecks.Capture(r,$"Docs/Captures/SideRest/{clip}_{view}_Pose{pose}.png");}
            for(int f=0;f<=Mathf.RoundToInt(r.clips[clip].length*30);f++){r.Sample(f/30.0);camera();AdultSleepChecks.Capture(r,$"Logs/SideSequence/{clip}_{view}_{f:D3}.png");}
        }
        r.Select(22);r.slow=false;r.paused=false;
        for(int f=0;f<=540;f++){
            if(f==30)r.RequestLeftSide();if(f==240)r.RequestRun(false);if(f>0)r.Advance(1f/30);
            r.reviewCamera.transform.position=new Vector3(4f,2.6f,3.5f);r.reviewCamera.transform.LookAt(new Vector3(0,.6f,-.5f));AdultSleepChecks.Capture(r,$"Logs/SideSequence/Cycle_{f:D3}.png");
        }
        r.Select(0);Debug.Log("SIDE_SEQUENCE_OK");
    }
}
