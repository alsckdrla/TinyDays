using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;

public static class FarmPlaybackChecks
{
    public static void Execute()
    {
        try
        {
            FarmClockChecks.Execute();
            var l=UnityEngine.Object.FindObjectOfType<FarmLightingStudy>();var d=l.GetComponent<FarmLifeDirector>();
            foreach(int minutes in new[]{1,5,10,20,30,120})for(int i=0;i<4;i++)
            {
                Require(d.Playback.SetDayMinutes(minutes.ToString()),"valid minutes");d.Playback.SetRate(i);d.paused=false;d.Restart();l.RestartClock();
                float seconds=minutes*60/(4*d.Playback.Rate);
                d.Advance(seconds);l.Advance(seconds,d.paused);
                Require(Mathf.Abs(l.Hour-18)<.001f&&Math.Abs(d.elapsed-seconds*d.Playback.Rate)<.001,"shared rate");
                l.Apply(0);double before=d.elapsed;d.Advance(1);l.Advance(1,false);
                Require(l.Hour==6&&Math.Abs(d.elapsed-before-d.Playback.Rate)<.001,"fixed lighting allows residents");
            }
            foreach(string bad in new[]{"","0","121","-1","abc","1.5"})Require(!d.Playback.SetDayMinutes(bad)&&d.Playback.DayMinutes==120,"invalid input");
            var hour=l.Hour;var elapsed=d.elapsed;d.Playback.SetDayMinutes("10");d.Playback.SetRate(1);
            Require(l.Hour==hour&&d.elapsed==elapsed,"setting change must not rewind");
            d.paused=true;l.ResumeClock();d.Advance(10);l.Advance(10,true);
            Require(l.Hour==hour&&d.elapsed==elapsed,"pause");d.Restart();l.RestartClock();
            Require(d.paused&&l.Hour==12&&d.elapsed==0&&d.Playback.DayMinutes==10,"restart preserves settings and pause");
            d.Playback.SetDayMinutes("5");d.Playback.SetRate(1);d.paused=false;
            File.WriteAllText("Docs/Stage27PlaybackVerification.txt","v0.46: 1/5/10/20/30/120 minutes x 0.5/1/2/4 speeds, shared elapsed time, fixed lighting with moving residents, invalid input rejection, no rewind on settings changes, pause and restart preserving settings PASS. Clock and lighting regression PASS. Direct checks, not live UI or long-term performance certification.\n");
            Debug.Log("STAGE27_PLAYBACK_OK");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
}
