using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;
using Model=FarmLowPolyAssets.Model;

public static class HouseVillageAssets
{
    public const string Folder="Assets/Art/Generated/HouseVillage";
    public static readonly string[] Names={"Timber Cottage","Ochre Farmhouse","Straw Cottage"};
    public static readonly float[] Width={3.6f,4.3f,3.8f},Depth={3.4f,3.8f,3.5f},Height={2.15f,3.05f,2.5f},Peak={4.65f,4.45f,4.05f};
    struct Hole {public float x,y,w,h;public bool door;public Hole(float x,float y,float w,float h,bool door=false){this.x=x;this.y=y;this.w=w;this.h=h;this.door=door;}}
    static void Frame(Model m,Vector3 origin,Quaternion q,Hole h,string trim)
    {
        Action<string,Vector3,Vector3> box=(mat,p,s)=>m.Box(mat,origin+q*p,s,q);
        const float t=.09f;
        foreach(int sign in new[]{-1,1})
        {
            box(trim,new Vector3(h.x+sign*(h.w-t)/2,h.y,-.03f),new Vector3(t,h.h,.18f));
            box(trim,new Vector3(h.x,h.y+sign*(h.h-t)/2,-.03f),new Vector3(h.w-2*t,t,.18f));
        }
        box(h.door?"Door":"Glass",new Vector3(h.x,h.y,.065f),new Vector3(h.w-2*t,h.h-2*t,.04f));
        if(h.door)
        {
            for(int j=-1;j<=1;j++)box("DarkWood",new Vector3(h.x+j*.18f,h.y,.04f),new Vector3(.012f,h.h-.2f,.008f));
            box("Brass",new Vector3(h.x+h.w*.27f,h.y,-.005f),new Vector3(.045f,.09f,.07f));
        }
        else
        {
            box(trim,new Vector3(h.x,h.y,-.035f),new Vector3(.05f,h.h-.18f,.06f));
            if(h.w>.7f)box(trim,new Vector3(h.x,h.y,-.035f),new Vector3(h.w-.18f,.05f,.06f));
            box(trim,new Vector3(h.x,h.y-h.h/2-.045f,-.09f),new Vector3(h.w+.14f,.09f,.32f));
        }
    }
    static void Wall(Model m,float width,float height,Vector3 origin,float yaw,string wall,string trim,params Hole[] holes)
    {
        var q=Quaternion.Euler(0,yaw,0);var xs=new List<float>{-width/2,width/2};foreach(var h in holes){xs.Add(h.x-h.w/2);xs.Add(h.x+h.w/2);}xs.Sort();
        for(int j=0;j<xs.Count-1;j++)
        {
            float x=(xs[j]+xs[j+1])/2,low=.22f;
            Action<float,float> fill=(a,b)=>{if(b-a>.0001f)m.Box(wall,origin+q*new Vector3(x,(a+b)/2,.08f),new Vector3(xs[j+1]-xs[j],b-a,.16f),q);};
            foreach(var hole in holes.Where(h=>x>h.x-h.w/2&&x<h.x+h.w/2).OrderBy(h=>h.y)){fill(low,hole.y-hole.h/2);low=hole.y+hole.h/2;}
            fill(low,height);
        }
        foreach(var h in holes)Frame(m,origin,q,h,trim);
    }
    static void Gable(Model m,float half,float bottom,float top,float z,string wall,bool window)
    {
        Func<float,float> width=y=>half*(top-y)/(top-bottom);
        Action<float,float> band=(a,b)=>{
            if(b>=top-.0001f)m.Prism(wall,new[]{new Vector2(-width(a),a),new Vector2(width(a),a),new Vector2(0,top)},z-.08f,z+.08f);
            else m.Prism(wall,new[]{new Vector2(-width(a),a),new Vector2(width(a),a),new Vector2(width(b),b),new Vector2(-width(b),b)},z-.08f,z+.08f);
        };
        if(!window){band(bottom,top);return;}
        float lo=bottom+.70f,hi=lo+.62f,x=.34f;
        band(bottom,lo);band(hi,top);
        m.Prism(wall,new[]{new Vector2(-width(lo),lo),new Vector2(-x,lo),new Vector2(-x,hi),new Vector2(-width(hi),hi)},z-.08f,z+.08f);
        m.Prism(wall,new[]{new Vector2(x,lo),new Vector2(width(lo),lo),new Vector2(width(hi),hi),new Vector2(x,hi)},z-.08f,z+.08f);
        Frame(m,new Vector3(0,0,z-.08f),Quaternion.identity,new Hole(0,(lo+hi)/2,x*2,hi-lo),"WoodTrim");
    }
    public static GameObject House(int variant,Transform root,Vector3 position,Dictionary<string,Material> mats)
    {
        var m=new Model();float w=Width[variant],d=Depth[variant],h=Height[variant],peak=Peak[variant];
        string wall=variant==0?"WoodWall":variant==1?"OchreWall":"IvoryWall",trim=variant==1?"Cream":"WoodTrim",roof=variant==0?"Slate":variant==1?"RoofBrown":"Straw";
        float doorX=variant==0?.25f:variant==1?-.55f:-.35f,doorH=variant==0?1.75f:1.95f;
        m.Box("Stone",new Vector3(0,.11f,0),new Vector3(w+.16f,.22f,d+.16f));
        var door=new Hole(doorX,.22f+doorH/2,.90f,doorH,true);
        Wall(m,w,h,new Vector3(0,0,-d/2),0,wall,trim,new Hole(-w*.35f,1.35f,.70f,.82f),door,new Hole(w*.32f,1.45f,variant==0?.65f:1.0f,variant==0?.75f:1.05f));
        Wall(m,w,h,new Vector3(0,0,d/2),180,wall,trim,new Hole(.15f,1.45f,.88f,.94f));
        Wall(m,d-.32f,h,new Vector3(-w/2,0,0),90,wall,trim,new Hole(0,1.35f,.82f,.95f));
        // The ochre annex joins an unperforated right wall; its outer wall carries a window.
        if(variant==1)Wall(m,d-.32f,h,new Vector3(w/2,0,0),-90,wall,trim);
        else Wall(m,d-.32f,h,new Vector3(w/2,0,0),-90,wall,trim,new Hole(.15f,1.38f,.74f,.88f));
        Gable(m,w/2,h,peak,-d/2+.08f,wall,variant==0);Gable(m,w/2,h,peak,d/2-.08f,wall,false);
        float eave=w/2+.3f,rise=(peak-h)*eave/(w/2),low=peak-rise,len=Mathf.Sqrt(eave*eave+rise*rise),thick=variant==2?.25f:.14f;
        for(int sign=-1;sign<=1;sign+=2)
        {
            var q=Quaternion.Euler(0,0,-sign*Mathf.Atan2(rise,eave)*Mathf.Rad2Deg);
            for(int j=0;j<5;j++)m.Box(j%2==0?roof:roof+"Alt",new Vector3(sign*eave/2,(low+peak)/2,-(d+.6f)/2+(j+.5f)*(d+.6f)/5),new Vector3(len,thick,(d+.6f)/5),q);
            foreach(float z in new[]{-d/2-.35f,d/2+.35f})m.Box(variant==2?"StrawAlt":trim,new Vector3(sign*eave/2,(low+peak)/2-.035f,z),new Vector3(len+.08f,thick+.05f,.11f),q);
            m.Box(variant==2?"StrawAlt":trim,new Vector3(sign*eave,low-.05f,0),new Vector3(.13f,thick+.05f,d+.8f));
        }
        m.Box(roof+"Alt",new Vector3(0,peak+.07f,0),new Vector3(.20f,.16f,d+.82f));
        if(variant==0)
        {
            for(float y=.48f;y<peak-.2f;y+=.42f)
            {
                float half=y<=h?w/2:w/2*(peak-y)/(peak-h);
                var cuts=new List<Vector2>{new Vector2(-half,half)};
                var openings=new[]{new Hole(-w*.35f,1.35f,.70f,.82f),door,new Hole(w*.32f,1.45f,.65f,.75f),new Hole(0,h+1.01f,.68f,.62f)};
                foreach(var hole in openings.Where(a=>Mathf.Abs(y-a.y)<a.h/2+.03f))
                {
                    var next=new List<Vector2>();float left=hole.x-hole.w/2-.03f,right=hole.x+hole.w/2+.03f;
                    foreach(var span in cuts){if(right<=span.x||left>=span.y)next.Add(span);else{if(left>span.x)next.Add(new Vector2(span.x,left));if(right<span.y)next.Add(new Vector2(right,span.y));}}cuts=next;
                }
                foreach(var span in cuts)if(span.y-span.x>.04f)m.Box("WoodTrim",new Vector3((span.x+span.y)/2,y,-d/2-.012f),new Vector3(span.y-span.x,.018f,.018f));
            }
            // A few broad siding courses are separated at the door/window openings.
            for(int j=0;j<7;j++)foreach(int s in new[]{-1,1})m.Box("WoodTrim",new Vector3(s*(w/2+.015f),.4f+j*.24f,.9f),new Vector3(.03f,.025f,1.15f));
            for(int j=0;j<6;j++)m.Box("Door",new Vector3(-1.45f+j*.58f,.21f,-d/2-.64f),new Vector3(.56f,.15f,1.28f));
            foreach(float x in new[]{-1.25f,1.25f})m.Box("DarkWood",new Vector3(x,.11f,-d/2-1.05f),new Vector3(.14f,.22f,.14f));
            m.Box("WoodTrim",new Vector3(doorX,.09f,-d/2-1.42f),new Vector3(1.25f,.18f,.4f));
        }
        else
        {
            m.Box("Stone",new Vector3(doorX,.11f,-d/2-.31f),new Vector3(1.3f,.22f,.64f));
            m.Box(roof,new Vector3(doorX,2.42f,-d/2-.30f),new Vector3(1.4f,.12f,.8f),Quaternion.Euler(-16,0,0));
            foreach(float x in new[]{doorX-.58f,doorX+.58f})m.Box(trim,new Vector3(x,2.12f,-d/2-.2f),new Vector3(.075f,.5f,.075f));
            float cx=-w*.25f,cy=peak-.05f;
            m.Box("Stone",new Vector3(cx,cy,.5f),new Vector3(.46f,1.15f,.48f));
            m.Box("Cream",new Vector3(cx,cy+.6f,.5f),new Vector3(.60f,.14f,.62f));
        }
        if(variant==1)
        {
            float x=w/2+.54f;
            m.Box("Stone",new Vector3(x,.11f,.3f),new Vector3(1.24f,.22f,2.14f));
            Wall(m,1.08f,2.04f,new Vector3(x,0,-.7f),0,wall,trim);
            Wall(m,1.08f,2.04f,new Vector3(x,0,1.3f),180,wall,trim);
            Wall(m,1.68f,2.04f,new Vector3(x+.54f,0,.3f),-90,wall,trim,new Hole(0,1.15f,.80f,.90f));
            m.Box("RoofBrown",new Vector3(x,2.25f,.3f),new Vector3(1.45f,.16f,2.5f),Quaternion.Euler(0,0,-16));
            // Fill the shallow roof wedge, including both ends and underside.
            m.Prism(wall,new[]{new Vector2(w/2,2.04f),new Vector2(w/2+1.08f,2.04f),new Vector2(w/2+1.08f,2.09f),new Vector2(w/2,2.40f)},-.7f,1.3f);
        }
        var house=m.Finish(Names[variant],Names[variant],root,position,mats,2000,Folder);
        var volume=house.AddComponent<FarmInteriorVolume>();volume.center=new Vector3(0,(h+.22f)/2,0);volume.size=new Vector3(w,h-.22f,d);volume.roofRise=peak-h;
        if(variant==1){var annex=new GameObject("Annex interior");annex.transform.SetParent(house.transform,false);var v=annex.AddComponent<FarmInteriorVolume>();v.center=new Vector3(w/2+.54f,1.1f,.3f);v.size=new Vector3(1.08f,1.76f,2);}
        return house;
    }
    static void Flower(Model m,Vector3 p,string color,float height)
    {
        m.Box("Leaf",p+Vector3.up*height/2,new Vector3(.025f,height,.025f));
        foreach(int s in new[]{-1,1})m.Octa("LeafLight",p+new Vector3(s*.10f,height*.45f,0),new Vector3(.14f,.045f,.07f));
        var center=p+Vector3.up*height;
        for(int j=0;j<5;j++){float a=j*Mathf.PI*2/5;m.Octa(color,center+new Vector3(Mathf.Cos(a)*.12f,0,Mathf.Sin(a)*.12f),new Vector3(.095f,.05f,.095f));}
        m.Octa("FlowerGold",center+Vector3.up*.045f,new Vector3(.065f,.04f,.065f));
    }
    public static GameObject Decoration(string kind,Transform root,Vector3 p,Dictionary<string,Material> mats)
    {
        var m=new Model();
        if(kind=="Shrub")
        {
            m.Blob("Leaf",new Vector3(-.18f,.34f,0),new Vector3(.38f,.34f,.32f));m.Blob("LeafLight",new Vector3(.2f,.30f,.02f),new Vector3(.31f,.28f,.30f));
        }
        else if(kind=="Pot")
        {
            m.Lathe("Terracotta",new[]{0f,.30f,.34f},new[]{.16f,.23f,.25f},true);
            m.Lathe("Soil",new[]{.342f,.355f},new[]{.205f,.205f},false);
            Flower(m,new Vector3(0,.35f,0),"Pink",.33f);
        }
        else if(kind=="PinkBed"||kind=="GoldBed")
        {
            m.Box("Soil",new Vector3(0,.10f,0),new Vector3(.88f,.20f,.48f));
            foreach(int s in new[]{-1,1}){m.Box("Stone",new Vector3(s*.47f,.12f,0),new Vector3(.10f,.24f,.64f));m.Box("Stone",new Vector3(0,.12f,s*.29f),new Vector3(.84f,.24f,.10f));}
            Flower(m,new Vector3(-.22f,.20f,0),kind=="PinkBed"?"Pink":"FlowerGold",.43f);Flower(m,new Vector3(.21f,.20f,0),kind=="PinkBed"?"Pink":"FlowerGold",.33f);
        }
        else if(kind=="Stones")
        {
            for(int j=0;j<3;j++)m.Box("Stone",new Vector3((j%2)*.08f,.035f,j*.50f),new Vector3(.48f,.07f,.37f),Quaternion.Euler(0,j*13-12,0));
        }
        else if(kind=="Fence")
        {
            foreach(float x in new[]{-.65f,.65f}){m.Box("WoodTrim",new Vector3(x,.42f,0),new Vector3(.13f,.84f,.13f));m.Box("WoodTrim",new Vector3(x,.87f,0),new Vector3(.18f,.08f,.18f));}
            foreach(float y in new[]{.32f,.64f})m.Box("WoodTrim",new Vector3(0,y,0),new Vector3(1.17f,.11f,.07f));
        }
        return m.Finish(kind,kind,root,p,mats,kind=="Fence"?100:300,Folder);
    }
    public static GameObject Existing(string key,string name,Transform root,Vector3 p)
    {
        var group=new GameObject(name);group.transform.SetParent(root,false);group.transform.localPosition=p;
        foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/Art/Generated/FarmStudy/LowPoly"}))
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));if(!mesh.name.StartsWith(key+"-"))continue;
            var part=new GameObject(mesh.name);part.transform.SetParent(group.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/FarmStudy/"+mesh.name.Substring(key.Length+1)+".mat");
        }
        if(key=="Crate"){var v=group.AddComponent<FarmInteriorVolume>();v.center=new Vector3(0,.24f,0);v.size=new Vector3(.61f,.41f,.5f);}
        return group;
    }
}
