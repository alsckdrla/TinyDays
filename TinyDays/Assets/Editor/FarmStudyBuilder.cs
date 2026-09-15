using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TinyDays.Review;

// Farm scenery is authored here as editable generated meshes. No character source is rewritten.
public static class FarmStudyBuilder
{
    public const string ScenePath="Assets/Scenes/FarmStudy.unity";
    const string Art="Assets/Art/Generated/FarmStudy";
    static Transform root;
    static Dictionary<string,Material> mats;
    static List<Bounds> buildings;
    static Vector3[] route;
    static readonly Vector3[] Stops={new Vector3(-5,0,3),new Vector3(2,0,3),new Vector3(6,0,1),new Vector3(5,0,-4),new Vector3(-3,0,-4),new Vector3(-7,0,0)};
    static void Ensure(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}

    [MenuItem("Tiny Days/Stage 2-6/Open farm review")]
    public static void OpenReview()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
    [MenuItem("Tiny Days/Stage 2-6/Rebuild and verify farm")]
    public static void Execute()
    {
        try{Build();Verify();Debug.Log("TINYDAYS_STAGE26_OK");}
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    static Material Mat(string name,string hex)
    {
        string path=Art+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        ColorUtility.TryParseHtmlString("#"+hex,out Color color);m.color=color;m.SetFloat("_Smoothness",.08f);
        EditorUtility.SetDirty(m);mats[name]=m;return m;
    }
    static GameObject Group(string name,Vector3 position,Transform parent=null)
    {
        var g=new GameObject(name);g.transform.SetParent(parent?parent:root,false);g.transform.localPosition=position;return g;
    }
    static GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 s,string mat,Transform parent=null)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent?parent:root,false);g.transform.localPosition=p;g.transform.localScale=s;
        g.GetComponent<Renderer>().sharedMaterial=mats[mat];UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static GameObject Box(string n,Vector3 p,Vector3 s,string m,Transform parent=null)=>Shape(n,PrimitiveType.Cube,p,s,m,parent);
    static GameObject MeshObject(string name,List<Vector3> vertices,List<int> triangles,string material)
    {
        string path=Art+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.name=name;
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var g=Group(name,Vector3.zero);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mats[material];return g;
    }
    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {int i=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
    static Vector3 Spline(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t)
    {return .5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);}
    static void Path()
    {
        var points=new List<Vector3>();
        for(int s=0;s<6;s++)for(int i=0;i<40;i++)points.Add(Spline(Stops[(s+5)%6],Stops[s],Stops[(s+1)%6],Stops[(s+2)%6],i/40f));
        points.Add(points[0]);route=points.ToArray();
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<route.Length-1;i++)
        {
            int count=route.Length-1;
            Vector3 side=Vector3.Cross(Vector3.up,(route[(i+1)%count]-route[(i+count-1)%count]).normalized)*.95f;
            Vector3 nextSide=Vector3.Cross(Vector3.up,(route[(i+2)%count]-route[i]).normalized)*.95f;
            Vector3 a=route[i]+Vector3.up*.018f,b=route[i+1]+Vector3.up*.018f;
            Quad(v,t,a-side,b-nextSide,b+nextSide,a+side);
        }
        MeshObject("Winding dirt path",v,t,"Path");
        // Short branches lead from the circulation path to doors and fields.
        foreach(var pair in new[]{new[]{new Vector3(-5,0,3),new Vector3(-5,0,4.4f)},new[]{new Vector3(2,0,3),new Vector3(2,0,4.4f)},new[]{new Vector3(6,0,1),new Vector3(8,0,2)},new[]{new Vector3(-7,0,0),new Vector3(-9,0,0)},new[]{new Vector3(-3,0,-4),new Vector3(-3,0,-5.7f)},new[]{new Vector3(5,0,-4),new Vector3(5,0,-5.7f)}})
        {
            var delta=pair[1]-pair[0];var g=Box("Path branch",(pair[0]+pair[1])*.5f+Vector3.up*.006f,new Vector3(1.1f,.02f,delta.magnitude),"Path");g.transform.rotation=Quaternion.LookRotation(delta);
        }
    }
    static void Building(string name,Vector3 pos,float width,float depth,string wall,string roof)
    {
        var g=Group(name,pos).transform;float h=2.55f;
        buildings.Add(new Bounds(pos+Vector3.up*1.5f,new Vector3(width,3,depth)));
        Box("Stone footing",new Vector3(0,.17f,0),new Vector3(width+.18f,.34f,depth+.18f),"Stone",g);
        Box("Limewash walls",new Vector3(0,h/2+.2f,0),new Vector3(width,h,depth),wall,g);
        float rise=1.35f,eave=width*.5f+.30f,front=-depth*.5f-.30f,back=-front,y=h+.2f;
        var v=new List<Vector3>();var t=new List<int>();
        Quad(v,t,new Vector3(-eave,y,front),new Vector3(-eave,y,back),new Vector3(0,y+rise,back),new Vector3(0,y+rise,front));
        Quad(v,t,new Vector3(0,y+rise,front),new Vector3(0,y+rise,back),new Vector3(eave,y,back),new Vector3(eave,y,front));
        // Solid gable faces, including rear, for the fixed reverse views.
        int k=v.Count;v.AddRange(new[]{new Vector3(-eave,y,front),new Vector3(eave,y,front),new Vector3(0,y+rise,front),new Vector3(-eave,y,back),new Vector3(0,y+rise,back),new Vector3(eave,y,back)});t.AddRange(new[]{k,k+2,k+1,k+3,k+5,k+4});
        var r=MeshObject(name+" roof",v,t,roof);r.transform.SetParent(g,false);r.transform.localPosition=Vector3.zero;
        // Narrow roof boards add gentle rhythm without modern roofing fixtures.
        for(int i=0;i<11;i++)
        {
            float z=Mathf.Lerp(front,back,i/10f);float len=Mathf.Sqrt(eave*eave+rise*rise);
            for(int side=-1;side<=1;side+=2){var b=Box("Roof seam",new Vector3(side*eave*.5f,y+rise*.5f+.035f,z),new Vector3(len,.055f,.035f),"Timber",g);b.transform.localRotation=Quaternion.Euler(0,0,-side*Mathf.Atan2(rise,eave)*Mathf.Rad2Deg);}
        }
        float face=-depth*.5f-.03f;
        Box("Door surround",new Vector3(0,1.02f,face),new Vector3(1.03f,1.9f,.13f),"Timber",g);
        Box("Plank door",new Vector3(0,1,face-.09f),new Vector3(.83f,1.73f,.10f),"Door",g);
        for(int i=-2;i<=2;i++)Box("Door grain",new Vector3(i*.16f,1,face-.15f),new Vector3(.016f,1.7f,.014f),"Timber",g);
        Shape("Handle",PrimitiveType.Sphere,new Vector3(.27f,1,face-.19f),Vector3.one*.08f,"Brass",g);
        Box("Door step",new Vector3(0,.12f,face-.35f),new Vector3(1.3f,.24f,.7f),"Stone",g);
        foreach(float x in new[]{-width*.31f,width*.31f})Window(g,new Vector3(x,1.72f,face-.015f),false);
        Window(g,new Vector3(width*.5f+.04f,1.7f,0),true);
        Window(g,new Vector3(-width*.5f-.04f,1.7f,0),true);
        var rear=Group("Rear window",new Vector3(0,1.7f,depth*.5f+.04f),g);rear.transform.localRotation=Quaternion.Euler(0,180,0);Window(rear.transform,Vector3.zero,false);
        foreach(float x in new[]{-width*.5f,width*.5f})foreach(float z in new[]{-depth*.5f,depth*.5f})Box("Corner beam",new Vector3(x,1.5f,z),new Vector3(.12f,2.5f,.12f),"Timber",g);
        Box("Chimney",new Vector3(-width*.27f,3.95f,.65f),new Vector3(.52f,1.3f,.55f),"Stone",g);
        Box("Chimney cap",new Vector3(-width*.27f,4.61f,.65f),new Vector3(.65f,.16f,.67f),"Cream",g);
    }
    static void Window(Transform parent,Vector3 pos,bool side)
    {
        var w=Group("Window",pos,parent).transform;if(side)w.localRotation=Quaternion.Euler(0,pos.x>0?-90:90,0);
        Box("Wood frame",Vector3.zero,new Vector3(.87f,.99f,.12f),"Cream",w);
        Box("Dark interior",new Vector3(0,0,-.08f),new Vector3(.70f,.80f,.04f),"Glass",w);
        Box("Vertical mullion",new Vector3(0,0,-.12f),new Vector3(.055f,.85f,.055f),"Cream",w);
        Box("Horizontal mullion",new Vector3(0,0,-.12f),new Vector3(.74f,.055f,.055f),"Cream",w);
        foreach(float x in new[]{-.58f,.58f})Box("Shutter",new Vector3(x,0,-.02f),new Vector3(.25f,.91f,.10f),"Sage",w);
        Box("Sill",new Vector3(0,-.53f,-.11f),new Vector3(1.02f,.1f,.35f),"Timber",w);
    }
    static void Fence(Vector3 a,Vector3 b)
    {
        int n=Mathf.CeilToInt(Vector3.Distance(a,b)/1.3f);
        for(int i=0;i<=n;i++)Box("Fence post",Vector3.Lerp(a,b,i/(float)n)+Vector3.up*.5f,new Vector3(.13f,1,.13f),"Fence");
        for(int i=0;i<n;i++)for(int row=0;row<2;row++)
        {Vector3 p=Vector3.Lerp(a,b,(i+.5f)/n)+Vector3.up*(.35f+row*.35f);var g=Box("Fence rail",p,new Vector3(.075f,.10f,Vector3.Distance(a,b)/n+.08f),"Fence");g.transform.rotation=Quaternion.LookRotation(b-a);}
    }
    static void Tree(Vector3 pos,float scale,int seed)
    {
        var g=Group("Spring tree",pos).transform;g.localScale=Vector3.one*scale;
        Shape("Trunk",PrimitiveType.Cylinder,new Vector3(0,1.25f,0),new Vector3(.27f,1.25f,.27f),"Timber",g);
        var random=new System.Random(seed);
        for(int i=0;i<5;i++)
        {float angle=i*2.4f;Vector3 p=new Vector3(Mathf.Sin(angle)*.72f,2.7f+(i%2)*.5f,Mathf.Cos(angle)*.72f);var crown=Shape("Leaf crown",PrimitiveType.Sphere,p,new Vector3(1.8f,1.9f,1.7f),i%2==0?"Leaves":"LeavesLight",g);crown.transform.localRotation=Quaternion.Euler(13,random.Next(360),7);}
    }
    static void Crate(Vector3 p,float angle=0)
    {
        var g=Group("Wooden produce crate",p).transform;g.localRotation=Quaternion.Euler(0,angle,0);
        Box("Interior",new Vector3(0,.22f,0),new Vector3(.57f,.4f,.45f),"Timber",g);
        for(int i=0;i<3;i++)foreach(int side in new[]{-1,1})Box("Crate slat",new Vector3(0,.09f+i*.145f,side*.25f),new Vector3(.67f,.1f,.065f),"Fence",g);
        foreach(int side in new[]{-1,1})Box("End board",new Vector3(side*.32f,.24f,0),new Vector3(.08f,.45f,.55f),"Door",g);
    }
    static void Barrel(Vector3 p)
    {
        var g=Group("Rain barrel",p).transform;
        Shape("Staves",PrimitiveType.Cylinder,new Vector3(0,.44f,0),new Vector3(.64f,.44f,.64f),"Door",g);
        foreach(float y in new[]{.15f,.7f})Shape("Band",PrimitiveType.Cylinder,new Vector3(0,y,0),new Vector3(.665f,.04f,.665f),"Timber",g);
        Shape("Lid",PrimitiveType.Cylinder,new Vector3(0,.89f,0),new Vector3(.61f,.025f,.61f),"Fence",g);
    }
    static void Field(float x,float z,int variant)
    {
        Box("Kitchen garden soil",new Vector3(x,.035f,z),new Vector3(5.0f,.07f,3.4f),"Soil");
        for(int row=0;row<4;row++)
        {
            Box("Raised soil row",new Vector3(x,.095f,z-1.25f+row*.8f),new Vector3(4.7f,.10f,.34f),"Furrow");
            for(int col=0;col<9;col++)
            {
                float px=x-2.05f+col*.51f,pz=z-1.25f+row*.8f;
                for(int leaf=0;leaf<3;leaf++)
                {var g=Shape("Spring crop leaf",PrimitiveType.Sphere,new Vector3(px+(leaf-1)*.09f,.25f,pz),new Vector3(.14f,.33f,.12f),variant==0?"Crop":"CropLight");g.transform.rotation=Quaternion.Euler(0,leaf*60,(leaf-1)*33);}
            }
        }
        Fence(new Vector3(x-2.7f,0,z-1.95f),new Vector3(x+2.7f,0,z-1.95f));
        Fence(new Vector3(x-2.7f,0,z-1.95f),new Vector3(x-2.7f,0,z+1.85f));
        Fence(new Vector3(x+2.7f,0,z-1.95f),new Vector3(x+2.7f,0,z+1.85f));
    }
    static void Scenery()
    {
        Shape("Meadow foundation",PrimitiveType.Cylinder,new Vector3(0,-.32f,0),new Vector3(29,.32f,26),"Earth");
        Shape("Spring meadow",PrimitiveType.Cylinder,new Vector3(0,-.04f,0),new Vector3(28.7f,.045f,25.7f),"Grass");
        Box("Backdrop",new Vector3(0,-.68f,0),new Vector3(300,.1f,300),"Backdrop");
        Path();
        Building("Home A",new Vector3(-5,0,6.5f),4.3f,4.0f,"WallHoney","RoofTerracotta");
        Building("Home B",new Vector3(2,0,6.7f),4.1f,4.1f,"WallCream","RoofOchre");
        Building("Storehouse",new Vector3(8.5f,0,4.2f),3.3f,3.7f,"WallSage","RoofDark");
        Field(-3.4f,-7,0);Field(4,-7,1);
        var rest=Group("Shaded resting place",new Vector3(-9,0,0)).transform;
        Shape("Resting clearing",PrimitiveType.Cylinder,new Vector3(0,.01f,0),new Vector3(3.3f,.02f,3.3f),"Path",rest);
        Box("Bench seat",new Vector3(-.3f,.53f,0),new Vector3(1.9f,.13f,.58f),"Door",rest);
        Box("Bench back",new Vector3(-.3f,.96f,.30f),new Vector3(1.9f,.5f,.10f),"Fence",rest);
        foreach(float x in new[]{-1,.4f})foreach(float z in new[]{-.20f,.20f})Box("Bench leg",new Vector3(x,.25f,z),new Vector3(.1f,.5f,.1f),"Timber",rest);
        Tree(new Vector3(-10.4f,0,1.6f),1.35f,7);
        Tree(new Vector3(-10,0,7),1.15f,8);Tree(new Vector3(11,0,-3),1.25f,9);
        Tree(new Vector3(-1,0,10),1.1f,10);Tree(new Vector3(7,0,10),1.2f,11);
        // A small central green leaves all of the circulation path visible.
        Tree(new Vector3(.2f,0,-.15f),.72f,12);
        Fence(new Vector3(-12,0,4),new Vector3(-12,0,9));Fence(new Vector3(-12,0,9),new Vector3(-8,0,9));
        Fence(new Vector3(11.3f,0,1),new Vector3(11.3f,0,7));
        foreach(Vector3 p in new[]{new Vector3(-7.5f,0,4.8f),new Vector3(4.5f,0,5),new Vector3(10.6f,0,2.5f)})Barrel(p);
        Crate(new Vector3(7.6f,0,1.75f),-8);Crate(new Vector3(8.35f,0,1.9f),5);Crate(new Vector3(8.1f,.48f,1.9f),-5);
        Crate(new Vector3(-6.5f,0,-5.5f),12);Crate(new Vector3(6.4f,0,-5.7f),-15);
        var random=new System.Random(26015);
        for(int i=0;i<120;i++)
        {
            float x=(float)random.NextDouble()*25-12.5f,z=(float)random.NextDouble()*21-10.5f;var p=new Vector3(x,0,z);
            if(x*x/169+z*z/121>1||route.Any(q=>Vector3.Distance(q,p)<1.4f)||buildings.Any(b=>b.SqrDistance(p)<1)|| (z<-4.5f&&x>-7&&x<7))continue;
            if(i%4==0)Shape("Meadow stone",PrimitiveType.Sphere,p+Vector3.up*.15f,new Vector3(.4f,.3f,.33f),"Stone");
            else
            {
                Shape("Grass tuft",PrimitiveType.Sphere,p+Vector3.up*.12f,new Vector3(.32f,.26f,.3f),"Crop");
                if(i%3==0)for(int j=0;j<3;j++)Shape("Spring flower",PrimitiveType.Sphere,p+new Vector3((j-1)*.10f,.25f,j%2*.09f),Vector3.one*.115f,i%2==0?"Flower":"Cream");
            }
        }
    }
    public static void Build()
    {
        Ensure(!Application.isPlaying,"Exit Play mode before regeneration.");
        Directory.CreateDirectory(Art);Directory.CreateDirectory("Docs/Captures/Stage26");AssetDatabase.Refresh();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())throw new OperationCanceledException();
            scene=File.Exists(ScenePath)?EditorSceneManager.OpenScene(ScenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
        var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="GeneratedFarmStudy");if(old)UnityEngine.Object.DestroyImmediate(old);
        if(!scene.GetRootGameObjects().Any(g=>g.name=="ManualEdits"))new GameObject("ManualEdits");
        root=new GameObject("GeneratedFarmStudy").transform;buildings=new List<Bounds>();mats=new Dictionary<string,Material>();
        string[] colors={"Grass:A4B776","Earth:AD9470","Backdrop:E4DECA","Path:CFB087","Soil:705642","Furrow:8C6B4C","Timber:665641","Door:AB8057","Fence:C5A274","Stone:AAA58D","Cream:EEDBB0","WallHoney:E2BD7C","WallCream:EAD9B7","WallSage:B4B7A0","RoofTerracotta:AF7250","RoofOchre:B89956","RoofDark:677576","Sage:779488","Glass:435E60","Brass:CFAC63","Leaves:819C59","LeavesLight:A0AD64","Crop:6C904C","CropLight:91A855","Flower:E6BB70"};
        foreach(string c in colors){var split=c.Split(':');Mat(split[0],split[1]);}
        Scenery();
        var light=Group("Spring afternoon sun",Vector3.zero).AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.35f;light.color=new Color(1,.93f,.79f);light.transform.rotation=Quaternion.Euler(48,-32,0);light.shadows=LightShadows.Soft;light.shadowBias=.015f;light.shadowNormalBias=.12f;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.68f,.75f,.79f);RenderSettings.ambientEquatorColor=new Color(.52f,.55f,.45f);RenderSettings.ambientGroundColor=new Color(.36f,.32f,.25f);RenderSettings.fog=false;
        var camera=Group("Farm review camera",Vector3.zero).AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=180;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=mats["Backdrop"].color;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;camera.allowMSAA=true;
        var director=root.gameObject.AddComponent<FarmLifeDirector>();director.route=route;director.distances=new float[route.Length];
        for(int i=1;i<route.Length;i++)director.distances[i]=director.distances[i-1]+Vector3.Distance(route[i-1],route[i]);
        director.stations=Enumerable.Range(0,6).Select(i=>director.distances[i*40]).ToArray();director.waits=new float[]{5,9,7,11,6,13};
        director.places=new[]{"집 앞","이웃집 앞","창고 앞","밭 가장자리","텃밭 가장자리","쉼터 앞"};director.residents=new FarmLifeDirector.Resident[6];
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Generated/RabbitMotion.fbx");Ensure(prefab,"Temporary rabbit missing");
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/RabbitMotion.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var idle=clips.Single(c=>c.name.EndsWith("Idle_Biped"));var walk=clips.Single(c=>c.name.EndsWith("Walk_Biped"));
        for(int i=0;i<6;i++)
        {
            var actor=Group("Temporary resident "+(i+1),Vector3.zero);var model=UnityEngine.Object.Instantiate(prefab,actor.transform);model.name="Replaceable visual";model.transform.localScale=Vector3.one*.65f;
            var visual=actor.AddComponent<FarmResidentVisual>();visual.animator=model.GetComponent<Animator>();visual.idle=idle;visual.walk=walk;
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
            director.residents[i]=new FarmLifeDirector.Resident{root=actor.transform,visual=visual,offset=director.Period*i/6};
        }
        var review=root.gameObject.AddComponent<FarmStudyReview>();review.director=director;review.reviewCamera=camera;
        director.Sample(0);review.SetView();
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    }
    public static void Verify()
    {
        var lines=new List<string>{"Stage 2-6 authored farm automatic checks; not actual Game-window or user approval."};
        var manual=GameObject.Find("ManualEdits");var probe=new GameObject("__Stage26PreservationProbe");probe.transform.SetParent(manual.transform);probe.transform.localPosition=new Vector3(13,14,15);int id=probe.GetInstanceID();
        Build();Build();Ensure(probe&&probe.GetInstanceID()==id&&probe.transform.localPosition==new Vector3(13,14,15),"Manual edits lost");UnityEngine.Object.DestroyImmediate(probe);
        lines.Add("Repeated generation preserves ManualEdits: PASS");
        var d=UnityEngine.Object.FindObjectOfType<FarmLifeDirector>();var review=UnityEngine.Object.FindObjectOfType<FarmStudyReview>();
        Ensure(d.residents.Length==6&&d.residents.Select(r=>r.visual.animator).Distinct().Count()==6,"Residents not independent");
        Ensure(!UnityEngine.Object.FindObjectsOfType<RabbitMotionReview>().Any(),"Character review controller copied into farm");
        Ensure(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,"URP missing");
        Ensure(buildings.Count==3,"Farm building count");
        float minimum=float.MaxValue,maxStep=0;int mixed=0;
        var visited=new HashSet<int>[6];for(int i=0;i<6;i++)visited[i]=new HashSet<int>();
        // One entire authored cycle plus the requested five-minute window.
        for(float time=0;time<Mathf.Max(300,(float)d.Period+1);time+=.10f)
        {
            var poses=d.residents.Select(r=>d.Resolve(time+r.offset)).ToArray();
            if(poses.Any(p=>p.moving)&&poses.Any(p=>!p.moving))mixed++;
            for(int i=0;i<6;i++)
            {
                var p=poses[i];if(!p.moving)visited[i].Add(p.station);
                Ensure(!float.IsNaN(p.position.x)&&Mathf.Abs(p.position.y)<.001f,"Invalid actor position");
                foreach(var b in buildings){var horizontal=new Bounds(new Vector3(b.center.x,0,b.center.z),new Vector3(b.size.x+.8f,10,b.size.z+.8f));Ensure(!horizontal.Contains(p.position),"Route enters building");}
                maxStep=Mathf.Max(maxStep,Vector3.Distance(p.position,d.Resolve(time+.01+rOffset(d,i)).position));
                for(int j=0;j<i;j++)minimum=Mathf.Min(minimum,Vector3.Distance(p.position,poses[j].position));
            }
        }
        Ensure(minimum>.85f,"Temporary residents overlap: "+minimum);Ensure(maxStep<.01f,"Timeline discontinuity");
        Ensure(visited.All(s=>s.Count==6)&&mixed>100,"Activities not independently staggered");
        d.Sample(73.2);var pos=d.residents.Select(r=>r.root.position).ToArray();d.Sample(73.2);Ensure(pos.Zip(d.residents,(p,r)=>Vector3.Distance(p,r.root.position)).All(v=>v<.0001f),"Same-time pose not stable");
        d.Restart();var restart=d.residents.Select(r=>r.root.position).ToArray();d.Sample(120);d.Restart();Ensure(restart.Zip(d.residents,(p,r)=>Vector3.Distance(p,r.root.position)).All(v=>v<.0001f),"Restart not deterministic");
        lines.Add($"Six distinct Generic visual instances; all six stations visited; mixed movement/rest samples {mixed}: PASS");
        lines.Add($"Full-cycle building clearance; minimum resident separation {minimum:F3}m; max 10ms step {maxStep:F5}m: PASS");
        lines.Add($"Deterministic same-time sample and restart; cycle {d.Period:F2}s: PASS");
        // Editor renders are layout evidence only; the actual skinned Game view is checked separately.
        foreach(var size in new[]{new Vector2Int(1440,900),new Vector2Int(800,600),new Vector2Int(600,900)})
        for(int view=0;view<(size.x==1440?4:1);view++)
        {
            review.view=view;var rt=new RenderTexture(size.x,size.y,24);review.reviewCamera.targetTexture=rt;review.reviewCamera.aspect=size.x/(float)size.y;review.SetView();review.reviewCamera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes($"Docs/Captures/Stage26/Layout-{size.x}x{size.y}-view{view}.png",image.EncodeToPNG());RenderTexture.active=previous;review.reviewCamera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
        review.view=0;review.reviewCamera.ResetAspect();review.SetView();d.Restart();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();
        File.WriteAllLines("Docs/Stage26Verification.txt",lines);
    }
    static double rOffset(FarmLifeDirector d,int i)=>d.residents[i].offset;
}
