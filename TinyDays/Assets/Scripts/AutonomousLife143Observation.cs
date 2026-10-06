using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    // Explicit, isolated integration observation; never installed in normal play.
    sealed class AutonomousLife143Observation : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-observeLife143");
            var world=GetComponent<AutonomousLifeWorld>();var save=GetComponent<AutonomousLifeSave>();
            var director=GetComponent<FarmLifeDirector>();
            string expected=Path.Combine(Path.GetDirectoryName(save.SavePath),"Expected143.json");
            bool resume=index+1<args.Length&&args[index+1]=="resume";
            if(resume){
                try{
                    var prior=AutonomousSaveCodec.Decode(File.ReadAllText(expected));
                    if(JsonUtility.ToJson(prior)!=JsonUtility.ToJson(world.CaptureSnapshot()))throw new Exception("Restart state mismatch");
                    Debug.Log("LIFE143_RESTART_EXACT_OK");director.paused=false;
                }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            }else if(save.HasSave||director.paused||director.Playback.Rate!=1||director.Playback.DayMinutes!=10){
                Debug.LogError("LIFE143_REQUIRES_FRESH_10MIN_1X_WORLD");Application.Quit(1);yield break;
            }
            float start=Time.realtimeSinceStartup,next=60,duration=resume?60:1800;
            int saveChanges=0;DateTime lastWrite=DateTime.MinValue;float savedElapsed=-1;
            float initialElapsed=world.Simulation.elapsed;int initialCompleted=world.Simulation.residents.Sum(r=>r.completed);
            Debug.Log("LIFE143_OBSERVATION_START resume="+resume+" duration="+duration);
            while(Time.realtimeSinceStartup-start<duration){
                yield return new WaitForSecondsRealtime(1);
                try{
                    world.Simulation.Validate();
                    if(director.paused||director.Playback.Rate!=1||director.Playback.DayMinutes!=10)throw new Exception("Observation playback altered");
                    if(File.Exists(save.SavePath)){
                        var stamp=File.GetLastWriteTimeUtc(save.SavePath);
                        if(stamp!=lastWrite){
                            var written=AutonomousSaveCodec.Decode(File.ReadAllText(save.SavePath));world.ValidateSnapshot(written);
                            if(written.simulation.elapsed<savedElapsed)throw new Exception("Autosave time moved backwards");
                            lastWrite=stamp;savedElapsed=written.simulation.elapsed;saveChanges++;
                        }
                    }
                    float wall=Time.realtimeSinceStartup-start;
                    if(wall>=next){
                        var s=world.Simulation;var clock=GetComponent<FarmLightingStudy>();
                        Debug.Log($"LIFE143_SAMPLE wall={wall:F1} elapsed={s.elapsed:F1} day={clock.Day} hour={clock.Hour:F2} food={s.food} produced={s.produced} consumed={s.consumed} rests={s.rests} home={s.homeArrivals} saves={saveChanges} actions={string.Join(",",s.residents.Select(r=>r.action.ToString()))}");next+=60;
                    }
                }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            }
            try{
                var s=world.Simulation;
                if(s.elapsed-initialElapsed<duration-3)throw new Exception("Simulation did not keep pace with wall time");
                if(!resume&&(saveChanges<28||s.produced<=0||s.consumed<=0||s.rests<=0||s.homeArrivals<9||GetComponent<FarmLightingStudy>().Day<4))throw new Exception("Incomplete multi-day life/save observation");
                if(resume&&s.residents.Sum(r=>r.completed)<=initialCompleted)throw new Exception("Resident activities did not resume");
                Debug.Log(resume?"LIFE143_RESUMED_LIFE_60SEC_OK":"LIFE143_REALTIME_30MIN_SAVE_OK");
                // Freeze only after the uninterrupted observation, to compare exact process restart state.
                director.paused=true;
                File.WriteAllText(expected,AutonomousSaveCodec.Encode(world.CaptureSnapshot()));
                Debug.Log("LIFE143_QUIT_EXPECTED_READY");Application.Quit(0);
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);}
        }
    }
}
