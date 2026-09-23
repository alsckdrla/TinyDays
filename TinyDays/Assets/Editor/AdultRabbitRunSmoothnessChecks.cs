using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TinyDays.Review;

// Same-time-axis, 240Hz source/runtime measurements; never substitutes for user input.
public static class AdultRabbitRunSmoothnessChecks
{
    const float Dt=1f/240;
    public static void CaptureBefore()=>Measure("BeforeV087",false);
    public static void Execute()=>Measure("V088",true);
    static void Measure(string version,bool check){
        typeof(AdultRabbitMotionBuilder).GetMethod("Build",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        var bones=r.resident.GetComponentsInChildren<Transform>();
        var pelvis=bones.Single(b=>b.name=="Pelvis");var head=bones.Single(b=>b.name=="Head"&&!b.GetComponent<Renderer>());
        var shoes=r.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Shoes");
        var mesh=shoes.sharedMesh;var shoeVertices=mesh.vertices;var weights=mesh.boneWeights;
        var footBone=new[]{"Foot_L","Foot_R"}.Select(name=>Array.FindIndex(shoes.bones,b=>b.name==name)).ToArray();
        var shoeIndices=footBone.Select(b=>Enumerable.Range(0,shoeVertices.Length).Where(i=>weights[i].boneIndex0==b&&weights[i].weight0>.99f).ToArray()).ToArray();
        Func<int,float> sole=k=>{var matrix=shoes.bones[footBone[k]].localToWorldMatrix*mesh.bindposes[footBone[k]];return shoeIndices[k].Min(i=>matrix.MultiplyPoint3x4(shoeVertices[i]).y);};
        var lines=new List<string>{"mode,time,phase,pelvis,head,pelvis_velocity,head_velocity,pelvis_acceleration,head_acceleration,source_pelvis,reach_drop,safety_drop,left_planted,right_planted"};
        var report=new List<string>();bool ok=true;
        foreach(bool moving in new[]{false,true}){
            if(moving){r.BeginMovement();r.slow=false;r.RequestRun(true);r.Advance(1.2f);}else r.Select(4);
            int count=Mathf.RoundToInt(AdultRunTiming.Data.duration/Dt)*2;
            var ps=new List<float>();var hs=new List<float>();float lastVP=0,lastVH=0,maxAP=0,maxAH=0,maxVStepP=0,maxVStepH=0,maxSafety=0,maxCorrection=0;int turnsP=0,turnsH=0,signP=0,signH=0;
            float highest=-100,peakPhase=0,peakSole=0,nearestApex=100,apexLeft=0,apexRight=0;bool peakPlanted=false;
            for(int i=0;i<=count;i++){
                if(moving){if(i>0)r.Advance(Dt);}else r.Sample(i*(double)Dt);
                float p=pelvis.position.y,h=head.position.y,vp=i==0?0:(p-ps[i-1])/Dt,vh=i==0?0:(h-hs[i-1])/Dt;
                float ap=i<2?0:(vp-lastVP)/Dt,ah=i<2?0:(vh-lastVH)/Dt;
                if(i>1){maxAP=Mathf.Max(maxAP,Mathf.Abs(ap));maxAH=Mathf.Max(maxAH,Mathf.Abs(ah));maxVStepP=Mathf.Max(maxVStepP,Mathf.Abs(vp-lastVP));maxVStepH=Mathf.Max(maxVStepH,Mathf.Abs(vh-lastVH));}
                if(i>0){int sp=Mathf.Abs(vp)<.015f?0:Math.Sign(vp),sh=Mathf.Abs(vh)<.015f?0:Math.Sign(vh);if(sp!=0){if(signP!=0&&sp!=signP)turnsP++;signP=sp;}if(sh!=0){if(signH!=0&&sh!=signH)turnsH++;signH=sh;}}
                var t=r.FootTransition;float src=moving?t.SourcePelvisHeight:p,reach=moving?t.ReachDrop:0,safety=moving?t.SafetyDrop:0;maxSafety=Mathf.Max(maxSafety,safety);
                maxCorrection=Mathf.Max(maxCorrection,Mathf.Abs(p-src));
                double phase=moving?t.WalkTime/.8%1:(i*Dt/AdultRunTiming.Data.duration)%1;
                float halfPhase=(float)(phase%.5),ls=sole(0),rs=sole(1);
                if(p>highest){highest=p;peakPhase=halfPhase;peakSole=Mathf.Min(ls,rs);peakPlanted=moving&&(t.LeftPlanted||t.RightPlanted);}
                float apexDistance=Mathf.Abs(halfPhase-(AdultRunTiming.Data.stance+.5f)/2);
                if(apexDistance<nearestApex){nearestApex=apexDistance;apexLeft=ls;apexRight=rs;}
                lines.Add(FormattableString.Invariant($"{(moving?"moving":"authored")},{i*Dt:F6},{phase:F6},{p:F7},{h:F7},{vp:F6},{vh:F6},{ap:F4},{ah:F4},{src:F7},{reach:F7},{safety:F7},{(moving&&t.LeftPlanted?1:0)},{(moving&&t.RightPlanted?1:0)}"));
                ps.Add(p);hs.Add(h);lastVP=vp;lastVH=vh;
            }
            float pr=ps.Max()-ps.Min(),hr=hs.Max()-hs.Min();
            report.Add(FormattableString.Invariant($"{(moving?"moving":"authored")}: pelvis range {pr:F6}m; head range {hr:F6}m; max acceleration {maxAP:F3}/{maxAH:F3}m/s2; max 240Hz velocity change {maxVStepP:F6}/{maxVStepH:F6}m/s; direction reversals {turnsP}/{turnsH} in two cycles; max safety drop {maxSafety:F6}m; max runtime height correction {maxCorrection:F6}m."));
            report.Add(FormattableString.Invariant($"Flight: body peak phase {peakPhase:F6}; minimum sole at peak {peakSole:F6}m; nearest midpoint soles L/R {apexLeft:F6}/{apexRight:F6}m; any planted at peak {peakPlanted}."));
            if(check)ok&=pr>=.079f*.9f&&pr<=.079f*1.1f&&hr>=.079f*.9f&&hr<=.079f*1.1f&&turnsP<=8&&turnsH<=8&&maxAP<30&&maxAH<30&&maxSafety<.001f&&maxCorrection<.001f;
            if(check)ok&=peakPhase>AdultRunTiming.Data.stance&&peakPhase<.5f&&!peakPlanted&&peakSole>=.02f&&apexLeft>=.02f&&apexRight>=.02f;
        }
        if(check){
            // Display frame rate must not change the underlying 240Hz curve.
            var reference=new List<Vector2>();float maxFrameDifference=0;
            foreach(int fps in new[]{120,60,30}){
                r.BeginMovement();r.slow=false;r.RequestRun(true);r.Advance(1.2f);
                int frameCount=Mathf.RoundToInt(AdultRunTiming.Data.duration*2*fps);
                for(int frame=0;frame<=frameCount;frame++){
                    if(frame>0)r.Advance(1f/fps);var position=new Vector2(pelvis.position.y,head.position.y);
                    if(fps==120)reference.Add(position);else{int index=frame*120/fps;if(index<reference.Count)maxFrameDifference=Mathf.Max(maxFrameDifference,Vector2.Distance(position,reference[index]));}
                }
            }
            report.Add($"30/60/120fps common-time pelvis/head difference {maxFrameDifference:F7}m.");ok&=maxFrameDifference<.001f;
        }
        report.Add(check?(ok?"PASS numerical smoothness; actual input and user quality approval pending":"INCOMPLETE smoothness criteria"):"BASELINE; not a smoothness pass");
        File.WriteAllLines("Docs/AdultRabbitRunSmoothness"+version+".csv",lines);File.WriteAllLines("Docs/AdultRabbitRunSmoothness"+version+".txt",report);
        r.Select(0);if(check&&!ok){Debug.LogError(string.Join("\n",report));if(Application.isBatchMode)EditorApplication.Exit(1);}else Debug.Log("RUN_SMOOTHNESS_OK");
    }
}
