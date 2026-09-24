using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;
public static class AdultRabbitSitChecks {
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Static;
    static Vector3[] World(SkinnedMeshRenderer skin)=>AdultRabbitSitCoat.World(skin);
    static void Capture(AdultRabbitMotionReview r,string name,string path="Docs/Captures/AdultRabbitSit"){
        Directory.CreateDirectory(path);typeof(AdultRabbitMotionBuilder).GetMethod("Capture",Private).Invoke(null,new object[]{r.reviewCamera,r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray(),name,path});
    }
    public static void Inspect(){Run(false);}
    public static void Execute(){Run(true);}
    static void Run(bool strict){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        var bones=r.resident.GetComponentsInChildren<Transform>();var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();
        var shoes=skins.Single(s=>s.name=="Shoes");var hands=skins.Single(s=>s.name=="BodyHands");var top=skins.Single(s=>s.name=="Top");var pack=skins.Single(s=>s.name=="Backpack");
        var errors=new List<string>();var report=new List<string>();var rows=new List<string>{"clip,time,pelvis,head,shoeMin,handMin,coatMin,packMin,spineAngle,headAngle,leftHandX,leftHandY,leftHandZ,rightHandX,rightHandY,rightHandZ"};
        Transform spine=bones.First(b=>b.name=="Spine"),head=bones.First(b=>b.name=="Head"&&!b.GetComponent<Renderer>()),leftHand=bones.First(b=>b.name=="Hand_L"),rightHand=bones.First(b=>b.name=="Hand_R");
        r.Select(0);var pairs=new[]{("Thigh_L","Shin_L"),("Shin_L","Foot_L"),("UpperArm_L","Forearm_L"),("Forearm_L","Hand_L"),("Thigh_R","Shin_R"),("Shin_R","Foot_R"),("UpperArm_R","Forearm_R"),("Forearm_R","Hand_R")}.Select(p=>(bones.First(b=>b.name==p.Item1),bones.First(b=>b.name==p.Item2))).ToArray();
        var lengths=pairs.Select(p=>Vector3.Distance(p.Item1.position,p.Item2.position)).ToArray();var original=top.sharedMesh;var originalPack=pack.sharedMesh;
        Func<float> kneeDelta=()=>r.resident.transform.InverseTransformPoint(bones.First(b=>b.name=="Shin_L").position).x-r.resident.transform.InverseTransformPoint(bones.First(b=>b.name=="Shin_R").position).x;
        float kneeSign=Mathf.Sign(kneeDelta());
        foreach(int clip in new[]{5,6}){
            r.Select(clip);double duration=r.clips[clip].length;float minS=100,minH=100,minC=100,minP=100,maxLength=0,minBottom=100,minKnees=100;double bottomTime=0;
            for(int i=0;i<=Math.Round(duration*240);i++){
                double t=Math.Min(duration,i/240.0);r.Sample(t);
                float s=World(shoes).Min(v=>v.y),h=World(hands).Min(v=>v.y),c=World(top).Min(v=>v.y),p=World(pack).Min(v=>v.y);
                minS=Mathf.Min(minS,s);minH=Mathf.Min(minH,h);minC=Mathf.Min(minC,c);minP=Mathf.Min(minP,p);
                float bottom=World(skins.Single(v=>v.name=="Bottom")).Min(v=>v.y);if(bottom<minBottom){minBottom=bottom;bottomTime=t;}minKnees=Mathf.Min(minKnees,kneeDelta()*kneeSign);
                for(int j=0;j<pairs.Length;j++)maxLength=Mathf.Max(maxLength,Mathf.Abs(Vector3.Distance(pairs[j].Item1.position,pairs[j].Item2.position)-lengths[j]));
                rows.Add(FormattableString.Invariant($"{clip},{t:F6},{bones.First(b=>b.name=="Pelvis").position.y:F6},{head.position.y:F6},{s:F6},{h:F6},{c:F6},{p:F6},{Mathf.DeltaAngle(0,spine.eulerAngles.x):F6},{Mathf.DeltaAngle(0,head.eulerAngles.x):F6},{leftHand.position.x:F6},{leftHand.position.y:F6},{leftHand.position.z:F6},{rightHand.position.x:F6},{rightHand.position.y:F6},{rightHand.position.z:F6}"));
            }
            report.Add($"clip {clip}: {duration:F6}s, minimum shoes/hands/coat/backpack {minS:F6}/{minH:F6}/{minC:F6}/{minP:F6}m, max segment change {maxLength:F6}m");
            report.Add($"Pants minimum {minBottom:F6}m at {bottomTime:F6}s; left-right knee gap {minKnees:F6}m");if(minBottom<-.0005f||minKnees<.20f)errors.Add("Pants ground / knee alignment "+clip);
            // Coat intersections are no longer an animation acceptance gate (v0.90).
            if(minS<-.0005f||minH<-.0005f||minP<-.0005f||maxLength>.001f)errors.Add("Ground/length criteria failed clip "+clip);
            for(int phase=0;phase<8;phase++){
                r.Pose(phase);foreach(var view in new[]{("Front",new Vector3(0,1,4.4f)),("Side",new Vector3(4.4f,1,0)),("Rear",new Vector3(-3,1,-3))}){
                    r.reviewCamera.transform.position=view.Item2;r.reviewCamera.transform.LookAt(new Vector3(0,.8f,0));Capture(r,$"{clip}_{phase}_{view.Item1}");
                }
            }
        }
        r.Select(7);float seatBottom=World(skins.Single(s=>s.name=="Bottom")).Min(v=>v.y);report.Add($"Seated pants minimum {seatBottom:F6}m");if(seatBottom<-.0005f||seatBottom>.005f)errors.Add("Seat contact gap");
        float maxSlip=0,minContact=100,maxContact=-100;
        foreach(var interval in new[]{(5,.2f/1.6f,.65f/1.6f,false),(5,1.02f/1.6f,1.14f/1.6f,true),(5,1.35f/1.6f,1f,false),(6,0f,.25f/1.8f,false),(6,.35f/1.8f,.55f/1.8f,true),(6,.65f/1.8f,1.5f/1.8f,false)}){
            float intervalSlip=0;double worstTime=0;r.Select(interval.Item1);var skin=interval.Item4?hands:shoes;var weights=skin.sharedMesh.boneWeights;
            foreach(string side in new[]{"L","R"}){
                int bi=Array.FindIndex(skin.bones,b=>b.name==(interval.Item4?"Hand_":"Foot_")+side);
                var ids=Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==bi&&weights[i].weight0>.99f).ToArray();
                r.Sample(r.clips[interval.Item1].length*interval.Item2);var first=World(skin);int contact=ids.OrderBy(i=>first[i].y).First();Vector3 anchor=first[contact];
                double supportStart=r.clips[interval.Item1].length*interval.Item2;
                double supportEnd=r.clips[interval.Item1].length*interval.Item3+(interval.Item4&&side=="R"?1.0/30:0);
                for(int i=0;i<=64;i++){double t=supportStart+(supportEnd-supportStart)*i/64;r.Sample(t);var vertices=World(skin);float low=ids.Min(k=>vertices[k].y);minContact=Mathf.Min(minContact,low);maxContact=Mathf.Max(maxContact,low);float slip=Vector3.Distance(anchor,vertices[contact]);maxSlip=Mathf.Max(maxSlip,slip);if(slip>intervalSlip){intervalSlip=slip;worstTime=t;}}
            }
            report.Add($"support {interval}: drift {intervalSlip:F6}m at {worstTime:F6}s");
        }
        report.Add($"Declared hand/foot supports (right hand release +1/30s): same-vertex drift {maxSlip:F6}m, sole/palm gap {minContact:F6}..{maxContact:F6}m.");if(maxSlip>.0035f||minContact<-.0005f||maxContact>.005f)errors.Add("Support contact criteria");
        foreach(int clip in new[]{5,6})foreach(int fps in new[]{30,60,120})foreach(bool half in new[]{false,true}){
            r.Select(clip);r.Sample(r.clips[clip].length);var expectedEnd=skins.Where(s=>s.enabled).SelectMany(World).ToArray();
            r.Select(clip);r.paused=false;r.slow=half;float duration=r.clips[clip].length/(half?.5f:1);int frames=Mathf.RoundToInt(duration*fps);
            for(int i=0;i<frames;i++)r.Advance(1f/fps);
            if(Math.Abs(r.Elapsed-r.clips[clip].length)>.0001)errors.Add("Timing "+clip);
            if(expectedEnd.Zip(skins.Where(s=>s.enabled).SelectMany(World),Vector3.Distance).Max()>.0001f)errors.Add("Frame-rate final pose "+clip);
            var end=World(shoes);r.Advance(1);if(end.Zip(World(shoes),Vector3.Distance).Max()>.00001)errors.Add("Endpoint loop "+clip);
            r.paused=true;double oldTime=r.Elapsed;r.Advance(.4f);if(r.Elapsed!=oldTime)errors.Add("Paused clock");
            r.Select(clip);r.paused=false;r.Advance(duration*.4f);var mid=skins.Where(s=>s.enabled).SelectMany(World).ToArray();oldTime=r.Elapsed;
            r.paused=true;r.Advance(.4f);if(r.Elapsed!=oldTime||mid.Zip(skins.Where(s=>s.enabled).SelectMany(World),Vector3.Distance).Max()>.00001f)errors.Add("Mid-transition pause/hem");
            r.paused=false;r.Advance(.1f);if(r.Elapsed<=oldTime)errors.Add("Mid-transition resume");
        }
        r.slow=false;r.paused=false;r.Select(0);r.RequestSit();r.Advance(.4f);double before=r.Elapsed;r.RequestSit();r.RequestStand();if(r.selected!=5||r.Elapsed!=before)errors.Add("Repeated transition input");r.Advance(1.3f);r.RequestStand();if(r.selected!=6)errors.Add("Stand request");r.Advance(2);
        r.BeginMovement();r.RequestWalk(true);r.Advance(.8f);var start=r.resident.transform.position;r.RequestSit();r.Advance(3);if(r.selected!=5||r.MovingReview||r.resident.transform.position.z<start.z)errors.Add("Movement to sit");
        r.Select(0);if(top.sharedMesh!=original)errors.Add("Coat restoration");
        r.Select(7);typeof(AdultRabbitMotionReview).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,null);if(top.sharedMesh!=original||pack.sharedMesh!=originalPack)errors.Add("Disable restoration");r.Select(5);r.paused=false;r.Advance(1.6f);if(r.SitState!="앉음")errors.Add("Reinitialization");
        var seatedVertices=skins.Where(s=>s.enabled).SelectMany(World).ToArray();r.Select(6);var nextVertices=skins.Where(s=>s.enabled).SelectMany(World).ToArray();float seam=seatedVertices.Zip(nextVertices,Vector3.Distance).Max();report.Add($"Sit-down end to stand-up start vertex difference {seam:F6}m; explicit disable/reset and recreation checked (not actual Play Mode input).");if(seam>.0035f)errors.Add("Seated endpoint mismatch");r.Select(0);
        r.resident.transform.localPosition=Vector3.zero;
        report.Add("30/60/120fps x 0.5/1 clocks and all-renderer final poses, endpoint hold, mid-transition pause/resume including hem, repeated requests, moving stop then sit, coat restore checked by direct calls.");
        report.AddRange(errors);report.Add(errors.Count==0?"NUMERICAL CHECKS PASS; contact/visual approval separate":"INCOMPLETE");
        File.WriteAllLines("Docs/AdultRabbitSitVerification.txt",report);File.WriteAllLines("Docs/AdultRabbitSitMotion.csv",rows);
        if(strict&&errors.Count>0){Debug.LogError(string.Join("\n",report));EditorApplication.Exit(1);}else Debug.Log(errors.Count==0?"ADULT_SIT_OK":"ADULT_SIT_INSPECTED");
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.Select(5);r.paused=false;r.slow=false;
        for(int frame=0;frame<126;frame++){
            if(frame==66)r.RequestStand();if(frame>0)r.Advance(1f/30);
            foreach(var view in new[]{("Front",new Vector3(0,1,4.4f)),("Side",new Vector3(4.4f,1,0)),("Quarter",new Vector3(2.8f,1.2f,3.4f))}){r.reviewCamera.transform.position=view.Item2;r.reviewCamera.transform.LookAt(new Vector3(0,.8f,0));Capture(r,$"{view.Item1}{frame:D3}","Logs/AdultRabbitSitSequence");}
        }
        r.Select(0);Debug.Log("ADULT_SIT_SEQUENCE_OK");
    }
}
