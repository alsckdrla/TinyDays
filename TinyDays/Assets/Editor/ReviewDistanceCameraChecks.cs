using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class ReviewDistanceCameraChecks
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception("Camera137: "+message);}
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Flags).SetValue(owner,value);
    static void Call(object owner,string name,params object[] args)=>owner.GetType().GetMethod(name,Flags).Invoke(owner,args);
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    static void Controller(MonoBehaviour owner,Camera camera,Action reset,Action<Vector2,float> move,Action<float,float> elevate){
        var farm=owner as FarmStudyReview;
        const float dt=1f/240;
        foreach(float distance in new[]{1f,2f,4f,8f,20f}){
            foreach(float sign in new[]{-1f,1f}){
                reset();Call(owner,"OnApplicationFocus",true);Set(owner,farm?"desiredDistance":"distance",distance);
                move(Vector2.zero,0);var p=camera.transform.position;var q=camera.transform.rotation;
                move(Vector2.up*sign,dt);var delta=camera.transform.position-p;
                Check(Mathf.Abs(delta.magnitude/dt-ReviewCameraKeys.Speed(distance))<.003f,"Horizontal speed "+owner.name+" d="+distance);
                Check(Mathf.Abs(delta.y)<.00002f&&Quaternion.Angle(q,camera.transform.rotation)<.001f,"Horizontal preserves height/angle");
                reset();Set(owner,farm?"desiredDistance":"distance",distance);move(Vector2.zero,0);p=camera.transform.position;q=camera.transform.rotation;
                elevate(sign,dt);delta=camera.transform.position-p;
                Check(Mathf.Abs(delta.y/dt-sign*ReviewCameraKeys.Speed(distance))<.003f,"Vertical speed "+owner.name+" d="+distance);
                Check(new Vector2(delta.x,delta.z).magnitude<.00002f&&Quaternion.Angle(q,camera.transform.rotation)<.001f,"Vertical preserves horizontal/angle");
            }
        }
        foreach(int fps in new[]{30,60,120}){
            reset();Set(owner,farm?"desiredDistance":"distance",8f);move(Vector2.zero,0);var p=camera.transform.position;
            for(int i=0;i<fps;i++)elevate(1,1f/fps);
            var delta=camera.transform.position-p;
            Debug.Log("DISTANCE137_FPS "+owner.GetType().Name+" "+fps+" "+delta.ToString("F6"));
            if(fps==30)reference=delta;else Check(Vector3.Distance(delta,reference)<Mathf.Max(.0001f,reference.magnitude*.01f),"Vertical frame rate tolerance "+owner.GetType().Name+" fps="+fps+" first="+reference.ToString("F6")+" actual="+delta.ToString("F6"));
        }
        reset();var origin=camera.transform.position;Call(owner,"OnApplicationFocus",false);elevate(1,1);move(Vector2.up,1);
        Check(Vector3.Distance(origin,camera.transform.position)<.00001f,"Both inputs focus-blocked");Call(owner,"OnApplicationFocus",true);
        elevate(1,.1f);reset();Check(Vector3.Distance(origin,camera.transform.position)<.00001f,"Height Home reset");
        foreach(string name in new[]{"paused","slow"}){
            var field=owner.GetType().GetField(name,Flags|BindingFlags.Public);if(field==null)continue;
            reset();elevate(1,.1f);var baseline=camera.transform.position;reset();field.SetValue(owner,true);elevate(1,.1f);
            Check(Vector3.Distance(baseline,camera.transform.position)<.00001f,"Vertical independent of animation time");field.SetValue(owner,false);
        }
    }
    static Vector3 reference;
    static void Existing(MonoBehaviour owner,Camera camera,Action reset,Action<Vector2,float> move){
        typeof(ReviewKeyboardCameraChecks).GetMethod("Controller",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{owner,camera,reset,move});
    }
    static T Open<T>(string path) where T:MonoBehaviour {EditorSceneManager.OpenScene(path);var result=UnityEngine.Object.FindObjectOfType<T>();Check(result,"Missing review scene");return result;}
    public static void Execute(){try{
        var paths=new[]{"Assets/Scenes/AdultRabbitStudy.unity","Assets/Scenes/AdultRabbitMotionStudy.unity","Assets/Scenes/RabbitHomeLifeStudy.unity",FarmStudyBuilder.ScenePath,HouseVillageBuilder.ScenePath};
        var hashes=paths.Concat(Directory.GetFiles("ArtSource","AdultRabbit*.blend")).Concat(Directory.GetFiles("Assets","AdultRabbit*.fbx",SearchOption.AllDirectories)).Distinct().ToDictionary(p=>p,Hash);
        typeof(ReviewKeyboardCameraChecks).GetMethod("Inputs",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        Check(ReviewCameraKeys.HeightAxis(k=>k==KeyCode.Q||k==KeyCode.E)==0,"Opposite height keys");
        Check(ReviewCameraKeys.Speed(0)==.5f&&ReviewCameraKeys.Speed(.5f)==.5f,"Minimum 1m distance");
        Check(FarmStudyReview.PanDistance(2)==8,"Mouse minimum reference preserved");
        var a=Open<AdultRabbitReview>(paths[0]);Controller(a,a.reviewCamera,a.Home,a.KeyboardMove,a.KeyboardElevate);Existing(a,a.reviewCamera,a.Home,a.KeyboardMove);
        var m=Open<AdultRabbitMotionReview>(paths[1]);Controller(m,m.reviewCamera,m.Home,m.KeyboardMove,m.KeyboardElevate);Existing(m,m.reviewCamera,m.Home,m.KeyboardMove);
        var l=Open<RabbitHomeLifeReview>(paths[2]);Controller(l,l.reviewCamera,l.Home,l.KeyboardMove,l.KeyboardElevate);Existing(l,l.reviewCamera,l.Home,l.KeyboardMove);
        var f=Open<FarmStudyReview>(paths[3]);Controller(f,f.reviewCamera,f.ShowOverview,f.KeyboardMove,f.KeyboardElevate);Existing(f,f.reviewCamera,f.ShowOverview,f.KeyboardMove);
        f.FocusResident(2);Set(f,"heightOffset",3f);f.KeyboardMove(Vector2.zero,0);
        var pivot=f.GetType().GetField("pivot",Flags);var p0=f.reviewCamera.transform.position;
        var distance0=Vector3.Distance(p0,(Vector3)pivot.GetValue(f));Check(Mathf.Abs(f.KeyboardTargetDistance-distance0)<.00001f,"Actual offset distance");
        f.KeyboardElevate(1,1f/240);Check(Mathf.Abs((f.reviewCamera.transform.position.y-p0.y)*240-ReviewCameraKeys.Speed(distance0))<.003f,"Follow/height offset speed");
        Check(f.IsFollowing&&f.focus==2,"Height preserves following");
        f.ShowOverview();Set(f,"settingsOpen",true);p0=f.reviewCamera.transform.position;f.KeyboardElevate(1,1);f.KeyboardMove(Vector2.up,1);Check(p0==f.reviewCamera.transform.position,"Settings block both");Set(f,"settingsOpen",false);
        var picker=f.GetType().GetField("colorPicker",Flags).GetValue(f);Set(picker,"lighting",UnityEngine.Object.FindObjectOfType<FarmLightingStudy>());
        f.KeyboardElevate(1,1);f.KeyboardMove(Vector2.up,1);Check(p0==f.reviewCamera.transform.position,"Color modal blocks both");Set(picker,"lighting",null);
        var h=Open<HouseVillageReview>(paths[4]);Controller(h,h.reviewCamera,()=>h.SetView(0),h.KeyboardMove,h.KeyboardElevate);Existing(h,h.reviewCamera,()=>h.SetView(0),h.KeyboardMove);
        ReviewCameraBindingChecks.Verify();
        File.WriteAllText("Docs/ReviewDistance137Verification.txt","v0.137 PASS: five saved camera controllers; distance 1/2/4/8/20m horizontal and Q/E speed=max(1,d)*0.5; world-up elevation preserves rotation/horizontal position; minimum distance1m; duplicate/opposite/diagonal keys, rotated and overhead views, zoom, Home, focus and animation pause/speed; farm height offset uses actual pivot distance, follow preserved, settings/color modal guards; 30/60/120fps constant and changing-distance comparison within1%. Existing mouse/selection/panel regression passed. Synthetic key states and direct calls, not OS key input or GUI typing.\n");
        Debug.Log("REVIEW_DISTANCE137_VERIFY_OK");
        AdultRabbitBuilder.BuildPlayer();AdultRabbitMotionBuilder.BuildPlayer();RabbitHomeLifeBuilder.BuildPlayer();ResidentOcclusionChecks.BuildReviewPlayer();
        Check(hashes.All(k=>Hash(k.Key)==k.Value),"Scene/art preservation");
        File.AppendAllText("Docs/ReviewDistance137Verification.txt","Four Windows builds passed; house checked in saved Unity scene; preserved "+hashes.Count+" scene/Blender/FBX hashes.\n");
        Debug.Log("REVIEW_DISTANCE137_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
