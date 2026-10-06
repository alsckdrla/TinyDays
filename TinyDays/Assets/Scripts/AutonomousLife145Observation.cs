using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using TinyDays.Review;
namespace TinyDays.Life
{
    sealed class AutonomousLife145Observation:MonoBehaviour
    {
        IEnumerator Start()
        {
            var w=GetComponent<AutonomousLifeWorld>();var d=GetComponent<FarmLifeDirector>();w.SelectSeason(3);
            float start=Time.realtimeSinceStartup,next=60;int snow=0,shelter=0;
            Debug.Log("LIFE145_WINTER_START");
            while(Time.realtimeSinceStartup-start<600){yield return new WaitForSecondsRealtime(1);
                try{
                    w.Simulation.Validate();w.ValidateSnapshot(w.CaptureSnapshot());
                    if(d.paused||d.Playback.Rate!=1||d.Playback.DayMinutes!=10||!w.Season.Winter)throw new Exception("Winter observation changed");
                    if(w.Weather.Kind==LifeWeather.Rain)snow++;if(w.Simulation.residents.Any(r=>r.action==LifeAction.Shelter&&!r.Moving))shelter++;
                    if(Time.realtimeSinceStartup-start>=next){var s=w.Simulation;Debug.Log($"LIFE145_SAMPLE wall={Time.realtimeSinceStartup-start:F1} day={GetComponent<FarmLightingStudy>().Day} weather={w.WeatherLabel} food={s.food} produced={s.produced} consumed={s.consumed} home={s.homeArrivals} shelter={shelter}");next+=60;}
                }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            }
            if(snow<100||shelter<10||w.Simulation.homeArrivals<3||w.Simulation.consumed<1){Debug.LogError("LIFE145_OBSERVATION_INCOMPLETE");Application.Quit(1);yield break;}
            Debug.Log($"LIFE145_REALTIME_10MIN_OK snow={snow} shelter={shelter}");Application.Quit(0);
        }
    }
}
