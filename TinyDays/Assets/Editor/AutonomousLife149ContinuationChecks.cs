using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife149ContinuationChecks
{
    static AutonomousLifeWorld w;static readonly List<string> rows=new List<string>();
    static void Need(bool b,string m){if(!b)throw new Exception(m);}
    static void Round(){var data=w.CaptureSnapshot();var raw=JsonUtility.ToJson(data);w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(data)));Need(raw==JsonUtility.ToJson(w.CaptureSnapshot()),"Continuation exact restore");}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();w.GetComponent<FarmStudyReview>().ResetCameraToPreset();
            // Recoverable cargo is counted once, including the reserved portion of a ground basket.
            var data=w.CaptureSnapshot();data.simulation.growth.stockpile=true;data.simulation.field.pressure=119;
            data.simulation.counts[0]=28;data.simulation.counts[1]=21; // 28 stored + 2 ground == initial9 + produced21
            data.simulation.nextBundleId=1;var p=w.definitions.First(x=>x.id=="wait-0").position;
            data.simulation.bundles=new[]{new FoodBundle{id=0,amount=2,owner=0,position=p}};
            foreach(var r in data.simulation.residents)r.state.wait=10000;
            var collector=data.simulation.residents[0];collector.bundleId=0;collector.state.action=LifeAction.Collect;collector.state.collectionReservation=2;
            w.RestoreSnapshot(data);w.Simulation.Tick(.1f,12);Need(w.Simulation.Field.pressure>119,"Reserved basket double-counted");Round();rows.Add("Reserved ground basket counted once at exact shortage threshold: PASS");
            data.simulation.bundles[0].amount=3;data.simulation.counts[1]=22;w.RestoreSnapshot(data);w.Simulation.Tick(.1f,12);Need(w.Simulation.Field.pressure==0,"Recoverable ground stock ignored");
            data.simulation.hasBlock=true;data.simulation.block=new Bounds(p,Vector3.one);w.RestoreSnapshot(data);w.Simulation.Tick(.1f,12);Need(w.Simulation.Field.pressure>119,"Unreachable basket counted as available");rows.Add("Recoverable unreserved remainder included; blocked basket excluded: PASS");
            w.RestoreSnapshot(AutonomousSaveCodec.Decode(File.ReadAllText("Logs/Save149Completed/AutonomousLife142.json")));var d=w.GetComponent<FarmLifeDirector>();d.paused=false;d.Playback.SetDayMinutes("10");d.Playback.SetRate(1);w.Simulation.SetGrowthDirection(true);var before=w.CaptureSnapshot();int meals=w.Simulation.meals;bool winter=false;int fieldHarvests=0;
            for(int i=0;i<144000;i++){
                w.Advance(.05f);if(i%20==0)w.Simulation.Validate();
                if(i%1200==0){Round();Debug.Log($"LIFE149_CONTINUATION t={w.Simulation.elapsed:F0} season={w.Season.Label} food={w.Simulation.food} field={w.Simulation.Field.stage}");}
                winter|=w.Season.Winter;
                if(w.Simulation.residents.Any(r=>r.action==LifeAction.Harvest&&w.Simulation.sites[r.site].id.StartsWith(FieldLayout.Prefix)))fieldHarvests++;
            }
            Need(winter&&w.Simulation.Field.stage==3&&w.Simulation.meals>meals+15&&fieldHarvests>0,"Incomplete seasonal field life");Round();rows.Add($"Accelerated 12 days / winter / additional field harvest / per-minute exact restore: PASS, additional meals={w.Simulation.meals-meals}, food={w.Simulation.food}/{w.Simulation.Capacity}");
            // Field soil and original crop growth use the same seasonal multiplier, without a separate balance.
            var crop=w.Simulation.sites.First(s=>s.id==FieldLayout.Prefix+"carrot-0");foreach(var r in w.Simulation.residents){w.Simulation.CancelWork(r.id);r.wait=10000;}crop.planted=true;crop.growth=.2f;w.Simulation.Winter=true;w.Simulation.Tick(1,12);Need(Mathf.Abs(crop.growth-(.2f+.5f/w.tuning.cropSeconds))<.00001f,"Additional crop winter multiplier");rows.Add("New carrot winter growth uses existing 50 percent factor: PASS");
            File.WriteAllLines("Docs/AutonomousLife149ContinuationVerification.txt",rows);Debug.Log("LIFE149_CONTINUATION_OK");
        }catch(Exception e){File.WriteAllLines("Logs/life149-continuation-partial.txt",rows);Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
