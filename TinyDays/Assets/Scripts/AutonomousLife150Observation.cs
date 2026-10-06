using System;
using System.Collections;
using UnityEngine;
using TinyDays.Review;
namespace TinyDays.Life
{
    sealed class AutonomousLife150Observation:MonoBehaviour
    {
        IEnumerator Start()
        {
            var w=GetComponent<AutonomousLifeWorld>();var d=GetComponent<FarmLifeDirector>();
            w.Simulation.SetGrowthDirection(false);float start=Time.realtimeSinceStartup,next=60;
            Debug.Log("LIFE150_OBSERVATION_START existing isolated fixture="+GetComponent<AutonomousLifeSave>().HasSave);
            while(Time.realtimeSinceStartup-start<1200){yield return new WaitForSecondsRealtime(1);
                try{w.Simulation.Validate();w.ValidateSnapshot(w.CaptureSnapshot());if(d.paused||d.Playback.Rate!=1||d.Playback.DayMinutes!=10)throw new Exception("Observation controls changed");
                    if(Time.realtimeSinceStartup-start>=next){var s=w.Simulation;Debug.Log($"LIFE150_SAMPLE wall={Time.realtimeSinceStartup-start:F1} food={s.food}/{s.Capacity} garden={s.Garden.stage}/{s.Garden.progress:F1} residents={s.residents.Length} field={s.Field.stage} pressure={s.Field.pressure:F1} work={s.Field.progress:F1} warehouse={s.Growth.stage} meals={s.meals} home={s.homeArrivals}");next+=60;}
                }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}}
            Debug.Log($"LIFE150_REALTIME_20MIN_OK wall={Time.realtimeSinceStartup-start:F1} garden={w.Simulation.Garden.stage}");Application.Quit(0);
        }
    }
}
