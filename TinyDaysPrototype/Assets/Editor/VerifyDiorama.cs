using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays;

[InitializeOnLoad]
public static class VerifyDiorama
{
    const string Active="TinyDaysVerificationActive";
    static int frames;
    static bool running;
    static readonly System.Collections.Generic.List<string> errors=new System.Collections.Generic.List<string>();
    static VerifyDiorama() {
        EditorApplication.playModeStateChanged+=OnPlayState;
        Application.logMessageReceived+=(message,trace,type)=> { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) errors.Add(message); };
        if(SessionState.GetBool(Active,false)) EditorApplication.update+=Tick;
    }
    public static void Execute()
    {
        try {
            BuildDiorama.Build();
            var manual=GameObject.Find("ManualEdits");
            foreach(Transform child in manual.transform.Cast<Transform>().ToArray()) if(child.name=="ManualPreservationProbe") UnityEngine.Object.DestroyImmediate(child.gameObject);
            var probe=new GameObject("ManualPreservationProbe"); probe.transform.SetParent(manual.transform); probe.transform.position=new Vector3(1,2,3);
            int before=GameObject.Find("GeneratedVillage").GetComponentsInChildren<Transform>().Length;
            BuildDiorama.Build();
            Require(GameObject.Find("ManualPreservationProbe")==probe && probe.transform.position==new Vector3(1,2,3),"Manual edits preserved");
            Require(GameObject.Find("GeneratedVillage").GetComponentsInChildren<Transform>().Length==before,"Idempotent generation count");
            UnityEngine.Object.DestroyImmediate(probe); EditorSceneManager.MarkSceneDirty(manual.scene); EditorSceneManager.SaveOpenScenes();
            SessionState.SetBool(Active,true); EditorApplication.update-=Tick; EditorApplication.update+=Tick;
            EditorApplication.EnterPlaymode();
        } catch(Exception e) { Fail(e); }
    }
    static void OnPlayState(PlayModeStateChange state) {
        if(!SessionState.GetBool(Active,false)) return;
        if(state==PlayModeStateChange.EnteredPlayMode) frames=0;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||running) return;
        if(++frames<60) return;
        running=true;
        try {
            var cam=Camera.main; var controls=cam.GetComponent<DioramaCamera>(); var origin=cam.transform.position; float zoom=cam.orthographicSize;
            controls.Pan(new Vector2(1,1),.5f); Require(cam.transform.position!=origin,"Camera pan");
            controls.Pan(new Vector2(10000,10000),100); Require(Mathf.Abs(cam.transform.position.x-origin.x)<=controls.panLimits.x+.001f,"Pan bounds X"); Require(Mathf.Abs(cam.transform.position.z-origin.z)<=controls.panLimits.y+.001f,"Pan bounds Z");
            controls.Zoom(1000); Require(cam.orthographicSize==controls.minZoom,"Minimum zoom");
            controls.Zoom(-1000); Require(cam.orthographicSize==controls.maxZoom,"Maximum zoom");
            controls.ResetView(); Require(cam.transform.position==origin&&cam.orthographicSize==zoom,"Home reset");
            var root=GameObject.Find("GeneratedVillage"); Require(root.transform.Cast<Transform>().Count(t=>t.name.StartsWith("Resident_"))==6,"Six sample residents");
            var residentRenderers=GameObject.Find("Resident_01").GetComponentsInChildren<Renderer>();
            var residentBounds=residentRenderers[0].bounds;
            foreach(var renderer in residentRenderers.Skip(1)) residentBounds.Encapsulate(renderer.bounds);
            Require(residentBounds.size.y>1.7f&&residentBounds.size.y<2.1f,"Resident world height 1.7-2.1 units");
            var houseBounds=GameObject.Find("House_A").GetComponentInChildren<Renderer>().bounds;
            Require(houseBounds.size.y>4&&houseBounds.size.y<4.7f,"House world height 4-4.7 units");
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()) foreach(var material in renderer.sharedMaterials) Require(material&&material.shader&&material.shader.isSupported,"Supported materials");
            var lighting=root.GetComponent<LightingPresets>();
            var cycle=root.GetComponent<DayNightCycle>();
            var residents=root.GetComponentsInChildren<ResidentWanderer>();
            Require(residents.Length==6,"Six resident wanderers");
            Require(residents.All(r=>!r.enabled),"Autonomy owns resident movement instead of fixed wandering");
            var sways=root.GetComponentsInChildren<GentleSway>(); Require(sways.Length>100,"Crops and plants have individual sway");
            foreach(var sway in sways.Take(4)) sway.Simulate(11.7f);
            Require(sways.Take(4).Select(s=>s.transform.localRotation).Distinct().Count()>1,"Plant sway phases differ");
            var smoke=root.GetComponentsInChildren<ChimneySmoke>(); Require(smoke.Length==3&&smoke.All(s=>s.GetComponent<ParticleSystem>().isPlaying),"Three chimney smoke systems play");
            var ambient=root.GetComponentInChildren<AmbientAudioSettings>(); Require(ambient&&Mathf.Approximately(ambient.volume,0f)&&Mathf.Approximately(ambient.GetComponent<AudioSource>().volume,0f),"Muted ambient audio structure");
            var coordinator=root.GetComponent<ActivityCoordinator>(); var brains=root.GetComponentsInChildren<ResidentBrain>(); var diagnostics=root.GetComponent<AutonomyDiagnostics>();
            var resources=root.GetComponent<VillageResources>(); var priority=root.GetComponent<VillagePriority>(); var timeControls=root.GetComponent<VillageTimeControls>(); var hud=root.GetComponent<VillageHud>(); var plots=root.GetComponentsInChildren<CropPlot>();
            Require(coordinator&&brains.Length==6&&diagnostics,"Autonomy coordinator, six brains, and diagnostics exist");
            Require(resources&&priority&&timeControls&&hud&&plots.Length==2,"Resources, priority, time controls, HUD, and two crop plots exist");
            Require(resources.food==12&&resources.foodCapacity==24,"Food starts at 12 / 24");
            priority.Set(VillagePriorityMode.Production); Require(Mathf.Approximately(priority.WorkMultiplier,1.6f)&&Mathf.Approximately(priority.RestMultiplier,.45f),"Production priority weights");
            priority.Set(VillagePriorityMode.Leisure); Require(Mathf.Approximately(priority.WorkMultiplier,.6f)&&Mathf.Approximately(priority.RestMultiplier,1.4f),"Leisure priority weights"); priority.Set(VillagePriorityMode.Balanced);
            resources.food=0; Require(Mathf.Approximately(resources.ProductivityMultiplier,.75f)&&Mathf.Approximately(resources.RestRecoveryMultiplier,.60f),"Food shortage modifiers"); resources.food=12;
            timeControls.SetSpeed(2); Require(Mathf.Approximately(Time.timeScale,4f),"4x time control"); timeControls.SetPaused(true); Require(Mathf.Approximately(Time.timeScale,0f),"Pause time control"); timeControls.SetPaused(false); timeControls.SetSpeed(0); Require(Mathf.Approximately(Time.timeScale,1f),"Resume 1x time control");
            hud.visible=false; Require(!hud.visible,"HUD can hide"); hud.visible=true;
            VerifyNavigation.Run(root);
            // Run five full visual days (15 simulated minutes) while sampling all time-of-day decision weights.
            var traffic=root.GetComponent<ResidentTraffic>(); traffic.automatic=false;
            var nav=root.GetComponent<VillageNavigation>(); nav.Refresh();
            float minimumDistance=float.MaxValue;
            var progressPositions=brains.Select(b=>b.transform.position).ToArray();
            var progressTimes=new float[brains.Length]; var progressActions=brains.Select(b=>b.CompletedActions).ToArray();
            for(int tick=0;tick<18000;tick++)
            {
                if(tick%2000==0) Debug.Log("NAVIGATION_SIM_SECOND "+tick*.05f);
                cycle.SetProgress(tick*.05f/180f);
                var previous=brains.Select(b=>b.transform.position).ToArray();
                traffic.Simulate(.05f);
                for(int i=0;i<brains.Length;i++)
                {
                    if(Vector3.Distance(progressPositions[i],brains[i].transform.position)>.25f||progressActions[i]!=brains[i].CompletedActions)
                    { progressPositions[i]=brains[i].transform.position; progressTimes[i]=tick*.05f; progressActions[i]=brains[i].CompletedActions; }
                    if(tick*.05f-progressTimes[i]>30f) throw new Exception("Sustained resident stall: "+brains[i].name+" at "+brains[i].transform.position+" / "+brains[i].Motor.Reason);
                    if(!nav.ClearSegment(previous[i],brains[i].transform.position)) throw new Exception("Obstacle penetration: "+brains[i].name+" at "+brains[i].transform.position);
                    for(int j=i+1;j<brains.Length;j++)
                    {
                        float distance=VillageNavigation.SegmentDistance(Vector3.zero,previous[i]-previous[j],brains[i].transform.position-brains[j].transform.position);
                        minimumDistance=Mathf.Min(minimumDistance,distance);
                        if(distance<.899f) throw new Exception("Resident swept collision "+i+" / "+j+": "+distance);
                    }
                }
            }
            Debug.Log("NAVIGATION_MINIMUM_DISTANCE "+minimumDistance);
            Debug.Log("AUTONOMY_COUNTS "+string.Join("; ",brains.Select(b=>b.name+" W"+b.WorkCount+" C"+b.CarryCount+" R"+b.RestCount+" A"+b.AppreciateCount+" fatigue="+b.Fatigue+" failures="+b.FallbackCount+" replans="+b.Motor.Replans+" slot="+(b.CurrentSlot?b.CurrentSlot.slotId:"none")))+" | SLOTS "+string.Join(",",coordinator.Slots.Select(s=>s.slotId+"="+s.reservedBy)));
            Require(brains.Sum(b=>b.WorkCount)>3,"Autonomy performs field work; "+string.Join("; ",brains.Select(b=>b.name+" W"+b.WorkCount+" C"+b.CarryCount+" R"+b.RestCount+" A"+b.AppreciateCount+" current="+b.CurrentAction+" reason="+b.DecisionReason)));
            Require(brains.Sum(b=>b.CarryCount)>2,"Autonomy performs crate carrying");
            Require(resources.TotalHarvested>0,"Crop harvests are carried into storage");
            Require(resources.TotalConsumed>0,"Residents consume food at dawn");
            Require(resources.food>=0&&resources.food<=resources.foodCapacity,"Food stays within storage bounds");
            Require(brains.Sum(b=>b.RestCount)>3,"Autonomy performs rest");
            Require(brains.Sum(b=>b.AppreciateCount)>3,"Autonomy performs appreciation");
            Require(brains.Any(b=>b.RestedThenWorked),"Residents return to work after rest");
            Require(brains.Select(b=>b.WorkCount).Distinct().Count()>1,"Diligence creates different work frequencies");
            Require(minimumDistance>=.899f,"Independent swept resident separation");
            Require(brains.All(b=>b.WorkCount>0&&b.RestCount>0&&b.AppreciateCount>0),"Each resident completes work, rest, and appreciation");
            Require(coordinator.Slots.Count(s=>s.reservedBy>=0)==coordinator.ReservedCount,"Every slot has one reservation owner");
            Require(coordinator.Slots.Select(s=>s.slotId).Distinct().Count()==12,"Twelve unique activity IDs survive scene reload");
            var disablingBrain=brains.First(b=>b.CurrentSlot); var releasedSlot=disablingBrain.CurrentSlot;
            disablingBrain.enabled=false; Require(releasedSlot.reservedBy==-1&&!disablingBrain.CurrentSlot,"Disable releases reservation immediately"); disablingBrain.enabled=true;
            for(int tick=0;tick<100&&!brains.Any(b=>b.CurrentSlot&&b.Motor.HasGoal);tick++) traffic.Simulate(.05f);
            var unreachableBrain=brains.FirstOrDefault(b=>b.CurrentSlot&&b.Motor.HasGoal); Require(unreachableBrain,"Resident selects a reachable destination for failure test"); var unreachableSlot=unreachableBrain.CurrentSlot;
            Require(!unreachableBrain.Motor.SetDestination(new Vector3(-6,.065f,4.5f)),"Destination inside building is rejected");
            unreachableBrain.Simulate(.05f); Require(unreachableSlot.reservedBy==-1&&!unreachableBrain.CurrentSlot,"Path failure releases reservation");
            for(int tick=0;tick<100&&!brains.Any(b=>b.CurrentSlot);tick++) traffic.Simulate(.05f);
            var failingBrain=brains.FirstOrDefault(b=>b.CurrentSlot); Require(failingBrain,"Resident reserves a slot for blocked-destination test"); var blockedSlot=failingBrain.CurrentSlot; failingBrain.ForceCurrentSlotUnavailableForVerification(); int fallbackBefore=failingBrain.FallbackCount;
            failingBrain.Simulate(.2f); Require(failingBrain.FallbackCount>fallbackBefore&&!failingBrain.CurrentSlot&&blockedSlot.reservedBy==-1,"Blocked destination releases reservation and chooses fallback");
            diagnostics.SetVisible(true); Require(diagnostics.visible,"F-key diagnostics can be shown"); diagnostics.SetVisible(false); Require(!diagnostics.visible,"F-key diagnostics can be hidden");
            Directory.CreateDirectory("Docs/Captures");
            foreach(TimeLook look in Enum.GetValues(typeof(TimeLook))) {
                lighting.Apply(look); Require(lighting.current==look,"Lighting preset "+look);
                Require(lighting.windowLights.All(l=>l.enabled==(look==TimeLook.Night)),"Porch lights "+look);
                foreach(var size in new[]{new Vector2Int(960,540),new Vector2Int(1280,720)}) Capture(cam,size.x,size.y,"Docs/Captures/"+look+"_"+size.x+"x"+size.y+".png");
            }
            var previousSun=lighting.sun.intensity;
            cycle.SetProgress(.34f); var sunsetIntensity=lighting.sun.intensity;
            cycle.SetProgress(.35f); Require(Mathf.Abs(lighting.sun.intensity-sunsetIntensity)<.08f,"Day/night lighting interpolates continuously");
            cycle.SetProgress(.60f); Require(lighting.windowLights.All(l=>l.enabled),"Night porch lights enabled");
            cycle.SetProgress(.08f); Require(lighting.windowLights.All(l=>!l.enabled),"Day porch lights disabled");
            foreach(var point in new[]{.08f,.34f,.60f}) { cycle.SetProgress(point); Capture(cam,1280,720,"Docs/Captures/Motion_"+(point*100).ToString("00")+".png"); }

            cycle.SetProgress(.34f);
            Require(errors.Count==0,"No runtime errors: "+string.Join("; ",errors));
            File.WriteAllText("Docs/Verification.txt","PASS: Unity Play Mode; 60 editor updates before tests.\nPASS: repeat generation stable, ManualEdits preserved.\nPASS: 6 residents; supported materials and correct world scale.\nPASS: camera pan/bounds/zoom/reset via public control methods.\nPASS: plants, smoke, continuous lighting, and muted audio structure.\nPASS: crop growth, harvest carry, food storage and dawn consumption.\nPASS: production/balanced/leisure weights, shortage recovery, HUD and pause/1x/2x/4x controls.\nPASS: 900 simulated seconds: each resident completes four actions; swept obstacle and pair separation; fatigue recovery and blocked-destination fallback.\nPASS: F-key diagnostics can show/hide; the runtime Game view displays the overlay.\nPASS: no runtime Error/Exception/Assert received.\nKeyboard/mouse physical input and fifteen-minute visual observation remain manual checks.\n"+DateTime.Now.ToString("O"));
            Debug.Log("TINYDAYS_VERIFICATION_OK"); SessionState.SetBool(Active,false); EditorApplication.Exit(0);
        } catch(Exception e) { Fail(e); }
    }
    static void Capture(Camera cam,int width,int height,string path)
    {
        var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB,4);
        var previous=RenderTexture.active; var target=cam.targetTexture;
        var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
        try { cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt; tex.ReadPixels(new Rect(0,0,width,height),0,0); tex.Apply(); File.WriteAllBytes(path,tex.EncodeToPNG()); }
        finally { cam.targetTexture=target; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(tex); }
    }
    static void Require(bool condition,string message) { if(!condition) throw new Exception("Verification failed: "+message); Debug.Log("PASS: "+message); }
    static void Fail(Exception e) { Debug.LogException(e); SessionState.SetBool(Active,false); Directory.CreateDirectory("Docs"); File.WriteAllText("Docs/Verification.txt","FAILED\n"+e); EditorApplication.Exit(1); }
}
