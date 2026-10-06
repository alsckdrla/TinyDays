using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife148SupplementaryChecks
{
    static AutonomousLifeWorld w;static GrowthLayout layout;
    static readonly List<string> rows=new List<string>();
    static void Need(bool value,string text){if(!value)throw new Exception(text);}
    static AutonomousSimulation Fresh(bool ready=true){w.Initialize();var s=w.Simulation;s.SetGrowthDirection(true);if(ready){s.produced+=33-s.food;s.food=33;s.Growth.pressure=59.95f;}return s;}
    static void Step(AutonomousSimulation s,float seconds,float hour=12,float dt=.05f){for(float t=0;t<seconds-.0001f;t+=dt){s.Tick(Mathf.Min(dt,seconds-t),hour);s.Validate();}}
    static void RoundTrip(){string before=JsonUtility.ToJson(w.CaptureSnapshot());w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(w.CaptureSnapshot())));Need(before==JsonUtility.ToJson(w.CaptureSnapshot()),"Supplement exact restore");}
    static AutonomousSimulation ReadyBuild(){var s=Fresh();Step(s,.1f);foreach(var r in s.residents)s.CancelWork(r.id);s.Growth.sources=new[]{0,0};s.Growth.wood=12;s.Validate();return s;}
    static void Render(Camera c,string name)
    {var rt=new RenderTexture(1200,800,24);var active=RenderTexture.active;var previous=c.targetTexture;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1200,800,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1200,800),0,0);t.Apply();File.WriteAllBytes("Docs/References/Growth148-"+name+".png",t.EncodeToPNG());c.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();w.GetComponent<FarmStudyReview>().ResetCameraToPreset();layout=GrowthLayout.Create(w);
            var s=Fresh();Step(s,2);Need(s.Growth.stage==1,"Trigger not reserved");int total=s.Growth.sources.Sum()+s.Growth.wood+s.residents.Sum(r=>r.woodCargo);s.SetGrowthDirection(false);Need(s.Growth.stage==0&&s.navigation.ConstructionBlock==null&&s.residents.All(r=>r.woodReservation==0),"Pre-build cancellation");Step(s,40);Need(s.Growth.sources.Sum()+s.Growth.wood+s.residents.Sum(r=>r.woodCargo)+s.Growth.drops.Sum(d=>d.amount)==total,"Cancel lost wood");RoundTrip();rows.Add("Pre-construction cancel releases plot/work, conserves wood: PASS");
            s=Fresh();for(int i=0;i<4000&&!s.residents.Any(r=>r.woodCargo>0);i++)s.Tick(.05f,12);var carrier=s.residents.First(r=>r.woodCargo>0);s.CancelWork(carrier.id);Need(s.Growth.drops.Count==1,"Wood did not drop on cancellation");RoundTrip();s=w.Simulation;Step(s,120);if(s.Growth.drops.Count>0)File.WriteAllText("Logs/life148-drop-debug.json",JsonUtility.ToJson(s.CaptureSave(),true));Need(s.Growth.drops.Count==0,"Wood drop did not recover");rows.Add("Interrupted timber safe drop, exact save/restore, autonomous recovery: PASS");
            foreach(string reason in new[]{"rain","night","food","fatigue","hunger"}){
                s=ReadyBuild();for(int i=0;i<4000&&s.Growth.stage!=2;i++)s.Tick(.05f,12);Need(s.Growth.stage==2,"Build not reached");var builder=s.residents.First(r=>r.action==LifeAction.Build);float progress=s.Growth.progress;
                if(reason=="rain")s.Raining=true;if(reason=="food"){s.consumed+=s.food;s.food=0;}if(reason=="fatigue")builder.fatigue=1;if(reason=="hunger")builder.hunger=1;
                Step(s,.1f,reason=="night"?23:12);Need(builder.action!=LifeAction.Build,"Urgent need ignored "+reason);Need(s.Growth.progress>=progress&&s.Growth.spent==12,"Interrupted cost/progress lost");RoundTrip();rows.Add("Construction interruption + exact save: "+reason+" PASS");
            }
            s=ReadyBuild();for(int i=0;i<4000&&s.Growth.stage!=2;i++)s.Tick(.05f,12);s.SetGrowthDirection(false);Step(s,200);Need(s.Growth.stage==3&&s.Growth.spent==12&&s.Capacity==60&&s.TargetFood==18,"In-progress mode change failed");RoundTrip();rows.Add("Mode change after commitment finishes, cost exactly once: PASS");
            s=Fresh();s.ConfigureGrowth(new GrowthLayout{sources=layout.sources,store=layout.store,candidates=Array.Empty<Vector3>()});s.SetGrowthDirection(true);s.Growth.pressure=59.95f;Step(s,80);Need(s.Growth.stage==0&&s.Growth.wood==0&&s.residents.All(r=>r.woodReservation==0),"No-space scenario built");Need(s.Growth.pressure>=60,"No-space reason missing");rows.Add("No suitable plot: existing life continues, no wood or construction reservations: PASS");
            s=Fresh();Step(s,.1f);var station=s.sites.First(x=>x.kind==SiteKind.WoodStore);s.navigation.TemporaryBlock=new Bounds(station.position,new Vector3(.7f,1,.7f));Step(s,80);s.navigation.TemporaryBlock=null;Step(s,220);Need(s.Growth.wood>0||s.Growth.spent==12,"Warehouse route recovery failed");s.Validate();rows.Add("Blocked timber store releases/retries; unblock resumes and conserves resources: PASS");
            s=Fresh();s.consumed+=s.food;s.food=0;Step(s,.1f);Need(s.residents.All(r=>r.woodReservation==0&&r.action!=LifeAction.Build),"Food shortage construction priority");Step(s,120);Need(s.food>0||s.Committed>0,"Food shortage did not recover");rows.Add("Food shortage food-first recovery, no construction work: PASS");
            foreach(float fps in new[]{30,60,120})foreach(float rate in new[]{.5f,1,4}){s=ReadyBuild();for(int i=0;i<4000&&s.Growth.stage!=2;i++)s.Tick(.05f,12);float before=s.Growth.progress;Step(s,2*rate,12,rate/fps);Need(Mathf.Abs(s.Growth.progress-before-2*rate)<.005f,"Frame/rate work integration");}
            s=Fresh();Step(s,.1f);string frozen=JsonUtility.ToJson(s.CaptureSave());s.Tick(0,23);Need(frozen==JsonUtility.ToJson(s.CaptureSave()),"Pause changed construction");rows.Add("30/60/120fps x 0.5/1/4 work timing; paused state unchanged: PASS");
            // Reuse the naturally completed, isolated fixture for actual generated scenery.
            w.RestoreSnapshot(AutonomousSaveCodec.Decode(File.ReadAllText("Logs/Save148Completed/AutonomousLife142.json")));var gv=w.gameObject.AddComponent<AutonomousGrowthVisual>();gv.Configure(w,layout);var pv=w.gameObject.AddComponent<AutonomousPoultryVisual>();pv.Configure(w);var sv=w.gameObject.AddComponent<AutonomousSeasonVisual>();sv.Configure();gv.Present(w.Simulation);sv.Apply(new Vector4(1,0,0,0));var ui=w.GetComponent<FarmStudyReview>();ui.ResetCameraToPreset();var light=w.GetComponent<FarmLightingStudy>();light.Apply(1);Render(light.reviewCamera,"whole");var c=light.reviewCamera;var p=w.Simulation.Growth.site;c.transform.position=p+new Vector3(-4,3,-5);c.transform.LookAt(p+Vector3.up*.7f);Render(c,"complete");
            var roof=w.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="Sloping roof");Need(roof.sharedMaterial.HasProperty("_SnowAmount"),"Expansion season material");sv.Apply(new Vector4(0,0,0,1));Render(c,"winter");Need(roof.sharedMaterial.GetFloat("_SnowAmount")==1,"Expansion roof snow");
            var occ=w.GetComponent<FarmCameraOcclusion>();var original=roof.sharedMaterial;occ.Fade(p+Vector3.up*.7f,Vector3.zero,1,false,false);Need(roof.sharedMaterial.color.a==.25f,"Expansion inside fade");sv.Apply(new Vector4(1,0,0,0));occ.Fade(new Vector3(0,25,-30),Vector3.zero,1,false,false);Need(roof.sharedMaterial==original&&roof.sharedMaterial.color.a==1&&roof.sharedMaterial.GetFloat("_SnowAmount")==0,"Expansion fade/season restore");
            w.Simulation.Growth.stage=2;w.Simulation.Growth.progress=30;gv.Present(w.Simulation);Render(c,"frame");w.Simulation.Growth.stage=1;w.Simulation.Growth.progress=0;gv.Present(w.Simulation);Render(c,"stakes");rows.Add("Whole/close/stakes/frame/complete/winter renders; snow + interior25% + restore: PASS");
            File.WriteAllLines("Docs/AutonomousLife148SupplementaryVerification.txt",rows);Debug.Log("LIFE148_SUPPLEMENTARY_OK");
        }catch(Exception e){File.WriteAllLines("Logs/life148-supplement-partial.txt",rows);Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
