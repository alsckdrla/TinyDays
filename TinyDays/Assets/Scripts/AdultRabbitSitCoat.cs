using System;
using System.Linq;
using UnityEngine;
namespace TinyDays.Review {
// Authored hem motion only: no leg sampling or collision projection.
public sealed class AdultRabbitSitCoat : IDisposable {
    readonly SkinnedMeshRenderer top;
    readonly Mesh source,copy;
    readonly Vector3[] rest,output,normals;
    public float MaxDisplacement{get;private set;}
    public static Vector3[] World(SkinnedMeshRenderer skin){
        var mesh=skin.sharedMesh;var vertices=mesh.vertices;var bw=mesh.boneWeights;var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
        for(int i=0;i<vertices.Length;i++){var p=vertices[i];var w=bw[i];vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}return vertices;
    }
    public AdultRabbitSitCoat(GameObject root){
        top=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Top");
        source=top.sharedMesh;copy=UnityEngine.Object.Instantiate(source);copy.name=source.name+" (sitting hem sway)";top.sharedMesh=copy;
        rest=source.vertices;normals=source.normals;output=new Vector3[rest.Length];
    }
    public void Apply(int clip,double time,float duration){
        float p=Mathf.Clamp01((float)time/duration);
        // Absolute clip time keeps pause, scrubbing and frame rates deterministic.
        float envelope=(clip==5||clip==6)?Mathf.Pow(Mathf.Sin(Mathf.PI*p),2):0;
        if(clip==8||clip==9){p=(float)(time%4)/4;envelope=.15f;}
        float sway=.010f*envelope*Mathf.Sin(2*Mathf.PI*p-.35f);
        MaxDisplacement=0;
        float height=source.bounds.size.y;
        for(int i=0;i<rest.Length;i++){
            float hem=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(source.bounds.min.y,source.bounds.min.y+height*.48f,rest[i].y));
            var offset=new Vector3(sway*.25f,0,sway)*hem*height/.55f;
            output[i]=rest[i]+offset;MaxDisplacement=Mathf.Max(MaxDisplacement,offset.magnitude*.55f/height);
        }
        copy.vertices=output;
        if(envelope<1e-6f)copy.normals=normals;else copy.RecalculateNormals();
        copy.RecalculateBounds();
    }
    public void Dispose(){if(top)top.sharedMesh=source;if(Application.isPlaying)UnityEngine.Object.Destroy(copy);else UnityEngine.Object.DestroyImmediate(copy);}
}}
