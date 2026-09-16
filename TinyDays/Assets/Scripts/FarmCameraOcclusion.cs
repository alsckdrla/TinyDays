using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TinyDays.Review
{
    // Bounds are broad phase only. Double-sided mesh tests do not alter scene physics.
    [ExecuteAlways]
    public sealed class FarmCameraOcclusion : MonoBehaviour
    {
        sealed class Item
        {
            public Renderer renderer;
            public Geometry geometry;
            public Material[] originals, fades;
            public float alpha=1;
            public Transform group;
            public bool ground;
            public SkinnedMeshRenderer skin;
            public Vector3[] restVertices;
            public BoneWeight[] weights;
            public Matrix4x4[] bindPoses, boneMatrices;
            public Transform[] bones;
            public bool resident;
        }
        readonly List<Item> items=new List<Item>();
        sealed class Geometry { public List<Vector3> vertices; public int[] triangles; }
        FarmInteriorVolume[] interiors;
        readonly Dictionary<Mesh,Geometry> geometryCache=new Dictionary<Mesh,Geometry>();
        Transform source;
        bool initialized;
        void OnEnable(){Initialize(source?source:transform);}
        public void Initialize(Transform scenery)
        {
            Restore();
            source=scenery;
            interiors=scenery.GetComponentsInChildren<FarmInteriorVolume>();
            foreach(var r in scenery.GetComponentsInChildren<Renderer>())
            {
                var skin=r as SkinnedMeshRenderer;
                var filter=r.GetComponent<MeshFilter>();
                var mesh=skin?skin.sharedMesh:filter?filter.sharedMesh:null;
                if(!mesh)continue;
                Geometry geometry;
                if(skin)geometry=new Geometry{vertices=new List<Vector3>(mesh.vertexCount)};
                else if(!geometryCache.TryGetValue(mesh,out geometry))
                {
                    geometry=new Geometry{vertices=new List<Vector3>(mesh.vertices),triangles=mesh.triangles};
                    geometryCache.Add(mesh,geometry);
                }
                Transform group=r.transform;
                while(group.parent&&group.parent!=scenery)group=group.parent;
                bool ground=r.name=="Spring meadow"||r.name=="Meadow foundation"||r.name=="Backdrop";
                items.Add(new Item{renderer=r,geometry=geometry,group=group,ground=ground,skin=skin,
                    resident=r.GetComponentInParent<FarmResidentVisual>()});
            }
            initialized=true;
        }
        readonly HashSet<Transform> blockedGroups=new HashSet<Transform>();
        readonly List<float> crossings=new List<float>();
        // Signed solid angle supports overlapping closed parts in combined meshes (union,
        // unlike parity, which incorrectly rejects the overlap of two solid parts).
        bool Inside(Item item,Vector3 camera)
        {
            if(!item.renderer.bounds.Contains(camera))return false;
            UpdateSkin(item);
            Vector3 p=item.renderer.transform.InverseTransformPoint(camera);var g=item.geometry;
            double angle=0;
            for(int k=0;k<g.triangles.Length;k+=3)
            {
                Vector3 a=g.vertices[g.triangles[k]]-p,b=g.vertices[g.triangles[k+1]]-p,c=g.vertices[g.triangles[k+2]]-p;
                double la=a.magnitude,lb=b.magnitude,lc=c.magnitude;
                if(la<1e-7||lb<1e-7||lc<1e-7)return true;
                double denominator=la*lb*lc+Vector3.Dot(a,b)*lc+Vector3.Dot(b,c)*la+Vector3.Dot(c,a)*lb;
                angle+=2*System.Math.Atan2(Vector3.Dot(a,Vector3.Cross(b,c)),denominator);
            }
            return System.Math.Abs(angle)>System.Math.PI*2;
        }
        void UpdateSkin(Item item)
        {
            if(item.skin)
            {
                // Current Generic assets use four bone weights and no blend shapes. Evaluate
                // those directly: imported FBX unit transforms make batch BakeMesh unreliable.
                if(item.restVertices==null)
                {
                    var mesh=item.skin.sharedMesh;
                    item.restVertices=mesh.vertices;item.weights=mesh.boneWeights;item.bindPoses=mesh.bindposes;
                    item.bones=item.skin.bones;item.boneMatrices=new Matrix4x4[item.bones.Length];item.geometry.triangles=mesh.triangles;
                }
                var worldToLocal=item.skin.transform.worldToLocalMatrix;
                for(int j=0;j<item.bones.Length;j++)item.boneMatrices[j]=worldToLocal*item.bones[j].localToWorldMatrix*item.bindPoses[j];
                var vertices=item.geometry.vertices;vertices.Clear();
                for(int j=0;j<item.restVertices.Length;j++)
                {
                    var w=item.weights[j];var v=item.restVertices[j];var m=item.boneMatrices;
                    vertices.Add(m[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+m[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+
                        m[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+m[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3);
                }
            }
        }
        // Pick the current animated surface, respecting opaque scenery and nearer residents.
        // No physics colliders or changes to the imported character are required.
        public Transform PickResident(Ray ray,float maxDistance)
        {
            if(!initialized)Initialize(source?source:transform);
            float nearest=maxDistance;Transform result=null;
            foreach(var item in items)
            {
                if(!item.renderer||!item.renderer.enabled||!item.renderer.gameObject.activeInHierarchy)continue;
                if(!item.resident&&item.alpha<.99f)continue;
                if(!item.renderer.bounds.IntersectRay(ray,out float entry)||entry>nearest)continue;
                UpdateSkin(item);
                var matrix=item.renderer.transform.localToWorldMatrix;
                var g=item.geometry;
                for(int k=0;k<g.triangles.Length;k+=3)
                    if(Triangle(ray.origin,ray.direction,matrix.MultiplyPoint3x4(g.vertices[g.triangles[k]]),matrix.MultiplyPoint3x4(g.vertices[g.triangles[k+1]]),matrix.MultiplyPoint3x4(g.vertices[g.triangles[k+2]]),out float t)&&t<nearest)
                    {nearest=t;result=item.resident?item.renderer.transform:null;}
            }
            return result;
        }
        bool BelowMeadow(Vector3 camera)
        {
            foreach(var item in items)
            {
                if(!item.ground||!item.renderer||item.renderer.name!="Spring meadow"||!item.renderer.enabled||!item.renderer.gameObject.activeInHierarchy)continue;
                var matrix=item.renderer.transform.worldToLocalMatrix;
                var origin=matrix.MultiplyPoint3x4(camera);var direction=matrix.MultiplyVector(Vector3.up);var g=item.geometry;
                for(int k=0;k<g.triangles.Length;k+=3)
                {
                    var a=g.vertices[g.triangles[k]];var b=g.vertices[g.triangles[k+1]];var c=g.vertices[g.triangles[k+2]];
                    if(matrix.transpose.MultiplyVector(Vector3.Cross(b-a,c-a)).normalized.y<.5f)continue;
                    if(Triangle(origin,direction,a,b,c,out float distance)&&distance>1e-5f)return true;
                }
            }
            return false;
        }
        static bool Triangle(Vector3 origin,Vector3 direction,Vector3 a,Vector3 b,Vector3 c,out float t)
        {
            t=0;Vector3 edge=b-a,edge2=c-a,p=Vector3.Cross(direction,edge2);
            float det=Vector3.Dot(edge,p);if(Mathf.Abs(det)<1e-8f)return false;
            Vector3 s=origin-a;float u=Vector3.Dot(s,p)/det;if(u<0||u>1)return false;
            Vector3 q=Vector3.Cross(s,edge);float v=Vector3.Dot(direction,q)/det;if(v<0||u+v>1)return false;
            t=Vector3.Dot(edge2,q)/det;return t>=0;
        }
        bool Occludes(Item item,Vector3 camera,Vector3 pivot)
        {
            var bounds=item.renderer.bounds;Vector3 delta=pivot-camera;
            bool insideBounds=bounds.Contains(camera);
            if(!insideBounds&&(!bounds.IntersectRay(new Ray(camera,delta.normalized),out float entry)||entry>delta.magnitude))return false;
            var matrix=item.renderer.transform.worldToLocalMatrix;
            Vector3 origin=matrix.MultiplyPoint3x4(camera),direction=matrix.MultiplyVector(delta);
            var g=item.geometry;
            for(int k=0;k<g.triangles.Length;k+=3)
                if(Triangle(origin,direction,g.vertices[g.triangles[k]],g.vertices[g.triangles[k+1]],g.vertices[g.triangles[k+2]],out float t)&&t>1e-5f&&t<1-1e-5f)return true;
            if(!insideBounds)return false;
            // Both endpoints can be inside a solid. Count unique exits, not shared triangle edges.
            crossings.Clear();direction=new Vector3(.317f,.573f,.756f);
            for(int k=0;k<g.triangles.Length;k+=3)
                if(Triangle(origin,direction,g.vertices[g.triangles[k]],g.vertices[g.triangles[k+1]],g.vertices[g.triangles[k+2]],out float t)&&t>1e-5f)crossings.Add(t);
            crossings.Sort();int count=0;float last=float.NegativeInfinity;
            foreach(float t in crossings)if(t-last>1e-4f){count++;last=t;}
            return count%2==1;
        }
        public Vector3 SurfaceTarget(Vector3 camera,Vector3 pivot,out bool clipped)
        {
            if(!initialized)Initialize(source?source:transform);
            float nearest=1;clipped=false;
            // Only downward views from above a surface qualify. Views from below retain fading.
            if(camera.y<=pivot.y)return pivot;
            foreach(var i in items)
            {
                if(!i.ground||!i.renderer||!i.renderer.enabled||!i.renderer.gameObject.activeInHierarchy)continue;
                // The meadow top defines the farm's surface; foundations/backdrop are lower layers.
                if(i.renderer.name!="Spring meadow")continue;
                var matrix=i.renderer.transform.worldToLocalMatrix;
                Vector3 origin=matrix.MultiplyPoint3x4(camera),direction=matrix.MultiplyVector(pivot-camera);
                var g=i.geometry;
                for(int k=0;k<g.triangles.Length;k+=3)
                {
                    Vector3 a=g.vertices[g.triangles[k]],b=g.vertices[g.triangles[k+1]],c=g.vertices[g.triangles[k+2]];
                    Vector3 normal=matrix.transpose.MultiplyVector(Vector3.Cross(b-a,c-a)).normalized;
                    if(normal.y<.5f)continue;
                    if(Triangle(origin,direction,a,b,c,out float t)&&t>1e-5f&&t<nearest)
                    {nearest=t;clipped=true;}
                }
            }
            return clipped?Vector3.Lerp(camera,pivot,nearest):pivot;
        }
        public void Fade(Vector3 camera,Vector3 pivot,float dt,bool useSurfaceTarget=false,bool hasTarget=true)
        {
            if(!isActiveAndEnabled)return;
            if(!initialized)Initialize(source?source:transform);
            bool clipped=false;
            if(hasTarget&&useSurfaceTarget)pivot=SurfaceTarget(camera,pivot,out clipped);
            blockedGroups.Clear();
            if(hasTarget)foreach(var i in items)if(!i.resident&&!(clipped&&i.ground)&&i.renderer&&i.renderer.enabled&&i.renderer.gameObject.activeInHierarchy&&!blockedGroups.Contains(i.group)&&Occludes(i,camera,pivot))blockedGroups.Add(i.group);
            foreach(var volume in interiors)
                if(volume&&volume.isActiveAndEnabled&&volume.Contains(camera))
                {
                    var group=volume.transform;while(group.parent&&group.parent!=source)group=group.parent;
                    blockedGroups.Add(group);
                }
            bool below=BelowMeadow(camera);
            foreach(var i in items)
                if(i.renderer&&i.renderer.enabled&&i.renderer.gameObject.activeInHierarchy&&!blockedGroups.Contains(i.group)&&
                    ((below&&i.ground)||Inside(i,camera)))blockedGroups.Add(i.group);
            foreach(var i in items)
            {
                if(!i.renderer)continue;
                const float hiddenAlpha=.25f;
                i.alpha=Mathf.MoveTowards(i.alpha,blockedGroups.Contains(i.group)?hiddenAlpha:1,Mathf.Max(0,dt)*(1-hiddenAlpha)/.2f);
                if(i.alpha>=1){if(i.originals!=null)i.renderer.sharedMaterials=i.originals;continue;}
                if(i.fades==null)
                {
                    i.originals=i.renderer.sharedMaterials;i.fades=new Material[i.originals.Length];
                    for(int k=0;k<i.fades.Length;k++)
                    {
                        var m=new Material(i.originals[k]);m.hideFlags=HideFlags.HideAndDontSave;
                        m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
                        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent;
                        m.SetShaderPassEnabled("ShadowCaster",false);i.fades[k]=m;
                    }
                }
                for(int k=0;k<i.fades.Length;k++){Color c=i.originals[k].color;c.a*=i.alpha;i.fades[k].color=c;}
                i.renderer.sharedMaterials=i.fades;
            }
        }
        public void RefreshLightingColors()
        {
            foreach(var i in items)
            {
                if(i.originals==null||i.fades==null)continue;
                for(int k=0;k<i.originals.Length;k++)
                {
                    var source=i.originals[k];var fade=i.fades[k];
                    if(!source||!fade)continue;
                    if(source.HasProperty("_BaseColor")){var c=source.GetColor("_BaseColor");c.a=i.alpha;fade.SetColor("_BaseColor",c);}
                    if(source.HasProperty("_EmissionColor"))fade.SetColor("_EmissionColor",source.GetColor("_EmissionColor"));
                }
            }
        }
        void OnDisable(){Restore();}
        public void Restore()
        {
            foreach(var i in items)
            {
                if(i.renderer&&i.originals!=null)i.renderer.sharedMaterials=i.originals;
                if(i.fades!=null)foreach(var m in i.fades){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}
            }
            items.Clear();
            interiors=new FarmInteriorVolume[0];
            geometryCache.Clear();blockedGroups.Clear();initialized=false;
        }
    }
}
