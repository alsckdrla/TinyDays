using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TinyDays.Review
{
    public sealed class FarmStudyReview : MonoBehaviour
    {
        public const int PanButton=0;
        public const int RotateButton=1;
        public const int HeightDragButton=2;
        public const float MinZoomDistance=1f;
        public const float ClosePanReferenceDistance=8f;
        public const float ControlHeight=42f;
        public const float MenuWrapScreenWidth=740f;
        public const float KeyboardPanMultiplier=.5f;
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
        float yaw, pitch, orthographicSize, heightOffset;
        bool cameraReady, rotatingCamera, panningCamera, rightRotateHeld, heightDragHeld;
        float desiredDistance, baseDistance;
        Vector3 previousPointer, previousHeightPointer;
        FarmCameraOcclusion occlusion;
        ShadowQualitySettings shadowQuality;
        bool following;
        bool settingsOpen;
        bool lightingOpen;
        float lightingScroll;
        readonly LightingColorPicker colorPicker=new LightingColorPicker();
        const float ControlGap=6,LightingContentHeight=590;
        Rect LightingToggle()=>new Rect(14,102,200,ControlHeight);
        Rect LightingPanel()
        {
            float width=Mathf.Min(400,Screen.width/Scale-28),top=154;
            float available=ButtonRect(0).y-top-10;
            return new Rect(14,top,width,Mathf.Clamp(available,120,620));
        }
        string dayMinutesInput="5";
        bool invalidDayMinutes;
        Rect DayApply()=>new Rect(286,336,LightingPanel().width-294,ControlHeight);
        Rect DayInput()=>new Rect(178,336,100,ControlHeight);
        Rect DayPreset(int i){float w=(LightingPanel().width-34)/4;return new Rect(8+i*(w+6),388,w,ControlHeight);}
        Rect RateButton(int i){float w=(LightingPanel().width-34)/4;return new Rect(8+i*(w+6),472,w,ControlHeight);}
        Rect AutomaticRow()=>new Rect(8,254,LightingPanel().width-16,ControlHeight);
        Rect LightingRow(int i)=>new Rect(8,48+i*50,LightingPanel().width-174,ControlHeight);
        Rect LightingSwatch(int i,int k)=>new Rect(LightingPanel().width-158+k*50,48+i*50,42,ControlHeight);
        Font font;
        readonly string[] views={"기본 구도","반대 구도","왼쪽 구도","오른쪽 구도"};
        static readonly Vector3[] PresetOffsets={new Vector3(19,24,-29),new Vector3(-19,24,29),new Vector3(-29,24,-19),new Vector3(29,24,19)};
        void OnEnable(){font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);}
        void OnDisable(){colorPicker.Dispose();if(font)Destroy(font);if(occlusion)occlusion.Restore();CancelPointer();StopCameraDrags();}
        void Start()
        {
            shadowQuality=GetComponent<ShadowQualitySettings>();if(!shadowQuality)shadowQuality=gameObject.AddComponent<ShadowQualitySettings>();
            shadowQuality.Configure(shadowLow,shadowBalanced,shadowHigh);ResetCameraToPreset();
        }
        void OnApplicationFocus(bool focused){if(!focused){StopCameraDrags();CancelPointer();}}
        float Scale=>Mathf.Clamp(Screen.width/1100f,.65f,1.5f);
        Rect ButtonRect(int i)
        {
            float w=Screen.width/Scale;int columns=MenuColumns(Screen.width);
            int rows=(6+columns-1)/columns;
            float width=(w-28-(columns-1)*ControlGap)/columns;
            float top=Screen.height/Scale-12-(rows*ControlHeight+(rows-1)*ControlGap);
            return new Rect(14+(i%columns)*(width+ControlGap),top+(i/columns)*(ControlHeight+ControlGap),width,ControlHeight);
        }
        public static int MenuColumns(float screenWidth)=>screenWidth<MenuWrapScreenWidth?3:6;
        public static float ClampLightingScroll(float requested,float viewportHeight)=>Mathf.Clamp(requested,0,Mathf.Max(0,LightingContentHeight-viewportHeight));
        Vector2 LightingPointer(Vector2 p)=>p-LightingPanel().position+Vector2.up*lightingScroll;
        void ClampLightingScroll(){lightingScroll=ClampLightingScroll(lightingScroll,LightingPanel().height);}
        void Update()
        {
            if(colorPicker.EscapeConsumed){colorPicker.EscapeConsumed=false;return;}
            if(colorPicker.Open){CancelPointer();StopCameraDrags();if(Input.GetKeyDown(KeyCode.Escape))colorPicker.Cancel();return;}
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                settingsOpen=!settingsOpen;CancelPointer();StopCameraDrags();return;
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
            if(lightingOpen)
            {
                ClampLightingScroll();
                Vector2 p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
                if(LightingPanel().Contains(p)&&Mathf.Abs(Input.mouseScrollDelta.y)>.001f)
                    lightingScroll=ClampLightingScroll(lightingScroll-Input.mouseScrollDelta.y*36,LightingPanel().height);
            }
            bool overMenu=PointerOverMenu();
            if(Input.GetMouseButtonDown(RotateButton)&&!overMenu&&!following)BeginOverviewOrbitAtScreenCenter();
            if(Input.GetMouseButtonDown(RotateButton))rightRotateHeld=!overMenu;
            if(Input.GetMouseButtonUp(RotateButton))rightRotateHeld=false;
            if(Input.GetMouseButtonDown(HeightDragButton)){heightDragHeld=!overMenu;previousHeightPointer=Input.mousePosition;}
            if(Input.GetMouseButtonUp(HeightDragButton))heightDragHeld=false;
            rotatingCamera=rightRotateHeld;
            if(heightDragHeld&&Input.GetMouseButton(HeightDragButton))
            {
                float delta=((Vector2)Input.mousePosition-(Vector2)previousHeightPointer).y;
                ElevateCamera(MouseHeightDelta(delta,PanDistance(desiredDistance),reviewCamera.fieldOfView,reviewCamera.pixelHeight));
                previousHeightPointer=Input.mousePosition;
            }
            if(Input.GetMouseButtonDown(PanButton))
            {
                Vector2 p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/Scale;
                bool handled=overMenu;
                if(!hidden)
                {
                    if(LightingToggle().Contains(p)){lightingOpen=!lightingOpen;if(!lightingOpen)lightingScroll=0;else ClampLightingScroll();}
                    else if(lightingOpen&&LightingPanel().Contains(p)&&AutomaticRow().Contains(LightingPointer(p)))GetComponent<FarmLightingStudy>()?.ResumeClock();
                    else if(lightingOpen&&LightingPanel().Contains(p)&&DayApply().Contains(LightingPointer(p)))
                    {invalidDayMinutes=!director.Playback.SetDayMinutes(dayMinutesInput);if(!invalidDayMinutes)GUI.FocusControl(null);}
                    else if(lightingOpen)for(int i=0;i<4;i++)
                    {
                        Vector2 local=LightingPointer(p);
                        if(LightingPanel().Contains(p)&&DayPreset(i).Contains(local)){dayMinutesInput=FarmPlaybackSettings.SuggestedMinutes[i].ToString();director.Playback.SetDayMinutes(dayMinutesInput);invalidDayMinutes=false;GUI.FocusControl(null);break;}
                        if(LightingPanel().Contains(p)&&RateButton(i).Contains(local)){director.Playback.SetRate(i);GUI.FocusControl(null);break;}
                        var light=GetComponent<FarmLightingStudy>();
                        if(LightingPanel().Contains(p)&&LightingRow(i).Contains(local)){light?.Apply(i);break;}
                        for(int k=0;k<3;k++)if(LightingPanel().Contains(p)&&LightingSwatch(i,k).Contains(local)&&light){colorPicker.Begin(light,i,k);CancelPointer();StopCameraDrags();return;}
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
                if(overMenu)StopCameraDrags();
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
            Vector3 movement=GroundScreenPan(reviewCamera.transform.rotation,position-(Vector2)previousPointer,PanDistance(desiredDistance),reviewCamera.fieldOfView,reviewCamera.pixelHeight);
            ApplyPlanarPan(movement);previousPointer=position;
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
        void StopCameraDrags(){rightRotateHeld=heightDragHeld=rotatingCamera=false;}
        public static bool RotationActive(bool rightHeld)=>rightHeld;
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
            float width=Mathf.Min(390,Screen.width/Scale-28),height=268;
            return new Rect((Screen.width/Scale-width)*.5f,(Screen.height/Scale-height)*.5f,width,height);
        }
        Rect ShadowRow(int i){var r=SettingsPanel();return new Rect(r.x+16,r.y+58+i*(ControlHeight+ControlGap),r.width-32,ControlHeight);}
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
            if(Input.GetKeyDown(KeyCode.Home)){CancelPointer();ShowOverview();return;}
            if(rotatingCamera)
            {
                yaw=Mathf.Repeat(yaw+Input.GetAxisRaw("Mouse X")*12.5f,360f);
                pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*10f,-90f,75f);
            }
            float wheel=Input.mouseScrollDelta.y;
            if(!PointerOverMenu()&&Mathf.Abs(wheel)>.001f)desiredDistance=ZoomFromWheel(desiredDistance,wheel,baseDistance);
            float height=(Input.GetKey(KeyCode.E)?1f:0f)-(Input.GetKey(KeyCode.Q)?1f:0f);
            if(height!=0)
            {
                ElevateCamera(height*5f*Time.unscaledDeltaTime);
            }
            float horizontal=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1f:0f)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1f:0f);
            float vertical=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1f:0f)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1f:0f);
            if(horizontal!=0||vertical!=0)
            {
                Vector3 movement=KeyboardPan(reviewCamera.transform.rotation,new Vector2(horizontal,vertical),desiredDistance)*Time.unscaledDeltaTime;
                ApplyPlanarPan(movement);
            }
        }
        public static Vector3 ScreenPan(Quaternion rotation,Vector2 pixels,float distance,float fov,int pixelHeight)
        {
            float units=2*distance*Mathf.Tan(fov*.5f*Mathf.Deg2Rad)/Mathf.Max(1,pixelHeight);
            return -(rotation*Vector3.right*pixels.x+rotation*Vector3.up*pixels.y)*units;
        }
        public static Vector3 GroundScreenPan(Quaternion rotation,Vector2 pixels,float distance,float fov,int pixelHeight)
        {
            return Vector3.ProjectOnPlane(ScreenPan(rotation,pixels,distance,fov,pixelHeight),Vector3.up);
        }
        public static float MouseHeightDelta(float verticalPixels,float distance,float fov,int pixelHeight)
        {
            return verticalPixels*2*distance*Mathf.Tan(fov*.5f*Mathf.Deg2Rad)/Mathf.Max(1,pixelHeight);
        }
        // Move the camera like an elevator: preserve its world-up motion and current view angle.
        // Overview rebinds the orbit target to the new center-ray surface when it is within zoom limits.
        void ElevateCamera(float amount)
        {
            if(Mathf.Abs(amount)<1e-6f)return;
            if(!following&&occlusion&&reviewCamera)
            {
                Vector3 position=reviewCamera.transform.position+Vector3.up*amount;
                var ray=new Ray(position,reviewCamera.transform.forward);
                if(occlusion.TryPickStaticSurface(ray,reviewCamera.farClipPlane,out Vector3 target))
                {
                    float distance=Vector3.Distance(position,target);
                    if(distance>=MinZoomDistance&&distance<=baseDistance*1.5f)
                    {
                        pivot=target;desiredDistance=distance;heightOffset=0;return;
                    }
                }
            }
            heightOffset+=amount;
        }
        Vector3 ResidentAnchor()=>director.residents[focus].root.position+Vector3.up*.8f;
        void ApplyPlanarPan(Vector3 movement)
        {
            movement=Vector3.ProjectOnPlane(movement,Vector3.up);
            if(movement.sqrMagnitude<1e-10f)return;
            if(following&&ValidResident(focus))
            {
                Vector3 anchor=ResidentAnchor(),current=anchor+followOffset;
                Vector3 target=occlusion?occlusion.ClampToMeadow(current,current+movement):current+movement;
                followOffset=target-anchor;
            }
            else pivot=occlusion?occlusion.ClampToMeadow(pivot,pivot+movement):pivot+movement;
        }
        void BeginOverviewOrbitAtScreenCenter()
        {
            if(!cameraReady||following||!occlusion||!reviewCamera)return;
            if(!occlusion.TryPickStaticSurface(reviewCamera.ViewportPointToRay(new Vector3(.5f,.5f,0)),reviewCamera.farClipPlane,out Vector3 target))return;
            Vector3 orbit=reviewCamera.transform.position-target-Vector3.up*heightOffset;
            float distance=orbit.magnitude;
            if(distance<MinZoomDistance||distance>baseDistance*1.5f)return;
            Vector3 look=-orbit/distance;
            yaw=Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg;
            pitch=-Mathf.Asin(look.y)*Mathf.Rad2Deg;
            pivot=target;desiredDistance=distance;
        }
        // Keyboard movement follows the screen's horizontal axes while remaining level with the farm.
        public static Vector3 KeyboardPan(Quaternion rotation,Vector2 input,float zoomDistance)
        {
            input=Vector2.ClampMagnitude(input,1);
            Vector3 right=Vector3.ProjectOnPlane(rotation*Vector3.right,Vector3.up).normalized;
            Vector3 up=Vector3.ProjectOnPlane(rotation*Vector3.forward,Vector3.up).normalized;
            return (right*input.x+up*input.y)*PanDistance(zoomDistance)*KeyboardPanMultiplier;
        }
        public static float ZoomFromWheel(float distance,float wheel,float baseDistance)=>ClampZoomDistance(distance*Mathf.Exp(wheel*.12f),baseDistance);
        public static float ClampZoomDistance(float distance,float baseDistance)=>Mathf.Clamp(distance,MinZoomDistance,baseDistance*1.5f);
        public static float PanDistance(float zoomDistance)=>Mathf.Max(zoomDistance,ClosePanReferenceDistance);
        void LateUpdate(){ApplyCamera();}
        void ApplyCamera()
        {
            if(!cameraReady)return;
            if(following&&!ValidResident(focus)){ReleaseFocus();followOffset=Vector3.zero;selected=-1;}
            if(following&&Application.isPlaying)
            {
                Vector3 anchor=ResidentAnchor();
                pivot=occlusion?occlusion.ClampToMeadow(anchor,anchor+followOffset):anchor+followOffset;
                followOffset=pivot-anchor;
            }
            Quaternion orbitRotation=Quaternion.Euler(pitch,yaw,0);
            Vector3 outward=-(orbitRotation*Vector3.forward);
            Vector3 position=pivot+outward*desiredDistance+Vector3.up*heightOffset;
            reviewCamera.transform.SetPositionAndRotation(position,orbitRotation);
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
            heightOffset=0;
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
            GUI.skin.label.fontSize=16;GUI.skin.button.fontSize=16;
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
                GUI.Label(new Rect(panel.x+16,panel.y+230,panel.width-32,24),"Esc를 누르면 설정을 닫습니다.");
                GUI.matrix=Matrix4x4.identity;return;
            }
            if(hidden){Draw(ButtonRect(5),"메뉴 보기",pointer);GUI.matrix=Matrix4x4.identity;return;}
            float w=Screen.width/Scale;
            var lighting=GetComponent<FarmLightingStudy>();
            if(lighting&&lighting.selected==3)GUI.skin.label.normal.textColor=new Color(.91f,.92f,.96f);
            Draw(LightingToggle(),lightingOpen?"시간대 비교 닫기":"시간대 비교",pointer);
            if(lightingOpen)DrawLightingPanel(lighting,pointer);
            GUI.skin.label.normal.textColor=lighting&&lighting.selected!=1?new Color(.94f,.94f,.90f):new Color(.27f,.29f,.22f);
            GUI.Label(new Rect(18,12,w-36,28),"Tiny Days · 봄날의 작은 농가");
            GUI.skin.label.fontSize=12;
            GUI.Label(new Rect(18,42,w-36,22),$"임시 생활 장면 · 이동과 머무르기 · {director.elapsed:F0}초");
            string[] labels={director.paused?"재생":"일시정지","처음부터","전체 보기",views[view],"주민 목록","메뉴 숨김"};
            for(int i=0;i<6;i++)Draw(ButtonRect(i),labels[i],pointer);
            GUI.Label(new Rect(18,66,w-36,24),"클릭 선택 · 더블클릭 따라보기 · 드래그 이동 · WASD/화살표 이동 · 가운데 드래그 높이 · 우클릭 드래그 회전 · 휠 줌 · Q/E 높이 · Home 전체");
            for(int i=0;i<director.residents.Length;i++)if((residentListOpen||selected==i)&&NameRect(i,out Rect rect))
                Draw(rect,(selected==i?"● ":"")+"주민 "+(i+1),pointer);
            if(residentListOpen)
            {
                GUI.Box(ListPanel(),"주민 목록");
                for(int i=0;i<director.residents.Length;i++)Draw(ListRow(i),(selected==i?"● ":"")+"주민 "+(i+1),pointer);
            }
            GUI.matrix=Matrix4x4.identity;
        }
        void DrawLightingPanel(FarmLightingStudy lighting,Vector2 pointer)
        {
            var panel=LightingPanel();ClampLightingScroll();GUI.Box(panel,"");
            var localPointer=LightingPointer(pointer);
            GUI.BeginGroup(panel);
            GUI.BeginGroup(new Rect(0,-lightingScroll,panel.width,LightingContentHeight));
            GUI.skin.label.normal.textColor=Color.white;GUI.skin.label.fontSize=13;
            for(int k=0;k<3;k++)GUI.Label(new Rect(panel.width-158+k*50,12,42,24),LightingColors.Labels[k]);
            for(int i=0;i<4;i++)
            {
                Draw(LightingRow(i),(lighting&&!lighting.Automatic&&lighting.selected==i?"● ":"")+FarmLightingStudy.Names[i],localPointer);
                for(int k=0;k<3;k++)if(lighting)LightingColorPicker.Swatch(LightingSwatch(i,k),lighting.Colors.Get(i,k));
            }
            if(lighting){int minutes=Mathf.FloorToInt(lighting.Hour*60)%1440;Draw(AutomaticRow(),(lighting.Automatic?"● ":"")+"자동 순환 · "+(minutes/60).ToString("00")+":"+(minutes%60).ToString("00"),localPointer);}
            GUI.Label(new Rect(8,308,164,24),"하루 길이 (1배속 기준·분)");
            GUI.skin.textField.fontSize=16;GUI.SetNextControlName("DayMinutesInput");
            dayMinutesInput=GUI.TextField(DayInput(),dayMinutesInput,4);
            Draw(DayApply(),"적용",localPointer);
            for(int i=0;i<4;i++)Draw(DayPreset(i),FarmPlaybackSettings.SuggestedMinutes[i]+"분",localPointer);
            GUI.Label(new Rect(8,444,panel.width-16,24),"재생속도 · 주민과 낮밤 함께");
            for(int i=0;i<4;i++)Draw(RateButton(i),(director.Playback.Rate==FarmPlaybackSettings.Rates[i]?"● ":"")+FarmPlaybackSettings.Rates[i].ToString("0.#")+"×",localPointer);
            GUI.Label(new Rect(8,526,panel.width-16,24),invalidDayMinutes?"1~120 사이의 정수(분)를 입력해주세요.":$"적용: {director.Playback.DayMinutes}분 · {director.Playback.Rate:0.#}× · 실제 하루 {director.Playback.DayMinutes/director.Playback.Rate:0.##}분");
            GUI.Label(new Rect(8,558,panel.width-16,24),"시간 고정 중에는 주민에게만 배속이 적용됩니다.");
            GUI.EndGroup();GUI.EndGroup();
            if(LightingContentHeight>panel.height)
            {
                GUI.skin.label.fontSize=12;GUI.skin.label.normal.textColor=Color.white;
                GUI.Label(new Rect(panel.x+10,panel.y+panel.height-22,panel.width-20,18),"패널 위에서 휠로 스크롤");
            }
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
