using System;
using System.Collections;
using UnityEngine;
using TinyDays.Review;
namespace TinyDays.Life
{
    sealed class AutonomousLife148Observation:MonoBehaviour
    {
        IEnumerator Start()
        {
            var w=GetComponent<AutonomousLifeWorld>();var d=GetComponent<FarmLifeDirector>();
            w.Simulation.SetGrowthDirection(true);float start=Time.realtimeSinceStartup,next=60;
            Debug.Log("LIFE148_OBSERVATION_START");
            while(Time.realtimeSinceStartup-start<1800&&(Time.realtimeSinceStartup-start<1200||w.Simulation.Growth.stage!=3)){yield return new WaitForSecondsRealtime(1);
                try{w.Simulation.Validate();w.ValidateSnapshot(w.CaptureSnapshot());if(d.paused||d.Playback.Rate!=1||d.Playback.DayMinutes!=10)throw new Exception("Observation controls changed");
                    if(Time.realtimeSinceStartup-start>=next){var s=w.Simulation;Debug.Log($"LIFE148_SAMPLE wall={Time.realtimeSinceStartup-start:F1} food={s.food}/{s.Capacity} stage={s.Growth.stage} pressure={s.Growth.pressure:F1} wood={s.Growth.wood} progress={s.Growth.progress:F1} meals={s.meals} home={s.homeArrivals}");next+=60;}
                }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}}
            if(w.Simulation.Growth.stage!=3){Debug.LogError("LIFE148_EXPANSION_INCOMPLETE");Application.Quit(1);yield break;}
            Debug.Log($"LIFE148_REALTIME_20MIN_OK wall={Time.realtimeSinceStartup-start:F1}");Application.Quit(0);
        }
    }
}
