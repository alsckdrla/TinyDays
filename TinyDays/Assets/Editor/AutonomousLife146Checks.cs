using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Life;
using TinyDays.Review;

public static class AutonomousLife146Checks
{
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    static void Step(AutonomousLifeWorld w,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds*20);i++){w.Advance(.05f);w.Simulation.Validate();}}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);
            var w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();
            var d=w.GetComponent<FarmLifeDirector>();var ui=w.GetComponent<FarmStudyReview>();ui.ResetCameraToPreset();d.paused=false;d.Playback.SetRate(1);
            var rows=new List<string>();
            var head=d.residents[0].visual.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.bones).FirstOrDefault(b=>b&&b.name=="Head");
            Need(head,"Head bone unavailable for greeting");
            AutonomousSaveData paired=null;
            for(int i=0;i<12000;i++){w.Advance(.05f);w.Simulation.Validate();if(w.Simulation.residents.Any(r=>r.socialPartner>0)){paired=w.CaptureSnapshot();break;}}
            Need(paired!=null,"No natural social encounter within 10 simulated minutes");
            int leader=w.Simulation.residents.First(r=>r.socialPartner>0).id,partner=w.Simulation.residents[leader].socialPartner-1;
            rows.Add("Natural autonomous approach/greeting at separate existing places: PASS at "+w.Simulation.elapsed);
            Step(w,2);Need(AutonomousSimulation.Talking(w.Simulation.residents[leader]),"Conversation absent");
            var talking=w.CaptureSnapshot();string text=JsonUtility.ToJson(talking);
            w.RestoreSnapshot(AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(talking)));Need(text==JsonUtility.ToJson(w.CaptureSnapshot()),"Paired conversation changed on restore");
            d.paused=true;Step(w,1);Need(text.Replace("\"paused\":false","\"paused\":true")==JsonUtility.ToJson(w.CaptureSnapshot()),"Pause social state");d.paused=false;
            Step(w,5);Need(AutonomousSimulation.SocialLabel(w.Simulation.residents[leader])=="함께 쉬기","Shared rest absent");
            Step(w,7);Need(w.Simulation.residents[leader].socialPartner==0&&w.Simulation.residents[partner].socialPartner==0,"Pair did not finish");
            rows.Add("Greeting/conversation/shared rest/end + exact restore/pause: PASS");
            foreach(string urgent in new[]{"hunger","fatigue","night","rain","shortage","cancel"}){
                w.RestoreSnapshot(talking);var r=w.Simulation.residents[leader];
                if(urgent=="hunger")r.hunger=1;
                if(urgent=="fatigue")r.fatigue=1;
                if(urgent=="night")w.GetComponent<FarmLightingStudy>().Apply(3);
                if(urgent=="rain")w.SelectWeather(2);
                if(urgent=="shortage"){int removed=w.Simulation.food;w.Simulation.food=0;w.Simulation.consumed+=removed;}
                if(urgent=="cancel")w.Simulation.CancelWork(leader);
                Step(w,.1f);Need(w.Simulation.residents.All(x=>x.socialPartner==0),"Social blocks "+urgent);rows.Add("Interrupt "+urgent+" / pair and reservations valid: PASS");
            }
            w.RestoreSnapshot(talking);var bad=AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(talking));bad.simulation.residents[partner].state.socialPartner=0;
            bool rejected=false;try{w.RestoreSnapshot(bad);}catch{rejected=true;}Need(rejected&&JsonUtility.ToJson(w.CaptureSnapshot())==text,"Invalid pair changed active state");rows.Add("Invalid asymmetric partner rejected without mutation: PASS");
            foreach(var fixture in new[]{"Logs/Save143UI/AutonomousLife142.json","Logs/Save144Observation/AutonomousLife142.json","Logs/Save145Observation/AutonomousLife142.json"}){
                string original=File.ReadAllText(fixture);var old=AutonomousSaveCodec.Decode(original);w.RestoreSnapshot(old);var current=w.CaptureSnapshot();
                var compare=AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(current));compare.simulation.sites=compare.simulation.sites.Take(old.simulation.sites.Length).ToArray();
                Need(JsonUtility.ToJson(compare.simulation)==JsonUtility.ToJson(old.simulation)&&JsonUtility.ToJson(current.camera)==JsonUtility.ToJson(old.camera)&&current.day==old.day&&current.hour==old.hour,"Legacy state lost");
                if(old.sourceVersion>=144)Need(JsonUtility.ToJson(current.weather)==JsonUtility.ToJson(old.weather),"Weather lost");
                if(old.sourceVersion>=145)Need(JsonUtility.ToJson(current.season)==JsonUtility.ToJson(old.season),"Season lost");
                string dir="Logs/Save146Legacy-"+old.sourceVersion+"-"+DateTime.UtcNow.Ticks;Directory.CreateDirectory(dir);var repo=new AutonomousSaveRepository(dir,x=>w.ValidateSnapshot(x));File.WriteAllText(repo.Path,original);repo.Write(current);Need(File.ReadAllText(repo.Path+".v"+old.sourceVersion)==original,"Legacy not archived");rows.Add("Legacy "+old.sourceVersion+" state and archive: PASS");
            }
            w.Initialize();d.paused=false;d.Playback.SetRate(1);int encounters=0;bool active=false;
            for(int i=0;i<72000;i++){
                w.Weather.Advance(.05f);w.Season.Advance(.05f,10);w.Simulation.Raining=w.Weather.Kind==LifeWeather.Rain;w.Simulation.Winter=w.Season.Winter;w.Simulation.Tick(.05f,Mathf.Repeat(12+i*.05f*24/600,24));if(i%20==0)w.Simulation.Validate();
                bool next=w.Simulation.residents.Any(r=>r.socialPartner>0);if(next&&!active)encounters++;active=next;
            }
            w.ValidateSnapshot(w.CaptureSnapshot());
            Need(encounters>1&&w.Simulation.homeArrivals>=18&&w.Simulation.meals>0&&w.Simulation.produced>0,"Six-day social/life regression");rows.Add("Six days natural life/weather/seasons/food/reservations: PASS encounters="+encounters+" home="+w.Simulation.homeArrivals);
            foreach(float rate in new[]{.5f,1,4}){
                w.RestoreSnapshot(talking);d.Playback.SetRate(Array.IndexOf(FarmPlaybackSettings.Rates,rate));float before=w.Simulation.residents[leader].socialTime;w.Advance(.1f);Need(Mathf.Abs(w.Simulation.residents[leader].socialTime-before-.1f*rate)<.0001f,"Social speed mismatch");
            }
            rows.Add("0.5/1/4x common social clock: PASS");
            w.RestoreSnapshot(paired);d.paused=true;d.Playback.SetRate(0);ui.FocusResident(leader);
            var preview=w.CaptureSnapshot();string previewDir="Logs/Save146UI";Directory.CreateDirectory(previewDir);File.WriteAllText(previewDir+"/AutonomousLife142.json",AutonomousSaveCodec.Encode(preview));
            File.WriteAllLines("Docs/AutonomousLife146Verification.txt",rows);Debug.Log("LIFE146_VERIFY_OK\n"+string.Join("\n",rows));
            AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE146_BUILD_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
