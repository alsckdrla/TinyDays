using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays
{
    public sealed class VillageNavigation : MonoBehaviour
    {
        public const float Cell=.25f;
        const int Width=109, Height=81;
        public float padding=.55f;
        NavigationObstacle[] obstacles=new NavigationObstacle[0];
        struct Footprint
        {
            public Matrix4x4 inverse;
            public Vector2 scale, center, half;
            public float radius;
            public Vector3 worldCenter;
            public float broadRadius;
            public bool circle, road;
            public bool Contains(Vector3 world,float margin)
            {
                float broad=broadRadius+margin;
                if(Mathf.Abs(world.x-worldCenter.x)>broad||Mathf.Abs(world.z-worldCenter.z)>broad) return false;
                var local=inverse.MultiplyPoint3x4(world);
                var p=Vector2.Scale(new Vector2(local.x,local.z)-center,scale);
                if(circle) return p.sqrMagnitude<(radius+margin)*(radius+margin);
                var q=new Vector2(Mathf.Max(0,Mathf.Abs(p.x)-half.x),Mathf.Max(0,Mathf.Abs(p.y)-half.y));
                return q.sqrMagnitude<margin*margin || (Mathf.Abs(p.x)<=half.x&&Mathf.Abs(p.y)<=half.y);
            }
        }
        Footprint[] footprints=new Footprint[0];
        bool[] blocked, roads;
        sbyte[] edges;
        int revision=-1;
        public int Version { get; private set; }
        public IReadOnlyList<NavigationObstacle> Obstacles => obstacles;
        public void Refresh()
        {
            foreach(var obstacle in obstacles) if(obstacle) obstacle.CheckForChanges();
            var settings=GetComponent<VillageAutonomySettings>();
            if(settings&&!Mathf.Approximately(padding,settings.residentRadius+settings.obstacleMargin)) { padding=settings.residentRadius+settings.obstacleMargin; revision=-1; }
            if(blocked!=null && revision==NavigationObstacle.Revision) return;
            obstacles=FindObjectsOfType<NavigationObstacle>().Where(o=>o.isActiveAndEnabled&&o.gameObject.scene==gameObject.scene).ToArray();
            footprints=obstacles.Select(o=>new Footprint { inverse=o.transform.worldToLocalMatrix, scale=new Vector2(Mathf.Abs(o.transform.lossyScale.x),Mathf.Abs(o.transform.lossyScale.z)), center=o.center, half=Vector2.Scale(o.size,new Vector2(Mathf.Abs(o.transform.lossyScale.x),Mathf.Abs(o.transform.lossyScale.z)))*.5f, radius=o.radius*Mathf.Max(Mathf.Abs(o.transform.lossyScale.x),Mathf.Abs(o.transform.lossyScale.z)), circle=o.circle,road=o.road, worldCenter=o.transform.TransformPoint(new Vector3(o.center.x,0,o.center.y)), broadRadius=o.circle?o.radius*Mathf.Max(Mathf.Abs(o.transform.lossyScale.x),Mathf.Abs(o.transform.lossyScale.z)):Vector2.Scale(o.size,new Vector2(Mathf.Abs(o.transform.lossyScale.x),Mathf.Abs(o.transform.lossyScale.z))).magnitude*.5f }).ToArray();
            blocked=new bool[Width*Height]; roads=new bool[Width*Height];
            edges=new sbyte[Width*Height*9];
            for(int i=0;i<blocked.Length;i++) { var p=Point(i); blocked[i]=!ClearPoint(p); roads[i]=IsRoad(p); }
            revision=NavigationObstacle.Revision; Version++;
        }
        public bool ClearPoint(Vector3 p)
        {
            if(Mathf.Abs(p.x)>13.5f || Mathf.Abs(p.z)>10f) return false;
            foreach(var o in footprints) if(!o.road&&o.Contains(p,padding)) return false;
            return true;
        }
        public bool ClearSegment(Vector3 a,Vector3 b)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.05f));
            for(int i=0;i<=steps;i++) if(!ClearPoint(Vector3.Lerp(a,b,(float)i/steps))) return false;
            return true;
        }
        public bool IsRoad(Vector3 p) { foreach(var o in footprints) if(o.road&&o.Contains(p,0)) return true; return false; }
        Vector3 Point(int i) => new Vector3(-13.5f+(i%Width)*Cell,.065f,-10f+(i/Width)*Cell);
        int Closest(Vector3 p)
        {
            int best=-1; float distance=float.MaxValue;
            int cx=Mathf.RoundToInt((p.x+13.5f)/Cell), cz=Mathf.RoundToInt((p.z+10f)/Cell);
            for(int z=Mathf.Max(0,cz-3);z<=Mathf.Min(Height-1,cz+3);z++)
            for(int x=Mathf.Max(0,cx-3);x<=Mathf.Min(Width-1,cx+3);x++)
            { int i=z*Width+x; if(blocked[i]) continue; float d=(Point(i)-p).sqrMagnitude; if(d<distance&&ClearSegment(p,Point(i))) { best=i; distance=d; } }
            return best;
        }
        public bool FindPath(Vector3 start,Vector3 end,float preference,List<Vector3> result,List<Vector3> temporary=null)
        {
            Refresh(); result.Clear();
            if(!ClearPoint(start)||!ClearPoint(end)) return false;
            int source=Closest(start), destination=Closest(end); if(source<0||destination<0) return false;
            var costs=Enumerable.Repeat(float.PositiveInfinity,blocked.Length).ToArray();
            var parents=Enumerable.Repeat(-1,blocked.Length).ToArray();
            var closed=new bool[blocked.Length];
            var open=new SortedSet<(float score,int id,int serial)>(); int serial=0;
            costs[source]=0; open.Add((0,source,serial++));
            while(open.Count>0)
            {
                var entry=open.Min; open.Remove(entry); int current=entry.id;
                if(closed[current]) continue; closed[current]=true;
                if(current==destination) break;
                int x=current%Width,z=current/Width;
                for(int dz=-1;dz<=1;dz++) for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dz==0) continue;
                    int nx=x+dx,nz=z+dz; if(nx<0||nx>=Width||nz<0||nz>=Height) continue;
                    int next=nz*Width+nx; if(blocked[next]||closed[next]) continue;
                    if(dx!=0&&dz!=0&&(blocked[z*Width+nx]||blocked[nz*Width+x])) continue;
                    var point=Point(next);
                    if(temporary!=null&&temporary.Any(p=>(p-point).sqrMagnitude<1.15f*1.15f) && (point-start).sqrMagnitude>1.5f) continue;
                    int edge=current*9+(dz+1)*3+dx+1;
                    if(edges[edge]==0) edges[edge]=(sbyte)(ClearSegment(Point(current),point)?1:-1);
                    if(edges[edge]<0) continue;
                    float candidate=costs[current]+Cell*(dx!=0&&dz!=0?1.414214f:1f)*(roads[next]?1f:Mathf.Lerp(1.15f,1.8f,preference));
                    if(candidate>=costs[next]) continue;
                    costs[next]=candidate; parents[next]=current;
                    open.Add((candidate+Vector3.Distance(point,Point(destination)),next,serial++));
                }
            }
            if(!closed[destination]) return false;
            var raw=new List<Vector3>{end};
            for(int at=destination;at!=source;at=parents[at]) raw.Add(Point(at));
            raw.Add(start); raw.Reverse();
            var cumulative=new float[raw.Count];
            for(int i=1;i<raw.Count;i++) cumulative[i]=cumulative[i-1]+WeightedLength(raw[i-1],raw[i],preference);
            // Smooth only when the shortcut is safe and does not erase the resident's road preference.
            int index=0;
            while(index<raw.Count-1)
            {
                int best=index+1;
                for(int j=raw.Count-1;j>index+1;j--)
                {
                    if(ClearSegment(raw[index],raw[j])&&WeightedLength(raw[index],raw[j],preference)<=(cumulative[j]-cumulative[index])*1.025f &&
                        (temporary==null||!temporary.Any(p=>SegmentDistance(p,raw[index],raw[j])<1.15f))) { best=j; break; }
                }
                result.Add(raw[best]); index=best;
            }
            return true;
        }
        float WeightedLength(Vector3 a,Vector3 b,float preference)
        {
            int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/Cell)); float cost=0;
            for(int i=0;i<n;i++) cost+=IsRoad(Vector3.Lerp(a,b,(i+.5f)/n))?1:Mathf.Lerp(1.15f,1.8f,preference);
            return Vector3.Distance(a,b)*cost/n;
        }
        public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b)
        {
            var d=b-a; float t=d.sqrMagnitude<.000001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);
            return Vector3.Distance(p,a+d*t);
        }
    }
}
