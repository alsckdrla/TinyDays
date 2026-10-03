using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TinyDays.Review {
    public sealed partial class RabbitWaterReview {
        public AnimationClip pickup,putdown;
        AnimationClipPlayable pickupPlay,putdownPlay;
        bool carryingCan=true,navigation,walkAfterTurn;
        float finalYaw;
        RabbitWalkingRoute walkingRoute;
        Vector3 storedPosition;
        Vector3 propBlendOffset;
        float propBlendTime;
        float workReachOffset;
        Quaternion storedRotation;
        Quaternion pickupStartRotation;
        public bool Carrying=>carryingCan;
        public float MaxCanBoundaryJump {get;private set;}
        void InitWorkPlayables(){
            if(pickup){pickupPlay=AnimationClipPlayable.Create(graph,pickup);graph.Connect(pickupPlay,0,mix,3);}
            if(putdown){putdownPlay=AnimationClipPlayable.Create(graph,putdown);graph.Connect(putdownPlay,0,mix,4);}
        }
        public void BeginTravel(Vector3 target,float arrivalYaw){
            if(!Active||State!=TaskState.Ready)return;
            navigation=true;finalYaw=arrivalYaw;
            Vector3 d=target-home.resident.transform.position;d.y=0;
            if(d.magnitude>=.08f){
                walkingRoute=new RabbitWalkingRoute(home.resident.transform,target,arrivalYaw);
                feet=new AdultRabbitFootTransition(home.resident,home.groundHeight){SmoothStopBalance=true,FollowWalkingHeading=true};
                feet.WalkingPoseAhead=distance=>walkingRoute.Predict(distance,feet.Speed>.1f?feet.Speed:.6f);
                feet.WalkingSpeedScale=walkingRoute.SpeedScale;
                feet.WalkingGoal=walkingRoute.Goal;
                feet.Request(true);stopRequested=false;followPour=false;walkAfterTurn=false;
                State=TaskState.Walking;TimeInState=0;return;
            }
            if(d.magnitude<.3f&&d.magnitude>.005f){
                var from=home.resident.transform.position;var rotation=home.resident.transform.rotation;
                support.BeginExactPair(u=>new Pose(Vector3.Lerp(from,target,u),rotation));
                State=TaskState.Repositioning;TimeInState=0;Array.Clear(wasPlanted,0,2);return;
            }
            // Existing .27m deceleration completes about .14m before its target.
            // Offset the braking marker, not the resident or planted feet.
            destination=target+(d.sqrMagnitude>.0001f?d.normalized*.14f:Vector3.zero);
            float yaw=d.sqrMagnitude>.0001f?Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg:arrivalYaw;
            if(d.magnitude<.08f){walkAfterTurn=false;BeginTurn(arrivalYaw);}
            else {walkAfterTurn=true;BeginTurn(yaw);}
        }
        void BeginTurn(float yaw){
            float angle=Mathf.Abs(Mathf.DeltaAngle(home.resident.transform.eulerAngles.y,yaw));
            if(angle<=5){if(walkAfterTurn){walkAfterTurn=false;StartWalk(false);}else{State=TaskState.Ready;navigation=false;}return;}
            support.Capture();support.BeginTurn(yaw,angle<=90?2:3,.4f);
            State=TaskState.Turning;TimeInState=0;Array.Clear(wasPlanted,0,2);
        }
        void TickTurn(){
            support.PlaceRoot(TimeInState);Sample(0);
            // Carrying arms stay on the handle; ordinary arms retain a small gait swing.
            if(!carryingCan)foreach(var bone in bones)if(bone.name.StartsWith("UpperArm_"))bone.localRotation*=Quaternion.AngleAxis(6*Mathf.Sin(TimeInState*Mathf.PI/.4f),Vector3.right);
            support.Apply(TimeInState);
            if(TimeInState>=support.Duration){
                support.Capture();State=TaskState.Ready;TimeInState=0;
                if(walkAfterTurn){walkAfterTurn=false;StartWalk(false);}else navigation=false;
            }
        }
        void FinishNavigation(){if(walkingRoute!=null){walkingRoute=null;navigation=false;return;}walkAfterTurn=false;BeginTurn(finalYaw);}
        void TickReposition(){support.PlaceRoot(TimeInState);Sample(0);support.Apply(TimeInState);if(TimeInState>=support.Duration){support.Capture();State=TaskState.Ready;TimeInState=0;FinishNavigation();}}
        public void BeginPickup(Vector3 position,Quaternion rotation,float reachOffset=0){BeginWork(true,position,rotation,reachOffset);}
        public void BeginPutdown(Vector3 position,Quaternion rotation,float reachOffset=0){BeginWork(false,position,rotation,reachOffset);}
        void BeginWork(bool pick,Vector3 position,Quaternion rotation,float reachOffset=0){
            if(State!=TaskState.Ready||!pickup||!putdown)return;
            storedPosition=position;storedRotation=rotation;State=pick?TaskState.Picking:TaskState.Putting;
            pickupStartRotation=can.rotation;
            workReachOffset=reachOffset;
            TimeInState=0;PourAmount=0;support.Capture();Array.Clear(wasPlanted,0,2);
        }
        void TickWork(){
            bool pick=State==TaskState.Picking;float t=Mathf.Min(TimeInState,1.8f);
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=restPositions[i];bones[i].localRotation=restRotations[i];}
            for(int i=0;i<5;i++)mix.SetInputWeight(i,i==(pick?3:4)?1:0);
            if(pick)pickupPlay.SetTime(t);else putdownPlay.SetTime(t);graph.Evaluate(0);
            float bend=pick?Ease(t/.65f)*(1-Ease((t-.75f)/1.05f)):Ease(t/1.05f)*(1-Ease((t-1.15f)/.65f));
            // Only the connected safe approach uses a little extra whole-body reach.
            // Feet remain planted, all bone lengths and source clip curves are unchanged.
            pelvis.position+=home.resident.transform.forward*(workReachOffset*bend);support.Hold();
            float lift=pick?Ease((t-.75f)/1.05f):1-Ease(t/1.05f);
            var actor=home.resident.transform;
            var held=spine.position+actor.rotation*canOffset;
            var groundRotation=pick?Quaternion.Slerp(pickupStartRotation,Quaternion.Euler(0,actor.eulerAngles.y,0),Ease(t/.65f)):storedRotation;
            can.SetPositionAndRotation(Vector3.Lerp(storedPosition,held,lift),Quaternion.Slerp(groundRotation,actor.rotation,lift));
            float contact=pick?Ease((t-.3f)/.35f):1-Ease((t-1.15f)/.65f);
            if(lift>0&&lift<1&&contact>.999f)for(int pass=0;pass<4;pass++)for(int i=0;i<2;i++){
                float reach=Vector3.Distance(arms[i].Upper.position,arms[i].Lower.position)+Vector3.Distance(arms[i].Lower.position,arms[i].End.position)-.0005f;
                var delta=grips[i].position-arms[i].Upper.position;
                if(delta.magnitude>reach){var p=can.position-delta.normalized*(delta.magnitude-reach);p.y=Mathf.Max(storedPosition.y,p.y);can.position=p;}
            }
            for(int i=0;i<2;i++){
                var target=Vector3.Lerp(arms[i].End.position,grips[i].position,contact);
                float length=Vector3.Distance(arms[i].Upper.position,arms[i].Lower.position)+Vector3.Distance(arms[i].Lower.position,arms[i].End.position);
                var delta=target-arms[i].Upper.position;if(contact<.999f&&delta.magnitude>length-.001f)target=arms[i].Upper.position+delta.normalized*(length-.001f);
                MaxReach=Mathf.Max(MaxReach,Vector3.Distance(arms[i].Upper.position,target)-length);
                float side=Mathf.Sign(actor.InverseTransformPoint(arms[i].Upper.position).x);
                arms[i].Solve(target,Quaternion.Slerp(arms[i].End.rotation,can.rotation*handRotations[i],contact),actor.forward,actor.rotation*new Vector3(side,-.6f,.15f));
                if(contact>.999f)MaxGripError=Mathf.Max(MaxGripError,Vector3.Distance(grips[i].position,arms[i].End.position));
            }
            if(TimeInState>=1.8f){
                var old=can.position;EnterCurrent(pick);
                if(!pick)can.SetPositionAndRotation(storedPosition,storedRotation);
                MaxCanBoundaryJump=Mathf.Max(MaxCanBoundaryJump,Vector3.Distance(old,can.position));
            }
        }
        Vector3[] handoffPositions;Quaternion[] handoffRotations;float handoffTime;
        public void EnterCurrentFromPose(bool carrying){
            var transforms=home.resident.GetComponentsInChildren<Transform>().Where(t=>t!=home.resident.transform).ToArray();
            var p=transforms.Select(t=>t.localPosition).ToArray();var q=transforms.Select(t=>t.localRotation).ToArray();
            EnterCurrent(carrying);clock=home.AutomaticBlinkClock;handoffPositions=p;handoffRotations=q;handoffTime=0;
            for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=p[i];transforms[i].localRotation=q[i];}
            // Remove the captured one-time floor solve before the next floor solve.
            int pi=Array.IndexOf(transforms,pelvis);handoffPositions[pi]=pelvis.parent.InverseTransformPoint(pelvis.position+Vector3.up*support.RestingPelvisDrop);
            support.Capture();
            // EnterCurrent's temporary source sample is not a planted frame of
            // the incoming actor. Seed contacts from the restored handoff pose.
            Array.Clear(wasPlanted,0,2);Measure();
        }
        public void AdvanceFlow(float dt){if(Active)Tick(dt);}
        public void PoseWork(bool pick,int index,Vector3 position,Quaternion rotation){
            EnterCurrent(!pick);BeginWork(pick,position,rotation);TimeInState=1.8f*index/7;TickWork();home.paused=true;
        }
    }
}
