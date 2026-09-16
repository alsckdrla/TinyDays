using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;

public static class FarmClockChecks
{
    public static void Execute()
    {
        try
        {
            FarmLightingChecks.Execute();
            var l=UnityEngine.Object.FindObjectOfType<FarmLightingStudy>();
            l.RestartClock();Require(l.Automatic&&l.Day==1&&l.Hour==12,"initial noon");
            l.Advance(75,false);Require(Mathf.Abs(l.Hour-18)<.001f,"quarter day");
            l.Advance(100,true);Require(Mathf.Abs(l.Hour-18)<.001f,"pause");
            l.Apply(0);l.Advance(70,false);Require(!l.Automatic&&l.Hour==6,"fixed time");
            l.ResumeClock();Require(l.Hour==6,"resume position");int day=l.Day;l.Advance(300,false);Require(Mathf.Abs(l.Hour-6)<.001f&&l.Day==day+1,"whole cycle");
            foreach(int t in new[]{3,0,1,2})
            {
                l.Apply(t);l.ResumeClock();l.Advance(74.999f,false);var color=l.sun.color;var rotation=l.sun.transform.rotation;var sky=RenderSettings.ambientSkyColor;
                l.Advance(.002f,false);
                Require(Vector4.Distance(color,l.sun.color)<.001f&&Quaternion.Angle(rotation,l.sun.transform.rotation)<.1f&&Vector4.Distance(sky,RenderSettings.ambientSkyColor)<.001f,"boundary continuity");
            }
            l.RestartClock();l.Advance(900,false);Require(l.Day==4&&Mathf.Abs(l.Hour-12)<.001f,"multiple days");int fixedDay=l.Day;l.Apply(0);Require(l.Day==fixedDay&&l.Hour==6,"fixed time keeps day");
            var d=l.GetComponent<FarmLifeDirector>();d.paused=true;d.Restart();l.RestartClock();Require(d.paused&&d.elapsed==0&&l.Day==1&&l.Hour==12&&l.Automatic,"restart keeps pause");d.paused=false;
            l.Apply(3);var o=l.GetComponent<FarmCameraOcclusion>();o.Fade(new Vector3(0,1,7.1f),new Vector3(0,1,7.2f),.2f,false);
            l.ResumeClock();l.Advance(50,false);o.Restore();
            l.RestartClock();
            File.WriteAllText("Docs/Stage27ClockVerification.txt","v0.47: Day 1/noon start, midnight day increase, multi-day advance, fixed-time day preservation, restart Day 1, 300-second cycle, fixed/resume, pause, restart preserving pause, four boundaries including midnight, lighting/occlusion update PASS. Representative lighting regression PASS. Direct checks, not live input or five-minute observation. User confirmation pending.\n");
            Debug.Log("STAGE27_CLOCK_OK");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
}
