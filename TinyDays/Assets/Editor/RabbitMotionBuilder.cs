using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Review;

public static class RabbitMotionBuilder
{
    public const string ScenePath="Assets/Scenes/RabbitMotionStudy.unity";
    const string ModelPath="Assets/Art/Generated/RabbitMotion.fbx";
    const string CapturePath="Docs/Captures/Stage23/Unity";
    static readonly string[] ClipNames={"Idle_Biped","Idle_Quadruped","Walk_Biped","Hop_Quadruped","To_Quadruped","To_Biped","Walk_Start","Walk_Stop","Hop_Start","Hop_Stop"};
    [Serializable] class Joint { public string name;public float[] position; }
    [Serializable] class Sample { public float time;public float[] min,max;public string[] contactNames;public Joint[] joints; }
    [Serializable] class TravelKey {public float time,distance;}
    [Serializable] class ClipAudit {public string name,next;public bool loop;public float duration,distance;public Sample[] samples;public TravelKey[] travel;}
    [Serializable] class Audit {public ClipAudit[] clips;}
    static void Ensure(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    [MenuItem("Tiny Days/Stage 2-3/Open motion review")]
    public static void OpenReview()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
    [MenuItem("Tiny Days/Stage 2-3/Rebuild and verify motion connections")]
    public static void Execute()
    {
        try { Build();Verify();Debug.Log("TINYDAYS_STAGE23_OK"); }
        catch(Exception ex){Debug.LogException(ex);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    static Material Material(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/Materials/"+name+".mat");
    public static void Build()
    {
        Ensure(!Application.isPlaying,"Exit Play mode before rebuilding.");
        Directory.CreateDirectory(CapturePath);AssetDatabase.Refresh();
        var importer=AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        Ensure(importer!=null,"Generate RabbitMotion.fbx first.");
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.importCameras=false;importer.importLights=false;importer.optimizeGameObjects=false;importer.isReadable=true;
        importer.globalScale=1;importer.importNormals=ModelImporterNormals.Import;
        foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Generated/Materials"})) {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),mat);
        }
        importer.SaveAndReimport();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath) {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())throw new OperationCanceledException();
            scene=File.Exists(ScenePath)?EditorSceneManager.OpenScene(ScenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
        var old=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="GeneratedMotionReview");if(old)UnityEngine.Object.DestroyImmediate(old);
        if(!scene.GetRootGameObjects().Any(o=>o.name=="ManualEdits"))new GameObject("ManualEdits");
        var root=new GameObject("GeneratedMotionReview");
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Review ground";ground.transform.SetParent(root.transform);
        ground.transform.localPosition=Vector3.zero;ground.transform.localScale=Vector3.one*200;
        ground.GetComponent<Renderer>().sharedMaterial=Material("Ground");UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
        // Stationary marks make contact sliding observable while the camera follows.
        for(int i=-6;i<100;i++) {
            var mark=GameObject.CreatePrimitive(PrimitiveType.Cube);mark.name="Ground marker";mark.transform.SetParent(root.transform);
            mark.transform.localPosition=new Vector3(.72f,.003f,i*.30f);mark.transform.localScale=new Vector3(.10f,.005f,.025f);
            mark.GetComponent<Renderer>().sharedMaterial=Material("Stitch");UnityEngine.Object.DestroyImmediate(mark.GetComponent<Collider>());
        }
        var lightGo=new GameObject("Warm daylight");lightGo.transform.SetParent(root.transform);lightGo.transform.rotation=Quaternion.Euler(42,144,0);
        var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.05f;light.color=new Color(1,.94f,.85f);
        light.shadows=LightShadows.Soft;light.shadowBias=.015f;light.shadowNormalBias=.1f;
        RenderSettings.ambientMode=AmbientMode.Custom;var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.30f,.33f,.36f));RenderSettings.ambientProbe=probe;
        RenderSettings.reflectionIntensity=.2f;RenderSettings.fog=false;
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));go.name="Rabbit motion";go.transform.SetParent(root.transform);
        go.GetComponent<Animator>().applyRootMotion=false;
        go.GetComponent<Animator>().enabled=false;
        foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
        var cameraGo=new GameObject("Motion camera");cameraGo.tag="MainCamera";cameraGo.transform.SetParent(root.transform);
        var camera=cameraGo.AddComponent<Camera>();camera.orthographic=true;camera.nearClipPlane=.05f;camera.farClipPlane=100;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.90f,.89f,.85f);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var review=root.AddComponent<RabbitMotionReview>();review.resident=go;review.reviewCamera=camera;
        var clips=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        review.clips=ClipNames.Select(n=>clips.Single(c=>c.name.EndsWith(n))).ToArray();
        var audit=JsonUtility.FromJson<Audit>(File.ReadAllText("Assets/Art/Generated/RabbitMotion.audit.json"));
        review.cycleDistances=ClipNames.Select(n=>audit.clips.Single(c=>c.name==n).distance).ToArray();
        review.looping=ClipNames.Select(n=>audit.clips.Single(c=>c.name==n).loop).ToArray();
        review.following=ClipNames.Select(n=>Array.IndexOf(ClipNames,audit.clips.Single(c=>c.name==n).next)).ToArray();
        review.travelCurves=ClipNames.Select(n=>{
            var curve=new AnimationCurve(audit.clips.Single(c=>c.name==n).travel.Select(k=>new Keyframe(k.time,k.distance)).ToArray());
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            return curve;
        }).ToArray();
        Ensure(review.following.All(i=>i>=0),"Missing follow clip");
        review.StartSequence();
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    }
    // FBX handedness conversion maps Blender (x,y,z) to Unity (-x,z,-y).
    static Vector3 Convert(float[] p)=>new Vector3(-p[0],p[2],-p[1]);
    static Vector3[] SkinPoints(GameObject go)
    {
        var points=new List<Vector3>();
        foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var vertices=mesh.vertices;
            var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
            for(int i=0;i<vertices.Length;i++) {var w=weights[i];var v=vertices[i];points.Add(matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3);}
        }
        return points.ToArray();
    }
    public static void Verify()
    {
        var log=new List<string>{"Stage 2-3 elastic motion automatic verification","Imported pose snapshots use explicitly evaluated skin matrices for deterministic batch rendering. They are not actual Game-window input checks."};
        var manual=GameObject.Find("ManualEdits");var sentinel=new GameObject("__Stage23Probe_"+Guid.NewGuid().ToString("N"));sentinel.transform.SetParent(manual.transform);sentinel.transform.localPosition=new Vector3(11,12,13);
        int id=sentinel.GetInstanceID();Build();Build();
        Ensure(sentinel&&sentinel.GetInstanceID()==id&&sentinel.transform.localPosition==new Vector3(11,12,13),"Manual preservation failed");
        UnityEngine.Object.DestroyImmediate(sentinel);log.Add("Repeated generation preserves ManualEdits: PASS");
        var review=UnityEngine.Object.FindObjectOfType<RabbitMotionReview>();var resident=review.resident;
        var avatar=resident.GetComponent<Animator>().avatar;Ensure(avatar&&avatar.isValid&&!avatar.isHuman,"Generic avatar invalid");
        Ensure(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,"URP not active");
        Ensure(resident.GetComponentsInChildren<SkinnedMeshRenderer>().All(s=>s.sharedMaterials.All(m=>m&&m.shader.name=="Universal Render Pipeline/Lit")),"Materials invalid");
        var audit=JsonUtility.FromJson<Audit>(File.ReadAllText("Assets/Art/Generated/RabbitMotion.audit.json"));
        for(int i=0;i<ClipNames.Length;i++) {
            review.Select(i);var source=audit.clips.Single(c=>c.name==ClipNames[i]);var clip=review.clips[i];
            Ensure(Mathf.Abs(clip.length-source.duration)<.002f,"Clip duration changed "+clip.name);
            float maxJoint=0,maxBounds=0;
            foreach(var sample in source.samples) {
                // Sample exact endpoint too; runtime uses modulo only for looping.
                resident.transform.localPosition=Vector3.zero;clip.SampleAnimation(resident,sample.time);
                foreach(var j in sample.joints) {
                    var bone=resident.GetComponentsInChildren<Transform>().Single(t=>t.name==j.name);
                    float delta=Vector3.Distance(bone.position,Convert(j.position));maxJoint=Mathf.Max(maxJoint,delta);
                }
                var points=SkinPoints(resident);var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
                var expectedMin=new Vector3(-sample.max[0],sample.min[2],-sample.max[1]);
                var expectedMax=new Vector3(-sample.min[0],sample.max[2],-sample.min[1]);
                maxBounds=Mathf.Max(maxBounds,Vector3.Distance(bounds.min,expectedMin),Vector3.Distance(bounds.max,expectedMax));
                Ensure(bounds.min.y>-.045f,"Ground penetration: "+clip.name+" "+sample.time);
            }
            Ensure(maxJoint<.015f,$"Joint import mismatch {clip.name}: {maxJoint}");
            Ensure(maxBounds<.02f,$"Skin import mismatch {clip.name}: {maxBounds}");
            resident.transform.localPosition=Vector3.zero;clip.SampleAnimation(resident,0);var start=SkinPoints(resident);
            clip.SampleAnimation(resident,clip.length);var end=SkinPoints(resident);
            float loop=start.Zip(end,(a,b)=>Vector3.Distance(a,b)).Max();if(source.loop)Ensure(loop<.002f,"Loop seam "+clip.name);
            log.Add($"{clip.name}: {(source.loop?"loop":"one-shot")} {clip.length:F3}s; joint delta {maxJoint:F6}m; skin bounds delta {maxBounds:F6}m"+(source.loop?$"; loop max {loop:F6}m":"")+": PASS");
            review.view=0;review.small=false;
            for(int v=0;v<4;v++) {
                review.view=v;
                int count=source.loop?4:5;
                for(int f=0;f<count;f++) {review.SamplePose(i,clip.length*f/4,review.travelCurves[i].Evaluate(clip.length*f/4));Capture(review.reviewCamera,$"{ClipNames[i]}_View{v}_Phase{f}",640,640);}
            }
            review.view=0;review.small=true;review.SamplePose(i,clip.length*.25,review.travelCurves[i].Evaluate(clip.length*.25f));Capture(review.reviewCamera,ClipNames[i]+"_Small",640,480);
        }
        int[] path=RabbitMotionReview.SequenceClips.Concat(new[]{0}).ToArray();
        for(int i=0;i<path.Length-1;i++){
            int a=path[i],b=path[i+1];
            resident.transform.localPosition=Vector3.zero;review.clips[a].SampleAnimation(resident,review.clips[a].length);var end=SkinPoints(resident);
            review.clips[b].SampleAnimation(resident,0);var start=SkinPoints(resident);
            float delta=end.Zip(start,(p,q)=>Vector3.Distance(p,q)).Max();Ensure(delta<.003f,$"Connector seam {ClipNames[a]} -> {ClipNames[b]}: {delta}");
            log.Add($"{ClipNames[a]} -> {ClipNames[b]}: surface endpoint delta {delta:F6}m: PASS");
        }
        review.StartSequence();double boundary=0;int[] repeat={1,1,2,1,1,1,1,3,1,1};
        for(int i=0;i<path.Length-1;i++){
            boundary+=review.clips[path[i]].length*repeat[i];
            review.Sample(boundary-.00001);var before=SkinPoints(resident);var pos=resident.transform.position;
            review.Sample(boundary+.00001);var after=SkinPoints(resident);
            Ensure(Vector3.Distance(pos,resident.transform.position)<.002f,"Root jump at "+boundary);
            Ensure(before.Zip(after,(p,q)=>Vector3.Distance(p,q)).Max()<.005f,"Runtime connector jump at "+boundary);
        }
        review.Sample(review.SequenceDuration+30);Ensure(review.activeClip==0,"Sequence replayed rather than staying in final idle");
        var finalPosition=resident.transform.position;review.Sample(review.SequenceDuration+60);Ensure(resident.transform.position==finalPosition,"Final idle moved");
        review.Sample(0);Ensure(resident.transform.position==Vector3.zero&&review.activeClip==0,"Sequence reset failed");
        for(int i=4;i<ClipNames.Length;i++){
            review.Select(i);review.Sample(review.clips[i].length+.1);Ensure(review.activeClip==review.following[i],"One-shot did not follow correctly");
            Ensure(resident.transform.position.z>=review.cycleDistances[i]-.00001f,"One-shot lost cumulative travel");
        }
        log.Add("Runtime sequence boundaries, cumulative travel, final idle, replay reset and one-shot following: PASS");
        review.small=false;review.view=0;review.StartSequence();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        log.Add("Elastic basic motion and transition user approval: PENDING. Previous approval does not cover revised motion. Special actions: NOT CREATED.");
        File.WriteAllLines("Docs/Stage23Verification.txt",log);
    }
    static void Capture(Camera camera,string name,int width,int height)
    {
        // Camera.Render in a synchronous editor batch does not advance the skinning
        // player loop. Freeze the evaluated imported mesh for this snapshot only.
        // Runtime still uses the original SkinnedMeshRenderer and Generic Playable.
        var review=UnityEngine.Object.FindObjectOfType<RabbitMotionReview>();
        var skin=review.resident.GetComponentInChildren<SkinnedMeshRenderer>();
        var snapshot=UnityEngine.Object.Instantiate(skin.sharedMesh);
        snapshot.vertices=SkinPoints(review.resident);
        var normals=skin.sharedMesh.normals;var weights=skin.sharedMesh.boneWeights;
        var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*skin.sharedMesh.bindposes[i]).ToArray();
        for(int i=0;i<normals.Length;i++) {var w=weights[i];var n=normals[i];normals[i]=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;}
        snapshot.normals=normals;snapshot.RecalculateBounds();
        var proxy=new GameObject("__BatchPoseSnapshot");proxy.AddComponent<MeshFilter>().sharedMesh=snapshot;proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
        skin.enabled=false;
        var rt=new RenderTexture(width,height,24);rt.antiAliasing=4;rt.Create();var old=camera.targetTexture;camera.targetTexture=rt;
        camera.Render();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
        File.WriteAllBytes(CapturePath+"/"+name+".png",texture.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=old;
        UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        skin.enabled=true;UnityEngine.Object.DestroyImmediate(proxy);UnityEngine.Object.DestroyImmediate(snapshot);
    }
}
