using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Life;
using TinyDays.Review;

public static class AutonomousLife147Checks
{
    static void Need(bool b,string message){if(!b)throw new Exception(message);}
    static AutonomousLifeWorld world;
    static AutonomousSimulation Fresh(){world.Initialize();return world.Simulation;}
    static void Step(AutonomousSimulation s,float seconds,float hour=12,float step=.05f){for(float t=0;t<seconds-.00001f;t+=step){s.Tick(Mathf.Min(step,seconds-t),hour);s.Validate();}}
    static void EggFixture(AutonomousSimulation s,int count){var p=s.Poultry;p.feedSpent=(count+2)/3;p.feedsEaten=p.laid=p.eggs=count;p.feed=p.feedSpent*3-count;s.food-=p.feedSpent;s.Validate();}
    static void RoundTrip(AutonomousSimulation s){string before=JsonUtility.ToJson(s.CaptureSave());var clone=new AutonomousSimulation(s.tuning,s.sites,world.starts,new AutonomousNavigation(world.land,world.RuntimeObstacles()));clone.RestoreSave(JsonUtility.FromJson<SimulationSaveData>(before));Need(before==JsonUtility.ToJson(clone.CaptureSave()),"Poultry/resident exact restore");}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();var rows=new List<string>();var s=Fresh();
            world.GetComponent<FarmStudyReview>().ResetCameraToPreset();
            Need(world.obstacles.All(b=>!PoultryLayout.Area.Intersects(b)),"Coop overlaps existing scenery");
            foreach(var site in s.sites)Need(s.navigation.Clear(site.position),"Station blocked "+site.id+" "+site.position);
            foreach(var site in PoultryLayout.Sites())Need(s.navigation.Find(world.starts[0],site.position)!=null,"Care station unreachable");
            rows.Add("Appended coop bounds, old stations, care routes: PASS");
            var observed=new HashSet<LifeAction>();
            for(int i=0;i<15000;i++){s.Tick(.05f,12);if(i%20==0)s.Validate();foreach(var r in s.residents)if((int)r.action>=12&&observed.Add(r.action))RoundTrip(s);if(s.Poultry.collected>=3)break;}
            Need(observed.Count==3&&s.Poultry.feedSpent>=1&&s.Poultry.laid>=3&&s.Poultry.collected>=3,"Autonomous feed/lay/collect incomplete "+JsonUtility.ToJson(s.Poultry));RoundTrip(s);
            rows.Add("Natural feed preparation/delivery/half-day eggs/collection + each action exact save: PASS at "+s.elapsed);
            // No care resident updates here: isolate hen timing and full-nest conservation.
            s=Fresh();foreach(var r in s.residents)r.wait=10000;EggFixture(s,6);
            s.Poultry.feedSpent++;s.food--;s.Poultry.feedsEaten+=3;foreach(var h in s.Poultry.hens){h.fed=true;h.progress=.99f;}
            Step(s,12);Need(s.Poultry.eggs==6&&s.Poultry.hens.All(h=>h.fed&&h.progress==1),"Full nest lost pending eggs");RoundTrip(s);
            s.Poultry.eggs-=3;s.Poultry.collected+=3;s.produced+=3;s.food+=3;Step(s,.1f);Need(s.Poultry.eggs==6&&s.Poultry.laid==9,"Full nest resume");rows.Add("Full nest pending production/no loss/recovery: PASS");
            s=Fresh();foreach(var r in s.residents)r.wait=10000;s.food--;s.Poultry.feedSpent=1;s.Poultry.feed=3;Step(s,.05f);float progress=s.Poultry.hens[0].progress;s.DayMinutes=20;Step(s,60);Need(Mathf.Abs(s.Poultry.hens[0].progress-progress-.1f)<.0001f,"Day length progress");
            s.Raining=true;Step(s,15);Need(s.Poultry.hens.All(h=>h.inside),"Rain shelter");RoundTrip(s);s.Raining=false;Step(s,15,23);Need(s.Poultry.hens.All(h=>h.inside),"Night shelter");Step(s,15,12);Need(s.Poultry.hens.All(h=>!h.inside&&h.position.z< -5.2f),"Clear/day return");rows.Add("Normalized half-day timing, day-length change, rain/night/return and indoor production: PASS");
            s=Fresh();EggFixture(s,3);int used=s.food;s.food=0;s.consumed+=used;Step(s,.1f);Need(s.residents.Any(r=>r.action==LifeAction.EggCollect)&&s.residents.All(r=>r.feedReservation==0),"Shortage egg priority / feed guard");Step(s,60);Need(s.Poultry.collected>0&&s.food>0,"Shortage recovery");rows.Add("Food zero ready-eggs priority and feed guard/recovery: PASS");
            s=Fresh();EggFixture(s,6);s.produced+=s.tuning.capacity-1-s.food;s.food=s.tuning.capacity-1;Step(s,.1f);Need(s.residents.Sum(r=>r.eggReservation)==1&&s.PlannedStock==s.tuning.capacity,"One remaining storage slot competition");RoundTrip(s);Step(s,70);rows.Add("Concurrent nest requests limited to remaining warehouse capacity: PASS");
            s=Fresh();for(int i=0;i<2000&&!s.residents.Any(r=>r.carryingFeed);i++)s.Tick(.05f,12);Need(s.residents.Any(r=>r.carryingFeed),"Feed carrier fixture");int carrier=s.residents.First(r=>r.carryingFeed).id;s.CancelWork(carrier);Need(s.bundles.Count==1&&s.residents[carrier].cargo==0,"Cancelled feed lost");RoundTrip(s);Step(s,100);Need(s.bundles.Count==0,"Dropped feed not recovered");rows.Add("Cancelled feed basket ownership and recovery: PASS");
            foreach(string interrupt in new[]{"hunger","fatigue","rain","night"}){
                s=Fresh();for(int i=0;i<2000&&!s.residents.Any(r=>r.carryingFeed);i++)s.Tick(.05f,12);var carrying=s.residents.First(r=>r.carryingFeed);
                if(interrupt=="hunger")carrying.hunger=1;if(interrupt=="fatigue")carrying.fatigue=1;if(interrupt=="rain")s.Raining=true;
                Step(s,.1f,interrupt=="night"?23:12);Need(!carrying.carryingFeed&&carrying.action!=LifeAction.FeedDeliver,"Urgent care did not release: "+interrupt);
                Step(s,75,interrupt=="night"?23:12);Need(carrying.cargo==0,"Urgent feed did not return: "+interrupt);RoundTrip(s);
            }
            rows.Add("Feed transport interrupted by hunger/fatigue/rain/night returns food without retry loop: PASS");
            s=Fresh();EggFixture(s,3);Step(s,.1f);var collecting=s.residents.First(r=>r.action==LifeAction.EggCollect);s.navigation.TemporaryBlock=new Bounds(PoultryLayout.Sites()[1].position,new Vector3(1,1,1));Step(s,35);Need(s.residents.All(r=>r.eggReservation==0),"Blocked nest reservation leaked");s.navigation.TemporaryBlock=null;Step(s,90);Need(s.Poultry.collected>0,"Unblocked nest did not recover");rows.Add("Blocked nest waits/releases, clear resumes within 90 seconds: PASS");
            foreach(string file in new[]{"Logs/Save143UI/AutonomousLife142.json","Logs/Save144Observation/AutonomousLife142.json","Logs/Save145Observation/AutonomousLife142.json","Logs/Save146UI/AutonomousLife142.json"}){
                string raw=File.ReadAllText(file);var old=AutonomousSaveCodec.Decode(raw);world.RestoreSnapshot(old);var now=world.CaptureSnapshot();var compare=JsonUtility.FromJson<SimulationSaveData>(JsonUtility.ToJson(now.simulation));compare.poultry=null;compare.sites=compare.sites.Take(old.simulation.sites.Length).ToArray();
                Need(JsonUtility.ToJson(compare)==JsonUtility.ToJson(old.simulation)&&JsonUtility.ToJson(now.camera)==JsonUtility.ToJson(old.camera)&&now.day==old.day&&now.hour==old.hour,"Legacy state lost "+old.sourceVersion);
                if(old.sourceVersion>=144)Need(JsonUtility.ToJson(old.weather)==JsonUtility.ToJson(now.weather),"Weather lost");if(old.sourceVersion>=145)Need(JsonUtility.ToJson(old.season)==JsonUtility.ToJson(now.season),"Season lost");
                string dir="Logs/Save147Legacy-"+old.sourceVersion+"-"+DateTime.UtcNow.Ticks;Directory.CreateDirectory(dir);var repo=new AutonomousSaveRepository(dir,x=>world.ValidateSnapshot(x));File.WriteAllText(repo.Path,raw);repo.Write(now);Need(File.ReadAllText(repo.Path+".v"+old.sourceVersion)==raw&&File.ReadAllText(file)==raw,"Legacy source not preserved");rows.Add("Legacy "+old.sourceVersion+" state/weather/season/camera/source archive: PASS");
            }
            var snapshot=world.CaptureSnapshot();string original=JsonUtility.ToJson(snapshot);snapshot.simulation.poultry.eggs=7;bool refused=false;try{world.RestoreSnapshot(snapshot);}catch{refused=true;}Need(refused&&JsonUtility.ToJson(world.CaptureSnapshot())==original,"Invalid poultry save altered active world");rows.Add("Malformed poultry save rejected atomically: PASS");
            foreach(float step in new[]{1f/30,1f/60,1f/120})foreach(float rate in new[]{.5f,1,4}){
                s=Fresh();foreach(var r in s.residents)r.wait=10000;s.food--;s.Poultry.feedSpent=1;s.Poultry.feed=3;Step(s,4*rate,12,step*rate);Need(Mathf.Abs(s.Poultry.hens[0].progress-4*rate/300)<.0001f,"Frame/rate hen timing");
            }
            s=Fresh();var d=world.GetComponent<FarmLifeDirector>();d.paused=true;string frozen=JsonUtility.ToJson(s.CaptureSave());world.Advance(1);Need(frozen==JsonUtility.ToJson(s.CaptureSave()),"Pause changed care");d.paused=false;d.Playback.SetRate(1);world.GetComponent<FarmLightingStudy>().Apply(1);float before=s.elapsed;world.Advance(.2f);Need(s.elapsed>before,"Locked lighting paused life");rows.Add("30/60/120fps x 0.5/1/4 rates, pause and lighting independence: PASS");
            s=Fresh();for(int i=0;i<144000;i++){float dt=.05f;world.Weather.Advance(dt);world.Season.Advance(dt,10);s.Raining=world.Weather.Kind==LifeWeather.Rain;s.Winter=world.Season.Winter;s.Tick(dt,Mathf.Repeat(12+i*dt*24/600,24));if(i%20==0)s.Validate();}
            Need(s.Poultry.collected>=12&&s.homeArrivals>=30&&s.meals>0,"12-day life incomplete");RoundTrip(s);rows.Add("12 accelerated days: PASS feed="+s.Poultry.feedSpent+" eggs="+s.Poultry.collected+" meals="+s.meals+" home="+s.homeArrivals);
            File.WriteAllLines("Docs/AutonomousLife147Verification.txt",rows);Debug.Log("LIFE147_VERIFY_OK");AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE147_BUILD_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
