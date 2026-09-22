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
    static List<Bounds> decorationBounds;
    static Vector3[] route;
    static readonly Vector3[] Stops={new Vector3(-6.7f,0,4.15f),new Vector3(-.55f,0,4.25f),new Vector3(6.15f,0,4.05f),new Vector3(8.8f,0,-1.8f),new Vector3(13,0,-.6f),new Vector3(5,0,-4),new Vector3(-3,0,-4),new Vector3(-7,0,0)};
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
        try{Build();Verify();FarmArtReview.Capture("After");Debug.Log("TINYDAYS_STAGE26_OK");}
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
        for(int s=0;s<Stops.Length;s++)for(int i=0;i<40;i++)points.Add(Spline(Stops[(s+Stops.Length-1)%Stops.Length],Stops[s],Stops[(s+1)%Stops.Length],Stops[(s+2)%Stops.Length],i/40f));
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
        foreach(var pair in new[]{new[]{Stops[0],new Vector3(-6.7f,0,5.02f)},new[]{Stops[1],new Vector3(-.55f,0,5.18f)},new[]{Stops[2],new Vector3(6.15f,0,5.03f)},new[]{Stops[4],new Vector3(11.7f,0,1.9f)},new[]{Stops[7],new Vector3(-9,0,0)},new[]{Stops[6],new Vector3(-3,0,-5.7f)},new[]{Stops[5],new Vector3(5,0,-5.7f)}})
        {
            var delta=pair[1]-pair[0];var g=Box("Path branch",(pair[0]+pair[1])*.5f+Vector3.up*.006f,new Vector3(1.1f,.02f,delta.magnitude),"Path");g.transform.rotation=Quaternion.LookRotation(delta);
        }
    }
    static void Building(string name,Vector3 pos,float width,float depth,string wall,string roof)
    {
        var g=Group(name,pos).transform;float h=2.55f;
        var interior=g.gameObject.AddComponent<FarmInteriorVolume>();interior.center=new Vector3(0,1.475f,0);interior.size=new Vector3(width,2.55f,depth);interior.roofRise=1.35f;
        buildings.Add(new Bounds(pos+Vector3.up*1.5f,new Vector3(width,3,depth)));
        Box("Stone footing",new Vector3(0,.17f,0),new Vector3(width+.18f,.34f,depth+.18f),"Stone",g);
        // The storehouse front is assembled around a real doorway so its recessed door is never hidden by a solid wall.
        float wallDepth=.16f,doorWidth=.83f,doorHeight=1.73f,frontWall=-depth*.5f;
        float sideWidth=(width-doorWidth)*.5f;
        Box("Rear wall",new Vector3(0,h/2+.2f,depth*.5f-wallDepth*.5f),new Vector3(width,h,wallDepth),wall,g);
        Box("Left wall",new Vector3(-width*.5f+wallDepth*.5f,h/2+.2f,0),new Vector3(wallDepth,h,depth-wallDepth*2),wall,g);
        Box("Right wall",new Vector3(width*.5f-wallDepth*.5f,h/2+.2f,0),new Vector3(wallDepth,h,depth-wallDepth*2),wall,g);
        Box("Front wall left",new Vector3(-(doorWidth+sideWidth)*.5f,h/2+.2f,frontWall+wallDepth*.5f),new Vector3(sideWidth,h,wallDepth),wall,g);
        Box("Front wall right",new Vector3((doorWidth+sideWidth)*.5f,h/2+.2f,frontWall+wallDepth*.5f),new Vector3(sideWidth,h,wallDepth),wall,g);
        Box("Front wall header",new Vector3(0,doorHeight+(h-doorHeight)*.5f+.2f,frontWall+wallDepth*.5f),new Vector3(doorWidth,h-doorHeight,wallDepth),wall,g);
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
        float frameFront=frontWall-.015f,doorFace=frontWall+wallDepth-.04f;
        Box("Door frame left",new Vector3(-(doorWidth*.5f+.075f),1.02f,frameFront),new Vector3(.23f,1.9f,.13f),"Timber",g);
        Box("Door frame right",new Vector3(doorWidth*.5f+.075f,1.02f,frameFront),new Vector3(.23f,1.9f,.13f),"Timber",g);
        Box("Door frame header",new Vector3(0,1.93f,frameFront),new Vector3(1.21f,.15f,.13f),"Timber",g);
        // Thin jamb liners cover the wall-thickness reveal while keeping the door's shallow recess.
        float frameInner=frameFront+.065f,doorFront=doorFace-.05f,linerDepth=doorFront-frameInner;
        foreach(int side in new[]{-1,1})Box("Door reveal liner",new Vector3(side*(doorWidth*.5f-.025f),1,frameInner+linerDepth*.5f),new Vector3(.06f,doorHeight,linerDepth),"Timber",g);
        Box("Door reveal header",new Vector3(0,doorHeight+.115f,frameInner+linerDepth*.5f),new Vector3(doorWidth,.06f,linerDepth),"Timber",g);
        Box("Recessed plank door",new Vector3(0,1,doorFace),new Vector3(doorWidth,doorHeight,.10f),"Door",g);
        for(int i=-2;i<=2;i++)Box("Door grain",new Vector3(i*.16f,1,doorFace-.056f),new Vector3(.016f,1.7f,.014f),"Timber",g);
        Shape("Recessed handle",PrimitiveType.Sphere,new Vector3(.27f,1,doorFace-.035f),Vector3.one*.08f,"Brass",g);
        Box("Door step",new Vector3(0,.12f,frontWall-.35f),new Vector3(1.3f,.24f,.7f),"Stone",g);
        foreach(float x in new[]{-1.1f,1.1f})Window(g,new Vector3(x,1.72f,frontWall-.015f),false,.65f);
        Window(g,new Vector3(width*.5f+.04f,1.7f,0),true);
        Window(g,new Vector3(-width*.5f-.04f,1.7f,0),true);
        var rear=Group("Rear window",new Vector3(0,1.7f,depth*.5f+.04f),g);rear.transform.localRotation=Quaternion.Euler(0,180,0);Window(rear.transform,Vector3.zero,false);
        foreach(float x in new[]{-width*.5f,width*.5f})foreach(float z in new[]{-depth*.5f,depth*.5f})Box("Corner beam",new Vector3(x,1.5f,z),new Vector3(.12f,2.5f,.12f),"Timber",g);
        Box("Chimney",new Vector3(-width*.27f,3.95f,.65f),new Vector3(.52f,1.3f,.55f),"Stone",g);
        Box("Chimney cap",new Vector3(-width*.27f,4.61f,.65f),new Vector3(.65f,.16f,.67f),"Cream",g);
    }
    static void Window(Transform parent,Vector3 pos,bool side,float scale=1f)
    {
        var w=Group("Window",pos,parent).transform;w.localScale=Vector3.one*scale;if(side)w.localRotation=Quaternion.Euler(0,pos.x>0?-90:90,0);
        Box("Wood frame",Vector3.zero,new Vector3(.87f,.99f,.12f),"Cream",w);
        Box("Dark interior",new Vector3(0,0,-.08f),new Vector3(.70f,.80f,.04f),"Glass",w);
        Box("Vertical mullion",new Vector3(0,0,-.12f),new Vector3(.055f,.85f,.055f),"Cream",w);
        Box("Horizontal mullion",new Vector3(0,0,-.12f),new Vector3(.74f,.055f,.055f),"Cream",w);
        foreach(float x in new[]{-.58f,.58f})Box("Shutter",new Vector3(x,0,-.02f),new Vector3(.25f,.91f,.10f),"Sage",w);
        Box("Sill",new Vector3(0,-.53f,-.11f),new Vector3(1.02f,.1f,.35f),"Timber",w);
    }
    static void Fence(Vector3 a,Vector3 b)
    {
        FarmLowPolyAssets.Fence(root,a,b,mats);
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
        FarmLowPolyAssets.Crate(root,p,angle,mats);
    }
    static void Barrel(Vector3 p)
    {
        FarmLowPolyAssets.Barrel(root,p,mats);
    }
    static void Residence(int variant,string name,Vector3 position)
    {
        var house=HouseVillageAssets.House(variant,root,position,mats);house.name=name;
        buildings.Add(new Bounds(position+Vector3.up*(HouseVillageAssets.Peak[variant]*.5f),new Vector3(HouseVillageAssets.Width[variant],HouseVillageAssets.Peak[variant],HouseVillageAssets.Depth[variant])));
    }
    static GameObject Decoration(string kind,Vector3 position,float yaw=0)
    {
        var g=HouseVillageAssets.Decoration(kind,root,position,mats);g.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var renderers=g.GetComponentsInChildren<Renderer>();
        if(renderers.Length>0)
        {
            var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            decorationBounds.Add(bounds);
        }
        return g;
    }
    static void Garden(int variant,Vector3 home)
    {
        Action<string,float,float> add=(kind,x,z)=>Decoration(kind,home+new Vector3(x,0,z));
        Action<string,float,float,float> turn=(kind,x,z,yaw)=>Decoration(kind,home+new Vector3(x,0,z),yaw);
        if(variant==0){add("Pot",-1.55f,-3.15f);add("SmallPot",2.4f,-.8f);add("Shrub",-2.55f,-.9f);add("Shrub",2.55f,.8f);add("Stones",-2.5f,-3.7f);turn("Woodpile",-2.35f,-3.15f,16);add("LaundryLine",2.45f,-3.45f);add("WildflowerCluster",-2.8f,-2.6f);}
        if(variant==1){add("PinkBed",-1.7f,-2.7f);add("GoldBed",1.3f,-2.75f);add("Stones",-.55f,-4.1f);add("Shrub",3.1f,-1.55f);add("Pot",.65f,-3.4f);add("SmallPot",2.55f,-3.65f);turn("GrassCluster",-2.65f,-3.6f,18);turn("WildflowerCluster",3.15f,-3.55f,-12);}
        if(variant==2){add("PinkBed",-1.55f,-2.7f);add("GoldBed",1.45f,-2.7f);add("Shrub",-2.6f,.25f);add("Shrub",2.5f,.4f);add("Fence",-2.15f,-3.75f);add("Fence",2.05f,-3.75f);add("Pot",2.4f,-1.6f);add("SmallPot",-2.55f,-2.55f);add("Woodpile",2.6f,-.2f);add("WildflowerCluster",2.65f,-3.0f);}
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
        Shape("Meadow foundation",PrimitiveType.Cylinder,new Vector3(0,-.32f,.55f),new Vector3(32,.32f,28),"Earth");
        Shape("Spring meadow",PrimitiveType.Cylinder,new Vector3(0,-.04f,.55f),new Vector3(31.7f,.045f,27.7f),"Grass");
        Box("Backdrop",new Vector3(0,-.68f,0),new Vector3(300,.1f,300),"Backdrop");
        Path();
        Residence(0,"Home A",new Vector3(-6.7f,0,6.7f));
        Residence(1,"Home B",new Vector3(0,0,7.1f));
        Residence(2,"Home C",new Vector3(6.5f,0,6.8f));
        Garden(0,new Vector3(-6.7f,0,6.7f));Garden(1,new Vector3(0,0,7.1f));Garden(2,new Vector3(6.5f,0,6.8f));
        Building("Storehouse",new Vector3(11.7f,0,3.7f),3.3f,3.7f,"WallSage","RoofDark");
        Field(-3.4f,-7,0);Field(4,-7,1);
        // Field-side tools sit outside the fence and leave the cultivation rows and circulation loop open.
        Decoration("WaterTub",new Vector3(-6.75f,0,-8.15f));Decoration("Workbench",new Vector3(-6.65f,0,-6.45f),90);Decoration("WildflowerCluster",new Vector3(-6.55f,0,-5.35f));
        Decoration("WaterTub",new Vector3(7.2f,0,-8.15f));Decoration("GrassCluster",new Vector3(7.05f,0,-5.35f));Decoration("SmallPot",new Vector3(6.85f,0,-6.0f));
        var rest=Group("Shaded resting place",new Vector3(-9,0,0)).transform;
        Shape("Resting clearing",PrimitiveType.Cylinder,new Vector3(0,.01f,0),new Vector3(3.3f,.02f,3.3f),"Path",rest);
        // Direct root ownership prevents the clearing fading together with the bench.
        FarmLowPolyAssets.Bench(root,new Vector3(-9.3f,0,0),mats);
        Tree(new Vector3(-10.4f,0,1.6f),1.35f,7);
        Tree(new Vector3(-11.2f,0,8.2f),1.15f,8);Tree(new Vector3(14.3f,0,-3),1.25f,9);
        Tree(new Vector3(-2.7f,0,11.5f),1.1f,10);Tree(new Vector3(8.8f,0,11.7f),1.2f,11);
        // A small central green leaves all of the circulation path visible.
        Tree(new Vector3(.2f,0,-.15f),.72f,12);
        Fence(new Vector3(-13,0,4),new Vector3(-13,0,10));Fence(new Vector3(-13,0,10),new Vector3(-8.8f,0,10));
        Fence(new Vector3(14.7f,0,1),new Vector3(14.7f,0,8));
        // A small work yard keeps stored goods, tools, and a waiting area beside the storehouse.
        Decoration("Woodpile",new Vector3(10.05f,0,5.85f),90);Decoration("Workbench",new Vector3(13.45f,0,5.15f),-90);Decoration("WaterTub",new Vector3(13.55f,0,4.2f));
        Decoration("WildflowerCluster",new Vector3(14.0f,0,6.35f));Decoration("GrassCluster",new Vector3(10.3f,0,6.35f));
        foreach(Vector3 p in new[]{new Vector3(-8.9f,0,4.9f),new Vector3(3.1f,0,5.15f),new Vector3(13.2f,0,2.2f)})Barrel(p);
        Crate(new Vector3(7.6f,0,1.75f),-8);Crate(new Vector3(8.35f,0,1.9f),5);Crate(new Vector3(8.1f,.48f,1.9f),-5);
        Crate(new Vector3(-6.5f,0,-5.5f),12);Crate(new Vector3(6.4f,0,-5.7f),-15);
        // Low decorative clusters enrich the shelter and route edges without filling the walking surface.
        Decoration("WildflowerCluster",new Vector3(-10.35f,0,-1.8f),-15);Decoration("GrassCluster",new Vector3(-8.15f,0,-1.5f),20);Decoration("Stones",new Vector3(-10.9f,0,-.65f),15);
        Decoration("WildflowerCluster",new Vector3(-1.9f,0,1.25f));Decoration("GrassCluster",new Vector3(2.1f,0,.85f),-12);Decoration("Stones",new Vector3(4.7f,0,1.1f),-8);
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
        root=new GameObject("GeneratedFarmStudy").transform;buildings=new List<Bounds>();decorationBounds=new List<Bounds>();mats=new Dictionary<string,Material>();
        string[] colors={"Grass:A4B776","Earth:AD9470","Backdrop:E4DECA","Path:CFB087","Soil:705642","Furrow:8C6B4C","Timber:665641","Door:AB8057","Fence:C5A274","Stone:AAA58D","Cream:EEDBB0","WallHoney:E2BD7C","WallCream:EAD9B7","WallSage:B4B7A0","RoofTerracotta:AF7250","RoofOchre:B89956","RoofDark:677576","Sage:779488","Glass:435E60","Brass:CFAC63","Leaves:819C59","LeavesLight:A0AD64","Crop:6C904C","CropLight:91A855","Flower:E6BB70","WoodWall:BA8751","WoodTrim:89623F","DarkWood:57432F","OchreWall:E1B34F","IvoryWall:EEE3C5","Slate:505E6B","SlateAlt:596775","RoofBrown:645E54","RoofBrownAlt:6E6658","Straw:BE9855","StrawAlt:CCAA65","Leaf:648A45","LeafLight:8DA654","Terracotta:AB694B","Pink:DD8795","FlowerGold:ECCA65","FlowerLavender:B69BC8","ClothBlue:7896A2","Water:5C8993"};
        foreach(string c in colors){var split=c.Split(':');Mat(split[0],split[1]);}
        Mat("HousePlaster","E7DFC8");Mat("HouseTrim","F2E7CE");Mat("HouseRoof","605B52");Mat("HouseRoofAlternate","676156");Mat("HouseGlass","6C9294");
        foreach(string key in new[]{"Glass","HouseGlass"}){mats[key].EnableKeyword("_EMISSION");mats[key].SetColor("_EmissionColor",new Color(.001f,.001f,.001f));mats[key].globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;EditorUtility.SetDirty(mats[key]);}
        FarmLowPolyAssets.BeginBuild();Scenery();
        var light=Group("Spring afternoon sun",Vector3.zero).AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.35f;light.color=new Color(1,.93f,.79f);light.transform.rotation=Quaternion.Euler(48,-32,0);light.shadows=LightShadows.Soft;light.shadowBias=.015f;light.shadowNormalBias=.12f;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.68f,.75f,.79f);RenderSettings.ambientEquatorColor=new Color(.52f,.55f,.45f);RenderSettings.ambientGroundColor=new Color(.36f,.32f,.25f);RenderSettings.fog=false;
        var camera=Group("Farm review camera",Vector3.zero).AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=180;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=mats["Backdrop"].color;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;camera.allowMSAA=true;
        var director=root.gameObject.AddComponent<FarmLifeDirector>();director.route=route;director.distances=new float[route.Length];
        for(int i=1;i<route.Length;i++)director.distances[i]=director.distances[i-1]+Vector3.Distance(route[i-1],route[i]);
        director.stations=Enumerable.Range(0,Stops.Length).Select(i=>director.distances[i*40]).ToArray();director.waits=new float[]{5,9,7,8,11,6,13,10};
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
        var shadowPresets=ShadowQualityPresetBuilder.EnsurePresets();var review=root.gameObject.AddComponent<FarmStudyReview>();review.director=director;review.reviewCamera=camera;
        review.shadowLow=shadowPresets.low;review.shadowBalanced=shadowPresets.balanced;review.shadowHigh=shadowPresets.high;
        var lighting=root.gameObject.AddComponent<FarmLightingStudy>();lighting.sun=light;lighting.reviewCamera=camera;
        SplitHouseWindows();
        director.Sample(0);review.ResetCameraToPreset();
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    }
    static void SplitHouseWindows()
    {
        // Model.Box emits 24 vertices per glass pane. Persist each pane independently without changing geometry.
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial==mats["Glass"]&&r.transform.parent.name.StartsWith("Home ")).ToArray())
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            Ensure(mesh.vertexCount%24==0,"House glass no longer consists of box panes");
            for(int i=0;i<mesh.vertexCount/24;i++)
            {
                string key=renderer.transform.parent.name+"-Window-"+i;
                string path=Art+"/"+key+".asset";
                var pane=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!pane){pane=new Mesh();AssetDatabase.CreateAsset(pane,path);}
                pane.Clear();pane.name=key;pane.vertices=mesh.vertices.Skip(i*24).Take(24).ToArray();pane.normals=mesh.normals.Skip(i*24).Take(24).ToArray();
                pane.triangles=mesh.triangles.Where(t=>t>=i*24&&t<(i+1)*24).Select(t=>t-i*24).ToArray();pane.RecalculateBounds();EditorUtility.SetDirty(pane);
                var part=Group(key,Vector3.zero,renderer.transform.parent);part.AddComponent<MeshFilter>().sharedMesh=pane;part.AddComponent<MeshRenderer>().sharedMaterial=mats["Glass"];
            }
            UnityEngine.Object.DestroyImmediate(renderer.gameObject);
        }
    }
    public static void Verify(bool captureLayouts=true)
    {
        var lines=new List<string>{"Stage 2-6 authored farm automatic checks; not actual Game-window or user approval."};
        var manual=GameObject.Find("ManualEdits");var probe=new GameObject("__Stage26PreservationProbe");probe.transform.SetParent(manual.transform);probe.transform.localPosition=new Vector3(13,14,15);int id=probe.GetInstanceID();
        Build();Build();Ensure(probe&&probe.GetInstanceID()==id&&probe.transform.localPosition==new Vector3(13,14,15),"Manual edits lost");UnityEngine.Object.DestroyImmediate(probe);
        lines.Add("Repeated generation preserves ManualEdits: PASS");
        var d=UnityEngine.Object.FindObjectOfType<FarmLifeDirector>();var review=UnityEngine.Object.FindObjectOfType<FarmStudyReview>();
        Ensure(d.residents.Length==6&&d.residents.Select(r=>r.visual.animator).Distinct().Count()==6,"Residents not independent");
        Ensure(!UnityEngine.Object.FindObjectsOfType<RabbitMotionReview>().Any(),"Character review controller copied into farm");
        Ensure(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,"URP missing");
        Ensure(buildings.Count==4,"Farm building count");
        Ensure(decorationBounds.Count>=35,"Farm lifestyle decoration set is incomplete");
        var functionalDecorations=root.GetComponentsInChildren<Transform>().Where(t=>new[]{"Woodpile","LaundryLine","Workbench","WaterTub"}.Contains(t.name)).ToArray();
        Ensure(functionalDecorations.Length>=7,"Farm functional decoration set is incomplete");
        // Small flowers can sit at a path edge; functional props keep a visible gap from the walking centerline.
        foreach(var decoration in functionalDecorations)
        {
            var renderers=decoration.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            Ensure(route.All(p=>bounds.SqrDistance(p)>.04f),"Lifestyle decoration blocks the resident route near "+bounds.center);
        }
        Ensure(new[]{"Woodpile","LaundryLine","Workbench","WaterTub","WildflowerCluster","GrassCluster"}.All(kind=>GameObject.Find(kind)),"Lifestyle decoration variety is incomplete");
        lines.Add($"Lifestyle decoration set ({decorationBounds.Count} groups), resident-route clearance, and house/storehouse/field/shelter coverage: PASS");
        var storehouse=GameObject.Find("Storehouse").transform;
        var recessedDoor=storehouse.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Recessed plank door");
        var recessedHandle=storehouse.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Recessed handle");
        float storeFront=storehouse.position.z-1.85f;
        var frameLeft=storehouse.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Door frame left");
        var frameRight=storehouse.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Door frame right");
        var frameHeader=storehouse.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Door frame header");
        var frontWallLeft=storehouse.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Front wall left");
        var frontWallRight=storehouse.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Front wall right");
        float frameInner=frameLeft.bounds.max.z;
        Ensure(recessedDoor.bounds.min.z>frameInner&&recessedDoor.bounds.min.z<frameInner+.03f,"Storehouse door is not shallowly recessed from frame");
        Ensure(recessedHandle.bounds.min.z>storeFront+.02f&&recessedHandle.bounds.min.z<recessedDoor.bounds.min.z,"Storehouse handle protrudes beyond wall or door");
        Ensure(storehouse.GetComponentsInChildren<Renderer>().Count(r=>r.name=="Door frame left"||r.name=="Door frame right"||r.name=="Door frame header")==3,"Storehouse doorway frame missing");
        Ensure(frameLeft.bounds.max.x>frontWallLeft.bounds.max.x+.03f&&frameRight.bounds.min.x<frontWallRight.bounds.min.x-.03f,"Storehouse frame does not overlap front wall edges");
        Ensure(frameHeader.bounds.min.x<=frameLeft.bounds.min.x&&frameHeader.bounds.max.x>=frameRight.bounds.max.x,"Storehouse frame header does not cover widened jambs");
        var liners=storehouse.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Door reveal liner"||r.name=="Door reveal header").ToArray();
        Ensure(liners.Length==3&&liners.All(l=>l.bounds.min.z>=frameInner-.001f&&l.bounds.max.z<=recessedDoor.bounds.min.z+.001f),"Storehouse wall reveal liner is outside frame or door");
        var frontWindows=storehouse.GetComponentsInChildren<Transform>().Where(t=>t.name=="Window"&&t.parent==storehouse&&t.localPosition.z<0).ToArray();
        Ensure(frontWindows.Length==2&&frontWindows.All(w=>Mathf.Abs(w.localPosition.x)-.705f*w.localScale.x>.83f*.5f+.075f),"Storehouse windows overlap doorway frame");
        lines.Add("Storehouse opening, three-piece frame with timber reveal liner, shallow recessed door/handle, and clear reduced front windows: PASS");
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
                foreach(var b in buildings){var horizontal=new Bounds(new Vector3(b.center.x,0,b.center.z),new Vector3(b.size.x+.8f,10,b.size.z+.8f));Ensure(!horizontal.Contains(p.position),"Route enters building at "+p.position+" near "+b.center);}
                maxStep=Mathf.Max(maxStep,Vector3.Distance(p.position,d.Resolve(time+.01+rOffset(d,i)).position));
                for(int j=0;j<i;j++)minimum=Mathf.Min(minimum,Vector3.Distance(p.position,poses[j].position));
            }
        }
        Ensure(minimum>.55f,"Temporary residents overlap: "+minimum);Ensure(maxStep<.01f,"Timeline discontinuity");
        Ensure(visited.All(s=>s.Count==Stops.Length)&&mixed>100,"Activities not independently staggered");
        d.Sample(73.2);var pos=d.residents.Select(r=>r.root.position).ToArray();d.Sample(73.2);Ensure(pos.Zip(d.residents,(p,r)=>Vector3.Distance(p,r.root.position)).All(v=>v<.0001f),"Same-time pose not stable");
        d.Restart();var restart=d.residents.Select(r=>r.root.position).ToArray();d.Sample(120);d.Restart();Ensure(restart.Zip(d.residents,(p,r)=>Vector3.Distance(p,r.root.position)).All(v=>v<.0001f),"Restart not deterministic");
        lines.Add($"Six distinct Generic visual instances; all six stations visited; mixed movement/rest samples {mixed}: PASS");
        lines.Add($"Full-cycle building clearance; minimum resident separation {minimum:F3}m; max 10ms step {maxStep:F5}m: PASS");
        lines.Add($"Deterministic same-time sample and restart; cycle {d.Period:F2}s: PASS");
        var obstruction=review.GetComponent<FarmCameraOcclusion>();
        Ensure(obstruction&&!review.reviewCamera.orthographic,"Perspective camera missing");
        Ensure(FarmStudyReview.PanButton==0&&FarmStudyReview.RotateButton==1&&FarmStudyReview.HeightDragButton==2,"Camera drag binding changed");
        Ensure(Mathf.Abs(FarmStudyReview.MinZoomDistance-1f)<.0001f,"Minimum zoom distance changed");
        Ensure(Mathf.Abs(FarmStudyReview.ClampZoomDistance(.1f,34f)-1f)<.0001f&&Mathf.Abs(FarmStudyReview.ClampZoomDistance(80f,34f)-51f)<.0001f,"Zoom distance clamp changed");
        Ensure(Mathf.Abs(FarmStudyReview.PanDistance(1f)-8f)<.0001f&&Mathf.Abs(FarmStudyReview.PanDistance(8f)-8f)<.0001f&&Mathf.Abs(FarmStudyReview.PanDistance(34f)-34f)<.0001f,"Close pan distance changed");
        foreach(float angle in new[]{-90f,-45f,0f,75f})
        {
            var rotation=Quaternion.Euler(angle,40,0);
            var delta=FarmStudyReview.ScreenPan(rotation,new Vector2(100,50),38,40,900);
            var local=Quaternion.Inverse(rotation)*delta;
            float units=76*Mathf.Tan(20*Mathf.Deg2Rad)/900;
            Ensure(Mathf.Abs(local.x+100*units)<.001f&&Mathf.Abs(local.y+50*units)<.001f&&Mathf.Abs(local.z)<.001f,"Screen pan direction or depth changed");
        }
        lines.Add("Left-drag screen pan / right-drag rotation / middle-drag height binding; pixel-matched planar pan direction, -90..75 degrees: PASS (not live input)");
        lines.Add("1m minimum zoom; lower clamp, 1.5x maximum zoom, overview/focus camera path retained: PASS (direct evaluation)");
        lines.Add("Close pan uses an 8m reference from 1m through 8m and keeps actual-distance speed above 8m: PASS (direct evaluation)");
        var homeRoot=GameObject.Find("Home A");var home=homeRoot.GetComponentsInChildren<MeshRenderer>();
        var originals=home.Select(r=>r.sharedMaterial).ToArray();
        obstruction.Fade(homeRoot.transform.position+Vector3.up*1.5f,homeRoot.transform.position+Vector3.back*5,.2f);
        Ensure(home.All(r=>Mathf.Abs(r.sharedMaterial.color.a-.25f)<.001f),"Whole building interior fade failed");
        obstruction.Fade(new Vector3(0,-3,0),new Vector3(0,1,0),.2f);
        Ensure(Mathf.Abs(GameObject.Find("Spring meadow").GetComponent<Renderer>().sharedMaterial.color.a-.25f)<.001f,"Ground fade failed");
        obstruction.Fade(new Vector3(0,40,0),new Vector3(0,50,0),.2f);
        Ensure(home.Select((r,i)=>r.sharedMaterial==originals[i]).All(v=>v),"Building restore failed");
        lines.Add("Building group inside fade, ground fade, original material restoration: PASS (direct evaluation)");
        var crop=UnityEngine.Object.FindObjectsOfType<MeshRenderer>().First(r=>r.name=="Spring crop leaf");
        var original=crop.sharedMaterial;Color originalColor=original.color;
        obstruction.Fade(crop.bounds.center-Vector3.forward*2,crop.bounds.center+Vector3.forward*2,.2f);
        Ensure(crop.sharedMaterial!=original&&Mathf.Abs(crop.sharedMaterial.color.a-.25f)<.001f,"Per-object transparency failed");
        Ensure(original.color==originalColor,"Shared material modified");
        obstruction.Fade(new Vector3(0,40,0),new Vector3(0,50,0),.2f);
        Ensure(crop.sharedMaterial==original,"Original material not restored");
        lines.Add("Small-object fade to 25%, shared material isolation, restore to original: PASS (direct evaluation)");
        VerifyOcclusionRecovery();
        lines.Add("Mesh gap/rounded edge, independent groups, 0.2s recovery, disable/re-enable and repeated reset: PASS (direct evaluation)");
        var meadow=GameObject.Find("Spring meadow").GetComponent<Renderer>();
        var meadowOriginal=meadow.sharedMaterial;
        var above=new Vector3(0,8,-5);var underground=new Vector3(0,-2,-2);
        // Legacy raw pivot reproduces the reported logical condition without changing the camera.
        obstruction.Fade(above,underground,.2f,false);
        Ensure(Mathf.Abs(meadow.sharedMaterial.color.a-.25f)<.001f,"Legacy underground-pivot reproduction failed");
        for(int i=0;i<3;i++)
        {
            obstruction.Fade(above,underground,.2f,true);
            Ensure(meadow.sharedMaterial==meadowOriginal,"Above-ground camera failed to restore meadow");
            var target=obstruction.SurfaceTarget(above,underground,out bool clipped);
            Ensure(clipped&&target.y>-.01f&&target.y<.02f,"Incorrect surface target");
            obstruction.Fade(new Vector3(0,-2,-2),new Vector3(0,2,-2),.2f,true);
            Ensure(Mathf.Abs(meadow.sharedMaterial.color.a-.25f)<.001f,"Below-ground fading lost");
        }
        obstruction.Fade(above,underground,.2f,true);
        Ensure(meadow.sharedMaterial==meadowOriginal,"Return above ground failed");
        var outside=new Vector3(50,-2,50);var outsideTop=new Vector3(50,8,50);
        Ensure(obstruction.SurfaceTarget(outsideTop,outside,out bool edgeClip)==outside&&!edgeClip,"Outside meadow target changed");
        obstruction.Fade(new Vector3(.2f,8,-.15f),new Vector3(.2f,-2,-.15f),.2f,true);
        var centerTree=UnityEngine.Object.FindObjectsOfType<MeshRenderer>().First(r=>r.name=="Trunk"&&Vector3.Distance(r.transform.parent.position,new Vector3(.2f,0,-.15f))<.01f);
        Ensure(Mathf.Abs(centerTree.sharedMaterial.color.a-.25f)<.001f&&meadow.sharedMaterial==meadowOriginal,"Foreground tree lost or terrain faded");
        obstruction.Fade(above,underground,.2f,false);
        Ensure(Mathf.Abs(meadow.sharedMaterial.color.a-.25f)<.001f,"Explicit target path changed");
        obstruction.Fade(new Vector3(0,40,0),new Vector3(0,50,0),.2f);
        review.ResetCameraToPreset();
        Ensure(meadow.sharedMaterial==meadowOriginal,"Home/preset material recovery failed");
        lines.Add("Legacy above-camera/underground-pivot fade REPRODUCED; same-position surface target restores original; 3 below/above cycles and outside edge: PASS (direct evaluation, not live input)");
        lines.Add("Foreground tree remains translucent over opaque ground; explicit follow-target path retained; preset reset restores original: PASS (direct evaluation)");
        FarmArtReview.VerifyAssets();
        lines.Add("Low-poly budgets, shared prototype meshes, closed geometry, actual wall openings and each prop fade/restore: PASS (direct evaluation; see Stage26LowPolyVerification.txt)");
        // Editor renders are layout evidence only; the actual skinned Game view is checked separately.
        Directory.CreateDirectory("Docs/Captures/Stage27Camera");
        foreach(var size in captureLayouts?new[]{new Vector2Int(1440,900),new Vector2Int(800,600),new Vector2Int(600,900)}:new Vector2Int[0])
        for(int view=0;view<(size.x==1440?4:1);view++)
        {
            review.view=view;var rt=new RenderTexture(size.x,size.y,24);review.reviewCamera.targetTexture=rt;review.reviewCamera.aspect=size.x/(float)size.y;review.ResetCameraToPreset();review.reviewCamera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes($"Docs/Captures/Stage27Camera/Layout-{size.x}x{size.y}-view{view}.png",image.EncodeToPNG());RenderTexture.active=previous;review.reviewCamera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
        review.view=0;review.reviewCamera.ResetAspect();review.ResetCameraToPreset();d.Restart();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();
        File.WriteAllLines("Docs/Stage26Verification.txt",lines);
    }
    static double rOffset(FarmLifeDirector d,int i)=>d.residents[i].offset;
    static void VerifyOcclusionRecovery()
    {
        var fixture=new GameObject("__CameraRecoveryTest");
        try
        {
            var tree=new GameObject("Tree group");tree.transform.SetParent(fixture.transform);
            var a=GameObject.CreatePrimitive(PrimitiveType.Sphere);a.transform.SetParent(tree.transform);a.transform.position=new Vector3(-2,0,0);
            var b=GameObject.CreatePrimitive(PrimitiveType.Sphere);b.transform.SetParent(tree.transform);b.transform.position=new Vector3(2,0,0);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ground.transform.SetParent(fixture.transform);ground.transform.position=new Vector3(0,-3,0);ground.transform.localScale=new Vector3(10,.1f,10);
            foreach(var r in fixture.GetComponentsInChildren<Renderer>())r.sharedMaterial=mats["Grass"];
            var original=mats["Grass"];var ar=a.GetComponent<Renderer>();var br=b.GetComponent<Renderer>();var gr=ground.GetComponent<Renderer>();
            var o=fixture.AddComponent<FarmCameraOcclusion>();o.Initialize(fixture.transform);
            for(int cycle=0;cycle<3;cycle++)
            {
                o.Fade(new Vector3(-2,0,-2),new Vector3(-2,0,2),.2f);
                Ensure(ar.sharedMaterial!=original&&br.sharedMaterial!=original,"Tree group not faded");
                o.Fade(new Vector3(0,0,-2),new Vector3(0,0,2),.1f);
                Ensure(ar.sharedMaterial.color.a>.25f&&ar.sharedMaterial.color.a<1,"Recovery not gradual");
                o.Fade(new Vector3(0,0,-2),new Vector3(0,0,2),.1f);
                Ensure(ar.sharedMaterial==original&&br.sharedMaterial==original,"Gap incorrectly blocks recovery");
                o.Fade(new Vector3(4,-4,4),new Vector3(4,-2,4),.2f);
                Ensure(gr.sharedMaterial==original,"Cylinder bounding corner falsely occluded");
                o.Fade(new Vector3(0,-4,0),new Vector3(0,-2,0),.2f);
                Ensure(gr.sharedMaterial!=original&&ar.sharedMaterial==original,"Independent ground fade failed");
                o.enabled=false;Ensure(gr.sharedMaterial==original,"Disable did not restore");
                o.enabled=true;o.Fade(new Vector3(-2,0,-2),new Vector3(-2,0,2),.2f);
                Ensure(ar.sharedMaterial!=original,"Re-enable did not resume");
                o.Initialize(fixture.transform);Ensure(ar.sharedMaterial==original,"Reset did not restore original");
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(fixture);}
    }
}
