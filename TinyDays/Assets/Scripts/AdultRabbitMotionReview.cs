using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TinyDays.Review
{
    public sealed class AdultRabbitMotionReview : MonoBehaviour
    {
        public GameObject resident; public Camera reviewCamera; public AnimationClip[] clips;
        public int selected; public bool paused,slow; double elapsed;
        public double Elapsed => elapsed;
        double DisplayTime => selected>=5&&selected<=7?Math.Min(elapsed,clips[selected].length):elapsed%clips[selected].length;
        public IdleSecondaryMotion idleProfile=new IdleSecondaryMotion();
        public CommonIdleDirector Idle {get;private set;}
        public bool AutomaticIdle=true;
        bool followBreathing;
        public void StartBreathing(bool seated){
            var position=resident.transform.position;var oldPivot=pivot;float oldYaw=yaw,oldPitch=pitch,oldDistance=distance;
            Select(seated?9:8);resident.transform.position=position;pivot=oldPivot;yaw=oldYaw;pitch=oldPitch;distance=oldDistance;ApplyCamera();
            if(graph.IsValid())graph.Destroy();active=-1;resident.GetComponent<Animator>().enabled=false;
            // Recover to the exact grounded locomotion entry pose, not the raw
            // legacy idle which sits about 1mm below this review floor.
            clips[0].SampleAnimation(resident,0);var entry=new AdultRabbitFootTransition(resident);entry.Apply();
            var joints=resident.GetComponentsInChildren<Transform>();var positions=new Vector3[joints.Length];var rotations=new Quaternion[joints.Length];
            for(int i=0;i<joints.Length;i++){positions[i]=joints[i].localPosition;rotations[i]=joints[i].localRotation;}
            Action recovery=()=>{for(int i=1;i<joints.Length;i++){joints[i].localPosition=positions[i];joints[i].localRotation=rotations[i];}};
            Idle=new CommonIdleDirector(resident,clips[8],clips[9],clips[6],clips[8],clips[7],seated,173,recovery,idleProfile.Supports);Idle.Automatic=AutomaticIdle;
            followBreathing=true;paused=false;
        }
        void AdvanceIdle(float dt){
            Idle.Automatic=AutomaticIdle;Idle.Advance(dt);
            if(Idle.State==CommonIdleDirector.Stage.Ready){var request=Idle.Pending;float remaining=(float)Idle.UnusedTime;Idle=null;BeginMovement(false);WantsToWalk=true;footTransition.RequestRun(request==CommonIdleDirector.Departure.Run);if(remaining>0)Advance(remaining/(slow?.5f:1));return;}
            float secondaryWeight=Idle.State==CommonIdleDirector.Stage.Breathing?1:0;
            idleProfile.Apply(Idle.Time,secondaryWeight);
            sitCoat?.Apply(Idle.State==CommonIdleDirector.Stage.Rising?6:Idle.Seated?9:8,Idle.Time,Idle.State==CommonIdleDirector.Stage.Rising?clips[6].length:4);
        }
        public bool MovingReview {get;private set;}
        public float MoveWeight {get;private set;}
        public bool WantsToWalk {get;private set;}
        public float Travel {get;private set;}
        AdultRabbitFootTransition footTransition;
        AdultRabbitCoatClearance coatClearance;
        AdultRabbitSitCoat sitCoat;
        bool pendingSit;
        public string SitState => pendingSit?"정지 후 앉기 대기":selected==5?(SitTransition?"앉는 중":"앉음"):selected==6?(SitTransition?"일어나는 중":"서기"):selected==7?"앉음":"서기";
        public bool SitTransition => (selected==5||selected==6)&&elapsed+1e-5<clips[selected].length;
        public void RequestSit(){if(Idle!=null){if(Idle.Seated||Idle.State!=CommonIdleDirector.Stage.Breathing)return;Select(5);followBreathing=true;paused=false;return;}if(SitTransition||selected==7||selected==5)return;if(MovingReview){pendingSit=true;RequestWalk(false);return;}Select(5);paused=false;}
        public void RequestStand(){if(Idle!=null){Idle.Rise();return;}if(selected==9){StartBreathing(true);Idle.Rise();return;}if(SitTransition||!(selected==5||selected==7))return;Select(6);paused=false;}
        public float CoatDisplacement => coatClearance?.MaxDisplacement??0;
        public AdultRabbitFootTransition FootTransition => footTransition;
        public float Speed => MovingReview&&footTransition!=null?footTransition.Speed:0;
        double walkTime,idleTime,movementRemainder;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable idlePlayable,walkPlayable,runPlayable;
        Vector3 pivot=new Vector3(0,1,0),previous;
        float yaw=32.4f,pitch=7.4f,distance=6.21f;
        int drag=-1;
        Rect Panel => new Rect(14,14,Screen.width-28,318);
        PlayableGraph graph; AnimationClipPlayable playable; int active=-1; Font font;
        static readonly string[] Labels={"두 발 대기","두 발 총총걸음","네 발 대기 (보류)","깡충 (보류)","두 발 달리기","앉기","일어서기","앉은 자세"};
        void OnEnable(){if(!Application.isPlaying)return;font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);if(resident&&clips!=null&&clips.Length>=10)StartBreathing(false);else Sample(0);}
        void OnDisable(){Idle=null;followBreathing=false;sitCoat?.Dispose();sitCoat=null;pendingSit=false;coatClearance?.Dispose();coatClearance=null;drag=-1;active=-1;MovingReview=false;MoveWeight=0;WantsToWalk=false;footTransition?.RestoreSourcePose();footTransition=null;if(resident)resident.transform.localPosition=Vector3.zero;selected=0;elapsed=0;if(graph.IsValid())graph.Destroy();if(font)Destroy(font);}
        void OnApplicationFocus(bool focus){if(!focus)drag=-1;}
        void Update(){HandleCamera();Advance(Time.unscaledDeltaTime);}
        public void Advance(float seconds){
            float dt=paused?0:Mathf.Max(0,seconds)*(slow?.5f:1);
            elapsed+=dt;
            if(Idle!=null){AdvanceIdle(dt);return;}
            if(!MovingReview){Sample(elapsed);if(followBreathing&&!SitTransition&&(selected==5||selected==6))StartBreathing(selected==5);return;}
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
            if(pendingSit&&footTransition.State==AdultRabbitFootTransition.Stage.Idle){var position=resident.transform.position;bool follow=followBreathing;Select(5);followBreathing=follow;resident.transform.position=position;Home();paused=false;}
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
        public void RequestWalk(bool walk){if(Idle!=null){if(walk)Idle.Request(CommonIdleDirector.Departure.Walk);else Idle.Cancel();return;}if(walk&&selected>=8){RequestRun(false);return;}if(!MovingReview)BeginMovement();WantsToWalk=walk;footTransition.Request(walk);}
        public void RequestRun(bool run){if(Idle!=null){Idle.Request(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk);return;}if(!MovingReview&&selected>=8){bool seated=selected==9;float phase=(float)elapsed;StartBreathing(seated);AdvanceIdle(phase);Idle.Request(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk);return;}if(!MovingReview&&(selected==7||selected==5&&!SitTransition)){StartBreathing(true);Idle.Request(run?CommonIdleDirector.Departure.Run:CommonIdleDirector.Departure.Walk);return;}if(!MovingReview)BeginMovement();WantsToWalk=true;footTransition.RequestRun(run);}
        public void Home(){pivot=resident.transform.position+new Vector3(0,1,0);yaw=32.4f;pitch=7.4f;distance=6.21f;drag=-1;ApplyCamera();}
        public void View(float angle){Home();yaw=angle;pitch=4;ApplyCamera();}
        void ApplyCamera(){reviewCamera.transform.rotation=Quaternion.Euler(pitch,180+yaw,0);reviewCamera.transform.position=pivot-reviewCamera.transform.forward*distance;}
        public void CameraDrag(int button,Vector2 delta){
            if(button==0)pivot+=FarmStudyReview.ScreenPan(reviewCamera.transform.rotation,delta,distance,reviewCamera.fieldOfView,Screen.height);
            if(button==1){yaw+=delta.x*.16f;pitch=Mathf.Clamp(pitch-delta.y*.16f,-89,75);}
            if(button==2)pivot+=Vector3.up*FarmStudyReview.MouseHeightDelta(delta.y,distance,reviewCamera.fieldOfView,Screen.height);
            ApplyCamera();}
        public void Zoom(float wheel){distance=Mathf.Clamp(distance*Mathf.Exp(wheel*.12f),.5f,12);ApplyCamera();}
        void HandleCamera(){
            if(Input.GetKeyDown(KeyCode.Home)){Home();return;}
            bool ui=Panel.Contains(new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y));
            if(ui)drag=-1;
            for(int b=0;b<3;b++)if(Input.GetMouseButtonDown(b)&&!ui){drag=b;previous=Input.mousePosition;}
            if(drag>=0&&!Input.GetMouseButton(drag))drag=-1;
            if(drag>=0){Vector3 d=Input.mousePosition-previous;CameraDrag(drag,new Vector2(d.x,d.y));previous=Input.mousePosition;}
            if(!ui){if(Input.mouseScrollDelta.y!=0)Zoom(Input.mouseScrollDelta.y);
                float h=(Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0);if(h!=0){pivot+=Vector3.up*h*2*Time.unscaledDeltaTime;ApplyCamera();}}
        }
        bool ShowingRun => selected==4||(MovingReview&&footTransition.RunWeight>.5f);
        static readonly float[] SitPhases={0,.125f,.32f,.52f,.6875f,.75f,.90f,1},StandPhases={0,.14f,.22f,.36f,.55f,.75f,.90f,1};
        public void Pose(int index){int clip=selected>=5?selected:ShowingRun?4:1;Select(clip);paused=true;int phase=Mathf.Clamp(index,0,7);elapsed=clips[clip].length*(clip==5?SitPhases[phase]:clip==6?StandPhases[phase]:clip==7?0:clip==4?AdultRunTiming.Phase(phase):phase/8.0);Sample(elapsed);}
        void Rebuild(){if(graph.IsValid())graph.Destroy();graph=PlayableGraph.Create("Adult rabbit motion");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var animator=resident.GetComponent<Animator>();animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            playable=AnimationClipPlayable.Create(graph,clips[selected]);var output=AnimationPlayableOutput.Create(graph,"Adult",animator);output.SetSourcePlayable(playable);graph.Play();active=selected;}
        public void Select(int index){Idle=null;followBreathing=false;sitCoat?.Dispose();sitCoat=null;pendingSit=false;coatClearance?.Dispose();coatClearance=null;footTransition?.RestoreSourcePose();footTransition=null;if(MovingReview){MovingReview=false;MoveWeight=0;WantsToWalk=false;Travel=0;resident.transform.localPosition=Vector3.zero;active=-1;Home();}selected=Mathf.Clamp(index,0,clips.Length-1);elapsed=0;if(selected==4){clips[0].SampleAnimation(resident,0);coatClearance=new AdultRabbitCoatClearance(resident);}if(selected>=5)sitCoat=new AdultRabbitSitCoat(resident);Sample(0);}
        public void Sample(double time){if(active!=selected||!graph.IsValid())Rebuild();playable.SetTime(selected>=5&&selected<=7?Math.Min(time,clips[selected].length):time%clips[selected].length);graph.Evaluate(0);if(selected==4)coatClearance?.Apply(1f/60);sitCoat?.Apply(selected,time,clips[selected].length);}
        void OnGUI(){if(font)GUI.skin.font=font;GUILayout.BeginArea(Panel,GUI.skin.box);GUILayout.Label("Tiny Days · 성인 토끼 동작 검토 v0.93 · "+(Idle!=null?(Idle.Seated?"앉음 · ":"서기 · ")+Idle.State:SitState));GUILayout.BeginHorizontal();
            for(int i=0;i<Math.Min(8,clips.Length);i++){if(i==2||i==3)continue;GUI.enabled=!SitTransition&&(i!=6||selected==5||selected==7||selected==9||Idle!=null&&Idle.Seated);if(GUILayout.Button((selected==i?"● ":"")+Labels[i],GUILayout.Height(32))){if(i==5){RequestSit();followBreathing=true;}else if(i==6){if(Idle==null)StartBreathing(true);RequestStand();followBreathing=true;}else Select(i);}}GUI.enabled=true;
            if(GUILayout.Button(paused?"재생":"일시정지",GUILayout.Height(32)))paused=!paused;
            if(GUILayout.Button((slow?"● ":"")+"0.5×",GUILayout.Height(32)))slow=true;
            if(GUILayout.Button((!slow?"● ":"")+"1×",GUILayout.Height(32)))slow=false;
            GUILayout.EndHorizontal();GUILayout.Label(MovingReview?$"현재 {(slow?"0.5":"1")}× · {(paused?"일시정지":"재생 중")} · 이동 {MoveWeight:P0} · 달리기 혼합 {footTransition.RunWeight:P0}":$"현재 {(slow?"0.5":"1")}× · {(paused?"일시정지":"재생 중")} · 클립 {DisplayTime:F2}/{clips[selected].length:F2}초 · 누적 {elapsed:F2}초");
            GUILayout.BeginHorizontal();if(GUILayout.Button("정면"))View(0);if(GUILayout.Button("측면"))View(90);if(GUILayout.Button("비스듬히 / Home"))Home();GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();string[] phases=selected>=8?new[]{"호흡 0","0.5초","1초","1.5초","2초","2.5초","3초","3.5초"}:selected>=5?(selected==6?new[]{"앉음","준비","손 지지","들기","올라가기","펴기","안정","서기"}:new[]{"서기","준비","굽힘","내려가기","접촉","손 짚기","안정","앉음"}):new[]{"L Contact","L Recoil","L Passing","L High","R Contact","R Recoil","R Passing","R High"};for(int i=0;i<8;i++)if(GUILayout.Button(ShowingRun?AdultRunTiming.Label(i):phases[i]))Pose(i);GUILayout.EndHorizontal();
            GUILayout.Label("왼쪽 패닝 · 오른쪽 회전 · 가운데 높이 · 휠 줌 · Q/E 높이 · Home 복귀");
            GUILayout.BeginHorizontal();if(GUILayout.Button("이동 검토 / 처음 위치"))BeginMovement();GUI.enabled=!SitTransition||Idle!=null;if(GUILayout.Button("걷기 / 출발"))RequestRun(false);if(GUILayout.Button("달리기"))RequestRun(true);GUI.enabled=MovingReview||Idle!=null;if(GUILayout.Button("정지 / 예약 취소"))RequestWalk(false);GUI.enabled=true;GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();if(GUILayout.Button("서기 기본 호흡"))StartBreathing(false);if(GUILayout.Button("앉기 기본 호흡"))StartBreathing(true);AutomaticIdle=GUILayout.Toggle(AutomaticIdle,"자동 변형");GUILayout.EndHorizontal();
            GUILayout.Label(Idle!=null?$"{Idle.CurrentId} · {Idle.Time:F2}초 · 예약 {Idle.Pending} · 완성된 추가 변형 {Idle.CandidateCount}개":"추가 변형은 순차 제작 · 기본 호흡 버튼에서 이동 연결 검토");
            if(Idle!=null&&Idle.CandidateCount>0){GUILayout.BeginHorizontal();foreach(var id in Idle.Candidates)if(GUILayout.Button(id))Idle.Play(id);GUILayout.EndHorizontal();}
            GUILayout.Label(MovingReview?$"{footTransition.StatusLabel} · 지지발 {(footTransition.LeftPlanted?"왼쪽 ":"")}{(footTransition.RightPlanted?"오른쪽":"")} · {(paused?0:Speed*(slow?.5f:1)):F2}m/s · {Travel:F2}m / 약 6m":"제자리 검토 · 이동 검토에서 출발/정지를 확인하세요");GUILayout.EndArea();}
    }
}
