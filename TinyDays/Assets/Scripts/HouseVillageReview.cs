using UnityEngine;

namespace TinyDays.Review
{
    public sealed class HouseVillageReview : MonoBehaviour
    {
        public Camera reviewCamera;
        public Transform[] houses;
        FarmCameraOcclusion occlusion;
        Vector3 pivot,previous;
        float yaw,pitch,distance,baseDistance;
        bool rotating,panning;
        Font font;
        public int view;
        void OnEnable(){font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);if(reviewCamera)SetView(0);}
        void Start(){SetView(0);}
        void OnDisable(){if(occlusion)occlusion.Restore();rotating=panning=false;if(font)Destroy(font);}
        void OnApplicationFocus(bool focused){if(!focused)rotating=panning=false;}
        public void SetView(int index)
        {
            view=index;occlusion=GetComponent<FarmCameraOcclusion>();if(!occlusion)occlusion=gameObject.AddComponent<FarmCameraOcclusion>();occlusion.Initialize(transform);
            pivot=index==0?new Vector3(0,1.5f,-.3f):houses[index-1].position+new Vector3(0,1.7f,-.3f);
            yaw=index==0?12:25;pitch=30;
            baseDistance=Mathf.Max(index==0?10:5,index==0?16/reviewCamera.aspect:5/reviewCamera.aspect)/Mathf.Tan(20*Mathf.Deg2Rad);
            distance=baseDistance;rotating=panning=false;Apply(false);
        }
        void Update()
        {
            bool ui=Input.mousePosition.y<58||Input.mousePosition.y>Screen.height-75;
            if(Input.GetMouseButtonDown(FarmStudyReview.PanButton)){panning=!ui;previous=Input.mousePosition;}
            if(Input.GetMouseButtonDown(FarmStudyReview.RotateButton))rotating=!ui;
            if(Input.GetMouseButtonUp(FarmStudyReview.PanButton))panning=false;if(Input.GetMouseButtonUp(FarmStudyReview.RotateButton))rotating=false;
            if(Input.GetKeyDown(KeyCode.Home)){SetView(0);return;}
            if(rotating){yaw=Mathf.Repeat(yaw+Input.GetAxisRaw("Mouse X")*12.5f,360);pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*10,-90,75);}
            if(panning){pivot+=FarmStudyReview.ScreenPan(Quaternion.Euler(pitch,yaw,0),Input.mousePosition-previous,distance,reviewCamera.fieldOfView,reviewCamera.pixelHeight);previous=Input.mousePosition;}
            if(!ui)distance=Mathf.Clamp(distance*Mathf.Exp(-Input.mouseScrollDelta.y*.12f),baseDistance*.2f,baseDistance*1.5f);
            pivot.y+=((Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0))*5*Time.unscaledDeltaTime;
        }
        void LateUpdate(){Apply(true);}
        void Apply(bool fade)
        {
            if(!reviewCamera)return;var q=Quaternion.Euler(pitch,yaw,0);reviewCamera.transform.SetPositionAndRotation(pivot-q*Vector3.forward*distance,q);
            if(fade&&Application.isPlaying)occlusion.Fade(reviewCamera.transform.position,Vector3.zero,Time.unscaledDeltaTime,false,false);
        }
        void OnGUI()
        {
            if(font)GUI.skin.font=font;GUI.skin.label.fontSize=16;GUI.skin.label.normal.textColor=new Color(.27f,.29f,.22f);
            GUI.Label(new Rect(18,12,Screen.width-36,26),"Tiny Days · 집 3종과 작은 앞마당 · 디자인 검토");GUI.skin.label.fontSize=12;
            GUI.Label(new Rect(18,40,Screen.width-36,25),"왼쪽 드래그 패닝 · 가운데 드래그 회전 · 휠 줌 · Q/E 높이 · Home 전체 보기");
            string[] labels={"전체 보기","목조집","노란 회벽집","작은 시골집"};float w=(Screen.width-32)/4f;
            for(int i=0;i<4;i++)if(GUI.Button(new Rect(16+i*w,Screen.height-46,w-6,32),labels[i]))SetView(i);
        }
    }
}
