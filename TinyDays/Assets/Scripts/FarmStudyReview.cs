using UnityEngine;

namespace TinyDays.Review
{
    public sealed class FarmStudyReview : MonoBehaviour
    {
        public FarmLifeDirector director;
        public Camera reviewCamera;
        public int view, focus;
        public bool close, hidden;
        Font font;
        readonly string[] views={"기본 구도","반대 구도","왼쪽 구도","오른쪽 구도"};
        void OnEnable(){font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);}
        void OnDisable(){if(font)Destroy(font);}
        float Scale=>Mathf.Clamp(Screen.width/1100f,.65f,1.5f);
        Rect ButtonRect(int i)
        {
            float w=Screen.width/Scale;int columns=w<760?3:6;
            int rows=(6+columns-1)/columns;
            return new Rect(14+(i%columns)*(w-28)/columns,Screen.height/Scale-12-rows*32+(i/columns)*32,(w-28)/columns-5,28);
        }
        void Update()
        {
            if(Input.GetMouseButtonDown(0))
            {
                Vector2 p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
                if(hidden){if(new Rect(12,12,112,30).Contains(p))hidden=false;}
                else for(int i=0;i<6;i++)if(ButtonRect(i).Contains(p))
                {
                    if(i==0)director.paused=!director.paused;
                    if(i==1)director.Restart();
                    if(i==2)close=!close;
                    if(i==3)view=(view+1)%4;
                    if(i==4){focus=(focus+1)%director.residents.Length;close=true;}
                    if(i==5)hidden=true;
                    break;
                }
            }
        }
        void LateUpdate(){SetView();}
        public void SetView()
        {
            Vector3[] offsets={new Vector3(19,24,-29),new Vector3(-19,24,29),new Vector3(-29,24,-19),new Vector3(29,24,19)};
            Vector3 target=close?director.residents[focus].root.position+Vector3.up*.8f:new Vector3(0,.8f,0);
            // Orthographic framing is unchanged by distance; stay within the existing URP shadow range.
            reviewCamera.transform.position=target+offsets[view]*.42f;reviewCamera.transform.LookAt(target);
            reviewCamera.orthographicSize=close?Mathf.Max(4.3f,4.8f/reviewCamera.aspect):Mathf.Max(12.4f,15.5f/reviewCamera.aspect);
        }
        void OnGUI()
        {
            GUI.matrix=Matrix4x4.Scale(Vector3.one*Scale);if(font)GUI.skin.font=font;
            GUI.skin.label.fontSize=16;GUI.skin.button.fontSize=14;
            GUI.skin.label.normal.textColor=new Color(.27f,.29f,.22f);
            Vector2 pointer=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
            if(hidden){Draw(new Rect(12,12,112,30),"메뉴 보기",pointer);GUI.matrix=Matrix4x4.identity;return;}
            float w=Screen.width/Scale;
            GUI.Label(new Rect(18,12,w-36,28),"Tiny Days · 봄날의 작은 농가");
            GUI.skin.label.fontSize=12;
            GUI.Label(new Rect(18,38,w-36,24),$"임시 생활 장면 · 이동과 머무르기 · {director.elapsed:F0}초");
            string[] labels={director.paused?"재생":"일시정지","처음부터",close?"전체 보기":"가까이 보기",views[view],$"주민 {focus+1} → 다음","메뉴 숨김"};
            for(int i=0;i<6;i++)Draw(ButtonRect(i),labels[i],pointer);
            GUI.matrix=Matrix4x4.identity;
        }
        void Draw(Rect r,string text,Vector2 pointer)
        {if(Event.current.type==EventType.Repaint)GUI.skin.button.Draw(r,new GUIContent(text),r.Contains(pointer),false,false,false);}
    }
}
