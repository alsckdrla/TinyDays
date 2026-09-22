using System;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review
{
    // Review-only corrective: the imported coat and its bind skeleton remain untouched.
    // Solve in world space, then invert each vertex's blended skin transform.
    public sealed class AdultRabbitCoatClearance : IDisposable
    {
        readonly Transform actor;
        readonly SkinnedMeshRenderer top, bottom;
        readonly Transform[] topBones, bottomBones;
        readonly Mesh original, runtime;
        readonly Vector3[] rest, legRest, world, legs, output;
        readonly Vector3[] originalNormals;
        readonly System.Collections.Generic.List<Vector3> normals;
        readonly BoneWeight[] weights, legWeights;
        readonly Matrix4x4[] bind, legBind, matrices, legMatrices, vertexMatrices;
        readonly int[] triangles, front;
        readonly float[] required, spread, offsets;
        readonly bool[] eligible;
        public float MaxDisplacement { get; private set; }

        public AdultRabbitCoatClearance(GameObject resident)
        {
            actor=resident.transform;
            var skins=resident.GetComponentsInChildren<SkinnedMeshRenderer>();
            top=skins.Single(s=>s.name=="Top");bottom=skins.Single(s=>s.name=="Bottom");
            original=top.sharedMesh;runtime=UnityEngine.Object.Instantiate(original);
            topBones=top.bones;bottomBones=bottom.bones;
            runtime.name=original.name+" (review clearance)";runtime.MarkDynamic();
            rest=original.vertices;weights=original.boneWeights;bind=original.bindposes;
            originalNormals=original.normals;normals=new System.Collections.Generic.List<Vector3>(rest.Length);
            legRest=bottom.sharedMesh.vertices;legWeights=bottom.sharedMesh.boneWeights;legBind=bottom.sharedMesh.bindposes;
            matrices=new Matrix4x4[bind.Length];legMatrices=new Matrix4x4[legBind.Length];
            vertexMatrices=new Matrix4x4[rest.Length];world=new Vector3[rest.Length];legs=new Vector3[legRest.Length];output=new Vector3[rest.Length];
            required=new float[rest.Length];spread=new float[rest.Length];offsets=new float[rest.Length];eligible=new bool[rest.Length];
            triangles=original.triangles;
            Skin();
            var local=world.Select(actor.InverseTransformPoint).ToArray();
            front=Enumerable.Range(0,triangles.Length/3).Where(t=>{
                var c=(local[triangles[t*3]]+local[triangles[t*3+1]]+local[triangles[t*3+2]])/3;
                return c.y<.83f&&c.y>.395f&&c.z>.035f&&Mathf.Abs(c.x)<.34f;
            }).ToArray();
            for(int i=0;i<eligible.Length;i++)eligible[i]=local[i].y<.835f&&local[i].y>.38f&&local[i].z>-.015f&&Mathf.Abs(local[i].x)<.34f;
            top.sharedMesh=runtime;
        }

        static Matrix4x4 Blend(Matrix4x4[] m,BoneWeight w)
        {
            var result=new Matrix4x4();
            for(int j=0;j<16;j++)result[j]=m[w.boneIndex0][j]*w.weight0+m[w.boneIndex1][j]*w.weight1+m[w.boneIndex2][j]*w.weight2+m[w.boneIndex3][j]*w.weight3;
            return result;
        }
        void Skin()
        {
            for(int i=0;i<matrices.Length;i++)matrices[i]=topBones[i].localToWorldMatrix*bind[i];
            for(int i=0;i<legMatrices.Length;i++)legMatrices[i]=bottomBones[i].localToWorldMatrix*legBind[i];
            for(int i=0;i<rest.Length;i++){vertexMatrices[i]=Blend(matrices,weights[i]);world[i]=vertexMatrices[i].MultiplyPoint3x4(rest[i]);}
            for(int i=0;i<legs.Length;i++)legs[i]=Blend(legMatrices,legWeights[i]).MultiplyPoint3x4(legRest[i]);
        }
        public void Apply(float dt)
        {
            Skin();Array.Clear(required,0,required.Length);
            var direction=actor.forward;
            foreach(var p in legs)
            {
                float nearest=float.MaxValue;int hit=-1;
                foreach(int t in front){
                    int ia=triangles[t*3],ib=triangles[t*3+1],ic=triangles[t*3+2];
                    float d=Ray(p+direction*2,-direction,world[ia],world[ib],world[ic]);
                    if(d>=0&&d<nearest){nearest=d;hit=t;}
                }
                // Only the visible front surface: deeper lining/folds must not demand a
                // larger push when the outer coat already covers the leg.
                if(hit<0||nearest<1.997f||nearest>2.12f)continue;
                int a=triangles[hit*3],b=triangles[hit*3+1],c=triangles[hit*3+2];
                float push=nearest-2+.003f;
                required[a]=Mathf.Max(required[a],push);required[b]=Mathf.Max(required[b],push);required[c]=Mathf.Max(required[c],push);
            }
            // Spatial falloff also couples coincident seams and the nearby inner lining.
            // Never alter bone poses to hide a cloth collision.
            for(int i=0;i<rest.Length;i++)
            {
                float value=required[i];
                if(eligible[i])for(int j=0;j<rest.Length;j++)if(required[j]>0)
                    value=Mathf.Max(value,required[j]*Mathf.Clamp01(1-Vector3.Distance(world[i],world[j])/.14f));
                spread[i]=value;
            }
            MaxDisplacement=0;
            for(int i=0;i<rest.Length;i++)
            {
                offsets[i]=Mathf.Max(spread[i],Mathf.MoveTowards(offsets[i],0,Mathf.Max(0,dt)*.3f));
                output[i]=rest[i]+vertexMatrices[i].inverse.MultiplyVector(direction*offsets[i]);
                MaxDisplacement=Mathf.Max(MaxDisplacement,offsets[i]);
            }
            runtime.vertices=output;runtime.RecalculateNormals();runtime.GetNormals(normals);
            // Keep authored selective smooth normals everywhere the corrective is inactive.
            for(int i=0;i<normals.Count;i++)if(offsets[i]<1e-6f)normals[i]=originalNormals[i];
            runtime.SetNormals(normals);runtime.RecalculateBounds();
        }
        static float Ray(Vector3 o,Vector3 d,Vector3 a,Vector3 b,Vector3 c)
        {
            var e=b-a;var f=c-a;var h=Vector3.Cross(d,f);float det=Vector3.Dot(e,h);if(Mathf.Abs(det)<1e-8f)return -1;
            var s=o-a;float u=Vector3.Dot(s,h)/det;if(u<0||u>1)return -1;
            var q=Vector3.Cross(s,e);float v=Vector3.Dot(d,q)/det;if(v<0||u+v>1)return -1;
            float t=Vector3.Dot(f,q)/det;return t>=0?t:-1;
        }
        public void Dispose()
        {
            if(top&&top.sharedMesh==runtime)top.sharedMesh=original;
            if(runtime){if(Application.isPlaying)UnityEngine.Object.Destroy(runtime);else UnityEngine.Object.DestroyImmediate(runtime);}
        }
    }
}
