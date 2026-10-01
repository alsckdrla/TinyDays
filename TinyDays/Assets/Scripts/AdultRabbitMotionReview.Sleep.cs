using System;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review {
public sealed partial class AdultRabbitMotionReview {
    public static bool IsSideSleep(int clip)=>clip>=29&&clip<=31;
    public static bool IsSleepClip(int clip)=>(clip>=22&&clip<=25)||IsSideSleep(clip);
    public static bool IsSleepLoop(int clip)=>clip==22||clip==24||clip==28||clip==30;
    static bool FallingAsleep(int clip)=>clip==23||clip==29;
    static bool Asleep(int clip)=>clip==24||clip==30;
    static bool Waking(int clip)=>clip==25||clip==31;
    public enum WakeDestination { Awake, Seated, Standing, Walk, Run, LeftSide, Supine }
    public WakeDestination SleepPending {get;private set;}
    public TextAsset sleepTiming;
    [Serializable] public class SleepCurve {public int clip;public float duration;public bool loop;public float[] eyes;}
    [Serializable] public class SleepCurves {public int sampleRate;public SleepCurve[] clips;public RestBreathingProfile breathing;}
    SleepCurves sleepCurves;
    SkinnedMeshRenderer eyeSkin;int eyeShape=-1;
    bool sleepFollow,sleepBlend;
    [Serializable] public class RestBreathingProfile {
        // Posture-specific reference poses encode chest axis and support points.
        // A later side-rest profile can use the same clock with different refs.
        public int awakeReference=22,sleepReference=24;
        public float awakePeriod=4,sleepPeriod=6,blendSeconds=.8f;
    }
    public RestBreathingProfile restBreathing=new RestBreathingProfile();
    // Phase is independent of pose/eye clip time. Reference actions contain the
    // same authored curve as Blender; their body pose REPLACES, never adds to,
    // the baked sleep breathing component.
    double restBreathPhase;
    float restSleepWeight,restSleepVelocity;
    bool liveRestBreath;
    Transform[] restBreathJoints;
    Vector3[] restAwakePositions;
    Quaternion[] restAwakeRotations;
    public double RestBreathPhase=>restBreathPhase;
    public float RestBreathSleepWeight=>restSleepWeight;
    void AdvanceRestBreath(float dt){
        if(pendingDirectSide&&!IsLyingClip(selected)){pendingSidePhase+=dt/4;return;}
        if(!IsSleepClip(selected)&&selected!=21&&!IsSideClip(selected))return;
        liveRestBreath=true;
        if(dt<=0)return;
        // Substeps keep frequency integration stable at different render rates.
        int steps=Math.Max(1,Mathf.CeilToInt(dt*240));float h=dt/steps;
        for(int i=0;i<steps;i++){
            restSleepWeight=Mathf.SmoothDamp(restSleepWeight,FallingAsleep(selected)||Asleep(selected)?1:0,ref restSleepVelocity,restBreathing.blendSeconds,Mathf.Infinity,h);
            restBreathPhase+=h/Mathf.Lerp(restBreathing.awakePeriod,restBreathing.sleepPeriod,restSleepWeight);
        }
    }
    void SampleRestBreath(double previewTime){
        if(IsSideClip(selected)){SampleSideBreath(previewTime);return;}
        if(!IsSleepClip(selected)&&selected!=21)return;
        if(clips.Length<26)return;
        if(!liveRestBreath&&(FallingAsleep(selected)||Waking(selected))){
            // Explicit pose scrubbing uses the source preview (zero initial
            // phase). Normal playback never reinitializes its clock here.
            float t=Mathf.Clamp((float)previewTime,0,clips[selected].length),k=2/restBreathing.blendSeconds;
            float decay=(1+k*t)*Mathf.Exp(-k*t);
            restSleepWeight=FallingAsleep(selected)?1-decay:decay;
            restSleepVelocity=(FallingAsleep(selected)?1:-1)*k*k*t*Mathf.Exp(-k*t);
            restBreathPhase=0;
            for(int i=1;i<=Mathf.CeilToInt(t*120);i++){
                float u=Mathf.Min(t,i/120f),h=u-(i-1)/120f;
                float d=(1+k*u)*Mathf.Exp(-k*u),w=FallingAsleep(selected)?1-d:d;
                restBreathPhase+=h/Mathf.Lerp(restBreathing.awakePeriod,restBreathing.sleepPeriod,w);
            }
            return;
        }
        double phase=liveRestBreath?restBreathPhase:previewTime/(Asleep(selected)?restBreathing.sleepPeriod:restBreathing.awakePeriod);
        float weight=liveRestBreath?restSleepWeight:(Asleep(selected)?1:0);
        if(!liveRestBreath){restBreathPhase=phase;restSleepWeight=weight;}
        if(IsSideSleep(selected))SampleBreathReferences(phase,weight,28,30);else SampleSupineBreathReference(phase,weight);
    }
    void SampleSupineBreathReference(double phase,float weight){
        SampleBreathReferences(phase,weight,restBreathing.awakeReference,restBreathing.sleepReference);
    }
    void SampleBreathReferences(double phase,float weight,int awake,int sleeping){
        if(restBreathJoints==null){restBreathJoints=resident.GetComponentsInChildren<Transform>();restAwakePositions=new Vector3[restBreathJoints.Length];restAwakeRotations=new Quaternion[restBreathJoints.Length];}
        float normalized=(float)(phase-Math.Floor(phase));
        clips[awake].SampleAnimation(resident,normalized*clips[awake].length);
        for(int i=0;i<restBreathJoints.Length;i++){restAwakePositions[i]=restBreathJoints[i].localPosition;restAwakeRotations[i]=restBreathJoints[i].localRotation;}
        clips[sleeping].SampleAnimation(resident,normalized*clips[sleeping].length);
        for(int i=0;i<restBreathJoints.Length;i++){
            var joint=restBreathJoints[i];var target=joint.localPosition;
            var position=Vector3.Lerp(restAwakePositions[i],target,weight);
            if(joint!=resident.transform&&target.magnitude>.0001f&&Mathf.Abs(target.magnitude-restAwakePositions[i].magnitude)<.0001f)position=position.normalized*target.magnitude;
            joint.localPosition=position;joint.localRotation=Quaternion.Slerp(restAwakeRotations[i],joint.localRotation,weight);
        }
    }
    float wakeEyeStart=1;
    Transform[] sleepJoints;Vector3[] sleepFromPosition,sleepFromVelocity,sleepEntryVelocity,sleepLastPosition;
    Quaternion[] sleepFromRotation,sleepLastRotation;
    Vector3[] sleepAngularVelocity,sleepEntryAngularVelocity;
    double sleepLastSample=-1;
    public float SleepEyeWeight {get;private set;}
    public string SleepStatus=>(IsSideSleep(selected)?"왼쪽 누움 · ":"")+new[]{"누운 기본 호흡","잠드는 중","수면 호흡","깨어나는 중"}[IsSideSleep(selected)?selected-28:Mathf.Clamp(selected-22,0,3)];
    public static readonly string[] SleepPoseLabels={"휴식","들이쉬기","가슴 상승","정점","내쉬기","내려오기","안정","끝 / 반복"};
    public static readonly float[] SleepPosePhases={0,.12f,.25f,.4f,.55f,.7f,.86f,1};
    void InitEyes(){
        if(!eyeSkin&&resident){
            eyeSkin=resident.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(s=>s.sharedMesh&&Enumerable.Range(0,s.sharedMesh.blendShapeCount).Any(i=>s.sharedMesh.GetBlendShapeName(i).EndsWith("SleepEyesClosed",StringComparison.Ordinal)));
            if(eyeSkin)eyeShape=Enumerable.Range(0,eyeSkin.sharedMesh.blendShapeCount).First(i=>eyeSkin.sharedMesh.GetBlendShapeName(i).EndsWith("SleepEyesClosed",StringComparison.Ordinal));
        }
        if(sleepCurves==null&&sleepTiming){sleepCurves=JsonUtility.FromJson<SleepCurves>(sleepTiming.text);if(sleepCurves.breathing!=null)restBreathing=sleepCurves.breathing;}
    }
    void Eyes(float value){InitEyes();SleepEyeWeight=Mathf.Clamp01(value);if(eyeSkin&&eyeShape>=0)eyeSkin.SetBlendShapeWeight(eyeShape,100*SleepEyeWeight);}
    void ResetSleep(){Eyes(0);sleepFollow=sleepBlend=false;wakeEyeStart=1;SleepPending=WakeDestination.Awake;sleepJoints=null;sleepLastSample=-1;sleepLastPosition=null;sleepLastRotation=null;sleepFromVelocity=sleepEntryVelocity=null;sleepAngularVelocity=sleepEntryAngularVelocity=null;restBreathPhase=0;restSleepWeight=restSleepVelocity=0;liveRestBreath=false;restBreathJoints=null;}
    double SleepTime(double time)=>Math.Max(0,time-(sleepBlend?.25:0));
    void SampleSleep(double time){
        InitEyes();float eye=0;
        if(IsSleepClip(selected)&&sleepCurves!=null){
            var curve=sleepCurves.clips.First(c=>c.clip==selected);
            float t=(float)(IsSleepLoop(selected)?time%curve.duration:Math.Min(time,curve.duration));
            float f=t*sleepCurves.sampleRate;int a=Mathf.Clamp((int)f,0,curve.eyes.Length-1);
            eye=Mathf.Lerp(curve.eyes[a],curve.eyes[Math.Min(a+1,curve.eyes.Length-1)],f-a);
            if(Waking(selected))eye*=wakeEyeStart;
        }
        Eyes(eye);
    }
    public void RequestSleep(){
        if(selected==28&&clips.Length>=32){StartSleepClip(29,WakeDestination.Awake);return;}
        if(clips.Length<26||Posture!=RestPosture.Supine||LyingTransition||selected==23||selected==24||selected==25)return;
        StartSleepClip(23,WakeDestination.Awake);
    }
    public void RequestWake(){WakeFor(WakeDestination.Awake);}
    public void RequestSupineBreathing(){
        if(Posture!=RestPosture.Supine||LyingTransition)return;
        if(selected==23||selected==24||selected==25)WakeFor(WakeDestination.Awake);
        else StartSleepClip(22,WakeDestination.Awake);
    }
    void WakeFor(WakeDestination destination){
        if(!IsSleepClip(selected))return;
        SleepPending=destination;
        if(Waking(selected)){sleepFollow=true;return;}
        if(selected==22){FinishWake();return;}
        StartSleepClip(IsSideSleep(selected)?31:25,destination);
    }
    void StartSleepClip(int clip,WakeDestination destination){
        bool fromSide=IsSideClip(selected);
        var joints=resident.GetComponentsInChildren<Transform>();
        var positions=joints.Select(j=>j.localPosition).ToArray();var rotations=joints.Select(j=>j.localRotation).ToArray();
        var velocity=sleepFromVelocity!=null&&sleepFromVelocity.Length==joints.Length?(Vector3[])sleepFromVelocity.Clone():new Vector3[joints.Length];
        var angular=sleepAngularVelocity!=null?(Vector3[])sleepAngularVelocity.Clone():new Vector3[joints.Length];
        float fromEye=SleepEyeWeight;var position=resident.transform.position;var oldPivot=pivot;
        SelectCoreForLying(clip);resident.transform.position=position;pivot=oldPivot;ApplyCamera();
        sleepJoints=joints;sleepFromPosition=positions;sleepFromRotation=rotations;sleepFromVelocity=velocity;sleepEntryVelocity=velocity;
        sleepEntryAngularVelocity=angular;
        sleepFollow=true;sleepBlend=fromSide;SleepPending=destination;paused=false;
        wakeEyeStart=Waking(clip)?fromEye:1;sleepFromEye=fromEye;
        ApplySleepFrame(0);
    }
    float sleepFromEye;
    void ApplySleepFrame(double time){
        double t=SleepTime(time);Sample(t);
        if(sleepBlend&&time<.25&&sleepJoints!=null){
            float u=Mathf.Clamp01((float)time/.25f);float smooth=u*u*u*(10+u*(-15+6*u));
            for(int i=0;i<sleepJoints.Length;i++){
                var j=sleepJoints[i];Vector3 target=j.localPosition;
                // Decaying source velocity avoids freezing a moving breath at entry.
                var p=Vector3.Lerp(sleepFromPosition[i],target,smooth)+sleepEntryVelocity[i]*(float)time*(1-smooth);
                if(j!=resident.transform&&target.magnitude>.0001f&&Mathf.Abs(target.magnitude-sleepFromPosition[i].magnitude)<.0001f)p=p.normalized*target.magnitude;
                j.localPosition=p;
                var angular=sleepEntryAngularVelocity==null?Vector3.zero:sleepEntryAngularVelocity[i];
                var following=Quaternion.AngleAxis(angular.magnitude*(float)time,angular.sqrMagnitude>1e-10f?angular.normalized:Vector3.up)*sleepFromRotation[i];
                j.localRotation=Quaternion.Slerp(following,j.localRotation,smooth);
            }
            Eyes(Mathf.Lerp(sleepFromEye,SleepEyeWeight,smooth));
        }
    }
    void AdvanceSleep(){
        ApplySleepFrame(elapsed);
        if(sleepJoints==null)sleepJoints=resident.GetComponentsInChildren<Transform>();
        var now=sleepJoints.Select(j=>j.localPosition).ToArray();
        var rotations=sleepJoints.Select(j=>j.localRotation).ToArray();
        if(sleepLastSample>=0&&elapsed>sleepLastSample&&sleepLastPosition!=null){
            sleepFromVelocity=now.Select((p,i)=>(p-sleepLastPosition[i])/(float)(elapsed-sleepLastSample)).ToArray();
            sleepAngularVelocity=new Vector3[now.Length];
            if(sleepLastRotation!=null)for(int i=0;i<now.Length;i++){
                var delta=rotations[i]*Quaternion.Inverse(sleepLastRotation[i]);delta.ToAngleAxis(out float angle,out Vector3 axis);
                if(angle>180)angle-=360;if(axis.sqrMagnitude>.0001f&&Mathf.Abs(angle)>.00001f)sleepAngularVelocity[i]=axis*angle/(float)(elapsed-sleepLastSample);
            }
        }
        sleepLastPosition=now;sleepLastRotation=rotations;sleepLastSample=elapsed;
        if(!sleepFollow||paused||IsSleepLoop(selected)||SleepTime(elapsed)+1e-5<clips[selected].length)return;
        if(FallingAsleep(selected))StartSleepClip(IsSideSleep(selected)?30:24,WakeDestination.Awake);
        else if(Waking(selected))FinishWake();
    }
    void FinishWake(){
        var destination=SleepPending;
        if(IsSideSleep(selected)){
            if(destination==WakeDestination.Awake){BeginSide(28,SideDestination.None);return;}
            var pending=destination==WakeDestination.Seated?SideDestination.Seated:destination==WakeDestination.Standing?SideDestination.Standing:destination==WakeDestination.Walk?SideDestination.Walk:destination==WakeDestination.Run?SideDestination.Run:SideDestination.Supine;
            BeginSideDeparture(pending);return;
        }
        if(destination==WakeDestination.LeftSide){BeginSide(26,SideDestination.None);return;}
        if(destination==WakeDestination.Awake){StartSleepClip(22,WakeDestination.Awake);return;}
        LyingDeparture=destination==WakeDestination.Run?CommonIdleDirector.Departure.Run:destination==WakeDestination.Walk?CommonIdleDirector.Departure.Walk:CommonIdleDirector.Departure.None;
        BeginLyingTransition(destination==WakeDestination.Seated?20:18);
    }
    bool HandleSleepMovement(bool move,bool run){
        if(!IsSleepClip(selected))return false;
        if(!move){SleepPending=WakeDestination.Awake;LyingDeparture=CommonIdleDirector.Departure.None;return true;}
        WakeFor(run?WakeDestination.Run:WakeDestination.Walk);return true;
    }
    void SleepPanel(){
        if(clips.Length<26)return;
        PanelText("누운 호흡 · 잠들기",true);
        bool ready=(Posture==RestPosture.Supine||selected==28)&&!LyingTransition;
        PanelText("기본 호흡 자동 적용 · 깨어 있음 4초 / 수면 6초");
        PanelAction("잠들기",RequestSleep,ready&&!FallingAsleep(selected)&&!Asleep(selected)&&!Waking(selected));
        PanelAction("깨어나기",RequestWake,FallingAsleep(selected)||Asleep(selected)||Waking(selected));
        if(IsSleepClip(selected))PanelText(SleepStatus+" · 예약 "+SleepPending+"\n호흡 위상 연속 · 클립 "+clips[selected].length.ToString("F1")+"초");
    }
}}
