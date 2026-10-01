using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TinyDays.Review;

public static class AdultLyingChecks {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Execute(){try{Run();Debug.Log("ADULT_LYING_OK");}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();r.AutomaticIdle=false;
        Check(r.clips.Length>=22,"22 clips required");
        var durations=new[]{3f,3.2f,2f,2.2f,1f};
        var bones=r.resident.GetComponentsInChildren<Transform>();
        var joints=new[]{"L","R"}.SelectMany(s=>new[]{("UpperArm_"+s,"Forearm_"+s),("Forearm_"+s,"Hand_"+s),("Thigh_"+s,"Shin_"+s),("Shin_"+s,"Foot_"+s)}).Select(p=>new[]{bones.Single(b=>b.name==p.Item1),bones.Single(b=>b.name==p.Item2)}).ToArray();
        r.Select(17);var lengths=joints.Select(j=>Vector3.Distance(j[0].position,j[1].position)).ToArray();
        var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>();var shoes=skins.Single(s=>s.name=="Shoes");var head=skins.Single(s=>s.name=="Head");var hands=skins.Single(s=>s.name=="BodyHands");
        var bag=skins.Single(s=>s.name=="Backpack");
        float len=0,minShoe=999,minHead=999,minHand=999,contactGap=0,contactDrift=0;
        var rows=new List<string>{"clip,time,pelvis_y,head_y,shoe_min,head_min,hand_min"};
        foreach(int clip in Enumerable.Range(17,5)){
            Check(Mathf.Abs(r.clips[clip].length-durations[clip-17])<.002&&!r.clips[clip].isLooping,"Duration / loop "+clip);
            r.Select(clip);Vector3[] contactStart=null;
            for(int sample=0;sample<=Mathf.RoundToInt(durations[clip-17]*240);sample++){
                float t=sample/240f;r.Sample(t);
                float s=AdultRabbitSitCoat.World(shoes).Min(p=>p.y),h=AdultRabbitSitCoat.World(head).Min(p=>p.y),hand=AdultRabbitSitCoat.World(hands).Min(p=>p.y);
                minShoe=Mathf.Min(minShoe,s);minHead=Mathf.Min(minHead,h);minHand=Mathf.Min(minHand,hand);
                for(int j=0;j<joints.Length;j++)len=Mathf.Max(len,Mathf.Abs(lengths[j]-Vector3.Distance(joints[j][0].position,joints[j][1].position)));
                // Palm holds in the standalone seated recline; moving feet/head
                // roll-down portions deliberately are not locked-support tests.
                if(clip==19&&t>=1.32f&&t<=1.40f){
                    contactGap=Mathf.Max(contactGap,hand);
                    var current=new[]{"Hand_L","Hand_R"}.Select(n=>bones.Single(b=>b.name==n).position).ToArray();
                    if(contactStart==null)contactStart=current;else contactDrift=Mathf.Max(contactDrift,current.Select((p,i)=>Vector3.Distance(p,contactStart[i])).Max());
                }
                rows.Add(FormattableString.Invariant($"{clip},{t:F6},{bones.Single(b=>b.name=="Pelvis").position.y:F7},{bones.First(b=>b.name=="Head"&&!b.GetComponent<Renderer>()).position.y:F7},{s:F7},{h:F7},{hand:F7}"));
            }
        }
        File.WriteAllLines("Docs/AdultLyingMotion.csv",rows);
        var report=new List<string>{$"240Hz limb length error {len*1000:F4}mm; shoe minimum {minShoe*1000:F4}mm; head/ears minimum {minHead*1000:F4}mm; hands minimum {minHand*1000:F4}mm; seated palm hold gap {contactGap*1000:F4}mm / drift {contactDrift*1000:F4}mm."};
        File.WriteAllLines("Docs/AdultLyingVerification.txt",report);
        Check(len<.0001&&minShoe>=-.0005&&minHead>=-.0005&&minHand>=-.0005,"Geometry / floor: "+report[0]);
        Check(contactGap<=.005&&contactDrift<=.0035,"Palm support");
        r.Select(21);var torso=skins.Single(s=>s.name=="BodyTorso");float torsoFloor=AdultRabbitSitCoat.World(torso).Min(p=>p.y);
        report.Add($"Supine anatomical torso support clearance {torsoFloor*1000:F4}mm (coat hem excluded).");
        Check(torsoFloor>=-.0005&&torsoFloor<=.005,"Supine body support "+torsoFloor);
        float endpointError=0;
        foreach(var endpoint in new[]{(17,0f,8),(17,3f,21),(18,0f,21),(18,3.2f,8),(19,0f,9),(19,2f,21),(20,0f,21),(20,2.2f,9)}){
            r.Select(endpoint.Item1);r.Sample(endpoint.Item2);var positions=bones.Select(b=>b.position).ToArray();
            r.Select(endpoint.Item3);r.Sample(0);var errors=bones.Select((b,i)=>Vector3.Distance(b.position,positions[i])).ToArray();endpointError=Mathf.Max(endpointError,errors.Max());
            report.Add($"Endpoint {endpoint.Item1}@{endpoint.Item2} -> {endpoint.Item3}: {errors.Max()*1000:F4}mm at {bones[Array.IndexOf(errors,errors.Max())].name}");
        }
        report.Add($"Endpoint joint-position difference to shared breathing/supine references {endpointError*1000:F4}mm.");
        File.WriteAllLines("Docs/AdultLyingVerification.txt",report);Check(endpointError<=.005,"Shared endpoint mismatch "+endpointError);
        int cases=0;
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(bool seated in new[]{false,true}){
            r.Select(0);r.StartBreathing(seated);r.slow=slow;r.RequestLie();r.RequestLie();
            for(int i=0;i<fps*8;i++)r.Advance(1f/fps);
            Check(r.Posture==AdultRabbitMotionReview.RestPosture.Supine&&(r.selected==21||r.selected==22)&&!bag.enabled,"Lying hold / bag");
            double clock=r.Elapsed;r.paused=true;r.Advance(.3f);Check(r.Elapsed==clock,"Pause");r.paused=false;
            r.RequestLyingReturn(seated);
            for(int i=0;i<fps*8;i++)r.Advance(1f/fps);
            Check(r.Idle!=null&&r.Idle.Seated==seated&&!bag.enabled,"Return posture");
            r.Select(0);Check(bag.enabled,"Bag restore on select");cases++;
        }
        foreach(int fps in new[]{30,60,120})foreach(bool slow in new[]{false,true})foreach(bool cancel in new[]{false,true})foreach(float phase in new[]{.1f,.5f,.9f}){
            r.Select(0);r.StartBreathing(false);r.slow=slow;r.RequestLie();r.Advance((.25f+phase*3)/(slow?.5f:1));
            r.RequestRun(false);r.RequestRun(true);if(cancel)r.RequestWalk(false);
            for(int i=0;i<fps*18&&!r.MovingReview;i++)r.Advance(1f/fps);
            Check(cancel?r.Posture==AdultRabbitMotionReview.RestPosture.Supine&&!r.MovingReview:r.MovingReview&&r.FootTransition.WantsRun,"Last request / cancellation");cases++;
        }
        r.Select(0);bag.enabled=false;r.Select(21);r.Select(0);Check(!bag.enabled,"Originally hidden bag restore");bag.enabled=true;
        r.Select(21);r.SendMessage("OnDisable");Check(bag.enabled&&!r.LyingReview,"Disable cleanup");
        r.BeginMovement();r.RequestRun(false);r.Advance(.8f);r.RequestLie();for(int i=0;i<2400;i++)r.Advance(1f/240);
        Check((r.selected==21||r.selected==22),"Stop before lying");r.Select(0);
        r.BeginMovement();r.RequestRun(false);r.Advance(.8f);r.RequestLie();r.RequestRun(true);
        Check(!r.PendingLie&&r.MovingReview&&r.FootTransition.WantsRun&&bag.enabled,"Moving lie cancelled by latest departure");
        r.RequestWalk(false);for(int i=0;i<720;i++)r.Advance(1f/240);
        Check(r.FootTransition.State==AdultRabbitFootTransition.Stage.Idle,"Stop after cancelled lie");r.Select(0);
        foreach(bool seated in new[]{false,true}){
            r.StartBreathing(seated);r.Idle.Play(seated?"앉아서 낙서하기":"서서 짝발 대기");r.Advance(2);r.RequestLie();
            for(int i=0;i<1800;i++)r.Advance(1f/240);Check((r.selected==21||r.selected==22),"Variation tidy before lying");r.Select(0);
        }
        report.Add($"{cases} timing/return/latest-request/cancel cases at 30/60/120fps and .5/1 speed PASS; pause/repeat/reset/previous visibility/disable/moving-stop PASS.");
        report.Add("Automatic samples and method calls, not physical mouse input or user motion-quality approval. Fine coat/leg collision excluded.");File.WriteAllLines("Docs/AdultLyingVerification.txt",report);
    }
    public static void Sequence(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        Directory.CreateDirectory("Logs/LyingSequence");Directory.CreateDirectory("Docs/Captures/Lying");
        foreach(int clip in Enumerable.Range(17,5))foreach(int view in new[]{0,45,90}){
            r.Select(clip);var skins=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
            Action camera=()=>{float a=view*Mathf.Deg2Rad;r.reviewCamera.transform.position=new Vector3(4.5f*Mathf.Sin(a),2.4f,4.5f*Mathf.Cos(a));r.reviewCamera.transform.LookAt(new Vector3(0,.80f,-.25f));};
            for(int pose=0;pose<8;pose++){r.Sample(r.clips[clip].length*AdultRabbitMotionReview.LyingPosePhases[pose]);camera();Capture(r.reviewCamera,skins,$"{clip}_{view}_Pose{pose}","Docs/Captures/Lying");}
            if(clip==21)continue;
            for(int frame=0;frame<=Mathf.RoundToInt(r.clips[clip].length*30);frame++){r.Sample(frame/30.0);camera();Capture(r.reviewCamera,skins,$"{clip}_{view}_{frame:D3}","Logs/LyingSequence");}
        }
        r.Select(0);Debug.Log("ADULT_LYING_SEQUENCE_OK");
    }
    static void Capture(Camera camera,SkinnedMeshRenderer[] skins,string name,string directory){
        var proxies=new List<GameObject>();var target=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(720,720,24){antiAliasing=4};Texture2D texture=null;
        try{
            foreach(var skin in skins){
                var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.vertices=AdultRabbitSitCoat.World(skin);
                var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                var normals=mesh.normals;var weights=mesh.boneWeights;
                for(int i=0;i<normals.Length;i++){var n=normals[i];var w=weights[i];normals[i]=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;}
                mesh.normals=normals;mesh.RecalculateBounds();var go=new GameObject("__LyingCapture");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;proxies.Add(go);skin.enabled=false;
            }
            rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            texture=new Texture2D(720,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,720,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());
        }finally{
            camera.targetTexture=target;RenderTexture.active=active;if(texture)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            foreach(var go in proxies){UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(go);}foreach(var skin in skins)skin.enabled=true;
        }
    }
}
