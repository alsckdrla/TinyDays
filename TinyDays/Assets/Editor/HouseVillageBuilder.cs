using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TinyDays.Review;
using Model=FarmLowPolyAssets.Model;

public static class HouseVillageBuilder
{
    public const string ScenePath="Assets/Scenes/HouseVillageStudy.unity";
    const string Captures="Docs/Captures/HouseVillage";
    static Transform root;
    static Dictionary<string,Material> mats;
    static List<GameObject>[] gardens;
    static GameObject[] houses;
    static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
    [MenuItem("Tiny Days/House village/Open comparison")]
    public static void OpenReview(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
    [MenuItem("Tiny Days/House village/Rebuild and verify")]
    public static void Execute()
    {
        try
        {
            Require(!Application.isPlaying,"Exit Play mode first");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var preserved=Directory.GetFiles("Assets/Art/Generated/FarmStudy","*",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/Art/Manual","*",SearchOption.AllDirectories)).Concat(new[]{FarmStudyBuilder.ScenePath}).ToDictionary(p=>p,Hash);
            Build();var probe=new GameObject("Preservation probe");probe.transform.SetParent(GameObject.Find("ManualEdits").transform);probe.transform.localPosition=new Vector3(3,4,5);int id=probe.GetInstanceID();
            Build();Build();Require(probe&&probe.GetInstanceID()==id&&probe.transform.localPosition==new Vector3(3,4,5),"ManualEdits lost");UnityEngine.Object.DestroyImmediate(probe);
            Verify();Capture();
            foreach(var p in preserved)Require(Hash(p.Key)==p.Value,"Existing asset changed: "+p.Key);
            File.AppendAllText("Docs/HouseVillageVerification.txt","Existing farm scene, entire FarmStudy generated assets and manual assets SHA-256 unchanged; repeated generation preserves ManualEdits: PASS\n");
            var review=root.GetComponent<HouseVillageReview>();review.SetView(0);EditorSceneManager.SaveScene(root.gameObject.scene,ScenePath);AssetDatabase.SaveAssets();
            Debug.Log("HOUSE_VILLAGE_OK");
        }
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    static string Hash(string path){using(var hash=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(stream));}
    static Material Mat(string name,string hex)
    {
        string path=HouseVillageAssets.Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        ColorUtility.TryParseHtmlString("#"+hex,out Color c);m.color=c;m.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(m);return m;
    }
    public static void Build()
    {
        Directory.CreateDirectory(HouseVillageAssets.Folder);Directory.CreateDirectory(Captures);AssetDatabase.Refresh();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)scene=File.Exists(ScenePath)?EditorSceneManager.OpenScene(ScenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="GeneratedHouseVillage");if(old)UnityEngine.Object.DestroyImmediate(old);
        if(!scene.GetRootGameObjects().Any(g=>g.name=="ManualEdits"))new GameObject("ManualEdits");
        root=new GameObject("GeneratedHouseVillage").transform;mats=new Dictionary<string,Material>();
        foreach(string text in new[]{"WoodWall:BA8751","WoodTrim:89623F","Door:A67548","DarkWood:57432F","OchreWall:E1B34F","IvoryWall:EEE3C5","Cream:F2E6CA","Slate:505E6B","SlateAlt:596775","RoofBrown:645E54","RoofBrownAlt:6E6658","Straw:BE9855","StrawAlt:CCAA65","Glass:759EAE","Brass:C8AB5A","Stone:B5AD92","Leaf:648A45","LeafLight:8DA654","Terracotta:AB694B","Soil:70533B","Pink:DD8795","FlowerGold:ECCA65","Ground:A8B974","Backdrop:E8E2CE"}){var c=text.Split(':');mats[c[0]]=Mat(c[0],c[1]);}
        houses=new GameObject[3];gardens=new List<GameObject>[3];
        for(int i=0;i<3;i++)
        {
            var pos=new Vector3((i-1)*9,0,0);houses[i]=HouseVillageAssets.House(i,root,pos,mats);gardens[i]=new List<GameObject>();
            var ground=new Model();ground.Box("Ground",new Vector3(0,-.12f,-.65f),new Vector3(7.5f,.24f,8));ground.Finish("Plot","Spring meadow",root,pos,mats,100,HouseVillageAssets.Folder);
            Action<string,float,float> deco=(kind,x,z)=>gardens[i].Add(HouseVillageAssets.Decoration(kind,root,pos+new Vector3(x,0,z),mats));
            Action<string,float,float> oldProp=(kind,x,z)=>gardens[i].Add(HouseVillageAssets.Existing(kind,"Garden "+i+" "+kind,root,pos+new Vector3(x,0,z)));
            if(i==0){deco("Pot",-1.55f,-3.2f);deco("Pot",1.8f,-2.4f);deco("Shrub",-2.55f,-.9f);deco("Shrub",2.55f,.8f);oldProp("Crate",-2.55f,-2.25f);oldProp("Barrel",2.6f,-1.4f);deco("Stones",.25f,-4.05f);}
            if(i==1){deco("PinkBed",-1.7f,-2.7f);deco("GoldBed",1.3f,-2.75f);deco("Stones",-.55f,-4.1f);oldProp("Bench",-2.75f,.45f);gardens[i].Last().transform.localRotation=Quaternion.Euler(0,90,0);deco("Shrub",3.1f,-1.55f);deco("Pot",.65f,-3.4f);}
            if(i==2){deco("PinkBed",-1.55f,-2.7f);deco("GoldBed",1.45f,-2.7f);deco("Shrub",-2.6f,.25f);deco("Shrub",2.5f,.4f);deco("Fence",-2.15f,-3.75f);deco("Fence",2.05f,-3.75f);deco("Pot",2.4f,-1.6f);deco("Stones",-.35f,-4.1f);}
        }
        var backdrop=new Model();backdrop.Box("Backdrop",new Vector3(0,-.35f,0),new Vector3(150,.1f,150));backdrop.Finish("Backdrop","Backdrop",root,Vector3.zero,mats,100,HouseVillageAssets.Folder);
        var sun=new GameObject("Comparison sunlight");sun.transform.SetParent(root);sun.transform.rotation=Quaternion.Euler(48,-32,0);var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.35f;light.color=new Color(1,.93f,.79f);light.shadows=LightShadows.Soft;light.shadowBias=.015f;light.shadowNormalBias=.12f;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.68f,.75f,.79f);RenderSettings.ambientEquatorColor=new Color(.52f,.55f,.45f);RenderSettings.ambientGroundColor=new Color(.36f,.32f,.25f);RenderSettings.fog=false;
        var cam=new GameObject("House comparison camera").AddComponent<Camera>();cam.transform.SetParent(root);cam.tag="MainCamera";cam.fieldOfView=40;cam.nearClipPlane=.03f;cam.farClipPlane=200;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=mats["Backdrop"].color;cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var review=root.gameObject.AddComponent<HouseVillageReview>();review.reviewCamera=cam;review.houses=houses.Select(h=>h.transform).ToArray();review.SetView(0);
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
    }
    static void Verify()
    {
        var lines=new List<string>{"House village automatic checks / NOT live Game input, NOT minimum-PC performance measurement."};
        Require(FarmStudyReview.PanButton==0&&FarmStudyReview.RotateButton==2,"Shared camera drag binding changed");
        var o=root.GetComponent<FarmCameraOcclusion>();var all=root.GetComponentsInChildren<MeshRenderer>();var original=all.Select(r=>r.sharedMaterial).ToArray();
        for(int i=0;i<3;i++)
        {
            var rs=houses[i].GetComponentsInChildren<MeshRenderer>();Require(rs.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3)<=2000,"House budget");Require(gardens[i].Count<=10,"Garden group budget");
            lines.Add(HouseVillageAssets.Names[i]+" house: "+FarmArtReview.Metrics(rs));lines.Add("Garden: "+FarmArtReview.Metrics(gardens[i].SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>()))+"; groups="+gardens[i].Count);
            foreach(var g in gardens[i])Require(g.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)<=(g.name=="Fence"?100:300),"Decoration budget "+g.name);
            float doorX=i==0?.25f:i==1?-.55f:-.35f;
            foreach(var g in gardens[i].Where(g=>g.name!="Stones"))
            {
                var parts=g.GetComponentsInChildren<Renderer>();Bounds b=parts[0].bounds;foreach(var r in parts)b.Encapsulate(r.bounds);
                b.center-=houses[i].transform.position;
                Require(!(b.min.x<doorX+.45f&&b.max.x>doorX-.45f&&b.min.z< -HouseVillageAssets.Depth[i]/2-.1f&&b.max.z> -4.4f),"Decoration blocks entrance "+g.name);
            }
            string wall=i==0?"WoodWall":i==1?"OchreWall":"IvoryWall";
            var wallMesh=houses[i].GetComponentsInChildren<MeshFilter>().Single(f=>f.sharedMesh.name==HouseVillageAssets.Names[i]+"-"+wall).sharedMesh;
            foreach(float x in new[]{doorX,-HouseVillageAssets.Width[i]*.35f,HouseVillageAssets.Width[i]*.32f})
                Require(!SegmentHit(wallMesh,new Vector3(x,1.4f,-HouseVillageAssets.Depth[i]/2-.2f),new Vector3(x,1.4f,-HouseVillageAssets.Depth[i]/2+.18f)),"Wall blocks door/window opening");
            if(i==0)Require(!SegmentHit(wallMesh,new Vector3(0,HouseVillageAssets.Height[i]+1.01f,-1.9f),new Vector3(0,HouseVillageAssets.Height[i]+1.01f,-1.5f)),"Attic window blocked");
            foreach(float y in new[]{1f,HouseVillageAssets.Height[i]+.3f})
            {
                o.Fade(houses[i].transform.position+Vector3.up*y,Vector3.zero,.2f,false,false);Require(rs.All(r=>r.sharedMaterial.color.a<.26f),"Empty interior/roof fade");
                o.Fade(new Vector3(0,30,-10),Vector3.zero,.2f,false,false);Require(all.Select((r,j)=>r.sharedMaterial==original[j]).All(v=>v),"Interior restore");
            }
            foreach(var g in gardens[i])
            {
                var r=g.GetComponentInChildren<MeshRenderer>();var mesh=r.GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;var t=mesh.triangles;
                var p=(v[t[0]]+v[t[1]]+v[t[2]])/3-Vector3.Cross(v[t[1]]-v[t[0]],v[t[2]]-v[t[0]]).normalized*.001f;
                o.Fade(r.transform.TransformPoint(p),Vector3.zero,.2f,false,false);Require(g.GetComponentsInChildren<MeshRenderer>().All(rr=>rr.sharedMaterial.color.a<.26f),"Decoration inside "+g.name);
                o.enabled=false;Require(all.Select((rr,j)=>rr.sharedMaterial==original[j]).All(vv=>vv),"Disable cleanup");o.enabled=true;
            }
        }
        foreach(var mesh in all.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct())
        {
            Require(mesh.isReadable,"Unreadable mesh");var v=mesh.vertices;var t=mesh.triangles;
            var edges=new Dictionary<string,int>();Func<Vector3,string> key=p=>Vector3Int.RoundToInt(p*100000).ToString();
            for(int i=0;i<t.Length;i+=3)Require(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).sqrMagnitude>1e-14f,"Degenerate triangle "+mesh.name);
            for(int i=0;i<t.Length;i+=3)for(int j=0;j<3;j++){string a=key(v[t[i+j]]),b=key(v[t[i+(j+1)%3]]);string k=string.CompareOrdinal(a,b)<0?a+"|"+b:b+"|"+a;edges[k]=edges.TryGetValue(k,out int n)?n+1:1;}
            Require(edges.Values.All(n=>n%2==0),"Unpaired surface edge "+mesh.name);
        }
        foreach(string name in new[]{"Pot","Shrub","Stones","PinkBed","GoldBed","Fence"})
        {
            var groups=gardens.SelectMany(g=>g).Where(g=>g.name==name).ToArray();
            foreach(var g in groups.Skip(1))Require(g.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).SequenceEqual(groups[0].GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh)),"Repeated meshes not shared");
        }
        root.GetComponent<HouseVillageReview>().SetView(0);Require(all.Select((r,j)=>r.sharedMaterial==original[j]).All(v=>v),"Home cleanup");
        lines.Add("Shared left-drag pan / middle-drag rotation binding, budgets, nondegenerate/readable geometry, each house empty/roof interior, each garden object inside, original material recovery, disable/re-enable and Home: PASS (direct calls; UI mouse event dispatch is live-input verification)");
        lines.Add("Entrance clearance, wall door/window voids including attic, closed-part edge incidence and repeated decoration mesh sharing: PASS");
        File.WriteAllLines("Docs/HouseVillageVerification.txt",lines);
    }
    static bool SegmentHit(Mesh mesh,Vector3 origin,Vector3 end)
    {
        var v=mesh.vertices;var t=mesh.triangles;var d=end-origin;
        for(int i=0;i<t.Length;i+=3)
        {
            var a=v[t[i]];var e=v[t[i+1]]-a;var f=v[t[i+2]]-a;var p=Vector3.Cross(d,f);float det=Vector3.Dot(e,p);if(Mathf.Abs(det)<1e-8f)continue;
            var s=origin-a;float u=Vector3.Dot(s,p)/det;if(u<0||u>1)continue;var q=Vector3.Cross(s,e);float w=Vector3.Dot(d,q)/det;if(w<0||u+w>1)continue;
            float distance=Vector3.Dot(f,q)/det;if(distance>0&&distance<1)return true;
        }
        return false;
    }
    static void Render(Camera cam,string name,int width,int height)
    {
        var rt=new RenderTexture(width,height,24);var previous=RenderTexture.active;var target=cam.targetTexture;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.aspect=width/(float)height;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Captures+"/"+name+".png",image.EncodeToPNG());}
        finally{RenderTexture.active=previous;cam.targetTexture=target;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
    static void Capture()
    {
        var review=root.GetComponent<HouseVillageReview>();var cam=review.reviewCamera;cam.aspect=1.8f;review.SetView(0);Render(cam,"Overview",1800,1000);
        var renderers=root.GetComponentsInChildren<MeshRenderer>();var states=renderers.Select(r=>r.enabled).ToArray();
        try
        {
            for(int i=0;i<3;i++)
            {
                float x=houses[i].transform.position.x;
                foreach(var r in renderers)r.enabled=r.name.Contains("Backdrop")||Mathf.Abs(r.bounds.center.x-x)<4.25f;
                Vector3 target=houses[i].transform.position+new Vector3(0,1.65f,-.6f);
                var offsets=new[]{new Vector3(0,1,-14),new Vector3(14,1,0),new Vector3(0,1,14),new Vector3(-8,7,-12),new Vector3(6,-1.35f,-12),new Vector3(8,7,-12)};var names=new[]{"Front","Side","Rear","Oblique","Low","ObliqueRight"};
                for(int j=0;j<offsets.Length;j++){cam.transform.position=target+offsets[j];cam.transform.LookAt(target);Render(cam,HouseVillageAssets.Names[i]+"-"+names[j],1200,1000);}
                cam.transform.position=target+offsets[3];cam.transform.LookAt(target);Render(cam,HouseVillageAssets.Names[i]+"-Small",480,360);
            }
        }
        finally{for(int i=0;i<renderers.Length;i++)renderers[i].enabled=states[i];cam.ResetAspect();review.SetView(0);}
    }
}
