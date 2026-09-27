using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TinyDays.Review;

public static class AdultSupportedSitChecks {
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static Vector3[] World(SkinnedMeshRenderer s)=>AdultRabbitSitCoat.World(s);
    static int[] Indices(SkinnedMeshRenderer s,string bone){int b=Array.FindIndex(s.bones,t=>t.name==bone);return Enumerable.Range(0,s.sharedMesh.vertexCount).Where(i=>s.sharedMesh.boneWeights[i].boneIndex0==b&&s.sharedMesh.boneWeights[i].weight0>.99f).ToArray();}
    static Vector3[] Pose(AdultRabbitMotionReview r)=>r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).SelectMany(World).ToArray();
    static float Difference(Vector3[] a,Vector3[] b)=>a.Zip(b,Vector3.Distance).Max();
    static float SurfaceHeight(Vector3 point,Vector3[] vertices,int[] triangles){
        float high=float.NegativeInfinity;
        for(int i=0;i<triangles.Length;i+=3){var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
            float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(den)<1e-10f)continue;
            float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.z-c.z))/den;
            float v=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.z-c.z))/den;
            if(u>=-.0001f&&v>=-.0001f&&u+v<=1.0001f)high=Mathf.Max(high,u*a.y+v*b.y+(1-u-v)*c.y);
        }return high;
    }
    public static void Execute(){try{Run();Debug.Log("SUPPORTED_SIT_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        Require(r.SupportedSitting&&r.SitClip==10&&r.StandClip==11,"Default style");
        var report=new List<string>{"v0.95 supported sitting; automatic clip/function checks, NOT real input/quality approval."};
        var rows=new List<string>{"clip,time,pelvis,head,rightPalm,leftPalm,kneeX,kneeY,kneeZ,handX,handY,handZ"};
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();var hands=skins.Single(s=>s.name=="BodyHands");var shoes=skins.Single(s=>s.name=="Shoes");
        var bones=r.resident.GetComponentsInChildren<Transform>();Func<string,Transform> bone=n=>bones.First(b=>b.name==n);
        var right=Indices(hands,"Hand_R");var left=Indices(hands,"Hand_L");
        var pairs=new[]{("Thigh_L","Shin_L"),("Shin_L","Foot_L"),("Thigh_R","Shin_R"),("Shin_R","Foot_R"),("UpperArm_L","Forearm_L"),("Forearm_L","Hand_L"),("UpperArm_R","Forearm_R"),("Forearm_R","Hand_R")};
        r.Select(0);var lengths=pairs.Select(p=>Vector3.Distance(bone(p.Item1).position,bone(p.Item2).position)).ToArray();
        float sign=Mathf.Sign((bone("Shin_L").position-bone("Shin_R").position).x);
        foreach(int clip in new[]{10,11}){
            r.Select(clip);float duration=r.clips[clip].length,minS=100,minH=100,minPants=100,minPack=100,maxLength=0,minKnee=100;
            Require(!r.clips[clip].isLooping&&Mathf.Abs(duration-(clip==10?1.6f:1.8f))<.0001f,"Duration/loop");
            for(int i=0;i<=Math.Round(duration*240);i++){
                float t=Mathf.Min(duration,i/240f);r.Sample(t);var h=World(hands);minS=Mathf.Min(minS,World(shoes).Min(v=>v.y));minH=Mathf.Min(minH,h.Min(v=>v.y));
                minPants=Mathf.Min(minPants,World(skins.Single(s=>s.name=="Bottom")).Min(v=>v.y));minPack=Mathf.Min(minPack,World(skins.Single(s=>s.name=="Backpack")).Min(v=>v.y));
                minKnee=Mathf.Min(minKnee,sign*(bone("Shin_L").position-bone("Shin_R").position).x);
                for(int j=0;j<pairs.Length;j++)maxLength=Mathf.Max(maxLength,Mathf.Abs(Vector3.Distance(bone(pairs[j].Item1).position,bone(pairs[j].Item2).position)-lengths[j]));
                var k=bone("Shin_L").position;var hand=bone("Hand_L").position;
                rows.Add(FormattableString.Invariant($"{clip},{t:F6},{bone("Pelvis").position.y:F6},{bone("Head").position.y:F6},{right.Min(j=>h[j].y):F6},{left.Min(j=>h[j].y):F6},{k.x:F6},{k.y:F6},{k.z:F6},{hand.x:F6},{hand.y:F6},{hand.z:F6}"));
            }
            report.Add($"{clip}: minimum shoe/palm {minS*1000:F3}/{minH*1000:F3}mm, segment error {maxLength*1000:F4}mm, knee separation {minKnee*1000:F3}mm.");
            report.Add($"{clip}: minimum pants/backpack height {minPants*1000:F3}/{minPack*1000:F3}mm (coat-leg intersections intentionally not tested).");
            Require(minPants>=-.0005f&&minPack>=-.0005f,string.Join("\n",report));
            Require(minS>=-.0005f&&minH>=-.0005f&&maxLength<=.001f&&minKnee>.20f,string.Join("\n",report));
            // Physical right palm ground support; moving left knee is not a world anchor.
            float a=clip==10?1.02f:.35f,b=clip==10?1.1733333f:.5833333f;
            r.Sample(a);var first=World(hands);int contact=right.OrderBy(j=>first[j].y).First();var anchor=first[contact];float drift=0,low=100,high=-100;
            for(int i=0;i<=Math.Ceiling((b-a)*240);i++){r.Sample(Math.Min(b,a+i/240f));var h=World(hands);drift=Mathf.Max(drift,Vector3.Distance(anchor,h[contact]));low=Mathf.Min(low,right.Min(j=>h[j].y));high=Mathf.Max(high,right.Min(j=>h[j].y));}
            report.Add($"{clip}: right palm ground drift {drift*1000:F3}mm, gap {low*1000:F3}..{high*1000:F3}mm.");Require(drift<=.0035f&&low>=-.0005f&&high<=.005f,string.Join("\n",report));
            a=clip==10?.90f:.30f;b=clip==10?1.12f:.60f;r.Sample(a);var relative=bone("Hand_L").position-bone("Shin_L").position;float kneeDrift=0;
            float minKneeGap=100,maxKneeGap=-100;var pants=skins.Single(s=>s.name=="Bottom");var triangles=pants.sharedMesh.triangles;
            for(int i=0;i<=Math.Ceiling((b-a)*240);i++){r.Sample(Math.Min(b,a+i/240f));var deviation=relative-(bone("Hand_L").position-bone("Shin_L").position);deviation.y=0;kneeDrift=Mathf.Max(kneeDrift,deviation.magnitude);
                var h=World(hands);var palm=h[left.OrderBy(j=>h[j].y).First()];float gap=palm.y-SurfaceHeight(palm,World(pants),triangles);minKneeGap=Mathf.Min(minKneeGap,gap);maxKneeGap=Mathf.Max(maxKneeGap,gap);
            }
            report.Add($"{clip}: left palm to actual pants surface vertical gap {minKneeGap*1000:F3}..{maxKneeGap*1000:F3}mm.");
            Require(minKneeGap>=-.0005f&&maxKneeGap<=.005f,string.Join("\n",report));
            report.Add($"{clip}: knee-relative horizontal hand drift {kneeDrift*1000:F3}mm; vertical follows actual knee surface, not world fixation.");Require(kneeDrift<=.0035f,"Knee hand drift");
            var intervals=clip==10?new[]{("R",.2f,.65f),("R",1.35f,1.6f),("L",.2f,.48f),("L",.94f,1.10f),("L",1.55f,1.6f)}:new[]{("R",0f,.25f),("R",.65f,1.5f),("L",0f,.08f),("L",.35f,.56f),("L",.78f,1.5f)};
            foreach(var interval in intervals){
                var ids=Indices(shoes,"Foot_"+interval.Item1);r.Select(clip);r.Sample(interval.Item2);var vertices=World(shoes);int sole=ids.OrderBy(j=>vertices[j].y).First();var start=vertices[sole];float slip=0,minGap=100,maxGap=-100;
                for(int i=0;i<=Math.Ceiling((interval.Item3-interval.Item2)*240);i++){r.Sample(Math.Min(interval.Item3,interval.Item2+i/240f));vertices=World(shoes);slip=Mathf.Max(slip,Vector3.Distance(start,vertices[sole]));float gap=ids.Min(j=>vertices[j].y);minGap=Mathf.Min(minGap,gap);maxGap=Mathf.Max(maxGap,gap);}
                report.Add($"{clip} foot {interval}: drift {slip*1000:F3}mm, gap {minGap*1000:F3}..{maxGap*1000:F3}mm.");Require(slip<=.0035f&&minGap>=-.0005f&&maxGap<=.005f,string.Join("\n",report));
            }
            // Same starting/ending renderer positions as preserved legacy counterpart.
            foreach(bool end in new[]{false,true}){r.Select(clip);r.Sample(end?duration:0);var pose=Pose(r);r.Select(clip==10?5:6);r.Sample(end?duration:0);float delta=Difference(pose,Pose(r));Require(delta<.0001f,"Legacy endpoint mismatch");}
            foreach(int fps in new[]{30,60,120})foreach(bool half in new[]{false,true}){
                r.Select(clip);r.Sample(duration);var expected=Pose(r);r.Select(clip);r.paused=false;r.slow=half;
                for(int i=0;i<Mathf.RoundToInt(duration*fps/(half?.5f:1));i++)r.Advance(1f/fps);
                Require(Difference(expected,Pose(r))<.0001f,"fps/speed endpoint");r.Advance(.3f);Require(Difference(expected,Pose(r))<.0001f,"One shot looped");
                r.Select(clip);r.Advance(.4f);var mid=Pose(r);double time=r.Elapsed;r.paused=true;r.Advance(.4f);Require(time==r.Elapsed&&Difference(mid,Pose(r))<.00001f,"Pause changed pose");r.paused=false;
            }
        }
        foreach(bool supported in new[]{true,false}){
            r.Select(0);r.SetSitStyle(supported);r.slow=false;r.paused=false;r.RequestSit();int sit=r.SitClip,stand=r.StandClip;r.Advance(.4f);double t=r.Elapsed;
            Require(!r.SetSitStyle(!supported),"Style switched mid transition");r.RequestSit();r.RequestStand();Require(r.selected==sit&&r.Elapsed==t,"Repeated input restarted");
            r.Advance(1.3f);r.RequestStand();Require(r.selected==stand,"Selected rise variant");r.Advance(2);
            r.BeginMovement();r.RequestWalk(true);r.Advance(.8f);r.RequestSit();r.Advance(3);Require(r.selected==sit&&!r.MovingReview,"Moving sit variant");
            r.Select(0);r.StartBreathing(true);r.RequestRun(true);r.Advance(.4f);Require(!r.SetSitStyle(!supported),"Style switched during idle rise");r.Advance(2.3f);Require(r.MovingReview&&r.FootTransition.WantsRun,"Idle departure variant");
        }
        r.Select(0);r.SetSitStyle(true);r.StartBreathing(true);r.RequestRun(false);r.Advance(.4f);r.RequestWalk(false);r.Advance(3);Require(!r.MovingReview&&r.Idle!=null&&!r.Idle.Seated,"Rise cancellation");
        typeof(AdultRabbitMotionReview).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,null);Require(r.Idle==null&&!r.SitTransition,"Disable cleanup");r.StartBreathing(false);r.RequestSit();Require(r.selected==10,"Reactivation default");r.Select(0);
        report.Add("Both style routes, 30/60/120fps x 0.5/1, endpoint holds, pause, repeat, queued movement, cancel and reset PASS. Legacy regression and visual checks separate.");
        File.WriteAllLines("Docs/AdultSupportedSitVerification.txt",report);File.WriteAllLines("Docs/AdultSupportedSitMotion.csv",rows);
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.Select(10);r.slow=false;r.paused=false;
        var capture=typeof(AdultRabbitMotionBuilder).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);
        Directory.CreateDirectory("Logs/AdultSupportedSitSequence");
        for(int frame=0;frame<126;frame++){
            if(frame==66)r.RequestStand();if(frame>0)r.Advance(1f/30);
            foreach(var view in new[]{("Front",new Vector3(0,1,4.4f)),("Side",new Vector3(4.4f,1,0)),("Quarter",new Vector3(2.8f,1.2f,3.4f))}){
                r.reviewCamera.transform.position=view.Item2;r.reviewCamera.transform.LookAt(new Vector3(0,.8f,0));
                capture.Invoke(null,new object[]{r.reviewCamera,r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray(),$"{view.Item1}{frame:D3}","Logs/AdultSupportedSitSequence"});
            }
        }
        r.Select(0);Debug.Log("SUPPORTED_SIT_SEQUENCE_OK");
    }
}
