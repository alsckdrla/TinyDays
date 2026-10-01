using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyDays.Review {
public sealed partial class AdultRabbitMotionReview {
    public enum RestPosture { Standing, Seated, Supine, LeftSide }
    public static bool IsLyingClip(int i)=>i>=17&&i<=35;
    public static bool IsLyingTransition(int i)=>(i>=17&&i<=20)||i==26||i==27||IsDirectSideReturn(i)||IsDirectSideEntry(i);
    public bool LyingTransition=>IsLyingTransition(selected)&&SleepTime(elapsed)+1e-5<clips[selected].length;
    public bool LyingReview {get;private set;}
    public bool PendingLie {get;private set;}
    public CommonIdleDirector.Departure LyingDeparture {get;private set;}
    readonly Dictionary<Renderer,bool> backpackVisibility=new Dictionary<Renderer,bool>();
    bool followLying;
    bool pendingDirectSide;
    double pendingSidePhase;
    public RestPosture Posture => Idle!=null?(Idle.Seated?RestPosture.Seated:RestPosture.Standing):
        IsSideClip(selected)||IsSideSleep(selected)?RestPosture.LeftSide:
        selected==21||selected==17||selected==19||IsSleepClip(selected)?RestPosture.Supine:
        selected==20||selected==7||selected==9||IsSitDown(selected)?RestPosture.Seated:RestPosture.Standing;
    public string LyingStatus=>IsSideClip(selected)?SideStatus:IsSleepClip(selected)?SleepStatus:PendingLie?"정리 후 눕기 대기":LyingTransition?
        new[]{"서기 → 눕는 중","누움 → 일어서는 중","앉기 → 눕는 중","누움 → 앉는 중"}[selected-17]:Posture==RestPosture.Supine?"누움":"눕기 검토 · "+Posture;
    public static readonly string[] LyingPoseLabels={"시작","준비","손 지지","체중 이동","팔꿈치 지지","몸 정리","안정","끝 자세"};
    public static readonly float[] LyingPosePhases={0,.14f,.28f,.43f,.58f,.73f,.88f,1};
    void HideBackpack(){
        if(!resident)return;
        foreach(var r in resident.GetComponentsInChildren<Renderer>(true))if(r.name=="Backpack"){
            if(!backpackVisibility.ContainsKey(r))backpackVisibility.Add(r,r.enabled);
            r.enabled=false;
        }
        LyingReview=true;
    }
    void RestoreBackpack(){foreach(var p in backpackVisibility)if(p.Key)p.Key.enabled=p.Value;backpackVisibility.Clear();LyingReview=false;}
    void ResetLying(){ResetSleep();ResetSide();PendingLie=false;pendingDirectSide=false;pendingSidePhase=0;followLying=false;LyingDeparture=CommonIdleDirector.Departure.None;RestoreBackpack();}
    void OnDestroy(){ResetLying();}
    public void ExitLyingReview(){ResetLying();StartBreathing(false);}
    public void RequestDirectSideLie(){
        if(clips.Length<36||LyingTransition||PendingLie||Posture==RestPosture.Supine||Posture==RestPosture.LeftSide)return;
        pendingSidePhase=Idle!=null?Idle.Time/4:IsBreathing(selected)?elapsed/4:restBreathPhase;
        pendingDirectSide=true;RequestLie();
    }
    void BeginPendingLie(bool seated){
        if(!pendingDirectSide){BeginLyingTransition(seated?19:17);return;}
        BeginSide(seated?35:34,SideDestination.None);
    }
    public void RequestLie(){
        if(clips.Length<22||LyingTransition||PendingLie||Posture==RestPosture.Supine||Posture==RestPosture.LeftSide)return;
        HideBackpack();LyingDeparture=CommonIdleDirector.Departure.None;
        if(MovingReview){PendingLie=true;WantsToWalk=false;footTransition.Request(false);return;}
        if(Idle!=null){PendingLie=true;Idle.Request(CommonIdleDirector.Departure.None);return;}
        if(SitTransition){PendingLie=true;return;}
        BeginPendingLie(Posture==RestPosture.Seated);
    }
    public void RequestLyingReturn(bool seated){
        if(IsSideSleep(selected)){WakeFor(seated?WakeDestination.Seated:WakeDestination.Standing);return;}
        if(IsSideClip(selected)){SideReturnTo(seated);return;}
        if(clips.Length<22||LyingTransition||Posture!=RestPosture.Supine)return;
        if(IsSleepClip(selected)){WakeFor(seated?WakeDestination.Seated:WakeDestination.Standing);return;}
        BeginLyingTransition(seated?20:18);
    }
    void BeginLyingTransition(int clip){
        bool fromSleep=IsSleepClip(selected);
        var fromJoints=fromSleep?resident.GetComponentsInChildren<Transform>():null;
        Vector3[] fromPositions=null,fromVelocity=sleepFromVelocity,fromAngular=sleepAngularVelocity;Quaternion[] fromRotations=null;
        if(fromSleep){fromPositions=Array.ConvertAll(fromJoints,j=>j.localPosition);fromRotations=Array.ConvertAll(fromJoints,j=>j.localRotation);}
        var position=resident.transform.position;var oldPivot=pivot;
        var request=LyingDeparture;
        // Internal transitions retain the original renderer-state snapshot.
        followLying=false;SelectCoreForLying(clip);
        resident.transform.position=position;pivot=oldPivot;ApplyCamera();
        PendingLie=false;LyingDeparture=request;followLying=true;paused=false;HideBackpack();
        if(fromSleep){sleepBlend=true;sleepJoints=fromJoints;sleepFromPosition=fromPositions;sleepFromRotation=fromRotations;sleepEntryVelocity=fromVelocity??new Vector3[fromJoints.Length];sleepEntryAngularVelocity=fromAngular??new Vector3[fromJoints.Length];sleepFromEye=0;ApplySleepFrame(0);}
    }
    void SelectCoreForLying(int clip){
        var snapshot=new Dictionary<Renderer,bool>(backpackVisibility);
        double phase=restBreathPhase;float weight=restSleepWeight,velocity=restSleepVelocity;
        bool retain=IsSleepClip(selected)||selected==21||IsSideClip(selected);
        Select(clip);
        if(retain){restBreathPhase=phase;restSleepWeight=weight;restSleepVelocity=velocity;liveRestBreath=true;if(IsSleepClip(clip)||clip==21||IsSideClip(clip))Sample(0);}
        foreach(var p in snapshot)backpackVisibility[p.Key]=p.Value;
        HideBackpack();
    }
    bool HandleLyingMovement(bool move,bool run){
        if(HandleSideMovement(move,run))return true;
        if(HandleSleepMovement(move,run))return true;
        if(!LyingReview&&!IsLyingClip(selected)&&!PendingLie)return false;
        if(MovingReview){
            PendingLie=false;pendingDirectSide=false;LyingDeparture=CommonIdleDirector.Departure.None;RestoreBackpack();
            WantsToWalk=move;if(move)footTransition.RequestRun(run);else footTransition.Request(false);
            return true;
        }
        if(Idle!=null&&!PendingLie&&!IsLyingClip(selected)){
            if(move)Idle.Request(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk);else Idle.Cancel();
            return true;
        }
        LyingDeparture=move?(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk):CommonIdleDirector.Departure.None;
        if(!move){PendingLie=false;pendingDirectSide=false;return true;}
        if(PendingLie){PendingLie=false;pendingDirectSide=false;}
        if(LyingTransition)return true;
        if(Posture==RestPosture.Supine){BeginLyingTransition(18);return true;}
        ResumeAfterLying();return true;
    }
    void ResumeAfterLying(){
        var request=LyingDeparture;bool seated=Posture==RestPosture.Seated;
        var saved=new Dictionary<Renderer,bool>(backpackVisibility);
        StartBreathing(seated);foreach(var p in saved)backpackVisibility[p.Key]=p.Value;HideBackpack();
        LyingDeparture=CommonIdleDirector.Departure.None;
        if(request!=CommonIdleDirector.Departure.None)Idle.Request(request);
    }
    bool AdvanceLying(){
        if(PendingLie){
            if(Idle!=null&&Idle.State==CommonIdleDirector.Stage.Breathing){BeginPendingLie(Idle.Seated);return true;}
            if(MovingReview&&footTransition.State==AdultRabbitFootTransition.Stage.Idle){BeginPendingLie(false);return true;}
            if(Idle==null&&!MovingReview&&!SitTransition){BeginPendingLie(Posture==RestPosture.Seated);return true;}
        }
        if(!followLying||LyingTransition)return false;
        if(selected==17||selected==19){
            if(LyingDeparture!=CommonIdleDirector.Departure.None)BeginLyingTransition(18);
            else {if(clips.Length>=26)StartSleepClip(22,WakeDestination.Awake);else SelectCoreForLying(21);followLying=false;}
            return true;
        }
        if(selected==18||selected==20){followLying=false;ResumeAfterLying();return true;}
        return false;
    }
    void LyingPanel(){
        if(clips.Length<22)return;
        PanelText("눕기 검토 · 배낭 숨김",true);
        PanelAction("서기 → 눕기",RequestLie,!LyingTransition&&!PendingLie&&Posture==RestPosture.Standing);
        PanelAction("앉기 → 눕기",RequestLie,!LyingTransition&&!PendingLie&&Posture==RestPosture.Seated);
        bool side=IsSideClip(selected)||IsSideSleep(selected);
        PanelAction(side?"옆누움 → 직접 서기":"누움 → 서기",()=>RequestLyingReturn(false),side||!LyingTransition&&Posture==RestPosture.Supine);
        PanelAction(side?"옆누움 → 직접 앉기":"누움 → 앉기",()=>RequestLyingReturn(true),side||!LyingTransition&&Posture==RestPosture.Supine);
        PanelAction("눕기 검토 종료",ExitLyingReview,LyingReview);
        if(LyingReview)PanelText(LyingStatus+"\n이동 예약 "+LyingDeparture);
    }
}}
