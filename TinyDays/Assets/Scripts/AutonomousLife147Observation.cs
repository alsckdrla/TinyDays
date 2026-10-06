using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using TinyDays.Review;
namespace TinyDays.Life
{
    sealed class AutonomousLife147Observation:MonoBehaviour
    {
        IEnumerator Start()
        {
            var w=GetComponent<AutonomousLifeWorld>();var d=GetComponent<FarmLifeDirector>();
            float start=Time.realtimeSinceStartup,next=60;int shelter=0;
            Debug.Log("LIFE147_OBSERVATION_START");
            while(Time.realtimeSinceStartup-start<600){yield return new WaitForSecondsRealtime(1);
                try{
                    w.Simulation.Validate();w.ValidateSnapshot(w.CaptureSnapshot());
                    if(d.paused||d.Playback.Rate!=1||d.Playback.DayMinutes!=10)throw new Exception("Observation controls changed");
                    var s=w.Simulation;var p=s.Poultry;if(p.hens.All(h=>h.inside))shelter++;
                    if(Time.realtimeSinceStartup-start>=next){Debug.Log($"LIFE147_SAMPLE wall={Time.realtimeSinceStartup-start:F1} food={s.food} produced={s.produced} consumed={s.consumed} feedSpent={p.feedSpent} laid={p.laid} collected={p.collected} sheltered={shelter}");next+=60;}
                }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            }
            var final=w.Simulation.Poultry;
            if(final.feedSpent<1||final.laid<1||final.collected<1||shelter<10){Debug.LogError("LIFE147_OBSERVATION_INCOMPLETE");Application.Quit(1);yield break;}
            Debug.Log($"LIFE147_REALTIME_10MIN_OK wall={Time.realtimeSinceStartup-start:F1} feed={final.feedSpent} laid={final.laid} collected={final.collected} shelter={shelter}");Application.Quit(0);
        }
    }
}
