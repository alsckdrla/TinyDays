using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife148Checks
{
    static AutonomousLifeWorld world;
    static void Need(bool yes,string why){if(!yes)throw new Exception(why);}
    static void RoundTrip(){var data=world.CaptureSnapshot();string json=JsonUtility.ToJson(data);world.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(data)));Need(JsonUtility.ToJson(world.CaptureSnapshot())==json,"148 exact restore");}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();world.Initialize();world.GetComponent<FarmStudyReview>().ResetCameraToPreset();
            var layout=GrowthLayout.Create(world);Debug.Log("LIFE148_LAYOUT candidates="+layout.candidates.Length+" first="+layout.candidates.FirstOrDefault()+" wood="+layout.store+" sources="+string.Join("/",layout.sources.Select(x=>x.ToString())));Need(layout.candidates.Length>0,"No usable expansion plot");
            var rows=new List<string>();world.Simulation.SetGrowthDirection(true);var observed=new HashSet<LifeAction>();
            for(int i=0;i<36000;i++){world.Advance(.05f);var s=world.Simulation;if(i%20==0)s.Validate();foreach(var r in s.residents)if((int)r.action>=15&&observed.Add(r.action))RoundTrip();if(i%1200==0)Debug.Log("LIFE148_STEP "+s.elapsed+" food="+s.food+" "+s.GrowthStatus+" wood="+s.Growth.wood);if(s.Growth.stage==3){RoundTrip();break;}}
            Need(world.Simulation.Growth.stage==3,"Natural expansion did not finish");Need(observed.Contains(LifeAction.WoodGather)&&observed.Contains(LifeAction.WoodDeliver)&&observed.Contains(LifeAction.Build),"Missing construction actions");rows.Add("Natural stockpile/gather/transport/build/60 capacity + action exact save: PASS at "+world.Simulation.elapsed);
            var completed=world.CaptureSnapshot();Directory.CreateDirectory("Logs/Save148Completed");File.WriteAllText("Logs/Save148Completed/AutonomousLife142.json",AutonomousSaveCodec.Encode(completed));world.Simulation.SetGrowthDirection(false);Need(world.Simulation.Capacity==60&&world.Simulation.TargetFood==18,"Completed capacity lost");world.Advance(60);RoundTrip();rows.Add("Completed mode change preserves capacity and life: PASS");
            foreach(string file in new[]{"Logs/Save143UI/AutonomousLife142.json","Logs/Save144Observation/AutonomousLife142.json","Logs/Save145Observation/AutonomousLife142.json","Logs/Save146UI/AutonomousLife142.json","Logs/Save147FinalObservation/AutonomousLife142.json"}){
                var raw=File.ReadAllText(file);var old=AutonomousSaveCodec.Decode(raw);world.RestoreSnapshot(old);var now=world.CaptureSnapshot();var compare=JsonUtility.FromJson<SimulationSaveData>(JsonUtility.ToJson(now.simulation));compare.growth=null;if(old.sourceVersion<147)compare.poultry=null;compare.sites=compare.sites.Take(old.simulation.sites.Length).ToArray();Need(JsonUtility.ToJson(compare)==JsonUtility.ToJson(old.simulation),"Legacy state changed "+old.sourceVersion);Need(JsonUtility.ToJson(now.camera)==JsonUtility.ToJson(old.camera)&&now.hour==old.hour&&now.day==old.day,"Legacy view/time changed");Need(now.sourceVersion==148&&!now.simulation.growth.stockpile&&now.simulation.growth.stage==0,"Legacy growth defaults");string dir="Logs/Save148Legacy-"+old.sourceVersion+"-"+DateTime.UtcNow.Ticks;Directory.CreateDirectory(dir);var repo=new AutonomousSaveRepository(dir,x=>world.ValidateSnapshot(x));File.WriteAllText(repo.Path,raw);repo.Write(now);Need(File.ReadAllText(repo.Path+".v"+old.sourceVersion)==raw,"Legacy archive");rows.Add("Legacy "+old.sourceVersion+" exact existing life, view, archive: PASS");}
            world.RestoreSnapshot(completed);var snapshot=world.CaptureSnapshot();snapshot.simulation.growth.wood=12;bool refused=false;try{world.RestoreSnapshot(snapshot);}catch{refused=true;}Need(refused,"Malformed wood accepted");rows.Add("Invalid wood conservation save rejected: PASS");
            File.WriteAllLines("Docs/AutonomousLife148Verification.txt",rows);Debug.Log("LIFE148_VERIFY_OK");AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE148_BUILD_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
