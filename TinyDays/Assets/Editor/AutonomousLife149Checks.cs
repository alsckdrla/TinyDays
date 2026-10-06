using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife149Checks
{
    static AutonomousLifeWorld w;static readonly List<string> rows=new List<string>();
    static void Need(bool value,string why){if(!value)throw new Exception(why);}
    static AutonomousSimulation Fresh(){w.Initialize();w.GetComponent<FarmLifeDirector>().paused=false;w.GetComponent<FarmStudyReview>().ResetCameraToPreset();w.Simulation.SetGrowthDirection(true);return w.Simulation;}
    static void Step(AutonomousSimulation s,float seconds,float hour=12,float dt=.05f){for(float t=0;t<seconds-.00001f;t+=dt){s.Tick(Mathf.Min(dt,seconds-t),hour);s.Validate();}}
    static void Round(){string before=JsonUtility.ToJson(w.CaptureSnapshot());w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(w.CaptureSnapshot())));Need(before==JsonUtility.ToJson(w.CaptureSnapshot()),"149 exact restore");}
    static void Init(){EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();Fresh();}
    public static void Execute()
    {
        try{
            Init();var s=w.Simulation;var layout=FieldLayout.Create(w,GrowthLayout.Create(w));Need(layout.candidates.Length>0,"No field plot");rows.Add("Safe field candidates: "+layout.candidates.Length+" first="+layout.candidates[0]);
            // Natural initial farm, without forced stocks, unavailable crops or altered tuning.
            var actions=new HashSet<LifeAction>();bool fieldPlant=false,fieldHarvest=false;
            for(int i=0;i<48000;i++){
                w.Advance(.05f);s=w.Simulation;
                if(i%20==0)s.Validate();
                foreach(var r in s.residents){if(r.action==LifeAction.ClearField&&actions.Add(r.action))Round();if(r.site>=0&&s.sites[r.site].id.StartsWith(FieldLayout.Prefix)){if(r.action==LifeAction.Plant&&!fieldPlant){fieldPlant=true;Round();}if(r.action==LifeAction.Harvest&&!fieldHarvest){fieldHarvest=true;Round();}}}
                if(i%2400==0)Debug.Log($"LIFE149_NATURAL t={s.elapsed} food={s.food} field={s.Field.stage}/{s.Field.pressure}/{s.Field.progress} warehouse={s.Growth.stage}");
            }
            s=w.Simulation;Round();rows.Add($"Natural 4 days: field={s.Field.stage} planted={fieldPlant} harvested={fieldHarvest} warehouse={s.Growth.stage} food={s.food}, no forced expansion");
            // Persist a separate shortage fixture with real world geometry; no user saves or tuning changes.
            s=Fresh();s.Field.pressure=119.9f;Step(s,.15f);Need(s.Field.stage==1,"Shortage did not reserve");Round();s=w.Simulation;
            var fixture=w.CaptureSnapshot();Directory.CreateDirectory("Logs/Save149ShortageFixture");File.WriteAllText("Logs/Save149ShortageFixture/AutonomousLife142.json",AutonomousSaveCodec.Encode(fixture));
            for(int i=0;i<16000&&s.Field.stage!=3;i++){s.Tick(.05f,12);if(i%20==0)s.Validate();}
            Need(s.Field.stage==3,"Prepared shortage did not finish");Round();s=w.Simulation;Step(s,400);Round();rows.Add("Prepared shortage: reserve/dig/finish/production exact save: PASS");
            File.WriteAllLines("Docs/AutonomousLife149Verification.txt",rows);Debug.Log("LIFE149_VERIFY_OK");AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE149_BUILD_OK");
        }catch(Exception e){File.WriteAllLines("Logs/life149-partial.txt",rows);Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
