using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class ReviewKeyboardCameraChecks
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Report="Docs/ReviewKeyboard136Verification.txt";
    static void Check(bool ok,string message){if(!ok)throw new Exception("Keyboard136: "+message);}
    static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Flags).SetValue(target,value);
    static T Field<T>(object target,string field)=>(T)target.GetType().GetField(field,Flags).GetValue(target);
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    static void Inputs(){
        Func<KeyCode[],Vector2> axes=keys=>ReviewCameraKeys.Axes(k=>keys.Contains(k));
        Check(axes(new[]{KeyCode.W,KeyCode.UpArrow})==Vector2.up,"Duplicate forward");
        Check(axes(new[]{KeyCode.W,KeyCode.S,KeyCode.A,KeyCode.D})==Vector2.zero,"Opposite keys");
        Check(axes(new[]{KeyCode.UpArrow,KeyCode.DownArrow,KeyCode.LeftArrow,KeyCode.RightArrow})==Vector2.zero,"Opposite arrows");
        foreach(var pair in new[]{new[]{KeyCode.W,KeyCode.UpArrow},new[]{KeyCode.S,KeyCode.DownArrow},new[]{KeyCode.A,KeyCode.LeftArrow},new[]{KeyCode.D,KeyCode.RightArrow}})
            Check(axes(pair)==axes(new[]{pair[0]})&&axes(pair)==axes(new[]{pair[1]}),"Equivalent keys");
        foreach(float yaw in new[]{0f,43f,180f,279f})foreach(float pitch in new[]{-90f,-89.99f,-45f,0f,75f})foreach(float zoom in new[]{2f,8f,20f}){
            var q=Quaternion.Euler(pitch,yaw,0);var v=FarmStudyReview.KeyboardPan(q,Vector2.up,zoom);
            var expected=Quaternion.Euler(0,yaw,0)*Vector3.forward;
            Check(Vector3.Dot(v.normalized,expected)>.9999f&&Mathf.Abs(v.y)<1e-6f,"Horizontal forward including vertical view");
            float speed=ReviewCameraKeys.Speed(zoom);
            Check(Mathf.Abs(v.magnitude-speed)<.0001f,"Zoom speed");
            Check(Mathf.Abs(FarmStudyReview.KeyboardPan(q,Vector2.one,zoom).magnitude-speed)<.0001f,"Diagonal normalization");
        }
        Check(ReviewCameraKeys.Allowed(true)&&!ReviewCameraKeys.Allowed(false)&&!ReviewCameraKeys.Allowed(true,true)&&!ReviewCameraKeys.Allowed(true,false,true),"Focus/modal/text guard");
    }
    static void Controller(MonoBehaviour owner,Camera camera,Action reset,Action<Vector2,float> move){
        foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right,Vector2.one}){
            reset();Call(owner,"OnApplicationFocus",true);var p=camera.transform.position;var q=camera.transform.rotation;
            move(direction,.01f);var delta=camera.transform.position-p;
            var expected=FarmStudyReview.KeyboardPan(q,direction,8).normalized;
            Check(delta.magnitude>.0001f&&Vector3.Dot(delta.normalized,expected)>.999f,"Controller direction "+owner.name);
            Check(Mathf.Abs(delta.y)<.00002f&&Quaternion.Angle(q,camera.transform.rotation)<.001f,"Height/rotation preserved");
        }
        foreach(float yaw in new[]{35f,175f})foreach(float pitch in new[]{-90f,45f}){
            reset();Set(owner,"yaw",yaw);Set(owner,"pitch",pitch);
            move(Vector2.zero,0);var p=camera.transform.position;var q=camera.transform.rotation;
            move(Vector2.up,.01f);var delta=camera.transform.position-p;
            Check(Vector3.Dot(delta.normalized,FarmStudyReview.KeyboardPan(q,Vector2.up,8).normalized)>.999f,"Scene rotated/vertical heading");
            Check(Mathf.Abs(delta.y)<.00002f&&Quaternion.Angle(q,camera.transform.rotation)<.001f,"Scene rotated/vertical invariants");
        }
        foreach(int fps in new[]{30,60,120}){
            reset();var p=camera.transform.position;for(int i=0;i<fps;i++)move(Vector2.one,.1f/fps);
            var total=camera.transform.position-p;
            if(fps==30)reference=total;else Check(Vector3.Distance(reference,total)<.0001f,"Frame rate independence "+owner.name);
        }
        reset();var origin=camera.transform.position;
        Call(owner,"OnApplicationFocus",false);move(Vector2.up,1);Check(Vector3.Distance(origin,camera.transform.position)<.00001f,"Unfocused movement");
        Call(owner,"OnApplicationFocus",true);move(Vector2.up,.1f);Check(Vector3.Distance(origin,camera.transform.position)>.001f,"Focus regain");
        reset();Check(Vector3.Distance(origin,camera.transform.position)<.00001f,"Home reset");
        // Public camera path receives real-time dt; animation pause/speed must not change it.
        foreach(string field in new[]{"paused","slow"})if(owner.GetType().GetField(field,Flags|BindingFlags.Public)!=null){
            reset();move(Vector2.up,.1f);var baseline=camera.transform.position;
            reset();var setting=owner.GetType().GetField(field,Flags|BindingFlags.Public);setting.SetValue(owner,true);move(Vector2.up,.1f);Check(Vector3.Distance(baseline,camera.transform.position)<.00001f,"Animation time leaked to camera");setting.SetValue(owner,false);
        }
    }
    static Vector3 reference;
    static T Open<T>(string path) where T:MonoBehaviour {EditorSceneManager.OpenScene(path);var owner=UnityEngine.Object.FindObjectOfType<T>();Check(owner,"Missing scene review "+path);return owner;}
    public static void Execute(){try{
        var scenes=new[]{"Assets/Scenes/AdultRabbitStudy.unity","Assets/Scenes/AdultRabbitMotionStudy.unity","Assets/Scenes/RabbitHomeLifeStudy.unity",FarmStudyBuilder.ScenePath,HouseVillageBuilder.ScenePath};
        var preserved=scenes.Concat(Directory.GetFiles("ArtSource","AdultRabbit*.blend")).Concat(Directory.GetFiles("Assets","AdultRabbit*.fbx",SearchOption.AllDirectories)).Distinct().ToDictionary(p=>p,Hash);
        Inputs();
        var a=Open<AdultRabbitReview>(scenes[0]);Controller(a,a.reviewCamera,a.Home,a.KeyboardMove);
        var m=Open<AdultRabbitMotionReview>(scenes[1]);Controller(m,m.reviewCamera,m.Home,m.KeyboardMove);
        var l=Open<RabbitHomeLifeReview>(scenes[2]);Controller(l,l.reviewCamera,l.Home,l.KeyboardMove);
        var f=Open<FarmStudyReview>(scenes[3]);Controller(f,f.reviewCamera,f.ResetCameraToPreset,f.KeyboardMove);
        f.FocusResident(2);var offset=Field<Vector3>(f,"followOffset");f.KeyboardMove(Vector2.right,.01f);
        Check(f.IsFollowing&&f.focus==2&&Vector3.Distance(offset,Field<Vector3>(f,"followOffset"))>.001f,"Follow offset");
        f.ShowOverview();Check(!f.IsFollowing&&Field<Vector3>(f,"followOffset")==Vector3.zero,"Follow Home reset");
        Set(f,"settingsOpen",true);var pos=f.reviewCamera.transform.position;f.KeyboardMove(Vector2.up,1);Check(pos==f.reviewCamera.transform.position,"Settings guard");Set(f,"settingsOpen",false);
        var picker=Field<LightingColorPicker>(f,"colorPicker");
        var lighting=UnityEngine.Object.FindObjectOfType<FarmLightingStudy>();Check(lighting,"Farm lighting missing");
        Set(picker,"lighting",lighting);f.KeyboardMove(Vector2.up,1);Check(pos==f.reviewCamera.transform.position,"Color modal guard");Set(picker,"lighting",null);
        var h=Open<HouseVillageReview>(scenes[4]);Controller(h,h.reviewCamera,()=>h.SetView(0),h.KeyboardMove);
        ReviewCameraBindingChecks.Verify();
        File.WriteAllText(Report,"v0.136 PASS: five saved review camera controllers, WASD/arrow equivalent and duplicate keys, opposite cancellation, diagonal normalization, rotated/overhead heading and zoom speed; position and pivot translate without height/rotation/zoom changes; 30/60/120fps, focus loss/regain, Home, animation pause/speed, farm follow offset/settings guard. Text/modal guard truth table checked. Existing v0.132 mouse/click/panel regression passed. Direct controller calls and synthetic key states, not OS keyboard/mouse input.\n");
        Debug.Log("REVIEW_KEYBOARD136_VERIFY_OK");
        AdultRabbitBuilder.BuildPlayer();AdultRabbitMotionBuilder.BuildPlayer();RabbitHomeLifeBuilder.BuildPlayer();ResidentOcclusionChecks.BuildReviewPlayer();
        Check(preserved.All(k=>Hash(k.Key)==k.Value),"Saved scene/art changed");
        File.AppendAllText(Report,"Four Windows players built; house camera checked in saved Unity scene; "+preserved.Count+" scene/source/FBX hashes preserved. No asset generation.\n");
        Debug.Log("REVIEW_KEYBOARD136_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
