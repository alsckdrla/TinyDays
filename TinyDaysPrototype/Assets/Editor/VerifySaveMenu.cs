using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays;

// Exercises runtime pointer handling, not a separate EditorWindow.
public static class VerifySaveMenu
{
    const string Active="TinyDaysSaveMenuVerification";
    const string Native="TinyDaysNativeMenuVerification";
    static int frames;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [InitializeOnLoadMethod] static void Register() { EditorApplication.update+=Tick; }
    public static void Execute()
    {
        EditorSceneManager.OpenScene(BuildDiorama.ScenePath);
        SessionState.SetBool(Active,true); EditorApplication.EnterPlaymode();
    }
    public static void PrepareNative()
    {
        EditorSceneManager.OpenScene(BuildDiorama.ScenePath);
        SessionState.SetBool(Native,true); EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(SessionState.GetBool(Native,false)&&EditorApplication.isPlaying&&GameObject.Find("GeneratedVillage"))
        {
            var saver=GameObject.Find("GeneratedVillage").GetComponent<VillageSaveSystem>();
            saver.verificationDirectory=Path.GetFullPath("Logs/NativeMenu-"+Guid.NewGuid().ToString("N"));
            SessionState.SetBool(Native,false);
            Debug.Log("Native menu verification saves: "+saver.verificationDirectory);
        }
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||++frames<80) return;
        SessionState.SetBool(Active,false);
        try
        {
            var root=GameObject.Find("GeneratedVillage"); var saver=root.GetComponent<VillageSaveSystem>();
            saver.verificationDirectory=Path.GetFullPath("Logs/SaveMenu-"+Guid.NewGuid().ToString("N"));
            var time=root.GetComponent<VillageTimeControls>(); time.SetSpeed(2); time.SetPaused(false);
            Invoke(saver,"SetMenu",true);
            var camera=Camera.main; var savedPosition=camera.transform.position;
            var resources=root.GetComponent<VillageResources>(); int food=resources.food;
            Click(saver,150,57);
            string file=Path.Combine(saver.verificationDirectory,"tiny-days-save.json");
            Require(File.Exists(file),"Pointer click writes save while paused");
            Require(Message(saver)=="마을을 저장했습니다","Save feedback");
            Require(!JsonUtility.FromJson<VillageSaveData>(File.ReadAllText(file)).paused,"Menu pause not persisted");
            camera.transform.position+=Vector3.right*2; resources.food=0;
            Click(saver,150,93);
            Require(Message(saver)=="저장한 마을을 불러왔습니다","Load feedback");
            Require(Vector3.Distance(camera.transform.position,savedPosition)<.001f&&resources.food==food,"Camera and food restored");
            Require(time.paused&&time.speedIndex==2,"Load remains paused in menu");
            var ambient=root.GetComponentInChildren<AmbientAudioSettings>();
            Pointer(saver,30,150,true,true,false); Pointer(saver,400,150,false,true,false);
            Require(ambient.volume==1,"Drag clamps maximum outside track");
            Pointer(saver,-50,150,false,false,true); Require(ambient.volume==0,"Drag clamps minimum");
            Pointer(saver,150,182,false,false,true); Invoke(saver,"Update");
            Require(saver.MenuOpen,"Release without press ignored");
            Pointer(saver,150,182,true,true,false); Pointer(saver,0,0,false,false,true); Invoke(saver,"Update");
            Require(saver.MenuOpen,"Release outside cancels button");
            Pointer(saver,150,182,true,true,false); Invoke(saver,"OnApplicationFocus",false);
            Pointer(saver,150,182,false,false,true); Invoke(saver,"Update");
            Require(saver.MenuOpen,"Focus loss cancels press");
            Click(saver,150,182);
            Require(!saver.MenuOpen&&!time.paused&&Time.timeScale==4,"Continue resumes 4x");
            time.SetPaused(true); Invoke(saver,"SetMenu",true); Click(saver,150,182);
            Require(time.paused,"Previously paused remains paused");
            File.WriteAllText("Docs/SaveMenuVerification.txt","PASS: runtime pointer state machine; save/load feedback, camera/food restoration, menu pause, 4x resume, prior pause, slider clamps, release outside, release without press, focus loss.\nIsolated saves under Logs; player save untouched.\nNative Game view mouse input is a separate verification.\n"+DateTime.Now.ToString("O"));
            Debug.Log("TINYDAYS_SAVE_MENU_OK"); EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); File.WriteAllText("Docs/SaveMenuVerification.txt","FAILED\n"+e); EditorApplication.Exit(1); }
    }
    static void Pointer(VillageSaveSystem saver,float x,float y,bool down,bool held,bool up) => Invoke(saver,"ProcessMenuPointer",new Vector2(x,y),down,held,up);
    static void Click(VillageSaveSystem saver,float x,float y)
    {
        Pointer(saver,x,y,true,true,false); Pointer(saver,x,y,false,false,true); Invoke(saver,"Update");
    }
    static string Message(VillageSaveSystem value) => (string)typeof(VillageSaveSystem).GetField("message",Private).GetValue(value);
    static void Invoke(VillageSaveSystem value,string method,params object[] args) => typeof(VillageSaveSystem).GetMethod(method,Private).Invoke(value,args);
    static void Require(bool condition,string text) { if(!condition) throw new Exception(text); }
}
