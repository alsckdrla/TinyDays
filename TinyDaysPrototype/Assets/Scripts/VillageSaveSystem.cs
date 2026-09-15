using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TinyDays
{
    // Versioned, local-only save file. The .bak file remains available if writing is interrupted.
    public sealed class VillageSaveSystem : MonoBehaviour
    {
        const string SaveFile="tiny-days-save.json", BackupFile="tiny-days-save.bak", TemporaryFile="tiny-days-save.tmp";
        VillageResources resources; VillagePriority priority; VillageTimeControls timeControls; VillageHud hud;
        DayNightCycle cycle; VillageExpansion expansion; ActivityCoordinator coordinator; ResidentTraffic traffic; AmbientAudioSettings ambient;
        DioramaCamera dioramaCamera;
        bool menu, wasPaused, applying;
        public bool MenuOpen => menu;
        enum MenuCommand { None, Save, Load, Continue }
        MenuCommand pendingCommand;
        MenuCommand pressedCommand;
        bool draggingVolume;
        Rect MenuBounds => new Rect(Screen.width*.5f-150,Screen.height*.5f-125,300,250);
        static readonly Rect SaveButton=new Rect(25,42,250,30), LoadButton=new Rect(25,78,250,30), ContinueButton=new Rect(25,170,250,25), VolumeTrack=new Rect(25,143,250,20);
        string message=""; float messageUntil;
#if UNITY_EDITOR
        public string verificationDirectory;
#endif
        string SaveDirectory {
            get {
#if UNITY_EDITOR
                if(!string.IsNullOrEmpty(verificationDirectory)) return verificationDirectory;
#endif
                return Application.persistentDataPath;
            }
        }
        string SavePath => Path.Combine(SaveDirectory,SaveFile);
        string BackupPath => Path.Combine(SaveDirectory,BackupFile);
        void Awake()
        {
            resources=GetComponent<VillageResources>(); priority=GetComponent<VillagePriority>(); timeControls=GetComponent<VillageTimeControls>(); hud=GetComponent<VillageHud>();
            cycle=GetComponent<DayNightCycle>(); expansion=GetComponent<VillageExpansion>(); coordinator=GetComponent<ActivityCoordinator>(); traffic=GetComponent<ResidentTraffic>(); ambient=GetComponentInChildren<AmbientAudioSettings>();
        }
        void Start()
        {
            dioramaCamera=Camera.main?Camera.main.GetComponent<DioramaCamera>():null;
#if !UNITY_EDITOR
            LoadNow();
#endif
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) SetMenu(!menu);
            var pointer=Input.mousePosition;
            ProcessMenuPointer(new Vector2(pointer.x,Screen.height-pointer.y)-MenuBounds.position,
                Input.GetMouseButtonDown(0),Input.GetMouseButton(0),Input.GetMouseButtonUp(0));
            var command=pendingCommand; pendingCommand=MenuCommand.None;
            if(command==MenuCommand.Save) SaveNow();
            else if(command==MenuCommand.Load) LoadNow();
            else if(command==MenuCommand.Continue) SetMenu(false);
        }
        void SetMenu(bool value)
        {
            if(menu==value) return; menu=value;
            CancelPointer();
            if(menu) { wasPaused=timeControls&&timeControls.paused; if(timeControls) timeControls.SetPaused(true); }
            else if(timeControls) timeControls.SetPaused(wasPaused);
        }
        // Poll the runtime input once per frame: Editor Game views can omit IMGUI MouseDown.
        // Drawing never dispatches commands, so one physical click cannot execute twice.
        void ProcessMenuPointer(Vector2 point,bool down,bool held,bool up)
        {
            if(!menu) { CancelPointer(); return; }
            if(down)
            {
                pressedCommand=CommandAt(point);
                draggingVolume=ambient&&VolumeTrack.Contains(point);
            }
            if(draggingVolume&&(down||held||up))
                ambient.SetVolume(Mathf.Clamp01((point.x-VolumeTrack.x-5)/(VolumeTrack.width-10)));
            if(up)
            {
                if(pressedCommand!=MenuCommand.None&&CommandAt(point)==pressedCommand) pendingCommand=pressedCommand;
                CancelPointer();
            }
            else if(!down&&!held) CancelPointer();
        }
        static MenuCommand CommandAt(Vector2 point)
        {
            if(SaveButton.Contains(point)) return MenuCommand.Save;
            if(LoadButton.Contains(point)) return MenuCommand.Load;
            if(ContinueButton.Contains(point)) return MenuCommand.Continue;
            return MenuCommand.None;
        }
        void CancelPointer() { pressedCommand=MenuCommand.None; draggingVolume=false; }
        void OnApplicationFocus(bool focused) { if(!focused) CancelPointer(); }
        void OnDisable() { CancelPointer(); pendingCommand=MenuCommand.None; }
        public void SaveNow()
        {
            if(applying) return;
            try
            {
                var data=Capture(); string directory=SaveDirectory; Directory.CreateDirectory(directory);
                string temporary=Path.Combine(directory,TemporaryFile); File.WriteAllText(temporary,JsonUtility.ToJson(data,true));
                if(File.Exists(SavePath)) File.Copy(SavePath,BackupPath,true);
                if(File.Exists(SavePath)) File.Delete(SavePath); File.Move(temporary,SavePath);
                Notify("마을을 저장했습니다");
            }
            catch(Exception e) { Debug.LogWarning("Tiny Days save failed: "+e.Message); Notify("저장하지 못했습니다"); }
        }
        public bool LoadNow()
        {
            VillageSaveData data;
            if(!TryRead(SavePath,out data) && !TryRead(BackupPath,out data)) { Notify("저장된 마을이 없습니다"); return false; }
            try { Apply(data); Notify("저장한 마을을 불러왔습니다"); return true; }
            catch(Exception e) { Debug.LogWarning("Tiny Days load failed: "+e.Message); Notify("불러오지 못했습니다"); return false; }
        }
        bool TryRead(string path,out VillageSaveData data)
        {
            data=null;
            try { if(!File.Exists(path)) return false; data=JsonUtility.FromJson<VillageSaveData>(File.ReadAllText(path)); return data!=null&&data.version==1; }
            catch(Exception e) { Debug.LogWarning("Tiny Days load skipped: "+e.Message); return false; }
        }
        VillageSaveData Capture()
        {
            var data=new VillageSaveData {
                food=resources.food, foodCapacity=resources.foodCapacity, totalHarvested=resources.TotalHarvested, totalConsumed=resources.TotalConsumed,
                priority=(int)priority.mode, speedIndex=timeControls.speedIndex, paused=menu?wasPaused:timeControls.paused, hudVisible=hud.visible,
                dayProgress=cycle.progress, ambientVolume=ambient?ambient.volume:0,
                expansionDecided=expansion.Decided, expansionComplete=expansion.IsComplete, expansionShortage=expansion.shortageSeconds, expansionBuild=expansion.buildSeconds
            };
            var camera=Camera.main; if(camera) { data.cameraX=camera.transform.position.x; data.cameraY=camera.transform.position.y; data.cameraZ=camera.transform.position.z; data.cameraYaw=camera.transform.eulerAngles.y; data.cameraPitch=camera.transform.eulerAngles.x; data.cameraZoom=camera.orthographicSize; }
            foreach(var crop in GetComponentsInChildren<CropPlot>().OrderBy(c=>c.plotId)) data.crops.Add(new CropSaveData { id=crop.plotId,growth=crop.growth,harvest=crop.HasHarvest,carrierId=crop.CarrierId });
            foreach(var resident in GetComponentsInChildren<ResidentBrain>().OrderBy(r=>r.residentId)) data.residents.Add(resident.CaptureSaveState());
            return data;
        }
        void Apply(VillageSaveData data)
        {
            applying=true; bool automatic=traffic&&traffic.automatic; if(traffic) traffic.automatic=false;
            try
            {
                resources.RestoreState(data.food,data.foodCapacity,data.totalHarvested,data.totalConsumed);
                cycle.SetProgress(data.dayProgress); resources.SyncDayProgress(data.dayProgress); priority.Set((VillagePriorityMode)Mathf.Clamp(data.priority,0,2)); hud.visible=data.hudVisible;
                if(ambient) ambient.SetVolume(data.ambientVolume);
                expansion.RestoreState(data.expansionShortage,data.expansionBuild,data.expansionDecided,data.expansionComplete);
                var camera=Camera.main; if(camera) { camera.transform.position=new Vector3(data.cameraX,data.cameraY,data.cameraZ); camera.transform.rotation=Quaternion.Euler(data.cameraPitch,data.cameraYaw,0); camera.orthographicSize=Mathf.Max(.01f,data.cameraZoom); }
                foreach(var saved in data.crops)
                {
                    var crop=GetComponentsInChildren<CropPlot>().FirstOrDefault(c=>c.plotId==saved.id);
                    if(crop) crop.RestoreState(saved.growth,saved.harvest,saved.carrierId);
                }
                coordinator.ClearReservations();
                foreach(var saved in data.residents.Where(r=>!string.IsNullOrEmpty(r.slotId))) coordinator.RestoreReservation(saved.slotId,saved.id);
                foreach(var saved in data.residents)
                {
                    var resident=GetComponentsInChildren<ResidentBrain>().FirstOrDefault(r=>r.residentId==saved.id);
                    var slot=coordinator.Slots.FirstOrDefault(s=>s.slotId==saved.slotId&&s.reservedBy==saved.id);
                    if(resident) resident.RestoreSaveState(saved,slot);
                }
                if(traffic) traffic.RefreshResidents();
                timeControls.SetSpeed(data.speedIndex);
                if(menu) wasPaused=data.paused;
                timeControls.SetPaused(menu||data.paused);
            }
            finally { if(traffic) traffic.automatic=automatic; applying=false; }
        }
        void Notify(string text) { message=text; messageUntil=Time.unscaledTime+3f; }
        void OnGUI()
        {
            GUI.depth=-100;
            if(menu)
            {
                GUI.Box(MenuBounds,"Tiny Days 설정");
                GUI.BeginGroup(MenuBounds);
                DrawMenu();
                GUI.EndGroup();
            }
            if(!string.IsNullOrEmpty(message)&&Time.unscaledTime<messageUntil) GUI.Box(new Rect(Screen.width*.5f-120,28,240,30),message);
        }
        void DrawMenu()
        {
            DrawButton(SaveButton,"저장",MenuCommand.Save);
            DrawButton(LoadButton,"불러오기",MenuCommand.Load);
            GUI.Label(new Rect(25,118,250,22),"환경음  "+Mathf.RoundToInt((ambient?ambient.volume:0)*100)+"%");
            GUI.Box(new Rect(VolumeTrack.x,VolumeTrack.y+7,VolumeTrack.width,6),GUIContent.none,GUI.skin.horizontalSlider);
            GUI.Box(new Rect(VolumeTrack.x+(VolumeTrack.width-10)*(ambient?ambient.volume:0),VolumeTrack.y,10,20),GUIContent.none,GUI.skin.horizontalSliderThumb);
            DrawButton(ContinueButton,"계속하기 (Esc)",MenuCommand.Continue);
            GUI.Label(new Rect(25,208,250,30),message);
        }
        void DrawButton(Rect rect,string text,MenuCommand command)
        {
            if(Event.current.type!=EventType.Repaint) return;
            var pointer=Input.mousePosition;
            bool hover=rect.Contains(new Vector2(pointer.x,Screen.height-pointer.y)-MenuBounds.position);
            GUI.skin.button.Draw(rect,new GUIContent(text),hover,pressedCommand==command,false,false);
        }
        void OnApplicationQuit() { if(!Application.isEditor) SaveNow(); }
    }
}
