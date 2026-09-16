using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TinyDays.Review
{
    public sealed class FarmStudyReview : MonoBehaviour
    {
        public const int PanButton=0;
        public const int RotateButton=2;
        public const float MinZoomDistance=1f;
        public const float ClosePanReferenceDistance=8f;
        public FarmLifeDirector director;
        public Camera reviewCamera;
        public UniversalRenderPipelineAsset shadowLow,shadowBalanced,shadowHigh;
        public int view, focus;
        public bool close, hidden;
        public int selected=-1;
        public bool residentListOpen;
        public bool IsFollowing=>following;
        bool pointerHeld;
        Vector2 pressPosition;
        int pressedResident=-1,lastClickedResident=-1;
        double lastClickTime=double.NegativeInfinity;
        Vector3 pivot;
        // Player-controlled framing relative to a followed resident.
        Vector3 followOffset;
        float yaw, pitch, orthographicSize;
        bool cameraReady, rotatingCamera, panningCamera;
        float desiredDistance, baseDistance;
        Vector3 previousPointer;
        FarmCameraOcclusion occlusion;
        ShadowQualitySettings shadowQuality;
        bool following;
        bool settingsOpen;
        bool lightingOpen;
        readonly LightingColorPicker colorPicker=new LightingColorPicker();
        Rect LightingToggle()=>new Rect(14,94,160,28);
        Rect LightingPanel()=>new Rect(14,126,366,360);
        string dayMinutesInput="5";
        bool invalidDayMinutes;
        Rect DayApply()=>new Rect(300,326,72,26);
        Rect DayPreset(int i)=>new Rect(22+i*88,358,82,26);
        Rect RateButton(int i)=>new Rect(22+i*88,410,82,26);
        Rect AutomaticRow()=>new Rect(22,292,350,28);
        Rect LightingRow(int i)=>new Rect(22,158+i*32,174,28);
        Rect LightingSwatch(int i,int k)=>new Rect(205+k*56,160+i*32,42,24);
        Font font;
        readonly string[] views={"기본 구도","반대 구도","왼쪽 구도","오른쪽 구도"};
        static readonly Vector3[] PresetOffsets={new Vector3(19,24,-29),new Vector3(-19,24,29),new Vector3(-29,24,-19),new Vector3(29,24,19)};
        void OnEnable(){font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);}
        void OnDisable(){colorPicker.Dispose();if(font)Destroy(font);if(occlusion)occlusion.Restore();CancelPointer();rotatingCamera=false;}
        void Start()
        {
            shadowQuality=GetComponent<ShadowQualitySettings>();if(!shadowQuality)shadowQuality=gameObject.AddComponent<ShadowQualitySettings>();
            shadowQuality.Configure(shadowLow,shadowBalanced,shadowHigh);ResetCameraToPreset();
        }
        void OnApplicationFocus(bool focused){if(!focused){rotatingCamera=false;CancelPointer();}}
        float Scale=>Mathf.Clamp(Screen.width/1100f,.65f,1.5f);
        Rect ButtonRect(int i)
        {
            float w=Screen.width/Scale;int columns=w<760?3:6;
            int rows=(6+columns-1)/columns;
            return new Rect(14+(i%columns)*(w-28)/columns,Screen.height/Scale-12-rows*32+(i/columns)*32,(w-28)/columns-5,28);
        }
        void Update()
        {
            if(colorPicker.EscapeConsumed){colorPicker.EscapeConsumed=false;return;}
            if(colorPicker.Open){CancelPointer();rotatingCamera=false;if(Input.GetKeyDown(KeyCode.Escape))colorPicker.Cancel();return;}
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                settingsOpen=!settingsOpen;CancelPointer();rotatingCamera=false;return;
            }
            if(settingsOpen)
            {
                if(Input.GetMouseButtonDown(PanButton)&&shadowQuality)
                {
                    Vector2 p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
                    for(int i=0;i<3;i++)if(ShadowRow(i).Contains(p)){shadowQuality.Apply(i);break;}
                }
                return;
            }
            bool overMenu=PointerOverMenu();
            if(Input.GetMouseButtonDown(RotateButton))rotatingCamera=!overMenu;
            if(Input.GetMouseButtonDown(PanButton))
            {
                Vector2 p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
                bool handled=overMenu;
                if(!hidden)
                {
                    if(LightingToggle().Contains(p))lightingOpen=!lightingOpen;
                    else if(lightingOpen&&AutomaticRow().Contains(p))GetComponent<FarmLightingStudy>()?.ResumeClock();
                    else if(lightingOpen&&DayApply().Contains(p))
                    {invalidDayMinutes=!director.Playback.SetDayMinutes(dayMinutesInput);if(!invalidDayMinutes)GUI.FocusControl(null);}
                    else if(lightingOpen)for(int i=0;i<4;i++)
                    {
                        if(DayPreset(i).Contains(p)){dayMinutesInput=FarmPlaybackSettings.SuggestedMinutes[i].ToString();director.Playback.SetDayMinutes(dayMinutesInput);invalidDayMinutes=false;GUI.FocusControl(null);break;}
                        if(RateButton(i).Contains(p)){director.Playback.SetRate(i);GUI.FocusControl(null);break;}
                        var light=GetComponent<FarmLightingStudy>();
                        if(LightingRow(i).Contains(p)){light?.Apply(i);break;}
                        for(int k=0;k<3;k++)if(LightingSwatch(i,k).Contains(p)&&light){colorPicker.Begin(light,i,k);CancelPointer();rotatingCamera=false;return;}
                    }
                }
                    if(hidden){if(ButtonRect(5).Contains(p))hidden=false;}
                else for(int i=0;i<6;i++)if(ButtonRect(i).Contains(p))
                {
                    if(i==0)director.paused=!director.paused;
                    if(i==1){director.Restart();GetComponent<FarmLightingStudy>()?.RestartClock();}
                    if(i==2)ShowOverview();
                    if(i==3){view=(view+1)%4;followOffset=Vector3.zero;ResetCameraToPreset();}
                    if(i==4)residentListOpen=!residentListOpen;
                    if(i==5){hidden=true;residentListOpen=false;}
                    break;
                }
                if(!hidden&&residentListOpen)
                    for(int i=0;i<director.residents.Length;i++)if(ListRow(i).Contains(p)){FocusResident(i);handled=true;break;}
                if(!handled)BeginPointer(Input.mousePosition);
                else lastClickedResident=-1;
            }
            if(pointerHeld&&Input.GetMouseButton(PanButton))MovePointer(Input.mousePosition);
            if(Input.GetMouseButtonUp(PanButton))EndPointer(Input.mousePosition,Time.unscaledTimeAsDouble);
            HandleCameraInput();
        }
        bool ValidResident(int i)=>director&&director.residents!=null&&i>=0&&i<director.residents.Length&&director.residents[i].root&&director.residents[i].root.gameObject.activeInHierarchy;
        public void FocusResident(int index)
        {
            if(!ValidResident(index))return;
            selected=focus=index;close=true;followOffset=Vector3.zero;residentListOpen=false;lastClickedResident=-1;
            ResetCameraToPreset();
        }
        public void ReleaseFocus(){following=false;close=false;}
        public void ShowOverview(){CancelPointer();ReleaseFocus();followOffset=Vector3.zero;selected=-1;lastClickedResident=-1;residentListOpen=false;view=0;ResetCameraToPreset();}
        public void ClickResident(int index,double time)
        {
            if(!ValidResident(index)){lastClickedResident=-1;return;}
            selected=index;
            if(residentListOpen||(lastClickedResident==index&&time-lastClickTime<=.3&&time>=lastClickTime))
            {FocusResident(index);return;}
            lastClickedResident=index;lastClickTime=time;
        }
        public void BeginPointer(Vector2 position)
        {
            pointerHeld=true;panningCamera=false;pressPosition=previousPointer=position;
            pressedResident=PickResident(position);
        }
        public void MovePointer(Vector2 position)
        {
            if(!pointerHeld)return;
            if(!panningCamera&&(position-pressPosition).sqrMagnitude>=36){panningCamera=true;lastClickedResident=-1;}
            if(!panningCamera)return;
            Vector3 movement=ScreenPan(Quaternion.Euler(pitch,yaw,0),position-(Vector2)previousPointer,PanDistance(desiredDistance),reviewCamera.fieldOfView,reviewCamera.pixelHeight);
            if(following)followOffset+=movement;else pivot+=movement;
            previousPointer=position;
        }
        public void EndPointer(Vector2 position,double time)
        {
            if(!pointerHeld)return;
            MovePointer(position);
            if(!panningCamera&&!PointerOverMenu()&&pressedResident>=0)ClickResident(pressedResident,time);
            else lastClickedResident=-1;
            pointerHeld=panningCamera=false;pressedResident=-1;
        }
        void CancelPointer(){pointerHeld=panningCamera=false;pressedResident=lastClickedResident=-1;}
        public int PickResident(Vector2 screenPosition)
        {
            if(!cameraReady||!reviewCamera.pixelRect.Contains(screenPosition))return -1;
            Vector2 p=new Vector2(screenPosition.x,Screen.height-screenPosition.y)/Scale;
            if(!hidden&&residentListOpen)
                for(int i=0;i<director.residents.Length;i++)if(NameRect(i,out Rect rect)&&rect.Contains(p))return i;
            var hit=occlusion.PickResident(reviewCamera.ScreenPointToRay(screenPosition),reviewCamera.farClipPlane);
            if(hit)for(int i=0;i<director.residents.Length;i++)if(ValidResident(i)&&hit.IsChildOf(director.residents[i].root))return i;
            return -1;
        }
        Rect ListPanel()
        {
            float width=Mathf.Min(210,Screen.width/Scale-28),height=30+director.residents.Length*28;
            return new Rect(Screen.width/Scale-width-14,Mathf.Max(94,ButtonRect(0).y-height-8),width,height);
        }
        Rect ListRow(int i){var r=ListPanel();return new Rect(r.x+6,r.y+26+i*28,r.width-12,26);}
        Rect SettingsPanel()
        {
            float width=Mathf.Min(330,Screen.width/Scale-28),height=216;
            return new Rect((Screen.width/Scale-width)*.5f,(Screen.height/Scale-height)*.5f,width,height);
        }
        Rect ShadowRow(int i){var r=SettingsPanel();return new Rect(r.x+16,r.y+58+i*32,r.width-32,28);}
        bool NameRect(int i,out Rect rect)
        {
            rect=default;if(!ValidResident(i))return false;
            var resident=director.residents[i];
            // Use the current visual bounds so future models need no fixed nameplate height.
            Vector3 top=resident.root.position+Vector3.up*1.5f;
            if(resident.visual)
            {
                var renderer=resident.visual.GetComponentInChildren<SkinnedMeshRenderer>();
                if(renderer)top=new Vector3(renderer.bounds.center.x,renderer.bounds.max.y+.12f,renderer.bounds.center.z);
            }
            var p=reviewCamera.WorldToScreenPoint(top);
            if(p.z<reviewCamera.nearClipPlane||p.z>reviewCamera.farClipPlane||!reviewCamera.pixelRect.Contains(p))return false;
            rect=new Rect(Mathf.Clamp(p.x/Scale-35,0,Screen.width/Scale-70),Mathf.Clamp((Screen.height-p.y)/Scale-22,0,Screen.height/Scale-24),70,24);return true;
        }
        bool PointerOverMenu()
        {
            if(settingsOpen||colorPicker.Open)return true;
            Vector2 p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
            if(hidden)return ButtonRect(5).Contains(p);
            if(LightingToggle().Contains(p)||(lightingOpen&&LightingPanel().Contains(p)))return true;
            if(residentListOpen&&ListPanel().Contains(p))return true;
            if(new Rect(12,12,Mathf.Max(112,Screen.width/Scale-24),78).Contains(p))return true;
            for(int i=0;i<6;i++)if(ButtonRect(i).Contains(p))return true;
            return false;
        }
        void HandleCameraInput()
        {
            if(!cameraReady)return;
            if(!hidden&&lightingOpen&&GUI.GetNameOfFocusedControl()=="DayMinutesInput")return;
            if(Input.GetMouseButtonUp(RotateButton))rotatingCamera=false;
            if(Input.GetKeyDown(KeyCode.Home)){CancelPointer();ShowOverview();return;}
            if(rotatingCamera)
            {
                yaw=Mathf.Repeat(yaw+Input.GetAxisRaw("Mouse X")*12.5f,360f);
                pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*10f,-90f,75f);
            }
            float wheel=Input.mouseScrollDelta.y;
            if(!PointerOverMenu()&&Mathf.Abs(wheel)>.001f)desiredDistance=ClampZoomDistance(desiredDistance*Mathf.Exp(-wheel*.12f),baseDistance);
            float height=(Input.GetKey(KeyCode.E)?1f:0f)-(Input.GetKey(KeyCode.Q)?1f:0f);
            if(height!=0)
            {
                float movement=height*5f*Time.unscaledDeltaTime;
                if(following)followOffset.y+=movement;else pivot.y+=movement;
            }
        }
        public static Vector3 ScreenPan(Quaternion rotation,Vector2 pixels,float distance,float fov,int pixelHeight)
        {
            float units=2*distance*Mathf.Tan(fov*.5f*Mathf.Deg2Rad)/Mathf.Max(1,pixelHeight);
            return -(rotation*Vector3.right*pixels.x+rotation*Vector3.up*pixels.y)*units;
        }
        public static float ClampZoomDistance(float distance,float baseDistance)=>Mathf.Clamp(distance,MinZoomDistance,baseDistance*1.5f);
        public static float PanDistance(float zoomDistance)=>Mathf.Max(zoomDistance,ClosePanReferenceDistance);
        void LateUpdate(){ApplyCamera();}
        void ApplyCamera()
        {
            if(!cameraReady)return;
            if(following&&!ValidResident(focus)){ReleaseFocus();followOffset=Vector3.zero;selected=-1;}
            if(following&&Application.isPlaying)pivot=director.residents[focus].root.position+Vector3.up*.8f+followOffset;
            Quaternion rotation=Quaternion.Euler(pitch,yaw,0);
            Vector3 outward=-(rotation*Vector3.forward);
            reviewCamera.transform.SetPositionAndRotation(pivot+outward*desiredDistance,rotation);
            if(Application.isPlaying)UpdateResidentOcclusion(Time.unscaledDeltaTime);
        }
        // Historical v0.22 helper, retained for compatibility with archived editor checks only.
        // Runtime selection and occlusion no longer use screen-center proximity.
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
            var occlusionResident=following&&ValidResident(focus)?director.residents[focus].root:null;
            occlusion.Fade(reviewCamera.transform.position,occlusionResident?occlusionResident.position+Vector3.up*.8f:Vector3.zero,dt,false,occlusionResident);
        }
        public void ResetCameraToPreset()
        {
            if(!reviewCamera || !director || director.residents==null || director.residents.Length==0)return;
            occlusion=GetComponent<FarmCameraOcclusion>();if(!occlusion)occlusion=gameObject.AddComponent<FarmCameraOcclusion>();
            following=close&&ValidResident(focus);close=following;
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
            if(colorPicker.Open){colorPicker.Draw(Screen.width/Scale,Screen.height/Scale);GUI.matrix=Matrix4x4.identity;return;}
            DrawClock(GetComponent<FarmLightingStudy>());
            if(settingsOpen)
            {
                var panel=SettingsPanel();GUI.Box(panel,"설정");
                GUI.Label(new Rect(panel.x+16,panel.y+28,panel.width-32,24),"그림자 품질");
                string[] choices={"낮음 · 선명한 경계 · 낮은 성능 부담","기본 · 부드러운 경계 · 균형","높음 · 더 부드러운 경계 · 높은 성능 부담"};
                int current=shadowQuality?shadowQuality.Selected:1;
                for(int i=0;i<3;i++)Draw(ShadowRow(i),(current==i?"● ":"")+choices[i],pointer);
                GUI.Label(new Rect(panel.x+16,panel.y+184,panel.width-32,24),"Esc를 누르면 설정을 닫습니다.");
                GUI.matrix=Matrix4x4.identity;return;
            }
            if(hidden){Draw(ButtonRect(5),"메뉴 보기",pointer);GUI.matrix=Matrix4x4.identity;return;}
            float w=Screen.width/Scale;
            var lighting=GetComponent<FarmLightingStudy>();
            if(lighting&&lighting.selected==3)GUI.skin.label.normal.textColor=new Color(.91f,.92f,.96f);
            Draw(LightingToggle(),lightingOpen?"시간대 비교 닫기":"시간대 비교",pointer);
            if(lightingOpen)
            {
                GUI.Box(LightingPanel(),"");
                GUI.skin.label.normal.textColor=Color.white;GUI.skin.label.fontSize=12;
                for(int k=0;k<3;k++)GUI.Label(new Rect(203+k*56,130,56,24),LightingColors.Labels[k]);
                for(int i=0;i<4;i++)
                {
                    Draw(LightingRow(i),(lighting&&!lighting.Automatic&&lighting.selected==i?"● ":"")+FarmLightingStudy.Names[i],pointer);
                    for(int k=0;k<3;k++)if(lighting)LightingColorPicker.Swatch(LightingSwatch(i,k),lighting.Colors.Get(i,k));
                }
                if(lighting){int minutes=Mathf.FloorToInt(lighting.Hour*60)%1440;Draw(AutomaticRow(),(lighting.Automatic?"● ":"")+"자동 순환 · "+(minutes/60).ToString("00")+":"+(minutes%60).ToString("00"),pointer);}
                GUI.Label(new Rect(22,328,178,24),"하루 길이 (1배속 기준·분)");
                GUI.skin.textField.fontSize=14;GUI.SetNextControlName("DayMinutesInput");
                dayMinutesInput=GUI.TextField(new Rect(202,326,90,26),dayMinutesInput,4);
                Draw(DayApply(),"적용",pointer);
                for(int i=0;i<4;i++)Draw(DayPreset(i),FarmPlaybackSettings.SuggestedMinutes[i]+"분",pointer);
                GUI.Label(new Rect(22,386,350,24),"재생속도 · 주민과 낮밤 함께");
                for(int i=0;i<4;i++)Draw(RateButton(i),(director.Playback.Rate==FarmPlaybackSettings.Rates[i]?"● ":"")+FarmPlaybackSettings.Rates[i].ToString("0.#")+"×",pointer);
                GUI.Label(new Rect(22,438,350,22),invalidDayMinutes?"1~120 사이의 정수(분)를 입력해주세요.":$"적용: {director.Playback.DayMinutes}분 · {director.Playback.Rate:0.#}× · 실제 하루 {director.Playback.DayMinutes/director.Playback.Rate:0.##}분");
                GUI.Label(new Rect(22,460,350,22),"시간 고정 중에는 주민에게만 배속이 적용됩니다.");
            }
            GUI.skin.label.normal.textColor=lighting&&lighting.selected!=1?new Color(.94f,.94f,.90f):new Color(.27f,.29f,.22f);
            GUI.Label(new Rect(18,12,w-36,28),"Tiny Days · 봄날의 작은 농가");
            GUI.skin.label.fontSize=12;
            GUI.Label(new Rect(18,38,w-36,24),$"임시 생활 장면 · 이동과 머무르기 · {director.elapsed:F0}초");
            string[] labels={director.paused?"재생":"일시정지","처음부터","전체 보기",views[view],"주민 목록","메뉴 숨김"};
            for(int i=0;i<6;i++)Draw(ButtonRect(i),labels[i],pointer);
            GUI.Label(new Rect(18,62,w-36,22),"클릭 선택 · 더블클릭 따라보기 · 왼쪽 드래그 이동 · 가운데 회전 · 휠 줌 · Q/E 높이 · Home 전체");
            for(int i=0;i<director.residents.Length;i++)if((residentListOpen||selected==i)&&NameRect(i,out Rect rect))
                Draw(rect,(selected==i?"● ":"")+"주민 "+(i+1),pointer);
            if(residentListOpen)
            {
                GUI.Box(ListPanel(),"주민 목록");
                for(int i=0;i<director.residents.Length;i++)Draw(ListRow(i),(selected==i?"● ":"")+"주민 "+(i+1),pointer);
            }
            GUI.matrix=Matrix4x4.identity;
        }
        void DrawClock(FarmLightingStudy lighting)
        {
            if(!lighting)return;
            float width=Screen.width/Scale;
            var rect=new Rect((width-132)*.5f,8,132,70);
            int minutes=Mathf.FloorToInt(lighting.Hour*60f)%1440;
            var alignment=GUI.skin.label.alignment;var size=GUI.skin.label.fontSize;var color=GUI.skin.label.normal.textColor;
            GUI.skin.label.alignment=TextAnchor.MiddleCenter;
            DrawClockText(new Rect(rect.x,rect.y+2,rect.width,30),(minutes/60).ToString("00")+":"+(minutes%60).ToString("00"),20,Color.white);
            DrawClockText(new Rect(rect.x,rect.y+30,rect.width,24),"Day "+lighting.Day,11,new Color(.90f,.91f,.84f));
            GUI.skin.label.alignment=alignment;GUI.skin.label.fontSize=size;GUI.skin.label.normal.textColor=color;
        }
        void DrawClockText(Rect rect,string text,int size,Color color)
        {
            GUI.skin.label.fontSize=size;
            GUI.skin.label.normal.textColor=new Color(.06f,.07f,.06f,.68f);
            GUI.Label(new Rect(rect.x+1.5f,rect.y+1.5f,rect.width,rect.height),text);
            GUI.skin.label.normal.textColor=color;
            GUI.Label(rect,text);
        }
        void Draw(Rect r,string text,Vector2 pointer)
        {if(Event.current.type==EventType.Repaint)GUI.skin.button.Draw(r,new GUIContent(text),r.Contains(pointer),false,false,false);}
    }
}
