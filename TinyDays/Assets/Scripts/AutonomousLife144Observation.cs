using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    sealed class AutonomousLife144Observation:MonoBehaviour
    {
        IEnumerator Start()
        {
            var world=GetComponent<AutonomousLifeWorld>();var playback=GetComponent<FarmLifeDirector>();
            float start=Time.realtimeSinceStartup,next=60;int rainSamples=0,shelterSamples=0;
            Debug.Log("LIFE144_OBSERVATION_START");
            while(Time.realtimeSinceStartup-start<600){
                yield return new WaitForSecondsRealtime(1);
                try{
                    world.Simulation.Validate();world.ValidateSnapshot(world.CaptureSnapshot());
                    if(playback.paused||playback.Playback.Rate!=1||playback.Playback.DayMinutes!=10||!world.Weather.State.automatic)throw new Exception("Observation playback changed");
                    if(world.Weather.Kind==LifeWeather.Rain)rainSamples++;
                    if(world.Simulation.residents.Any(r=>r.action==LifeAction.Shelter&&!r.Moving))shelterSamples++;
                    if(Time.realtimeSinceStartup-start>=next){var s=world.Simulation;Debug.Log($"LIFE144_SAMPLE wall={Time.realtimeSinceStartup-start:F1} day={GetComponent<FarmLightingStudy>().Day} weather={world.Weather.Label} food={s.food} produced={s.produced} consumed={s.consumed} home={s.homeArrivals} shelterSamples={shelterSamples}");next+=60;}
                }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            }
            if(rainSamples<100||shelterSamples<10||world.Simulation.homeArrivals<3||world.Simulation.consumed<1){Debug.LogError("LIFE144_OBSERVATION_INCOMPLETE");Application.Quit(1);yield break;}
            Debug.Log("LIFE144_REALTIME_10MIN_OK rainSamples="+rainSamples+" shelterSamples="+shelterSamples);Application.Quit(0);
        }
    }
}
