using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class ReviewCameraBindingChecks {
    const string AdultScene="Assets/Scenes/AdultRabbitStudy.unity",MotionScene="Assets/Scenes/AdultRabbitMotionStudy.unity",LifeScene="Assets/Scenes/RabbitHomeLifeStudy.unity";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message){if(!value)throw new Exception("Camera132: "+message);}
    static void Call(object owner,string method,params object[] args)=>owner.GetType().GetMethod(method,Flags).Invoke(owner,args);
    static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Flags).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Flags).SetValue(owner,value);
    static T Open<T>(string scene) where T:UnityEngine.Object {EditorSceneManager.OpenScene(scene);var r=UnityEngine.Object.FindObjectOfType<T>();Check(r,"Missing review "+scene);return r;}
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    static void DragCheck(Camera camera,Action reset,Action rotate,Action pan){
        reset();var rotation=camera.transform.rotation;rotate();Check(Quaternion.Angle(rotation,camera.transform.rotation)>1,"Rotation did not change direction");
        reset();rotation=camera.transform.rotation;var position=camera.transform.position;pan();Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"Pan changed direction");Check(Vector3.Distance(position,camera.transform.position)>.001f,"Pan did not translate");
    }
    public static void Verify(){
        Check(FarmStudyReview.RotateButton==0&&FarmStudyReview.PanButton==1&&FarmStudyReview.HeightDragButton==2&&FarmStudyReview.ClickButton==0,"Bindings");
        var a=Open<AdultRabbitReview>(AdultScene);
        DragCheck(a.reviewCamera,a.Home,()=>{Call(a,"RotateCamera",new Vector2(1,1));Call(a,"Apply");},()=>{Call(a,"PanCamera",new Vector2(40,20));Call(a,"Apply");});
        var h=Open<HouseVillageReview>(HouseVillageBuilder.ScenePath);
        DragCheck(h.reviewCamera,()=>h.SetView(0),()=>{Call(h,"RotateCamera",new Vector2(1,1));Call(h,"Apply",false);},()=>{Call(h,"PanCamera",new Vector2(40,20));Call(h,"Apply",false);});
        var m=Open<AdultRabbitMotionReview>(MotionScene);
        DragCheck(m.reviewCamera,m.Home,()=>m.CameraDrag(0,new Vector2(40,20)),()=>m.CameraDrag(1,new Vector2(40,20)));
        m.Home();var wheelPosition=m.reviewCamera.transform.position;
        m.PointerInput(new Vector2(20,20),Vector2.zero,0,0,1);
        Check(Vector3.Distance(wheelPosition,m.reviewCamera.transform.position)<.00001f,"Panel wheel zoomed camera");
        m.PointerInput(new Vector2(Screen.width-1,Screen.height/2),Vector2.zero,0,0,1);
        Check(Vector3.Distance(wheelPosition,m.reviewCamera.transform.position)>.001f,"Outside wheel did not zoom");
        foreach(int button in new[]{0,1,2}){
            m.Home();var p=m.reviewCamera.transform.position;var q=m.reviewCamera.transform.rotation;
            var outside=new Vector2(Screen.width-1,Screen.height/2);var panel=new Vector2(20,20);
            m.PointerInput(panel,Vector2.zero,1<<button,1<<button,0);m.PointerInput(outside,new Vector2(40,20),0,1<<button,0);
            Check(Vector3.Distance(p,m.reviewCamera.transform.position)<.00001f&&Quaternion.Angle(q,m.reviewCamera.transform.rotation)<.001f,"Panel-start drag leaked");
            m.PointerInput(outside,Vector2.zero,1<<button,1<<button,0);m.PointerInput(outside,new Vector2(40,20),0,1<<button,0);Check(m.CameraDragging,"Outside drag not active");
            m.PointerInput(panel,new Vector2(40,20),0,1<<button,0);Check(!m.CameraDragging,"Panel entry did not cancel");
            m.PointerInput(outside,Vector2.zero,1<<button,1<<button,0);m.PointerInput(outside,Vector2.zero,0,0,0);Check(!m.CameraDragging,"Release did not cancel");
            m.PointerInput(outside,Vector2.zero,1<<button,1<<button,0);Call(m,"OnApplicationFocus",false);Check(!m.CameraDragging,"Focus loss did not cancel");
            m.PointerInput(outside,Vector2.zero,1<<button,1<<button,0);m.Home();Check(!m.CameraDragging,"Home did not cancel");
        }
        var life=Open<RabbitHomeLifeReview>(LifeScene);
        DragCheck(life.reviewCamera,life.Home,()=>life.CameraDrag(0,new Vector2(40,20)),()=>life.CameraDrag(1,new Vector2(40,20)));
        var f=Open<FarmStudyReview>(FarmStudyBuilder.ScenePath);f.ResetCameraToPreset();f.selected=3;
        f.BeginPointer(new Vector2(10,200));f.MovePointer(new Vector2(15,200));Check(!Field<bool>(f,"rotatingCamera"),"5px jitter rotated");
        f.MovePointer(new Vector2(16,200));Check(Field<bool>(f,"rotatingCamera"),"6px did not start rotation");
        f.EndPointer(new Vector2(16,200),4);Check(f.selected==3&&!Field<bool>(f,"rotatingCamera"),"Drag selected resident or remained active");
        f.FocusResident(3);var follow=Field<Vector3>(f,"followOffset");var q0=f.reviewCamera.transform.rotation;
        f.PanPointer(new Vector2(40,20));Call(f,"ApplyCamera");Check(Field<Vector3>(f,"followOffset")!=follow&&f.IsFollowing&&f.selected==3,"Pan affected selection/follow");Check(Quaternion.Angle(q0,f.reviewCamera.transform.rotation)<.001f,"Farm pan rotated");
        f.ShowOverview();f.ClickResident(2,10);Check(f.selected==2&&!f.IsFollowing,"Click selection");f.ClickResident(2,10.2);Check(f.IsFollowing&&f.focus==2,"Double-click follow");
        f.BeginPointer(new Vector2(10,200));f.MovePointer(new Vector2(16,200));Call(f,"OnApplicationFocus",false);Check(!Field<bool>(f,"pointerHeld")&&!Field<bool>(f,"rotatingCamera"),"Farm focus loss");
        f.BeginPointer(new Vector2(10,200));f.MovePointer(new Vector2(16,200));f.ShowOverview();Check(!Field<bool>(f,"pointerHeld")&&!Field<bool>(f,"rotatingCamera"),"Farm Home/overview");
        EditorSceneManager.OpenScene(LifeScene);
        File.WriteAllText("Docs/ReviewCamera132Verification.txt","v0.132 editor checks PASS: shared left=orbit/right=pan/middle=existing height; five saved review scenes; orbit changes direction and pan preserves direction in motion/life/appearance/house; motion panel start/entry/release/focus/Home for three buttons; farm 5px jitter/6px drag, no selection after drag, right pan preserves follow/selection/direction, click/double-click and focus/overview reset. Direct calls, not OS mouse input.\n");
        Debug.Log("REVIEW_CAMERA132_VERIFY_OK");
    }
    public static void Execute(){try{
        var scenes=new[]{AdultScene,MotionScene,LifeScene,FarmStudyBuilder.ScenePath,HouseVillageBuilder.ScenePath}.ToDictionary(p=>p,Hash);
        Verify();
        AdultRabbitBuilder.BuildPlayer();AdultRabbitMotionBuilder.BuildPlayer();RabbitHomeLifeBuilder.BuildPlayer();ResidentOcclusionChecks.BuildReviewPlayer();
        Check(scenes.All(k=>Hash(k.Key)==k.Value),"Saved scene changed during verification/build");
        File.AppendAllText("Docs/ReviewCamera132Verification.txt","Four Windows players built successfully; five saved scene hashes preserved.\n");
        Debug.Log("REVIEW_CAMERA132_COMPLETE_OK");
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
