using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    [Serializable] public sealed class FieldState
    {
        public int stage; // 0 no plot, 1 reserved, 2 working, 3 cultivated
        public float pressure, progress, retry;
        public Vector3 site;
    }

    public sealed class FieldLayout
    {
        public Vector3[] candidates;
        public Vector3 placeholder;
        public const string Prefix="field149-";
        public static Bounds Footprint(Vector3 p)=>new Bounds(p+Vector3.up*.25f,new Vector3(1.8f,.5f,1.2f));
        public static Vector3 Work(Vector3 p)=>p+Vector3.back*1.3f;
        public static Vector3 Crop(Vector3 p,int i)=>p+new Vector3(i==0?-.48f:.48f,0,0);
        public static Vector3 Station(Vector3 p,int i)=>Crop(p,i)+Vector3.forward*1.25f;
        public LifeSite[] Sites()=>new[]{new LifeSite{id=Prefix+"dig",kind=SiteKind.ClearField,position=placeholder},new LifeSite{id=Prefix+"carrot-0",kind=SiteKind.Crop,position=placeholder},new LifeSite{id=Prefix+"carrot-1",kind=SiteKind.Crop,position=placeholder}};

        public static FieldLayout Create(AutonomousLifeWorld w,GrowthLayout warehouse)
        {
            var nav=new AutonomousNavigation(w.land,w.RuntimeObstacles());
            var props=w.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.GetComponentInParent<FarmResidentVisual>()&&!r.transform.name.StartsWith("Recoverable")&&!r.transform.name.StartsWith("Generated149")&&r.bounds.max.y>.10f&&r.bounds.size.x<20&&r.name!="Spring meadow"&&r.name!="Meadow foundation"&&r.name!="Backdrop")
                .Where(r=>!IsGenerated(r.transform)).Select(r=>r.bounds).ToList();
            foreach(var mf in w.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name=="Winding dirt path"||m.name=="Path branch"||m.name=="Resting clearing")){
                var v=mf.sharedMesh.vertices;var t=mf.sharedMesh.triangles;
                for(int i=0;i<t.Length;i+=3){var b=new Bounds(mf.transform.TransformPoint(v[t[i]]),Vector3.zero);b.Encapsulate(mf.transform.TransformPoint(v[t[i+1]]));b.Encapsulate(mf.transform.TransformPoint(v[t[i+2]]));b.center=new Vector3(b.center.x,.5f,b.center.z);b.size=new Vector3(b.size.x,4,b.size.z);props.Add(b);}
            }
            var sites=w.definitions.Concat(AutonomousWeather.Shelters(w)).Concat(PoultryLayout.Sites()).Concat(warehouse.Sites()).ToArray();
            var existing=w.definitions.Where(s=>s.kind==SiteKind.Crop).Select(s=>s.position).ToArray();
            var possible=new List<Vector3>();
            for(float z=w.land.min.z+2;z<w.land.max.z-2;z+=.5f)for(float x=w.land.min.x+2;x<w.land.max.x-2;x+=.5f){
                var p=new Vector3(x,0,z);var b=Footprint(p);b.Expand(new Vector3(.25f,4,.25f));
                if(props.Any(q=>b.Intersects(q))||sites.Any(site=>Mathf.Abs(site.position.x-p.x)<1.5f&&Mathf.Abs(site.position.z-p.z)<1.8f)||warehouse.candidates.Any(q=>b.Intersects(GrowthLayout.Footprint(q))))continue;
                if(new[]{new Vector3(-1.1f,0,-.8f),new Vector3(1.1f,0,-.8f),new Vector3(-1.1f,0,.8f),new Vector3(1.1f,0,.8f)}.Any(o=>!nav.Clear(p+o)))continue;
                possible.Add(p);
            }
            var points=new List<Vector3>();
            foreach(var p in possible.OrderBy(p=>existing.Min(c=>(p-c).sqrMagnitude)).ThenBy(p=>p.z).ThenBy(p=>p.x)){
                nav.FieldBlock=Footprint(p);
                if(nav.Find(w.starts[0],Work(p))!=null&&Enumerable.Range(0,2).All(i=>nav.Find(w.starts[0],Station(p,i))!=null)&&sites.All(site=>nav.Find(w.starts[0],site.position)!=null))points.Add(p);
                nav.FieldBlock=null;
                if(points.Count>=8)break;
            }
            Debug.Log("FIELD149_LAYOUT candidates="+points.Count+" first="+points.FirstOrDefault());
            return new FieldLayout{candidates=points.ToArray(),placeholder=warehouse.store};
        }
        static bool IsGenerated(Transform t){for(;t;t=t.parent)if(t.name.StartsWith("Generated148")||t.name.StartsWith("Generated149"))return true;return false;}
    }

    public sealed partial class AutonomousSimulation
    {
        public FieldState Field {get;private set;}
        FieldLayout fieldLayout;
        public const float FieldPressureSeconds=120,FieldWorkSeconds=60;
        public int FieldTarget=>Math.Min(TargetFood,Capacity);
        // A reserved basket remains in bundles until collected: count it once, not again in PlannedStock.
        int FieldSupply=>food+Committed+ReservedProduction+bundles.Where(b=>residents.Any(r=>navigation.Find(r.position,b.position)!=null)).Sum(b=>b.amount);
        public string FieldStatus=>Field==null?"농지 준비 중":Field.stage==3?"추가 밭 완성 · 당근 자리 2개":Field.stage==2?$"밭 개간 중 {Field.progress/FieldWorkSeconds:P0}":Field.stage==1?"밭 부지 확보 · 개간 준비":!Growth.stockpile?"밭 확장 대기 · 생활 유지":Growth.stage==1||Growth.stage==2?"밭 확장 대기 · 창고 작업 우선":Field.pressure>=FieldPressureSeconds?"공간 부족으로 밭 확장 대기":$"밭 부족 판단 {Field.pressure:F0}/120초";
        public void ConfigureField(FieldLayout layout){fieldLayout=layout;Field=new FieldState();PlaceFieldSites();}
        bool FieldSiteEnabled(LifeSite s)=>!s.id.StartsWith(FieldLayout.Prefix)||(Field!=null&&(s.kind==SiteKind.ClearField?Field.stage==1||Field.stage==2:Field.stage==3));
        void PlaceFieldSites()
        {
            if(Field==null)return;bool active=Field.stage>0;
            sites.First(s=>s.id==FieldLayout.Prefix+"dig").position=active?FieldLayout.Work(Field.site):fieldLayout.placeholder;
            for(int i=0;i<2;i++)sites.First(s=>s.id==FieldLayout.Prefix+"carrot-"+i).position=active?FieldLayout.Station(Field.site,i):fieldLayout.placeholder;
            navigation.FieldBlock=active?(Bounds?)FieldLayout.Footprint(Field.site):null;
        }
        void CancelFieldPlan()
        {
            if(Field==null||Field.stage>=2)return;
            foreach(var r in residents)if(r.action==LifeAction.ClearField)Release(r,false);
            Field.stage=0;Field.pressure=0;Field.retry=0;Field.site=Vector3.zero;PlaceFieldSites();
        }
        void AdvanceField(float dt)
        {
            if(Field==null||Field.stage>=2||Garden!=null&&Garden.stage==2)return;
            if(!Growth.stockpile||FieldSupply>FieldTarget-6){CancelFieldPlan();return;}
            if(Night||Raining)return;
            Field.pressure=Mathf.Min(FieldPressureSeconds,Field.pressure+dt);
            if(Growth.stage==1||Growth.stage==2){if(Field.stage==1)CancelFieldPlan();return;}
            if(Field.stage==1||Field.pressure<FieldPressureSeconds||elapsed<Field.retry)return;
            CancelGardenPlan();Field.retry=elapsed+10;
            foreach(var p in fieldLayout.candidates){
                var old=navigation.FieldBlock;navigation.FieldBlock=FieldLayout.Footprint(p);
                bool valid=residents.All(r=>navigation.Clear(r.position))&&bundles.All(b=>navigation.Clear(b.position))&&Growth.drops.All(b=>navigation.Clear(b.position))&&sites.Where(s=>!s.id.StartsWith(FieldLayout.Prefix)&&GardenSiteEnabled(s)).All(s=>navigation.Find(residents[0].position,s.position)!=null)&&navigation.Find(residents[0].position,FieldLayout.Work(p))!=null&&Enumerable.Range(0,2).All(i=>navigation.Find(residents[0].position,FieldLayout.Station(p,i))!=null);
                if(valid){Field.site=p;Field.stage=1;PlaceFieldSites();break;}navigation.FieldBlock=old;
            }
        }
        void InterruptField(LifeResident r){if(r.action==LifeAction.ClearField&&GrowthEmergency(r))Release(r,false);}
        bool ChooseField(LifeResident r)=>Field!=null&&(Field.stage==1||Field.stage==2)&&Growth.stage!=1&&Growth.stage!=2&&(Garden==null||Garden.stage!=2)&&!GrowthEmergency(r)&&Assign(r,SiteKind.ClearField,LifeAction.ClearField,"먹거리 비축을 늘리기 위해 작은 밭 개간");
        bool CompleteField(LifeResident r,float dt)
        {
            if(r.action!=LifeAction.ClearField)return false;
            Field.stage=2;Field.progress=Mathf.Min(FieldWorkSeconds,Field.progress+dt);r.fatigue=Mathf.Min(1,r.fatigue+dt*tuning.workFatigue);
            if(Field.progress>=FieldWorkSeconds){Field.stage=3;Release(r);}return true;
        }
        void RestoreField(FieldState state)
        {
            if(fieldLayout==null){AutonomousSaveCodec.Require(state==null,"Unexpected field state");return;}
            AutonomousSaveCodec.Require(state!=null,"Missing field state");Field=JsonUtility.FromJson<FieldState>(JsonUtility.ToJson(state));
            if(Field.stage>0)AutonomousSaveCodec.Require(fieldLayout.candidates.Any(p=>(p-Field.site).sqrMagnitude<.00001f),"Unknown field plot");PlaceFieldSites();
        }
        void ValidateField()
        {
            Action<bool,string> need=AutonomousSaveCodec.Require;
            if(Field==null){need(residents.All(r=>r.action!=LifeAction.ClearField),"Unexpected field worker");return;}
            var f=Field;need(f.stage>=0&&f.stage<=3&&new[]{f.pressure,f.progress,f.retry}.All(AutonomousSaveCodec.Finite)&&AutonomousSaveCodec.Finite(f.site)&&f.pressure>=0&&f.pressure<=FieldPressureSeconds&&f.progress>=0&&f.progress<=FieldWorkSeconds&&f.retry>=0,"Invalid field state");
            need((f.stage==3)==(f.progress==FieldWorkSeconds)&&(f.stage>=2||f.progress==0),"Field progress mismatch");
            need(!(f.stage==1||f.stage==2)||!(Growth.stage==1||Growth.stage==2),"Concurrent construction");
            foreach(var s in sites.Where(s=>s.id.StartsWith(FieldLayout.Prefix))){need(s.owner<0||FieldSiteEnabled(s),"Inactive field reserved");if(s.kind==SiteKind.Crop&&f.stage<3)need(!s.planted&&s.growth==0,"Unfinished field growing");}
            foreach(var r in residents.Where(r=>r.action==LifeAction.ClearField))need(r.site>=0&&sites[r.site].kind==SiteKind.ClearField&&(f.stage==1||f.stage==2),"Invalid field worker");
        }
    }
}
