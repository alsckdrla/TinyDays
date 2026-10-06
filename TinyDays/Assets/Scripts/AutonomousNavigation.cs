using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyDays.Life
{
    // A small scene-sized grid. It has no dependency on the prototype's fixed terrain.
    public sealed class AutonomousNavigation
    {
        readonly Bounds land;
        readonly Bounds[] obstacles;
        readonly float cell, radius;
        readonly int width, height;
        readonly bool[] blocked;
        public Bounds? TemporaryBlock,ConstructionBlock,FieldBlock,GardenBlock; public Vector3[] Avoidance=Array.Empty<Vector3>();
        public AutonomousNavigation(Bounds land, Bounds[] obstacles, float radius=.32f, float cell=.35f)
        {
            this.land=land;this.obstacles=obstacles;this.radius=radius;this.cell=cell;
            width=Mathf.CeilToInt(land.size.x/cell)+1;height=Mathf.CeilToInt(land.size.z/cell)+1;
            blocked=new bool[width*height];
            for(int i=0;i<blocked.Length;i++)blocked[i]=!GeometryClear(Point(i));
        }
        public Vector3 Point(int i)=>new Vector3(land.min.x+(i%width)*cell,0,land.min.z+(i/width)*cell);
        int Index(Vector3 p)=>Mathf.Clamp(Mathf.RoundToInt((p.z-land.min.z)/cell),0,height-1)*width+Mathf.Clamp(Mathf.RoundToInt((p.x-land.min.x)/cell),0,width-1);
        bool GeometryClear(Vector3 p)
        {
            float x=(p.x-land.center.x)/(land.extents.x-radius),z=(p.z-land.center.z)/(land.extents.z-radius);
            if(x*x+z*z>1)return false;
            foreach(var b in obstacles)if(p.x>b.min.x-radius&&p.x<b.max.x+radius&&p.z>b.min.z-radius&&p.z<b.max.z+radius)return false;
            return true;
        }
        public bool Clear(Vector3 p)
        {
            if(!GeometryClear(p))return false;foreach(var other in Avoidance)if((p-other).sqrMagnitude<.599f*.599f)return false;
            if(GardenBlock.HasValue){var b=GardenBlock.Value;if(p.x>b.min.x-radius&&p.x<b.max.x+radius&&p.z>b.min.z-radius&&p.z<b.max.z+radius)return false;}
            if(FieldBlock.HasValue){var b=FieldBlock.Value;if(p.x>b.min.x-radius&&p.x<b.max.x+radius&&p.z>b.min.z-radius&&p.z<b.max.z+radius)return false;}
            if(ConstructionBlock.HasValue){var b=ConstructionBlock.Value;if(p.x>b.min.x-radius&&p.x<b.max.x+radius&&p.z>b.min.z-radius&&p.z<b.max.z+radius)return false;}
            if(TemporaryBlock.HasValue){var b=TemporaryBlock.Value;if(p.x>b.min.x-radius&&p.x<b.max.x+radius&&p.z>b.min.z-radius&&p.z<b.max.z+radius)return false;}
            return true;
        }
        static bool Crosses(Vector3 a,Vector3 b,Bounds box,float padding)
        {
            float lo=0,hi=1;Vector3 d=b-a;
            foreach(int axis in new[]{0,2}){
                float min=box.min[axis]-padding,max=box.max[axis]+padding;
                if(Mathf.Abs(d[axis])<1e-8f){if(a[axis]<=min||a[axis]>=max)return false;continue;}
                float x=(min-a[axis])/d[axis],y=(max-a[axis])/d[axis];if(x>y){float swap=x;x=y;y=swap;}
                lo=Mathf.Max(lo,x);hi=Mathf.Min(hi,y);if(lo>=hi)return false;
            }
            return hi>0&&lo<1;
        }
        public bool Segment(Vector3 a,Vector3 b)
        {
            if(!Clear(a)||!Clear(b))return false;
            foreach(var box in obstacles)if(Crosses(a,b,box,radius))return false;
            if(GardenBlock.HasValue&&Crosses(a,b,GardenBlock.Value,radius))return false;
            if(FieldBlock.HasValue&&Crosses(a,b,FieldBlock.Value,radius))return false;
            if(ConstructionBlock.HasValue&&Crosses(a,b,ConstructionBlock.Value,radius))return false;
            if(TemporaryBlock.HasValue&&Crosses(a,b,TemporaryBlock.Value,radius))return false;
            Vector3 d=b-a;foreach(var p in Avoidance){float u=d.sqrMagnitude<1e-9f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);if((a+d*u-p).sqrMagnitude<.599f*.599f)return false;}
            return true;
        }        public Vector3 Nearby(Vector3 p,float range=2)
        {
            float best=float.PositiveInfinity;Vector3 result=p;
            for(int i=0;i<blocked.Length;i++)if(!blocked[i]&&Clear(Point(i)))
            {float d=(Point(i)-p).sqrMagnitude;if(d<best&&d<=range*range){best=d;result=Point(i);}}
            if(float.IsPositiveInfinity(best))throw new InvalidOperationException("No accessible station near "+p);
            return result;
        }
        int NearestConnection(Vector3 p)
        {
            int center=Index(p),best=-1;float distance=float.PositiveInfinity;
            for(int dz=-3;dz<=3;dz++)for(int dx=-3;dx<=3;dx++){
                int x=center%width+dx,z=center/width+dz;if(x<0||x>=width||z<0||z>=height)continue;
                int i=z*width+x;float d=(Point(i)-p).sqrMagnitude;if(!blocked[i]&&d<distance&&Segment(p,Point(i))){best=i;distance=d;}}
            return best;
        }
        public List<Vector3> Find(Vector3 start,Vector3 end)
        {
            if(!Clear(start)||!Clear(end))return null;
            if(Segment(start,end))return new List<Vector3>{end};
            int from=NearestConnection(start),to=NearestConnection(end),length=blocked.Length;if(from<0||to<0)return null;
            if(blocked[from]||blocked[to]||!Segment(start,Point(from))||!Segment(Point(to),end))return null;
            var cost=new float[length];var parent=new int[length];var closed=new bool[length];
            for(int i=0;i<length;i++){cost[i]=float.PositiveInfinity;parent[i]=-1;}
            var open=new SortedSet<(float,int)>();cost[from]=0;open.Add((0,from));
            while(open.Count>0)
            {
                var entry=open.Min;open.Remove(entry);int at=entry.Item2;if(closed[at])continue;closed[at]=true;
                if(at==to){var raw=new List<Vector3>{end};while(at!=from){raw.Add(Point(at));at=parent[at];}raw.Add(Point(from));raw.Reverse();
                    var path=new List<Vector3>();Vector3 anchor=start;int k=0;
                    while(k<raw.Count){int far=k;while(far+1<raw.Count&&Segment(anchor,raw[far+1]))far++;path.Add(raw[far]);anchor=raw[far];k=far+1;}return path;}
                int x=at%width,z=at/width;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,nz=z+dz;if((dx==0&&dz==0)||nx<0||nx>=width||nz<0||nz>=height)continue;
                    int next=nz*width+nx;if(closed[next]||blocked[next]||!Segment(Point(at),Point(next)))continue;
                    float c=cost[at]+cell*(dx!=0&&dz!=0?1.414214f:1);
                    if(c>=cost[next])continue;cost[next]=c;parent[next]=at;open.Add((c+Vector3.Distance(Point(next),end),next));
                }
            }
            return null;
        }
    }
}
