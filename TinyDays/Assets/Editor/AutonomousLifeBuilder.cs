using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TinyDays.Review;
using TinyDays.Life;

public static class AutonomousLifeBuilder
{
    public const string ScenePath="Assets/Scenes/AutonomousLifeStudy.unity";
    const string Owner="GeneratedAutonomousLife";
    const string Art="Assets/Art/Generated/AutonomousLife";
    static void Check(bool good,string text){if(!good)throw new InvalidOperationException(text);}
    static GameObject Shape(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static Material Mat(string name,string color)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));ColorUtility.TryParseHtmlString(color,out var c);m.color=c;m.SetFloat("_Smoothness",.08f);AssetDatabase.CreateAsset(m,path);}return m;
    }
    [MenuItem("Tiny Days/Stage 3/Build autonomous life review")]
    public static void Build()
    {
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        var target=SceneManager.GetActiveScene();if(target.path!=ScenePath)target=File.Exists(ScenePath)?EditorSceneManager.OpenScene(ScenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        foreach(var g in target.GetRootGameObjects().Where(g=>g.name==Owner))UnityEngine.Object.DestroyImmediate(g);
        if(!target.GetRootGameObjects().Any(g=>g.name=="ManualEdits"))SceneManager.MoveGameObjectToScene(new GameObject("ManualEdits"),target);
        var source=EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath,OpenSceneMode.Additive);
        var sourceRoot=source.GetRootGameObjects().Single(g=>g.name=="GeneratedFarmStudy");
        var root=UnityEngine.Object.Instantiate(sourceRoot);root.name=Owner;SceneManager.MoveGameObjectToScene(root,target);
        EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(target);
        var director=root.GetComponent<FarmLifeDirector>();director.enabled=false;
        foreach(var r in director.residents)if(r.root)UnityEngine.Object.DestroyImmediate(r.root.gameObject);
        foreach(var r in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Spring crop leaf").ToArray())UnityEngine.Object.DestroyImmediate(r.gameObject);
        var world=root.AddComponent<AutonomousLifeWorld>();
        world.land=root.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Spring meadow").bounds;
        var excluded=new[]{"soil","furrow","path","clearing","grass","flower","leaf","meadow","foundation","backdrop","stone step"};
        world.obstacles=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.bounds.max.y>.25f&&r.bounds.min.y<1.25f&&!excluded.Any(n=>r.name.ToLowerInvariant().Contains(n)))
            .Select(r=>r.bounds).Where(b=>b.size.x<20&&b.size.z<20).ToArray();
        var nav=new AutonomousNavigation(world.land,world.obstacles);
        var definitions=new List<LifeSite>();
        Action<string,SiteKind,Vector3,int> site=(id,kind,p,home)=>definitions.Add(new LifeSite{id=id,kind=kind,position=nav.Nearby(p),homeResident=home});
        for(int i=0;i<3;i++)site("home-"+i,SiteKind.Home,new Vector3(new[]{-6.7f,0,6.5f}[i],0,4.1f),i);
        for(int i=0;i<3;i++)site("store-"+i,SiteKind.Store,new Vector3(10.5f+i*.85f,0,1.15f),-1);
        Vector3[] rest={new Vector3(-8.5f,0,-.7f),new Vector3(-9.6f,0,-1.3f),new Vector3(-8.4f,0,1)};
        for(int i=0;i<3;i++)site("rest-"+i,SiteKind.Rest,rest[i],-1);
        Vector3[] leisure={new Vector3(-1.5f,0,1),new Vector3(4.5f,0,-4.7f),new Vector3(-7.2f,0,-4.7f)};
        Vector3[] waiting={new Vector3(-3.5f,0,-2),new Vector3(4,0,-1.2f),new Vector3(8.8f,0,-2.2f)};
        for(int i=0;i<3;i++){site("leisure-"+i,SiteKind.Leisure,leisure[i],-1);site("wait-"+i,SiteKind.Wait,waiting[i],-1);}
        var plants=new List<Transform>();var fruit=new List<Transform>();
        var green=Mat("Leaf","#749451");var orange=Mat("Carrot","#E8A252");var berry=Mat("Berry","#CB6872");var wood=Mat("Basket","#B89059");
        for(int i=0;i<6;i++)
        {
            Vector3 p=new Vector3((i<3?-4.5f:2.9f)+(i%3)*1.1f,0,-7);
            site("carrot-"+i,SiteKind.Crop,p+Vector3.forward*.7f,-1);
            var cluster=new GameObject("Growing carrots "+i).transform;cluster.SetParent(root.transform,false);cluster.localPosition=p;
            for(int j=0;j<3;j++){var v=new Vector3((j-1)*.23f,.13f,0);Shape(cluster,"Carrot root",PrimitiveType.Capsule,v,new Vector3(.12f,.14f,.12f),orange);Shape(cluster,"Carrot leaves",PrimitiveType.Sphere,v+Vector3.up*.19f,new Vector3(.23f,.3f,.16f),green);}plants.Add(cluster);
        }
        for(int i=0;i<3;i++)
        {
            Vector3 p=new Vector3(-10.5f+i*1.4f,0,-4.4f);site("berries-"+i,SiteKind.Berry,p+Vector3.forward*.8f,-1);
            Shape(root.transform,"Berry bush "+i,PrimitiveType.Sphere,p+Vector3.up*.4f,new Vector3(.9f,.8f,.8f),green);
            var fruits=new GameObject("Ripe berries "+i).transform;fruits.SetParent(root.transform,false);fruits.localPosition=p;
            for(int j=0;j<5;j++)Shape(fruits,"Berry",PrimitiveType.Sphere,new Vector3(Mathf.Sin(j*2.4f)*.33f,.5f+(j%2)*.15f,Mathf.Cos(j*2.4f)*.33f),Vector3.one*.14f,berry);fruit.Add(fruits);
        }
        world.definitions=definitions.ToArray();world.plants=plants.ToArray();world.berries=fruit.ToArray();
        world.starts=definitions.Where(s=>s.kind==SiteKind.Home).Select(s=>s.position).ToArray();
        director.residents=new FarmLifeDirector.Resident[3];world.cargo=new Transform[3];
        const string modelPath="Assets/Art/Generated/AdultRabbit/AdultRabbitMotion.fbx";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);Check(prefab,"Missing canonical-derived adult rabbit");
        var clips=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        for(int i=0;i<3;i++)
        {
            var actor=new GameObject("Resident "+(i+1)).transform;actor.SetParent(root.transform,false);actor.position=world.starts[i];
            var model=UnityEngine.Object.Instantiate(prefab,actor);model.name="Current adult rabbit visual";model.transform.localScale=Vector3.one*.65f;
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;if(new[]{"BodyTorso","BodyArms","BodyLegs","BodyFeet"}.Contains(skin.name))skin.enabled=false;}
            var visual=actor.gameObject.AddComponent<FarmResidentVisual>();visual.animator=model.GetComponent<Animator>();visual.idle=clips.Single(c=>c.name.EndsWith("Adult_Idle_Biped"));visual.walk=clips.Single(c=>c.name.EndsWith("Adult_Walk_Biped"));
            director.residents[i]=new FarmLifeDirector.Resident{root=actor,visual=visual};
            world.cargo[i]=Shape(actor,"Food basket",PrimitiveType.Cube,new Vector3(0,.58f,.32f),new Vector3(.34f,.24f,.24f),wood).transform;
        }
        world.storageStock=Shape(root.transform,"Shared food stock",PrimitiveType.Cube,new Vector3(12.7f,.4f,-.2f),new Vector3(1,.8f,.7f),wood).transform;
        world.obstacles=world.obstacles.Concat(root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Berry bush")||r.name=="Shared food stock").Select(r=>r.bounds)).Concat(plants.Select(p=>new Bounds(p.position+Vector3.up*.2f,new Vector3(.7f,.4f,.45f)))).ToArray();
        var finalNavigation=new AutonomousNavigation(world.land,world.obstacles);
        foreach(var place in world.definitions)if(!finalNavigation.Clear(place.position))place.position=finalNavigation.Nearby(place.position);
        world.starts=world.definitions.Where(s=>s.kind==SiteKind.Home).Select(s=>s.position).ToArray();
        for(int i=0;i<director.residents.Length;i++)director.residents[i].root.position=world.starts[i];
        var lighting=root.GetComponent<FarmLightingStudy>();lighting.enabled=false;
        root.GetComponent<FarmStudyReview>().ResetCameraToPreset();
        EditorSceneManager.SaveScene(target,ScenePath);AssetDatabase.SaveAssets();
        Debug.Log("LIFE140_SCENE_OK obstacles="+world.obstacles.Length);
    }
    public static void Execute()
    {
        try{Build();Verify();BuildPlayer();Debug.Log("LIFE140_BUILD_OK");}
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    public static void BuildPlayer()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Logs/AutonomousLifePlayer/TinyDaysAutonomousLife.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        Check(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded,"Life player failed");Debug.Log("LIFE140_PLAYER_OK");
    }
    public static void Verify()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
        var world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();Check(world,"Missing autonomous scene");
        var rows=new List<string>{"v0.140 accelerated baseline logic checks; not real-time observation or user approval."};
        Func<LifeTuning,AutonomousSimulation> create=t=>new AutonomousSimulation(t,world.definitions,world.starts,new AutonomousNavigation(world.land,world.obstacles));
        foreach(var s in world.definitions)Check(world.definitions.Count(x=>x.id==s.id)==1,"Duplicate site ID");
        foreach(string scenario in new[]{"normal","empty food","full store"})
        {
            var t=new LifeTuning();if(scenario=="empty food")t.initialFood=0;if(scenario=="full store")t.initialFood=t.capacity;
            var sim=create(t);
            for(int i=0;i<36000;i++){sim.Tick(.05f,Mathf.Repeat(12+i*.05f*24/600,24));if(i%20==0)sim.Validate();}
            sim.Validate();rows.Add($"{scenario}: planted={sim.planted} harvested={sim.harvested} gathered={sim.gathered} consumed={sim.consumed} rests={sim.rests} home={sim.homeArrivals} food={sim.food} recovered={sim.recovered}");
            Debug.Log(rows.Last());Check(sim.meals>0&&sim.homeArrivals>=9&&sim.rests>0,"Missing daily cycle "+scenario+" "+rows.Last());
            if(scenario!="full store")Check(sim.harvested>0&&sim.gathered>0,"Missing both production sources "+scenario);
            foreach(var r in sim.residents)Check(r.walkDistance>20,"Resident permanently stuck "+r.id);
        }
        var blocked=create(new LifeTuning());
        var homes=world.definitions.Where(s=>s.kind==SiteKind.Home).ToArray();
        // Start in a clear outdoor position; block the whole home zone, then release it.
        for(int i=0;i<3;i++)blocked.residents[i].position=world.definitions.First(s=>s.id=="rest-"+i).position;
        blocked.navigation.TemporaryBlock=new Bounds(new Vector3(0,0,7),new Vector3(25,3,10));
        for(int i=0;i<400;i++)blocked.Tick(.05f,22);
        Check(blocked.homeArrivals==0,"Blocked homes should not teleport");blocked.navigation.TemporaryBlock=null;
        for(int i=0;i<3000;i++)blocked.Tick(.05f,22);
        blocked.Validate();Check(blocked.homeArrivals>=3,"Home retry failed");rows.Add("Blocked homes wait safely and recover: PASS");
        world.Initialize();var director=world.GetComponent<FarmLifeDirector>();
        director.paused=true;world.Advance(3);Check(world.Simulation.elapsed==0,"Pause advanced life");
        director.paused=false;director.Playback.SetRate(3);world.Advance(2);Check(Mathf.Abs(world.Simulation.elapsed-8)<.01f,"Rate mismatch");
        Check(Mathf.Abs(world.GetComponent<FarmLightingStudy>().Hour-(12+8*24/600f))<.01f,"Clock mismatch");
        director.paused=true;world.Restart();Check(world.Simulation.food==9&&world.Simulation.elapsed==0&&director.paused,"Restart mismatch");rows.Add("Single clock / 4x / pause / reset: PASS");
        Directory.CreateDirectory("Docs");File.WriteAllLines("Docs/AutonomousLife140Baseline.txt",rows);Debug.Log("LIFE140_VERIFY_OK\n"+string.Join("\n",rows));
    }
}
