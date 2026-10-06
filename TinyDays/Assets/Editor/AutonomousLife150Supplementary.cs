using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife150Supplementary
{
    static AutonomousLifeWorld w;static readonly List<string> rows=new List<string>();
    static void Need(bool v,string why){if(!v)throw new Exception(why);}
    static AutonomousSimulation Fresh(){w.Initialize();w.GetComponent<FarmLifeDirector>().paused=false;w.GetComponent<FarmStudyReview>().ResetCameraToPreset();return w.Simulation;}
    static void Step(AutonomousSimulation s,float time,float hour=12,float dt=.05f){for(float t=0;t<time-.00001f;t+=dt){s.Tick(Mathf.Min(dt,time-t),hour);s.Validate();}}
    static void Food(AutonomousSimulation s,int n){s.produced+=Math.Max(0,n-s.food);s.consumed+=Math.Max(0,s.food-n);s.food=n;}
    static AutonomousSimulation Stable(){var s=Fresh();Food(s,18);foreach(var r in s.residents){r.wait=300;r.hunger=.1f;r.fatigue=.1f;}return s;}
    static void Round(){string before=JsonUtility.ToJson(w.CaptureSnapshot());w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(w.CaptureSnapshot())));Need(before==JsonUtility.ToJson(w.CaptureSnapshot()),"Exact save mismatch");}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();var s=Stable();
            Step(s,60);Need(s.Garden.stage==0&&Mathf.Abs(s.Garden.stability-60)<.1f,"Premature garden");s.Raining=true;Step(s,15);s.Raining=false;Step(s,15,22);Need(Mathf.Abs(s.Garden.stability-60)<.1f,"Weather/night pressure not paused");Step(s,60.1f);Need(s.Garden.stage==1,"Stable120 not reserved");Round();
            s=w.Simulation;Food(s,11);Step(s,.1f);Need(s.Garden.stage==0&&s.Garden.stability==0,"Unstarted instability cancel");rows.Add("Stability120, rain/night pause, short stability no build, food11 cancels: PASS");
            s=Stable();s.ConfigureGarden(new GardenLayout{candidates=Array.Empty<Vector3>(),placeholder=s.sites[0].position});Step(s,121);Need(s.Garden.stage==0&&s.GardenStatus.Contains("공간 부족"),"No-space wait");rows.Add("Unavailable space waits without construction: PASS");
            s=Stable();Step(s,120.1f);s.SetGrowthDirection(true);Food(s,30);s.Growth.pressure=59.99f;Step(s,.1f);Need(s.Garden.stage==0&&s.Growth.stage==1,"Warehouse did not preempt reservation");rows.Add("Production project cancels unstarted garden reservation: PASS");
            s=Stable();Step(s,120.1f);foreach(var r in s.residents)r.wait=0;
            for(int i=0;i<6000&&s.Garden.stage<2;i++)Step(s,.05f);
            Need(s.Garden.stage==2,"No garden worker");Round();s=w.Simulation;
            s.SetGrowthDirection(true);Food(s,30);s.Growth.pressure=59.99f;float progress=s.Garden.progress;s.Raining=true;Step(s,5);Need(s.Garden.progress==progress&&s.Growth.stage==0,"Started priority/rain pause");s.Raining=false;
            Step(s,5,22);Need(s.Garden.progress==progress,"Night work");s.SetGrowthDirection(false);
            for(int i=0;i<18000&&s.Garden.stage<3;i++){s.Tick(.05f,12);if(i%20==0)s.Validate();}
            Need(s.Garden.stage==3,"Interrupted garden never resumed");Round();rows.Add("Started garden priority, rain/night pause, direction change, completion exact save: PASS");
            // Both independent new rest stations must be usable, with exclusive ownership.
            s=w.Simulation;foreach(var r in s.residents)s.CancelWork(r.id);
            var seats=s.sites.Where(x=>x.id.StartsWith(GardenLayout.Prefix)&&x.kind==SiteKind.Rest).ToArray();
            for(int i=0;i<6;i++){var r=s.residents[i];r.position=s.sites.Single(x=>x.kind==SiteKind.Home&&x.homeResident==i).position;r.wait=20;r.sleeping=false;r.hunger=.05f;r.fatigue=.1f;}
            for(int i=0;i<2;i++){var r=s.residents[i];r.position=seats[i].position;r.wait=0;r.fatigue=.85f;}
            Step(s,.1f);Need(seats.All(x=>x.owner>=0)&&seats[0].owner!=seats[1].owner,"Two independent rest reservations");Round();Step(w.Simulation,8);Round();rows.Add("Both garden rest seats reserve independently and restore while resting: PASS");
            // Backward versions start with three residents and no added sites. No real user save involved.
            foreach(int version in new[]{142,144,145,146,147,148,149}){
                s=Fresh();var data=w.CaptureSnapshot();data.sourceVersion=version;var oldSites=w.definitions.Length+(version>=144?3:0)+(version>=147?PoultryLayout.Sites().Length:0)+(version>=148?4:0)+(version>=149?3:0);
                data.simulation.residents=data.simulation.residents.Take(3).ToArray();data.simulation.sites=data.simulation.sites.Take(oldSites).ToArray();data.simulation.garden=null;
                if(version<149)data.simulation.field=null;if(version<148)data.simulation.growth=null;if(version<147)data.simulation.poultry=null;
                string encoded=AutonomousSaveCodec.Encode(data);w.RestoreSnapshot(AutonomousSaveCodec.Decode(encoded));Need(w.Simulation.residents.Length==6&&w.Simulation.Garden.stage==0,"Legacy conversion "+version);Round();Need(encoded==AutonomousSaveCodec.Encode(data),"Legacy source mutation");
            }
            rows.Add("142/144/145/146/147/148/149 source-preserving migration and repeat150 restore: PASS");
            var bad=w.CaptureSnapshot();bad.simulation.garden.stage=3;bad.simulation.garden.progress=1;string unchanged=JsonUtility.ToJson(w.CaptureSnapshot());bool rejected=false;try{w.RestoreSnapshot(bad);}catch{rejected=true;}Need(rejected&&unchanged==JsonUtility.ToJson(w.CaptureSnapshot()),"Bad save mutated live state");
            rows.Add("Malformed garden rejected without changing live state: PASS");
            foreach(float fps in new[]{30f,60f,120f})foreach(float rate in new[]{.5f,1f,4f}){s=Stable();Step(s,120.2f,12,rate/fps);Need(s.Garden.stage==1,"Rate pressure failure");s.Validate();}
            rows.Add("30/60/120fps x 0.5/1/4 rates: pressure threshold/reservations PASS");
            File.WriteAllLines("Docs/AutonomousLife150SupplementaryVerification.txt",rows);Debug.Log("LIFE150_SUPPLEMENTARY_OK");
        }catch(Exception e){File.WriteAllLines("Logs/life150-supplement-partial.txt",rows);Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
    public static void Continuation()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();Fresh();w.RestoreSnapshot(AutonomousSaveCodec.Decode(File.ReadAllText("Logs/Save150Natural/AutonomousLife142.json")));w.Simulation.SetGrowthDirection(true);
            var s=w.Simulation;var initial=s.residents.Select(r=>r.completed).ToArray();int meals=s.meals;bool winter=false,shared=false;var users=new HashSet<int>();
            for(int i=0;i<144000;i++){
                w.Advance(.05f);s=w.Simulation;winter|=s.Winter;
                if(i%20==0)s.Validate();if(i%1200==0)Round();
                foreach(var r in s.residents)if(r.site>=0&&s.sites[r.site].id.StartsWith(GardenLayout.Prefix)&&r.action!=LifeAction.Garden){users.Add(r.id);shared|=r.socialPartner>0;}
                if(i%12000==0)Debug.Log($"LIFE150_12DAY t={s.elapsed:F0} food={s.food}/{s.Capacity} garden={s.Garden.stage} warehouse={s.Growth.stage} field={s.Field.stage} meals={s.meals}");
            }
            Need(winter&&s.Garden.stage==3&&s.meals>meals+24&&s.residents.All(r=>r.completed>initial[r.id]+10),"12day life failure");Need(users.Count>=2,"Garden not used");Round();
            File.WriteAllText("Docs/AutonomousLife150ContinuationVerification.txt",$"Accelerated12days: PASS; winter={winter}; new meals={s.meals-meals}; garden users={string.Join(",",users)}; shared social={shared}; stock={s.food}/{s.Capacity}; warehouse={s.Growth.stage}; field={s.Field.stage}\nMinute exact save roundtrip, every-second conservation/reservations: PASS\nNot actual2hour observation.\n");Debug.Log("LIFE150_CONTINUATION_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
    public static void Recovery()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();var s=Stable();
            Step(s,120.1f);s.residents[0].wait=0;
            for(int i=0;i<3000&&!s.residents.Any(r=>r.action==LifeAction.Garden&&r.Moving);i++)Step(s,.05f);
            Need(s.residents.Any(r=>r.action==LifeAction.Garden&&r.Moving),"No approaching gardener");
            var target=GardenLayout.Work(s.Garden.site);var block=new Bounds(target+Vector3.up*.2f,new Vector3(.35f,.4f,.35f));
            Need(s.residents.All(r=>(r.position-target).sqrMagnitude>1),"Unsafe block fixture");s.navigation.TemporaryBlock=block;Step(s,15);Need(s.Garden.progress==0,"Blocked garden progressed");Round();s=w.Simulation;s.navigation.TemporaryBlock=null;
            for(int i=0;i<18000&&s.Garden.stage!=3;i++)Step(s,.05f);Need(s.Garden.stage==3,"Garden path never recovered");Round();rows.Add("Blocked garden approach waits, reservation roundtrip and unblock completion: PASS");
            s=Fresh();Food(s,0);foreach(var r in s.residents)r.hunger=.8f;Step(s,300);Need(s.produced>0&&s.meals>=6&&s.residents.All(r=>r.walkDistance>2),"Six-resident zero food recovery");Round();rows.Add("Six-resident zero food production and at least6 meals recover: PASS");
            string path="Logs/Save149FinalObservation/AutonomousLife142.json";string original=File.ReadAllText(path);var old=AutonomousSaveCodec.Decode(original);old.hour=22;old.simulation.hour=22;
            var residents=old.simulation.residents.Select(r=>JsonUtility.ToJson(r)).ToArray();w.RestoreSnapshot(old);s=w.Simulation;Need(s.residents.Skip(3).All(r=>r.sleeping&&r.action==LifeAction.Home),"Night new residents not home");Need(w.CaptureSnapshot().simulation.residents.Take(3).Select(r=>JsonUtility.ToJson(r)).SequenceEqual(residents),"Night migration reset existing actors");Round();Step(w.Simulation,60,6);Round();Need(File.ReadAllText(path)==original,"Night fixture altered source");rows.Add("Night legacy migration: three additions asleep, original3 preserved, safe dawn resume: PASS");
            File.WriteAllLines("Docs/AutonomousLife150RecoveryVerification.txt",rows);Debug.Log("LIFE150_RECOVERY_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
    public static void StartBoundary()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();var s=Stable();Food(s,12);Step(s,120.1f);Need(s.Garden.stage==1,"No boundary plot");
            var worker=s.residents[1];int site=Array.FindIndex(s.sites,x=>x.kind==SiteKind.Garden);worker.position=s.sites[site].position;worker.site=site;worker.action=LifeAction.Garden;worker.path=null;worker.wait=0;s.sites[site].owner=worker.id;
            s.residents[0].wait=0;s.residents[0].hunger=.8f;s.Validate();Step(s,.05f);
            Need(s.residents[0].mealReservation==1&&s.Garden.stage==0&&s.Garden.progress==0&&worker.action!=LifeAction.Garden,"Same-frame meal reservation bypassed stability");Round();
            File.WriteAllText("Docs/AutonomousLife150BoundaryVerification.txt","Food12 -> earlier resident reserves meal in same tick -> gardener cancels before first work; no construction/ownership leak; exact restore: PASS\n");Debug.Log("LIFE150_BOUNDARY_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
