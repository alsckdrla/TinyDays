using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TinyDays.Review
{
    public sealed partial class AdultRabbitMotionReview : MonoBehaviour
    {
        public GameObject resident; public Camera reviewCamera; public AnimationClip[] clips;
        public int selected; public bool paused,slow; double elapsed;
        public double Elapsed => elapsed;
        public static bool IsSitDown(int clip)=>clip==5||clip==10;
        public static bool IsStandUp(int clip)=>clip==6||clip==11;
        public static bool IsBreathing(int clip)=>clip==8||clip==9;
        public static bool IsSigh(int clip)=>clip==12||clip==13;
        public static bool IsVariation(int clip)=>AdultIdleVariations.Find(clip)!=null;
        public static bool IsOneShot(int clip)=>IsSitDown(clip)||IsStandUp(clip)||clip==7||IsVariation(clip)||(IsLyingClip(clip)&&!IsSleepLoop(clip));
        public int DisplayClip => Idle!=null&&Idle.IsVariation?Array.IndexOf(clips,Idle.CurrentClip):selected;
        public bool SupportedSitting=true;
        public int SitClip => SupportedSitting?10:5;
        public int StandClip => SupportedSitting?11:6;
        public bool CanChangeSitStyle => !SitTransition&&!LyingTransition&&!MovingReview&&(Idle==null||Idle.State==CommonIdleDirector.Stage.Breathing);
        public bool SetSitStyle(bool supported){
            if(!CanChangeSitStyle)return false;
            if(SupportedSitting==supported)return true;
            bool breathing=Idle!=null,seated=Idle?.Seated??false;double time=Idle?.Time??0;
            bool wasPaused=paused;SupportedSitting=supported;
            if(breathing){StartBreathing(seated);AdvanceIdle((float)time);paused=wasPaused;}
            return true;
        }
        double DisplayTime {get{double t=IsSleepClip(selected)?SleepTime(elapsed):elapsed;return IsOneShot(selected)?Math.Min(t,clips[selected].length):t%clips[selected].length;}}
        public IdleSecondaryMotion idleProfile=new IdleSecondaryMotion();
        public CommonIdleDirector Idle {get;private set;}
        public bool AutomaticIdle=true;
        bool followBreathing;
        double idleCoatClock;
        public bool PlaySigh(){return Idle!=null&&Idle.Play(Idle.Seated?"앉아서 한숨":"서서 한숨");}
        public IdleLiftRecovery LiftRecovery {get;private set;}
        public void StartBreathing(bool seated){
            idleProfile.RestoreFidget();
            var position=resident.transform.position;var oldPivot=pivot;float oldYaw=yaw,oldPitch=pitch,oldDistance=distance;
            Select(seated?9:8);resident.transform.position=position;pivot=oldPivot;yaw=oldYaw;pitch=oldPitch;distance=oldDistance;ApplyCamera();
            if(graph.IsValid())graph.Destroy();active=-1;resident.GetComponent<Animator>().enabled=false;
            // Recover to the exact grounded locomotion entry pose, not the raw
            // legacy idle which sits about 1mm below this review floor.
            clips[0].SampleAnimation(resident,0);var entry=new AdultRabbitFootTransition(resident);entry.Apply();
            var joints=resident.GetComponentsInChildren<Transform>();var positions=new Vector3[joints.Length];var rotations=new Quaternion[joints.Length];
            for(int i=0;i<joints.Length;i++){positions[i]=joints[i].localPosition;rotations[i]=joints[i].localRotation;}
            Action recovery=()=>{for(int i=1;i<joints.Length;i++){joints[i].localPosition=positions[i];joints[i].localRotation=rotations[i];}};
            Idle=new CommonIdleDirector(resident,clips[8],clips[9],clips[StandClip],clips[8],clips[7],seated,173,recovery,idleProfile.Supports,idleProfile.Apply);Idle.Automatic=AutomaticIdle;
            AdultIdleVariations.ConfigureHeels(resident,idleProfile.Supports);
            LiftRecovery=clips.Length>15?new IdleLiftRecovery(resident,clips[15],idleProfile.Supports):null;
            var handRecovery=clips.Length>16?new IdleHandRecovery(resident,clips[16],Array.Find(joints,b=>b.name=="UpperArm_R"),Array.Find(joints,b=>b.name=="Forearm_R"),Array.Find(joints,b=>b.name=="Hand_R")):null;
            foreach(var definition in AdultIdleVariations.All){
                if(definition.Index>=clips.Length)continue;
                var d=definition;bool fidget=d.Index>=14;
                bool lifted=d.Index==15;
                Idle.Register(new CommonIdleDirector.Variation{Id=d.Label,Seated=d.Seated,Clip=clips[d.Index],HeelSupport=fidget,LiftsFeet=lifted,
                    BeginRecovery=lifted?(Action<double>)LiftRecovery.Begin:d.Index==16?(Action<double>)handRecovery.Begin:null,RecoveryDuration=lifted?(Func<float>)(()=>LiftRecovery.Duration):d.Index==16?(Func<float>)(()=>IdleHandRecovery.Duration):null,RecoverFeet=lifted?(Action<float>)LiftRecovery.Apply:null,
                    RecoverPose=d.Index==16?(Action<float>)handRecovery.Apply:null,
                    FeetSupported=lifted?(Func<bool>)LiftRecovery.Supported:fidget?(Func<bool>)(()=>AdultIdleVariations.Supported(resident,idleProfile.Supports)):null,
                    Secondary=fidget?(Action<double>)(t=>AdultIdleVariations.Secondary(idleProfile,t,clips[d.Index].length)):idleProfile.ApplySigh});
            }
            idleCoatClock=0;
            followBreathing=true;paused=false;
        }
        void AdvanceIdle(float dt){
            idleProfile.RestoreFidget();
            idleCoatClock+=dt;
            Idle.Automatic=AutomaticIdle&&!PendingLie;Idle.Advance(dt);
            if(AdvanceLying())return;
            if(Idle.State==CommonIdleDirector.Stage.Ready){var request=Idle.Pending;float remaining=(float)Idle.UnusedTime;Idle=null;BeginMovement(false);WantsToWalk=true;footTransition.RequestRun(request==CommonIdleDirector.Departure.Run);if(remaining>0)Advance(remaining/(slow?.5f:1));return;}
            sitCoat?.Apply(Idle.State==CommonIdleDirector.Stage.Rising?StandClip:Idle.Seated?9:8,Idle.State==CommonIdleDirector.Stage.Rising?Idle.Time:idleCoatClock,Idle.State==CommonIdleDirector.Stage.Rising?clips[StandClip].length:4);
        }
        public bool MovingReview {get;private set;}
        public float MoveWeight {get;private set;}
        public bool WantsToWalk {get;private set;}
        public float Travel {get;private set;}
        AdultRabbitFootTransition footTransition;
        AdultRabbitCoatClearance coatClearance;
        AdultRabbitSitCoat sitCoat;
        bool pendingSit;
        public string SitState => pendingSit?"정지 후 앉기 대기":IsSitDown(selected)?(SitTransition?"앉는 중":"앉음"):IsStandUp(selected)?(SitTransition?"일어나는 중":"서기"):selected==7?"앉음":"서기";
        public bool SitTransition => (IsSitDown(selected)||IsStandUp(selected))&&elapsed+1e-5<clips[selected].length;
        public void RequestSit(){if(IsLyingClip(selected)){RequestLyingReturn(true);return;}if(Idle!=null){if(Idle.Seated||Idle.State!=CommonIdleDirector.Stage.Breathing)return;Select(SitClip);followBreathing=true;paused=false;return;}if(SitTransition||selected==7||IsSitDown(selected))return;if(MovingReview){pendingSit=true;RequestWalk(false);return;}Select(SitClip);paused=false;}
        public void RequestStand(){if(IsLyingClip(selected)){RequestLyingReturn(false);return;}if(Idle!=null){Idle.Rise();return;}if(selected==9){StartBreathing(true);Idle.Rise();return;}if(SitTransition||!(IsSitDown(selected)||selected==7))return;Select(StandClip);paused=false;}
        public float CoatDisplacement => coatClearance?.MaxDisplacement??0;
        public AdultRabbitFootTransition FootTransition => footTransition;
        public float Speed => MovingReview&&footTransition!=null?footTransition.Speed:0;
        double walkTime,idleTime,movementRemainder;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable idlePlayable,walkPlayable,runPlayable;
        Vector3 pivot=new Vector3(0,1,0),previous;
        float yaw=32.4f,pitch=7.4f,distance=6.21f;
        int drag=-1;
        public static Rect PanelBounds(float width,float height)=>new Rect(12,12,Mathf.Max(1,Mathf.Min(300,width-24)),Mathf.Max(1,height-24));
        Rect Panel => PanelBounds(Screen.width,Screen.height);
        public static bool IsPanelPoint(Vector2 point,float width,float height)=>PanelBounds(width,height).Contains(point);
        Vector2 panelScroll;
        GUIStyle panelButton,panelLabel,panelHeading;
        float panelX,panelY,panelWidth;bool panelDraw;
        public float PanelContentHeight {get;private set;}
        public struct PanelItem {public string Text;public Rect Bounds;public bool Button;public Action Click;}
        public readonly System.Collections.Generic.List<PanelItem> PanelItems=new System.Collections.Generic.List<PanelItem>();
        public float PanelScrollY {get=>panelScroll.y;set=>panelScroll.y=value;}
        PlayableGraph graph; AnimationClipPlayable playable; int active=-1; Font font;
        static readonly string[] Labels={"두 발 대기","두 발 총총걸음","네 발 대기 (보류)","깡충 (보류)","두 발 달리기","앉기","일어서기","앉은 자세"};
        void OnEnable(){faceMotion=null;if(!Application.isPlaying)return;panelButton=panelLabel=panelHeading=null;font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",13);if(resident&&clips!=null&&clips.Length>=10)StartBreathing(false);else Sample(0);}
        void OnDisable(){faceMotion?.Clear();faceMotion=null;ResetLying();idleProfile.RestoreFidget();panelButton=panelLabel=panelHeading=null;panelScroll=Vector2.zero;PanelItems.Clear();Idle=null;followBreathing=false;sitCoat?.Dispose();sitCoat=null;pendingSit=false;coatClearance?.Dispose();coatClearance=null;drag=-1;active=-1;MovingReview=false;MoveWeight=0;WantsToWalk=false;footTransition?.RestoreSourcePose();footTransition=null;if(resident)resident.transform.localPosition=Vector3.zero;selected=0;elapsed=0;if(graph.IsValid())graph.Destroy();if(font)Destroy(font);}
        bool cameraFocused=true;
        void OnApplicationFocus(bool focus){cameraFocused=focus;if(!focus)drag=-1;}
        void Update(){HandleCamera();Advance(Time.unscaledDeltaTime);}
        AdultFaceMotion faceMotion;
        public int blinkSeed=1701;
        int faceAdvanceDepth;
        AdultFaceMotion FaceMotion=>faceMotion??(faceMotion=new AdultFaceMotion(resident,blinkSeed));
        public double AutomaticBlinkClock=>FaceMotion.Clock;
        public float AutomaticBlinkWeight=>FaceMotion.Blink;
        void LateUpdate(){FaceMotion.Apply(SleepEyeWeight);}
        public void Advance(float seconds){
            bool outer=faceAdvanceDepth++==0;
            if(outer)FaceMotion.Advance(paused?0:Math.Max(0,seconds)*(slow?.5:1),FallingAsleep(selected)||Asleep(selected)||Waking(selected));
            try{AdvanceBody(seconds);}finally{faceAdvanceDepth--;if(outer)FaceMotion.Apply(SleepEyeWeight);}
        }
        void AdvanceBody(float seconds){
            float dt=paused?0:Mathf.Max(0,seconds)*(slow?.5f:1);
            elapsed+=dt;
            AdvanceRestBreath(dt);
            if(Idle!=null){AdvanceIdle(dt);return;}
            if(IsSideClip(selected)){AdvanceSide();return;}
            if(IsSleepClip(selected)){AdvanceSleep();return;}
            if(IsLyingClip(selected)){if(sleepBlend)ApplySleepFrame(elapsed);else Sample(elapsed);AdvanceLying();return;}
            if(!MovingReview){Sample(elapsed);if(AdvanceLying())return;if(followBreathing&&!SitTransition&&(IsSitDown(selected)||IsStandUp(selected)))StartBreathing(IsSitDown(selected));return;}
            // Small deterministic integration steps keep speed, phase and travel synchronized.
            movementRemainder+=dt;
            while(movementRemainder+1e-6>=1.0/240){float step=1f/240f;movementRemainder-=1.0/240;
                if(Travel>=(footTransition.RunWeight>.01f?4.8f:5.25f)&&WantsToWalk)RequestWalk(false);
                float distanceStep=footTransition.Step(step);
                walkTime=footTransition.WalkTime;MoveWeight=footTransition.Weight;idleTime+=step;Travel+=distanceStep;
                var delta=resident.transform.forward*distanceStep;resident.transform.position+=delta;pivot+=delta;
                EvaluateMovement();
            }
            if(paused||seconds<=0)EvaluateMovement();coatClearance?.Apply(dt);ApplyCamera();
            if(AdvanceLying())return;
            if(pendingSit&&footTransition.State==AdultRabbitFootTransition.Stage.Idle){var position=resident.transform.position;bool follow=followBreathing;Select(SitClip);followBreathing=follow;resident.transform.position=position;Home();paused=false;}
        }
        void EvaluateMovement(){
            footTransition?.RestoreSourcePose();
            idlePlayable.SetTime(idleTime%clips[0].length);walkPlayable.SetTime(walkTime%clips[1].length);
            float run=footTransition?.RunWeight??0;runPlayable.SetTime((walkTime/.8%1)*clips[4].length);
            mixer.SetInputWeight(0,1-MoveWeight);mixer.SetInputWeight(1,MoveWeight*(1-run));mixer.SetInputWeight(2,MoveWeight*run);graph.Evaluate(0);footTransition?.Apply();
        }
        public void BeginMovement(){BeginMovement(true);}
        void BeginMovement(bool resetPosition){
            var position=resident.transform.position;Select(0);if(resetPosition){resident.transform.localPosition=Vector3.zero;Home();}else resident.transform.position=position;MovingReview=true;paused=false;MoveWeight=0;Travel=0;walkTime=idleTime=movementRemainder=0;WantsToWalk=false;
            if(graph.IsValid())graph.Destroy();graph=PlayableGraph.Create("Adult movement transition");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            idlePlayable=AnimationClipPlayable.Create(graph,clips[0]);walkPlayable=AnimationClipPlayable.Create(graph,clips[1]);
            runPlayable=AnimationClipPlayable.Create(graph,clips[4]);
            mixer=AnimationMixerPlayable.Create(graph,3);graph.Connect(idlePlayable,0,mixer,0);graph.Connect(walkPlayable,0,mixer,1);graph.Connect(runPlayable,0,mixer,2);
            var animator=resident.GetComponent<Animator>();animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var output=AnimationPlayableOutput.Create(graph,"Locomotion",animator);output.SetSourcePlayable(mixer);graph.Play();footTransition=null;EvaluateMovement();coatClearance=new AdultRabbitCoatClearance(resident);footTransition=new AdultRabbitFootTransition(resident);Advance(0);
        }
        public void RequestWalk(bool walk){if(HandleLyingMovement(walk,false))return;if(Idle!=null){if(walk)Idle.Request(CommonIdleDirector.Departure.Walk);else Idle.Cancel();return;}if(walk&&(IsBreathing(selected)||IsVariation(selected)||IsSitDown(selected)||selected==7)){RequestRun(false);return;}if(!MovingReview)BeginMovement();WantsToWalk=walk;footTransition.Request(walk);}
        public void RequestRun(bool run){if(HandleLyingMovement(true,run))return;if(SitTransition)return;if(Idle!=null){Idle.Request(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk);return;}if(!MovingReview&&IsVariation(selected)){var d=AdultIdleVariations.Find(selected);float phase=(float)Math.Min(elapsed,clips[selected].length);StartBreathing(d.Seated);Idle.Play(d.Label);AdvanceIdle(CommonIdleDirector.BlendSeconds+phase);Idle.Request(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk);return;}if(!MovingReview&&IsBreathing(selected)){bool seated=selected==9;float phase=(float)elapsed;StartBreathing(seated);AdvanceIdle(phase);Idle.Request(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk);return;}if(!MovingReview&&(selected==7||IsSitDown(selected))){StartBreathing(true);Idle.Request(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk);return;}if(!MovingReview)BeginMovement();WantsToWalk=true;footTransition.RequestRun(run);}
        public void Home(){pivot=resident.transform.position+new Vector3(0,1,0);yaw=32.4f;pitch=7.4f;distance=6.21f;drag=-1;ApplyCamera();}
        public void View(float angle){Home();yaw=angle;pitch=4;ApplyCamera();}
        void ApplyCamera(){reviewCamera.transform.rotation=Quaternion.Euler(pitch,180+yaw,0);reviewCamera.transform.position=pivot-reviewCamera.transform.forward*distance;}
        public void CameraDrag(int button,Vector2 delta){
            if(button==FarmStudyReview.PanButton)pivot+=FarmStudyReview.ScreenPan(reviewCamera.transform.rotation,delta,distance,reviewCamera.fieldOfView,Screen.height);
            if(button==FarmStudyReview.RotateButton){yaw+=delta.x*.16f;pitch=Mathf.Clamp(pitch-delta.y*.16f,-89,75);}
            if(button==2)pivot+=Vector3.up*FarmStudyReview.MouseHeightDelta(delta.y,distance,reviewCamera.fieldOfView,Screen.height);
            ApplyCamera();}
        public void Zoom(float wheel){distance=Mathf.Clamp(distance*Mathf.Exp(wheel*.12f),.5f,12);ApplyCamera();}
        public bool CameraDragging => drag>=0;
        public void PointerInput(Vector2 point,Vector2 movement,int pressedMask,int heldMask,float wheel){
            bool ui=IsPanelPoint(point,Screen.width,Screen.height);
            if(ui)drag=-1;
            for(int b=0;b<3;b++)if((pressedMask&(1<<b))!=0&&!ui)drag=b;
            if(drag>=0&&(heldMask&(1<<drag))==0)drag=-1;
            if(drag>=0&&pressedMask==0)CameraDrag(drag,movement);
            if(!ui&&wheel!=0)Zoom(wheel);
        }
        void HandleCamera(){
            if(Input.GetKeyDown(KeyCode.Home)){Home();previous=Input.mousePosition;return;}
            Vector2 point=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            int pressed=0,held=0;for(int b=0;b<3;b++){if(Input.GetMouseButtonDown(b))pressed|=1<<b;if(Input.GetMouseButton(b))held|=1<<b;}
            Vector3 delta=Input.mousePosition-previous;previous=Input.mousePosition;
            PointerInput(point,new Vector2(delta.x,delta.y),pressed,held,Input.mouseScrollDelta.y);
            KeyboardMove(ReviewCameraKeys.Read(),Time.unscaledDeltaTime);
            KeyboardElevate(ReviewCameraKeys.ReadHeight(),Time.unscaledDeltaTime);
        }
        public void KeyboardMove(Vector2 input,float seconds){
            if(!reviewCamera||!ReviewCameraKeys.Allowed(cameraFocused,editing:ReviewCameraKeys.EditingText))return;
            ApplyCamera();
            pivot+=FarmStudyReview.KeyboardPan(reviewCamera.transform.rotation,input,distance)*seconds;ApplyCamera();
        }
        public void KeyboardElevate(float input,float seconds){
            if(!reviewCamera||!ReviewCameraKeys.Allowed(cameraFocused,editing:ReviewCameraKeys.EditingText))return;
            pivot+=Vector3.up*Mathf.Clamp(input,-1,1)*ReviewCameraKeys.Speed(distance)*seconds;ApplyCamera();
        }
        bool ShowingRun => selected==4||(MovingReview&&footTransition.RunWeight>.5f);
        static readonly float[] SitPhases={0,.125f,.32f,.52f,.6875f,.75f,.90f,1},StandPhases={0,.14f,.22f,.36f,.55f,.75f,.90f,1};
        public void Pose(int index){int clip=Idle!=null&&Idle.IsVariation?DisplayClip:selected>=5?selected:ShowingRun?4:1;Select(clip);paused=true;int phase=Mathf.Clamp(index,0,7);elapsed=IsVariation(clip)?AdultIdleVariations.Find(clip).Times[phase]:clips[clip].length*(IsSleepClip(clip)?SleepPosePhases[phase]:IsLyingClip(clip)?LyingPosePhases[phase]:IsSitDown(clip)?SitPhases[phase]:IsStandUp(clip)?StandPhases[phase]:clip==7?0:clip==4?AdultRunTiming.Phase(phase):phase/8.0);Sample(elapsed);}
        void Rebuild(){if(graph.IsValid())graph.Destroy();graph=PlayableGraph.Create("Adult rabbit motion");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var animator=resident.GetComponent<Animator>();animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            playable=AnimationClipPlayable.Create(graph,clips[selected]);var output=AnimationPlayableOutput.Create(graph,"Adult",animator);output.SetSourcePlayable(playable);graph.Play();active=selected;}
        public void Select(int index){ResetLying();Idle=null;followBreathing=false;sitCoat?.Dispose();sitCoat=null;pendingSit=false;coatClearance?.Dispose();coatClearance=null;footTransition?.RestoreSourcePose();footTransition=null;if(MovingReview){MovingReview=false;MoveWeight=0;WantsToWalk=false;Travel=0;resident.transform.localPosition=Vector3.zero;active=-1;Home();}selected=Mathf.Clamp(index,0,clips.Length-1);elapsed=0;if(selected==4){clips[0].SampleAnimation(resident,0);coatClearance=new AdultRabbitCoatClearance(resident);}if(selected>=5)sitCoat=new AdultRabbitSitCoat(resident);if(IsLyingClip(selected))HideBackpack();Sample(0);}
        public void Sample(double time){idleProfile.RestoreFidget();if(active!=selected||!graph.IsValid())Rebuild();playable.SetTime(IsOneShot(selected)?Math.Min(time,clips[selected].length):time%clips[selected].length);graph.Evaluate(0);if(IsSigh(selected))idleProfile.ApplySigh(Math.Min(time,5));if(selected>=14&&IsVariation(selected))AdultIdleVariations.Secondary(idleProfile,Math.Min(time,clips[selected].length),clips[selected].length);if(selected==4)coatClearance?.Apply(1f/60);SampleRestBreath(time);sitCoat?.Apply(selected,time,clips[selected].length);SampleSleep(time);}
        void PanelRow(){if(panelX>0){panelX=0;panelY+=28;}}
        void PanelText(string text,bool heading=false){
            PanelRow();var style=heading?panelHeading:panelLabel;float h=style.CalcHeight(new GUIContent(text),panelWidth);
            if(!panelDraw)PanelItems.Add(new PanelItem{Text=text,Bounds=new Rect(0,panelY,panelWidth,h)});
            if(panelDraw)GUI.Label(new Rect(0,panelY,panelWidth,h),text,style);panelY+=h+6;
        }
        void PanelAction(string text,Action action,bool enabled=true){
            float w=Mathf.Min(panelWidth,Mathf.Ceil(panelButton.CalcSize(new GUIContent(text)).x));
            if(panelX>0&&panelX+w>panelWidth)PanelRow();
            if(!panelDraw)PanelItems.Add(new PanelItem{Text=text,Bounds=new Rect(panelX,panelY,w,24),Button=true,Click=enabled?action:null});
            if(panelDraw){bool prior=GUI.enabled;GUI.enabled=enabled;
                if(GUI.Button(new Rect(panelX,panelY,w,24),text,panelButton))action();GUI.enabled=prior;}
            panelX+=w+4;
        }
        void PanelContents(bool draw){
            panelDraw=draw;panelX=panelY=0;
            if(!draw)PanelItems.Clear();
            PanelText("Tiny Days · 동작 검토 v0.112",true);
            PanelText(LyingReview?LyingStatus:Idle!=null?(Idle.Seated?"앉음 · ":"서기 · ")+Idle.State:SitState);
            PanelText("기본 호흡 자동 · 눈 깜빡임 자동 (수면 중 억제)");
            PanelText("동작 선택",true);
            PanelAction((SupportedSitting?"● ":"")+"한 손 지지",()=>SetSitStyle(true),CanChangeSitStyle);
            PanelAction((!SupportedSitting?"● ":"")+"양손 지지",()=>SetSitStyle(false),CanChangeSitStyle);PanelRow();
            for(int i=0;i<Math.Min(8,clips.Length);i++){
                if(i==2||i==3)continue;int index=i;
                bool enabled=!SitTransition&&!LyingTransition&&(i!=6||IsSitDown(selected)||selected==7||selected==9||Posture==RestPosture.Supine||Idle!=null&&Idle.Seated);
                PanelAction((selected==(i==5?SitClip:i==6?StandClip:i)?"● ":"")+Labels[i],()=>{
                    if(index==5){RequestSit();followBreathing=true;}
                    else if(index==6){if(Idle==null&&!IsLyingClip(selected))StartBreathing(true);RequestStand();followBreathing=true;}
                    else Select(index);
                },enabled);
            }
            LyingPanel();
            SleepPanel();
            SidePanel();
            PanelText("재생 · 배속",true);
            PanelAction(paused?"재생":"일시정지",()=>paused=!paused);
            PanelAction((slow?"● ":"")+"0.5×",()=>slow=true);PanelAction((!slow?"● ":"")+"1×",()=>slow=false);
            PanelText(Idle!=null&&Idle.IsVariation?
                $"{Idle.State} {Idle.Time:F2}/{(Idle.State==CommonIdleDirector.Stage.Variation?Idle.VariationDuration:CommonIdleDirector.BlendSeconds):F2}초":
                MovingReview?$"이동 {MoveWeight:P0} · 달리기 혼합 {footTransition.RunWeight:P0}":
                $"클립 {DisplayTime:F2}/{clips[selected].length:F2}초 · 누적 {elapsed:F2}초");
            PanelText($"현재 {(slow?"0.5":"1")}× · {(paused?"일시정지":"재생 중")}");
            PanelText("구도 · 주요 8포즈",true);
            PanelAction("정면",()=>View(0));PanelAction("측면",()=>View(90));PanelAction("비스듬히 / Home",Home);PanelRow();
            string[] phases=IsDirectSideEntry(selected)?SideDownLabels:IsDirectSideReturn(selected)?DirectSideLabels:IsSideClip(selected)?SideLabels:IsSleepClip(selected)?SleepPoseLabels:IsLyingClip(selected)?LyingPoseLabels:IsVariation(DisplayClip)?AdultIdleVariations.Find(DisplayClip).Poses:
                IsBreathing(selected)?new[]{"호흡 0","0.5초","1초","1.5초","2초","2.5초","3초","3.5초"}:
                selected>=5?(IsStandUp(selected)?new[]{"앉음","준비","손 지지","들기","올라가기","펴기","안정","서기"}:new[]{"서기","준비","굽힘","내려가기","접촉","손 짚기","안정","앉음"}):
                new[]{"L Contact","L Recoil","L Passing","L High","R Contact","R Recoil","R Passing","R High"};
            for(int i=0;i<8;i++){int phase=i;PanelAction(ShowingRun?AdultRunTiming.Label(i):phases[i],()=>Pose(phase));}
            PanelText("왼쪽 회전 · 오른쪽 패닝 · 가운데 높이\nWASD/화살표 이동 · 휠 줌\nQ/E 높이 · Home 복귀");
            PanelText("이동",true);
            PanelAction("이동 검토 / 처음 위치",BeginMovement);
            PanelAction("걷기 / 출발",()=>RequestRun(false),!SitTransition||Idle!=null);
            PanelAction("달리기",()=>RequestRun(true),!SitTransition||Idle!=null);
            PanelAction("정지 / 예약 취소",()=>RequestWalk(false),MovingReview||Idle!=null||LyingReview);
            PanelText(MovingReview?$"{footTransition.StatusLabel} · 지지발 {(footTransition.LeftPlanted?"왼쪽 ":"")}{(footTransition.RightPlanted?"오른쪽":"")}\n{(paused?0:Speed*(slow?.5f:1)):F2}m/s · {Travel:F2}m / 약 6m":"제자리 검토 · 이동 검토에서 출발/정지 확인");
            PanelText("기본 호흡 · 변형",true);
            PanelAction("서기 기본 호흡",()=>StartBreathing(false));PanelAction("앉기 기본 호흡",()=>StartBreathing(true));
            PanelAction((AutomaticIdle?"● ":"")+"자동 변형",()=>AutomaticIdle=!AutomaticIdle);PanelRow();
            if(Idle!=null)foreach(var id in Idle.Candidates){string name=id;PanelAction(name,()=>Idle.Play(name),Idle.State==CommonIdleDirector.Stage.Breathing);}
            PanelText(Idle!=null?$"{Idle.CurrentId} · {Idle.Time:F2}초\n예약 {Idle.Pending}":"기본 호흡을 선택하면 변형 재생 가능");
            PanelText("발목 4초·3쌍 · 짝발 13초·각 5회\n낙서 8초 · 손 회수 0.3초\n이동 시 중단 · 서기 정리 0.4초");PanelRow();
        }
        void OnGUI(){
            if(resident==null||clips==null||clips.Length==0)return;
            if(panelButton==null){
                panelButton=new GUIStyle(GUI.skin.button){font=font,fontSize=13,fixedHeight=24,stretchWidth=false,padding=new RectOffset(8,8,2,2),alignment=TextAnchor.MiddleCenter};
                panelLabel=new GUIStyle(GUI.skin.label){font=font,fontSize=13,wordWrap=true,alignment=TextAnchor.UpperLeft};
                panelHeading=new GUIStyle(panelLabel){fontStyle=FontStyle.Bold};
            }
            var outer=Panel;GUI.Box(outer,GUIContent.none);
            var viewport=new Rect(outer.x+8,outer.y+8,Mathf.Max(1,outer.width-16),Mathf.Max(1,outer.height-16));
            // Reserve the scrollbar gutter even when hidden: wrapping never oscillates.
            panelWidth=Mathf.Max(1,viewport.width-18);PanelContents(false);PanelContentHeight=panelY;
            panelScroll.x=0;panelScroll.y=Mathf.Clamp(panelScroll.y,0,Mathf.Max(0,PanelContentHeight-viewport.height));
            panelScroll=GUI.BeginScrollView(viewport,panelScroll,new Rect(0,0,panelWidth,PanelContentHeight),false,false);
            PanelContents(true);GUI.EndScrollView();
        }
    }
}
