using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife150Checks
{
    static AutonomousLifeWorld w;static readonly List<string> rows=new List<string>();
    static void Need(bool v,string why){if(!v)throw new Exception(why);}
    static AutonomousSimulation Fresh(){w.Initialize();w.GetComponent<FarmLifeDirector>().paused=false;w.GetComponent<FarmStudyReview>().ResetCameraToPreset();return w.Simulation;}
    static void Step(AutonomousSimulation s,float seconds,float hour=12,float dt=.05f){for(float t=0;t<seconds-.00001f;t+=dt){s.Tick(Mathf.Min(dt,seconds-t),hour);s.Validate();}}
    static void Round(){string before=JsonUtility.ToJson(w.CaptureSnapshot());w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(w.CaptureSnapshot())));Need(before==JsonUtility.ToJson(w.CaptureSnapshot()),"150 exact restore");}
    static void Init(){EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();Fresh();}
    public static void Execute()
    {
        try{
            Init();var s=w.Simulation;Need(s.residents.Length==6&&s.sites.Count(x=>x.kind==SiteKind.Home)==6&&s.sites.Count(x=>x.kind==SiteKind.Shelter)==6,"Six population/stations");
            Need(s.residents.GroupBy(r=>r.temperament).All(g=>g.Count()==2),"Traits");s.Validate();Round();rows.Add("Six residents, 6 homes/shelters, paired traits, fresh exact save: PASS");
            // Validate actual prior save without reading any user data directory.
            string path="Logs/Save149FinalObservation/AutonomousLife142.json";
            Need(File.Exists(path),"Missing isolated149 fixture");string original=File.ReadAllText(path);var old=AutonomousSaveCodec.Decode(original);Need(old.sourceVersion==149,"Fixture version");
            var expected=old.simulation.residents.Select(r=>JsonUtility.ToJson(r)).ToArray();w.RestoreSnapshot(old);s=w.Simulation;
            Need(s.residents.Length==6&&s.food==old.simulation.counts[0],"Legacy population/stock");
            var upgraded=w.CaptureSnapshot();Need(upgraded.simulation.residents.Take(3).Select(r=>JsonUtility.ToJson(r)).SequenceEqual(expected),"Existing resident changed during migration");
            Need(upgraded.simulation.sites.Take(old.simulation.sites.Length).Select(r=>JsonUtility.ToJson(r)).SequenceEqual(old.simulation.sites.Select(r=>JsonUtility.ToJson(r))),"Existing reservations changed");
            Round();w.RestoreSnapshot(old);Need(w.Simulation.residents.Length==6&&File.ReadAllText(path)==original,"Duplicate migration/source mutation");Step(w.Simulation,80);Round();rows.Add("149 active save migration, first3/reservations untouched, repeat load, continuation: PASS");
            s=Fresh();bool completed=false;int used=0;
            for(int i=0;i<36000;i++){
                w.Advance(.05f);s=w.Simulation;if(i%20==0)s.Validate();
                if(s.Garden.stage>0&&!completed&&i%200==0)Round();
                completed|=s.Garden.stage==3;
                used=Math.Max(used,s.sites.Count(x=>x.id.StartsWith(GardenLayout.Prefix)&&x.kind==SiteKind.Rest&&x.owner>=0));
                if(i%2400==0)Debug.Log($"LIFE150_NATURAL t={s.elapsed:F1} food={s.food} garden={s.Garden.stage}/{s.Garden.stability:F1}/{s.Garden.progress:F1} meals={s.meals} home={s.homeArrivals}");
            }
            Round();Need(s.meals>=12&&s.homeArrivals>=12,"Six residents life did not persist");Need(s.residents.All(r=>r.walkDistance>25),"Resident stuck");
            rows.Add($"Natural 3 days: garden complete={completed}, occupied rest seats max={used}, meals={s.meals}, home={s.homeArrivals}, food={s.food}");
            var natural=w.CaptureSnapshot();Directory.CreateDirectory("Logs/Save150Natural");File.WriteAllText("Logs/Save150Natural/AutonomousLife142.json",AutonomousSaveCodec.Encode(natural));
            if(!completed)throw new Exception("Natural maintenance did not build garden in3days");
            File.WriteAllLines("Docs/AutonomousLife150Verification.txt",rows);Debug.Log("LIFE150_VERIFY_OK");AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE150_BUILD_OK");
        }catch(Exception e){File.WriteAllLines("Logs/life150-partial.txt",rows);Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
