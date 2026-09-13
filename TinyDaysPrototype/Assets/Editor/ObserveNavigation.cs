using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs normal Editor Play Mode in real time, independently of the accelerated verification loop.
[InitializeOnLoad]
public static class ObserveNavigation
{
    const string Key="TinyDaysObserveNavigation";
    static double started=-1, next;
    static int captures, errors;
    static ObserveNavigation()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(message,trace,type)=> { if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)) errors++; };
    }
    public static void Execute()
    {
        EditorSceneManager.OpenScene(BuildDiorama.ScenePath);
        SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying) return;
        if(started<0) { started=EditorApplication.timeSinceStartup; next=started+.1; }
        double now=EditorApplication.timeSinceStartup;
        if(now>=next && Camera.main)
        {
            Directory.CreateDirectory("Logs/NavigationFrames");
            Capture(Camera.main,"Logs/NavigationFrames/Live_"+(captures++).ToString("0000")+".png"); next+=.1;
        }
        if(now-started<30) return;
        File.WriteAllText("Docs/NavigationObservation.txt","Normal Editor Play Mode observation: "+(now-started).ToString("F1")+" wall seconds; "+captures+" camera captures; runtime errors: "+errors+".\nKeyboard interaction and user visual acceptance remain manual.\n"+DateTime.Now.ToString("O"));
        SessionState.SetBool(Key,false); EditorApplication.Exit(errors==0?0:1);
    }
    public static void Capture(Camera camera,string path)
    {
        var rt=RenderTexture.GetTemporary(1280,720,24); var previous=RenderTexture.active; var target=camera.targetTexture;
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); }
        finally { camera.targetTexture=target; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(texture); }
    }
}
