using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TinyDays.Review;
public static class AdultSideRiseRotationChecks {
    public static void Execute(){try{Run();Debug.Log("SIDE_RISE_ROTATION_OK");}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Run(){
        AdultRabbitMotionBuilder.BuildOnly();var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        var bones=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name=="Head").bones;
        var legs=new[]{"Thigh_L","Shin_L","Foot_L","Thigh_R","Shin_R","Foot_R"}.Select(n=>bones.First(b=>b.name==n)).ToArray();
        var spine=bones.First(b=>b.name=="Spine");var head=bones.First(b=>b.name=="Head");
        var lines=new List<string>{"clip,mode,time,chest_inclination,head_y,"+string.Join(",",legs.SelectMany(b=>new[]{b.name+"_step_deg",b.name+"_speed_deg_s"}))};
        var report=new List<string>{"v0.114 240Hz raw/runtime angular continuity. Anatomical right=legacy L; left=legacy R. Not user quality approval."};
        float globalPeak=0,endPeak=0;
        foreach(int clip in new[]{32,33})foreach(bool live in new[]{false,true}){
            r.Select(clip);var previous=new Quaternion[legs.Length];var peaks=new float[legs.Length];float tilt=0;
            int count=Mathf.RoundToInt(r.clips[clip].length*240);
            for(int f=0;f<=count;f++){
                float t=f/240f;if(live)r.Sample(t);else r.clips[clip].SampleAnimation(r.resident,t);
                float inclination=Vector3.Angle(head.position-spine.position,Vector3.up);if(clip==32&&t>=1.2f)tilt=Mathf.Max(tilt,inclination);
                var values=new List<string>{clip.ToString(),live?"runtime":"raw",t.ToString("F6",System.Globalization.CultureInfo.InvariantCulture),inclination.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),head.position.y.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)};
                for(int j=0;j<legs.Length;j++){
                    float step=f==0?0:Quaternion.Angle(previous[j],legs[j].rotation);previous[j]=legs[j].rotation;peaks[j]=Mathf.Max(peaks[j],step);globalPeak=Mathf.Max(globalPeak,step);if(f==count)endPeak=Mathf.Max(endPeak,step);
                    values.Add(step.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));values.Add((step*240).ToString("F6",System.Globalization.CultureInfo.InvariantCulture));
                }
                lines.Add(string.Join(",",values));
            }
            report.Add($"{clip} {(live?"runtime":"raw")}: max angular step "+string.Join(", ",legs.Select((b,i)=>b.name+"="+peaks[i].ToString("F4")))+$" deg; seated settle chest inclination {tilt:F3}deg.");
        }
        report.Add($"Overall max {globalPeak:F4}deg / 240Hz; endpoint max {endPeak:F4}deg.");
        File.WriteAllLines("Docs/AdultSideRiseRotation.csv",lines);File.WriteAllLines("Docs/AdultSideRiseRotation.txt",report);
        if(globalPeak>4||endPeak>.2f)throw new Exception("Leg angular spike or endpoint discontinuity");
    }
}
