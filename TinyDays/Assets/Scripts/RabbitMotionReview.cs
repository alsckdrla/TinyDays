using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TinyDays.Review
{
    // Deterministic review timeline. No autonomy, navigation or game speed settings.
    public sealed class RabbitMotionReview : MonoBehaviour
    {
        public GameObject resident;
        public Camera reviewCamera;
        public AnimationClip[] clips;
        public float[] cycleDistances;
        public bool[] looping;
        public int[] following;
        public AnimationCurve[] travelCurves;
        public int selected=2;
        public int view;
        public bool small,paused,slow,sequence;
        public double elapsed;
        public int activeClip;
        PlayableGraph graph;
        AnimationClipPlayable playable;
        int playingClip=-1;
        Font font;
        static readonly string[] Labels={"두 발 대기","네 발 대기","두 발 걷기","낮은 깡충 이동","두 발 → 네 발","네 발 → 두 발","걷기 출발","걷기 정지","깡충 출발","깡충 정지"};
        static readonly string[] Views={"비스듬히","정면","측면","후면"};
        public static readonly int[] SequenceClips={0,6,2,7,4,1,8,3,9,5};
        static readonly int[] Repeats={1,1,2,1,1,1,1,3,1,1};
        public double SequenceDuration { get { double total=0;for(int i=0;i<SequenceClips.Length;i++)total+=clips[SequenceClips[i]].length*Repeats[i];return total; } }

        void OnEnable()
        {
            if(!Application.isPlaying)return;
            font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);
            Sample(elapsed);
        }
        void OnDisable(){if(graph.IsValid())graph.Destroy();if(font)Destroy(font);}
        void RebuildPlayable(int index)
        {
            if(graph.IsValid())graph.Destroy();
            graph=PlayableGraph.Create("Rabbit motion review");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var animator=resident.GetComponent<Animator>();animator.enabled=true;animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            playable=AnimationClipPlayable.Create(graph,clips[index]);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);
            var output=AnimationPlayableOutput.Create(graph,"Generic rabbit",animator);output.SetSourcePlayable(playable);graph.Play();playingClip=index;
        }
        void Update()
        {
            if(Input.GetMouseButtonDown(0)){
                var p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/UIScale;
                for(int i=0;i<19;i++)if(ButtonRect(i).Contains(p)){Activate(i);break;}
            }
            if(!paused)elapsed+=Time.unscaledDeltaTime*(slow?.5:1);
            Sample(elapsed);
        }
        float UIScale=>Mathf.Clamp(Screen.width/1280f,.65f,1.5f);
        Rect ButtonRect(int index)
        {
            int cols=index<15?5:4,row=index<15?index/5:3,col=index<15?index%5:index-15;
            float width=(Screen.width/UIScale-32-(cols-1)*4)/cols;
            return new Rect(16+col*(width+4),40+row*30,width,27);
        }
        void Activate(int index)
        {
            if(index<10)Select(index);
            else if(index==10)StartSequence();
            else if(index==11)paused=!paused;
            else if(index==12){elapsed=0;Sample(0);}
            else if(index==13)slow=!slow;
            else if(index==14){small=!small;SetView();}
            else{view=index-15;SetView();}
        }
        public void Select(int index){sequence=false;selected=Mathf.Clamp(index,0,clips.Length-1);elapsed=0;Sample(0);}
        public void StartSequence(){sequence=true;elapsed=0;Sample(0);}
        float Travel(int index,double time)=>travelCurves[index].Evaluate((float)Math.Min(clips[index].length,Math.Max(0,time)));
        public void Resolve(double time,out int index,out double localTime,out float distance)
        {
            time=Math.Max(0,time);double offset=0;
            if(sequence){
                for(int i=0;i<SequenceClips.Length;i++){
                    int c=SequenceClips[i];double duration=clips[c].length*Repeats[i];
                    if(time<duration){ResolveLoop(c,time,offset,out index,out localTime,out distance);return;}
                    time-=duration;offset+=cycleDistances[c]*Repeats[i];
                }
                ResolveLoop(0,time,offset,out index,out localTime,out distance);return;
            }
            int current=selected;
            for(int guard=0;guard<clips.Length+1;guard++){
                if(looping[current]||time<clips[current].length){ResolveLoop(current,time,offset,out index,out localTime,out distance);return;}
                time-=clips[current].length;offset+=cycleDistances[current];current=following[current];
            }
            throw new InvalidOperationException("Motion follow chain does not terminate in a loop");
        }
        void ResolveLoop(int current,double time,double offset,out int index,out double localTime,out float distance)
        {
            index=current;double cycles=looping[current]?Math.Floor(time/clips[current].length):0;
            localTime=looping[current]?time-cycles*clips[current].length:Math.Min(time,clips[current].length);
            distance=(float)(offset+cycles*cycleDistances[current])+Travel(current,localTime);
        }
        public void Sample(double time)
        {
            Resolve(time,out int index,out double local,out float distance);
            SamplePose(index,local,distance);
        }
        // Exact endpoint sampling for imported-pose and connector checks.
        public void SamplePose(int index,double time,float distance)
        {
            activeClip=index;resident.transform.localPosition=new Vector3(0,0,distance);
            if(!graph.IsValid()||playingClip!=index)RebuildPlayable(index);
            playable.SetTime(Math.Max(0,Math.Min(time,clips[index].length)));graph.Evaluate(0);SetView();
        }
        public void SetView()
        {
            var center=resident.transform.position+new Vector3(0,1.03f,0);
            Vector3[] offsets={new Vector3(-4,2.3f,6),new Vector3(0,.12f,7),new Vector3(-7,.12f,0),new Vector3(0,.12f,-7)};
            reviewCamera.transform.position=center+offsets[view];reviewCamera.transform.LookAt(center);
            reviewCamera.orthographicSize=small?2.8f:1.95f;
        }
        void OnGUI()
        {
            float scale=UIScale;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);if(font)GUI.skin.font=font;
            GUI.skin.button.fontSize=15;GUI.skin.label.fontSize=15;
            float width=Screen.width/scale;
            GUI.Box(new Rect(12,10,width-24,194),GUIContent.none);
            GUI.Label(new Rect(18,13,width-36,25),"Tiny Days · 2-3 탄력 동작 검토 | 개별 선택은 초기화 · 연결 장면은 자연스럽게 이어집니다");
            var pointer=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/scale;
            for(int i=0;i<19;i++){
                string label=i<10?(!sequence&&i==selected?"● ":"")+Labels[i]:i==10?(sequence?"● 연결 장면":"연결 장면 재생"):i==11?(paused?"재생":"일시정지"):i==12?"처음부터":i==13?(slow?"느린 재생 0.5×":"정상 재생 1×"):i==14?(small?"가까이 보기":"작게 보기"):Views[i-15];
                if(Event.current.type==EventType.Repaint)GUI.skin.button.Draw(ButtonRect(i),new GUIContent(label),ButtonRect(i).Contains(pointer),false,false,false);
            }
            string mode=sequence?(elapsed>=SequenceDuration?"연결 완료 · 대기 유지":"연결 장면"):"개별 동작";
            GUI.Label(new Rect(18,166,width-36,28),$"{mode} · {Labels[activeClip]} · {(paused?"정지":"재생 중")} · {elapsed:F2}초");
            GUI.matrix=Matrix4x4.identity;
        }
    }
}
