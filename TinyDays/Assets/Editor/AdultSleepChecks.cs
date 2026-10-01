using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;

public static class AdultSleepChecks {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Execute(){try{Run();Debug.Log("ADULT_SLEEP_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void ContinuousReview(){try{Run();Sequence();Cycle();AdultRabbitMotionBuilder.BuildPlayer();Debug.Log("CONTINUOUS_SLEEP_REVIEW_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    public static void PreservedMovementRegression(){AdultLyingChecks.Execute();AdultCommonIdleChecks.Execute();AdultRabbitRunChecks.Execute();Debug.Log("CONTINUOUS_SLEEP_REGRESSION_OK");}
    static void Tick(AdultRabbitMotionReview r,float seconds,int fps){for(int i=0;i<Mathf.CeilToInt(seconds*fps);i++)r.Advance(1f/fps);}
    static Vector3[] World(SkinnedMeshRenderer skin,Mesh cache){
        var mesh=skin.sharedMesh;var vertices=mesh.vertices;var delta=new Vector3[vertices.Length];
        for(int shape=0;shape<mesh.blendShapeCount;shape++){
            float weight=skin.GetBlendShapeWeight(shape)/100;if(Mathf.Abs(weight)<1e-6f)continue;
            mesh.GetBlendShapeFrameVertices(shape,mesh.GetBlendShapeFrameCount(shape)-1,delta,null,null);
            for(int i=0;i<vertices.Length;i++)vertices[i]+=delta[i]*weight;
        }
        var weights=mesh.boneWeights;var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
        for(int i=0;i<vertices.Length;i++){var p=vertices[i];var w=weights[i];vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}
        return vertices;
    }
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        Check(r.clips.Length>=26&&r.sleepTiming,"Original 26 clips and shared timing");
        var joints=r.resident.GetComponentsInChildren<Transform>();var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();
        var head=skins.Single(s=>s.name=="Head");var shoes=skins.Single(s=>s.name=="Shoes");var bag=skins.Single(s=>s.name=="Backpack");
        Check(head.sharedMesh.blendShapeCount>=1,"Missing eyelid shape");
        int tris=skins.Sum(s=>s.sharedMesh.triangles.Length/3);int materials=skins.SelectMany(s=>s.sharedMaterials).Distinct().Count();Check(tris<=6000&&materials<=6,"Mesh budget");
        var supportNames=new[]{"Pelvis","Head","Foot_L","Foot_R"};var supports=supportNames.Select(n=>head.bones.Single(j=>j.name==n)).ToArray();
        var spine=head.bones.Single(j=>j.name=="Spine");var headBone=supports[1];
        var pairs=new[]{"L","R"}.SelectMany(s=>new[]{("UpperArm_"+s,"Forearm_"+s),("Forearm_"+s,"Hand_"+s),("Thigh_"+s,"Shin_"+s),("Shin_"+s,"Foot_"+s)}).Select(p=>new[]{joints.Single(j=>j.name==p.Item1),joints.Single(j=>j.name==p.Item2)}).ToArray();
        var report=new List<string>{"v0.108 continuous supine sleep: automated samples/method calls, not OS input or quality approval.",$"Geometry {tris} triangles / {materials} unique materials; optional eye blendshape imported."};
        var csv=new List<string>{"clip,time,chest_y,head_y,pelvis_y,eyes,shoe_min,head_min"};
        var mesh=new Mesh();float drift=0,limb=0,neck=0,minShoe=99,minHead=99,loopPosition=0,loopVelocity=0;
        try{
            foreach(int clip in Enumerable.Range(22,4)){
                r.Select(21);var lengths=pairs.Select(p=>Vector3.Distance(p[0].position,p[1].position)).ToArray();float neckLength=Vector3.Distance(spine.position,headBone.position);
                r.Select(clip);float duration=new[]{4f,3f,6f,1.5f}[clip-22];Check(Mathf.Abs(r.clips[clip].length-duration)<.002,"Duration "+clip);Check(r.clips[clip].isLooping==AdultRabbitMotionReview.IsSleepLoop(clip),"Loop "+clip);
                var baseline=supports.Select(s=>s.position).ToArray();
                // Mid-chest marker, defined in the posed spine frame, not world translation.
                Vector3 local=spine.InverseTransformPoint(Vector3.Lerp(spine.position,headBone.position,.49f)+Vector3.up*.17f);
                float low=99,high=-99;var chest=new List<Vector3>();
                for(int i=0;i<=Mathf.RoundToInt(duration*240);i++){
                    float t=i/240f;r.Sample(t);Vector3 chestPoint=spine.TransformPoint(local);chest.Add(chestPoint);
                    low=Mathf.Min(low,chestPoint.y);high=Mathf.Max(high,chestPoint.y);
                    drift=Mathf.Max(drift,supports.Select((s,k)=>Vector3.Distance(s.position,baseline[k])).Max());
                    limb=Mathf.Max(limb,pairs.Select((p,k)=>Mathf.Abs(Vector3.Distance(p[0].position,p[1].position)-lengths[k])).Max());
                    neck=Mathf.Max(neck,Mathf.Abs(Vector3.Distance(spine.position,headBone.position)-neckLength));
                    float sm=World(shoes,mesh).Min(p=>p.y),hm=World(head,mesh).Min(p=>p.y);minShoe=Mathf.Min(minShoe,sm);minHead=Mathf.Min(minHead,hm);
                    csv.Add(FormattableString.Invariant($"{clip},{t:F6},{chestPoint.y:F7},{headBone.position.y:F7},{supports[0].position.y:F7},{r.SleepEyeWeight:F6},{sm:F7},{hm:F7}"));
                }
                Check(r.SleepEyeWeight==(clip==23||clip==24?1:0),"Eye endpoint "+clip);
                if(AdultRabbitMotionReview.IsSleepLoop(clip)){
                    loopPosition=Mathf.Max(loopPosition,Vector3.Distance(chest.First(),chest.Last()));
                    loopVelocity=Mathf.Max(loopVelocity,((chest[1]-chest[0])-(chest[chest.Count-1]-chest[chest.Count-2])).magnitude*240);
                    float expected=clip==22?.0078f:.0104f;Check(Mathf.Abs(high-low-expected)<.001,$"Chest range {clip}: {high-low}");
                }
                report.Add($"Clip {clip}: {duration:F1}s, loop={r.clips[clip].isLooping}, chest landmark range {(high-low)*1000:F4}mm; eyes endpoint {r.SleepEyeWeight}.");
            }
        }finally{UnityEngine.Object.DestroyImmediate(mesh);}
        report.Add($"Support joint drift {drift*1000:F4}mm; limb difference {limb*1000:F4}mm; neck-pivot distance difference {neck*1000:F4}mm; shoe minimum {minShoe*1000:F4}mm; head/ears minimum {minHead*1000:F4}mm.");
        report.Add($"Loop position {loopPosition*1000:F4}mm; finite-difference seam velocity {loopVelocity:F6}m/s.");
        File.WriteAllLines("Docs/AdultSleepVerification.txt",report);File.WriteAllLines("Docs/AdultSleepMotion.csv",csv);
        Check(drift<=.0035&&limb<=.0005&&neck<=.0005,"Support / limb / neck");Check(minShoe>=-.0005&&minShoe<=.005&&minHead>=-.0005,"Floor");Check(loopPosition<.0005&&loopVelocity<.01,"Loop boundary");
        int cases=0;
        int phaseCases=0;float switchPosition=0,switchVelocity=0;
        foreach(int phase in Enumerable.Range(0,8))foreach(bool wake in new[]{false,true}){
            r.Select(wake?24:22);r.slow=false;r.paused=false;
            r.Advance((wake?6:4)*phase/8f);
            Vector3 marker=spine.InverseTransformPoint(Vector3.Lerp(spine.position,headBone.position,.49f)+Vector3.up*.17f);
            r.Advance(1f/240);Vector3 before=spine.TransformPoint(marker);r.Advance(1f/240);Vector3 at=spine.TransformPoint(marker);
            double clock=r.RestBreathPhase;if(wake)r.RequestWake();else r.RequestSleep();
            Check(Math.Abs(r.RestBreathPhase-clock)<1e-9,"Breath phase reset");
            switchPosition=Mathf.Max(switchPosition,Vector3.Distance(at,spine.TransformPoint(marker)));
            r.Advance(1f/240);Vector3 after=spine.TransformPoint(marker);
            switchVelocity=Mathf.Max(switchVelocity,((after-at)-(at-before)).magnitude*240);
            r.paused=true;clock=r.RestBreathPhase;r.Advance(2);Check(r.RestBreathPhase==clock,"Breath pause");r.paused=false;phaseCases++;
        }
        Check(switchPosition<.0005&&switchVelocity<.01,"Continuous breath switch position/velocity");
        report.Add($"{phaseCases} awake/sleep phase switch + pause cases at 240Hz PASS; immediate chest jump {switchPosition*1000:F4}mm, velocity change {switchVelocity:F6}m/s.");
        foreach(int fps in new[]{30,60,120})foreach(bool half in new[]{false,true})foreach(int clip in new[]{22,24}){
            r.Select(clip);r.slow=half;r.paused=false;float wall=(clip==22?4:6)/(half?.5f:1);
            Tick(r,wall,fps);Check(Math.Abs(r.RestBreathPhase-1)<.0001,"Breath frequency / playback speed");
        }
        report.Add("12 awake/sleep period cases at 30/60/120fps and .5/1 speed PASS; breathing clock and clip clock remain independent.");
        r.Select(22);r.slow=false;r.paused=false;
        Vector3 continuousMarker=spine.InverseTransformPoint(Vector3.Lerp(spine.position,headBone.position,.49f)+Vector3.up*.17f);
        var continuous=new List<string>{"time,clip,phase,sleep_weight,chest_y,velocity,acceleration"};
        var liveSupport=supports.Select(s=>s.position).ToArray();var liveLengths=pairs.Select(p=>Vector3.Distance(p[0].position,p[1].position)).ToArray();float liveNeck=Vector3.Distance(spine.position,headBone.position),liveDrift=0,liveLimbDifference=0,liveNeckDifference=0;
        Vector3 previous=spine.TransformPoint(continuousMarker);float previousVelocity=0,maxStep=0,maxAcceleration=0;double previousPhase=r.RestBreathPhase;
        for(int frame=1;frame<=4080;frame++){
            if(frame==480)r.RequestSleep();if(frame==2640)r.RequestWake();
            r.Advance(1f/240);Vector3 point=spine.TransformPoint(continuousMarker);float velocity=(point.y-previous.y)*240,acceleration=(velocity-previousVelocity)*240;
            Check(r.RestBreathPhase>previousPhase,"Continuous breathing stalled/reset");
            maxStep=Mathf.Max(maxStep,Mathf.Abs(point.y-previous.y));maxAcceleration=Mathf.Max(maxAcceleration,Mathf.Abs(acceleration));
            liveDrift=Mathf.Max(liveDrift,supports.Select((s,i)=>Vector3.Distance(s.position,liveSupport[i])).Max());
            liveLimbDifference=Mathf.Max(liveLimbDifference,pairs.Select((p,i)=>Mathf.Abs(Vector3.Distance(p[0].position,p[1].position)-liveLengths[i])).Max());
            liveNeckDifference=Mathf.Max(liveNeckDifference,Mathf.Abs(Vector3.Distance(spine.position,headBone.position)-liveNeck));
            continuous.Add(FormattableString.Invariant($"{frame/240f:F6},{r.selected},{r.RestBreathPhase:F9},{r.RestBreathSleepWeight:F6},{point.y:F7},{velocity:F7},{acceleration:F7}"));
            previous=point;previousPhase=r.RestBreathPhase;previousVelocity=velocity;
        }
        Check(maxStep<.0001&&maxAcceleration<2,"Continuous chest discontinuity");
        Check(liveDrift<=.0035&&liveLimbDifference<=.0005&&liveNeckDifference<=.0005,"Continuous breathing support / lengths");
        File.WriteAllLines("Docs/AdultContinuousBreathMotion.csv",continuous);
        report.Add($"17s awake/sleep/wake trace at 240Hz PASS; maximum chest step {maxStep*1000:F4}mm, finite-difference acceleration {maxAcceleration:F6}m/s². Full trace: AdultContinuousBreathMotion.csv.");
        report.Add($"Continuous phase-blended support drift {liveDrift*1000:F4}mm; limb difference {liveLimbDifference*1000:F4}mm; neck-pivot distance difference {liveNeckDifference*1000:F4}mm PASS.");
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(int clip in new[]{23,24})foreach(int phase in Enumerable.Range(0,8))foreach(int destination in Enumerable.Range(0,4)){
            r.Select(clip);r.slow=slow;r.Advance(r.clips[clip].length*phase/8/(slow?.5f:1));
            if(destination<2)r.RequestLyingReturn(destination==0);else r.RequestRun(destination==3);
            Check(r.selected==25,"Must wake before return");
            for(int i=0;i<fps*17;i++){
                r.Advance(1f/fps);if(destination>=2&&r.MovingReview)break;
            }
            Check(destination<2?r.Idle!=null&&r.Idle.Seated==(destination==0):r.MovingReview&&r.FootTransition.WantsRun==(destination==3),$"Wake destination {clip}/{phase}/{destination}/{fps}/{slow}");cases++;
        }
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(int phase in Enumerable.Range(0,8)){
            r.Select(24);r.slow=slow;r.Advance(6*phase/8f/(slow?.5f:1));r.RequestRun(false);r.RequestRun(true);r.Advance(.1f);r.RequestWalk(false);Tick(r,5,fps);
            Check(r.selected==22&&!r.MovingReview,"Cancel waking remains supine");
            r.RequestSleep();double elapsed=r.Elapsed;r.RequestSleep();Check(r.Elapsed==elapsed,"Repeated sleep restart");
            Tick(r,7,fps);Check(r.selected==24&&r.SleepEyeWeight>.999,"Fall asleep -> loop");
            r.paused=true;elapsed=r.Elapsed;float eye=r.SleepEyeWeight;r.Advance(2);Check(r.Elapsed==elapsed&&r.SleepEyeWeight==eye,"Pause");r.paused=false;
            r.RequestRun(false);r.RequestRun(true);for(int i=0;i<fps*17&&!r.MovingReview;i++)r.Advance(1f/fps);Check(r.MovingReview&&r.FootTransition.WantsRun,"Last departure");cases++;
        }
        r.Select(24);r.Sample(1);Check(r.SleepEyeWeight==1&&!bag.enabled,"Sleep eyes/bag");r.Select(0);Check(r.SleepEyeWeight==0&&bag.enabled,"Select cleanup");
        r.Select(24);r.Sample(1);r.SendMessage("OnDisable");Check(r.SleepEyeWeight==0&&bag.enabled,"Disable cleanup");r.Select(0);
        r.slow=false;r.StartBreathing(false);r.RequestLie();Tick(r,5,60);Check(r.selected==22,"Lying arrives awake, not asleep");r.Select(0);
        report.Add($"{cases} request/return/cancel/latest/pause cases at 30/60/120fps and .5/1 speed PASS; lying arrival, eye/bag reset/disable PASS.");
        report.Add("Existing 22 source actions/base geometry audited separately. Skin support sampling is not physical support-force verification. Fine coat/leg tests excluded.");File.WriteAllLines("Docs/AdultSleepVerification.txt",report);
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        Directory.CreateDirectory("Logs/SleepSequence");Directory.CreateDirectory("Docs/Captures/Sleep");
        foreach(int clip in Enumerable.Range(22,4))foreach(int view in new[]{0,45,90}){
            r.Select(clip);
            Action camera=()=>{float a=view*Mathf.Deg2Rad;r.reviewCamera.transform.position=new Vector3(3.3f*Mathf.Sin(a),2.5f,3.3f*Mathf.Cos(a));r.reviewCamera.transform.LookAt(new Vector3(0,.20f,-.35f));};
            for(int p=0;p<8;p++){r.Sample(r.clips[clip].length*AdultRabbitMotionReview.SleepPosePhases[p]);camera();Capture(r,$"Docs/Captures/Sleep/{clip}_{view}_Pose{p}.png");}
            for(int f=0;f<=Mathf.RoundToInt(r.clips[clip].length*30);f++){r.Sample(f/30.0);camera();Capture(r,$"Logs/SleepSequence/{clip}_{view}_{f:D3}.png");}
        }
        CaptureEyes(r);
        r.Select(0);Debug.Log("ADULT_SLEEP_SEQUENCE_OK");
    }
    public static void Face(){AdultRabbitMotionBuilder.BuildOnly();CaptureEyes(UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>());Debug.Log("SLEEP_FACE_OK");}
    public static void Cycle(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;r.slow=false;r.Select(22);
        Directory.CreateDirectory("Logs/SleepCycle");
        for(int f=0;f<=600;f++){
            if(f==30)r.RequestSleep();if(f==330)r.RequestWake();if(f==420)r.RequestLyingReturn(false);if(f>0)r.Advance(1f/30);
            r.reviewCamera.transform.position=new Vector3(3.5f,2.6f,3.5f);r.reviewCamera.transform.LookAt(new Vector3(0,.75f,-.25f));Capture(r,$"Logs/SleepCycle/{f:D3}.png");
        }
        r.Select(0);Debug.Log("SLEEP_CYCLE_OK");
    }
    static void CaptureEyes(AdultRabbitMotionReview r){
        Directory.CreateDirectory("Docs/Captures/Sleep");
        foreach(float time in new[]{0f,1.5f,3f}){r.Select(23);r.Sample(time);var h=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Head").bones.Single(j=>j.name=="Head");var focus=h.position+new Vector3(0,.18f,-.30f);r.reviewCamera.transform.position=focus+new Vector3(.25f,1.8f,.65f);r.reviewCamera.transform.LookAt(focus);Capture(r,$"Docs/Captures/Sleep/Eyes{time:F1}.png");}
        r.Select(0);
    }
    public static void Capture(AdultRabbitMotionReview r,string path){
        var camera=r.reviewCamera;var old=camera.targetTexture;var oldActive=RenderTexture.active;var rt=new RenderTexture(640,640,24){antiAliasing=4};var objects=new List<GameObject>();var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();Texture2D texture=null;
        try{
            foreach(var skin in skins){
                var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.vertices=World(skin,mesh);
                var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var normals=mesh.normals;var weights=mesh.boneWeights;
                for(int i=0;i<normals.Length;i++){var n=normals[i];var w=weights[i];normals[i]=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;}
                mesh.normals=normals;mesh.RecalculateBounds();var go=new GameObject("__SleepCapture");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;objects.Add(go);skin.enabled=false;
            }
            rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture=new Texture2D(640,640,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,640,640),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
        }finally{camera.targetTexture=old;RenderTexture.active=oldActive;if(texture)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);foreach(var go in objects){UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(go);}foreach(var s in skins)s.enabled=true;}
    }
}
