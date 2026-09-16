using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class InteriorOcclusionChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Execute()
    {
        try
        {
            ResidentOcclusionChecks.Execute();
            EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath);
            var review=UnityEngine.Object.FindObjectOfType<FarmStudyReview>();review.ResetCameraToPreset();
            var o=review.GetComponent<FarmCameraOcclusion>();var root=o.transform;
            var homeA=GameObject.Find("Home A").transform;
            var rs=root.GetComponentsInChildren<Renderer>();var originals=rs.Select(r=>r.sharedMaterials).ToArray();
            Action<Vector3> at=p=>o.Fade(p,Vector3.zero,.2f,false,false);
            Action restored=()=>{at(new Vector3(40,40,40));Require(rs.Select((r,i)=>r.sharedMaterials.SequenceEqual(originals[i])).All(v=>v),"Original material arrays not restored");};
            Action<string,bool> faded=(name,expected)=>Require(GameObject.Find(name).GetComponentsInChildren<Renderer>().All(r=>Mathf.Abs(r.sharedMaterial.color.a-(expected?.25f:1))<.001f),name+" wrong alpha");
            for(int cycle=0;cycle<3;cycle++)
            {
                at(new Vector3(0,-5,0));foreach(var name in new[]{"Spring meadow","Meadow foundation","Backdrop"})faded(name,true);
                at(new Vector3(0,3,0));faded("Spring meadow",false);
                at(new Vector3(50,-5,50));faded("Spring meadow",false);faded("Backdrop",false);
                at(homeA.position+Vector3.up*1.5f);faded("Home A",true);faded("Home B",false);
                at(homeA.position+Vector3.up*3.6f);faded("Home A",true);
                at(homeA.position+Vector3.left*2.12f+Vector3.up*1.6f);faded("Home A",true);
                // Leave the house, but a resident target still lies behind it.
                o.Fade(homeA.position+Vector3.back*5+Vector3.up*1.5f,homeA.position+Vector3.forward*2+Vector3.up*1.5f,.2f,false,true);faded("Home A",true);
                at(homeA.position+Vector3.back*5+Vector3.up*1.5f);faded("Home A",false);
                var crate=GameObject.Find("Wooden produce crate");at(crate.transform.TransformPoint(new Vector3(0,.24f,0)));faded(crate.name,true);restored();
                o.enabled=false;Require(rs.Select((r,i)=>r.sharedMaterials.SequenceEqual(originals[i])).All(v=>v),"Disable restoration failed");o.enabled=true;
            }
            foreach(string name in new[]{"Home B","Home C","Storehouse"}){at(GameObject.Find(name).transform.position+Vector3.up*1.5f);faded(name,true);restored();}
            var tree=root.Cast<Transform>().First(t=>t.name=="Spring tree");at(tree.TransformPoint(new Vector3(0,1.25f,0)));Require(tree.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.color.a<.26f),"Trunk group not faded");restored();
            // Actual moving resident: evaluate several animation poses, enter a surface from outside.
            var actor=review.director.residents[0];var skin=actor.root.GetComponentInChildren<SkinnedMeshRenderer>();
            var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var rest=mesh.vertices;
            Require(mesh.isReadable&&mesh.blendShapeCount==0,"Resident CPU skinning asset contract changed");
            try
            {
                foreach(double time in new[]{0.0,1.3,3.8})
                {
                    review.director.Sample(time);var triangles=mesh.triangles;bool found=false;
                    var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                    var vertices=rest.Select((v,i)=>{var w=weights[i];return matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;}).ToArray();
                    for(int k=0;k<triangles.Length&&!found;k+=3)
                    {
                        var a=vertices[triangles[k]];var b=vertices[triangles[k+1]];var c=vertices[triangles[k+2]];
                        var point=(a+b+c)/3-Vector3.Cross(b-a,c-a).normalized*.002f;
                        at(point);found=skin.sharedMaterial.color.a<.26f;
                    }
                    Require(found,"Animated resident interior not detected at "+time);
                    Require(actor.root.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.color.a<.26f),"Resident group fade incomplete");
                    o.enabled=false;Require(rs.Select((r,i)=>r.sharedMaterials.SequenceEqual(originals[i])).All(v=>v),"Resident disable restoration failed");o.enabled=true;restored();
                }
            }
            finally{restored();}
            // Isolated fixtures exclude accidental occlusion by other scenery.
            var fixture=new GameObject("Interior gaps fixture");
            try
            {
                var group=new GameObject("Split crowns");group.transform.SetParent(fixture.transform);
                foreach(float x in new[]{-2f,2f}){var s=GameObject.CreatePrimitive(PrimitiveType.Sphere);s.transform.SetParent(group.transform);s.transform.position=new Vector3(x,20,0);}
                var test=fixture.AddComponent<FarmCameraOcclusion>();test.Initialize(fixture.transform);
                test.Fade(new Vector3(0,20,0),Vector3.zero,.2f,false,false);
                Require(group.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.color.a==1),"Crown gap falsely inside");
                test.Fade(new Vector3(-2,20,0),Vector3.zero,.2f,false,false);Require(group.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.color.a<.26f),"Crown inside missed");
                test.enabled=false;
            }
            finally{UnityEngine.Object.DestroyImmediate(fixture);}
            var fence=root.Cast<Transform>().First(t=>t.name=="Fence span");
            at(fence.TransformPoint(new Vector3(0,.54f,.4f)));Require(fence.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.color.a==1),"Fence gap falsely inside");restored();
            at(homeA.position+Vector3.up*1.5f);review.ResetCameraToPreset();review.UpdateResidentOcclusion(.2f);faded("Home A",false);
            File.WriteAllText("Docs/InteriorOcclusionVerification.txt","PASS: three underground/above/outside cycles; all ground layers; empty house/roof/wall; other houses; crate cavity; trunk/crown group and empty crown/fence gaps; resident bone-deformed surface across 3 animation samples; inside OR resident occlusion; independent groups; original arrays recovery; disable/re-enable and preset cleanup. Editor direct calls, not live Game input.\n");
            Debug.Log("INTERIOR_OCCLUSION_OK");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
