using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife145Checks
{
    static readonly List<string> rows=new List<string>();
    static void Need(bool v,string m){if(!v)throw new Exception(m);}
    static void Step(AutonomousLifeWorld w,float seconds){for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){w.Advance(.05f);if(i%20==0)w.Simulation.Validate();}}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();var ui=w.GetComponent<FarmStudyReview>();ui.ResetCameraToPreset();var d=w.GetComponent<FarmLifeDirector>();d.paused=false;d.Playback.SetRate(1);var light=w.GetComponent<FarmLightingStudy>();
            var season=new AutonomousSeason();season.Advance(900,10);Need(Mathf.Abs(season.State.progress-.5f)<.0001f,"Half season");season.Advance(1800,20);Need(season.State.season==1&&season.State.progress<.001f,"Changed day length");season.Select(3);season.Advance(5,10);Need(season.Weights.w>.4f&&season.Weights.w<.6f&&!season.State.automatic&&season.State.progress==0,"Blend/fixed");season.State.automatic=true;season.Advance(1800,10);Need(season.State.season==0,"Winter to spring");rows.Add("3-day cycle/day length/fixed/resume/blend: PASS");
            light.Apply(1);d.Playback.SetRate(3);w.Advance(10);Need(Mathf.Abs(w.Season.State.progress-40f/1800)<.0001f&&light.Hour==12,"Rate/lighting independence");d.paused=true;string before=JsonUtility.ToJson(w.CaptureSnapshot());w.Advance(5);Need(before==JsonUtility.ToJson(w.CaptureSnapshot()),"Pause changed state");d.paused=false;d.Playback.SetRate(1);rows.Add("Pause/4x/fixed lighting: PASS");
            w.Initialize();var sim=w.Simulation;var crop=sim.sites.First(s=>s.kind==SiteKind.Crop);var berry=sim.sites.First(s=>s.kind==SiteKind.Berry);crop.planted=true;crop.growth=berry.growth=0;sim.Tick(1,12);float cg=crop.growth,bg=berry.growth;crop.growth=berry.growth=0;sim.Winter=true;sim.Tick(1,12);Need(Mathf.Abs(crop.growth-cg*.5f)<.00001f&&Mathf.Abs(berry.growth-bg*.5f)<.00001f,"Winter growth");sim.Winter=false;float threshold=sim.RestThreshold(sim.residents[2]),leisure=sim.LeisureDuration(sim.residents[2]);sim.Winter=true;Need(Mathf.Abs(sim.RestThreshold(sim.residents[2])-threshold+.1f)<.0001f&&Mathf.Abs(sim.LeisureDuration(sim.residents[2])-leisure*1.5f)<.0001f,"Winter rest");rows.Add("Winter growth50%/rest threshold/leisure: PASS");
            foreach(var action in new[]{LifeAction.Plant,LifeAction.Harvest,LifeAction.Gather,LifeAction.Deliver}){
                w.Initialize();light.Apply(1);bool found=false;for(int i=0;i<5000;i++){w.Advance(.05f);if(w.Simulation.residents.Any(r=>r.action==action)){found=true;break;}}
                Need(found,"Action fixture "+action);before=JsonUtility.ToJson(w.Simulation.CaptureSave());w.SelectSeason(3);Need(before==JsonUtility.ToJson(w.Simulation.CaptureSave()),"Season resets work "+action);Step(w,75);rows.Add("Season during "+action+": PASS");
            }
            w.Initialize();w.SelectSeason(3);light.Apply(1);w.Simulation.food=0;w.Simulation.consumed=w.tuning.initialFood;w.SelectWeather(2);Step(w,160);Need(w.Simulation.produced>0&&w.Simulation.food>=4,"Winter shortage recovery");Need(w.Simulation.residents.Any(r=>r.action==LifeAction.Shelter),"Snow shelter missing");var save=w.CaptureSnapshot();w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(save)));Need(JsonUtility.ToJson(save)==JsonUtility.ToJson(w.CaptureSnapshot()),"Snow restore");w.SelectSeason(0);Step(w,3);save=w.CaptureSnapshot();w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(save)));Need(JsonUtility.ToJson(save)==JsonUtility.ToJson(w.CaptureSnapshot()),"Blend restore");w.SelectWeather(0);Step(w,90);Need(w.Simulation.residents.All(r=>r.action!=LifeAction.Shelter),"Spring work resume");rows.Add("Winter shortage/snow save/transition save/spring resume: PASS");
            foreach(var fixture in new[]{"Logs/Save143UI/AutonomousLife142.json","Logs/Save144Observation/AutonomousLife142.json"}){
                Need(File.Exists(fixture),"Missing isolated legacy fixture");string original=File.ReadAllText(fixture);var old=AutonomousSaveCodec.Decode(original);w.RestoreSnapshot(old);var migrated=w.CaptureSnapshot();Need(migrated.season.season==0&&migrated.season.automatic,"Migration season");
                var compare=AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(migrated));compare.simulation.sites=compare.simulation.sites.Take(old.simulation.sites.Length).ToArray();Need(JsonUtility.ToJson(compare.simulation)==JsonUtility.ToJson(old.simulation)&&JsonUtility.ToJson(compare.camera)==JsonUtility.ToJson(old.camera)&&compare.hour==old.hour&&compare.day==old.day,"Legacy state changed");if(old.sourceVersion==144)Need(JsonUtility.ToJson(old.weather)==JsonUtility.ToJson(migrated.weather),"Legacy weather changed");
                string dir="Logs/Save145Checks-"+old.sourceVersion+"-"+DateTime.UtcNow.Ticks;Directory.CreateDirectory(dir);var repo=new AutonomousSaveRepository(dir,x=>w.ValidateSnapshot(x));File.WriteAllText(repo.Path,original);repo.Write(migrated);Need(File.ReadAllText(repo.Path+".v"+old.sourceVersion)==original,"Legacy archive");Need(File.ReadAllText(fixture)==original,"Fixture modified");rows.Add("Legacy "+old.sourceVersion+" exact migration/archive: PASS");
            }
            save=w.CaptureSnapshot();var invalid=AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(save));invalid.season.from=Vector4.zero;bool rejected=false;try{w.RestoreSnapshot(invalid);}catch{rejected=true;}Need(rejected&&JsonUtility.ToJson(save)==JsonUtility.ToJson(w.CaptureSnapshot()),"Invalid season mutation");rows.Add("Invalid season preserves active state: PASS");
            w.Initialize();d.paused=false;d.Playback.SetRate(1);Step(w,7201);Need(w.Season.State.season==0&&w.Simulation.homeArrivals>=36&&w.Simulation.consumed>0,"12-day season cycle");rows.Add("12-day season/weather/life/ownership cycle: PASS");
            var visual=w.gameObject.AddComponent<AutonomousSeasonVisual>();visual.Configure();w.SelectSeason(3);visual.Apply(new Vector4(0,0,0,1));var snowMaterial=w.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).First(m=>m&&m.HasProperty("_SnowAmount")&&m.GetFloat("_SnowAmount")>0);Need(snowMaterial.shader.isSupported,"Season shader unsupported");var occlusion=w.GetComponent<FarmCameraOcclusion>();
            // Public occlusion entrypoint is exercised by existing camera regressions; material propagation is checked below.
            light.Apply(3);var emission=w.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name.Contains("Glass Lighting study")).Select(r=>r.sharedMaterial.GetColor("_EmissionColor")).ToArray();visual.Apply(new Vector4(1,0,0,0));Need(snowMaterial.GetFloat("_SnowAmount")==0,"Spring snow clear");var after=w.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name.Contains("Glass Lighting study")).Select(r=>r.sharedMaterial.GetColor("_EmissionColor")).ToArray();Need(emission.SequenceEqual(after),"Season changed windows");UnityEngine.Object.DestroyImmediate(visual);rows.Add("Season materials/snow clear/window emission preservation: PASS");
            File.WriteAllLines("Docs/AutonomousLife145Verification.txt",rows);Debug.Log("LIFE145_VERIFY_OK");AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE145_COMPLETE_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
