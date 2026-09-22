using System.Collections.Generic;
using System.Linq;
using TinyDays.Characters;
using UnityEngine;

namespace TinyDays.Review
{
    public sealed class AdultRabbitReview : MonoBehaviour
    {
        public ModularCharacter character;
        public Camera reviewCamera;
        public Transform poseRoot;
        float yaw=25,pitch=8,distance=5.6f;
        Vector3 pivot=new Vector3(0,1.06f,0),previous;
        bool pan,rotate;
        Font font;
        readonly Dictionary<Transform,Quaternion> rest=new Dictionary<Transform,Quaternion>();
        readonly Dictionary<Transform,Vector3> positions=new Dictionary<Transform,Vector3>();
        void OnEnable(){font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",16);}
        void OnDisable(){pan=rotate=false;if(font)Destroy(font);}
        void OnApplicationFocus(bool focused){if(!focused)pan=rotate=false;}
        void Start(){CachePose();Home();}
        void CachePose(){if(rest.Count>0)return;foreach(var t in character.bodyParts.SelectMany(s=>s.bones).Distinct()){rest[t]=t.localRotation;positions[t]=t.localPosition;}}
        public void SetPose(int pose)
        {
            CachePose();foreach(var kv in rest){kv.Key.localRotation=kv.Value;kv.Key.localPosition=positions[kv.Key];}
            if(pose==0)return;
            if(pose==2){QuadrupedProbe();return;}
            // Static deformation probes, not locomotion animations.
            Twist("UpperArm_L",Vector3.right,-45);Twist("UpperArm_R",Vector3.right,20);
            Twist("Forearm_L",Vector3.right,-55);Twist("Forearm_R",Vector3.right,-25);
            Twist("Thigh_L",Vector3.right,-30);Twist("Shin_L",Vector3.right,55);
        }
        Transform Bone(string name)=>rest.Keys.First(t=>t.name==name);
        void QuadrupedProbe()
        {
            var pelvis=Bone("Pelvis");var p=pelvis.position;p.y=.29f;pelvis.position=p;
            Twist("Spine",Vector3.right,75);Twist("Head",Vector3.right,-75);
            foreach(var side in new[]{"L","R"}){
                float x=side=="L"?-.20f:.20f;
                // Use actual imported side positions; FBX handedness can flip X.
                x=Mathf.Sign(Bone("Thigh_"+side).position.x)*.20f;
                SolveLimb("Thigh_"+side,"Shin_"+side,"Foot_"+side,new Vector3(x,.12f,-.08f),Vector3.forward);
                SolveLimb("UpperArm_"+side,"Forearm_"+side,"Hand_"+side,new Vector3(x*1.25f,.11f,.57f),Vector3.back);
            }
        }
        void SolveLimb(string upperName,string lowerName,string endName,Vector3 target,Vector3 pole)
        {
            var upper=Bone(upperName);var lower=Bone(lowerName);var end=Bone(endName);
            var a=upper.position;float l1=Vector3.Distance(a,lower.position),l2=Vector3.Distance(lower.position,end.position);
            var direction=(target-a).normalized;float d=Mathf.Clamp(Vector3.Distance(a,target),Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);
            float along=(l1*l1-l2*l2+d*d)/(2*d);float height=Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            var bend=Vector3.ProjectOnPlane(pole,direction).normalized;var joint=a+direction*along+bend*height;
            upper.rotation=Quaternion.FromToRotation(lower.position-a,joint-a)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(end.position-lower.position,a+direction*d-lower.position)*lower.rotation;
        }
        void Twist(string name,Vector3 axis,float angle)
        {
            var bone=rest.Keys.FirstOrDefault(t=>t.name==name);if(bone)bone.rotation=Quaternion.AngleAxis(angle,poseRoot.TransformDirection(axis))*bone.rotation;
        }
        public void Home(){yaw=25;pitch=8;distance=5.6f;pivot=new Vector3(0,1.06f,0);pan=rotate=false;Apply();}
        void Update()
        {
            bool ui=Input.mousePosition.y<145||Input.mousePosition.y>Screen.height-75;
            if(Input.GetMouseButtonDown(0)){pan=!ui;previous=Input.mousePosition;}
            if(Input.GetMouseButtonDown(1))rotate=!ui;
            if(!Input.GetMouseButton(0))pan=false;if(!Input.GetMouseButton(1))rotate=false;
            if(ui){pan=rotate=false;}
            if(rotate){yaw+=Input.GetAxis("Mouse X")*12.5f;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*10,-65,75);}
            if(pan){Vector3 delta=Input.mousePosition-previous;pivot+=FarmStudyReview.ScreenPan(reviewCamera.transform.rotation,new Vector2(delta.x,delta.y),distance,35,Screen.height);previous=Input.mousePosition;}
            if(!ui)distance=Mathf.Clamp(distance*Mathf.Exp(Input.mouseScrollDelta.y*.12f),1.6f,9);
            if(Input.GetKeyDown(KeyCode.Home))Home();Apply();
        }
        void Apply(){if(!reviewCamera)return;reviewCamera.transform.rotation=Quaternion.Euler(pitch,180+yaw,0);reviewCamera.transform.position=pivot-reviewCamera.transform.forward*distance;}
        void OnGUI()
        {
            if(font)GUI.skin.font=font;GUI.skin.label.normal.textColor=new Color(.2f,.22f,.25f);
            GUI.Label(new Rect(18,12,Screen.width-30,30),"Tiny Days · 성인 토끼 / 공용 의상 디자인 검토");
            GUI.Label(new Rect(18,40,Screen.width-30,26),"왼쪽 패닝 · 오른쪽 회전 · 휠 줌 · Home 기본 구도");
            GUILayout.BeginArea(new Rect(12,Screen.height-140,Screen.width-24,138));GUILayout.BeginHorizontal();
            string[] labels={"상의","바지","신발","목수건","배낭"};
            for(int i=0;i<character.wardrobe.Length;i++){
                var item=character.wardrobe[i];bool worn=character.IsEquipped(item.slot);
                if(GUILayout.Button(labels[i]+(worn?" ON":" OFF"),GUILayout.Height(34))){if(worn)character.Unequip(item.slot);else character.Equip(item);}}
            GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
            if(GUILayout.Button("전체 착용",GUILayout.Height(32)))character.Dress();
            if(GUILayout.Button("기본 몸",GUILayout.Height(32)))character.Undress();
            if(GUILayout.Button("정면",GUILayout.Height(32))){Home();yaw=0;pitch=0;Apply();}
            if(GUILayout.Button("뒷면",GUILayout.Height(32))){Home();yaw=180;pitch=0;Apply();}
            GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
            if(GUILayout.Button("기본 자세",GUILayout.Height(30)))SetPose(0);
            if(GUILayout.Button("관절 굽힘 확인",GUILayout.Height(30)))SetPose(1);
            if(GUILayout.Button("네 발 전환 검토 자세",GUILayout.Height(30)))SetPose(2);
            GUILayout.EndHorizontal();GUILayout.EndArea();
        }
    }
}
