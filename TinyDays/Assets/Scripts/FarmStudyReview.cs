using UnityEngine;

namespace TinyDays.Review
{
    public sealed class FarmStudyReview : MonoBehaviour
    {
        public const int PanButton=0;
        public const int RotateButton=2;
        public FarmLifeDirector director;
        public Camera reviewCamera;
        public int view, focus;
        public bool close, hidden;
        Vector3 pivot;
        float yaw, pitch, orthographicSize;
        bool cameraReady, rotatingCamera, panningCamera;
        float desiredDistance, baseDistance;
        Vector3 previousPointer;
        FarmCameraOcclusion occlusion;
        bool following;
        Transform[] occlusionResidents;
        Transform occlusionResident;
        Font font;
        readonly string[] views={"기본 구도","반대 구도","왼쪽 구도","오른쪽 구도"};
        static readonly Vector3[] PresetOffsets={new Vector3(19,24,-29),new Vector3(-19,24,29),new Vector3(-29,24,-19),new Vector3(29,24,19)};
        void OnEnable(){font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);}
        void OnDisable(){if(font)Destroy(font);if(occlusion)occlusion.Restore();rotatingCamera=panningCamera=false;}
        void Start(){ResetCameraToPreset();}
        void OnApplicationFocus(bool focused){if(!focused){rotatingCamera=false;panningCamera=false;}}
        float Scale=>Mathf.Clamp(Screen.width/1100f,.65f,1.5f);
        Rect ButtonRect(int i)
        {
            float w=Screen.width/Scale;int columns=w<760?3:6;
            int rows=(6+columns-1)/columns;
            return new Rect(14+(i%columns)*(w-28)/columns,Screen.height/Scale-12-rows*32+(i/columns)*32,(w-28)/columns-5,28);
        }
        void Update()
        {
            bool overMenu=PointerOverMenu();
            if(Input.GetMouseButtonDown(PanButton)){panningCamera=!overMenu;previousPointer=Input.mousePosition;}
            if(Input.GetMouseButtonDown(RotateButton))rotatingCamera=!overMenu;
            if(Input.GetMouseButtonDown(PanButton))
            {
                Vector2 p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
                if(hidden){if(new Rect(12,12,112,30).Contains(p))hidden=false;}
                else for(int i=0;i<6;i++)if(ButtonRect(i).Contains(p))
                {
                    if(i==0)director.paused=!director.paused;
                    if(i==1)director.Restart();
                    if(i==2){close=!close;ResetCameraToPreset();}
                    if(i==3){view=(view+1)%4;ResetCameraToPreset();}
                    if(i==4){focus=(focus+1)%director.residents.Length;close=true;ResetCameraToPreset();}
                    if(i==5)hidden=true;
                    break;
                }
            }
            HandleCameraInput();
        }
        bool PointerOverMenu()
        {
            Vector2 p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
            if(hidden)return new Rect(12,12,112,30).Contains(p);
            if(new Rect(12,12,Mathf.Max(112,Screen.width/Scale-24),78).Contains(p))return true;
            for(int i=0;i<6;i++)if(ButtonRect(i).Contains(p))return true;
            return false;
        }
        void HandleCameraInput()
        {
            if(!cameraReady)return;
            if(Input.GetMouseButtonUp(PanButton))panningCamera=false;
            if(Input.GetMouseButtonUp(RotateButton))rotatingCamera=false;
            if(Input.GetKeyDown(KeyCode.Home)){view=0;close=false;ResetCameraToPreset();return;}
            if(rotatingCamera)
            {
                yaw=Mathf.Repeat(yaw+Input.GetAxisRaw("Mouse X")*12.5f,360f);
                pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*10f,-90f,75f);
            }
            if(panningCamera)
            {
                following=false;
                Vector3 delta=Input.mousePosition-previousPointer;
                pivot+=ScreenPan(Quaternion.Euler(pitch,yaw,0),delta,desiredDistance,reviewCamera.fieldOfView,reviewCamera.pixelHeight);
                previousPointer=Input.mousePosition;
            }
            float wheel=Input.mouseScrollDelta.y;
            if(!PointerOverMenu()&&Mathf.Abs(wheel)>.001f)desiredDistance=Mathf.Clamp(desiredDistance*Mathf.Exp(-wheel*.12f),baseDistance*.2f,baseDistance*1.5f);
            float height=(Input.GetKey(KeyCode.E)?1f:0f)-(Input.GetKey(KeyCode.Q)?1f:0f);
            if(height!=0){following=false;pivot.y+=height*5f*Time.unscaledDeltaTime;}
        }
        public static Vector3 ScreenPan(Quaternion rotation,Vector2 pixels,float distance,float fov,int pixelHeight)
        {
            float units=2*distance*Mathf.Tan(fov*.5f*Mathf.Deg2Rad)/Mathf.Max(1,pixelHeight);
            return -(rotation*Vector3.right*pixels.x+rotation*Vector3.up*pixels.y)*units;
        }
        void LateUpdate(){ApplyCamera();}
        void ApplyCamera()
        {
            if(!cameraReady)return;
            if(following&&Application.isPlaying)pivot=director.residents[focus].root.position+Vector3.up*.8f;
            Quaternion rotation=Quaternion.Euler(pitch,yaw,0);
            Vector3 outward=-(rotation*Vector3.forward);
            reviewCamera.transform.SetPositionAndRotation(pivot+outward*desiredDistance,rotation);
            if(Application.isPlaying)UpdateResidentOcclusion(Time.unscaledDeltaTime);
        }
        // Selection uses projection, not visibility: a resident behind a house is still eligible.
        // Radius is measured relative to the shorter screen dimension, independent of aspect.
        public static Transform SelectOcclusionResident(Camera camera,Transform[] residents,Transform current=null)
        {
            if(residents==null)return null;
            Transform best=null;float bestDistance=float.PositiveInfinity;
            foreach(var resident in residents)
            {
                if(!resident||!resident.gameObject.activeInHierarchy)continue;
                Vector3 p=camera.WorldToViewportPoint(resident.position+Vector3.up*.8f);
                if(p.z<=camera.nearClipPlane||p.z>camera.farClipPlane)continue;
                Vector2 delta=new Vector2((p.x-.5f)*camera.pixelWidth,(p.y-.5f)*camera.pixelHeight);
                float distance=delta.magnitude/Mathf.Max(1,Mathf.Min(camera.pixelWidth,camera.pixelHeight));
                // Small spatial hysteresis prevents target flicker at the boundary; no timed hold.
                if(resident==current&&distance<=.15f)return resident;
                if(distance<=.12f&&distance<bestDistance){best=resident;bestDistance=distance;}
            }
            return best;
        }
        public void UpdateResidentOcclusion(float dt)
        {
            occlusionResident=SelectOcclusionResident(reviewCamera,occlusionResidents,occlusionResident);
            occlusion.Fade(reviewCamera.transform.position,occlusionResident?occlusionResident.position+Vector3.up*.8f:Vector3.zero,dt,false,occlusionResident);
        }
        public void ResetCameraToPreset()
        {
            if(!reviewCamera || !director || director.residents==null || director.residents.Length==0)return;
            occlusion=GetComponent<FarmCameraOcclusion>();if(!occlusion)occlusion=gameObject.AddComponent<FarmCameraOcclusion>();
            occlusion.Initialize(transform);
            occlusionResidents=new Transform[director.residents.Length];
            for(int i=0;i<occlusionResidents.Length;i++)occlusionResidents[i]=director.residents[i].root;
            occlusionResident=null;
            following=close;
            reviewCamera.orthographic=false;reviewCamera.fieldOfView=40;reviewCamera.nearClipPlane=.03f;
            pivot=close?director.residents[Mathf.Clamp(focus,0,director.residents.Length-1)].root.position+Vector3.up*.8f:new Vector3(0,.8f,-2);
            Vector3 offset=PresetOffsets[Mathf.Clamp(view,0,PresetOffsets.Length-1)];
            Vector3 look=(-offset).normalized;
            yaw=Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg;
            pitch=-Mathf.Asin(look.y)*Mathf.Rad2Deg;
            orthographicSize=close?Mathf.Max(4.3f,4.8f/reviewCamera.aspect):Mathf.Max(12.4f,15.5f/reviewCamera.aspect);
            baseDistance=Mathf.Max(12.4f,15.5f/reviewCamera.aspect)/Mathf.Tan(20f*Mathf.Deg2Rad);
            desiredDistance=orthographicSize/Mathf.Tan(20f*Mathf.Deg2Rad);
            cameraReady=true;ApplyCamera();
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
            GUI.Label(new Rect(18,62,w-36,22),"휠 확대/축소 · 왼쪽 드래그 이동 · 가운데 드래그 회전 · Q/E 높이 · Home 기본 구도");
            GUI.matrix=Matrix4x4.identity;
        }
        void Draw(Rect r,string text,Vector2 pointer)
        {if(Event.current.type==EventType.Repaint)GUI.skin.button.Draw(r,new GUIContent(text),r.Contains(pointer),false,false,false);}
    }
}
