using System;
using System.Linq;
using UnityEngine;
namespace TinyDays.Review {
public sealed partial class AdultRabbitMotionReview {
    public static bool IsSideClip(int i)=>(i>=26&&i<=28)||IsDirectSideReturn(i)||IsDirectSideEntry(i);
    public static bool IsDirectSideReturn(int i)=>i==32||i==33;
    public static bool IsDirectSideEntry(int i)=>i==34||i==35;
    public enum SideDestination { None, Supine, Seated, Standing, Walk, Run }
    public SideDestination SidePending {get;private set;}
    bool sideFollow;
    Transform[] sideJoints;
    Vector3[] sideBaseP,sideNeutralP,sideDeltaP;
    Quaternion[] sideBaseQ,sideNeutralQ,sideDeltaQ;
    Transform[] sideEntryJoints;
    Vector3[] sideEntryOffset;
    Quaternion[] sideEntryRotation;
    public string SideStatus=>selected==34?"서기 → 왼쪽으로 눕는 중":selected==35?"앉기 → 왼쪽으로 눕는 중":selected==32?"옆누움 → 직접 앉는 중":selected==33?"옆누움 → 직접 일어서는 중":selected==26?"왼쪽으로 돌아눕는 중":selected==27?"바로 눕기로 복귀 중":"왼쪽 누움 · 기본 호흡";
    public static readonly string[] SideDownLabels={"시작","체중 이동","무릎·손 지지","골반 낮추기","아래팔 지지","머리 놓기","안정","팔베개"};
    public static readonly string[] DirectSideLabels={"팔베개","머리 들기","손 지지","상체 밀기","다리 정리","체중 이동","안정","끝 자세"};
    public static readonly string[] SideLabels={"시작","손 옮기기","무릎 정리","골반 회전","가슴 따라가기","머리 놓기","안정","끝 자세"};
    void ResetSide(){sideFollow=false;SidePending=SideDestination.None;sideJoints=null;sideEntryJoints=null;}
    public void RequestLeftSide(){
        if(clips.Length<29||IsSideClip(selected)||LyingTransition||Posture!=RestPosture.Supine)return;
        if(selected==23||selected==24||selected==25){WakeFor(WakeDestination.LeftSide);return;}
        BeginSide(26,SideDestination.None);
    }
    public void RequestSupineReturn(){
        if(IsSideSleep(selected)){WakeFor(WakeDestination.Supine);return;}
        if(!IsSideClip(selected))return;
        sideFollow=true;paused=false;
        SidePending=SideDestination.Supine;
        if(selected==28)BeginSide(27,SidePending);
    }
    void BeginSide(int clip,SideDestination pending){
        var joints=resident.GetComponentsInChildren<Transform>();
        var positions=joints.Select(j=>j.localPosition).ToArray();
        var rotations=joints.Select(j=>j.localRotation).ToArray();
        double phase=pendingDirectSide?pendingSidePhase:Idle!=null?Idle.Time/4:restBreathPhase;
        var world=resident.transform.position;var oldPivot=pivot;
        float oldYaw=yaw,oldPitch=pitch,oldDistance=distance;
        SelectCoreForLying(clip);resident.transform.position=world;pivot=oldPivot;
        if(IsDirectSideEntry(clip)){yaw=oldYaw;pitch=oldPitch;distance=oldDistance;restBreathPhase=phase;restSleepWeight=restSleepVelocity=0;liveRestBreath=true;}
        SidePending=pending;sideFollow=true;paused=false;Sample(0);ApplyCamera();
        sideEntryJoints=joints;
        sideEntryOffset=joints.Select((j,i)=>positions[i]-j.localPosition).ToArray();
        sideEntryRotation=joints.Select((j,i)=>Quaternion.Inverse(j.localRotation)*rotations[i]).ToArray();
        ApplySideEntry(0);
    }
    void ApplySideEntry(double time){
        if(sideEntryJoints==null||time>=.25)return;
        float weight=1-SideEase(Mathf.Clamp01((float)time/.25f));
        for(int i=0;i<sideEntryJoints.Length;i++){
            sideEntryJoints[i].localPosition+=sideEntryOffset[i]*weight;
            sideEntryJoints[i].localRotation*=Quaternion.Slerp(Quaternion.identity,sideEntryRotation[i],weight);
        }
    }
    bool HandleSideMovement(bool move,bool run){
        if(!IsSideClip(selected))return false;
        if(move){sideFollow=true;paused=false;}
        SidePending=!move?SideDestination.None:run?SideDestination.Run:SideDestination.Walk;
        if(selected==28&&move)BeginSideDeparture(SidePending);
        return true;
    }
    void SideReturnTo(bool seated){
        sideFollow=true;paused=false;
        SidePending=seated?SideDestination.Seated:SideDestination.Standing;
        if(selected==28)BeginSideDeparture(SidePending);
    }
    void BeginSideDeparture(SideDestination pending){
        if(pending==SideDestination.None){BeginSide(28,pending);return;}
        BeginSide(pending==SideDestination.Supine?27:pending==SideDestination.Seated?32:33,pending);
    }
    void AdvanceSide(){
        Sample(elapsed);
        if(!sideFollow||paused||selected==28||elapsed+1e-5<clips[selected].length)return;
        var pending=SidePending;
        if(selected==26||IsDirectSideEntry(selected)){BeginSideDeparture(pending);return;}
        if(IsDirectSideReturn(selected)){FinishDirectSideReturn(selected==32,pending);return;}
        StartSleepClip(22,WakeDestination.Awake);
        if(pending==SideDestination.Seated||pending==SideDestination.Standing)RequestLyingReturn(pending==SideDestination.Seated);
        else if(pending==SideDestination.Walk||pending==SideDestination.Run)RequestRun(pending==SideDestination.Run);
    }
    void SampleSideBreath(double time){
        if(IsDirectSideReturn(selected)||IsDirectSideEntry(selected)){SampleDirectSideBreath(time);return;}
        if(selected==28){
            if(liveRestBreath)SampleBreathReferences(restBreathPhase,restSleepWeight,28,30);
            else restBreathPhase=time/4;
            ApplySideEntry(time);
            return;
        }
        if(sideJoints==null){
            sideJoints=resident.GetComponentsInChildren<Transform>();int n=sideJoints.Length;
            sideBaseP=new Vector3[n];sideNeutralP=new Vector3[n];sideDeltaP=new Vector3[n];
            sideBaseQ=new Quaternion[n];sideNeutralQ=new Quaternion[n];sideDeltaQ=new Quaternion[n];
        }
        // Keep the authored roll. Add only the phase-dependent local offsets
        // between each posture's neutral and breathing reference poses.
        for(int i=0;i<sideJoints.Length;i++){sideBaseP[i]=sideJoints[i].localPosition;sideBaseQ[i]=sideJoints[i].localRotation;}
        double phase=liveRestBreath?restBreathPhase:time/4;
        float p=Mathf.Clamp01((float)time/2.4f);p=SideEase(SideEase(p));if(selected==27)p=1-p;
        for(int reference=0;reference<2;reference++){
            int clip=reference==0?22:28;
            clips[clip].SampleAnimation(resident,0);
            for(int i=0;i<sideJoints.Length;i++){sideNeutralP[i]=sideJoints[i].localPosition;sideNeutralQ[i]=sideJoints[i].localRotation;}
            clips[clip].SampleAnimation(resident,(float)(phase%1)*4);
            for(int i=0;i<sideJoints.Length;i++){
                var dp=sideJoints[i].localPosition-sideNeutralP[i];var dq=Quaternion.Inverse(sideNeutralQ[i])*sideJoints[i].localRotation;
                if(reference==0){sideDeltaP[i]=dp;sideDeltaQ[i]=dq;}
                else {sideDeltaP[i]=Vector3.Lerp(sideDeltaP[i],dp,p);sideDeltaQ[i]=Quaternion.Slerp(sideDeltaQ[i],dq,p);}
            }
        }
        for(int i=0;i<sideJoints.Length;i++){
            var joint=sideJoints[i];var pos=sideBaseP[i]+sideDeltaP[i];
            // Spine is the breathing pivot, not a fixed-length limb segment.
            // Normalizing its offset would shorten the breath and snap on return.
            if(joint!=resident.transform&&joint.name!="Spine"&&sideBaseP[i].magnitude>.0001f)pos=pos.normalized*sideBaseP[i].magnitude;
            joint.localPosition=pos;joint.localRotation=sideBaseQ[i]*sideDeltaQ[i];
        }
        // Reconcile the authored hold and the live supine reference near the
        // shared endpoint, before changing clips. Keep the same breath phase.
        float endpoint=selected==26?1-SideEase(Mathf.Clamp01((float)time/.25f)):
            SideEase(Mathf.Clamp01(((float)time-2.15f)/.25f));
        if(endpoint>0){
            for(int i=0;i<sideJoints.Length;i++){sideBaseP[i]=sideJoints[i].localPosition;sideBaseQ[i]=sideJoints[i].localRotation;}
            SampleSupineBreathReference(phase,restSleepWeight);
            for(int i=0;i<sideJoints.Length;i++){
                var joint=sideJoints[i];var target=joint.localPosition;
                var pos=Vector3.Lerp(sideBaseP[i],target,endpoint);
                if(joint!=resident.transform&&joint.name!="Spine"&&target.magnitude>.0001f)pos=pos.normalized*Mathf.Lerp(sideBaseP[i].magnitude,target.magnitude,endpoint);
                joint.localPosition=pos;joint.localRotation=Quaternion.Slerp(sideBaseQ[i],joint.localRotation,endpoint);
            }
        }
        ApplySideEntry(time);
    }
    static float SideEase(float p)=>p*p*p*(10+p*(-15+6*p));
    void SidePanel(){
        if(clips.Length<29)return;
        PanelText("왼쪽으로 눕기",true);
        if(clips.Length>=36){
            PanelAction("서기 → 왼쪽 눕기",RequestDirectSideLie,!LyingTransition&&!PendingLie&&Posture==RestPosture.Standing);
            PanelAction("앉기 → 왼쪽 눕기",RequestDirectSideLie,!LyingTransition&&!PendingLie&&Posture==RestPosture.Seated);
        }
        PanelAction("왼쪽으로 돌아눕기",RequestLeftSide,Posture==RestPosture.Supine&&!LyingTransition&&!IsSideClip(selected));
        PanelAction("바로 눕기로 복귀",RequestSupineReturn,selected==26||selected==28||IsSideSleep(selected));
        if(IsSideClip(selected))PanelText(SideStatus+" · 예약 "+SidePending);
    }
}}
