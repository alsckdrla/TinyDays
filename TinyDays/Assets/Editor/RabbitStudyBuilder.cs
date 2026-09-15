using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using TinyDays.Review;

public static class RabbitStudyBuilder
{
    [Serializable] class PoseAudit { public float[] min; public float[] max; public float[] size; }
    [Serializable] class PoseSet { public PoseAudit Pose_Biped; public PoseAudit Pose_Quadruped; }
    [Serializable] class SourceAudit { public string blender; public PoseSet poses; }
    public const string ScenePath = "Assets/Scenes/RabbitStudy.unity";
    const string ModelPath = "Assets/Art/Generated/RabbitStudy.fbx";
    const string Materials = "Assets/Art/Generated/Materials";
    const string ReviewRoot = "GeneratedReview";
    const string Captures = "Docs/Captures/Stage22/Unity";
    static readonly Dictionary<string,string> Palette = new Dictionary<string,string> {
        {"Fur","DCCDB5"},{"Muzzle","EDE1CC"},{"Ear","BD9689"},{"Nose","8B6D65"},
        {"Eye","302D2B"},{"Sclera","EEE9DD"},{"Shirt","EEE4CE"},{"Cloth","657E7A"},{"Pocket","718C86"},
        {"Stitch","B8B695"},{"Button","B89960"},{"Ground","D4D5C1"}
    };
    static Color ColorOf(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c); return c; }
    static void Ensure(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); }

    [MenuItem("Tiny Days/Stage 2-2/Open rabbit review")]
    public static void OpenReview()
    {
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }

    [MenuItem("Tiny Days/Stage 2-2/Rebuild and verify rabbit study")]
    public static void Execute()
    {
        try { Build(); VerifyAndCapture(); Debug.Log("TINYDAYS_STAGE22_OK"); }
        catch(Exception ex) { Debug.LogException(ex); if(Application.isBatchMode) EditorApplication.Exit(1); else throw; }
    }

    public static void Build()
    {
        Ensure(!Application.isPlaying,"Exit Play Mode before rebuilding.");
        foreach(var path in new[]{"Assets/Settings","Assets/Scenes",Materials,"Assets/Art/Generated/Prefabs","Assets/Art/Generated/Controllers","Assets/Art/Manual",Captures}) Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
        ConfigurePipeline();
        ConfigureModel();
        var scene=SceneManagerSetup();
        var old=scene.GetRootGameObjects().FirstOrDefault(o=>o.name==ReviewRoot);
        if(old) UnityEngine.Object.DestroyImmediate(old);
        if(!scene.GetRootGameObjects().Any(o=>o.name=="ManualEdits")) new GameObject("ManualEdits");
        var root=new GameObject(ReviewRoot).transform;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name="Review Ground";
        floor.transform.SetParent(root);floor.transform.position=new Vector3(0,-.025f,0);floor.transform.localScale=Vector3.one*20;
        floor.GetComponent<Renderer>().sharedMaterial=Mat("Ground");
        var collider=floor.GetComponent<Collider>();if(collider) UnityEngine.Object.DestroyImmediate(collider);
        CreateResident(root,"Biped",new Vector3(-1.05f,0,0));
        CreateResident(root,"Quadruped",new Vector3(1.05f,0,0));
        var lightGo=new GameObject("Warm daylight");lightGo.transform.SetParent(root);
        lightGo.transform.rotation=Quaternion.Euler(42,144,0);
        var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.05f;
        light.color=ColorOf("FFF8EC");light.shadows=LightShadows.Soft;light.shadowBias=.015f;light.shadowNormalBias=.1f;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=ColorOf("C2D0DE")*.8f;
        RenderSettings.ambientEquatorColor=ColorOf("DCD7C8")*.65f;
        RenderSettings.ambientGroundColor=ColorOf("A29A86")*.45f;
        var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.30f,.33f,.36f));RenderSettings.ambientProbe=probe;
        RenderSettings.reflectionIntensity=.2f; RenderSettings.fog=false;
        var camGo=new GameObject("Review Camera");camGo.tag="MainCamera";camGo.transform.SetParent(root);
        var cam=camGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=2.0f;
        cam.nearClipPlane=.05f;cam.farClipPlane=80;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=ColorOf("E6E4D9");
        cam.allowHDR=true;cam.allowMSAA=true;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        SetCamera(cam,new Vector3(-3.8f,3.0f,6.5f),new Vector3(0,1.0f,0));
        PlayerSettings.companyName="Tiny Days";PlayerSettings.productName="Tiny Days";
        PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        Selection.activeGameObject=root.gameObject;
    }

    static UnityEngine.SceneManagement.Scene SceneManagerSetup()
    {
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(active.path==ScenePath) return active;
        if(!Application.isBatchMode && active.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new OperationCanceledException();
        return File.Exists(ScenePath)?EditorSceneManager.OpenScene(ScenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
    static void ConfigurePipeline()
    {
        const string rendererPath="Assets/Settings/TinyDaysRenderer.asset";
        const string pipelinePath="Assets/Settings/TinyDaysURP.asset";
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if(!renderer) { renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,rendererPath); }
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if(!pipeline) { pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,pipelinePath); }
        pipeline.msaaSampleCount=4;pipeline.renderScale=1;pipeline.shadowDistance=30;
        pipeline.supportsHDR=true;
        GraphicsSettings.defaultRenderPipeline=pipeline;
        for(int i=0;i<QualitySettings.names.Length;i++) { QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=pipeline; }
        EditorUtility.SetDirty(pipeline);
    }
    static Material Mat(string name)
    {
        string path=Materials+"/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader=Shader.Find("Universal Render Pipeline/Lit");Ensure(shader!=null,"URP Lit shader is missing.");
        if(!mat) {mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
        mat.shader=shader;mat.SetColor("_BaseColor",ColorOf(Palette[name]));mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_Metallic",0);
        EditorUtility.SetDirty(mat);return mat;
    }
    static void ConfigureModel()
    {
        Ensure(File.Exists(ModelPath),"Generate RabbitStudy.fbx with Blender first.");
        var importer=AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        Ensure(importer!=null,"Rabbit importer is missing.");
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
        importer.optimizeGameObjects=false;importer.globalScale=1;
        importer.importNormals=ModelImporterNormals.Import;
        foreach(var name in Palette.Keys) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),Mat(name));
        importer.SaveAndReimport();
    }
    static void CreateResident(Transform parent,string pose,Vector3 position)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var go=UnityEngine.Object.Instantiate(model);go.name="Rabbit_"+pose;go.transform.SetParent(parent);go.transform.position=position;
        var clips=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var clip=clips.FirstOrDefault(c=>c.name.Contains("Pose_"+pose));
        Ensure(clip!=null,"Missing pose clip "+pose+"; found "+string.Join(",",clips.Select(c=>c.name)));
        var display=go.AddComponent<RabbitPoseDisplay>();display.pose=clip;
        var animator=go.GetComponent<Animator>();Ensure(animator!=null,"Generic Animator missing.");
        string path="Assets/Art/Generated/Controllers/"+pose+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!controller) controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine=controller.layers[0].stateMachine;
        var state=machine.states.Length>0?machine.states[0].state:machine.AddState("Static reference pose");
        state.motion=clip;machine.defaultState=state;EditorUtility.SetDirty(controller);
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen=true;
        display.Apply();
        PrefabUtility.SaveAsPrefabAsset(go,"Assets/Art/Generated/Prefabs/Rabbit_"+pose+".prefab");
    }
    static void SetCamera(Camera camera,Vector3 position,Vector3 target) { camera.transform.position=position;camera.transform.LookAt(target); }
    static Bounds BakedBounds(GameObject go)
    {
        Bounds bounds=new Bounds();bool first=true;
        foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            // Evaluate skinning in WORLD space directly. Imported FBX transforms can
            // carry unit conversion; applying renderer scale to BakeMesh doubles it.
            var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
            var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
            for(int i=0;i<vertices.Length;i++) {
                var w=weights[i];var v=vertices[i];
                var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0
                    +matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1
                    +matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2
                    +matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
            }
        }
        Ensure(!first,"No skinned vertices");return bounds;
    }
    public static void VerifyAndCapture()
    {
        Ensure(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,"URP is not active.");
        // Exercise the destructive boundary twice using an isolated sentinel in ManualEdits.
        var manual=GameObject.Find("ManualEdits");Ensure(manual!=null,"Manual root missing");
        var sentinel=new GameObject("__Stage22_PreservationProbe_"+Guid.NewGuid().ToString("N"));
        sentinel.transform.SetParent(manual.transform);sentinel.transform.localPosition=new Vector3(11,12,13);
        var sentinelId=sentinel.GetInstanceID();
        int firstCount=GameObject.Find(ReviewRoot).GetComponentsInChildren<Transform>(true).Length;
        Build();Build();
        Ensure(sentinel!=null&&sentinel.GetInstanceID()==sentinelId&&sentinel.transform.localPosition==new Vector3(11,12,13),"Manual sentinel was changed.");
        Ensure(GameObject.Find(ReviewRoot).GetComponentsInChildren<Transform>(true).Length==firstCount,"Repeated generation changed object count.");
        UnityEngine.Object.DestroyImmediate(sentinel);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        var report=new List<string>{"Stage 2-2 automatic verification", "Unity "+Application.unityVersion,"URP 14.0.10 / Generic static poses", "ManualEdits sentinel and repeated generation: PASS"};
        var residents=GameObject.Find(ReviewRoot).GetComponentsInChildren<RabbitPoseDisplay>();
        Ensure(residents.Length==2,"Expected two reference poses.");
        var source=JsonUtility.FromJson<SourceAudit>(File.ReadAllText("Assets/Art/Generated/RabbitStudy.audit.json"));
        foreach(var resident in residents) {
            resident.Apply();var animator=resident.GetComponent<Animator>();
            Ensure(animator.avatar!=null&&animator.avatar.isValid&&!animator.avatar.isHuman,"Invalid generic avatar");
            var bones=resident.GetComponentsInChildren<Transform>();Ensure(bones.Any(t=>t.name=="Spine")&&bones.Any(t=>t.name=="Head"),"Required rig bones missing");
            foreach(var skin in resident.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                Ensure(skin.bones.All(b=>b!=null),"Missing skin bone");
                Ensure(skin.sharedMaterials.All(m=>m!=null&&m.shader.name=="Universal Render Pipeline/Lit"),"Non-URP material");
            }
            var bounds=BakedBounds(resident.gameObject);
            var sourcePose=resident.name.EndsWith("Biped")?source.poses.Pose_Biped:source.poses.Pose_Quadruped;
            var expected=new Vector3(sourcePose.size[0],sourcePose.size[2],sourcePose.size[1]);
            Ensure(Vector3.Distance(bounds.size,expected)<.015f,"Blender/Unity pose dimensions differ: "+resident.name);
            Ensure(bounds.size.y>.7f&&bounds.size.y<3,"Unexpected model units: "+resident.name+" "+bounds);
            Ensure(bounds.min.y>-.045f,"Model penetrates review floor");
            report.Add(resident.name+" bounds: "+bounds+" / generic avatar, bones, materials, Blender dimension comparison (<0.015m): PASS");
        }
        var biped=residents.First(r=>r.name.EndsWith("Biped"));var quad=residents.First(r=>r.name.EndsWith("Quadruped"));
        Ensure(BakedBounds(biped.gameObject).size.y>BakedBounds(quad.gameObject).size.y+.2f,"Pose clips did not produce different heights.");
        var camera=Camera.main;
        var homePosition=camera.transform.position;var homeRotation=camera.transform.rotation;float homeSize=camera.orthographicSize;
        var views=new Dictionary<string,Vector3>{{"Front",new Vector3(0,.25f,7)},{"Side",new Vector3(-7,.25f,0)},{"Back",new Vector3(0,.25f,-7)},{"ThreeQuarter",new Vector3(-4,2.3f,6)}};
        foreach(var resident in residents) {
            foreach(var other in residents) other.gameObject.SetActive(other==resident);
            var center=resident.transform.position+new Vector3(0,1.10f,0);
            camera.orthographicSize=1.4f;
            foreach(var pair in views) { SetCamera(camera,center+pair.Value,center);Capture(camera,resident.name+"_"+pair.Key,800,800); }
            camera.orthographicSize=2.8f;SetCamera(camera,center+views["ThreeQuarter"],center);Capture(camera,resident.name+"_Small",640,480);
        }
        foreach(var resident in residents) {resident.gameObject.SetActive(true);resident.Apply();}
        camera.transform.SetPositionAndRotation(homePosition,homeRotation);camera.orthographicSize=homeSize;
        Capture(camera,"Overview",1280,800);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        report.Add("Two poses x front/side/back/three-quarter + small views: rendered for visual review.");
        report.Add("These are Unity camera renders, not physical Game-window input validation. User visual approval: PENDING. No locomotion clips created.");
        File.WriteAllLines("Docs/Stage22Verification.txt",report);
    }
    static void Capture(Camera camera,string name,int width,int height)
    {
        var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);rt.antiAliasing=4;rt.Create();
        var previous=camera.targetTexture;camera.targetTexture=rt;
        // Warm SRP/material state before recording a new view in batch mode.
        camera.Render();camera.Render();
        var previousActive=RenderTexture.active;RenderTexture.active=rt;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
        File.WriteAllBytes(Captures+"/"+name+".png",texture.EncodeToPNG());
        RenderTexture.active=previousActive;camera.targetTexture=previous;
        UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
}
