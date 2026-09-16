using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

// Deterministic editor renders, NOT recordings of Game-window input.
public static class FarmArtReview
{
    public const string Folder = "Docs/Captures/Stage26LowPoly";
    public static void CaptureBefore()
    {
        try { EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath); Capture("Before"); Debug.Log("FARM_ART_BEFORE_OK"); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static string Metrics(IEnumerable<MeshRenderer> renderers)
    {
        var rs=renderers.ToArray();
        return $"triangles={rs.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3)}, renderers={rs.Length}, materials={rs.SelectMany(r=>r.sharedMaterials).Distinct().Count()}";
    }
    public static void Capture(string version)
    {
        string dir=Folder+"/"+version; Directory.CreateDirectory(dir);
        var review=UnityEngine.Object.FindObjectOfType<FarmStudyReview>(); review.ResetCameraToPreset();
        var camera=review.reviewCamera; var oldP=camera.transform.position; var oldR=camera.transform.rotation;
        float oldAspect=camera.aspect;
        var all=UnityEngine.Object.FindObjectsOfType<MeshRenderer>(); var states=all.Select(r=>r.enabled).ToArray();
        var home=GameObject.Find("Home A").transform;
        var sets=new Dictionary<string,MeshRenderer[]> {
            {"HomeA",home.GetComponentsInChildren<MeshRenderer>()},
            {"Crate",GameObject.Find("Wooden produce crate").GetComponentsInChildren<MeshRenderer>()},
            {"Barrel",GameObject.Find("Rain barrel").GetComponentsInChildren<MeshRenderer>()},
            {"Bench",all.Where(r=>r.name.StartsWith("Bench")||r.transform.parent.name=="Bench").ToArray()},
            {"FenceTotal",all.Where(r=>r.name.StartsWith("Fence")||r.transform.parent.name.StartsWith("Fence span")).ToArray()},
            {"Environment",all}
        };
        File.WriteAllLines(dir+"/Metrics.txt",sets.Select(p=>p.Key+": "+Metrics(p.Value)));
        try
        {
            foreach(var r in all)r.enabled=sets["HomeA"].Contains(r)||r.name=="Spring meadow"||r.name=="Meadow foundation"||r.name=="Backdrop";
            var aim=home.position+Vector3.up*2;
            var offsets=new[]{new Vector3(0,1,-12),new Vector3(12,1,0),new Vector3(0,1,12),new Vector3(-8,6,-10),new Vector3(6,-1.7f,-10)};
            var labels=new[]{"Front","Side","Rear","Oblique","Low"};
            for(int i=0;i<offsets.Length;i++) { camera.transform.position=aim+offsets[i];camera.transform.LookAt(aim);Render(camera,dir+"/HomeA-"+labels[i]+".png",1100,850); }
            camera.transform.position=aim+offsets[3];camera.transform.LookAt(aim);Render(camera,dir+"/HomeA-Small.png",480,360);
            foreach(string key in new[]{"Crate","Barrel","Bench"})
            {
                var rs=sets[key];foreach(var r in all)r.enabled=rs.Contains(r)||r.name=="Spring meadow"||r.name=="Backdrop";
                Bounds bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                camera.transform.position=bounds.center+new Vector3(-1.1f,.85f,-1.4f)*Mathf.Max(1,bounds.size.magnitude)*1.7f;
                camera.transform.LookAt(bounds.center);Render(camera,dir+"/"+key+".png",700,550);
            }
            for(int i=0;i<all.Length;i++)all[i].enabled=states[i];
            review.ResetCameraToPreset();Render(camera,dir+"/Farm.png",1440,900);
            if(version=="After")CapturePropSet(camera,dir);
        }
        finally { for(int i=0;i<all.Length;i++)if(all[i])all[i].enabled=states[i];camera.transform.SetPositionAndRotation(oldP,oldR);camera.aspect=oldAspect; }
    }
    static void CapturePropSet(Camera camera,string dir)
    {
        var all=UnityEngine.Object.FindObjectsOfType<Renderer>();var enabled=all.Select(r=>r.enabled).ToArray();
        var clones=new List<GameObject>();
        try
        {
            foreach(var r in all)r.enabled=r.name=="Spring meadow"||r.name=="Backdrop";
            string[] names={"Bench","Wooden produce crate","Rain barrel","Fence span"};
            Vector3[] positions={new Vector3(-2.4f,0,0),new Vector3(-.65f,0,-.1f),new Vector3(.55f,0,0),new Vector3(1.65f,0,0)};
            for(int i=0;i<names.Length;i++)
            {
                var source=i==3?GameObject.Find("GeneratedFarmStudy").transform.Cast<Transform>().First(t=>t.name=="Fence span"&&t.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)==72).gameObject:GameObject.Find(names[i]);
                var copy=UnityEngine.Object.Instantiate(source);clones.Add(copy);
                copy.transform.SetParent(null);copy.transform.position=positions[i];copy.transform.rotation=Quaternion.Euler(0,i==3?90:0,0);
                foreach(var r in copy.GetComponentsInChildren<Renderer>())r.enabled=true;
            }
            camera.transform.position=new Vector3(-3.8f,3.4f,-8.3f);camera.transform.LookAt(new Vector3(0,.48f,0));
            Render(camera,dir+"/PropSet.png",1250,650);
        }
        finally {foreach(var g in clones)UnityEngine.Object.DestroyImmediate(g);for(int i=0;i<all.Length;i++)all[i].enabled=enabled[i];}
    }
    static void Render(Camera camera,string path,int width,int height)
    {
        var rt=new RenderTexture(width,height,24);var previous=RenderTexture.active;var oldTarget=camera.targetTexture;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
        try {camera.targetTexture=rt;camera.aspect=width/(float)height;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally {RenderTexture.active=previous;camera.targetTexture=oldTarget;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    public static void VerifyAssets()
    {
        var lines=new List<string>{"Low-poly first set: automated geometry and direct occlusion calls. Not live Game-window input or minimum-PC performance testing."};
        var root=GameObject.Find("GeneratedFarmStudy").transform;
        var occlusion=root.GetComponent<FarmCameraOcclusion>();
        foreach(string name in new[]{"Home A","Home B","Home C","Wooden produce crate","Rain barrel","Bench","Fence span"})
        {
            var groups=root.Cast<Transform>().Where(t=>t.name==name).ToArray();Require(groups.Length>0,"Missing "+name);
            foreach(var group in groups)
            {
                var rs=group.GetComponentsInChildren<MeshRenderer>();int count=rs.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);
                bool house=name=="Home A"||name=="Home B"||name=="Home C";
                Require(count<=(house?2000:name=="Fence span"?100:300),name+" budget failed");
                var original=rs.Select(r=>r.sharedMaterial).ToArray();
                var mesh=rs[0].GetComponent<MeshFilter>().sharedMesh;var vs=mesh.vertices;var ts=mesh.triangles;
                var center=rs[0].transform.TransformPoint((vs[ts[0]]+vs[ts[1]]+vs[ts[2]])/3);
                var normal=rs[0].transform.TransformDirection(Vector3.Cross(vs[ts[1]]-vs[ts[0]],vs[ts[2]]-vs[ts[0]]).normalized);
                occlusion.Fade(center+normal*.12f,center-normal*.12f,.2f,false);
                Require(rs.All(r=>Mathf.Abs(r.sharedMaterial.color.a-.25f)<.001f),name+" group fade failed");
                occlusion.Fade(new Vector3(0,40,0),new Vector3(0,50,0),.2f,false);
                Require(rs.Select((r,i)=>r.sharedMaterial==original[i]).All(v=>v),name+" restore failed");
            }
            lines.Add(name+": "+Metrics(groups[0].GetComponentsInChildren<MeshRenderer>())+"; instances="+groups.Length+"; budget and fade/restore PASS");
            if(name=="Wooden produce crate"||name=="Rain barrel")
                foreach(var group in groups.Skip(1))Require(group.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).SequenceEqual(groups[0].GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh)),name+" meshes not shared");
        }
        var meshes=root.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).Distinct().Where(m=>AssetDatabase.GetAssetPath(m).Contains("/LowPoly/")).ToArray();
        foreach(var mesh in meshes)
        {
            Require(mesh.isReadable,"Occlusion needs readable mesh");var vs=mesh.vertices;var ts=mesh.triangles;
            var edges=new Dictionary<string,int>();
            Func<Vector3,string> key=v=>Vector3Int.RoundToInt(v*100000).ToString();
            for(int i=0;i<ts.Length;i+=3)
            {
                Require(Vector3.Cross(vs[ts[i+1]]-vs[ts[i]],vs[ts[i+2]]-vs[ts[i]]).sqrMagnitude>1e-14f,"Degenerate triangle "+mesh.name);
                for(int j=0;j<3;j++)
                {
                    string a=key(vs[ts[i+j]]),b=key(vs[ts[i+(j+1)%3]]);string k=string.CompareOrdinal(a,b)<0?a+"|"+b:b+"|"+a;
                    edges[k]=edges.TryGetValue(k,out int n)?n+1:1;
                }
            }
            Require(edges.Values.All(n=>n%2==0),"Open surface edge "+mesh.name);
        }
        foreach(string name in new[]{"Home A","Home B","Home C"})
        {
            var house=GameObject.Find(name);Require(house.GetComponent<FarmInteriorVolume>(),name+" interior volume missing");
            Require(house.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh.isReadable),name+" readable mesh contract changed");
        }
        lines.Add("Farm low-poly meshes readable, nondegenerate and closed (even edge incidence); three approved house meshes have readable geometry and interior volumes: PASS.");
        lines.Add("Repeated crate/barrel instances share mesh assets. Three-home placement and resident-route clearance are covered by Stage26Verification.txt.");
        File.WriteAllLines("Docs/Stage26LowPolyVerification.txt",lines);
    }
    static bool Intersects(Mesh mesh,Vector3 start,Vector3 end)
    {
        var v=mesh.vertices;var t=mesh.triangles;var direction=end-start;
        for(int i=0;i<t.Length;i+=3)
        {
            var a=v[t[i]];var e1=v[t[i+1]]-a;var e2=v[t[i+2]]-a;var p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);
            if(Mathf.Abs(det)<1e-7f)continue;var s=start-a;float u=Vector3.Dot(s,p)/det;if(u<0||u>1)continue;
            var q=Vector3.Cross(s,e1);float w=Vector3.Dot(direction,q)/det;if(w<0||u+w>1)continue;
            float distance=Vector3.Dot(e2,q)/det;if(distance>=0&&distance<=1)return true;
        }
        return false;
    }
}
