using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife149VisualChecks
{
    static void Need(bool value,string why){if(!value)throw new Exception(why);}
    static void Render(Camera c,string name){var rt=new RenderTexture(1200,800,24);var old=c.targetTexture;var active=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1200,800,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1200,800),0,0);t.Apply();File.WriteAllBytes("Docs/References/Field149-"+name+".png",t.EncodeToPNG());c.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();w.RestoreSnapshot(AutonomousSaveCodec.Decode(File.ReadAllText("Logs/Save149ShortageFixture/AutonomousLife142.json")));
            var s=w.Simulation;for(int i=0;i<16000&&s.Field.stage<3;i++)s.Tick(.05f,12);Need(s.Field.stage==3,"No completed field");
            bool planted=false,harvested=false;for(int i=0;i<12000;i++){s.Tick(.05f,12);if(i%20==0)s.Validate();foreach(var r in s.residents.Where(r=>r.site>=0&&s.sites[r.site].id.StartsWith(FieldLayout.Prefix))){planted|=r.action==LifeAction.Plant;harvested|=r.action==LifeAction.Harvest;}if(planted&&harvested)break;}
            var full=w.CaptureSnapshot();full.paused=true;Directory.CreateDirectory("Logs/Save149Completed");File.WriteAllText("Logs/Save149Completed/AutonomousLife142.json",AutonomousSaveCodec.Encode(full));
            var gv=w.gameObject.AddComponent<AutonomousGrowthVisual>();gv.Configure(w,GrowthLayout.Create(w));gv.Present(s);
            var fv=w.gameObject.AddComponent<AutonomousFieldVisual>();fv.Configure(w);fv.Present(s);
            var pv=w.gameObject.AddComponent<AutonomousPoultryVisual>();pv.Configure(w);
            var sv=w.gameObject.AddComponent<AutonomousSeasonVisual>();sv.Configure();sv.Apply(new Vector4(1,0,0,0));w.Present();
            w.GetComponent<FarmStudyReview>().ResetCameraToPreset();var light=w.GetComponent<FarmLightingStudy>();light.Apply(1);var c=light.reviewCamera;Render(c,"whole");var pos=s.Field.site;c.transform.position=pos+new Vector3(-3,3,-4);c.transform.LookAt(pos);Render(c,"complete");
            var soil=w.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Cultivated soil");Need(soil.sharedMaterial.HasProperty("_SnowAmount"),"Field soil season missing");sv.Apply(new Vector4(0,0,0,1));Need(soil.sharedMaterial.GetFloat("_SnowAmount")==1,"Field winter snow");Render(c,"winter");
            var occ=w.GetComponent<FarmCameraOcclusion>();var original=soil.sharedMaterial;occ.Fade(pos+Vector3.up*.045f,pos,1,false,false);Need(Mathf.Abs(soil.sharedMaterial.color.a-.25f)<.001f,"Field inside fade");sv.Apply(new Vector4(1,0,0,0));occ.Fade(new Vector3(0,25,-30),Vector3.zero,1,false,false);Need(soil.sharedMaterial==original&&soil.sharedMaterial.color.a==1&&soil.sharedMaterial.GetFloat("_SnowAmount")==0,"Field season/fade restore");
            s.Field.stage=2;s.Field.progress=30;fv.Present(s);Render(c,"digging");s.Field.stage=1;s.Field.progress=0;fv.Present(s);Render(c,"stakes");w.RestoreSnapshot(full);fv.Present(w.Simulation);
            var d=w.GetComponent<FarmLifeDirector>();d.Playback.SetDayMinutes("7");d.Playback.SetRate(2);d.paused=true;float rate=d.Playback.Rate;w.Restart();Need(w.Simulation.Field.stage==0&&w.Simulation.Field.progress==0&&!w.Simulation.Growth.stockpile&&d.paused&&d.Playback.DayMinutes==7&&d.Playback.Rate==rate,"New farm reset/playback");
            File.WriteAllText("Docs/AutonomousLife149VisualVerification.txt","Whole/close/stakes/digging/complete/winter renders: PASS\nSoil snow, inside 25%, spring/opaque restoration: PASS\nNew farm resets field and preserves playback: PASS\nActual window/user quality confirmation is separate.\n");Debug.Log("LIFE149_VISUAL_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
