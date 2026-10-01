using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TinyDays.Review {
    // One-owner, flat-ground authored task; not autonomous gardening AI.
    public sealed class RabbitWaterReview : MonoBehaviour {
        public enum TaskState { Ready, Walking, Stopping, Pouring, Returning }
        public RabbitHomeLifeReview home;
        public AnimationClip carryIdle,carryWalk,pour;
        public Transform can;
        public Transform[] grips;
        public Transform spout;
        public ParticleSystem water;
        public Vector3 start=new Vector3(-.4f,.275f,-3.7f),destination=new Vector3(1.3f,.275f,-3.7f);
        public Vector3 canOffset=new Vector3(0,-.10f,.34f);
        public string bodyFamily="AdultStandard_v2";
        public TaskState State {get;private set;}
        public float TimeInState {get;private set;}
        public float MaxGripError {get;private set;}
        public float MinSole {get;private set;}
        public float MaxDrift {get;private set;}
        public float MaxSupportGap {get;private set;}
        public float MaxReach {get;private set;}
        public float PourAmount {get;private set;}
        public float DistanceTravelled {get;private set;}
        public int DropletsLanded {get;private set;}
        public int DropletsOutsideBed {get;private set;}
        public bool Active {get;private set;}
        public string Label=>State==TaskState.Ready?"운반 대기":State==TaskState.Walking?"화단으로 걷기":State==TaskState.Stopping?"감속 · 발 모으기":State==TaskState.Pouring?"물 주는 중":"물뿌리개 세우기";
        PlayableGraph graph;AnimationMixerPlayable mix;
        AnimationClipPlayable idlePlay,walkPlay,pourPlay;
        Transform[] bones;Transform spine,pelvis;
        Vector3[] restPositions,returnPositions,enterPositions;
        Quaternion[] restRotations,returnRotations,enterRotations;
        IdleSupportLeg[] arms;
        Quaternion[] handRotations;
        AdultRabbitFootTransition feet;RabbitHomeFootwork support;
        double clock;float remainder,returnAmount,recoveryTime;
        Vector3 recoveryOffset;
        bool followPour,stopRequested;
        readonly Vector3[] anchors=new Vector3[2];readonly bool[] wasPlanted=new bool[2];
        readonly ParticleSystem.Particle[] drops=new ParticleSystem.Particle[96];
        int dropCount,dropSerial;float emissionClock;
        static float Ease(float x){x=Mathf.Clamp01(x);return x*x*x*(x*(x*6-15)+10);}
        public static float Amount(float t)=>Ease(t/1.2f)*(1-Ease((t-4.4f)/1.6f));
        public void Shutdown(){Active=false;if(graph.IsValid())graph.Destroy();if(can)can.gameObject.SetActive(false);if(water)water.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        void OnDisable(){Shutdown();}
        System.Collections.IEnumerator Start(){
            if(!Environment.GetCommandLineArgs().Contains("-waterSmoke"))yield break;
            home.SelectWaterMode(true);StartWalk(true);bool poured=false;
            for(int i=0;i<60*20;i++){home.Advance(1f/60);poured|=State==TaskState.Pouring;if(i%120==0)yield return null;}
            bool pass=poured&&State==TaskState.Ready&&MaxGripError<=.01f&&MinSole>=-.0005f&&MaxDrift<=.0035f&&DropletsLanded>0&&DropletsOutsideBed==0;
            Debug.Log($"WATER_PLAYER_SMOKE_{(pass?"OK":"FAILED")} grip={MaxGripError:F6} sole={MinSole:F6} drift={MaxDrift:F6} drops={DropletsLanded} outside={DropletsOutsideBed}. Automated callbacks, not OS input.");
            home.SelectWaterMode(false);Application.Quit(pass?0:1);
        }
        public void ResetTask(){
            Shutdown();home.SuspendPose();Active=true;State=TaskState.Ready;TimeInState=0;clock=remainder=0;
            followPour=stopRequested=false;recoveryTime=1;recoveryOffset=Vector3.zero;PourAmount=0;DistanceTravelled=0;
            dropCount=dropSerial=DropletsLanded=DropletsOutsideBed=0;emissionClock=0;
            var actor=home.resident;actor.transform.SetPositionAndRotation(start,Quaternion.Euler(0,90,0));
            carryIdle.SampleAnimation(actor,0);bones=actor.GetComponentsInChildren<Transform>().Where(t=>t!=actor.transform).ToArray();
            spine=bones.First(t=>t.name=="Spine");pelvis=bones.First(t=>t.name=="Pelvis");
            restPositions=bones.Select(t=>t.localPosition).ToArray();restRotations=bones.Select(t=>t.localRotation).ToArray();
            graph=PlayableGraph.Create("Water task pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            idlePlay=AnimationClipPlayable.Create(graph,carryIdle);walkPlay=AnimationClipPlayable.Create(graph,carryWalk);pourPlay=AnimationClipPlayable.Create(graph,pour);
            mix=AnimationMixerPlayable.Create(graph,3);graph.Connect(idlePlay,0,mix,0);graph.Connect(walkPlay,0,mix,1);graph.Connect(pourPlay,0,mix,2);
            var animator=actor.GetComponent<Animator>();animator.enabled=true;
            AnimationPlayableOutput.Create(graph,"Water pose",animator).SetSourcePlayable(mix);graph.Play();
            Sample(0);support=new RabbitHomeFootwork(actor,home.groundHeight);support.Hold();support.Capture();feet=new AdultRabbitFootTransition(actor,home.groundHeight){SmoothStopBalance=true};
            arms=new IdleSupportLeg[2];handRotations=new Quaternion[2];
            for(int i=0;i<2;i++){
                string s=i==0?"L":"R";arms[i]=new IdleSupportLeg{Upper=bones.First(t=>t.name=="UpperArm_"+s),Lower=bones.First(t=>t.name=="Forearm_"+s),End=bones.First(t=>t.name=="Hand_"+s)};
                handRotations[i]=Quaternion.Inverse(actor.transform.rotation)*arms[i].End.rotation;
                // FBX handedness reverses authored X; use actual shoulder side,
                // never infer anatomical side from the legacy L/R bone suffix.
                float side=Mathf.Sign(actor.transform.InverseTransformPoint(arms[i].Upper.position).x);
                var grip=grips[i].localPosition;grip.x=side*Mathf.Abs(grip.x);grips[i].localPosition=grip;
            }
            can.gameObject.SetActive(true);MaxGripError=MaxDrift=MaxReach=MaxSupportGap=0;MinSole=float.PositiveInfinity;Array.Clear(wasPlanted,0,2);
            ApplyProp(0);Measure();
        }
        void Sample(float walkWeight,float walkTime=0,bool pouring=false){
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=restPositions[i];bones[i].localRotation=restRotations[i];}
            idlePlay.SetTime(clock%4);walkPlay.SetTime(walkTime%.8f);pourPlay.SetTime(Mathf.Clamp(TimeInState,0,6));
            mix.SetInputWeight(0,pouring?0:1-walkWeight);mix.SetInputWeight(1,pouring?0:walkWeight);mix.SetInputWeight(2,pouring?1:0);graph.Evaluate(0);
            pelvis.position+=recoveryOffset*(1-Ease(recoveryTime/.15f));
        }
        public void StartWalk(bool completeTask){
            if(!Active||State!=TaskState.Ready)return;
            followPour=completeTask;
            if(Vector3.Dot(destination-home.resident.transform.position,home.resident.transform.forward)<.3f){if(completeTask)StartPour();return;}
            feet=new AdultRabbitFootTransition(home.resident,home.groundHeight){SmoothStopBalance=true};feet.Request(true);stopRequested=false;State=TaskState.Walking;TimeInState=0;
        }
        public void StartPour(){
            if(!Active||State!=TaskState.Ready)return;
            enterPositions=bones.Select(t=>t.localPosition).ToArray();enterRotations=bones.Select(t=>t.localRotation).ToArray();
            State=TaskState.Pouring;TimeInState=0;followPour=false;
        }
        public void StopTask(){
            if(!Active)return;followPour=false;
            if(State==TaskState.Walking){feet.Request(false);stopRequested=true;State=TaskState.Stopping;}
            else if(State==TaskState.Pouring)BeginReturn();
        }
        void BeginReturn(){
            returnPositions=bones.Select(t=>t.localPosition).ToArray();returnRotations=bones.Select(t=>t.localRotation).ToArray();returnAmount=PourAmount;
            State=TaskState.Returning;TimeInState=0;
        }
        public void Advance(float seconds){
            if(!Active||home.paused)return;remainder+=seconds*(home.slow?.5f:1);
            while(remainder>=1f/240){remainder-=1f/240;Tick(1f/240);}
        }
        void Tick(float dt){
            clock+=dt;TimeInState+=dt;recoveryTime+=dt;
            if(State==TaskState.Walking||State==TaskState.Stopping){
                if(!stopRequested&&Vector3.Dot(destination-home.resident.transform.position,home.resident.transform.forward)<.27f){stopRequested=true;feet.Request(false);State=TaskState.Stopping;}
                float moved=feet.Step(dt);home.resident.transform.position+=home.resident.transform.forward*moved;DistanceTravelled+=moved;
                feet.RestoreSourcePose();Sample(feet.Weight,(float)feet.WalkTime);Vector3 source=pelvis.position;feet.Apply();
                if(stopRequested&&feet.State==AdultRabbitFootTransition.Stage.Idle){
                    recoveryOffset=pelvis.position-source+Vector3.up*support.RestingPelvisDrop;recoveryTime=0;support.Capture();State=TaskState.Ready;TimeInState=0;
                    if(followPour)StartPour();
                }
            }else if(State==TaskState.Pouring){
                Sample(0,0,true);
                float blend=Ease(TimeInState/.25f);
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.Lerp(enterPositions[i],bones[i].localPosition,blend);bones[i].localRotation=Quaternion.Slerp(enterRotations[i],bones[i].localRotation,blend);}
                support.Hold();PourAmount=Amount(TimeInState);
                if(TimeInState>=6){PourAmount=0;BeginReturn();}
            }else{
                Sample(0);PourAmount=0;
                if(State==TaskState.Returning){
                    float blend=Ease(TimeInState/.5f);
                    for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.Lerp(returnPositions[i],bones[i].localPosition,blend);bones[i].localRotation=Quaternion.Slerp(returnRotations[i],bones[i].localRotation,blend);}
                    PourAmount=returnAmount*(1-blend);
                    if(TimeInState>=.5f){State=TaskState.Ready;TimeInState=0;}
                }
                support.Hold();
            }
            ApplyProp(PourAmount);Measure();
            UpdateDrops(dt);
        }
        void ApplyProp(float amount){
            var actor=home.resident.transform;
            can.SetPositionAndRotation(spine.position+actor.rotation*(canOffset+new Vector3(0,.05f*amount,0)),actor.rotation*Quaternion.Euler(25*amount,0,0));
            for(int i=0;i<2;i++){
                float length=Vector3.Distance(arms[i].Upper.position,arms[i].Lower.position)+Vector3.Distance(arms[i].Lower.position,arms[i].End.position);
                MaxReach=Mathf.Max(MaxReach,Vector3.Distance(arms[i].Upper.position,grips[i].position)-length);
                float side=Mathf.Sign(actor.InverseTransformPoint(arms[i].Upper.position).x);
                arms[i].Solve(grips[i].position,can.rotation*handRotations[i],actor.forward,actor.rotation*new Vector3(side,-.6f,.15f));
                MaxGripError=Mathf.Max(MaxGripError,Vector3.Distance(grips[i].position,arms[i].End.position));
            }
        }
        void UpdateDrops(float dt){
            if(!water)return;
            for(int i=dropCount-1;i>=0;i--){
                var p=drops[i];p.remainingLifetime-=dt;p.position+=p.velocity*dt+Vector3.down*(4.905f*dt*dt);p.velocity+=Vector3.down*(9.81f*dt);
                if(p.remainingLifetime<=0||p.position.y<home.groundHeight+.2f){
                    if(p.position.y<home.groundHeight+.2f){DropletsLanded++;if(Mathf.Abs(p.position.x-2.15f)>.24f||Mathf.Abs(p.position.z+3.7f)>.44f)DropletsOutsideBed++;}
                    drops[i]=drops[--dropCount];continue;
                }drops[i]=p;
            }
            if(State==TaskState.Pouring&&TimeInState>=1.2f&&TimeInState<=4.4f){
                emissionClock+=dt;
                while(emissionClock>=.025f){emissionClock-=.025f;
                    for(int j=0;j<3&&dropCount<drops.Length;j++){
                        float spread=(j-1)*.035f;
                        var p=new ParticleSystem.Particle{position=spout.position+home.resident.transform.right*spread,velocity=home.resident.transform.forward*(.62f+.08f*Mathf.Sin(dropSerial++))+Vector3.down*.65f,startLifetime=.7f,remainingLifetime=.7f,startSize=.018f,startColor=new Color32(140,212,255,255)};
                        drops[dropCount++]=p;
                    }
                }
            }else emissionClock=0;
            water.Pause();water.SetParticles(drops,dropCount);
        }
        void Measure(){
            for(int i=0;i<2;i++){
                bool planted=(State==TaskState.Walking||State==TaskState.Stopping)?(i==0?feet.LeftPlanted:feet.RightPlanted):true;
                float h=support.SoleHeight(i);MinSole=Mathf.Min(MinSole,h);if(planted)MaxSupportGap=Mathf.Max(MaxSupportGap,h);
                Vector3 p=support.FootPosition(i);if(planted&&wasPlanted[i])MaxDrift=Mathf.Max(MaxDrift,Vector3.Distance(p,anchors[i]));else if(planted)anchors[i]=p;
                wasPlanted[i]=planted;
            }
        }
        public void Pose(int action,int index){
            ResetTask();if(action==2)StartPour();float u=index/7f;TimeInState=action==2?6*u:action==1?.8f*u:4*u;clock=TimeInState;
            Sample(action==1?1:0,TimeInState,action==2);support.Hold();State=action==2?TaskState.Pouring:TaskState.Ready;
            PourAmount=action==2?Amount(TimeInState):0;ApplyProp(PourAmount);home.paused=true;
            if(action==1){BeginReturn();}
        }
    }
}
