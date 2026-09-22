using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyDays.Characters;
using TinyDays.Review;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class AdultRabbitBuilder
{
    const string Folder="Assets/Art/Generated/AdultRabbit";
    const string ScenePath="Assets/Scenes/AdultRabbitStudy.unity";
    const string CaptureFolder="Docs/Captures/AdultRabbit";
    const string Owner="GeneratedAdultRabbitReview";
    static readonly string[] MatNames={"Fur","Cloth","Pink","Dark","Leather","Brass"};
    static readonly string[] Colors={"F3E8DB","D9DBD7","CF8C89","403D42","A87147","D3B477"};
    static readonly string[] BodyNames={"Head","BodyTorso","BodyArms","BodyLegs","BodyFeet","BodyHands","BodyTail"};
    static void Check(bool valid,string message){if(!valid)throw new Exception(message);}
    [MenuItem("Tiny Days/Adult rabbit/Rebuild and verify")]
    public static void Execute()
    {
        try{Build();Verify();Debug.Log("ADULT_RABBIT_UNITY_OK");}
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    [MenuItem("Tiny Days/Adult rabbit/Open design review")]
    public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
    public static void Build()
    {
        Directory.CreateDirectory(Folder);Directory.CreateDirectory(CaptureFolder);AssetDatabase.Refresh();
        var importer=AssetImporter.GetAtPath(Folder+"/AdultRabbit.fbx") as ModelImporter;
        Check(importer!=null,"Missing AdultRabbit FBX");
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=false;importer.isReadable=true;importer.optimizeGameObjects=false;
        importer.importNormals=ModelImporterNormals.Import;importer.importCameras=false;importer.importLights=false;
        for(int i=0;i<MatNames.Length;i++){
            string path=Folder+"/"+MatNames[i]+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            ColorUtility.TryParseHtmlString("#"+Colors[i],out var color);m.SetColor("_BaseColor",color);
            m.SetFloat("_Smoothness",MatNames[i]=="Dark"?.25f:MatNames[i]=="Leather"?.28f:MatNames[i]=="Brass"?.4f:.12f);m.SetFloat("_Metallic",MatNames[i]=="Brass"?.35f:0);EditorUtility.SetDirty(m);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),MatNames[i]),m);
        }
        importer.SaveAndReimport();
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)scene=File.Exists(ScenePath)?EditorSceneManager.OpenScene(ScenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==Owner);if(existing)UnityEngine.Object.DestroyImmediate(existing);
        if(!scene.GetRootGameObjects().Any(g=>g.name=="ManualEdits"))new GameObject("ManualEdits");
        var root=new GameObject(Owner);
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/AdultRabbit.fbx"));
        actor.name="AdultRabbit";actor.transform.SetParent(root.transform,false);
        var animator=actor.GetComponent<Animator>();if(animator)animator.enabled=false;
        var character=actor.AddComponent<ModularCharacter>();character.bodyFamily="AdultStandard_v2";
        character.bodyParts=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>BodyNames.Contains(s.name)).ToArray();
        Check(character.bodyParts.Length==7,"Expected seven independently masked body modules");
        var wardrobe=new List<CharacterWearable>();
        foreach(WearSlot slot in Enum.GetValues(typeof(WearSlot))){
            var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name==slot.ToString());
            string path=Folder+"/"+slot+".asset";var item=AssetDatabase.LoadAssetAtPath<CharacterWearable>(path);
            if(!item){item=ScriptableObject.CreateInstance<CharacterWearable>();AssetDatabase.CreateAsset(item,path);}
            item.age=BodyAge.Adult;item.bodyFamily="AdultStandard_v2";item.slot=slot;item.mesh=skin.sharedMesh;item.materials=skin.sharedMaterials;
            item.boneNames=skin.bones.Select(b=>b.name).ToArray();item.rootBoneName=skin.rootBone.name;
            Check(skin.transform.parent==actor.transform,"Unexpected FBX skin hierarchy");
            item.localPosition=skin.transform.localPosition;item.localRotation=skin.transform.localRotation;item.localScale=skin.transform.localScale;
            item.localBounds=skin.localBounds;
            item.coveredBodyParts=slot==WearSlot.Top?new[]{"BodyTorso","BodyArms"}:slot==WearSlot.Bottom?new[]{"BodyLegs"}:slot==WearSlot.Shoes?new[]{"BodyFeet"}:Array.Empty<string>();
            EditorUtility.SetDirty(item);wardrobe.Add(item);UnityEngine.Object.DestroyImmediate(skin.gameObject);
        }
        character.wardrobe=wardrobe.ToArray();
        // Prefab stores only the body, skeleton and wardrobe references; runtime Dress constructs gear.
        PrefabUtility.SaveAsPrefabAsset(actor,Folder+"/AdultRabbit.prefab");
        character.Dress();
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Review floor";ground.transform.SetParent(root.transform);ground.transform.position=new Vector3(0,-.018f,0);ground.transform.localScale=Vector3.one*10;
        string floorPath=Folder+"/ReviewFloor.mat";var floor=AssetDatabase.LoadAssetAtPath<Material>(floorPath);
        if(!floor){floor=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(floor,floorPath);}
        floor.SetColor("_BaseColor",new Color(.79f,.82f,.80f));floor.SetFloat("_Smoothness",0);ground.GetComponent<Renderer>().sharedMaterial=floor;
        foreach(var entry in new[]{new Vector4(40,-25,0,1.1f),new Vector4(20,140,0,.5f)}){
            var go=new GameObject("Review light");go.transform.SetParent(root.transform);go.transform.rotation=Quaternion.Euler(entry.x,entry.y,entry.z);
            var l=go.AddComponent<Light>();l.type=LightType.Directional;l.intensity=entry.w;l.color=Color.white;l.shadows=entry.w>1?LightShadows.Soft:LightShadows.None;l.shadowBias=.01f;l.shadowNormalBias=.02f;}
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.72f,.75f,.8f);RenderSettings.fog=false;
        var cg=new GameObject("Adult review camera");cg.transform.SetParent(root.transform);cg.tag="MainCamera";
        var cam=cg.AddComponent<Camera>();cam.fieldOfView=35;cam.nearClipPlane=.03f;cam.farClipPlane=70;cam.backgroundColor=new Color(.84f,.86f,.85f);cam.clearFlags=CameraClearFlags.SolidColor;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var review=root.AddComponent<AdultRabbitReview>();review.character=character;review.poseRoot=actor.transform;review.reviewCamera=cam;review.Home();
        // Remove runtime gear before serialization so Start never duplicates persisted equipment.
        character.Undress();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();character.Dress();
    }
    static Vector3[] WorldVertices(SkinnedMeshRenderer skin)
    {
        var m=skin.sharedMesh;var v=m.vertices;var w=m.boneWeights;var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*m.bindposes[i]).ToArray();
        for(int i=0;i<v.Length;i++){
            var p=v[i];var b=w[i];v[i]=matrices[b.boneIndex0].MultiplyPoint3x4(p)*b.weight0+matrices[b.boneIndex1].MultiplyPoint3x4(p)*b.weight1+matrices[b.boneIndex2].MultiplyPoint3x4(p)*b.weight2+matrices[b.boneIndex3].MultiplyPoint3x4(p)*b.weight3;}
        return v;
    }
    static void Verify()
    {
        var sentinel=new GameObject("AdultManualSentinel");sentinel.transform.SetParent(GameObject.Find("ManualEdits").transform);sentinel.transform.localPosition=new Vector3(9,8,7);
        Build();Build();Check(sentinel&&sentinel.transform.localPosition==new Vector3(9,8,7),"Manual edit lost");UnityEngine.Object.DestroyImmediate(sentinel);
        var review=GameObject.Find(Owner).GetComponent<AdultRabbitReview>();var actor=review.character;
        review.SetPose(0); // Populate rest-pose cache before repeated gear replacement.
        var lines=new List<string>{"Adult rabbit review / automatic and camera render checks; not live input or minimum-PC benchmark.","ManualEdits survives repeated generation: PASS"};
        Check(actor.bodyFamily=="AdultStandard_v2"&&actor.wardrobe.All(w=>w.bodyFamily==actor.bodyFamily),"Mixed bind-pose families");
        var headSkin=actor.bodyParts.Single(s=>s.name=="Head");
        float headWidth=WorldVertices(headSkin).Max(v=>v.x)-WorldVertices(headSkin).Min(v=>v.x);
        Check(headWidth<.80f,"Head width regression");
        var skeleton=actor.bodyParts.SelectMany(s=>s.bones).Distinct().ToArray();
        float shoulderWidth=Vector3.Distance(skeleton.Single(b=>b.name=="UpperArm_L").position,skeleton.Single(b=>b.name=="UpperArm_R").position);
        Check(Mathf.Abs(shoulderWidth-.408f)<.002f,"Shoulder width regression");
        lines.Add("v2 head width: "+headWidth.ToString("F3")+"m; shoulder joint spacing: "+shoulderWidth.ToString("F3")+"m. Reference likeness remains a visual judgment.");
        var allMeshes=actor.bodyParts.Select(s=>s.sharedMesh).Concat(actor.wardrobe.Select(w=>w.mesh)).Distinct().ToArray();
        int triangles=allMeshes.Sum(m=>m.triangles.Length/3);Check(triangles<=6000,"Triangle budget exceeded: "+triangles);
        foreach(var m in allMeshes){Check(m.isReadable,"Unreadable mesh");foreach(var w in m.boneWeights)Check(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.001f,"Unweighted vertex");}
        for(int cycle=0;cycle<3;cycle++)foreach(var item in actor.wardrobe){
            actor.Unequip(item.slot);foreach(var n in item.coveredBodyParts)Check(actor.bodyParts.Single(p=>p.name==n).enabled,"Body not restored");
            Check(actor.Equip(item),"Cannot re-equip");foreach(var n in item.coveredBodyParts)Check(!actor.bodyParts.Single(p=>p.name==n).enabled,"Covered body still visible");}
        var invalid=ScriptableObject.Instantiate(actor.wardrobe[0]);invalid.age=BodyAge.Child;
        Check(!actor.Equip(invalid)&&actor.IsEquipped(WearSlot.Top),"Incompatible age replaced gear");
        invalid.age=BodyAge.Adult;invalid.bodyFamily="AdultStandard_v1";Check(!actor.Equip(invalid)&&actor.IsEquipped(WearSlot.Top),"Old bind-pose family accepted");
        invalid.bodyFamily="SpecialBody";Check(!actor.Equip(invalid),"Special body accepted");UnityEngine.Object.DestroyImmediate(invalid);
        // Rebind the identical garment assets to a separate adult rig and compare skin positions.
        var clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/AdultRabbit.prefab"));
        var other=clone.GetComponent<ModularCharacter>();other.Dress();
        foreach(var item in actor.wardrobe){
            var a=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Wear_"+item.slot);
            var b=other.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Wear_"+item.slot);
            Check(a.sharedMesh==b.sharedMesh,"Garment geometry not shared");
            var av=WorldVertices(a);var bv=WorldVertices(b);Check(av.Zip(bv,(x,y)=>Vector3.Distance(x,y)).Max()<.001f,"Rebound geometry mismatch");}
        UnityEngine.Object.DestroyImmediate(clone);
        var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        var verts=skins.SelectMany(WorldVertices).ToArray();Check(verts.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.y)),"Invalid deformation");
        Check(verts.Max(v=>v.y)>2.18f&&verts.Max(v=>v.y)<2.23f,"Short-ear scale/axis regression");
        Check(verts.Min(v=>v.y)>-.02f,"Feet below floor");
        var materials=skins.SelectMany(s=>s.sharedMaterials).Distinct().ToArray();Check(materials.Length<=6,"Material budget");
        lines.Add("All modules triangles: "+triangles+"; Unity imported vertices: "+allMeshes.Sum(m=>m.vertexCount));
        lines.Add("Dressed triangles: "+skins.Sum(s=>s.sharedMesh.triangles.Length/3)+"; active skinned renderers: "+skins.Length+"; unique character materials: "+materials.Length);
        lines.Add("Three equip/unequip cycles, covered body recovery, age/v1/special-family rejection, separate v2 adult rig rebind with shared mesh: PASS");
        foreach(var item in actor.wardrobe)lines.Add(item.slot+": "+item.mesh.triangles.Length/3+" triangles / bones="+item.boneNames.Length);
        var cam=review.reviewCamera;
        foreach(var pair in new Dictionary<string,Vector3>{{"Front",new Vector3(0,1.30f,4.8f)},{"Side",new Vector3(4.8f,1.3f,0)},{"Rear",new Vector3(0,1.3f,-4.8f)},{"ThreeQuarter",new Vector3(2.8f,1.75f,4.2f)},{"Backpack",new Vector3(3,1.7f,-4)}}){
            SetCamera(cam,pair.Value);Capture(cam,pair.Key,960,960);}
        SetCamera(cam,new Vector3(2.8f,1.75f,4.2f));Capture(cam,"Small",400,400);
        foreach(var pair in new Dictionary<string,Vector3>{{"FaceFront",new Vector3(0,1.51f,1.8f)},{"FaceSide",new Vector3(1.8f,1.51f,0)},{"FaceQuarter",new Vector3(1.2f,1.56f,1.4f)}}){cam.transform.position=pair.Value;cam.transform.LookAt(new Vector3(0,1.50f,0));Capture(cam,pair.Key,960,960);}
        SetCamera(cam,new Vector3(3.3f,2.5f,-3.5f));Capture(cam,"ReferenceAngle",1200,1200);
        cam.transform.position=new Vector3(1.5f,1.6f,-2.0f);cam.transform.LookAt(new Vector3(0,1.03f,0));Capture(cam,"BackpackDetail",1200,1200);
        cam.transform.position=new Vector3(1.7f,.12f,2.6f);cam.transform.LookAt(new Vector3(0,.62f,0));Capture(cam,"CoatHemLow",960,960);
        SetCamera(cam,new Vector3(2.8f,1.75f,4.2f));
        // All eight Top/Neckwear/Backpack combinations, not only one-at-a-time toggles.
        var reviewSlots=new[]{WearSlot.Top,WearSlot.Neckwear,WearSlot.Backpack};
        for(int mask=0;mask<8;mask++){
            actor.Dress();
            for(int bit=0;bit<3;bit++)if((mask&(1<<bit))==0)actor.Unequip(reviewSlots[bit]);
            Check(actor.bodyParts.Single(s=>s.name=="BodyTorso").enabled==((mask&1)==0),"Combination body visibility");
            Capture(cam,"WearCombination"+mask,600,600);
        }
        actor.Dress();lines.Add("Eight Top/Neckwear/Backpack combinations: body visibility PASS; captured for visual review.");
        actor.Undress();Capture(cam,"BaseBody",960,960);actor.Dress();
        var neutral=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).SelectMany(WorldVertices).ToArray();
        for(int pose=1;pose<=2;pose++){
            review.SetPose(pose);var posed=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).SelectMany(WorldVertices).ToArray();
            Check(posed.All(v=>!float.IsNaN(v.x)&&v.magnitude<5),"Exploding pose");
            float poseDelta=neutral.Zip(posed,(a,b)=>Vector3.Distance(a,b)).Max();
            lines.Add("Static pose "+pose+" evaluated vertex displacement: "+poseDelta.ToString("F3")+"m (deformation probe, not animation approval).");
            Check(poseDelta>.1f,"Pose did not change geometry");
            Capture(cam,pose==1?"JointBend":"QuadrupedProbe",960,960);}
        review.SetPose(0);review.Home();actor.Undress();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        lines.Add("Pose captures: static joint/low-posture probes only, not final quadruped locomotion or foot-contact approval.");
        lines.Add("Other species/Infant/Child not implemented or verified. User design approval and live Game input: PENDING.");
        File.WriteAllLines("Docs/AdultRabbitVerification.txt",lines);AssetDatabase.SaveAssets();
    }
    static void SetCamera(Camera c,Vector3 p){c.transform.position=p;c.transform.LookAt(new Vector3(0,1.22f,0));}
    static void Capture(Camera c,string name,int width,int height)
    {
        // Evaluate static pose geometry explicitly: batch camera.Render does not run a
        // PlayerLoop, and can otherwise reuse the previous skinned GPU buffers.
        var skins=GameObject.Find(Owner).GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
        var snapshots=new List<GameObject>();var meshes=new List<Mesh>();
        foreach(var skin in skins){
            var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.vertices=WorldVertices(skin);
            var normals=skin.sharedMesh.normals;var weights=skin.sharedMesh.boneWeights;
            var matrices=skin.bones.Select((b,i)=>(b.localToWorldMatrix*skin.sharedMesh.bindposes[i]).inverse.transpose).ToArray();
            for(int i=0;i<normals.Length;i++){var n=normals[i];var w=weights[i];normals[i]=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;}
            mesh.normals=normals;mesh.RecalculateBounds();
            var go=new GameObject("__AdultCapture");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
            snapshots.Add(go);meshes.Add(mesh);skin.enabled=false;
        }
        var rt=new RenderTexture(width,height,24);rt.antiAliasing=4;rt.Create();var old=c.targetTexture;var active=RenderTexture.active;
        c.targetTexture=rt;c.Render();c.Render();RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(CaptureFolder+"/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=active;c.targetTexture=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        foreach(var go in snapshots)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var skin in skins)skin.enabled=true;
    }
    public static void BuildPlayer()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        Check(report.summary.result==BuildResult.Succeeded,"Adult rabbit player failed");Debug.Log("ADULT_RABBIT_PLAYER_OK");
    }
}
