using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TinyDays.Review;
using Model=FarmLowPolyAssets.Model;

public static class RabbitHomeLifeBuilder
{
    const string ScenePath="Assets/Scenes/RabbitHomeLifeStudy.unity";
    const string Fbx="Assets/Art/Generated/RabbitHome/AdultRabbitHome.fbx";
    const string Folder="Assets/Art/Generated/RabbitHome";
    const string Owner="GeneratedRabbitHomeLife";
    const float Scale=1.25f;
    static void Check(bool pass,string why){if(!pass)throw new Exception("Rabbit home: "+why);}
    [MenuItem("Tiny Days/Rabbit home/Open life review")]
    public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
    public static void BuildOnly(){Build();Debug.Log("RABBIT_HOME_BUILD_OK");}
    public static void Execute(){Build();Verify();Debug.Log("RABBIT_HOME_OK");}
    static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(HouseVillageAssets.Folder+"/"+name+".mat");
    static Mesh ReviewDarkWood(Mesh source)
    {
        // The original cottage stores three decorative door slats in the
        // DarkWood mesh, independently of its Door mesh. Remove only those
        // review-copy triangles so they cannot remain across the opening.
        string path=Folder+"/RabbitHomeDarkWood.asset";
        var vertices=source.vertices;var triangles=source.triangles;var kept=new List<int>(triangles.Length);
        int removed=0;
        for(int i=0;i<triangles.Length;i+=3){
            Vector3 center=(vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3;
            bool slat=center.x>-.75f&&center.x<.08f&&center.y>.28f&&center.y<2.12f&&center.z>-1.73f&&center.z<-1.69f;
            if(slat){removed++;continue;}
            kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);
        }
        Check(removed==36,"expected exactly three original door slats; removed "+removed+" triangles");
        var review=UnityEngine.Object.Instantiate(source);review.name="RabbitHomeDarkWood";review.triangles=kept.ToArray();review.RecalculateBounds();
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(review,existing);UnityEngine.Object.DestroyImmediate(review);return existing;}
        AssetDatabase.CreateAsset(review,path);return review;
    }
    static GameObject CopyHouse(Transform parent)
    {
        var house=new GameObject("Straw Cottage · review copy");house.transform.SetParent(parent,false);
        house.transform.localScale=Vector3.one*Scale;
        const string prefix="Straw Cottage-";
        string[] names={"IvoryWall","WoodTrim","Glass","Straw","StrawAlt","Stone","Cream","DarkWood"};
        foreach(string material in names){
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(HouseVillageAssets.Folder+"/"+prefix+material+".asset");
            if(!mesh)continue;
            if(material=="DarkWood")mesh=ReviewDarkWood(mesh);
            if(material=="Stone"){
                var copy=UnityEngine.Object.Instantiate(mesh);copy.name="RabbitHomeFoundation";
                copy.vertices=copy.vertices.Select(v=>new Vector3(v.x,v.y>.2f?v.y-.02f:v.y,v.z)).ToArray();copy.RecalculateBounds();
                string path=Folder+"/RabbitHomeFoundation.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing){EditorUtility.CopySerialized(copy,existing);UnityEngine.Object.DestroyImmediate(copy);mesh=existing;}
                else{AssetDatabase.CreateAsset(copy,path);mesh=copy;}
            }
            var part=new GameObject(material);part.transform.SetParent(house.transform,false);
            part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=Mat(material);
        }
        var volume=house.AddComponent<FarmInteriorVolume>();volume.center=new Vector3(0,(2.5f+.22f)/2,0);volume.size=new Vector3(3.8f,2.28f,3.5f);volume.roofRise=1.55f;
        return house;
    }
    static void Door(Transform house,RabbitHomeLifeReview review)
    {
        // Original opening: x=-.35 +/- .45, bottom .22, top 2.17 (house-local).
        // A dedicated review mesh avoids modifying the shared closed-door mesh.
        var hinge=new GameObject("Door hinge · outside left");hinge.transform.SetParent(house,false);
        hinge.transform.localPosition=new Vector3(-.78f,0,-1.685f);
        var materials=new Dictionary<string,Material>{{"Door",Mat("Door")},{"DarkWood",Mat("DarkWood")},{"Brass",Mat("Brass")}};
        var door=new Model();
        door.Box("Door",new Vector3(.39f,1.195f,0),new Vector3(.78f,1.90f,.07f));
        foreach(float x in new[]{.16f,.39f,.62f})door.Box("DarkWood",new Vector3(x,1.195f,-.039f),new Vector3(.012f,1.75f,.008f));
        foreach(int side in new[]{-1,1}){
            door.Box("Brass",new Vector3(.65f,1.08f,side*.04f),new Vector3(.045f,.10f,.012f));
            door.Box("Brass",new Vector3(.65f,1.08f,side*.056f),new Vector3(.024f,.024f,.032f));
            door.Box("Brass",new Vector3(.625f,1.08f,side*.068f),new Vector3(.095f,.035f,.024f));
        }
        door.Finish("RabbitHomeDoor","Door leaf and handles",hinge.transform,Vector3.zero,materials,180,Folder);
        review.hinge=hinge.transform;
        var outside=new GameObject("Outside handle target");outside.transform.SetParent(hinge.transform,false);
        outside.transform.localPosition=new Vector3(.625f,1.08f,-.08f);review.outsideHandle=outside.transform;
        var inside=new GameObject("Inside handle target");inside.transform.SetParent(hinge.transform,false);
        inside.transform.localPosition=new Vector3(.625f,1.08f,.08f);review.insideHandle=inside.transform;
    }
    static void Floor(Transform root)
    {
        var mats=new Dictionary<string,Material>{{"Ground",Mat("Ground")},{"DarkWood",Mat("DarkWood")}};
        for(int i=0;i<2;i++){
            string key="FloorWood"+i,path=Folder+"/"+key+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Mat("Door"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",i==0?new Color(.57f,.39f,.23f):new Color(.52f,.35f,.20f));mats.Add(key,material);
        }
        var floor=new Model();
        floor.Box("Ground",new Vector3(0,.115f,-1.8f),new Vector3(10,.26f,10));
        floor.Box("Ground",new Vector3(0,.26f,-4.5f),new Vector3(10,.03f,4.625f));
        foreach(int side in new[]{-1,1})floor.Box("Ground",new Vector3(side*3.75f,.26f,0),new Vector3(2.5f,.03f,4.375f));
        floor.Box("DarkWood",new Vector3(0,.2525f,0),new Vector3(4.55f,.025f,4.2f));
        const float width=4.55f,depth=4.2f,row=.2333333f;
        for(int z=0;z<18;z++)for(int x=0;x<5;x++){
            float a=-width/2+x*1.14f-(z%2)*.57f,b=Mathf.Min(width/2,a+1.14f);a=Mathf.Max(-width/2,a);
            if(b<=a)continue;
            floor.Box("FloorWood"+((x+z)%2),new Vector3((a+b)/2,.27f,-depth/2+(z+.5f)*row),new Vector3(b-a-.003f,.01f,row-.003f));
        }
        floor.Box("FloorWood0",new Vector3(-.4375f,.26f,-2.15f),new Vector3(.9f,.03f,.2f));
        floor.Finish("RabbitHomeFloor","Interior wooden planks and approach ground",root,Vector3.zero,mats,1600,Folder);
    }
    static AnimationClip Clip(AnimationClip[] all,string suffix)=>all.Single(c=>c.name.EndsWith(suffix,StringComparison.Ordinal));
    public static void Build()
    {
        Check(File.Exists(Fbx),"Blender home FBX missing; run Tools/build_rabbit_home_art.py");
        Check(AssetDatabase.LoadAssetAtPath<Mesh>(HouseVillageAssets.Folder+"/Straw Cottage-IvoryWall.asset"),"Build existing HouseVillage assets first");
        var importer=AssetImporter.GetAtPath(Fbx) as ModelImporter;Check(importer,"FBX import missing");
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;importer.importBlendShapes=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.optimizeGameObjects=false;importer.isReadable=true;importer.importNormals=ModelImporterNormals.Import;
        foreach(string name in new[]{"Fur","Cloth","Pink","Dark","Leather","Brass"}){
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/AdultRabbit/"+name+".mat");
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),material);
        }
        importer.clipAnimations=Array.Empty<ModelImporterClipAnimation>();importer.SaveAndReimport();
        var defaults=importer.defaultClipAnimations;
        foreach(var c in defaults)if(c.name.EndsWith("Adult_Idle_Biped")||c.name.EndsWith("Adult_Walk_Biped")){c.loopTime=true;c.loopPose=true;}
        importer.clipAnimations=defaults;importer.SaveAndReimport();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)scene=File.Exists(ScenePath)?EditorSceneManager.OpenScene(ScenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==Owner);if(old)UnityEngine.Object.DestroyImmediate(old);
        if(!scene.GetRootGameObjects().Any(g=>g.name=="ManualEdits"))new GameObject("ManualEdits");
        var root=new GameObject(Owner);var review=root.AddComponent<RabbitHomeLifeReview>();
        var house=CopyHouse(root.transform);Door(house.transform,review);
        // The slab top is .22*1.25=.275. Extend that same level across the
        // short indoor and outdoor path; the leaf retains its small bottom gap.
        Floor(root.transform);
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));actor.name="Adult rabbit resident";actor.transform.SetParent(root.transform,false);
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())if(new[]{"BodyTorso","BodyArms","BodyLegs","BodyFeet"}.Contains(skin.name))skin.enabled=false;
        actor.GetComponent<Animator>().enabled=false;review.resident=actor;
        var clips=AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        Check(clips.Length>=37,"Expected 36 retained actions and door reach");
        review.idleClip=Clip(clips,"Adult_Idle_Biped");review.walkClip=Clip(clips,"Adult_Walk_Biped");review.interactClip=Clip(clips,"Adult_Door_Interact");
        float x=-.35f*Scale,y=.275f;
        review.insideWait=new Vector3(x,y,.55f);review.insideDoor=new Vector3(x,y,0);
        review.outsideWait=new Vector3(x,y,-4.2f);review.outsideDoor=new Vector3(x,y,-2.95f);review.outsideClear=new Vector3(x,y,-2.95f);
        var sun=new GameObject("Review sunlight");sun.transform.SetParent(root.transform,false);sun.transform.rotation=Quaternion.Euler(48,-32,0);
        var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.color=new Color(1,.94f,.83f);light.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.68f,.75f,.79f);
        RenderSettings.ambientEquatorColor=new Color(.52f,.55f,.45f);RenderSettings.ambientGroundColor=new Color(.36f,.32f,.25f);
        var camera=new GameObject("Rabbit home camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.tag="MainCamera";
        camera.fieldOfView=40;camera.nearClipPlane=.03f;camera.farClipPlane=120;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.89f,.93f,.92f);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;review.reviewCamera=camera;
        RabbitWaterBuilder.Attach(review);
        RabbitBenchBuilder.Attach(review);
        review.Home();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    }
    static void Verify()
    {
        var r=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();
        Check(r&&r.resident&&r.hinge,"scene wiring");
        Directory.CreateDirectory("Docs/Captures/RabbitHomeLife");
        r.ResetStudy();r.ViewInside();Capture(r.reviewCamera,"InsideClosed");
        var rows=new List<string>{"v0.130 continue walking, automatic close; direct-call verification, not OS input."};
        foreach(bool exit in new[]{true,false}){
            r.Request(exit);bool opening=false,crossing=false,closing=false,resumed=false;int frame=0;float wait=0;
            Vector3 waitPosition=Vector3.zero;
            for(;frame<120*60&&!(exit?r.Outside:r.Inside);frame++){
                r.Advance(1f/60);
                if(r.Current==RabbitHomeLifeReview.Step.WaitForClosing){wait+=1f/60;waitPosition=r.resident.transform.position;}
                if(r.Current==RabbitHomeLifeReview.Step.WalkToDestination){resumed=true;}
                if(!opening&&r.Door==RabbitHomeLifeReview.DoorState.Opening&&Mathf.Abs(r.DoorAngle)>45){
                    r.View(155);Capture(r.reviewCamera,exit?"InsideOpening":"OutsideOpening");opening=true;
                }
                if(!crossing&&r.Current==(exit?RabbitHomeLifeReview.Step.CrossOut:RabbitHomeLifeReview.Step.CrossIn)){
                    Check(Mathf.Abs(r.DoorAngle-r.doorOpenAngle)<.01f,"crossing began before full opening");
                    r.View(155);Capture(r.reviewCamera,exit?"CrossOut":"CrossIn");crossing=true;
                }
                if(!closing&&r.Door==RabbitHomeLifeReview.DoorState.Closing){r.View(155);Capture(r.reviewCamera,exit?"ExitAutoClosing":"EntryAutoClosing");closing=true;}
            }
            Check(exit?r.Outside:r.Inside,$"trip did not finish: {r.Current} root {r.resident.transform.position} door {r.Door} {r.DoorAngle} radius {r.BodyClearanceRadius} feet {r.FootStatus}");
            Check(r.DoorClosed&&opening&&crossing&&closing&&resumed&&wait==0,"missing continuous/automatic close phase");
            Check(Vector3.Dot(r.resident.transform.forward,exit?Vector3.back:Vector3.forward)>.999f,"endpoint facing");
            rows.Add($"{(exit?"Exit":"Entry")}: {frame/60f:F3}s, root {r.resident.transform.position:F4}, forward {r.resident.transform.forward:F3}");
            rows.Add($"Closing wait {wait:F3}s at {waitPosition:F4}; resumed distance {Vector3.Distance(waitPosition,r.resident.transform.position):F3}m; target {r.ClosingWaitTarget(exit):F4}");
        }
        rows.Add($"sole {r.MinimumSole:F6}m; support gap {r.MaximumSupportGap:F6}m; planted drift {r.MaximumSupportDrift:F6}m; step reach {r.MaxStepReach:F6}m; maximum turn footfalls {r.MaxTurnSteps}; reverse travel {r.ReverseTravel:F6}m; lateral {r.MaximumLateralDeviation:F6}m");
        rows.Add($"Unsafe closing samples {r.UnsafeDoorSamples}; horizontal body safety radius {r.BodyClearanceRadius:F3}m; hand contact/gesture removed.");
        rows.Add("Support detail: "+r.SupportDetail+"; reach detail: "+r.ReachDetail);
        CheckClosing(r,2);rows.Add($"Closing stops {r.ClosingStops}; legacy wait drift {r.MaximumClosingDrift:F6}m; legacy wait samples {r.PrematureDepartureSamples}");
        File.WriteAllLines("Docs/RabbitHomeLifeVerification.txt",rows);Debug.Log(string.Join("\n",rows));
        Check(r.MinimumSole>=-.0005f&&r.MaximumSupportGap<=.005f&&r.MaximumSupportDrift<=.0035f,"foot contacts; see verification");
        Check(r.MaxTurnSteps<=3&&r.UnsafeDoorSamples==0&&r.EarlyPassSamples==0&&r.ReverseTravel<.001f&&r.MaximumLateralDeviation<.01f,"automatic route criteria");
        r.ResetStudy();EditorSceneManager.SaveScene(r.gameObject.scene,ScenePath);
    }
    static float BodyForwardExtent(GameObject resident)
    {
        float z=float.NegativeInfinity;
        foreach(var skin in resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled&&
            !new[]{"BodyHands","BodyArms"}.Contains(s.name))){
            z=Mathf.Max(z,skin.bounds.max.z);
        }
        return z;
    }
    static void Capture(Camera camera,string name,string directory="Docs/Captures/RabbitHomeLife")
    {
        var rt=new RenderTexture(960,720,24);var previous=RenderTexture.active;var old=camera.targetTexture;
        var image=new Texture2D(960,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.aspect=960f/720f;camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();
            ReviewCaptureFile.Write(directory+"/"+name+".png",image.EncodeToPNG());}
        finally{RenderTexture.active=previous;camera.targetTexture=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
    public static void BuildPlayer()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Logs/RabbitHomeLifePlayer/TinyDaysRabbitHomeLife.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        Check(report.summary.result==BuildResult.Succeeded,"Windows player build failed");Debug.Log("RABBIT_HOME_PLAYER_OK");
    }
    public static void Stress()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var r=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();Check(r,"Missing home scene");
        var lines=new List<string>{"Rabbit home deterministic editor scenario checks; not live OS input."};
        foreach(bool slow in new[]{false,true})foreach(int fps in new[]{30,60,120}){
            r.ResetStudy();r.slow=slow;r.RequestExit();
            int n=0;while(!r.Outside&&n++<fps*240)r.Advance(1f/fps);
            Check(r.Outside&&r.DoorClosed,$"exit stalled at {fps}fps {(slow?"0.5":"1")}x: {r.Current} {r.FootStatus}");
            r.RequestEnter();n=0;while(!r.Inside&&n++<fps*240)r.Advance(1f/fps);
            Check(r.Inside&&r.DoorClosed,$"entry stalled at {fps}fps {(slow?"0.5":"1")}x: {r.Current} {r.FootStatus}");
            Check(r.Trips==2&&r.UnsafeDoorSamples==0,"trip count/contact");
            CheckClosing(r,2);
            Check(r.UnsafeDoorSamples==0&&r.MaxTurnSteps<=3&&r.MinimumSole>=-.0005f&&r.MaximumSupportGap<=.005f&&r.MaximumSupportDrift<=.0035f,"stress physical contact criteria");
            lines.Add($"{fps}fps {(slow?"0.5":"1")}x: 2 trips, closed door, automatic opening/closing, no handle IK; distance={r.TotalTravel:F3}m");
        }
        r.ResetStudy();r.RequestExit();r.Advance(.6f);
        var p=r.resident.transform.position;float angle=r.DoorAngle;var state=r.Current;float time=r.Clock;
        r.paused=true;r.Advance(5);Check(r.resident.transform.position==p&&r.DoorAngle==angle&&r.Current==state&&r.Clock==time,"pause changed action");
        r.paused=false;r.RequestExit();r.RequestEnter();
        for(int i=0;i<60*240&&r.Trips<2;i++)r.Advance(1f/60);
        Check(r.Trips==2&&r.Inside&&r.DoorClosed,"latest opposite request was lost");
        lines.Add("Pause/resume, duplicate exit, opposite request deferred to safe endpoint: PASS");
        r.ResetStudy();r.paused=true;r.RequestExit();
        Check(r.Current==RabbitHomeLifeReview.Step.ApproachInside,"paused idle request was not retained");
        r.Advance(1);Check(r.Current==RabbitHomeLifeReview.Step.ApproachInside&&r.Clock==0,"paused idle request advanced");
        r.paused=false;for(int i=0;i<60*120&&!r.Outside;i++)r.Advance(1f/60);
        Check(r.Outside&&r.DoorClosed,"paused idle request did not resume");
        lines.Add("Request while paused at inside, then resume: PASS");
        // Hold the closing leaf when the resident envelope re-enters its sweep.
        r.ResetStudy();r.RequestExit();
        for(int i=0;i<240*20&&r.Door!=RabbitHomeLifeReview.DoorState.ClosingDelay;i++)r.Advance(1f/240);
        Check(r.Door==RabbitHomeLifeReview.DoorState.ClosingDelay,"missing closing delay");
        var safePosition=r.resident.transform.position;
        r.resident.transform.position=new Vector3(safePosition.x,r.groundHeight,r.hinge.position.z);
        float heldAngle=r.DoorAngle;
        typeof(RabbitHomeLifeReview).GetMethod("UpdateDoor",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(r,new object[]{1f});
        Check(r.DoorAngle==heldAngle,"automatic door closed through blocked sweep");
        r.resident.transform.position=safePosition;r.ResetStudy();
        Check(r.Inside&&r.DoorClosed&&r.Pending=="없음","reset leaves a pending door action");
        lines.Add("Occupied closing envelope holds door open; reset clears door/request state: PASS (injected geometry check, not OS input)");
        foreach(bool exit in new[]{true,false}){
            r.ResetStudy();
            if(!exit){r.RequestExit();for(int i=0;i<60*120&&!r.Outside;i++)r.Advance(1f/60);}
            r.Request(exit);for(int i=0;i<240*120&&r.Door!=RabbitHomeLifeReview.DoorState.Closing;i++)r.Advance(1f/240);
            Check(r.Current==RabbitHomeLifeReview.Step.WalkToDestination,"closing must overlap destination walking");
            p=r.resident.transform.position;angle=r.DoorAngle;time=r.Clock;
            r.paused=true;r.Advance(2);Check(r.Clock==time&&r.DoorAngle==angle&&r.resident.transform.position==p,"closing pause changed pose/door");
            r.Request(exit);r.Request(!exit);r.paused=false;
            int expected=exit?2:3;
            for(int i=0;i<60*240&&r.Trips<expected;i++)r.Advance(1f/60);
            Check(r.Trips==expected&&r.DoorClosed&&(exit?r.Inside:r.Outside),"opposite request during closing lost");
            CheckClosing(r,expected);
            r.ResetStudy();Check(r.Inside&&r.ClosingStops==0&&r.DoorClosed,"closing state reset");
        }
        lines.Add("Both moving close phases: pause/resume, repeat/opposite request and reset: PASS");
        r.ResetStudy();r.automatic=true;
        float firstAutoError=float.NaN;
        for(int i=0;i<30*3600&&r.Trips<40;i++){
            r.Advance(1f/30);
            if(r.Trips==2&&float.IsNaN(firstAutoError))firstAutoError=Vector3.Distance(r.resident.transform.position,r.insideWait);
        }
        Check(r.Trips==40&&r.Inside&&r.DoorClosed,"20 automatic round trips stalled: "+r.Trips+" "+r.Current);
        CheckClosing(r,40);
        Check(r.UnsafeDoorSamples==0&&r.MaxTurnSteps<=3&&r.MinimumSole>=-.0005f&&r.MaximumSupportGap<=.005f&&r.MaximumSupportDrift<=.0035f,"automatic physical contact criteria");
        float finalError=Vector3.Distance(r.resident.transform.position,r.insideWait);
        Check(!float.IsNaN(firstAutoError)&&finalError<.15f&&Mathf.Abs(finalError-firstAutoError)<.15f,"round trip escaped 15cm stopping area: first "+firstAutoError+", final "+finalError);
        lines.Add($"20 automatic round trips: closed at inside, first/final position errors={firstAutoError:F4}/{finalError:F4}m, drift={Mathf.Abs(finalError-firstAutoError)*1000:F2}mm (15cm bounded stop area; no coordinate-chasing steps); PASS");
        lines.Add($"40 automatic closes: legacy wait drift {r.MaximumClosingDrift:F6}m; legacy wait samples {r.PrematureDepartureSamples}; PASS");
        File.WriteAllLines("Docs/RabbitHomeLifeStress.txt",lines);
        r.ResetStudy();Debug.Log("RABBIT_HOME_STRESS_OK");
    }
    static void CheckClosing(RabbitHomeLifeReview r,int stops){
        Check(r.ClosingStops==0&&r.PrematureDepartureSamples==0&&r.UnsafeDoorSamples==0,"automatic close without waiting invariant");
    }
    // Focused v0.130 checks; known v0.129 gait quality remains a separate open issue.
    public static void AutomaticClosingVerify(){
        EditorSceneManager.OpenScene(ScenePath);
        var r=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();
        var lines=new List<string>{"v0.130 automatic door checks; direct editor calls, not OS input."};
        foreach(bool slow in new[]{false,true}){
            r.ResetStudy();r.slow=slow;
            foreach(bool exit in new[]{true,false}){
                r.Request(exit);float moved=0;bool pausedOnce=false;int samples=0;
                for(int i=0;i<240*120&&(!(exit?r.Outside:r.Inside)||!r.DoorClosed);i++){
                    var before=r.resident.transform.position;
                    r.Advance(1f/240);
                    Check(r.Current!=RabbitHomeLifeReview.Step.WaitForClosing,"resident waited for closing");
                    if(r.Door==RabbitHomeLifeReview.DoorState.Closing){
                        moved+=Vector3.Distance(before,r.resident.transform.position);samples++;
                        if(!pausedOnce){
                            var p=r.resident.transform.position;var a=r.DoorAngle;
                            r.paused=true;r.Advance(2);
                            Check(r.resident.transform.position==p&&r.DoorAngle==a,"pause changed moving close");
                            r.paused=false;pausedOnce=true;
                            var safe=p;r.resident.transform.position=new Vector3(p.x,r.groundHeight,r.hinge.position.z);
                            typeof(RabbitHomeLifeReview).GetMethod("UpdateDoor",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(r,new object[]{.1f});
                            Check(r.DoorAngle==a,"unsafe closing did not hold");
                            r.resident.transform.position=safe;
                        }
                    }
                }
                Check((exit?r.Outside:r.Inside)&&r.DoorClosed,"trip/close stalled: "+r.Current);
                Check(samples>0&&moved>.05f,"no resident movement during closing: "+moved);
                CheckClosing(r,0);
                lines.Add($"{(exit?"exit":"entry")} {(slow?.5f:1)}x: movement during closing {moved:F4}m; pause and unsafe sweep hold PASS");
            }
        }
        r.ResetStudy();r.RequestExit();
        for(int i=0;i<240*120&&r.Door!=RabbitHomeLifeReview.DoorState.Closing;i++)r.Advance(1f/240);
        r.RequestExit();r.RequestEnter();
        for(int i=0;i<240*240&&(r.Trips<2||!r.DoorClosed);i++)r.Advance(1f/240);
        Check(r.Trips==2&&r.Inside&&r.DoorClosed,"queued reverse request lost");
        r.ResetStudy();Check(r.Inside&&r.DoorClosed&&r.Pending=="없음","reset failed");
        lines.Add("Repeat/opposite request, reset: PASS");
        r.SelectLifeFlow();r.lifeFlow.StartFlow(true);
        bool handoff=false;float closedAt=-1;
        for(int i=0;i<240*120&&r.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Finished;i++){
            r.Advance(1f/240);
            if(r.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Inside&&r.lifeFlow.State!=RabbitWaterLifeFlow.Phase.Exiting){
                handoff=true;if(r.DoorClosed&&closedAt<0)closedAt=i/240f;
            }
        }
        Check(handoff&&r.DoorClosed&&r.lifeFlow.State==RabbitWaterLifeFlow.Phase.Finished,"connected life door/routine incomplete: "+r.lifeFlow.State);
        Check(r.UnsafeDoorSamples==0,"connected life unsafe close");
        lines.Add($"Exit/water/bench routine finished; automatic door closed by {closedAt:F3}s. This does not approve v0.129 posture/bench collision quality.");
        // Inject a safe closing leaf after the flow owns the resident, to exercise
        // the independent door clock even when the normal trip closes earlier.
        typeof(RabbitHomeLifeReview).GetField("<Door>k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(r,RabbitHomeLifeReview.DoorState.Closing);
        typeof(RabbitHomeLifeReview).GetField("doorClock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(r,0f);
        typeof(RabbitHomeLifeReview).GetMethod("SetDoor",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(r,new object[]{r.doorOpenAngle});
        r.paused=true;r.Advance(1);Check(Mathf.Abs(r.DoorAngle-r.doorOpenAngle)<.001f,"flow pause changed door");
        r.paused=false;r.Advance(.35f);
        Check(Mathf.Abs(r.DoorAngle-r.doorOpenAngle*.5f)<1.5f,"flow door clock missing or double advanced: "+r.DoorAngle);
        r.Advance(.4f);Check(r.DoorClosed,"flow-owned automatic closing did not finish");
        lines.Add("Flow-owned door: pause freezes;0.35s reaches half-close;0.75s closes (single simulation clock): PASS. Injected door state.");

        File.WriteAllLines("Docs/RabbitAutomaticDoorVerification.txt",lines);
        r.ResetStudy();Debug.Log(string.Join("\n",lines));Debug.Log("RABBIT_AUTOMATIC_DOOR_OK");
    }
    public static void CaptureSequence()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var r=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();Check(r,"Missing home scene");
        r.ResetStudy();r.View(155);
        foreach(bool exit in new[]{true,false}){
            string folder="Logs/RabbitHomeSequenceV0119/"+(exit?"Exit":"Entry");Directory.CreateDirectory(folder);
            string front=folder+"Front";Directory.CreateDirectory(front);
            string inside=folder+"Inside";Directory.CreateDirectory(inside);
            if(exit)r.RequestExit();else r.RequestEnter();
            for(int frame=0;frame<30*120;frame++){
                r.Advance(1f/30);
                Capture(r.reviewCamera,$"frame{frame:D4}",folder);
                r.View(180);Capture(r.reviewCamera,$"frame{frame:D4}",front);r.View(155);
                r.ViewInside();Capture(r.reviewCamera,$"frame{frame:D4}",inside);r.View(155);
                if(exit?r.Outside:r.Inside)break;
            }
            Check(exit?r.Outside:r.Inside,"sequence capture stalled");
        }
        r.ResetStudy();Debug.Log("RABBIT_HOME_SEQUENCE_OK");
    }
}
