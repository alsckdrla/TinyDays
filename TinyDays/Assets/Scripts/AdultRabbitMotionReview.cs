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
        public bool MovingReview {get;private set;}
        public float MoveWeight {get;private set;}
        public bool WantsToWalk {get;private set;}
        public float Travel {get;private set;}
        AdultRabbitFootTransition footTransition;
        AdultRabbitCoatClearance coatClearance;
        public float CoatDisplacement => coatClearance?.MaxDisplacement??0;
        public AdultRabbitFootTransition FootTransition => footTransition;
        public float Speed => MovingReview&&footTransition!=null?footTransition.Speed:0;
        double walkTime,idleTime,movementRemainder;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable idlePlayable,walkPlayable;
        Vector3 pivot=new Vector3(0,1,0),previous;
        float yaw=32.4f,pitch=7.4f,distance=6.21f;
        int drag=-1;
        Rect Panel => new Rect(14,14,Screen.width-28,255);
        PlayableGraph graph; AnimationClipPlayable playable; int active=-1; Font font;
        static readonly string[] Labels={"두 발 대기","두 발 총총걸음","네 발 대기","낮은 깡충 이동"};
        void OnEnable(){if(!Application.isPlaying)return;font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);Sample(0);}
        void OnDisable(){coatClearance?.Dispose();coatClearance=null;drag=-1;active=-1;MovingReview=false;MoveWeight=0;WantsToWalk=false;footTransition?.RestoreSourcePose();footTransition=null;if(resident)resident.transform.localPosition=Vector3.zero;if(graph.IsValid())graph.Destroy();if(font)Destroy(font);}
        void OnApplicationFocus(bool focus){if(!focus)drag=-1;}
        void Update(){HandleCamera();Advance(Time.unscaledDeltaTime);}
        public void Advance(float seconds){
            float dt=paused?0:Mathf.Max(0,seconds)*(slow?.5f:1);
            elapsed+=dt;
            if(!MovingReview){Sample(elapsed);return;}
            // Small deterministic integration steps keep speed, phase and travel synchronized.
            movementRemainder+=dt;
            while(movementRemainder+1e-6>=1.0/240){float step=1f/240f;movementRemainder-=1.0/240;
                if(Travel>=5.25f&&WantsToWalk)RequestWalk(false);
                float distanceStep=footTransition.Step(step);
                walkTime=footTransition.WalkTime;MoveWeight=footTransition.Weight;idleTime+=step;Travel+=distanceStep;
                var delta=resident.transform.forward*distanceStep;resident.transform.position+=delta;pivot+=delta;
                EvaluateMovement();
            }
            if(paused||seconds<=0)EvaluateMovement();coatClearance?.Apply(dt);ApplyCamera();
        }
        void EvaluateMovement(){
            footTransition?.RestoreSourcePose();
            idlePlayable.SetTime(idleTime%clips[0].length);walkPlayable.SetTime(walkTime%clips[1].length);
            mixer.SetInputWeight(0,1-MoveWeight);mixer.SetInputWeight(1,MoveWeight);graph.Evaluate(0);footTransition?.Apply();
        }
        public void BeginMovement(){
            Select(0);MovingReview=true;paused=false;MoveWeight=0;Travel=0;walkTime=idleTime=movementRemainder=0;WantsToWalk=false;
            if(graph.IsValid())graph.Destroy();graph=PlayableGraph.Create("Adult movement transition");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            idlePlayable=AnimationClipPlayable.Create(graph,clips[0]);walkPlayable=AnimationClipPlayable.Create(graph,clips[1]);
            mixer=AnimationMixerPlayable.Create(graph,2);graph.Connect(idlePlayable,0,mixer,0);graph.Connect(walkPlayable,0,mixer,1);
            var animator=resident.GetComponent<Animator>();animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var output=AnimationPlayableOutput.Create(graph,"Locomotion",animator);output.SetSourcePlayable(mixer);graph.Play();footTransition=null;EvaluateMovement();coatClearance=new AdultRabbitCoatClearance(resident);footTransition=new AdultRabbitFootTransition(resident);Advance(0);
        }
        public void RequestWalk(bool walk){if(!MovingReview)BeginMovement();WantsToWalk=walk;footTransition.Request(walk);}
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
        public void Pose(int index){Select(1);paused=true;elapsed=clips[1].length*Mathf.Clamp(index,0,7)/8.0;Sample(elapsed);}
        void Rebuild(){if(graph.IsValid())graph.Destroy();graph=PlayableGraph.Create("Adult rabbit motion");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var animator=resident.GetComponent<Animator>();animator.enabled=true;animator.applyRootMotion=false;
            playable=AnimationClipPlayable.Create(graph,clips[selected]);var output=AnimationPlayableOutput.Create(graph,"Adult",animator);output.SetSourcePlayable(playable);graph.Play();active=selected;}
        public void Select(int index){coatClearance?.Dispose();coatClearance=null;footTransition?.RestoreSourcePose();footTransition=null;if(MovingReview){MovingReview=false;MoveWeight=0;WantsToWalk=false;Travel=0;resident.transform.localPosition=Vector3.zero;active=-1;Home();}selected=Mathf.Clamp(index,0,clips.Length-1);elapsed=0;Sample(0);}
        public void Sample(double time){if(active!=selected||!graph.IsValid())Rebuild();playable.SetTime(time%clips[selected].length);graph.Evaluate(0);}
        void OnGUI(){if(font)GUI.skin.font=font;GUILayout.BeginArea(Panel,GUI.skin.box);GUILayout.Label("Tiny Days · 성인 토끼 동작 검토 v0.82");GUILayout.BeginHorizontal();
            for(int i=0;i<clips.Length;i++)if(GUILayout.Button((selected==i?"● ":"")+Labels[i],GUILayout.Height(32)))Select(i);
            if(GUILayout.Button(paused?"재생":"일시정지",GUILayout.Height(32)))paused=!paused;
            if(GUILayout.Button((slow?"● ":"")+"0.5×",GUILayout.Height(32)))slow=true;
            if(GUILayout.Button((!slow?"● ":"")+"1×",GUILayout.Height(32)))slow=false;
            GUILayout.EndHorizontal();GUILayout.Label(MovingReview?$"현재 {(slow?"0.5":"1")}× · {(paused?"일시정지":"재생 중")} · 걷기 혼합 {MoveWeight:P0} · 주기 {walkTime%clips[1].length:F2}/{clips[1].length:F2}초":$"현재 {(slow?"0.5":"1")}× · {(paused?"일시정지":"재생 중")} · 클립 {elapsed%clips[selected].length:F2}/{clips[selected].length:F2}초 · 누적 {elapsed:F2}초");
            GUILayout.BeginHorizontal();if(GUILayout.Button("정면"))View(0);if(GUILayout.Button("측면"))View(90);if(GUILayout.Button("비스듬히 / Home"))Home();GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();string[] phases={"L Contact","L Recoil","L Passing","L High","R Contact","R Recoil","R Passing","R High"};for(int i=0;i<8;i++)if(GUILayout.Button(phases[i]))Pose(i);GUILayout.EndHorizontal();
            GUILayout.Label("왼쪽 패닝 · 오른쪽 회전 · 가운데 높이 · 휠 줌 · Q/E 높이 · Home 복귀");
            GUILayout.BeginHorizontal();if(GUILayout.Button("이동 검토 / 처음 위치"))BeginMovement();if(GUILayout.Button("출발"))RequestWalk(true);if(GUILayout.Button("정지"))RequestWalk(false);GUILayout.EndHorizontal();
            GUILayout.Label(MovingReview?$"{footTransition.StatusLabel} · 지지발 {(footTransition.LeftPlanted?"왼쪽 ":"")}{(footTransition.RightPlanted?"오른쪽":"")} · {(paused?0:Speed*(slow?.5f:1)):F2}m/s · {Travel:F2}m / 약 6m":"제자리 검토 · 이동 검토에서 출발/정지를 확인하세요");GUILayout.EndArea();}
    }
}
