using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Reproducible source for the first art set. Meshes stay readable for camera occlusion.
// Faces are split for hard edges; only the barrel side normals are smoothed.
public static class FarmLowPolyAssets
{
    const string Folder="Assets/Art/Generated/FarmStudy/LowPoly";
    static readonly HashSet<Vector3Int> fencePosts=new HashSet<Vector3Int>();
    public static void BeginBuild(){fencePosts.Clear();}
    sealed class Surface
    {
        public readonly List<Vector3> v=new List<Vector3>(), normals=new List<Vector3>();
        public readonly List<int> t=new List<int>();
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int i=v.Count;var n=Vector3.Cross(b-a,c-a).normalized;
            v.AddRange(new[]{a,b,c,d});normals.AddRange(new[]{n,n,n,n});t.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
        }
        public void Triangle(Vector3 a,Vector3 b,Vector3 c)
        {
            int i=v.Count;var n=Vector3.Cross(b-a,c-a).normalized;
            v.AddRange(new[]{a,b,c});normals.AddRange(new[]{n,n,n});t.AddRange(new[]{i,i+1,i+2});
        }
    }
    public sealed class Model
    {
        readonly Dictionary<string,Surface> surfaces=new Dictionary<string,Surface>();
        Surface S(string material){if(!surfaces.TryGetValue(material,out var s)){s=new Surface();surfaces.Add(material,s);}return s;}
        public void Box(string material,Vector3 p,Vector3 size,Quaternion q=default(Quaternion))
        {
            if(q==default(Quaternion))q=Quaternion.identity;
            var a=new Vector3[8];for(int i=0;i<8;i++)a[i]=p+q*Vector3.Scale(new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1),size*.5f);
            var s=S(material);
            s.Quad(a[0],a[2],a[3],a[1]);s.Quad(a[4],a[5],a[7],a[6]);
            s.Quad(a[0],a[4],a[6],a[2]);s.Quad(a[1],a[3],a[7],a[5]);
            s.Quad(a[0],a[1],a[5],a[4]);s.Quad(a[2],a[6],a[7],a[3]);
        }
        public void Gable(float z)
        {
            var s=S("HousePlaster");float half=2.15f,y=2.9f,peak=4f;
            var a=new Vector3(-half,y,z-.09f);var b=new Vector3(half,y,z-.09f);var c=new Vector3(0,peak,z-.09f);
            var d=a+Vector3.forward*.18f;var e=b+Vector3.forward*.18f;var f=c+Vector3.forward*.18f;
            s.Triangle(a,c,b);s.Triangle(d,e,f);s.Quad(a,b,e,d);s.Quad(a,d,f,c);s.Quad(b,c,f,e);
        }
        // Closed 10-sided rotational sections, shared normals only on the side strip.
        public void Lathe(string mat,float[] heights,float[] radii,bool smooth)
        {
            var s=S(mat);const int n=10;
            for(int row=0;row<heights.Length-1;row++)for(int j=0;j<n;j++)
            {
                float a=j*Mathf.PI*2/n,b=(j+1)*Mathf.PI*2/n;
                Vector3 u=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),w=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                int start=s.v.Count;
                s.Quad(u*radii[row]+Vector3.up*heights[row],u*radii[row+1]+Vector3.up*heights[row+1],w*radii[row+1]+Vector3.up*heights[row+1],w*radii[row]+Vector3.up*heights[row]);
                if(smooth)
                {
                    float slope=(radii[row]-radii[row+1])/(heights[row+1]-heights[row]);
                    s.normals[start]=(u+Vector3.up*slope).normalized;s.normals[start+1]=s.normals[start];
                    s.normals[start+2]=(w+Vector3.up*slope).normalized;s.normals[start+3]=s.normals[start+2];
                }
            }
            for(int j=0;j<n;j++)
            {
                float a=j*Mathf.PI*2/n,b=(j+1)*Mathf.PI*2/n;int last=heights.Length-1;
                var u=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var w=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                s.Triangle(Vector3.up*heights[0],u*radii[0]+Vector3.up*heights[0],w*radii[0]+Vector3.up*heights[0]);
                s.Triangle(Vector3.up*heights[last],w*radii[last]+Vector3.up*heights[last],u*radii[last]+Vector3.up*heights[last]);
            }
        }
        public void Prism(string material,Vector2[] polygon,float front,float back)
        {
            var s=S(material);
            Func<int,float,Vector3> p=(i,z)=>new Vector3(polygon[i].x,polygon[i].y,z);
            for(int i=1;i<polygon.Length-1;i++){s.Triangle(p(0,front),p(i+1,front),p(i,front));s.Triangle(p(0,back),p(i,back),p(i+1,back));}
            for(int i=0;i<polygon.Length;i++){int j=(i+1)%polygon.Length;s.Quad(p(i,front),p(j,front),p(j,back),p(i,back));}
        }
        public void Blob(string material,Vector3 center,Vector3 scale,bool smooth=true)
        {
            var s=S(material);const int count=8;
            Func<int,int,Vector3> point=(ring,j)=>{float a=j*Mathf.PI*2/count,b=ring*Mathf.PI/4;return new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));};
            Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{
                int start=s.v.Count;s.Triangle(center+Vector3.Scale(a,scale),center+Vector3.Scale(b,scale),center+Vector3.Scale(c,scale));
                if(smooth){var vv=new[]{a,b,c};for(int i=0;i<3;i++)s.normals[start+i]=new Vector3(vv[i].x/scale.x,vv[i].y/scale.y,vv[i].z/scale.z).normalized;}
            };
            for(int j=0;j<count;j++)
            {
                tri(Vector3.up,point(1,j+1),point(1,j));tri(Vector3.down,point(3,j),point(3,j+1));
                for(int r=1;r<3;r++){tri(point(r,j),point(r,j+1),point(r+1,j+1));tri(point(r,j),point(r+1,j+1),point(r+1,j));}
            }
        }
        public void Octa(string material,Vector3 center,Vector3 scale)
        {
            var s=S(material);var ring=new[]{Vector3.right,Vector3.forward,Vector3.left,Vector3.back};
            for(int j=0;j<4;j++)
            {
                var a=center+Vector3.Scale(ring[j],scale);var b=center+Vector3.Scale(ring[(j+1)%4],scale);
                s.Triangle(center+Vector3.up*scale.y,b,a);s.Triangle(center-Vector3.up*scale.y,a,b);
            }
        }
        public GameObject Finish(string assetKey,string name,Transform parent,Vector3 position,Dictionary<string,Material> materials,int budget,string outputFolder=Folder)
        {
            int count=surfaces.Values.Sum(s=>s.t.Count/3);
            if(count>budget)throw new InvalidOperationException(assetKey+" triangle budget exceeded: "+count);
            System.IO.Directory.CreateDirectory(outputFolder);AssetDatabase.Refresh();
            var group=new GameObject(name);group.transform.SetParent(parent,false);group.transform.localPosition=position;
            foreach(var pair in surfaces)
            {
                string path=outputFolder+"/"+assetKey+"-"+pair.Key+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
                var s=pair.Value;mesh.Clear();mesh.name=assetKey+"-"+pair.Key;mesh.SetVertices(s.v);mesh.SetNormals(s.normals);mesh.SetTriangles(s.t,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                var part=new GameObject(name+" "+pair.Key);part.transform.SetParent(group.transform,false);
                part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=materials[pair.Key];
            }
            return group;
        }
    }
    struct Opening
    {
        public float x,y,w,h;public bool door;
        public Opening(float x,float y,float w,float h,bool door=false){this.x=x;this.y=y;this.w=w;this.h=h;this.door=door;}
    }
    static void Wall(Model m,float width,Vector3 origin,float yaw,params Opening[] holes)
    {
        var q=Quaternion.Euler(0,yaw,0);
        Action<string,Vector3,Vector3> box=(mat,p,s)=>m.Box(mat,origin+q*p,s,q);
        // Vertical strips fill only solid regions; openings remain genuinely empty behind frames.
        var xs=new List<float>{-width/2,width/2};foreach(var h in holes){xs.Add(h.x-h.w/2);xs.Add(h.x+h.w/2);}xs.Sort();
        for(int i=0;i<xs.Count-1;i++)
        {
            float x=(xs[i]+xs[i+1])*.5f,low=.24f;
            foreach(var h in holes.Where(h=>x>h.x-h.w/2&&x<h.x+h.w/2).OrderBy(h=>h.y))
            {
                float bottom=h.y-h.h/2;if(bottom-low>.0001f)box("HousePlaster",new Vector3(x,(low+bottom)/2,.09f),new Vector3(xs[i+1]-xs[i],bottom-low,.18f));
                low=h.y+h.h/2;
            }
            if(low<2.9f)box("HousePlaster",new Vector3(x,(low+2.9f)/2,.09f),new Vector3(xs[i+1]-xs[i],2.9f-low,.18f));
        }
        foreach(var h in holes)
        {
            string frame=h.door?"Timber":"HouseTrim";float t=.10f;
            foreach(int side in new[]{-1,1})box(frame,new Vector3(h.x+side*(h.w-t)/2,h.y,-.018f),new Vector3(t,h.h,.20f));
            foreach(int side in new[]{-1,1})box(frame,new Vector3(h.x,h.y+side*(h.h-t)/2,-.018f),new Vector3(h.w-2*t,t,.20f));
            // Opaque glass/door sits deeper than the frame and doesn't overlap a solid wall.
            box(h.door?"Door":"HouseGlass",new Vector3(h.x,h.y,.075f),new Vector3(h.w-2*t,h.h-2*t,.045f));
            if(h.door)
            {
                for(int j=1;j<4;j++)box("Timber",new Vector3(h.x-h.w/2+t+(h.w-2*t)*j/4,h.y,.046f),new Vector3(.012f,h.h-2*t,.008f));
                box("Brass",new Vector3(h.x+h.w*.27f,h.y,-.005f),new Vector3(.055f,.12f,.07f));
                box("Stone",new Vector3(h.x,.16f,-.30f),new Vector3(1.38f,.32f,.78f));
            }
            else
            {
                box("HouseTrim",new Vector3(h.x,h.y,-.025f),new Vector3(.055f,h.h-2*t,.065f));
                box("HouseTrim",new Vector3(h.x,h.y+.09f,-.025f),new Vector3(h.w-2*t,.055f,.065f));
                box("Stone",new Vector3(h.x,h.y-h.h/2-.045f,-.07f),new Vector3(h.w+.16f,.09f,.35f));
            }
        }
    }
    public static GameObject House(Transform parent,Vector3 p,Dictionary<string,Material> mats)
    {
        var m=new Model();
        m.Box("Stone",new Vector3(0,.12f,0),new Vector3(4.48f,.24f,4.18f));
        Wall(m,4.3f,new Vector3(0,0,-2),0,new Opening(-1.43f,1.76f,.84f,1.04f),new Opening(-.25f,1.245f,.98f,2.01f,true),new Opening(1.23f,1.76f,1.12f,1.18f));
        Wall(m,4.3f,new Vector3(0,0,2),180,new Opening(.4f,1.76f,1.1f,1.08f));
        Wall(m,3.64f,new Vector3(-2.15f,0,0),90,new Opening(0,1.76f,1.15f,1.1f));
        Wall(m,3.64f,new Vector3(2.15f,0,0),-90,new Opening(.2f,1.76f,.92f,1.1f));
        m.Gable(-1.91f);m.Gable(1.91f);
        float eave=2.45f,rise=1.25f,angle=Mathf.Atan2(rise,eave)*Mathf.Rad2Deg,len=Mathf.Sqrt(eave*eave+rise*rise);
        for(int side=-1;side<=1;side+=2)
        {
            var q=Quaternion.Euler(0,0,-side*angle);
            for(int i=0;i<5;i++)m.Box(i%2==0?"HouseRoof":"HouseRoofAlternate",new Vector3(side*eave/2,3.375f,-1.88f+i*.94f),new Vector3(len,.14f,.94f),q);
            foreach(float z in new[]{-2.40f,2.40f})m.Box("HouseTrim",new Vector3(side*eave/2,3.32f,z),new Vector3(len+.08f,.16f,.10f),q);
            m.Box("HouseTrim",new Vector3(side*2.45f,2.69f,0),new Vector3(.12f,.17f,4.9f));
        }
        m.Box("HouseRoof",new Vector3(0,4.055f,0),new Vector3(.18f,.14f,4.88f));
        // Lean-to canopy, embedded rear edge, closed slab and two visible brackets.
        var canopy=Quaternion.Euler(-14,0,0);
        m.Box("HouseRoof",new Vector3(-.25f,2.43f,-2.36f),new Vector3(1.58f,.12f,.95f),canopy);
        m.Box("HouseTrim",new Vector3(-.25f,2.27f,-2.82f),new Vector3(1.64f,.13f,.10f));
        foreach(float x in new[]{-.87f,.37f})m.Box("Timber",new Vector3(x,2.12f,-2.23f),new Vector3(.085f,.085f,.58f),Quaternion.Euler(-40,0,0));
        m.Box("Stone",new Vector3(-1.12f,3.98f,.75f),new Vector3(.46f,1.22f,.48f));
        // Four coping pieces frame a recessed dark chimney opening.
        foreach(int s in new[]{-1,1})
        {
            m.Box("HouseTrim",new Vector3(-1.12f+s*.245f,4.63f,.75f),new Vector3(.13f,.14f,.62f));
            m.Box("HouseTrim",new Vector3(-1.12f,4.63f,.75f+s*.245f),new Vector3(.36f,.14f,.13f));
        }
        m.Box("Timber",new Vector3(-1.12f,4.59f,.75f),new Vector3(.36f,.025f,.36f));
        var house=m.Finish("HomeA","Home A",parent,p,mats,2000);
        var interior=house.AddComponent<TinyDays.Review.FarmInteriorVolume>();interior.center=new Vector3(0,1.57f,0);interior.size=new Vector3(4.3f,2.66f,4);interior.roofRise=1.1f;
        return house;
    }
    public static GameObject Crate(Transform parent,Vector3 p,float angle,Dictionary<string,Material> mats)
    {
        var m=new Model();
        for(int i=0;i<3;i++)m.Box("Door",new Vector3((i-1)*.205f,.035f,0),new Vector3(.20f,.07f,.48f));
        for(int row=0;row<3;row++)foreach(int s in new[]{-1,1})
        {
            m.Box("Fence",new Vector3(0,.13f+row*.13f,s*.25f),new Vector3(.67f,.105f,.055f));
            m.Box("Door",new Vector3(s*.305f,.13f+row*.13f,0),new Vector3(.06f,.105f,.445f));
        }
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})m.Box("Timber",new Vector3(x*.255f,.24f,z*.195f),new Vector3(.055f,.39f,.055f));
        var g=m.Finish("Crate","Wooden produce crate",parent,p,mats,300);g.transform.localRotation=Quaternion.Euler(0,angle,0);
        var interior=g.AddComponent<TinyDays.Review.FarmInteriorVolume>();interior.center=new Vector3(0,.24f,0);interior.size=new Vector3(.61f,.41f,.5f);return g;
    }
    public static GameObject Barrel(Transform parent,Vector3 p,Dictionary<string,Material> mats)
    {
        var m=new Model();m.Lathe("Door",new[]{.04f,.25f,.64f,.86f},new[]{.28f,.33f,.33f,.28f},true);
        m.Lathe("Timber",new[]{.16f,.23f},new[]{.321f,.337f},false);
        m.Lathe("Timber",new[]{.67f,.74f},new[]{.334f,.318f},false);
        m.Lathe("Fence",new[]{.86f,.905f},new[]{.287f,.287f},false);
        for(int i=-1;i<=1;i++)m.Box("Timber",new Vector3(i*.13f,.909f,0),new Vector3(.008f,.005f,i==0?.55f:.47f));
        return m.Finish("Barrel","Rain barrel",parent,p,mats,300);
    }
    public static GameObject Bench(Transform parent,Vector3 p,Dictionary<string,Material> mats)
    {
        var m=new Model();
        for(int i=0;i<3;i++)m.Box("Door",new Vector3(0,.53f,(i-1)*.19f),new Vector3(1.9f,.11f,.17f));
        foreach(float y in new[]{.85f,1.08f})m.Box("Fence",new Vector3(0,y,.30f),new Vector3(1.9f,.19f,.09f));
        foreach(float x in new[]{-.7f,.7f})
        {
            foreach(float z in new[]{-.2f,.25f})m.Box("Timber",new Vector3(x,.25f,z),new Vector3(.11f,.5f,.11f));
            m.Box("Timber",new Vector3(x,.43f,0),new Vector3(.13f,.10f,.62f));
            m.Box("Timber",new Vector3(x,.84f,.34f),new Vector3(.09f,.64f,.09f));
        }
        m.Box("Timber",new Vector3(0,.24f,.25f),new Vector3(1.4f,.08f,.08f));
        return m.Finish("Bench","Bench",parent,p,mats,300);
    }
    public static void Fence(Transform parent,Vector3 a,Vector3 b,Dictionary<string,Material> mats)
    {
        int n=Mathf.CeilToInt(Vector3.Distance(a,b)/1.3f);float length=Vector3.Distance(a,b)/n;
        for(int i=0;i<n;i++)
        {
            var m=new Model();
            // Share corner ownership across adjoining fence runs, not just within one run.
            var start=Vector3.Lerp(a,b,i/(float)n);var finish=Vector3.Lerp(a,b,(i+1)/(float)n);
            bool hasStart=fencePosts.Add(Vector3Int.RoundToInt(start*1000));
            bool hasEnd=i==n-1&&fencePosts.Add(Vector3Int.RoundToInt(finish*1000));
            for(int end=0;end<2;end++)
            {
                if(end==0&&!hasStart||end==1&&!hasEnd)continue;
                float z=end*length;
                m.Box("Fence",new Vector3(0,.47f,z),new Vector3(.15f,.94f,.15f));
                m.Box("Fence",new Vector3(0,.97f,z),new Vector3(.19f,.08f,.19f));
            }
            foreach(float y in new[]{.36f,.71f})m.Box("Fence",new Vector3(0,y,length/2),new Vector3(.08f,.12f,length-.15f));
            string key="Fence-"+length.ToString("F4",CultureInfo.InvariantCulture)+(hasStart?"-Start":"")+(hasEnd?"-End":"");
            var g=m.Finish(key,"Fence span",parent,Vector3.Lerp(a,b,i/(float)n),mats,100);g.transform.rotation=Quaternion.LookRotation(b-a);
        }
    }
}
