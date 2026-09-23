using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using TinyDays.Review;
using UnityEditor;
using UnityEngine;

// Direct-call tests and render captures; not evidence of real player input.
public static class AdultRabbitRunChecks
{
    static readonly BindingFlags Private=BindingFlags.Static|BindingFlags.NonPublic;
    static readonly List<string> report=new List<string>(),failures=new List<string>();
    static Vector3[] Vertices(SkinnedMeshRenderer skin)=>(Vector3[])typeof(AdultRabbitMotionBuilder).GetMethod("WorldVertices",Private).Invoke(null,new object[]{skin});
    static void Check(bool ok,string message){if(!ok)failures.Add(message);}
    static void Capture(AdultRabbitMotionReview r,string name,string directory="Docs/Captures/AdultRabbitRun"){
        Directory.CreateDirectory(directory);
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        typeof(AdultRabbitMotionBuilder).GetMethod("Capture",Private).Invoke(null,new object[]{r.reviewCamera,skins,name,directory});
    }
    public static void Execute(){
        try{Run();}catch(Exception e){failures.Add(e.ToString());}
        report.AddRange(failures.Select(s=>"FAIL: "+s));report.Add(failures.Count==0?"PASS: direct calls and captures only; user approval pending":"INCOMPLETE");
        File.WriteAllLines("Docs/AdultRabbitRunVerification.txt",report);
        if(failures.Count>0){Debug.LogError(string.Join("\n",failures));if(Application.isBatchMode)EditorApplication.Exit(1);}
        else Debug.Log("ADULT_RABBIT_RUN_OK");
    }
    public static void InspectRunCoat(){
        typeof(AdultRabbitMotionBuilder).GetMethod("Build",Private).Invoke(null,null);var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();var rows=new List<string>();
        for(int i=0;i<8;i++){r.Select(4);r.Pose(i);rows.Add($"{AdultRunTiming.Label(i)} coat {r.CoatDisplacement:F6}m");r.reviewCamera.transform.position=new Vector3(4.8f,1.25f,0);r.reviewCamera.transform.LookAt(new Vector3(0,.98f,0));Capture(r,"RearCoat"+i);}
        r.BeginMovement();r.slow=false;r.RequestRun(true);r.Advance(1.2f);float worst=0,phase=0;
        for(int i=0;i<Mathf.RoundToInt(AdultRunTiming.Data.duration*240);i++){
            r.Advance(1f/240);if(r.CoatDisplacement>worst){worst=r.CoatDisplacement;phase=(float)(r.FootTransition.WalkTime/.8%1);}
        }
        rows.Add($"Runtime coat maximum {worst:F6}m at phase {phase:F6}");
        File.WriteAllLines("Logs/run-rear-coat-inspection.txt",rows);r.Select(0);
    }
    public static void MeasureRhythm(){
        typeof(AdultRabbitMotionBuilder).GetMethod("Build",Private).Invoke(null,null);
        var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        var bones=r.resident.GetComponentsInChildren<Transform>();
        var pelvis=bones.Single(b=>b.name=="Pelvis");var head=bones.Single(b=>b.name=="Head"&&!b.GetComponent<Renderer>());
        var rows=new List<string>{"mode,time,pelvis,head"};var summary=new List<string>();
        foreach(bool moving in new[]{false,true}){
            if(moving){r.BeginMovement();r.slow=false;r.RequestRun(true);r.Advance(1.2f);}else r.Select(4);
            float minP=100,maxP=-100,minH=100,maxH=-100,maxVP=0,maxVH=0,prevP=0,prevH=0;
            int count=Mathf.RoundToInt(AdultRunTiming.Data.duration*240);
            for(int i=0;i<=count;i++){
                if(moving){if(i>0)r.Advance(1f/240);}else r.Sample(i/240.0);
                float p=pelvis.position.y,h=head.position.y;minP=Mathf.Min(minP,p);maxP=Mathf.Max(maxP,p);minH=Mathf.Min(minH,h);maxH=Mathf.Max(maxH,h);
                if(i>0){maxVP=Mathf.Max(maxVP,Mathf.Abs(p-prevP)*240);maxVH=Mathf.Max(maxVH,Mathf.Abs(h-prevH)*240);}prevP=p;prevH=h;
                rows.Add(FormattableString.Invariant($"{(moving?"moving":"authored")},{i/240f:F6},{p:F6},{h:F6}"));
            }
            summary.Add(FormattableString.Invariant($"{(moving?"moving":"authored")}: duration {AdultRunTiming.Data.duration:F6}s; pelvis range {maxP-minP:F6}m/max speed {maxVP:F6}m/s; head range {maxH-minH:F6}m/max speed {maxVH:F6}m/s"));
        }
        string stem="Docs/AdultRabbitRunRhythm"+Mathf.RoundToInt(AdultRunTiming.Data.duration*30);
        File.WriteAllLines(stem+".csv",rows);File.WriteAllLines(stem+".txt",summary);r.Select(0);Debug.Log("RUN_RHYTHM_OK");
    }
    static void Run(){
        typeof(AdultRabbitMotionBuilder).GetMethod("Build",Private).Invoke(null,null);
        var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        var bones=r.resident.GetComponentsInChildren<Transform>();
        var feet=new[]{bones.Single(b=>b.name=="Foot_L"),bones.Single(b=>b.name=="Foot_R")};
        var knees=new[]{bones.Single(b=>b.name=="Shin_L"),bones.Single(b=>b.name=="Shin_R")};
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();var shoes=skins.Single(s=>s.name=="Shoes");
        var top=skins.Single(s=>s.name=="Top");var bottom=skins.Single(s=>s.name=="Bottom");
        var original=top.sharedMesh;
        var hips=new[]{bones.Single(b=>b.name=="Thigh_L"),bones.Single(b=>b.name=="Thigh_R")};
        r.Select(0);var restUpper=Enumerable.Range(0,2).Select(k=>Vector3.Distance(hips[k].position,knees[k].position)).ToArray();
        var restLower=Enumerable.Range(0,2).Select(k=>Vector3.Distance(knees[k].position,feet[k].position)).ToArray();
        float segmentError=0;var pelvis=bones.Single(b=>b.name=="Pelvis");
        r.Select(4);
        int poseSamples=Mathf.RoundToInt(AdultRunTiming.Data.duration*30)*4;
        Check(Mathf.Abs(r.clips[4].length-AdultRunTiming.Data.duration)<.0001f,"Clip/shared duration mismatch");
        for(int sample=0;sample<=poseSamples;sample++){
            r.Sample(r.clips[4].length*sample/(double)poseSamples);
            for(int k=0;k<2;k++)segmentError=Mathf.Max(segmentError,Mathf.Abs(Vector3.Distance(hips[k].position,knees[k].position)-restUpper[k]),Mathf.Abs(Vector3.Distance(knees[k].position,feet[k].position)-restLower[k]));
        }
        r.Pose(0);float contactHeight=pelvis.position.y;r.Pose(1);float passingHeight=pelvis.position.y;r.Pose(3);float upHeight=pelvis.position.y;
        report.Add($"Authored {poseSamples+1} poses: maximum segment-length error {segmentError:F6}m; pelvis Contact {contactHeight:F4}, Passing {passingHeight:F4}, Up {upHeight:F4}m.");
        // v0.87 validates the full periodic body curve in SmoothnessChecks, not
        // three body heights attached to foot-pose names (which caused re-dips).
        Check(segmentError<.002f,"Authored leg stretching");
        r.Select(0);r.paused=false;
        var shoeIndices=feet.Select(f=>{int bi=Array.IndexOf(shoes.bones,f);return Enumerable.Range(0,shoes.sharedMesh.vertexCount).Where(i=>shoes.sharedMesh.boneWeights[i].boneIndex0==bi&&shoes.sharedMesh.boneWeights[i].weight0>.99f).ToArray();}).ToArray();
        r.Select(0);var rest=Vertices(top);var triangles=top.sharedMesh.triangles;
        var front=Enumerable.Range(0,triangles.Length/3).Where(t=>{var p=(rest[triangles[t*3]]+rest[triangles[t*3+1]]+rest[triangles[t*3+2]])/3;return p.y<.83f&&p.y>.395f&&p.z>.035f&&Mathf.Abs(p.x)<.34f;}).ToArray();
        var ray=(Func<Vector3,Vector3,Vector3,Vector3,Vector3,float>)Delegate.CreateDelegate(typeof(Func<Vector3,Vector3,Vector3,Vector3,Vector3,float>),typeof(AdultRabbitMotionBuilder).GetMethod("RayTriangle",Private));
        float minSole=float.MaxValue,maxSlip=0,maxGap=0,maxCorrection=0,maxPenetration=0,minKneeGap=float.MaxValue;int samples=0,flight=0,coatSamples=0;string correctionContext="none";
        Action measure=()=>{
            samples++;var vs=Vertices(shoes);minSole=Mathf.Min(minSole,vs.Min(v=>v.y));
            minKneeGap=Mathf.Min(minKneeGap,Mathf.Abs(r.resident.transform.InverseTransformPoint(knees[0].position).x-r.resident.transform.InverseTransformPoint(knees[1].position).x));
            if(r.CoatDisplacement>maxCorrection){maxCorrection=r.CoatDisplacement;correctionContext=$"{r.FootTransition.State}, phase {r.FootTransition.WalkTime/.8%1:F4}, run blend {r.FootTransition.RunWeight:F3}";}
            if(!r.FootTransition.LeftPlanted&&!r.FootTransition.RightPlanted&&shoeIndices.All(ids=>ids.Min(i=>vs[i].y)>.005f))flight++;
            if(samples%4!=0)return;coatSamples++;var coat=Vertices(top);
            foreach(var p in Vertices(bottom)){
                float nearest=float.MaxValue;foreach(int t in front){float d=ray(p+Vector3.forward*2,Vector3.back,coat[triangles[3*t]],coat[triangles[3*t+1]],coat[triangles[3*t+2]]);if(d>=0)nearest=Mathf.Min(nearest,d);}
                if(nearest<float.MaxValue)maxPenetration=Mathf.Max(maxPenetration,nearest-2);
            }
        };
        var distances=new List<float>();
        foreach(int fps in new[]{30,60,120})foreach(bool half in new[]{false,true})for(int phase=0;phase<8;phase++){
            r.BeginMovement();r.slow=half;r.RequestRun(true);float dt=1f/fps,mult=half?.5f:1;
            int startFrames=(int)Math.Round((1.25f+phase*AdultRunTiming.Data.duration/8)/dt/mult);
            for(int i=0;i<startFrames;i++){r.Advance(dt);measure();}
            r.RequestWalk(false);
            for(int i=0;i<Mathf.CeilToInt(1.5f/dt/mult);i++){
                var previous=Enumerable.Range(0,2).Select(k=>r.FootTransition.ContactPosition(k)).ToArray();var contactIds=Enumerable.Range(0,2).Select(k=>r.FootTransition.ContactIndex(k)).ToArray();var planted=new[]{r.FootTransition.LeftPlanted,r.FootTransition.RightPlanted};
                r.Advance(dt);measure();
                for(int k=0;k<2;k++)if(planted[k]&&(k==0?r.FootTransition.LeftPlanted:r.FootTransition.RightPlanted)&&contactIds[k]==r.FootTransition.ContactIndex(k))maxSlip=Mathf.Max(maxSlip,Vector3.Distance(previous[k],r.FootTransition.ContactPosition(k)));
            }
            Check(r.FootTransition.State==AdultRabbitFootTransition.Stage.Idle,$"Stop incomplete {fps}/{half}/{phase}: {r.FootTransition.State}");
            maxGap=Mathf.Max(maxGap,r.FootTransition.FootForwardGap);
            Check(r.FootTransition.BothFeetGrounded,$"Stop contact {fps}/{half}/{phase}");distances.Add(r.Travel);
        }
        float distanceSpread=Enumerable.Range(0,8).Max(p=>Enumerable.Range(0,6).Max(g=>distances[g*8+p])-Enumerable.Range(0,6).Min(g=>distances[g*8+p]));
        Check(distanceSpread<.08f,"Frame rate distance divergence beyond one 30fps frame");
        report.Add($"48 stop cases, {samples} samples: min sole {minSole:F6}m; support slip {maxSlip:F6}m; final forward gap {maxGap:F6}m; min knee spacing {minKneeGap:F6}m; airborne samples {flight}; frame-rate distance spread {distanceSpread:F6}m (frame-rounded request times).");
        report.Add($"Coat front probes {coatSamples}: penetration {maxPenetration:F6}m, runtime correction {maxCorrection:F6}m at {correctionContext}. Sampled front visibility, not complete intersection proof.");
        Check(minSole>=-.0005f,"Floor penetration");Check(maxSlip<=.0035f,"Support slip");Check(maxGap<=.01f,"Feet not aligned");Check(minKneeGap>.12f,"Knees cross/collapse");Check(flight>0,"No flight in run");Check(maxPenetration<=.001f,"Run coat penetration");Check(maxCorrection<=.065f,"Coat correction over 6.5cm");
        foreach(bool half in new[]{false,true}){
            r.BeginMovement();r.slow=half;r.RequestRun(true);r.Advance(half?2.6f:1.3f);
            float distanceStart=r.Travel;double phaseStart=r.FootTransition.WalkTime;
            r.Advance(1);float elapsed=half?.5f:1;
            Check(Mathf.Abs(r.Travel-distanceStart-AdultRunTiming.Data.speed*elapsed)<.01f,"Run real-time speed");
            Check(Math.Abs((r.FootTransition.WalkTime-phaseStart)/.8-elapsed/AdultRunTiming.Data.duration)<.002,"Run real-time cadence");
        }
        r.BeginMovement();r.slow=false;r.RequestRun(true);r.Advance(1.3f);float start=r.Travel;
        r.paused=true;start=r.Travel;r.Advance(.5f);Check(r.Travel==start,"Paused drift");r.paused=false;
        r.RequestRun(false);r.Advance(.5f);Check(r.FootTransition.RunWeight==0&&Mathf.Abs(r.Speed-1.15f)<.01f,"Run to walk");
        foreach(float stopAt in new[]{.1f,.4f,1.1f})foreach(float delay in new[]{.05f,.3f,.6f}){
            r.BeginMovement();r.RequestRun(true);r.Advance(stopAt);r.RequestWalk(false);r.Advance(delay);r.RequestRun(true);r.Advance(.7f);r.RequestWalk(false);r.Advance(1.5f);
            Check(r.FootTransition.State==AdultRabbitFootTransition.Stage.Idle&&r.FootTransition.BothFeetGrounded,"Restart stop");
        }
        r.BeginMovement();r.RequestRun(true);r.Advance(8);Check(r.FootTransition.State==AdultRabbitFootTransition.Stage.Idle&&r.Travel<6.1f,"Automatic run stop");
        r.Select(0);Check(top.sharedMesh==original,"Coat restore on reset");
        report.Add($"Speed {AdultRunTiming.Data.speed}m/s, {AdultRunTiming.Data.duration:F6}s cycle at 0.5x/1x, pause, run-to-walk, nine interrupted/restarted stops, automatic stop and coat cleanup checked.");
        // Inspect the authored 8 poses with the same phase table used by the buttons.
        for(int phase=0;phase<8;phase++){
            r.Select(4);r.Pose(phase);
            foreach(var view in new[]{("Front",new Vector3(0,1.25f,4.8f)),("Side",new Vector3(4.8f,1.25f,0))}){
                r.reviewCamera.transform.position=view.Item2;r.reviewCamera.transform.LookAt(new Vector3(0,.98f,0));
                Capture(r,$"Pose{phase}_{AdultRunTiming.Label(phase).Replace(' ','_')}_{view.Item1}");
            }
        }
        r.BeginMovement();r.slow=false;r.RequestRun(true);r.Advance(1.2f);
        float minKneeAngle=180,maxRoll=0;var flatRot=feet.Select(f=>f.rotation).ToArray();
        for(int i=0;i<Mathf.RoundToInt(AdultRunTiming.Data.duration*240);i++){
            r.Advance(1f/240);for(int k=0;k<2;k++){
                var hip=bones.Single(b=>b.name==(k==0?"Thigh_L":"Thigh_R"));
                minKneeAngle=Mathf.Min(minKneeAngle,Vector3.Angle(hip.position-knees[k].position,feet[k].position-knees[k].position));
                maxRoll=Mathf.Max(maxRoll,Quaternion.Angle(flatRot[k],feet[k].rotation));
            }
        }
        report.Add($"Runtime minimum knee interior angle {minKneeAngle:F2} degrees; foot rotation excursion {maxRoll:F2} degrees; contact slip tracks the same sole vertex, not ankle translation.");
        Check(minKneeAngle<110,"Recovery knee not folded");Check(maxRoll>25,"Runtime foot roll suppressed");
        // Continuous start, cruise, stop, closing, settled render sequence.
        r.BeginMovement();r.slow=false;r.RequestRun(true);
        for(int frame=0;frame<108;frame++){
            if(frame==60)r.RequestWalk(false);if(frame>0)r.Advance(1f/30);
            foreach(var view in new[]{("Front",new Vector3(0,1.25f,4.8f)),("Side",new Vector3(4.8f,1.25f,0))}){
                var center=r.resident.transform.position;r.reviewCamera.transform.position=center+view.Item2;r.reviewCamera.transform.LookAt(center+new Vector3(0,.98f,0));
                Capture(r,$"{view.Item1}{frame:D3}","Logs/AdultRabbitRunSequence");
                if(new[]{12,36,48,63,72,96}.Contains(frame))Capture(r,$"Run{frame:D3}{view.Item1}");
            }
        }
        r.Select(0);r.Home();
    }
}
