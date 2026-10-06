using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife150VisualChecks
{
    static void Need(bool v,string why){if(!v)throw new Exception(why);}
    static void Render(Camera c,string name){var rt=new RenderTexture(1200,800,24);var old=c.targetTexture;var active=RenderTexture.active;c.targetTexture=rt;c.Render();c.Render();RenderTexture.active=rt;var t=new Texture2D(1200,800,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1200,800),0,0);t.Apply();File.WriteAllBytes("Docs/References/Garden150-"+name+".png",t.EncodeToPNG());c.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);}
    public static void Execute()
    {
        try{
            ShaderUtil.allowAsyncCompilation=false;
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();w.RestoreSnapshot(AutonomousSaveCodec.Decode(File.ReadAllText("Logs/Save150Natural/AutonomousLife142.json")));var s=w.Simulation;Need(s.Garden.stage==3,"No completed garden");
            var gv=w.gameObject.AddComponent<AutonomousGardenVisual>();gv.Configure();gv.Present(s);
            var pv=w.gameObject.AddComponent<AutonomousPoultryVisual>();pv.Configure(w);
            var sv=w.gameObject.AddComponent<AutonomousSeasonVisual>();sv.Configure();sv.Apply(new Vector4(1,0,0,0));w.Present();
            var full=w.CaptureSnapshot();full.paused=true;Directory.CreateDirectory("Logs/Save150Completed");File.WriteAllText("Logs/Save150Completed/AutonomousLife142.json",AutonomousSaveCodec.Encode(full));
            w.GetComponent<FarmStudyReview>().ResetCameraToPreset();var light=w.GetComponent<FarmLightingStudy>();light.Apply(1);var c=light.reviewCamera;Render(c,"whole");var pos=s.Garden.site;c.transform.position=pos+new Vector3(-3,3,-4);c.transform.LookAt(pos);Render(c,"complete");
            var soil=w.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Prepared garden ground");var bench=w.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Garden bench seat");
            Need(soil.sharedMaterial.HasProperty("_SnowAmount")&&bench.sharedMaterial.HasProperty("_SnowAmount"),"Garden season missing");sv.Apply(new Vector4(0,0,0,1));Need(soil.sharedMaterial.GetFloat("_SnowAmount")==1,"Garden winter snow");Render(c,"winter");
            var occ=w.GetComponent<FarmCameraOcclusion>();var original=bench.sharedMaterial;occ.Fade(bench.bounds.center,pos,1,false,false);Need(Mathf.Abs(bench.sharedMaterial.color.a-.25f)<.001f,"Bench inside fade");sv.Apply(new Vector4(1,0,0,0));occ.Fade(new Vector3(0,25,-30),Vector3.zero,1,false,false);Need(bench.sharedMaterial==original&&bench.sharedMaterial.color.a==1&&bench.sharedMaterial.GetFloat("_SnowAmount")==0,"Season/fade restore");
            s.Garden.stage=2;s.Garden.progress=30;gv.Present(s);Render(c,"working");s.Garden.stage=1;s.Garden.progress=0;gv.Present(s);Render(c,"stakes");w.RestoreSnapshot(full);gv.Present(w.Simulation);
            var d=w.GetComponent<FarmLifeDirector>();d.Playback.SetDayMinutes("7");d.Playback.SetRate(2);d.paused=true;float rate=d.Playback.Rate;w.Restart();Need(w.Simulation.residents.Length==6&&w.Simulation.Garden.stage==0&&w.Simulation.Field.stage==0&&!w.Simulation.Growth.stockpile&&d.paused&&d.Playback.DayMinutes==7&&d.Playback.Rate==rate,"New farm/reset controls");
            File.WriteAllText("Docs/AutonomousLife150VisualVerification.txt","Whole/close/stakes/working/complete/winter renders: PASS\nGarden soil and wood snow, inside25%, spring/opaque restore: PASS\nReset six residents/no garden, preserve playback: PASS\nActual UI and user quality confirmation remain separate.\n");Debug.Log("LIFE150_VISUAL_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
    public static void FinalChecks(){AutonomousLife150Supplementary.Recovery();Execute();}
    public static void Extended(){Execute();AutonomousLife150Supplementary.Continuation();}
    public static void StageBuild(){var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{AutonomousLifeBuilder.ScenePath},locationPathName="Logs/AutonomousLifePlayer150Staging/TinyDaysAutonomousLife.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});Need(r.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded,"Staged player failed");Debug.Log("LIFE150_FINAL_BUILD_OK");}
    public static void Build(){try{AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE150_FINAL_BUILD_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}}
}
