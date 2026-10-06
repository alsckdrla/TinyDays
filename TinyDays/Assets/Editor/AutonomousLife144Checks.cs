using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Life;
using TinyDays.Review;

public static class AutonomousLife144Checks
{
    static readonly List<string> rows=new List<string>();
    static void Check(bool b,string message){if(!b)throw new Exception(message);}
    static void Tick(AutonomousSimulation s,float seconds,float hour=12){for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){s.Tick(.05f,hour);s.Validate();}}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();world.Initialize();world.GetComponent<FarmStudyReview>().ResetCameraToPreset();
            var clock=world.GetComponent<FarmLightingStudy>();var director=world.GetComponent<FarmLifeDirector>();
            var weather=new AutonomousWeather();weather.Advance(120);Check(weather.Kind==LifeWeather.Cloudy,"Clear boundary");weather.Advance(30);Check(weather.Kind==LifeWeather.Rain,"Rain boundary");weather.Advance(90);Check(weather.Kind==LifeWeather.Cloudy&&weather.State.phase==3,"Recovery cloud");weather.Advance(30);Check(weather.Kind==LifeWeather.Clear,"Loop");
            weather.Select(LifeWeather.Rain);weather.Advance(300);Check(weather.Kind==LifeWeather.Rain&&!weather.State.automatic,"Fixed rain");weather.Resume();weather.Advance(90);Check(weather.State.phase==3,"Resume phase");rows.Add("Weather cycle/fixed/resume: PASS");
            world.SelectWeather(2);var initial=world.CaptureSnapshot();director.paused=true;world.Advance(4);Check(JsonUtility.ToJson(initial)==JsonUtility.ToJson(world.CaptureSnapshot()).Replace("\"paused\":true","\"paused\":false"),"Paused weather changed");director.paused=false;
            world.Restart();clock.Apply(1);director.Playback.SetRate(3);world.Advance(10);Check(Mathf.Abs(world.Weather.State.elapsed-40)<.01f&&clock.Hour==12,"Weather follows speed independent of fixed light");director.Playback.SetRate(1); // 1x index
            rows.Add("Pause,4x, fixed lighting independence: PASS");
            Func<AutonomousSimulation> fresh=()=>new AutonomousSimulation(new LifeTuning(),world.definitions.Concat(AutonomousWeather.Shelters(world)).ToArray(),world.starts,new AutonomousNavigation(world.land,world.obstacles));
            var sim=fresh();sim.Raining=true;Tick(sim,45);Check(sim.residents.All(r=>r.action==LifeAction.Shelter&&!r.Moving),"All three must shelter");
            sim.Raining=false;Tick(sim,70);Check(sim.residents.All(r=>r.action!=LifeAction.Shelter)&&sim.produced>0,"Rain release/resume production");rows.Add("Three shelter reservations,clear resumes production: PASS");
            foreach(var action in new[]{LifeAction.Plant,LifeAction.Harvest,LifeAction.Gather,LifeAction.Deliver}){
                sim=fresh();bool found=false;for(int i=0;i<5000;i++){sim.Tick(.05f,12);if(sim.residents.Any(r=>r.action==action)){found=true;break;}}
                Check(found,"Action missing "+action);int before=sim.residents.Sum(r=>r.completed);sim.Raining=true;Tick(sim,75);Check(sim.residents.Sum(r=>r.completed)>before&&sim.residents.All(r=>r.cargo==0),"Rain interrupted work/cargo "+action);rows.Add("Rain during "+action+": PASS");
            }
            sim=fresh();sim.food=0;sim.consumed=sim.tuning.initialFood;sim.Raining=true;Tick(sim,120);Check(sim.produced>0&&sim.food>=4,"Rain shortage recovery");rows.Add("Food shortage during rain: PASS");
            sim=fresh();sim.Raining=true;var shelterBounds=new Bounds(sim.sites.First(s=>s.kind==SiteKind.Shelter).position,Vector3.one*.2f);foreach(var shelter in sim.sites.Where(s=>s.kind==SiteKind.Shelter))shelterBounds.Encapsulate(shelter.position);shelterBounds.Expand(.4f);sim.navigation.TemporaryBlock=shelterBounds;
            // Start outside the blocked home strip; blocked destinations must not teleport residents.
            for(int i=0;i<3;i++)sim.residents[i].position=sim.sites.First(s=>s.id=="rest-"+i).position;
            Tick(sim,20);Check(!sim.residents.Any(r=>r.action==LifeAction.Shelter&&!r.Moving),"Blocked shelter entered");sim.navigation.TemporaryBlock=null;Tick(sim,90);Check(sim.residents.Any(r=>r.action==LifeAction.Shelter&&!r.Moving),"Shelter recovery failed");Tick(sim,80,22);Check(sim.residents.All(r=>r.sleeping),"Night priority");rows.Add("Blocked shelters recover/night returns: PASS");
            world.Initialize();world.GetComponent<FarmStudyReview>().ResetCameraToPreset();world.SelectWeather(2);world.Advance(45);var saved=world.CaptureSnapshot();var encoded=AutonomousSaveCodec.Encode(saved);world.RestoreSnapshot(AutonomousSaveCodec.Decode(encoded));Check(JsonUtility.ToJson(saved)==JsonUtility.ToJson(world.CaptureSnapshot()),"Rain save mismatch");world.Advance(2);world.Simulation.Validate();rows.Add("Rain action/weather/ownership save exact: PASS");
            // Generate a genuine old-layout snapshot without using any player save.
            world.Initialize();var legacy=new AutonomousSimulation(world.tuning,world.definitions,world.starts,new AutonomousNavigation(world.land,world.obstacles));Tick(legacy,34);
            var old=world.CaptureSnapshot();old.sourceVersion=142;old.weather=null;old.simulation=legacy.CaptureSave();old.sampleTime=legacy.elapsed;
            string oldJson=AutonomousSaveCodec.Encode(old);world.RestoreSnapshot(AutonomousSaveCodec.Decode(oldJson));var migrated=world.CaptureSnapshot();
            Check(migrated.weather.phase==0&&migrated.weather.automatic&&migrated.simulation.sites.Length==old.simulation.sites.Length+3,"Legacy extension");

            for(int i=0;i<legacy.residents.Length;i++)Check(JsonUtility.ToJson(migrated.simulation.residents[i])==JsonUtility.ToJson(old.simulation.residents[i]),"Legacy action reset");
            string directory="Logs/Save144Checks-"+DateTime.UtcNow.Ticks;Directory.CreateDirectory(directory);var repo=new AutonomousSaveRepository(directory,d=>world.ValidateSnapshot(d));File.WriteAllText(repo.Path,oldJson);repo.Write(migrated);Check(File.ReadAllText(repo.Path+".v142")==oldJson,"Legacy archive missing");repo.Write(migrated);Check(File.ReadAllText(repo.Path+".v142")==oldJson,"Legacy archive overwritten");rows.Add("142 migration preserves progress/sites; original archive: PASS");
            var broken=AutonomousSaveCodec.Decode(oldJson);broken.simulation.counts[0]=-1;bool rejected=false;try{world.RestoreSnapshot(broken);}catch{rejected=true;}Check(rejected,"Invalid migration accepted");Check(JsonUtility.ToJson(world.CaptureSnapshot())==JsonUtility.ToJson(migrated),"Failed migration changed world");rows.Add("Invalid migration rejected without mutation: PASS");
            world.Initialize();director.paused=false;director.Playback.SetRate(1);
            for(int i=0;i<36000;i++){world.Advance(.05f);if(i%20==0)world.Simulation.Validate();}
            Check(world.Simulation.consumed>0&&world.Simulation.homeArrivals>=9,"Weather multi-day cycle");rows.Add("Accelerated three-day weather/life/ownership regression: PASS");
            File.WriteAllLines("Docs/AutonomousLife144Verification.txt",rows);Debug.Log("LIFE144_VERIFY_OK");AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE144_COMPLETE_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
