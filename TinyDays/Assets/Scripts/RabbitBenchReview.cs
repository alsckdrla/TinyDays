using System;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review {
    // Authored one-resident seating task, with independent seat/contact settings.
    public sealed class RabbitBenchReview : MonoBehaviour {
        public enum TaskState { Standing, Walking, Stopping, Turning, Sitting, Resting, Rising, Departing, Backstepping, ClearingCan }
        public RabbitHomeLifeReview home;
        public AnimationClip sitClip,breatheClip,standClip;
        public Transform bench;
        public Vector3 start=new Vector3(-4.1f,.275f,-3.72f),approach=new Vector3(-2.4f,.275f,-3.72f);
        public float seatedYaw=180,restSeconds=6,departureDistance=1;
        public float seatHeight=.30f;
        public Vector3 SeatPreparation {get;private set;}
        public Bounds BenchBounds {get;private set;}
        public float AirborneSole=>support.SoleHeight(0);
        public float SoleHeight(int side)=>support.SoleHeight(side);
        public string bodyFamily="AdultStandard_v2";
        public TaskState State {get;private set;}
        public bool Active {get;private set;}
        public float TimeInState {get;private set;}
        public float MinSole {get;private set;}
        public float MaxGap {get;private set;}
        public float MaxDrift {get;private set;}
        public float MaxReach {get;private set;}
        public string ReachDetail=>support.ReachDetail;
        public int TurnSteps {get;private set;}
        public bool PendingWalk=>pendingWalk;
        public string Label=>new[]{"서서 대기","벤치로 걷기","감속 · 발 모으기","방향 정리","앉는 중","벤치 휴식","일어나는 중","걷기 재개","두 걸음 착석 준비","보관 소품에서 낮은 뒤걸음"}[(int)State];
        AdultRabbitFootTransition feet;RabbitHomeFootwork support;
        RabbitWalkingRoute walkingRoute;
        AdultRabbitSitCoat coat;
        Transform[] bones;Transform pelvis;
        Vector3[] basePositions,fromPositions;
        Quaternion[] baseRotations,fromRotations;
        readonly Vector3[] anchors=new Vector3[2];readonly bool[] plantedBefore=new bool[2];
        bool fullFlow,pendingWalk,pendingRise,pendingSit,stopRequested,turnToApproach,continuousBreathing;
        float remainder,blendTime;bool seatBlend;double clock;Vector3 destination,recoveryOffset;float recoveryTime=1;
        static float Ease(float x){x=Mathf.Clamp01(x);return x*x*x*(x*(x*6-15)+10);}
        public void Shutdown(){Active=false;pendingWalk=pendingRise=pendingSit=fullFlow=false;if(coat!=null){coat.Dispose();coat=null;}}
        void OnDisable(){Shutdown();}
        System.Collections.IEnumerator Start(){
            if(!Environment.GetCommandLineArgs().Contains("-benchSmoke"))yield break;
            home.SelectBenchMode();StartFlow();bool rested=false,captured=false;
            for(int i=0;i<60*30;i++){
                home.Advance(1f/60);rested|=State==TaskState.Resting;
                if(!captured&&State==TaskState.Resting&&TimeInState>.2f){
                    captured=true;yield return null;yield return new WaitForEndOfFrame();
                    var args=Environment.GetCommandLineArgs();int capture=Array.IndexOf(args,"-benchCapture");if(capture>=0&&capture+1<args.Length)CapturePlayerCamera(args[capture+1]);
                }
                if(i%120==0)yield return null;
            }
            bool ok=rested&&State==TaskState.Standing&&MinSole>=-.0005f&&MaxDrift<=.0035f&&MaxReach<=.001f;
            Debug.Log($"BENCH_PLAYER_SMOKE_{(ok?"OK":"FAILED")} sole={MinSole:F6} drift={MaxDrift:F6} reach={MaxReach:F6}. Automated callbacks, not OS input.");
            home.SelectWaterMode(false);Application.Quit(ok?0:1);
        }
        public void CapturePlayerCamera(string path){
            var camera=home.reviewCamera;var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(960,720,24);var texture=new Texture2D(960,720,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.aspect=4f/3;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,960,720),0,0);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(texture);}
        }
        public void ResetTask(){
            EnterCurrent(true);
        }
        // A lifecycle handoff is not a review reset: preserve actor/face clocks.
        public void EnterCurrent(bool resetPosition=false){
            var incoming=home.resident.GetComponentsInChildren<Transform>().Where(t=>t!=home.resident.transform).ToArray();
            var incomingP=incoming.Select(t=>t.localPosition).ToArray();var incomingQ=incoming.Select(t=>t.localRotation).ToArray();
            var renderers=bench.GetComponentsInChildren<Renderer>();BenchBounds=renderers[0].bounds;foreach(var r in renderers)BenchBounds.Encapsulate(r.bounds);
            seatHeight=BenchBounds.max.y-home.groundHeight;var center=BenchBounds.center;center.y=home.groundHeight;
            seatedYaw=bench.eulerAngles.y;approach=center+bench.forward*(BenchBounds.extents.z+.34f);
            SeatPreparation=center+bench.forward*(BenchBounds.extents.z+.14f);
            start=approach+bench.right*1.7f;
            Shutdown();home.SuspendPose();Active=true;home.resident.GetComponent<Animator>().enabled=false;
            if(resetPosition)home.resident.transform.SetPositionAndRotation(start,Quaternion.Euler(0,90,0));home.idleClip.SampleAnimation(home.resident,0);
            bones=home.resident.GetComponentsInChildren<Transform>().Where(t=>t!=home.resident.transform).ToArray();pelvis=bones.First(t=>t.name=="Pelvis");
            basePositions=bones.Select(t=>t.localPosition).ToArray();baseRotations=bones.Select(t=>t.localRotation).ToArray();
            support=new RabbitHomeFootwork(home.resident,home.groundHeight){FitStandingReach=true};support.Hold();support.Capture();
            feet=null;State=TaskState.Standing;TimeInState=remainder=0;continuousBreathing=!resetPosition;clock=resetPosition?0:home.AutomaticBlinkClock;recoveryOffset=Vector3.zero;recoveryTime=1;
            MaxDrift=MaxGap=MaxReach=0;MinSole=float.PositiveInfinity;TurnSteps=0;Array.Clear(plantedBefore,0,2);Measure();
            coat=new AdultRabbitSitCoat(home.resident);
            fromPositions=null;
            if(!resetPosition){
                for(int i=0;i<incoming.Length;i++){incoming[i].localPosition=incomingP[i];incoming[i].localRotation=incomingQ[i];}
                support.Capture();CaptureBlend(.25f,true);
                Array.Clear(plantedBefore,0,2);Measure();
            }
        }
        void Restore(){for(int i=0;i<bones.Length;i++){bones[i].localPosition=basePositions[i];bones[i].localRotation=baseRotations[i];}}
        void Sample(AnimationClip clip,float time){Restore();clip.SampleAnimation(home.resident,time);pelvis.position+=recoveryOffset*(1-Ease(recoveryTime/.15f));}
        void CaptureBlend(float duration,bool seatContact=false){
            fromPositions=bones.Select(t=>t.localPosition).ToArray();fromRotations=bones.Select(t=>t.localRotation).ToArray();blendTime=duration;seatBlend=seatContact;
            // Captured support poses already contain the one-time floor offset.
            // Lift it out before the next support solve, avoiding a double drop.
            if(seatContact)fromPositions[Array.IndexOf(bones,pelvis)]=pelvis.parent.InverseTransformPoint(pelvis.position+Vector3.up*support.RestingPelvisDrop);
        }
        void Blend(){if(fromPositions==null)return;float u=Ease(TimeInState/blendTime);for(int i=0;i<bones.Length;i++){
            // Seat and foot preparation share one continuous authored hip path.
            // A second hip crossfade would delay the forward shift behind landing.
            if(seatBlend&&bones[i]==pelvis)continue;
            bones[i].localPosition=Vector3.Lerp(fromPositions[i],bones[i].localPosition,u);bones[i].localRotation=Quaternion.Slerp(fromRotations[i],bones[i].localRotation,u);
        }if(u>=1)fromPositions=null;}
        void Begin(TaskState state){State=state;TimeInState=0;}
        public void StartFlow(){if(!Active||State!=TaskState.Standing)return;fullFlow=true;RequestSit(true);}
        public void StartConnectedFlow(Transform storedCan){
            if(!Active||State!=TaskState.Standing)return;
            fullFlow=true;pendingSit=true;
            var origin=home.resident.transform.position;var rotation=home.resident.transform.rotation;
            var away=origin-storedCan.position;away.y=0;away.Normalize();
            var bounds=storedCan.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.bounds).ToArray();
            float canExtent=bounds.SelectMany(box=>new[]{box.min,box.max,new Vector3(box.min.x,0,box.max.z),new Vector3(box.max.x,0,box.min.z)}).Max(p=>Vector3.Dot(p-storedCan.position,away));
            float shoeReach=home.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name=="Shoes").SelectMany(AdultRabbitSitCoat.World).Max(p=>Vector3.Dot(origin-p,away));
            float current=Vector3.Dot(origin-storedCan.position,away);
            // Clearance includes swing toes during the coming yaw, not just standing shoes.
            float retreat=Mathf.Max(0,canExtent+Mathf.Max(.24f,shoeReach)+.07f-current);
            if(retreat<.01f){RequestSit(true);return;}
            var target=origin+away*retreat;support.Capture();support.BeginExactPair(u=>new Pose(Vector3.Lerp(origin,target,u),rotation));Begin(TaskState.ClearingCan);
        }
        public void ResumeFlow(bool departure=false){
            if(!Active)return;
            fullFlow=true;
            if(State==TaskState.Resting)return; // Preserve elapsed seated breath/rest.
            if(State==TaskState.Standing){if(departure){pendingWalk=true;Depart();}else RequestSit(true);}
        }
        // Parent routine supplies already scaled 240Hz time; never scale twice.
        public void AdvanceFlow(float dt){if(Active)Tick(dt);}
        public void RequestSit(bool fromFlow=false){
            if(!Active)return;
            if(!fromFlow)fullFlow=false;
            pendingWalk=pendingRise=false;
            if(State==TaskState.Sitting||State==TaskState.Resting)return;
            if(State!=TaskState.Standing){pendingSit=true;return;}
            pendingSit=true;
            if(Vector3.Distance(home.resident.transform.position,approach)>.35f){
                var delta=approach-home.resident.transform.position;float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
                BeginWalk(approach,TaskState.Walking);
            }else BeginTurn();
        }
        void BeginWalk(Vector3 target,TaskState state){
            CaptureBlend(.2f);destination=target;
            var delta=target-home.resident.transform.position;
            float arrival=state==TaskState.Walking?Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg:home.resident.transform.eulerAngles.y;
            walkingRoute=new RabbitWalkingRoute(home.resident.transform,target,arrival);
            feet=new AdultRabbitFootTransition(home.resident,home.groundHeight){SmoothStopBalance=true,FollowWalkingHeading=true};
            feet.WalkingPoseAhead=distance=>walkingRoute.Predict(distance,feet.Speed>.1f?feet.Speed:.6f);
            feet.WalkingSpeedScale=walkingRoute.SpeedScale;
            feet.WalkingGoal=walkingRoute.Goal;
            feet.Request(true);stopRequested=false;Begin(state);
        }
        void BeginTurn(float targetYaw=float.NaN,bool forApproach=false){
            if(float.IsNaN(targetYaw))targetYaw=seatedYaw;turnToApproach=forApproach;
            float delta=Mathf.Abs(Mathf.DeltaAngle(home.resident.transform.eulerAngles.y,targetYaw));
            if(delta<=5){if(forApproach)BeginWalk(approach,TaskState.Walking);else BeginBackstep();return;}
            support.Capture();TurnSteps=delta<=90.1f?2:3;support.BeginTurn(targetYaw,TurnSteps,.4f,.04f);Begin(TaskState.Turning);
        }
        void BeginBackstep(){
            support.Capture();var origin=home.resident.transform.position;var rotation=home.resident.transform.rotation;
            support.BeginExactPair(u=>new Pose(Vector3.Lerp(origin,SeatPreparation,u),rotation));Begin(TaskState.Backstepping);
        }
        void BeginSit(){pendingSit=false;support.Capture();CaptureBlend(.2f,true);Begin(TaskState.Sitting);}
        public void RequestRise(){
            if(!Active)return;pendingWalk=pendingSit=false;fullFlow=false;
            if(State==TaskState.Resting){CaptureBlend(.25f,true);Begin(TaskState.Rising);}else if(State==TaskState.Sitting)pendingRise=true;
            else if(State!=TaskState.Rising&&State!=TaskState.Standing){StopTask();}
        }
        public void RequestWalk(){
            if(!Active)return;fullFlow=false;pendingSit=pendingRise=false;pendingWalk=true;
            if(State==TaskState.Resting){CaptureBlend(.25f,true);Begin(TaskState.Rising);}
            else if(State==TaskState.Standing)Depart();
            else if(State==TaskState.Walking){walkingRoute?.StopTurning();feet.WalkingPoseAhead=null;feet.Request(false);stopRequested=true;Begin(TaskState.Stopping);}
        }
        void Depart(){pendingWalk=false;BeginWalk(home.resident.transform.position+home.resident.transform.forward*departureDistance,TaskState.Departing);}
        public void StopTask(){
            fullFlow=pendingWalk=pendingRise=pendingSit=false;
            if(State==TaskState.Walking||State==TaskState.Departing){walkingRoute?.StopTurning();feet.WalkingPoseAhead=null;feet.Request(false);stopRequested=true;Begin(TaskState.Stopping);}
        }
        public void Advance(float seconds){if(!Active||home.paused)return;remainder+=seconds*(home.slow?.5f:1);while(remainder>=1f/240){remainder-=1f/240;Tick(1f/240);}}
        void Tick(float dt){
            clock+=dt;TimeInState+=dt;recoveryTime+=dt;
            if(State==TaskState.Walking||State==TaskState.Stopping||State==TaskState.Departing){
                bool approaching=State==TaskState.Walking;
                if(!stopRequested&&walkingRoute.Remaining<feet.EstimatedStopTravel+.14f){stopRequested=true;feet.WalkingPoseAhead=null;feet.Request(false);}
                feet.WalkingSpeedScale=walkingRoute.SpeedScale;
                var pose=walkingRoute.Advance(feet.Step(dt),dt);home.resident.transform.SetPositionAndRotation(pose.position,pose.rotation);
                feet.RestoreSourcePose();Sample(home.walkClip,(float)feet.WalkTime%.8f);
                // Preserve the established idle/walk blend without inheriting last frame's IK.
                var walkP=bones.Select(t=>t.localPosition).ToArray();var walkQ=bones.Select(t=>t.localRotation).ToArray();
                Sample(home.idleClip,(float)(clock%home.idleClip.length));
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.Lerp(bones[i].localPosition,walkP[i],feet.Weight);bones[i].localRotation=Quaternion.Slerp(bones[i].localRotation,walkQ[i],feet.Weight);}
                Blend();
                var source=pelvis.position;feet.Apply();
                if(feet.State==AdultRabbitFootTransition.Stage.Closing||feet.State==AdultRabbitFootTransition.Stage.Settling)walkingRoute?.StopTurning();
                if(stopRequested&&feet.State==AdultRabbitFootTransition.Stage.Idle){
                    recoveryOffset=pelvis.position-source+Vector3.up*support.RestingPelvisDrop;recoveryTime=0;support.Capture();
                    if(approaching&&pendingSit){BeginTurn();}else{Begin(TaskState.Standing);if(pendingWalk)Depart();else if(pendingSit)RequestSit();}
                }
            }else if(State==TaskState.Turning){
                support.PlaceRoot(TimeInState);Sample(home.walkClip,TimeInState%.8f);Blend();support.Apply(TimeInState);
                if(TimeInState>=support.Duration){support.Capture();if(pendingWalk)Depart();else if(pendingSit){if(turnToApproach)BeginWalk(approach,TaskState.Walking);else BeginBackstep();}else{CaptureBlend(.2f);Begin(TaskState.Standing);}}
            }else if(State==TaskState.Backstepping){
                support.PlaceRoot(TimeInState);Sample(home.walkClip,(.8f-TimeInState%.8f)%.8f);support.Apply(TimeInState);
                if(TimeInState>=support.Duration){support.Capture();if(pendingWalk)Depart();else if(pendingSit)BeginSit();else Begin(TaskState.Standing);}
            }else if(State==TaskState.ClearingCan){
                support.PlaceRoot(TimeInState);Sample(home.idleClip,(float)(clock%home.idleClip.length));Blend();support.Apply(TimeInState);
                if(TimeInState>=support.Duration){support.Capture();Begin(TaskState.Standing);if(fullFlow&&pendingSit)RequestSit(true);}
            }else if(State==TaskState.Sitting){
                Sample(sitClip,Mathf.Min(TimeInState,1.8f));Blend();support.HoldLift(.0536f*Ease((TimeInState-1.45f)/.35f));
                if(TimeInState>=1.8f){Begin(TaskState.Resting);if(continuousBreathing)CaptureBlend(.25f,true);if(pendingWalk||pendingRise){CaptureBlend(.25f,true);pendingRise=false;Begin(TaskState.Rising);}}
            }else if(State==TaskState.Resting){
                Sample(breatheClip,(float)((continuousBreathing?clock:TimeInState)%4));if(continuousBreathing)Blend();support.HoldLift(.0536f);
                if(fullFlow&&TimeInState>=restSeconds){pendingWalk=true;CaptureBlend(.25f,true);Begin(TaskState.Rising);}
            }else if(State==TaskState.Rising){
                Sample(standClip,Mathf.Min(TimeInState,1.8f));Blend();support.HoldLift(.0536f*(1-Ease(TimeInState/.35f)));
                if(TimeInState>=1.8f){Begin(TaskState.Standing);if(pendingWalk)Depart();else if(pendingSit)RequestSit();}
            }else{Sample(home.idleClip,(float)(clock%home.idleClip.length));Blend();support.Hold();}
            coat?.ApplyGentleSway(clock);MaxReach=Mathf.Max(MaxReach,support.MaxReachError);Measure();
        }
        void Measure(){
            for(int i=0;i<2;i++){
                bool planted=feet!=null&&(State==TaskState.Walking||State==TaskState.Departing||State==TaskState.Stopping)?(i==0?feet.LeftPlanted:feet.RightPlanted):support.Planted[i];
                float h=support.SoleHeight(i);MinSole=Mathf.Min(MinSole,h);if(planted)MaxGap=Mathf.Max(MaxGap,h);
                Vector3 p=support.FootPosition(i);if(planted&&plantedBefore[i])MaxDrift=Mathf.Max(MaxDrift,Vector3.Distance(p,anchors[i]));else if(planted)anchors[i]=p;plantedBefore[i]=planted;
            }
        }
        public void Pose(int action,int index){
            ResetTask();home.resident.transform.SetPositionAndRotation(SeatPreparation,Quaternion.Euler(0,seatedYaw,0));Sample(home.idleClip,0);support=new RabbitHomeFootwork(home.resident,home.groundHeight);support.Hold();support.Capture();
            var clip=action==0?sitClip:action==1?breatheClip:standClip;TimeInState=clip.length*index/7f;Sample(clip,TimeInState);support.HoldLift(.0536f*(action==1?1:action==0?Ease((TimeInState-1.45f)/.35f):1-Ease(TimeInState/.35f)));
            State=action==0?TaskState.Sitting:action==1?TaskState.Resting:TaskState.Rising;home.paused=true;fromPositions=null;
            // Explicit pose preview establishes a new contact frame; it is not a walking frame.
            Array.Clear(plantedBefore,0,2);MaxDrift=MaxGap=MaxReach=0;MinSole=float.PositiveInfinity;Measure();
        }
    }
}
