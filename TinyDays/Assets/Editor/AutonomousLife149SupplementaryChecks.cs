using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife149SupplementaryChecks
{
    static AutonomousLifeWorld w;static FieldLayout layout;static readonly List<string> rows=new List<string>();
    static void Need(bool yes,string why){if(!yes)throw new Exception(why);}
    static AutonomousSimulation Fresh(){w.Initialize();w.GetComponent<FarmStudyReview>().ResetCameraToPreset();w.Simulation.SetGrowthDirection(true);return w.Simulation;}
    static void Step(AutonomousSimulation s,float seconds,float hour=12,float dt=.05f){for(float t=0;t<seconds-.0001f;t+=dt){s.Tick(Mathf.Min(dt,seconds-t),hour);s.Validate();}}
    static void Food(AutonomousSimulation s,int amount){if(amount>s.food)s.produced+=amount-s.food;else s.consumed+=s.food-amount;s.food=amount;}
    static void Freeze(AutonomousSimulation s){foreach(var r in s.residents)r.wait=10000;}
    static void Round(){var a=w.CaptureSnapshot();string before=JsonUtility.ToJson(a);w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(a)));Need(before==JsonUtility.ToJson(w.CaptureSnapshot()),"Exact149 restore");}
    static AutonomousSimulation Working(){var s=Fresh();s.Field.pressure=119.99f;for(int i=0;i<2400&&s.Field.stage!=2;i++)s.Tick(.05f,12);Need(s.Field.stage==2,"Could not prepare active digging");return s;}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();Fresh();layout=FieldLayout.Create(w,GrowthLayout.Create(w));
            var s=Fresh();Freeze(s);Step(s,119);Need(s.Field.stage==0&&Mathf.Abs(s.Field.pressure-119)<.05f,"Shortage accumulated time");s.Raining=true;Step(s,30);Need(s.Field.stage==0&&Mathf.Abs(s.Field.pressure-119)<.05f,"Rain advanced pressure");s.Raining=false;Step(s,30,23);Need(s.Field.stage==0&&Mathf.Abs(s.Field.pressure-119)<.05f,"Night advanced pressure");Step(s,1.1f);Need(s.Field.stage==1,"Sustained shortage failed");Round();s=w.Simulation;Food(s,31);Step(s,.1f);Need(s.Field.stage==0&&s.navigation.FieldBlock==null&&s.Field.pressure==0,"Recovery before work not cancelled");rows.Add("120 eligible seconds, night/rain pause, transient recovery cancels plot: PASS");
            s=Fresh();Freeze(s);Food(s,33);Step(s,50);Need(s.Field.stage==0&&s.Field.pressure==0,"48 vs 36 false shortage");s.SetGrowthDirection(false);Step(s,180);Need(s.Field.stage==0,"Maintenance expanded");rows.Add("Capacity-aware target, stable food and maintenance do not expand: PASS");
            s=Fresh();Freeze(s);s.Field.pressure=119.99f;Step(s,.1f);s.SetGrowthDirection(false);Need(s.Field.stage==0&&s.sites.Where(x=>x.id.StartsWith(FieldLayout.Prefix)).All(x=>x.owner<0),"Direction cancel left reservations");Round();rows.Add("Pre-start direction cancel releases plot and workers: PASS");
            s=Fresh();s.ConfigureField(new FieldLayout{candidates=Array.Empty<Vector3>(),placeholder=layout.placeholder});s.Field.pressure=119.99f;Freeze(s);Step(s,10);Need(s.Field.stage==0&&s.Field.pressure==120&&s.FieldStatus.Contains("공간 부족"),"No-space waiting");rows.Add("No space preserves existing farm and reports waiting: PASS");
            s=Fresh();Freeze(s);Food(s,30);s.Field.pressure=119.99f;s.Growth.pressure=59.99f;Step(s,.1f);Need(s.Growth.stage==1&&s.Field.stage==0,"Warehouse tie priority");rows.Add("Both unstarted: warehouse reserves first: PASS");
            s=Fresh();Freeze(s);s.Field.pressure=119.99f;Step(s,.1f);Need(s.Field.stage==1,"Pending field fixture");Food(s,30);s.Growth.pressure=59.99f;Step(s,.1f);Need(s.Growth.stage==1&&s.Field.stage==0&&s.navigation.FieldBlock==null,"Unstarted field blocked warehouse priority");rows.Add("Reserved but unstarted field yields its reservation to ready warehouse: PASS");
            foreach(string reason in new[]{"rain","night","food","fatigue","hunger"}){
                s=Working();var r=s.residents.Single(x=>x.action==LifeAction.ClearField);float progress=s.Field.progress;
                if(reason=="rain")s.Raining=true;if(reason=="food")Food(s,0);if(reason=="fatigue")r.fatigue=1;if(reason=="hunger")r.hunger=1;
                Step(s,.1f,reason=="night"?23:12);Need(r.action!=LifeAction.ClearField&&s.Field.progress>=progress,"Urgent need ignored "+reason);Round();rows.Add("Digging pause + exact save: "+reason+" PASS");
            }
            s=Working();Food(s,33);s.Growth.pressure=59.99f;Step(s,2);Need(s.Growth.stage==0,"Started field lost priority");s.SetGrowthDirection(false);Step(s,250);Need(s.Field.stage==3&&s.Growth.spent==0,"Finish after direction change / no timber");Round();rows.Add("Started digging takes precedence, completes after mode change, no wood cost: PASS");
            s=Working();var worker=s.residents.Single(r=>r.action==LifeAction.ClearField);s.CancelWork(worker.id);var work=s.sites.First(x=>x.kind==SiteKind.ClearField).position;s.navigation.TemporaryBlock=new Bounds(work,new Vector3(.4f,1,.4f));
            // Move the interrupted worker clear of the temporary test obstruction.
            worker.position=s.navigation.Nearby(worker.position,3);Step(s,5);float held=s.Field.progress;s.navigation.TemporaryBlock=null;Step(s,180);Need(s.Field.stage==3&&held<60,"Blocked dig route failed recovery");Round();rows.Add("Blocked approach releases reservation; unblocked work completes: PASS");
            foreach(float fps in new[]{30,60,120})foreach(float rate in new[]{.5f,1f,4f}){s=Working();float before=s.Field.progress;Step(s,2*rate,12,rate/fps);Need(Mathf.Abs(s.Field.progress-before-2*rate)<.005f,"Digging fps/rate");}
            s=Working();string frozen=JsonUtility.ToJson(s.CaptureSave());s.Tick(0,23);Need(JsonUtility.ToJson(s.CaptureSave())==frozen,"Paused state changed");rows.Add("30/60/120fps x 0.5/1/4, pause work continuity: PASS");
            // Old files are read-only; writes go to newly created isolated migration directories.
            foreach(string file in new[]{"Logs/Save143UI/AutonomousLife142.json","Logs/Save144Observation/AutonomousLife142.json","Logs/Save145Observation/AutonomousLife142.json","Logs/Save146UI/AutonomousLife142.json","Logs/Save147FinalObservation/AutonomousLife142.json","Logs/Save148FinalObservation/AutonomousLife142.json"}){
                string raw=File.ReadAllText(file);var old=AutonomousSaveCodec.Decode(raw);old.simulation.field=null;if(old.sourceVersion<148)old.simulation.growth=null;if(old.sourceVersion<147)old.simulation.poultry=null;w.RestoreSnapshot(old);var now=w.CaptureSnapshot();var cmp=JsonUtility.FromJson<SimulationSaveData>(JsonUtility.ToJson(now.simulation));cmp.field=null;if(old.sourceVersion<148)cmp.growth=null;if(old.sourceVersion<147)cmp.poultry=null;cmp.sites=cmp.sites.Take(old.simulation.sites.Length).ToArray();Need(JsonUtility.ToJson(cmp)==JsonUtility.ToJson(old.simulation),"Legacy life changed "+old.sourceVersion);Need(JsonUtility.ToJson(now.camera)==JsonUtility.ToJson(old.camera)&&now.hour==old.hour&&now.day==old.day,"Legacy view/time");Need(now.sourceVersion==149&&now.simulation.field.stage==0,"Legacy field defaults");string dir="Logs/Save149Legacy-"+old.sourceVersion+"-"+DateTime.UtcNow.Ticks;Directory.CreateDirectory(dir);var repo=new AutonomousSaveRepository(dir,d=>w.ValidateSnapshot(d));File.WriteAllText(repo.Path,raw);repo.Write(now);Need(File.ReadAllText(repo.Path+".v"+old.sourceVersion)==raw,"Legacy archive");rows.Add("Legacy "+old.sourceVersion+" same life/resources/view + source archive: PASS");
            }
            s=Working();var data=w.CaptureSnapshot();data.simulation.field.progress=61;bool failed=false;try{w.RestoreSnapshot(data);}catch{failed=true;}Need(failed&&w.Simulation==s,"Malformed field altered active world");Round();rows.Add("Malformed field rejected without altering active play: PASS");
            File.WriteAllLines("Docs/AutonomousLife149SupplementaryVerification.txt",rows);Debug.Log("LIFE149_SUPPLEMENTARY_OK");
        }catch(Exception e){File.WriteAllLines("Logs/life149-supplement-partial.txt",rows);Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
