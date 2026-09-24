using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using TinyDays.Review;

public static class AdultCommonIdleChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Vector3[] Points(AdultRabbitMotionReview r)=>r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).SelectMany(AdultRabbitSitCoat.World).ToArray();
    static float Delta(Vector3[] a,Vector3[] b)=>a.Zip(b,Vector3.Distance).Max();
    public static void Execute(){try{Run();Debug.Log("COMMON_IDLE_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void FinalizePlayer(){Execute();Sequence();AdultRabbitMotionBuilder.BuildPlayer();}
    public static void InspectEntry(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        r.StartBreathing(false);var bones=r.resident.GetComponentsInChildren<Transform>();var feet=bones.Where(b=>b.name=="Foot_L"||b.name=="Foot_R").ToArray();var start=feet.Select(b=>b.position).ToArray();
        r.RequestRun(false);for(int i=0;i<=6;i++){if(i>0)r.Advance(.04f);Debug.Log($"ENTRY {i*.04f:F2}: "+string.Join(" / ",feet.Select((f,j)=>f.name+" "+f.position.ToString("F6")+" delta "+(f.position-start[j]).ToString("F6"))));}
    }
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        var report=new List<string>{"v0.93 common idle: automatic function/clip checks; NOT real player input or visual approval."};
        var bones=r.resident.GetComponentsInChildren<Transform>();var spine=bones.First(b=>b.name=="Spine");var head=bones.First(b=>b.name=="Head");
        var fixedBones=new[]{"Pelvis","Foot_L","Foot_R"}.Select(n=>bones.First(b=>b.name==n)).ToArray();
        foreach(bool seated in new[]{false,true}){
            int clip=seated?9:8;Check(Mathf.Abs(r.clips[clip].length-4)<1e-4&&r.clips[clip].isLooping,"Breathing duration/loop");
            r.Select(clip);r.Sample(0);var first=Points(r);var scales=bones.Select(b=>b.localScale).ToArray();var supports=fixedBones.Select(b=>b.position).ToArray();float low=999,high=-999,drift=0,neck=Vector3.Distance(head.position,head.parent.position),neckDelta=0;
            var shoes=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Shoes");var shoePoints=AdultRabbitSitCoat.World(shoes);var weights=shoes.sharedMesh.boneWeights;
            foreach(var foot in fixedBones.Skip(1)){int index=Array.IndexOf(shoes.bones,foot);float sole=Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==index&&weights[i].weight0>.99f).Min(i=>shoePoints[i].y);Check(sole>=-.0005&&sole<=.005,$"{clip} {foot.name} sole {sole*1000:F3}mm");report.Add($"{clip} {foot.name} sole {sole*1000:F3}mm.");}
            for(int i=0;i<=960;i++){
                r.Sample(i/240.0);low=Mathf.Min(low,spine.position.y);high=Mathf.Max(high,spine.position.y);
                drift=Mathf.Max(drift,Delta(supports,fixedBones.Select(b=>b.position).ToArray()));neckDelta=Mathf.Max(neckDelta,Mathf.Abs(neck-Vector3.Distance(head.position,head.parent.position)));
                Check(Delta(scales,bones.Select(b=>b.localScale).ToArray())<1e-5,"Animated body scale");
            }
            float seam=Delta(first,Points(r));Check(seam<.0001&&drift<.0001&&neckDelta<.0001,"Loop/support/neck changed");
            Check(high-low>.004&&high-low<.009,$"Breathing amplitude {high-low}");
            report.Add($"{(seated?"Sit":"Stand")}: chest range {(high-low)*1000:F3}mm; support drift {drift*1000:F4}mm; neck change {neckDelta*1000:F4}mm; seam {seam*1000:F4}mm.");
        }
        int count=0;float maxTeleport=0;
        foreach(bool seated in new[]{false,true})foreach(bool run in new[]{false,true})foreach(int phase in Enumerable.Range(0,8))foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true}){
            r.Select(0);r.resident.transform.position=new Vector3(2,0,-3);r.StartBreathing(seated);r.slow=slow;r.Advance(phase*.5f/(slow?.5f:1));
            var origin=r.resident.transform.position;r.RequestRun(!run);r.RequestRun(run);r.RequestRun(run);
            float time=0;while(!r.MovingReview&&time<6){r.Advance(1f/fps);time+=1f/fps;maxTeleport=Mathf.Max(maxTeleport,Vector3.Distance(origin,r.resident.transform.position));}
            Check(r.MovingReview&&r.FootTransition.WantsRun==run,"Latest departure failed");Check(Vector3.Distance(origin,r.resident.transform.position)<.001,"Actor teleported at launch");
            float expected=(seated?.5f+r.clips[6].length:.25f)/(slow?.5f:1);Check(Mathf.Abs(time-expected)<=1.01f/fps+.0001f,"Departure timing changed with frame rate");
            for(int i=0;i<fps;i++)r.Advance(1f/fps);Check(r.Travel>0,"No travel");count++;
        }
        report.Add($"{count} sit/stand × 8 phase × walk/run × fps/speed departure combinations PASS; launch root delta {maxTeleport*1000:F4}mm.");
        foreach(bool seated in new[]{false,true}){
            r.StartBreathing(seated);r.slow=false;r.Advance(.8f);r.RequestRun(true);r.Advance(.1f);var before=Points(r);var clock=r.Idle.Time;
            r.paused=true;r.Advance(1);Check(Delta(before,Points(r))<1e-5&&r.Idle.Time==clock,"Pause changed pose");r.paused=false;
            r.RequestWalk(false);r.Advance(3);Check(!r.MovingReview&&r.Idle.Pending==CommonIdleDirector.Departure.None,"Cancelled launch ran");
        }
        r.StartBreathing(true);r.RequestRun(true);r.Advance(.5f);Check(r.Idle.State==CommonIdleDirector.Stage.Rising,"Not rising");r.RequestWalk(false);r.Advance(2);Check(!r.MovingReview&&!r.Idle.Seated,"Cancelled rise not safely finished");
        r.StartBreathing(false);r.Advance(30);Check(r.Idle.State==CommonIdleDirector.Stage.Breathing&&r.Idle.CandidateCount==0,"Unfinished variant selected");
        r.RequestRun(true);r.Select(0);Check(r.Idle==null&&!r.MovingReview,"Reset retained queue");
        report.Add("Pause, repeated requests, cancel during tidy/rise, empty automatic roster, clip reset PASS.");
        // Completed synthetic test clips exercise selection, not shipped actions.
        string Schedule(int seed){
            var director=new CommonIdleDirector(r.resident,r.clips[8],r.clips[9],r.clips[6],r.clips[0],r.clips[7],false,seed);
            director.Register(new CommonIdleDirector.Variation{Id="Test A",Clip=r.clips[8]});director.Register(new CommonIdleDirector.Variation{Id="Test B",Clip=r.clips[8]});
            var ids=new List<string>();string last="";
            for(int i=0;i<2000;i++){director.Advance(.1f);if(director.State==CommonIdleDirector.Stage.Variation&&director.CurrentId!=last){last=director.CurrentId;ids.Add(last);}if(director.State==CommonIdleDirector.Stage.Breathing)Check(director.Wait<=12.0001,"Wait above 12 seconds");}
            Check(ids.Count>=10&&ids.Zip(ids.Skip(1),(a,b)=>a!=b).All(x=>x),"Repeated variant");return string.Join(",",ids);
        }
        Check(Schedule(173)==Schedule(173),"Seed not reproducible");report.Add("Synthetic-only two-clip scheduler: seeded sequence/repeat exclusion PASS. No extra variants shipped.");
        float minSole=999,maxTidyDrift=0,maxFrameJump=0;
        foreach(bool seated in new[]{false,true})foreach(int phase in Enumerable.Range(0,8)){
            r.StartBreathing(seated);r.slow=false;r.Advance(phase*.5f);var feet=fixedBones.Skip(1).Select(b=>b.position).ToArray();r.RequestRun(false);
            var shoe=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Shoes");var prior=Points(r);
            for(int i=0;i<600&&!r.MovingReview;i++){
                r.Advance(1f/240);var pts=Points(r);maxFrameJump=Mathf.Max(maxFrameJump,Delta(prior,pts));prior=pts;
                minSole=Mathf.Min(minSole,AdultRabbitSitCoat.World(shoe).Min(p=>p.y));
                if(r.Idle!=null&&r.Idle.State==CommonIdleDirector.Stage.Tidying&&r.Idle.Seated==seated&&i<59)maxTidyDrift=Mathf.Max(maxTidyDrift,Delta(feet,fixedBones.Skip(1).Select(b=>b.position).ToArray()));
            }
        }
        report.Add($"Departure diagnostics: minimum sole {minSole*1000:F3}mm; first tidy support drift {maxTidyDrift*1000:F3}mm; largest 240Hz vertex step {maxFrameJump*1000:F3}mm.");
        Check(minSole>=-.0005&&maxTidyDrift<=.0035,$"Departure contact failure: sole {minSole*1000:F3}mm, drift {maxTidyDrift*1000:F3}mm, vertex step {maxFrameJump*1000:F3}mm");
        r.StartBreathing(true);r.Pose(3);r.RequestRun(false);Check(r.Idle!=null&&r.Idle.Seated,"Scrubbed seated pose bypassed rise");
        typeof(AdultRabbitMotionReview).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,null);Check(r.Idle==null&&!r.MovingReview,"Disable retained queue");r.StartBreathing(false);r.Advance(.1f);Check(r.Idle!=null,"Playback did not resume after reset");
        report.Add("Scrubbed pose departure, disable cleanup and explicit restart PASS (not real Play-mode lifecycle input).");
        // Optional secondary joints absent must not block body playback.
        r.idleProfile.Parts=Array.Empty<IdleSecondaryMotion.Part>();r.StartBreathing(true);r.Advance(.7f);Check(r.Idle!=null,"Missing appendages blocked body");
        r.Select(0);File.WriteAllLines("Docs/AdultCommonIdleVerification.txt",report);
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        var capture=typeof(AdultRabbitMotionBuilder).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Camera),typeof(SkinnedMeshRenderer[]),typeof(string),typeof(string)},null);
        Directory.CreateDirectory("Logs/CommonIdleSequence");
        foreach(bool seated in new[]{false,true}){
            r.StartBreathing(seated);r.slow=false;
            for(int frame=0;frame<240;frame++){
                if(frame==120)r.RequestRun(!seated);if(frame>0)r.Advance(1f/30);
                var center=r.resident.transform.position;r.reviewCamera.transform.position=center+new Vector3(3,1.6f,4.5f);r.reviewCamera.transform.LookAt(center+new Vector3(0,1,0));
                capture.Invoke(null,new object[]{r.reviewCamera,skins,(seated?"Sit":"Stand")+frame.ToString("D3"),"Logs/CommonIdleSequence"});
            }
        }
        r.Select(0);Debug.Log("COMMON_IDLE_SEQUENCE_OK");
    }
}
