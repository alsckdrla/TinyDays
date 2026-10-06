using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife148ContinuationChecks
{
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();w.GetComponent<FarmStudyReview>().ResetCameraToPreset();
            w.RestoreSnapshot(AutonomousSaveCodec.Decode(File.ReadAllText("Logs/Save148Completed/AutonomousLife142.json")));var initial=w.CaptureSnapshot();
            int meals=w.Simulation.meals,home=w.Simulation.homeArrivals;float elapsed=w.Simulation.elapsed;
            for(int i=0;i<36000;i++){w.Advance(.05f);if(i%20==0)w.Simulation.Validate();if(i%1200==0){var d=w.CaptureSnapshot();var s=w.ValidateSnapshot(d);if(JsonUtility.ToJson(s.CaptureSave())!=JsonUtility.ToJson(d.simulation))throw new Exception("Post-build roundtrip mismatch");}}
            var result=w.Simulation;var g=result.Growth;
            if(g.stage!=3||g.spent!=12||g.sources.Sum()!=0||result.Capacity!=60||result.meals<=meals||result.homeArrivals<=home)throw new Exception("Post-build life did not continue");
            // New game clears the construction state without changing preserved playback settings.
            var director=w.GetComponent<FarmLifeDirector>();director.paused=true;director.Playback.SetDayMinutes("20");director.Playback.SetRate(2);float rate=director.Playback.Rate;w.Restart();
            if(w.Simulation.Growth.stockpile||w.Simulation.Growth.stage!=0||w.Simulation.Growth.sources.Sum()!=12||w.Simulation.Capacity!=36||!director.paused||director.Playback.DayMinutes!=20||director.Playback.Rate!=rate)throw new Exception("New farm growth reset/playback preservation");
            File.WriteAllText("Docs/AutonomousLife148ContinuationVerification.txt",$"After natural completion, accelerated 3 additional days: PASS\nAdditional meals={result.meals-meals}, home arrivals={result.homeArrivals-home}, food={result.food}/60, wood spent exactly12\nEvery simulated second invariants, every minute exact save restore: PASS\nNew farm direction/wood/plot reset + day length/rate/pause preserved: PASS\nThis is accelerated testing, not 30 real minutes or release stability.\n");Debug.Log("LIFE148_CONTINUATION_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
