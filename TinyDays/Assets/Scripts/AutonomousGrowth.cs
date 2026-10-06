using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    [Serializable] public sealed class GrowthState
    {
        public bool stockpile;
        public int stage,wood,spent,nextDrop; // 0 waiting, 1 collecting, 2 building, 3 completed
        public float pressure,progress,retry;
        public int[] sources=new[]{6,6};
        public Vector3 site;
        public List<WoodDrop> drops=new List<WoodDrop>();
    }
    [Serializable] public sealed class WoodDrop { public int id,amount,owner=-1;public Vector3 position;public float retry; }
    public sealed class GrowthLayout
    {
        public Vector3[] sources;public Vector3 store;public Vector3[] candidates;
        public static Bounds Footprint(Vector3 p)=>new Bounds(p+Vector3.up*.8f,new Vector3(1.8f,1.6f,1.6f));
        public static Vector3 Work(Vector3 p)=>p+Vector3.back*1.45f;
        public LifeSite[] Sites()=>new[]{new LifeSite{id="wood-source-0",kind=SiteKind.WoodSource,position=sources[0]},new LifeSite{id="wood-source-1",kind=SiteKind.WoodSource,position=sources[1]},new LifeSite{id="wood-store",kind=SiteKind.WoodStore,position=store},new LifeSite{id="warehouse-build",kind=SiteKind.Build,position=store}};
        public static GrowthLayout Create(AutonomousLifeWorld w)
        {
            var nav=new AutonomousNavigation(w.land,w.RuntimeObstacles());
            // Preserve path triangles as well as above-ground props; never cover the original roads.
            var props=w.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.GetComponentInParent<FarmResidentVisual>()&&!r.name.Contains("Backdrop")&&!r.name.Contains("meadow")&&!r.name.Contains("foundation")&&r.bounds.max.y>.12f&&r.bounds.size.x<20&& !r.transform.name.StartsWith("Recoverable"))
                .Select(r=>r.bounds).ToArray();
            var roads=new List<Bounds>();
            foreach(var mf in w.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name=="Winding dirt path"||m.name=="Path branch"||m.name=="Resting clearing")){
                var v=mf.sharedMesh.vertices;var t=mf.sharedMesh.triangles;
                for(int i=0;i<t.Length;i+=3){var b=new Bounds(mf.transform.TransformPoint(v[t[i]]),Vector3.zero);b.Encapsulate(mf.transform.TransformPoint(v[t[i+1]]));b.Encapsulate(mf.transform.TransformPoint(v[t[i+2]]));b.center=new Vector3(b.center.x,.5f,b.center.z);b.size=new Vector3(b.size.x,4,b.size.z);roads.Add(b);}
            }
            var reserved=w.definitions.Concat(AutonomousWeather.Shelters(w)).Concat(PoultryLayout.Sites()).ToArray();
            bool Empty(Vector3 p,Vector3 size){var b=new Bounds(p+Vector3.up*.5f,size);return !roads.Any(x=>b.Intersects(x))&&!props.Any(x=>b.Intersects(x))&&!reserved.Any(s=>Mathf.Abs(s.position.x-p.x)<size.x*.5f+.65f&&Mathf.Abs(s.position.z-p.z)<size.z*.5f+.65f);}
            var points=new List<Vector3>();int empty=0,edge=0,route=0;string failed="";
            for(float z=-1;z<=10;z+=.5f)for(float x=2;x<=14;x+=.5f){var p=new Vector3(x,0,z);if(!Empty(p,new Vector3(2.1f,4,1.9f))||!nav.Clear(Work(p)))continue;empty++;var b=Footprint(p);var check=new AutonomousNavigation(w.land,w.RuntimeObstacles().Concat(new[]{b}).ToArray());if(new[]{new Vector3(-1,0,-.9f),new Vector3(1,0,-.9f),new Vector3(-1,0,.9f),new Vector3(1,0,.9f)}.Any(c=>!nav.Clear(p+c)))continue;edge++;var lost=reserved.FirstOrDefault(s=>check.Find(w.starts[0],s.position)==null);if(lost==null&&check.Find(w.starts[0],Work(p))!=null){route++;points.Add(p);}else failed=lost?.id??"work";}
            Debug.Log($"GROWTH_LAYOUT_DIAGNOSTIC empty={empty} edge={edge} route={route} failed={failed}");
            points=points.OrderBy(p=>(p-new Vector3(10.8f,0,3.8f)).sqrMagnitude).ToList();
            Vector3 Spot(Vector3 near){for(float radius=0;radius<6;radius+=.4f)for(int i=0;i<16;i++){var p=near+new Vector3(Mathf.Cos(i*Mathf.PI/8),0,Mathf.Sin(i*Mathf.PI/8))*radius;if(Empty(p,new Vector3(.75f,1,.75f))&&nav.Find(w.starts[0],p)!=null&&!points.Any(c=>Mathf.Abs(c.x-p.x)<1.4f&&Mathf.Abs(c.z-p.z)<1.4f))return p;}throw new InvalidOperationException("No safe timber station near "+near);}
            return new GrowthLayout{sources=new[]{Spot(new Vector3(-5,0,1)),Spot(new Vector3(5,0,0))},store=Spot(new Vector3(9,0,1)),candidates=points.ToArray()};
        }
    }
    public sealed partial class AutonomousSimulation
    {
        public GrowthState Growth {get;private set;}
        GrowthLayout growthLayout;
        public int Capacity=>Growth!=null&&Growth.stage==3?60:tuning.capacity;
        public int TargetFood=>Growth!=null&&Growth.stockpile?48:tuning.targetFood;
        public void ConfigureGrowth(GrowthLayout layout){growthLayout=layout;Growth=new GrowthState();}
        public void SetGrowthDirection(bool stockpile)
        {
            if(Growth==null)return;Growth.stockpile=stockpile;
            if(!stockpile)CancelFieldPlan();
            if(!stockpile&&Growth.stage<2){foreach(var r in residents)if(r.action==LifeAction.WoodGather||r.action==LifeAction.Build)Release(r,false);Growth.stage=0;Growth.pressure=0;navigation.ConstructionBlock=null;sites.First(s=>s.kind==SiteKind.Build).position=growthLayout.store;}
        }
        public string GrowthStatus=>Growth==null?"준비 중":Growth.stage==3?"창고 확장 완료":Growth.stage==2?$"건설 중 {Growth.progress/60:P0}":Growth.stage==1?"확장 목재 수집 중":!Growth.stockpile?"현재 생활 유지":Growth.pressure>=60?"공간 부족으로 확장 대기":$"식량 비축 중 · 확장 판단 {Growth.pressure:F0}/60초";
        void AdvanceGrowth(float dt)
        {
            if(Growth==null)return;var g=Growth;
            if(g.stage!=0||!g.stockpile||Field!=null&&Field.stage==2||Garden!=null&&Garden.stage==2)return;
            g.pressure=food>=30?Mathf.Min(60,g.pressure+dt):0;
            if(g.pressure<60||elapsed<g.retry)return;g.retry=elapsed+10;
            if(Field!=null&&Field.stage==1)CancelFieldPlan();CancelGardenPlan();
            foreach(var p in growthLayout.candidates){var b=GrowthLayout.Footprint(p);var old=navigation.ConstructionBlock;navigation.ConstructionBlock=b;
                bool valid=residents.All(r=>navigation.Clear(r.position))&&bundles.All(x=>navigation.Clear(x.position))&&g.drops.All(x=>navigation.Clear(x.position))&&sites.Where(s=>s.kind!=SiteKind.Build&&FieldSiteEnabled(s)&&GardenSiteEnabled(s)).All(s=>navigation.Find(residents[0].position,s.position)!=null)&&navigation.Find(residents[0].position,GrowthLayout.Work(p))!=null;
                if(!valid){navigation.ConstructionBlock=old;continue;}
                CancelFieldPlan();g.site=p;g.stage=1;sites.First(s=>s.kind==SiteKind.Build).position=GrowthLayout.Work(p);
                // Existing reservations survive. Movement checks reroute if its old path crosses the new footprint.
                break;
            }
        }
        bool GrowthEmergency(LifeResident r)=>Night||Raining||AvailableFood<4||r.fatigue>=RestThreshold(r)||r.hunger>=tuning.hungerThreshold;
        void InterruptGrowth(LifeResident r)
        {if((r.action==LifeAction.WoodGather||r.action==LifeAction.Build)&&GrowthEmergency(r))Release(r,false);}
        bool ChooseWoodCargo(LifeResident r)
        {
            if(r.woodCargo<=0)return false;
            if(sites.Any(s=>s.kind==SiteKind.WoodStore&&s.owner>=0)){WaitAside(r);r.reason="목재 보관 자리가 비기를 기다리는 중";return true;}
            if(Assign(r,SiteKind.WoodStore,LifeAction.WoodDeliver,"모은 목재를 창고 옆에 보관"))return true;
            DropWood(r);return false;
        }
        bool ChooseGrowth(LifeResident r)
        {
            if(Growth==null||GrowthEmergency(r))return false;var g=Growth;
            foreach(var b in g.drops.Where(b=>b.owner<0&&elapsed>=b.retry).OrderBy(b=>(b.position-r.position).sqrMagnitude)){
                var route=Route(r,b.position);if(route==null)continue;b.owner=r.id;r.woodDrop=b.id+1;r.action=LifeAction.WoodRecover;r.path=route;r.waypoint=0;r.reason="내려둔 목재를 다시 회수";return true;
            }
            if(g.stage==0||g.stage==3)return false;
            if(g.stage==2||g.wood==12){if(Assign(r,SiteKind.Build,LifeAction.Build,"저장공간을 늘리기 위해 창고 짓기"))return true;}
            if(g.stage!=1)return false;
            foreach(int i in Enumerable.Range(0,2))if(g.sources[i]>0&&Assign(r,SiteKind.WoodSource,LifeAction.WoodGather,"확장에 쓸 떨어진 나뭇가지 모으기",s=>s.id=="wood-source-"+i)){r.woodReservation=Mathf.Min(2,g.sources[i]);return true;}
            return false;
        }
        bool CompleteGrowth(LifeResident r,float dt)
        {
            if(r.action==LifeAction.WoodRecover){var b=Growth.drops.Single(x=>x.id+1==r.woodDrop);r.woodCargo=b.amount;Growth.drops.Remove(b);Release(r);return true;}
            if(r.action==LifeAction.Build){
                if(Growth.stage==1){if(Growth.wood!=12){Release(r,false);return true;}Growth.wood-=12;Growth.spent=12;Growth.stage=2;}
                Growth.progress=Mathf.Min(60,Growth.progress+dt);r.fatigue=Mathf.Min(1,r.fatigue+dt*tuning.workFatigue);
                if(Growth.progress>=60){Growth.stage=3;Release(r);}return true;
            }
            if(r.action!=LifeAction.WoodGather&&r.action!=LifeAction.WoodDeliver)return false;
            if(r.work<tuning.workSeconds)return true;
            if(r.action==LifeAction.WoodGather){int i=sites[r.site].id.EndsWith("0")?0:1;Growth.sources[i]-=r.woodReservation;r.woodCargo=r.woodReservation;}
            else {Growth.wood+=r.woodCargo;r.woodCargo=0;}
            Release(r);return true;
        }
        void ReleaseGrowth(LifeResident r){if(Growth!=null&&r.woodDrop>0){var b=Growth.drops.FirstOrDefault(x=>x.id+1==r.woodDrop);if(b!=null)b.owner=-1;}r.woodReservation=0;r.woodDrop=0;}
        void DropWood(LifeResident r){if(Growth==null||r.woodCargo==0)return;Growth.drops.Add(new WoodDrop{id=Growth.nextDrop++,amount=r.woodCargo,position=r.position,retry=elapsed+8});r.woodCargo=0;}
        void RestoreGrowth(GrowthState state)
        {
            if(growthLayout==null){AutonomousSaveCodec.Require(state==null||state.sources==null,"Unexpected construction state");return;}
            AutonomousSaveCodec.Require(state!=null&&state.sources!=null,"Missing construction state");Growth=JsonUtility.FromJson<GrowthState>(JsonUtility.ToJson(state));
            if(Growth.stage>0){AutonomousSaveCodec.Require(growthLayout.candidates.Any(p=>(p-Growth.site).sqrMagnitude<.00001f),"Unknown construction plot");navigation.ConstructionBlock=GrowthLayout.Footprint(Growth.site);sites.First(s=>s.kind==SiteKind.Build).position=GrowthLayout.Work(Growth.site);}
            else navigation.ConstructionBlock=null;
        }
        void ValidateGrowth()
        {
            Action<bool,string> need=AutonomousSaveCodec.Require;
            if(Growth==null){need(residents.All(r=>r.woodCargo==0&&r.woodReservation==0&&r.woodDrop==0&&(int)r.action<15),"Unexpected wood resident");return;}
            var g=Growth;need(g.stage>=0&&g.stage<=3&&g.sources!=null&&g.sources.Length==2&&g.sources.All(n=>n>=0&&n<=6)&&g.drops!=null&&g.wood>=0&&g.wood<=12,"Invalid growth state");
            need(new[]{g.pressure,g.progress,g.retry}.All(AutonomousSaveCodec.Finite)&&g.pressure>=0&&g.pressure<=60&&g.progress>=0&&g.progress<=60&&g.retry>=0&&AutonomousSaveCodec.Finite(g.site),"Invalid construction progress");
            need(g.spent==(g.stage>=2?12:0)&&(g.stage==3)==(g.progress==60)&&(g.stage>=2||g.progress==0),"Construction cost/progress mismatch");
            need(g.sources.Sum()+g.wood+g.spent+residents.Sum(r=>r.woodCargo)+g.drops.Sum(b=>b.amount)==12,"Wood conservation");
            need(g.nextDrop>=0&&g.drops.Select(b=>b.id).Distinct().Count()==g.drops.Count,"Wood identities");
            foreach(var b in g.drops)need(b.id>=0&&b.id<g.nextDrop&&b.amount>0&&b.amount<=2&&b.owner>=-1&&b.owner<residents.Length&&AutonomousSaveCodec.Finite(b.position)&&AutonomousSaveCodec.Finite(b.retry)&&(b.owner<0||residents[b.owner].woodDrop==b.id+1),"Wood drop ownership");
            foreach(var r in residents){need(r.woodCargo>=0&&r.woodCargo<=2&&!(r.woodCargo>0&&r.cargo>0)&&r.woodReservation>=0&&r.woodReservation<=2&&(r.woodReservation>0)==(r.action==LifeAction.WoodGather),"Wood reservation");need((r.action==LifeAction.WoodRecover)==(r.woodDrop>0)&& (r.woodDrop==0||g.drops.Any(b=>b.id+1==r.woodDrop&&b.owner==r.id)),"Wood recovery ownership");if(r.action==LifeAction.WoodGather)need(sites[r.site].kind==SiteKind.WoodSource&&g.stage==1&&r.woodReservation<=g.sources[sites[r.site].id.EndsWith("0")?0:1],"Wood source reservation");if(r.action==LifeAction.Build)need(sites[r.site].kind==SiteKind.Build&&(g.stage==1||g.stage==2),"Build reservation");if(r.action==LifeAction.WoodDeliver)need(sites[r.site].kind==SiteKind.WoodStore&&r.woodCargo>0,"Wood delivery");}
        }
    }
}
