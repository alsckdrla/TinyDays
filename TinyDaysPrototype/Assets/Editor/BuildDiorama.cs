using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TinyDays;

public static class BuildDiorama
{
    public const string ScenePath = "Assets/Scenes/TinyDays.unity";
    const string Generated = "Assets/Art/Generated";
    static Transform root;
    static Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly Dictionary<string, string> palette = new Dictionary<string, string> {
        {"cream","EDD9AF"},{"yellow","D8A643"},{"roof","555B60"},{"wood","956344"},{"darkwood","684937"},
        {"leaf","769C48"},{"leaflight","A4B858"},{"trunk","796044"},{"soil","886345"},{"green","648C43"},
        {"orange","EBA64D"},{"white","F4EAD4"},{"pink","DAA798"},{"blue","638B9E"},{"glass","8EB8B9"},
        {"black","343A3E"},{"red","BB7158"},{"grass","94AA63"},{"path","CEB17A"},{"edge","8B8058"} };
    public static Color ColorOf(string hex) { ColorUtility.TryParseHtmlString("#"+hex, out var c); return c; }
    static Material Mat(string name) { return materials[name]; }
    [MenuItem("Tiny Days/Rebuild generated village")]
    public static void Build()
    {
        built.Clear();
        if (Application.isPlaying) throw new InvalidOperationException("Rebuild requires Edit Mode.");
        Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory(Generated+"/Materials"); Directory.CreateDirectory(Generated+"/Prefabs");
        AssetDatabase.Refresh();
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) {
            if (scene.isDirty && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = File.Exists(ScenePath) ? EditorSceneManager.OpenScene(ScenePath) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        var prior = scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="GeneratedVillage");
        if(prior) UnityEngine.Object.DestroyImmediate(prior);
        if(!scene.GetRootGameObjects().Any(o=>o.name=="ManualEdits")) new GameObject("ManualEdits");
        root = new GameObject("GeneratedVillage").transform;
        materials.Clear();
        foreach(var pair in palette) {
            string path=Generated+"/Materials/"+pair.Key+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) { mat=new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat,path); }
            mat.color=ColorOf(pair.Value); mat.SetFloat("_Glossiness",.08f); mat.SetFloat("_Metallic",0); materials[pair.Key]=mat; EditorUtility.SetDirty(mat);
        }
        QualitySettings.SetQualityLevel(5,true); QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowResolution=ShadowResolution.High;
        QualitySettings.shadowDistance=100; QualitySettings.antiAliasing=4; QualitySettings.pixelLightCount=6;
        PlayerSettings.companyName="Tiny Days"; PlayerSettings.productName="Tiny Days Prototype";
        PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.resizableWindow=true; PlayerSettings.runInBackground=true;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        Cube("Diorama earth",new Vector3(0,-.55f,0),new Vector3(28,1,21),"edge");
        Cube("Meadow",new Vector3(0,-.055f,0),new Vector3(28,.12f,21),"grass");
        Cube("Main footpath",new Vector3(0,.025f,-2.2f),new Vector3(26,.055f,2.1f),"path");
        Cube("Courtyard path",new Vector3(-.7f,.028f,2.1f),new Vector3(2.2f,.06f,9),"path");
        Cube("House footpath",new Vector3(-6,.03f,1.2f),new Vector3(1.35f,.06f,5),"path");
        Cube("Store footpath",new Vector3(6.5f,.03f,2.0f),new Vector3(1.7f,.06f,6),"path");
        Place("House",-6,4.5f,180,1,"House_A"); var secondHouse=Place("House",-6.6f,-6.0f,0,.92f,"House_B");
        foreach(var r in secondHouse.GetComponentsInChildren<Renderer>()) r.sharedMaterials=r.sharedMaterials.Select(m=>m==Mat("yellow")?Mat("cream"):m).ToArray();
        Place("Storehouse",6.2f,5.0f,180,1,"Community_Storehouse");
        var plot0=Field(4.8f,-6.1f,0); var plot1=Field(9.5f,-5.7f,1);
        for(int i=0;i<6;i++) Place("Fence",-12+i*2,8.8f,0);
        for(int i=0;i<5;i++) Place("Fence",3+i*2,8.8f,0);
        for(int i=0;i<4;i++) Place("Fence",-12.3f,1+i*2,90);
        foreach(float x in new[]{2.7f,4.7f,6.7f,8.7f,10.7f}) Place("Fence",x,-8.4f,0);
        Place("Bench",-1.9f,5.7f,15); Place("Bench",-10.1f,-.1f,-60);
        float[,] trees={{-11,6},{-9.5f,8},{-2.3f,8},{2,8.6f},{11,7},{12,2},{-12,-6},{-10,-8},{.2f,-8.7f}};
        for(int i=0;i<trees.GetLength(0);i++) Place("Tree",trees[i,0],trees[i,1],i*41,.8f+(i%3)*.12f);
        foreach(var p in new[]{new Vector2(4,3),new Vector2(4.7f,3),new Vector2(8.8f,4),new Vector2(-8.5f,3),new Vector2(2,-4.1f),new Vector2(8,-3.1f)}) Place("Crate",p.x,p.y,12);
        Place("Crate",4.2f,3.65f,-8); Place("Barrel",8.8f,3.1f); Place("Barrel",-8.5f,4.1f); Place("Barrel",-4.3f,-5.1f);
        var settings=root.gameObject.AddComponent<VillageAutonomySettings>();
        root.gameObject.AddComponent<VillageResources>(); root.gameObject.AddComponent<VillagePriority>(); root.gameObject.AddComponent<VillageTimeControls>(); root.gameObject.AddComponent<VillageHud>();
        root.gameObject.AddComponent<VillageNavigation>(); root.gameObject.AddComponent<ResidentTraffic>();
        var coordinator=root.gameObject.AddComponent<ActivityCoordinator>();
        AddSlot("Field_0_A",ResidentAction.Work,new Vector3(3.7f,.065f,-3.65f),new Vector3(4.8f,.065f,-6.1f)).cropPlot=plot0;
        AddSlot("Field_0_B",ResidentAction.Work,new Vector3(5.8f,.065f,-3.65f),new Vector3(4.8f,.065f,-6.1f)).cropPlot=plot0;
        AddSlot("Field_1_A",ResidentAction.Work,new Vector3(8.8f,.065f,-3.2f),new Vector3(9.5f,.065f,-5.7f)).cropPlot=plot1;
        AddSlot("Field_1_B",ResidentAction.Work,new Vector3(10.6f,.065f,-3.2f),new Vector3(9.5f,.065f,-5.7f)).cropPlot=plot1;
        AddSlot("HarvestCarry_0",ResidentAction.Carry,new Vector3(3.7f,.065f,-3.65f),new Vector3(4.8f,.065f,-6.1f),new Vector3(5.6f,.065f,2f)).cropPlot=plot0;
        AddSlot("HarvestCarry_1",ResidentAction.Carry,new Vector3(8.8f,.065f,-3.2f),new Vector3(9.5f,.065f,-5.7f),new Vector3(7f,.065f,2f)).cropPlot=plot1;
        AddSlot("Crate_Store_C",ResidentAction.Carry,new Vector3(-9.5f,.065f,2.7f),new Vector3(-6f,.065f,4.5f),new Vector3(-6f,.065f,1.5f));
        AddSlot("Bench_North",ResidentAction.Rest,new Vector3(-1.9f,.065f,4.65f),new Vector3(-1.9f,.065f,5.7f));
        AddSlot("Bench_West",ResidentAction.Rest,new Vector3(-10.1f,.065f,-1.4f),new Vector3(-10.1f,.065f,-.1f));
        AddSlot("View_Fields",ResidentAction.Appreciate,new Vector3(.2f,.065f,-3.6f),new Vector3(7f,.065f,-5.8f));
        AddSlot("View_Village",ResidentAction.Appreciate,new Vector3(1.5f,.065f,5.5f),new Vector3(-4f,.065f,1.5f));
        AddSlot("View_Garden",ResidentAction.Appreciate,new Vector3(-7.9f,.065f,-2.7f),new Vector3(-6.6f,.065f,-6f));
        coordinator.Refresh();
        float[,] villagers={{-4,-2},{.2f,-.5f},{4,-2.4f},{7,-1f},{-1.8f,4},{-8,-2}};
        var cratePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+"/Prefabs/Crate.prefab");
        for(int i=0;i<6;i++) {
            var resident = Place("Rabbit",villagers[i,0],villagers[i,1],new[]{-25,45,160,10,90,-80}[i],1,"Resident_"+(i+1).ToString("00"));
            var wanderer = resident.AddComponent<ResidentWanderer>(); wanderer.residentIndex=i; wanderer.walkSpeed=1.15f+i*.08f; wanderer.initialDelay=.65f+i*.82f;
            resident.AddComponent<ResidentMotor>();
            var brain=resident.AddComponent<ResidentBrain>(); brain.residentId=i; brain.diligence=.30f+i*.075f; brain.calmness=.70f-i*.065f; brain.carryVisualPrefab=cratePrefab; brain.pathPreference=(float)new System.Random(571+i*97).NextDouble();
        }
        root.gameObject.AddComponent<AutonomyDiagnostics>();
        foreach(var p in new[]{new Vector2(-8,5),new Vector2(-8,5.8f),new Vector2(-4.1f,5.6f),new Vector2(-9,-6.3f),new Vector2(-8.6f,-7.5f),new Vector2(8.8f,6),new Vector2(9.4f,5.5f),new Vector2(-2,7)}) Place("Shrub",p.x,p.y,0,.9f);
        var rng=new System.Random(74);
        // Small planting clusters soften the straight terrain and fence edges.
        for(int i=0;i<210;i++) {
            float x,z;
            if(i<90) { x=-12.9f+(float)rng.NextDouble()*25.6f; z=(i%2==0?9.1f:-9.3f)+(float)rng.NextDouble()*.35f; }
            else { x=(i%2==0?-12.8f:12.7f)+(float)rng.NextDouble()*.3f; z=-8+(float)rng.NextDouble()*16; }
            if(Mathf.Abs(z+2.2f)<1.3f) continue;
            var planting=Place(i%5==0?"Flower":"Grass",x,z,i*37,.9f+(float)rng.NextDouble()*.6f); AddSway(planting,i);
        }
        for(int i=0;i<95;i++) {
            float x=(float)rng.NextDouble()*25-12.5f, z=(float)rng.NextDouble()*18-9;
            if(Mathf.Abs(z+2.2f)<1.5f || Mathf.Abs(x+.7f)<1.6f || (x>2&&z<-3) || (x<-3&&x>-9&&Mathf.Abs(z)>2) || (x>3&&z>2)) continue;
            var flower=Place("Flower",x,z,i*19,.8f+(float)rng.NextDouble()*.7f); AddSway(flower,i);
            var grassA=Place("Grass",x+.15f,z-.15f,i*23,1.2f); AddSway(grassA,i+101);
            var grassB=Place("Grass",x-.12f,z+.16f,i*29,.9f); AddSway(grassB,i+202);
            if(i%4==0) Place("Rock",x+.3f,z+.3f,i*17,.35f);
        }
        var camObj=new GameObject("Main Camera"); camObj.transform.SetParent(root); camObj.tag="MainCamera";
        var cam=camObj.AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=12.8f;
        cam.nearClipPlane=.1f; cam.farClipPlane=150; cam.clearFlags=CameraClearFlags.SolidColor; cam.allowHDR=true;
        var focus=new Vector3(0,.6f,0); cam.transform.rotation=Quaternion.Euler(38, -25,0); cam.transform.position=focus-cam.transform.forward*40;
        camObj.AddComponent<AudioListener>(); camObj.AddComponent<DioramaCamera>();
        var sunObject=new GameObject("Sun"); sunObject.transform.SetParent(root); var sun=sunObject.AddComponent<Light>(); sun.type=LightType.Directional; sun.shadows=LightShadows.Soft; sun.shadowStrength=.68f; sun.shadowBias=.035f; sun.shadowNormalBias=.15f;
        var lighting=root.gameObject.AddComponent<LightingPresets>(); lighting.sun=sun; lighting.view=cam;
        lighting.looks=new[]{
            new LightingLook{sunlight=ColorOf("FFF2D4"),ambient=ColorOf("A7B5C2"),background=ColorOf("D7DFC7"),intensity=1.05f,sunRotation=new Vector3(55,-40,0)},
            new LightingLook{sunlight=ColorOf("FFD398"),ambient=ColorOf("A8ACB8"),background=ColorOf("DBCEB2"),intensity=1.1f,sunRotation=new Vector3(28,-55,0)},
            new LightingLook{sunlight=ColorOf("A8C5F5"),ambient=ColorOf("536783"),background=ColorOf("26364B"),intensity=.48f,sunRotation=new Vector3(48,35,0)} };
        var lamps=new List<Light>();
        foreach(var p in new[]{new Vector3(-6,1.9f,2.4f),new Vector3(-6.6f,1.8f,-4),new Vector3(6.2f,2,2.8f)}) {
            var obj=new GameObject("Warm porch light"); obj.transform.SetParent(root); obj.transform.position=p;
            var light=obj.AddComponent<Light>(); light.type=LightType.Point; light.range=5; light.intensity=2.6f; light.color=ColorOf("FFC27C"); lamps.Add(light);
        }
        lighting.windowLights=lamps.ToArray();
        var cycle=root.gameObject.AddComponent<DayNightCycle>(); cycle.cycleSeconds=180f; cycle.progress=.34f;
        var smokeMaterialPath=Generated+"/Materials/smoke.mat";
        var smokeMaterial=AssetDatabase.LoadAssetAtPath<Material>(smokeMaterialPath);
        if(!smokeMaterial) { smokeMaterial=new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(smokeMaterial,smokeMaterialPath); }
        smokeMaterial.shader=Shader.Find("Standard"); smokeMaterial.color=new Color(.62f,.62f,.58f,1f); smokeMaterial.SetFloat("_Glossiness",.02f); EditorUtility.SetDirty(smokeMaterial);
        var smokeMesh=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+"/Rock.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
        foreach(var chimney in new[]{new Vector3(-6,4.3f,4.5f),new Vector3(-6.6f,4.3f,-6),new Vector3(7.1f,4.3f,5.6f)}) {
            var smoke=new GameObject("Chimney smoke"); smoke.transform.SetParent(root); smoke.transform.position=chimney; smoke.AddComponent<ParticleSystem>(); smoke.AddComponent<ChimneySmoke>(); var particleRenderer=smoke.GetComponent<ParticleSystemRenderer>(); particleRenderer.sharedMaterial=smokeMaterial; particleRenderer.mesh=smokeMesh;
        }
        var ambient=new GameObject("Ambient audio"); ambient.transform.SetParent(root); ambient.AddComponent<AudioSource>(); ambient.AddComponent<AmbientAudioSettings>();
        ConfigureNavigation();
        lighting.ApplyCycle(cycle.progress);
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets();
        Debug.Log("TINYDAYS_BUILD_OK");
    }
    static GameObject Place(string model,float x,float z,float yaw=0,float scale=1,string label=null)
    {
        var path=Generated+"/Prefabs/"+model+".prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        // Refresh generated prefabs once per build, preserving their GUIDs.
        if(!built.Contains(model)) {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+"/"+model+".fbx");
            if(!source) throw new Exception("Missing generated model: "+model);
            var temp=new GameObject(model);
            var visual=(GameObject)UnityEngine.Object.Instantiate(source); visual.name="Visual";
            visual.transform.SetParent(temp.transform,false);
            foreach(var r in temp.GetComponentsInChildren<Renderer>()) r.sharedMaterials=r.sharedMaterials.Select(m=>Mat(m.name.Split('.')[0])).ToArray();
            prefab=PrefabUtility.SaveAsPrefabAsset(temp,path); UnityEngine.Object.DestroyImmediate(temp); built.Add(model);
        }
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);
        instance.name=label??model; instance.transform.localPosition=new Vector3(x,.065f,z); instance.transform.localRotation=Quaternion.Euler(0,yaw,0); instance.transform.localScale=Vector3.one*scale;
        return instance;
    }
    static readonly HashSet<string> built=new HashSet<string>();
    static GameObject Cube(string name,Vector3 p,Vector3 size,string mat)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name; obj.transform.SetParent(root); obj.transform.position=p; obj.transform.localScale=size; obj.GetComponent<Renderer>().sharedMaterial=Mat(mat); return obj;
    }
    static CropPlot Field(float x,float z,int index)
    {
        var field=Cube("Field_"+index,new Vector3(x,.035f,z),new Vector3(3.8f,.12f,3.5f),"soil");
        var plot=field.AddComponent<CropPlot>(); plot.plotId="밭 "+(index+1); var crops=new List<Transform>();
        for(int row=0;row<5;row++) {
            Cube("Raised soil row",new Vector3(x-1.4f+row*.7f,.11f,z),new Vector3(.37f,.13f,3.1f),"soil");
            for(int col=0;col<6;col++) { var crop=Place("Crop",x-1.4f+row*.7f,z-1.3f+col*.51f,(row+col)*23,1.0f); crops.Add(crop.transform); AddSway(crop,index*30+row*6+col); }
        }
        plot.cropVisuals=crops.ToArray(); var crate=Place("Crate",x,z-1.42f,0,1,"Harvest crate "+index); crate.SetActive(false); plot.harvestCrate=crate; return plot;
    }
    static void AddSway(GameObject obj, int seed)
    {
        var sway=obj.AddComponent<GentleSway>(); sway.phaseOffset=seed*.73f; sway.speed=.52f+(seed%5)*.09f; sway.amplitude=2.3f+(seed%4)*.55f;
    }
    static ActivitySlot AddSlot(string id, ResidentAction action, Vector3 position, Vector3 lookPosition, Vector3? carryDestination=null)
    {
        var slotObject=new GameObject(id); slotObject.transform.SetParent(root); slotObject.transform.position=position;
        var lookObject=new GameObject(id+"_Look"); lookObject.transform.SetParent(slotObject.transform); lookObject.transform.position=lookPosition;
        var slot=slotObject.AddComponent<ActivitySlot>(); slot.slotId=id; slot.action=action; slot.lookTarget=lookObject.transform; slot.carryDestination=carryDestination??position; slot.reservedBy=-1;
        return slot;
    }
    static void ConfigureNavigation()
    {
        foreach(Transform t in root)
        {
            string n=t.name;
            if(n=="Main footpath"||n.EndsWith("footpath")||n=="Courtyard path"||n=="Field_0"||n=="Field_1")
            {
                var o=t.gameObject.AddComponent<NavigationObstacle>(); o.size=Vector2.one; o.road=n.Contains("path"); continue;
            }
            if(!(n.StartsWith("House_")||n=="Community_Storehouse"||new[]{"Tree","Fence","Bench","Crate","Barrel","Rock","Shrub"}.Contains(n))) continue;
            var obstacle=t.gameObject.AddComponent<NavigationObstacle>();
            if(n=="Tree") { obstacle.circle=true; obstacle.radius=.24f; continue; }
            if(n=="Barrel") { obstacle.circle=true; obstacle.radius=.33f; continue; }
            // Conservative local footprint from the actual imported mesh, not a hard-coded world size.
            bool first=true; var bounds=new Bounds();
            foreach(var filter in t.GetComponentsInChildren<MeshFilter>())
            {
                var b=filter.sharedMesh.bounds;
                for(int k=0;k<8;k++)
                {
                    var p=b.center+Vector3.Scale(b.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));
                    p=t.InverseTransformPoint(filter.transform.TransformPoint(p));
                    if(first) { bounds=new Bounds(p,Vector3.zero); first=false; } else bounds.Encapsulate(p);
                }
            }
            obstacle.center=new Vector2(bounds.center.x,bounds.center.z); obstacle.size=new Vector2(bounds.size.x,bounds.size.z);
        }
        var nav=root.GetComponent<VillageNavigation>(); nav.Refresh();
        var occupied=new List<Vector3>();
        foreach(var slot in root.GetComponentsInChildren<ActivitySlot>().OrderBy(s=>s.slotId))
        {
            slot.transform.position=FreePoint(nav,slot.transform.position,occupied); occupied.Add(slot.transform.position);
            if(slot.action==ResidentAction.Carry) { slot.carryDestination=FreePoint(nav,slot.carryDestination,occupied); occupied.Add(slot.carryDestination); }
        }
        foreach(var resident in root.GetComponentsInChildren<ResidentBrain>())
        { resident.transform.position=FreePoint(nav,resident.transform.position,occupied); occupied.Add(resident.transform.position); }
        var route=new List<Vector3>(); var origin=root.GetComponentsInChildren<ResidentBrain>()[0].transform.position;
        foreach(var slot in root.GetComponentsInChildren<ActivitySlot>())
        {
            if(!nav.FindPath(origin,slot.transform.position,.5f,route) || (slot.action==ResidentAction.Carry&&!nav.FindPath(slot.transform.position,slot.carryDestination,.5f,route)))
                throw new InvalidOperationException("Unreachable activity: "+slot.slotId);
        }
    }
    static Vector3 FreePoint(VillageNavigation nav,Vector3 preferred,List<Vector3> occupied)
    {
        Vector3 best=preferred; float score=float.MaxValue;
        for(int x=-12;x<=12;x++) for(int z=-12;z<=12;z++)
        {
            var point=preferred+new Vector3(x*.25f,0,z*.25f); float d=(point-preferred).sqrMagnitude;
            if(d<score&&nav.ClearPoint(point)&&occupied.All(p=>Vector3.Distance(p,point)>1.1f)) { best=point; score=d; }
        }
        if(float.IsPositiveInfinity(score)||score==float.MaxValue) throw new InvalidOperationException("No clear approach near "+preferred);
        return best;
    }
    public static void Regenerate() { built.Clear(); Build(); }
}
