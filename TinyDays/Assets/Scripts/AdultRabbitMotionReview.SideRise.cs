using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace TinyDays.Review {
public sealed partial class AdultRabbitMotionReview {
    void FinishDirectSideReturn(bool seated,SideDestination destination){
        double phase=restBreathPhase;
        var saved=new Dictionary<Renderer,bool>(backpackVisibility);
        StartBreathing(seated);Idle.SetBreathingPhase(phase);
        foreach(var p in saved)backpackVisibility[p.Key]=p.Value;
        HideBackpack();
        if(destination==SideDestination.Walk||destination==SideDestination.Run)RequestRun(destination==SideDestination.Run);
        else if(destination==SideDestination.Seated&&!seated){RequestSit();followBreathing=true;}
        else if(destination==SideDestination.Standing&&seated)RequestStand();
        else if(destination==SideDestination.Supine)RequestLie();
    }
    void SampleDirectSideBreath(double time){
        // Sample the authored pose before layering the continuous rest breath.
        clips[selected].SampleAnimation(resident,Mathf.Clamp((float)time,0,clips[selected].length));
        var joints=resident.GetComponentsInChildren<Transform>();
        var p=joints.Select(j=>j.localPosition).ToArray();var q=joints.Select(j=>j.localRotation).ToArray();
        var bones=resident.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name=="Head").bones;
        var ends=new[]{"Hand_L","Hand_R","Foot_L","Foot_R"}.Select(n=>bones.First(b=>b.name==n)).ToArray();
        var targets=ends.Select(b=>b.position).ToArray();var rotations=ends.Select(b=>b.rotation).ToArray();
        var bends=new List<Vector3>();
        for(int i=0;i<ends.Length;i++){
            string suffix=i%2==0?"L":"R",upper=i<2?"UpperArm_":"Thigh_",lower=i<2?"Forearm_":"Shin_";
            bends.Add(bones.First(b=>b.name==lower+suffix).position-bones.First(b=>b.name==upper+suffix).position);
        }
        double phase=liveRestBreath?restBreathPhase:time/4;float normalized=(float)(phase-Math.Floor(phase));
        float duration=clips[selected].length,t=Mathf.Clamp((float)time,0,duration);
        bool entering=IsDirectSideEntry(selected);
        int first=entering?(selected==35?9:8):28,last=entering?28:selected==32?9:8;
        float from=1-SideEase(Mathf.Clamp01(t/.9f));
        float to=SideEase(Mathf.Clamp01((t-.7f)/(duration-.7f)));
        var dp=new Vector3[joints.Length];var dq=Enumerable.Repeat(Quaternion.identity,joints.Length).ToArray();
        foreach(var reference in new[]{first,last}){
            clips[reference].SampleAnimation(resident,0);
            var neutral=joints.Select(j=>j.localPosition).ToArray();var neutralQ=joints.Select(j=>j.localRotation).ToArray();
            clips[reference].SampleAnimation(resident,normalized*clips[reference].length);
            float weight=reference==first?from:to;
            // The braced descent has less chest freedom than resting. Preserve
            // phase while easing the additive breath around loaded arm contact.
            if(entering)weight*=1-.85f*Mathf.Pow(Mathf.Sin(Mathf.PI*t/duration),2);
            for(int i=0;i<joints.Length;i++){
                dp[i]+=(joints[i].localPosition-neutral[i])*weight;
                dq[i]*=Quaternion.Slerp(Quaternion.identity,Quaternion.Inverse(neutralQ[i])*joints[i].localRotation,weight);
            }
        }
        for(int i=0;i<joints.Length;i++){joints[i].localPosition=p[i]+dp[i];joints[i].localRotation=q[i]*dq[i];}
        for(int i=0;i<ends.Length;i++){
            if(!entering&&i<2&&targets[i].y-resident.transform.position.y>.17f)continue;
            string suffix=i%2==0?"L":"R",upper=i<2?"UpperArm_":"Thigh_",lower=i<2?"Forearm_":"Shin_";
            new IdleSupportLeg{Upper=bones.First(b=>b.name==upper+suffix),Lower=bones.First(b=>b.name==lower+suffix),End=ends[i]}.Solve(targets[i],rotations[i],resident.transform.forward,bends[i]);
        }
        if(entering&&t>duration-.3f){
            // Release support correction into the exact live resting posture;
            // the free hand should follow the chest at the shared endpoint.
            var bp=joints.Select(j=>j.localPosition).ToArray();var bq=joints.Select(j=>j.localRotation).ToArray();
            SampleBreathReferences(phase,0,28,30);float w=SideEase(Mathf.Clamp01((t-duration+.3f)/.3f));
            for(int i=0;i<joints.Length;i++){joints[i].localPosition=Vector3.Lerp(bp[i],joints[i].localPosition,w);joints[i].localRotation=Quaternion.Slerp(bq[i],joints[i].localRotation,w);}
        }
        ApplySideEntry(time);
    }
}}
