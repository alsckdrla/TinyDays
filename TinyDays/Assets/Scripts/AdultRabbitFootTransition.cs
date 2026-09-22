using System;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review
{
    // Flat-ground review controller. All targets are world-space and use the existing v2 bones.
    public sealed class AdultRabbitFootTransition
    {
        public enum Stage { Idle, Starting, Walking, Landing, Closing, Settling }
        public Stage State { get; private set; }
        public float Speed { get; private set; }
        public float Weight { get; private set; }
        public double WalkTime { get; private set; }
        public bool LeftPlanted => planted[0];
        public bool RightPlanted => planted[1];
        public float MaxReachError { get; private set; }
        public float LeftSoleHeight { get; private set; }
        public float RightSoleHeight { get; private set; }
        public bool BothFeetGrounded => LeftSoleHeight>=-.0005f&&LeftSoleHeight<=.005f&&RightSoleHeight>=-.0005f&&RightSoleHeight<=.005f;
        public float FootForwardGap => Mathf.Abs(Vector3.Dot(foot[0].position-foot[1].position,actor.forward));
        public string StatusLabel => State==Stage.Landing?"착지 중":State==Stage.Closing?"발 모으기":State==Stage.Settling?"양발 안정 중":State==Stage.Idle?"정지 완료":State==Stage.Starting?"출발 중":"걷는 중";
#if UNITY_EDITOR
        // Editor-only A/B verification; never changes the player or source animation assets.
        public static bool DisableClearanceForChecks;
#endif
        float balanceOffset,recovery,closingTravel,standingForward;
        float solveDt,smoothedHeight;
        bool heightReady;
        public float MaxTransitionLengthScale {get;private set;}=1;
        readonly Vector3[][] shoePoints=new Vector3[2][];
        bool heldLowerBody,captureLanding;
        readonly Vector3[] heldHip=new Vector3[2];
        readonly float[] heldUpper=new float[2],heldLower=new float[2];
        readonly Transform[] controlled;
        readonly Vector3[] sourcePosition;
        readonly Quaternion[] sourceRotation;
        readonly Transform actor,pelvis;
        readonly Transform[] thigh=new Transform[2],shin=new Transform[2],foot=new Transform[2];
        readonly Vector3[] target=new Vector3[2],from=new Vector3[2],destination=new Vector3[2],swingOffset=new Vector3[2];
        readonly Quaternion[] flat=new Quaternion[2];
        readonly float[] ground=new float[2],lateral=new float[2],upper=new float[2],lower=new float[2];
        readonly bool[] planted={true,true};
        float clock,duration,startSpeed,startWeight,decelPower,startEndSpeed,startDecelPower;
        double startPhase,endPhase;
        int swing;
        bool wanted;
        public AdultRabbitFootTransition(GameObject resident)
        {
            actor=resident.transform;var bones=resident.GetComponentsInChildren<Transform>();
            pelvis=bones.First(t=>t.name=="Pelvis");
            var shoes=resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Shoes");
            var mesh=shoes.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
            for(int i=0;i<2;i++){
                string suffix=i==0?"L":"R";
                thigh[i]=bones.First(t=>t.name=="Thigh_"+suffix);shin[i]=bones.First(t=>t.name=="Shin_"+suffix);foot[i]=bones.First(t=>t.name=="Foot_"+suffix);
                upper[i]=Vector3.Distance(thigh[i].position,shin[i].position);lower[i]=Vector3.Distance(shin[i].position,foot[i].position);
                flat[i]=foot[i].rotation;lateral[i]=actor.InverseTransformPoint(foot[i].position).x;
                int bi=Array.FindIndex(shoes.bones,b=>b==foot[i]);var matrix=foot[i].localToWorldMatrix*mesh.bindposes[bi];
                lateral[i]=actor.InverseTransformPoint(shoes.transform.TransformPoint(mesh.bindposes[bi].inverse.MultiplyPoint3x4(Vector3.zero))).x;
                shoePoints[i]=Enumerable.Range(0,vertices.Length).Where(v=>weights[v].boneIndex0==bi&&weights[v].weight0>.99f).Select(v=>mesh.bindposes[bi].MultiplyPoint3x4(vertices[v])).ToArray();
                float sole=Enumerable.Range(0,vertices.Length).Where(v=>weights[v].boneIndex0==bi&&weights[v].weight0>.99f).Min(v=>matrix.MultiplyPoint3x4(vertices[v]).y);
                ground[i]=foot[i].position.y-sole+.0025f;target[i]=foot[i].position;target[i].y=ground[i];
            }
            controlled=new[]{pelvis,thigh[0],shin[0],foot[0],thigh[1],shin[1],foot[1]};
            standingForward=Vector3.Dot((target[0]+target[1])*.5f-actor.position,actor.forward);
            sourcePosition=new Vector3[controlled.Length];sourceRotation=new Quaternion[controlled.Length];CaptureSourcePose();
            State=Stage.Idle;
        }
        void CaptureSourcePose(){for(int i=0;i<controlled.Length;i++){sourcePosition[i]=controlled[i].localPosition;sourceRotation[i]=controlled[i].localRotation;}}
        public void RestoreSourcePose(){
#if UNITY_EDITOR
            if(DisableClearanceForChecks)return;
#endif
            for(int i=0;i<controlled.Length;i++){controlled[i].localPosition=sourcePosition[i];controlled[i].localRotation=sourceRotation[i];}
        }
        public void Request(bool walk){
            if(wanted==walk)return;wanted=walk;
            if(walk){
                if(State==Stage.Idle||State==Stage.Landing||State==Stage.Closing||State==Stage.Settling)StartStep();
            }else if(State!=Stage.Idle&&State!=Stage.Settling&&State!=Stage.Closing)StopStep();
        }
        static float Ease(float t)=>t*t*(3-2*t);
        Vector3 GroundAt(int i,float distance)=>actor.position+actor.right*lateral[i]+actor.forward*distance+Vector3.up*(ground[i]-actor.position.y);
        void StartStep(){
            bool idleStart=State==Stage.Idle;
            swing=!planted[0]?0:!planted[1]?1:(Vector3.Dot(target[0]-target[1],actor.forward)<=0?0:1);
            if(idleStart&&!heldLowerBody)swing=0;
            if(idleStart)WalkTime=swing==0?.4:0;
            clock=0;duration=idleStart?.45f:.3f;startSpeed=Speed;startWeight=Weight;startPhase=WalkTime;
            float available=Mathf.Max(.025f,.255f+Vector3.Dot(target[1-swing]-actor.position,actor.forward));
            // A rear support foot limits acceleration, not the time available to place the
            // other foot. Compressing this step to 60ms produced abrupt crossed/squatting poses.
            startEndSpeed=Mathf.Clamp(2*available/duration-startSpeed,0,1.15f);
            // If the current velocity cannot stop within the support-foot allowance under
            // linear braking, use a steeper continuous deceleration, not excess travel.
            startDecelPower=startSpeed*duration*.5f>available?Mathf.Max(1,startSpeed*duration/available-1):0;
            double contact=swing==0?0:.4;
            endPhase=Math.Floor(WalkTime/.8)*.8+contact;while(endPhase<=WalkTime+.00001)endPhase+=.8;
            float travel=startDecelPower>0?startSpeed*duration/(startDecelPower+1):(startSpeed+startEndSpeed)*duration*.5f;
            for(int i=0;i<2;i++){from[i]=target[i];destination[i]=i==swing?GroundAt(i,travel+.23f):target[i];planted[i]=i!=swing;}
            State=Stage.Starting;
        }
        void StopStep(){
            if(heldLowerBody)HoldLowerBody();
            clock=0;startSpeed=Speed;startWeight=Weight;
            // Treat the sub-millimetre touchdown neighbourhood as double support.
            for(int i=0;i<2;i++)if(target[i].y-ground[i]<.001f){target[i].y=ground[i];planted[i]=true;}
            bool airborne=!planted[0]||!planted[1];swing=!planted[0]?0:!planted[1]?1:-1;
            startPhase=WalkTime;endPhase=airborne?(Math.Floor(WalkTime/.4+1e-6)+1)*.4:WalkTime;
            float phase=(float)(WalkTime/.8%1);float remaining=.4f*(1-((phase*2)%1));
            duration=airborne?Mathf.Clamp(remaining,.15f,.45f):.15f;
            float allowed=.2f;
            for(int i=0;i<2;i++)if(planted[i])allowed=Mathf.Min(allowed,Mathf.Max(.015f,.36f+Vector3.Dot(target[i]-actor.position,actor.forward)));
            decelPower=Mathf.Max(1,startSpeed*duration/allowed-1);
            float distance=startSpeed*duration/(decelPower+1);
            for(int i=0;i<2;i++){from[i]=target[i];destination[i]=i==swing?GroundAt(i,Mathf.Max(distance+.045f,Vector3.Dot(target[i]-actor.position,actor.forward))):target[i];}
            State=Stage.Landing;
        }
        void BeginClosing(){
            HoldLowerBody();
            float difference=Vector3.Dot(target[0]-target[1],actor.forward);
            if(Mathf.Abs(difference)<=.01f){BeginSettling();return;}
            swing=difference<0?0:1;int support=1-swing;
            for(int i=0;i<2;i++){from[i]=target[i];destination[i]=target[i];}
            destination[swing]+=actor.forward*Vector3.Dot(target[support]-target[swing],actor.forward);
            destination[swing].y=ground[swing];
            closingTravel=Mathf.Max(0,Vector3.Dot((destination[0]+destination[1])*.5f-actor.position,actor.forward)-standingForward);
            clock=0;duration=.3f;startWeight=Weight;State=Stage.Closing;planted[swing]=false;
        }
        void BeginSettling(){State=Stage.Settling;captureLanding=true;startWeight=Weight;clock=0;Speed=0;}
        public float Step(float dt){
            solveDt=dt;
            if(State==Stage.Starting)recovery=.45f;else recovery=Mathf.Max(0,recovery-dt);
            bool standing=State==Stage.Landing||State==Stage.Closing||State==Stage.Settling||State==Stage.Idle;
            float balance=standing?Mathf.Clamp(Vector3.Dot((target[0]+target[1])*.5f-actor.position,actor.forward),-.2f,.2f):0;
            balanceOffset=Mathf.MoveTowards(balanceOffset,balance,dt*1.5f);
            float before=clock;clock+=dt;float movement=0;
            if(State==Stage.Idle){Speed=Weight=0;return 0;}
            if(State==Stage.Starting){
                float a=Mathf.Clamp01(before/duration),b=Mathf.Clamp01(clock/duration);
                movement=duration*(startSpeed*(b-a)+(startEndSpeed-startSpeed)*(b*b-a*a)*.5f);
                Speed=Mathf.Lerp(startSpeed,startEndSpeed,b);
                if(startDecelPower>0){movement=startSpeed*duration/(startDecelPower+1)*(Mathf.Pow(1-a,startDecelPower+1)-Mathf.Pow(1-b,startDecelPower+1));Speed=startSpeed*Mathf.Pow(1-b,startDecelPower);}
                Weight=Mathf.Lerp(startWeight,1,Ease(b));WalkTime=startPhase+(endPhase-startPhase)*Ease(b);
                target[swing]=Vector3.Lerp(from[swing],destination[swing],Ease(b))+Vector3.up*(.018f*Mathf.Sin(Mathf.PI*b)*Mathf.Sin(Mathf.PI*b));
                // A restart with an already raised foot continues its landing instead of adding
                // a second lift on top of the interrupted swing.
                if(from[swing].y>ground[swing]+.01f)target[swing].y=Mathf.Lerp(from[swing].y,destination[swing].y,1-(1-b)*(1-b));
                if(b>=1){State=Stage.Walking;planted[0]=planted[1]=true;clock=0;}
            }else if(State==Stage.Walking){float oldSpeed=Speed;Speed=Mathf.MoveTowards(Speed,1.15f,dt*(1.15f/.15f));Weight=1;movement=(oldSpeed+Speed)*.5f*dt;WalkTime+=movement/1.15f;
            }else if(State==Stage.Landing){
                float a=Mathf.Clamp01(before/duration),b=Mathf.Clamp01(clock/duration);
                movement=startSpeed*duration/(decelPower+1)*(Mathf.Pow(1-a,decelPower+1)-Mathf.Pow(1-b,decelPower+1));
                Speed=startSpeed*Mathf.Pow(1-b,decelPower);Weight=startWeight;
                WalkTime=startPhase+(endPhase-startPhase)*Ease(b);
                if(swing>=0){target[swing]=Vector3.Lerp(from[swing],destination[swing],Ease(b));target[swing].y=Mathf.Lerp(from[swing].y,destination[swing].y,1-(1-b)*(1-b));}
                // Apply() checks each actual shoe after IK before completing the landing.
                if(b>=1)Speed=0;
            }else if(State==Stage.Closing){
                float a=Mathf.Clamp01(before/duration),b=Mathf.Clamp01(clock/duration);
                movement=closingTravel*(Ease(b)-Ease(a));Speed=closingTravel*6*b*(1-b)/duration;
                target[swing]=Vector3.Lerp(from[swing],destination[swing],Ease(b))+Vector3.up*(.015f*Mathf.Sin(Mathf.PI*b)*Mathf.Sin(Mathf.PI*b));
                Weight=startWeight*(1-.5f*Ease(b));
            }else if(State==Stage.Settling){Speed=0;Weight=startWeight*(1-Ease(Mathf.Clamp01(clock/.15f)));if(clock>=.15f&&BothFeetGrounded&&FootForwardGap<=.01f){State=Stage.Idle;Weight=0;}}
            return movement;
        }
        public void Apply(){
            CaptureSourcePose();
            // The source clips key segment translations as well as rotations. Mixing the legs
            // back to idle after a staggered landing would change their lengths and squat the
            // pelvis between fixed feet. Hold the landed lower body while the upper body settles.
            for(int i=0;i<2;i++){
                upper[i]=Vector3.Distance(thigh[i].position,shin[i].position);lower[i]=Vector3.Distance(shin[i].position,foot[i].position);
            }
            if(State==Stage.Walking)heldLowerBody=false;
            bool preserveLandedPose=heldLowerBody;
#if UNITY_EDITOR
            if(DisableClearanceForChecks)preserveLandedPose=false;
#endif
            if(preserveLandedPose){
                float blend=State==Stage.Starting||State==Stage.Closing?Ease(Mathf.Clamp01(clock/duration)):
                    State==Stage.Settling||State==Stage.Idle?1:0;
                for(int i=0;i<2;i++){
                    thigh[i].localPosition=Vector3.Lerp(heldHip[i],thigh[i].localPosition,blend);
                    upper[i]=Mathf.Lerp(heldUpper[i],upper[i],blend);lower[i]=Mathf.Lerp(heldLower[i],lower[i],blend);
                }
            }
            float postureWeight=State==Stage.Walking?Ease(Mathf.Clamp01(recovery/.45f)):1;
            float commonUpper=Mathf.Max(upper[0],upper[1]),commonLower=Mathf.Max(lower[0],lower[1]);
            for(int i=0;i<2;i++){upper[i]=Mathf.Lerp(upper[i],commonUpper,postureWeight);lower[i]=Mathf.Lerp(lower[i],commonLower,postureWeight);}
            pelvis.position+=actor.forward*balanceOffset;
            if(State==Stage.Walking){
                for(int i=0;i<2;i++){
                    float phase=(float)((WalkTime/.8+(i==0?0:.5))%1);bool support=phase<.5f;
                    if(support){if(!planted[i]){target[i]=foot[i].position;target[i].y=ground[i];}planted[i]=true;}
                    else{
                        if(planted[i])swingOffset[i]=target[i]-foot[i].position;
                        // On first swing after starting, retain the previous supporting foot position.
                        planted[i]=false;target[i]=foot[i].position+swingOffset[i]*(1-Ease((phase-.5f)*2));target[i].y=Mathf.Max(ground[i],target[i].y);
                    }
                }
            }
            // Fit both legs; transitions may also lift the pelvis out of a frozen crouch.
            float drop=float.NegativeInfinity;
            for(int i=0;i<2;i++){
                // Do not add the transition's bend margin to the approved straight support pose.
                float reachMargin=State==Stage.Walking?.0001f:.002f;
#if UNITY_EDITOR
                if(DisableClearanceForChecks)reachMargin=.002f;
#endif
                var d=thigh[i].position-target[i];float horizontal=d.x*d.x+d.z*d.z;float length=upper[i]+lower[i]-reachMargin;
                float maxY=Mathf.Sqrt(Mathf.Max(.0001f,length*length-horizontal));drop=Mathf.Max(drop,d.y-maxY);
            }
            float rawHeight=pelvis.position.y-Mathf.Max(-.06f*postureWeight,drop);
            if(!heightReady){smoothedHeight=rawHeight;heightReady=true;}
            if(solveDt>0)smoothedHeight=Mathf.MoveTowards(smoothedHeight,rawHeight,.35f*solveDt);
            var fitted=pelvis.position;fitted.y=Mathf.Lerp(rawHeight,smoothedHeight,postureWeight);pelvis.position=fitted;
            // Source clips animate segment translations. Preserve a continuous body height
            // by spreading small reach corrections over both segments, rather than snapping
            // the pelvis downward. This is bounded and fades with the transition correction.
            for(int i=0;i<2;i++){
                float needed=Vector3.Distance(thigh[i].position,target[i])+.002f;
                float scale=Mathf.Clamp(needed/(upper[i]+lower[i]),1,1+.12f*postureWeight);
                upper[i]*=scale;lower[i]*=scale;MaxTransitionLengthScale=Mathf.Max(MaxTransitionLengthScale,scale);
            }
            // Reach safety remains authoritative even when height smoothing cannot fit.
            float safety=0;
            for(int i=0;i<2;i++){var d=thigh[i].position-target[i];float length=upper[i]+lower[i]-.0001f;
                safety=Mathf.Max(safety,d.y-Mathf.Sqrt(Mathf.Max(.0001f,length*length-d.x*d.x-d.z*d.z)));}
            pelvis.position-=Vector3.up*safety;solveDt=0;
            for(int i=0;i<2;i++){
                // Once recovery has finished, an unconstrained swing needs no IK rewrite.
                // Keep the approved source knee/cloth pose instead of recomputing it numerically.
                bool authoredSwing=State==Stage.Walking&&!planted[i]&&Vector3.Distance(foot[i].position,target[i])<.002f;
#if UNITY_EDITOR
                if(DisableClearanceForChecks)authoredSwing=false;
#endif
                if(!authoredSwing)Solve(i);
            }
            LeftSoleHeight=SoleHeight(0);RightSoleHeight=SoleHeight(1);
            if((State==Stage.Landing||State==Stage.Closing)&&clock>=duration){
                planted[0]=LeftSoleHeight>=-.0005f&&LeftSoleHeight<=.005f;
                planted[1]=RightSoleHeight>=-.0005f&&RightSoleHeight<=.005f;
                if(BothFeetGrounded){if(State==Stage.Landing)BeginClosing();else BeginSettling();}
                else for(int i=0;i<2;i++)if(!planted[i]){target[i].y+=.0025f-(i==0?LeftSoleHeight:RightSoleHeight);destination[i].y=target[i].y;}
            }
            if(captureLanding){HoldLowerBody();captureLanding=false;}
        }
        float SoleHeight(int i){float minimum=float.MaxValue;var matrix=foot[i].localToWorldMatrix;foreach(var p in shoePoints[i])minimum=Mathf.Min(minimum,matrix.MultiplyPoint3x4(p).y);return minimum;}
        void HoldLowerBody(){
            heldLowerBody=true;
            for(int i=0;i<2;i++){heldHip[i]=thigh[i].localPosition;heldUpper[i]=upper[i];heldLower[i]=lower[i];}
        }
        void Solve(int i){
            var a=thigh[i].position;var delta=target[i]-a;float reach=upper[i]+lower[i]-.0001f;
            MaxReachError=Mathf.Max(MaxReachError,Mathf.Max(0,delta.magnitude-reach));
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper[i]-lower[i])+.0001f,reach);var direction=delta.normalized;
            float along=(upper[i]*upper[i]-lower[i]*lower[i]+distance*distance)/(2*distance);
            // The imported rig mirrors Blender's X axis. Never infer knee direction from L/R names.
            // Bend in the forward hip-to-ankle plane, without a sideways anti-clipping bias.
            var pole=Vector3.ProjectOnPlane(actor.forward,direction).normalized;
            var knee=a+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,upper[i]*upper[i]-along*along));
            thigh[i].rotation=Quaternion.FromToRotation(shin[i].position-a,knee-a)*thigh[i].rotation;
            shin[i].position=knee;
            shin[i].rotation=Quaternion.FromToRotation(foot[i].position-shin[i].position,target[i]-shin[i].position)*shin[i].rotation;
            foot[i].position=a+direction*distance;
            foot[i].rotation=flat[i];
        }
    }
}
