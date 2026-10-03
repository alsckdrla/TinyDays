using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TinyDays.Review
{
    // Authored one-resident doorway study. The house/door anchors are data, not
    // a production pathfinding or autonomous-life implementation.
    public sealed class RabbitHomeLifeReview : MonoBehaviour
    {
        public enum Step { Inside, ApproachInside, OpenInside, CrossOut, Outside,
                           ApproachOutside, OpenOutside, CrossIn, WalkToRoom,
                           PassDeceleration, WaitForClosing, WalkToDestination }
        public GameObject resident;
        public Transform hinge, outsideHandle, insideHandle;
        public Camera reviewCamera;
        public AnimationClip idleClip, walkClip, interactClip;
        public Vector3 insideWait, insideDoor, outsideWait, outsideDoor, outsideClear;
        public float doorOpenAngle=-95;
        public float groundHeight=.275f;
        public bool automatic, paused, slow;
        public RabbitWaterReview watering;
        public RabbitBenchReview benchRest;
        public RabbitWaterLifeFlow lifeFlow;
        public bool FlowMode {get;private set;}
        public void SelectLifeFlow(){var face=faceMotion;ResetLifeFlow();faceMotion=face;}
        public void ResetLifeFlow(){ResetStudy();FlowMode=true;automatic=false;lifeFlow.ResetTask();ViewLifeFlow(45);}
        public void ViewLifeFlow(float angle){pivot=new Vector3(-.6f,1.05f,-3.8f);yaw=180+angle;pitch=15;distance=10;reviewCamera.fieldOfView=40;ApplyCamera();}
        public void AdvanceDoorTick(float dt){Tick(dt);}
        public bool BenchMode {get;private set;}
        int benchPoseAction;
        public void SelectBenchMode(){var face=faceMotion;ResetStudy();faceMotion=face;BenchMode=true;automatic=false;benchRest.ResetTask();ViewBench(140);}
        public void ViewBench(float angle){pivot=new Vector3(-2.5f,.95f,-3.6f);yaw=angle;pitch=14;distance=4.8f;reviewCamera.fieldOfView=40;ApplyCamera();}
        public bool WaterMode {get;private set;}
        int waterPoseAction;
        public void SuspendPose(){feet?.RestoreSourcePose();if(graph.IsValid())graph.Destroy();}
        public void SelectWaterMode(bool enabled){
            var face=faceMotion;if(watering)watering.Shutdown();WaterMode=false;ResetStudy();faceMotion=face;
            if(enabled&&watering){WaterMode=true;automatic=false;watering.ResetTask();ViewWater(130);}
        }
        public void ViewWater(float angle){pivot=new Vector3(1,.95f,-3.7f);yaw=angle;pitch=12;distance=4.8f;reviewCamera.fieldOfView=40;ApplyCamera();}
        public Step Current { get; private set; }=Step.Inside;
        public bool Inside => Current==Step.Inside;
        public bool Outside => Current==Step.Outside;
        public bool DoorClosed => Mathf.Abs(hinge.localEulerAngles.y)<.1f;
        public float Clock { get; private set; }
        public float DoorAngle {get;private set;}
        public float TotalTravel {get;private set;}
        public string FootStatus => feet==null?"none":feet.State+" / "+feet.FootForwardGap.ToString("F3")+" / "+feet.LeftSoleHeight.ToString("F3")+","+feet.RightSoleHeight.ToString("F3");
        public int Trips {get;private set;}
        public string Pending => pending.HasValue?(pending.Value?"외출":"귀가"):"없음";
        bool? pending;
        AdultRabbitFootTransition feet;
        RabbitHomeFootwork footwork;
        PlayableGraph graph;
        AnimationClipPlayable idlePlayable,walkPlayable;
        AnimationMixerPlayable mixer;
        Transform[] poseBones;
        Transform pelvis;
        Vector3 restRecoveryOffset;
        float restRecoveryTime=1;
        Vector3[] basePositions;
        Quaternion[] baseRotations;
        double idleTime;
        int navPhase;
        float actionTime;
        bool usingWalk;
        public float MinimumSole {get;private set;}
        public float MaximumSupportDrift {get;private set;}
        public float MaximumSupportGap {get;private set;}
        public float MaxStepReach=>footwork==null?0:footwork.MaxReachError;
        public string SupportDetail {get;private set;}
        public string ReachDetail=>footwork?.ReachDetail;
        readonly Vector3[] lastSupport=new Vector3[2];
        readonly bool[] wasPlanted=new bool[2];
        bool stopping;
        float waitClock,remainder;
        Vector3 pivot,previous;
        float yaw=155,pitch=18,distance=8;
        int drag=-1;
        Vector2 panelScroll;
        Font font;
        GUIStyle buttonStyle,labelStyle;
        const float PanelWidth=300;
        Rect Panel=>new Rect(12,12,Mathf.Min(PanelWidth,Screen.width-24),Screen.height-24);
        public static bool PointerInPanel(Vector2 p,float w,float h)=>new Rect(12,12,Mathf.Min(PanelWidth,w-24),h-24).Contains(p);
        void OnEnable(){if(Application.isPlaying){font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",13);ResetStudy();}}
        bool smoke;
        System.Collections.IEnumerator Start(){
            smoke=Environment.GetCommandLineArgs().Any(a=>new[]{"-homeLifeSmoke","-waterSmoke","-benchSmoke","-waterLifeSmoke","-waterBenchLifeSmoke"}.Contains(a));
            if(!Environment.GetCommandLineArgs().Contains("-homeLifeSmoke"))yield break;
            if(!smoke)yield break;
            ResetStudy();
            foreach(bool exit in new[]{true,false}){
                Request(exit);
                for(int i=0;i<60*240&&!(exit?Outside:Inside);i++){
                    Advance(1f/60);if(i%120==0)yield return null;
                }
                if(!(exit?Outside:Inside)||!DoorClosed){Debug.LogError("HOME_PLAYER_SMOKE_FAILED "+Current);Application.Quit(1);yield break;}
            }
            bool pass=UnsafeDoorSamples==0&&MaxTurnSteps<=3&&MinimumSole>=-.0005f&&MaximumSupportDrift<=.0035f;
            Debug.Log($"HOME_PLAYER_SMOKE_{(pass?"OK":"FAILED")} trips={Trips} sole={MinimumSole:F6} drift={MaximumSupportDrift:F6} turnSteps={MaxTurnSteps}. Automated callbacks, not OS input.");
            Application.Quit(pass?0:1);
        }
        void OnDisable(){faceMotion?.Clear();faceMotion=null;drag=-1;pending=null;feet?.RestoreSourcePose();feet=null;if(graph.IsValid())graph.Destroy();if(font)Destroy(font);}
        void OnApplicationFocus(bool focus){if(!focus)drag=-1;}
        static float Ease(float u){u=Mathf.Clamp01(u);return u*u*(3-2*u);}
        static float Yaw(Vector3 direction)=>Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
        void SetYaw(float degrees){resident.transform.rotation=Quaternion.Euler(0,degrees,0);}
        void SetDoor(float angle){DoorAngle=angle;hinge.localRotation=Quaternion.Euler(0,angle,0);}
        public void ResetStudy(){
            faceMotion=null;
            if(lifeFlow)lifeFlow.Shutdown();FlowMode=false;
            if(benchRest)benchRest.Shutdown();BenchMode=false;
            if(watering)watering.Shutdown();WaterMode=false;
            if(graph.IsValid())graph.Destroy();
            pending=null;automatic=paused=slow=false;waitClock=remainder=Clock=TotalTravel=0;Trips=0;
            Current=Step.Inside;SetDoor(0);resident.transform.position=insideWait;SetYaw(180);
            idleClip.SampleAnimation(resident,0);
            poseBones=resident.GetComponentsInChildren<Transform>().Where(t=>t!=resident.transform).ToArray();
            pelvis=poseBones.First(t=>t.name=="Pelvis");restRecoveryOffset=Vector3.zero;restRecoveryTime=1;
            basePositions=poseBones.Select(t=>t.localPosition).ToArray();baseRotations=poseBones.Select(t=>t.localRotation).ToArray();
            graph=PlayableGraph.Create("Rabbit home continuous locomotion");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            idlePlayable=AnimationClipPlayable.Create(graph,idleClip);walkPlayable=AnimationClipPlayable.Create(graph,walkClip);
            mixer=AnimationMixerPlayable.Create(graph,2);graph.Connect(idlePlayable,0,mixer,0);graph.Connect(walkPlayable,0,mixer,1);
            var animator=resident.GetComponent<Animator>();animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            AnimationPlayableOutput.Create(graph,"Home pose",animator).SetSourcePlayable(mixer);graph.Play();
            idleTime=0;SampleBase(0,0);feet=new AdultRabbitFootTransition(resident,groundHeight);
            footwork=new RabbitHomeFootwork(resident,groundHeight){FitStandingReach=true};
            // Initialize the resting clearance now, not on the first moving frame.
            footwork.Hold();footwork.Capture();
            MinimumSole=float.PositiveInfinity;MaximumSupportDrift=MaximumSupportGap=0;
            Array.Clear(wasPlanted,0,2);navPhase=0;actionTime=0;usingWalk=false;
            Door=DoorState.Closed;doorClock=DoorClock=0;TurnSteps=MaxTurnSteps=UnsafeDoorSamples=EarlyPassSamples=0;
            MaximumLateralDeviation=ReverseTravel=MaximumClosingDrift=0;
            ClosingStops=PrematureDepartureSamples=0;
            BodyClearanceRadius=.55f;
            foreach(var skin in resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(v=>v.enabled)){
                var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
                var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                for(int i=0;i<vertices.Length;i++){
                    var v=vertices[i];var w=weights[i];
                    var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                    var local=resident.transform.InverseTransformPoint(p);BodyClearanceRadius=Mathf.Max(BodyClearanceRadius,new Vector2(local.x,local.z).magnitude);
                }
            }
            Home();
        }
        public void RequestExit(){Request(true);}
        public void RequestEnter(){Request(false);}
        public void Request(bool exit){
            if((exit&&Outside)||(!exit&&Inside)){pending=null;return;}
            if((exit&&Inside)||(!exit&&Outside)){
                // Enter the requested phase even while paused; Advance keeps its
                // clock frozen until Resume, so the request cannot get stranded.
                exiting=exit;Begin(exit?Step.ApproachInside:Step.ApproachOutside);
            }else pending=exit;
        }
        public enum DoorState { Closed, Waiting, Opening, Open, ClosingDelay, Closing }
        public DoorState Door {get;private set;}
        public int TurnSteps {get;private set;}
        public int MaxTurnSteps {get;private set;}
        public int UnsafeDoorSamples {get;private set;}
        public int EarlyPassSamples {get;private set;}
        public float MaximumLateralDeviation {get;private set;}
        public float ReverseTravel {get;private set;}
        public float DoorClock {get;private set;}
        public float BodyClearanceRadius {get;private set;}
        public float MaximumClosingDrift {get;private set;}
        public int ClosingStops {get;private set;}
        public int PrematureDepartureSamples {get;private set;}
        Vector3 closingPosition;
        Quaternion closingRotation;
        bool exiting;
        Vector3 travelDirection;
        RabbitWalkingRoute walkingRoute;
        float doorClock;
        public string DoorLabel=>Door==DoorState.Closed?"닫힘":Door==DoorState.Waiting?"열기 대기":Door==DoorState.Opening?"열리는 중":Door==DoorState.Open?"열림":Door==DoorState.ClosingDelay?"닫기 대기":"닫히는 중";
        public string MovementLabel=>Current==Step.Inside?"실내 대기":Current==Step.Outside?"앞마당 대기":Current==Step.PassDeceleration?"통과 후 감속":Current==Step.WaitForClosing?"문 닫힘 대기":Current==Step.WalkToDestination?"목적지로 이동":Current==Step.ApproachInside||Current==Step.ApproachOutside?"방향 맞추기":Current==Step.WalkToRoom?"문 앞 접근":Current==Step.OpenInside||Current==Step.OpenOutside?"문 열림 대기":Current==Step.CrossOut?"외출 중":"귀가 중";
        public Vector3 ClosingWaitTarget(bool exit){
            // 25cm includes the existing 15cm stopping tolerance plus 10cm reserve.
            float z=exit?hinge.position.z-.08f-BodyClearanceRadius-.25f:
                hinge.position.z+.78f*hinge.lossyScale.x+.08f+BodyClearanceRadius+.25f;
            return new Vector3(insideWait.x,groundHeight,z);
        }
        bool ClearOfSweep(){
            // Conservative horizontal swept-leaf box, expanded by an authored
            // resident envelope (head/ears/backpack included at reset).
            var h=hinge.position;float radius=.78f*hinge.lossyScale.x;
            Vector3 p=resident.transform.position;
            return p.z+BodyClearanceRadius<h.z-.08f || p.z-BodyClearanceRadius>h.z+radius+.08f;
        }
        void Begin(Step step){
            if((step==Step.CrossOut||step==Step.CrossIn)&&(Door!=DoorState.Open||Mathf.Abs(DoorAngle-doorOpenAngle)>.01f))EarlyPassSamples++;
            if(step==Step.WalkToDestination&&Door!=DoorState.Closed)PrematureDepartureSamples++;
            Current=step;Clock=0;navPhase=0;actionTime=0;usingWalk=false;stopping=false;
            footwork.Capture();
        }
        void Finish(Step next){
            Current=next;Clock=waitClock=0;Trips++;usingWalk=false;footwork.Capture();
            if(pending.HasValue){bool requested=pending.Value;pending=null;Request(requested);}
        }
        void StartTurn(float targetYaw){
            float difference=Mathf.Abs(Mathf.DeltaAngle(resident.transform.eulerAngles.y,targetYaw));
            TurnSteps=difference<=5?0:difference<=90?2:3;
            MaxTurnSteps=Mathf.Max(MaxTurnSteps,TurnSteps);
            if(TurnSteps>0)footwork.BeginTurn(targetYaw,TurnSteps,.4f);
            actionTime=0;
        }
        bool Rotate(float targetYaw,float dt){
            if(navPhase==0){StartTurn(targetYaw);navPhase=1;}
            if(TurnSteps==0)return true;
            actionTime+=dt;footwork.PlaceRoot(actionTime);
            SampleBase(0,0);SmallArmSwing(actionTime,footwork.Duration);
            footwork.Apply(actionTime);return actionTime>=footwork.Duration;
        }
        void SmallArmSwing(float time,float duration){
            float weight=Mathf.Sin(Mathf.PI*Mathf.Clamp01(time/duration));
            foreach(var bone in poseBones){
                if(bone.name=="UpperArm_L"||bone.name=="UpperArm_R"){
                    float side=bone.name.EndsWith("L")?1:-1;
                    bone.localRotation*=Quaternion.AngleAxis(side*8*weight*Mathf.Sin(time*Mathf.PI/.4f),Vector3.right);
                }
            }
        }
        bool Walk(Vector3 target,float dt){
            if(navPhase==0){
                feet=new AdultRabbitFootTransition(resident,groundHeight){FollowWalkingHeading=true};
                var direction=target-resident.transform.position;direction.y=0;
                walkingRoute=new RabbitWalkingRoute(resident.transform,target,Yaw(direction));
                feet.WalkingPoseAhead=distance=>walkingRoute.Predict(distance,feet.Speed>.1f?feet.Speed:.6f);
                feet.WalkingSpeedScale=walkingRoute.SpeedScale;
                feet.WalkingGoal=walkingRoute.Goal;
                feet.Request(true);
                travelDirection=resident.transform.forward;navPhase=1;stopping=false;
            }
            usingWalk=true;
            float remaining=walkingRoute.Remaining;
            if(!stopping&&remaining<feet.EstimatedStopTravel+.14f){stopping=true;feet.WalkingPoseAhead=null;feet.Request(false);}
            feet.WalkingSpeedScale=walkingRoute.SpeedScale;
            float moved=feet.Step(dt);var pose=walkingRoute.Advance(moved,dt);resident.transform.SetPositionAndRotation(pose.position,pose.rotation);TotalTravel+=Mathf.Abs(moved);
            if(moved<0)ReverseTravel-=moved;
            feet.RestoreSourcePose();SampleBase(feet.Weight,(float)(feet.WalkTime%walkClip.length));
            Vector3 sourcePelvis=pelvis.position;feet.Apply();
            if(feet.State==AdultRabbitFootTransition.Stage.Closing||feet.State==AdultRabbitFootTransition.Stage.Settling)walkingRoute.StopTurning();
            if(stopping&&feet.State==AdultRabbitFootTransition.Stage.Idle){
                // Keep the landed balance offset continuous while switching to
                // the planted idle solver; release it over the closing wait.
                restRecoveryOffset=pelvis.position-sourcePelvis+Vector3.up*footwork.RestingPelvisDrop;
                restRecoveryTime=0;usingWalk=false;footwork.Capture();navPhase=0;return true;
            }
            return false;
        }
        void Stand(){SampleBase(0,0);footwork.Hold();}
        void StartOpening(){
            Door=DoorState.Waiting;doorClock=0;
            Begin(exiting?Step.OpenInside:Step.OpenOutside);
        }
        void UpdateDoor(float dt){
            switch(Door){
                case DoorState.Waiting:
                    doorClock+=dt;if(doorClock>=.2f){Door=DoorState.Opening;doorClock=0;}break;
                case DoorState.Opening:
                    doorClock+=dt;SetDoor(doorOpenAngle*Ease(doorClock/.7f));
                    if(doorClock>=.7f){SetDoor(doorOpenAngle);Door=DoorState.Open;doorClock=0;}break;
                case DoorState.Open:
                    if(Current==Step.WaitForClosing&&feet.State==AdultRabbitFootTransition.Stage.Idle&&
                       feet.BothFeetGrounded&&ClearOfSweep()){
                        Door=DoorState.ClosingDelay;doorClock=0;
                    }break;
                case DoorState.ClosingDelay:
                    if(!ClearOfSweep()){doorClock=0;break;}
                    doorClock+=dt;if(doorClock>=.2f){Door=DoorState.Closing;doorClock=0;}break;
                case DoorState.Closing:
                    if(!ClearOfSweep())break; // Freeze rather than close through an occupant.
                    doorClock+=dt;SetDoor(doorOpenAngle*(1-Ease(doorClock/.7f)));
                    if(doorClock>=.7f){SetDoor(0);Door=DoorState.Closed;}break;
            }
            DoorClock=doorClock;
        }
        void SampleBase(float weight,float walkTime){
            for(int i=0;i<poseBones.Length;i++){poseBones[i].localPosition=basePositions[i];poseBones[i].localRotation=baseRotations[i];}
            idlePlayable.SetTime(idleTime%idleClip.length);walkPlayable.SetTime(walkTime%walkClip.length);
            mixer.SetInputWeight(0,1-weight);mixer.SetInputWeight(1,weight);graph.Evaluate(0);
            pelvis.position+=restRecoveryOffset*(1-Ease(restRecoveryTime/.15f));
        }
        AdultFaceMotion faceMotion;
        public int blinkSeed=2701;
        AdultFaceMotion FaceMotion=>faceMotion??(faceMotion=new AdultFaceMotion(resident,blinkSeed));
        public double AutomaticBlinkClock=>FaceMotion.Clock;
        void LateUpdate(){FaceMotion.Apply();}
        public void Advance(float seconds){
            FaceMotion.Advance(paused?0:Math.Max(0,seconds)*(slow?.5:1),false);
            try{AdvanceBody(seconds);}finally{FaceMotion.Apply();}
        }
        void AdvanceBody(float seconds){
            if(FlowMode){lifeFlow.Advance(seconds);return;}
            if(BenchMode){benchRest.Advance(seconds);return;}
            if(WaterMode){watering.Advance(seconds);return;}
            if(paused||seconds<=0)return;
            float elapsed=seconds*(slow?.5f:1);
            remainder+=elapsed;
            while(remainder>=1f/240f){remainder-=1f/240f;Tick(1f/240f);}
        }
        void Tick(float dt){
            Clock+=dt;idleTime+=dt;restRecoveryTime+=dt;
            switch(Current){
                case Step.Inside:case Step.Outside:
                    Stand();if(automatic){waitClock+=dt;if(waitClock>=5)Request(Inside);}break;
                case Step.ApproachInside:
                    if(Rotate(180,dt))StartOpening();break;
                case Step.ApproachOutside:
                    if(Walk(outsideDoor,dt))StartOpening();break;
                case Step.WalkToRoom:
                    if(Walk(outsideDoor,dt))StartOpening();break;
                case Step.OpenInside:case Step.OpenOutside:
                    Stand();if(Door==DoorState.Open)Begin(exiting?Step.CrossOut:Step.CrossIn);break;
                case Step.CrossOut:case Step.CrossIn:case Step.PassDeceleration:
                    bool stopped=Walk(ClosingWaitTarget(exiting),dt);
                    if(stopping)Current=Step.PassDeceleration;
                    if(stopped){
                        closingPosition=resident.transform.position;closingRotation=resident.transform.rotation;
                        ClosingStops++;Begin(Step.WaitForClosing);
                    }
                    break;
                case Step.WaitForClosing:
                    Stand();
                    MaximumClosingDrift=Mathf.Max(MaximumClosingDrift,Vector3.Distance(closingPosition,resident.transform.position));
                    if(Quaternion.Angle(closingRotation,resident.transform.rotation)>.01f)PrematureDepartureSamples++;
                    if(Door==DoorState.Closed){
                        // The connected routine's next destination is the can,
                        // not an extra outside waypoint followed by another stop.
                        if(FlowMode&&exiting)Finish(Step.Outside);else Begin(Step.WalkToDestination);
                    }
                    break;
                case Step.WalkToDestination:
                    if(Walk(exiting?outsideWait:insideWait,dt))Finish(exiting?Step.Outside:Step.Inside);
                    break;
            }
            float previousDoorAngle=DoorAngle;UpdateDoor(dt);
            if((Door==DoorState.ClosingDelay||Door==DoorState.Closing)&&Current!=Step.WaitForClosing)PrematureDepartureSamples++;
            MaximumLateralDeviation=Mathf.Max(MaximumLateralDeviation,Mathf.Abs(resident.transform.position.x-insideWait.x));
            if(Mathf.Abs(previousDoorAngle-DoorAngle)>.000001f&&!ClearOfSweep())UnsafeDoorSamples++;
            for(int i=0;i<2;i++){
                MinimumSole=Mathf.Min(MinimumSole,footwork.SoleHeight(i));
                bool planted=usingWalk?(i==0?feet.LeftPlanted:feet.RightPlanted):footwork.Planted[i];
                if(planted)MaximumSupportGap=Mathf.Max(MaximumSupportGap,footwork.SoleHeight(i));
                Vector3 p=footwork.FootPosition(i);
                if(planted){if(wasPlanted[i]){float drift=Vector3.Distance(lastSupport[i],p);if(drift>MaximumSupportDrift){MaximumSupportDrift=drift;SupportDetail=$"{Current} nav {navPhase} door {Door} t {actionTime:F3} foot {i} walk {usingWalk} actor {resident.transform.position:F3} anchor {lastSupport[i]:F3} actual {p:F3}";}}else lastSupport[i]=p;}
                wasPlanted[i]=planted;
            }
        }
        void Update(){if(smoke)return;CameraInput();Advance(Time.unscaledDeltaTime);ApplyCamera();}
        public void Home(){pivot=new Vector3(0,1.3f,-1.7f);yaw=155;pitch=18;distance=8;drag=-1;if(reviewCamera)reviewCamera.fieldOfView=40;ApplyCamera();}
        public void View(float y){pivot=new Vector3(0,1.3f,-1.8f);yaw=y;pitch=8;distance=7;if(reviewCamera)reviewCamera.fieldOfView=40;ApplyCamera();}
        public void ViewInside(){pivot=new Vector3(-.45f,1f,-.7f);yaw=-30;pitch=25;distance=2.8f;if(reviewCamera)reviewCamera.fieldOfView=65;ApplyCamera();}
        void ApplyCamera(){if(!reviewCamera)return;reviewCamera.transform.rotation=Quaternion.Euler(pitch,180+yaw,0);reviewCamera.transform.position=pivot-reviewCamera.transform.forward*distance;}
        void CameraInput(){
            if(Input.GetKeyDown(KeyCode.Home)){Home();previous=Input.mousePosition;return;}
            Vector2 point=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            bool ui=PointerInPanel(point,Screen.width,Screen.height);
            Vector3 delta=Input.mousePosition-previous;previous=Input.mousePosition;
            if(ui)drag=-1;
            for(int b=0;b<3;b++)if(Input.GetMouseButtonDown(b))drag=ui?-1:b;
            if(drag>=0&&!Input.GetMouseButton(drag))drag=-1;
            if(drag==0)pivot+=FarmStudyReview.ScreenPan(reviewCamera.transform.rotation,delta,distance,reviewCamera.fieldOfView,Screen.height);
            if(drag==1){yaw+=delta.x*.16f;pitch=Mathf.Clamp(pitch-delta.y*.16f,-89,75);}
            if(drag==2)pivot+=Vector3.up*FarmStudyReview.MouseHeightDelta(delta.y,distance,reviewCamera.fieldOfView,Screen.height);
            if(!ui&&Input.mouseScrollDelta.y!=0)distance=Mathf.Clamp(distance*Mathf.Exp(-Input.mouseScrollDelta.y*.12f),1,14);
            if(!ui)pivot.y+=((Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0))*2*Time.unscaledDeltaTime;
        }
        void OnGUI(){
            if(!resident)return;
            if(buttonStyle==null){buttonStyle=new GUIStyle(GUI.skin.button){font=font,fontSize=13,fixedHeight=24};labelStyle=new GUIStyle(GUI.skin.label){font=font,fontSize=13,wordWrap=true};}
            var panel=Panel;GUI.Box(panel,GUIContent.none);
            var viewport=new Rect(panel.x+8,panel.y+8,panel.width-16,panel.height-16);
            panelScroll=GUI.BeginScrollView(viewport,panelScroll,new Rect(0,0,viewport.width-18,FlowMode?800:WaterMode||BenchMode?710:560),false,false);
            float y=0,w=viewport.width-20;
            GUI.Label(new Rect(0,y,w,24),"기본 호흡 · 눈 깜빡임 자동",labelStyle);y+=26;
            GUI.Label(new Rect(0,y,w,38),"토끼 집 · 외출/귀가 검토",labelStyle);y+=35;
            if(watering){
                if(GUI.Button(new Rect(0,y,w/2-3,24),"출입 검토",buttonStyle))SelectWaterMode(false);
                if(GUI.Button(new Rect(w/2+3,y,w/2-3,24),"물 주기 검토",buttonStyle))SelectWaterMode(true);y+=29;
            }
            if(benchRest){if(GUI.Button(new Rect(0,y,w,24),"벤치 휴식 검토",buttonStyle))SelectBenchMode();y+=29;}
            if(lifeFlow){if(GUI.Button(new Rect(0,y,w,24),"생활 흐름 · 외출해서 물 주기",buttonStyle))SelectLifeFlow();y+=29;}
            if(FlowMode){lifeFlow.DrawPanel(ref y,w,buttonStyle,labelStyle);GUI.EndScrollView();return;}
            if(BenchMode){
                GUI.Label(new Rect(0,y,w,48),$"{benchRest.Label} · {benchRest.TimeInState:F1}초\n{(paused?"일시정지":"재생")} · {(slow?"0.5×":"1×")} · 이동 예약 {(benchRest.PendingWalk?"있음":"없음")}",labelStyle);y+=48;
                if(GUI.Button(new Rect(0,y,w,24),"전체 흐름 · 벤치 휴식",buttonStyle)){benchRest.StartFlow();paused=false;}y+=29;
                if(GUI.Button(new Rect(0,y,w/2-3,24),"앉기",buttonStyle)){benchRest.RequestSit();paused=false;}
                if(GUI.Button(new Rect(w/2+3,y,w/2-3,24),"일어서기",buttonStyle)){benchRest.RequestRise();paused=false;}y+=29;
                if(GUI.Button(new Rect(0,y,w/2-3,24),"걷기 재개",buttonStyle)){benchRest.RequestWalk();paused=false;}
                if(GUI.Button(new Rect(w/2+3,y,w/2-3,24),"정지 / 예약 취소",buttonStyle))benchRest.StopTask();y+=29;
                if(GUI.Button(new Rect(0,y,w,24),"처음 위치",buttonStyle)){benchRest.ResetTask();paused=false;}y+=29;
                if(GUI.Button(new Rect(0,y,w/3-3,24),paused?"재개":"일시정지",buttonStyle))paused=!paused;
                if(GUI.Button(new Rect(w/3,y,w/3-3,24),"0.5×",buttonStyle))slow=true;
                if(GUI.Button(new Rect(2*w/3,y,w/3-3,24),"1×",buttonStyle))slow=false;y+=29;
                if(GUI.Button(new Rect(0,y,w/3-3,24),"정면",buttonStyle))ViewBench(180);
                if(GUI.Button(new Rect(w/3,y,w/3-3,24),"측면",buttonStyle))ViewBench(90);
                if(GUI.Button(new Rect(2*w/3,y,w/3-3,24),"비스듬",buttonStyle))ViewBench(140);y+=29;
                GUI.Label(new Rect(0,y,w,24),"주요 8포즈",labelStyle);y+=26;
                for(int i=0;i<3;i++)if(GUI.Button(new Rect(i*w/3,y,w/3-3,24),(benchPoseAction==i?"● ":"")+new[]{"앉기","호흡","서기"}[i],buttonStyle))benchPoseAction=i;y+=29;
                for(int i=0;i<8;i++)if(GUI.Button(new Rect((i%4)*w/4,y+(i/4)*29,w/4-3,24),(i+1).ToString(),buttonStyle))benchRest.Pose(benchPoseAction,i);y+=62;
                GUI.Label(new Rect(0,y,w,80),"접근 → 앉기 → 6초 휴식 → 서기 → 걷기\n배낭 착용 · 호흡 자동 적용\n마우스 패닝/회전/줌 · Q/E 높이",labelStyle);
                GUI.EndScrollView();return;
            }
            if(WaterMode){
                GUI.Label(new Rect(0,y,w,48),$"{watering.Label} · {watering.TimeInState:F1}초\n{(paused?"일시정지":"재생")} · {(slow?"0.5×":"1×")}",labelStyle);y+=48;
                if(GUI.Button(new Rect(0,y,w,24),"전체 흐름 · 화단 물 주기",buttonStyle)){watering.StartWalk(true);paused=false;}y+=29;
                if(GUI.Button(new Rect(0,y,w/2-3,24),"운반 대기 / 초기화",buttonStyle)){watering.ResetTask();paused=false;}
                if(GUI.Button(new Rect(w/2+3,y,w/2-3,24),"운반 걷기",buttonStyle)){watering.StartWalk(false);paused=false;}y+=29;
                if(GUI.Button(new Rect(0,y,w/2-3,24),"물 주기",buttonStyle)){watering.StartPour();paused=false;}
                if(GUI.Button(new Rect(w/2+3,y,w/2-3,24),"정지 / 물 끊기",buttonStyle)){watering.StopTask();paused=false;}y+=29;
                if(GUI.Button(new Rect(0,y,w/3-3,24),paused?"재개":"일시정지",buttonStyle))paused=!paused;
                if(GUI.Button(new Rect(w/3,y,w/3-3,24),"0.5×",buttonStyle))slow=true;
                if(GUI.Button(new Rect(2*w/3,y,w/3-3,24),"1×",buttonStyle))slow=false;y+=29;
                if(GUI.Button(new Rect(0,y,w/3-3,24),"정면",buttonStyle))ViewWater(90);
                if(GUI.Button(new Rect(w/3,y,w/3-3,24),"측면",buttonStyle))ViewWater(180);
                if(GUI.Button(new Rect(2*w/3,y,w/3-3,24),"비스듬",buttonStyle))ViewWater(130);y+=29;
                GUI.Label(new Rect(0,y,w,24),"주요 8포즈 · 대기 / 걷기 / 물 주기",labelStyle);y+=26;
                for(int i=0;i<3;i++)if(GUI.Button(new Rect(i*w/3,y,w/3-3,24),(waterPoseAction==i?"● ":"")+new[]{"대기","걷기","물 주기"}[i],buttonStyle))waterPoseAction=i;y+=29;
                for(int i=0;i<8;i++)if(GUI.Button(new Rect((i%4)*w/4,y+(i/4)*29,w/4-3,24),(i+1).ToString(),buttonStyle))watering.Pose(waterPoseAction,i);y+=62;
                GUI.Label(new Rect(0,y,w,80),"양손 운반 → 감속 → 물 주기\n집기·내려놓기는 후속 작업\n마우스 패닝/회전/줌 · Q/E 높이",labelStyle);
                GUI.EndScrollView();return;
            }
            GUI.Label(new Rect(0,y,w,55),$"{MovementLabel} · 예약 {Pending}\n자동문 {DoorLabel} {DoorAngle:F0}° · {(paused?"일시정지":"재생")}",labelStyle);y+=53;
            if(GUI.Button(new Rect(0,y,w/2-3,24),"외출",buttonStyle))RequestExit();
            if(GUI.Button(new Rect(w/2+3,y,w/2-3,24),"귀가",buttonStyle))RequestEnter();y+=29;
            if(GUI.Button(new Rect(0,y,w,24),(automatic?"● ":"")+"자동 왕복 (5초 대기)",buttonStyle))automatic=!automatic;y+=29;
            if(GUI.Button(new Rect(0,y,w/3-3,24),paused?"재개":"일시정지",buttonStyle))paused=!paused;
            if(GUI.Button(new Rect(w/3,y,w/3-3,24),"0.5×",buttonStyle))slow=true;
            if(GUI.Button(new Rect(2*w/3,y,w/3-3,24),"1×",buttonStyle))slow=false;y+=29;
            if(GUI.Button(new Rect(0,y,w,24),"처음 위치 / 문 닫기",buttonStyle))ResetStudy();y+=32;
            if(GUI.Button(new Rect(0,y,w/3-3,24),"정면",buttonStyle))View(180);
            if(GUI.Button(new Rect(w/3,y,w/3-3,24),"측면",buttonStyle))View(90);
            if(GUI.Button(new Rect(2*w/3,y,w/3-3,24),"실내",buttonStyle))ViewInside();y+=31;
            GUI.Label(new Rect(0,y,w,90),"왼쪽 드래그 패닝 · 오른쪽 회전\n가운데 높이 · 휠 줌 · Q/E 높이\nHome 기본 구도",labelStyle);
            GUI.EndScrollView();
        }
    }
}
