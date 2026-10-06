using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    [Serializable] public sealed class GardenState
    {
        public int stage;
        public float stability,progress,retry;
        public Vector3 site;
    }
    public sealed class GardenLayout
    {
        public const string Prefix="garden150-";
        public Vector3[] candidates;
        public Vector3 placeholder;
        public static Bounds Footprint(Vector3 p)=>new Bounds(p+Vector3.up*.4f,new Vector3(2.4f,.8f,1.4f));
        public static Vector3 Work(Vector3 p)=>p+Vector3.back*1.45f;
        // Seats are approach/relaxation stations; temporary standing idle is retained.
        public static Vector3 Seat(Vector3 p,int i)=>p+new Vector3(i==0?-.55f:.55f,0,-1.3f);
        public LifeSite[] Sites()=>new[]{new LifeSite{id=Prefix+"work",kind=SiteKind.Garden,position=placeholder},new LifeSite{id=Prefix+"rest-0",kind=SiteKind.Rest,position=placeholder},new LifeSite{id=Prefix+"rest-1",kind=SiteKind.Rest,position=placeholder}};
        public static GardenLayout Create(AutonomousLifeWorld w,GrowthLayout growth,FieldLayout field,LifeSite[] extra)
        {
            var nav=new AutonomousNavigation(w.land,w.RuntimeObstacles());
            var props=w.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.GetComponentInParent<FarmResidentVisual>()&&!Generated(r.transform)&&r.bounds.max.y>.1f&&r.bounds.size.x<20&&r.name!="Spring meadow"&&r.name!="Meadow foundation"&&r.name!="Backdrop").Select(r=>r.bounds).ToList();
            foreach(var mf in w.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name=="Winding dirt path"||m.name=="Path branch"||m.name=="Resting clearing")){
                var v=mf.sharedMesh.vertices;var t=mf.sharedMesh.triangles;
                for(int i=0;i<t.Length;i+=3){var b=new Bounds(mf.transform.TransformPoint(v[t[i]]),Vector3.zero);b.Encapsulate(mf.transform.TransformPoint(v[t[i+1]]));b.Encapsulate(mf.transform.TransformPoint(v[t[i+2]]));b.center=new Vector3(b.center.x,.5f,b.center.z);b.size=new Vector3(b.size.x,4,b.size.z);props.Add(b);}
            }
            var sites=w.definitions.Concat(AutonomousWeather.Shelters(w)).Concat(PoultryLayout.Sites()).Concat(growth.Sites()).Concat(extra).ToArray();
            var near=w.definitions.Where(s=>s.kind==SiteKind.Rest).Select(s=>s.position).ToArray();
            var possible=new List<Vector3>();
            for(float z=w.land.min.z+2;z<w.land.max.z-2;z+=.5f)for(float x=w.land.min.x+2;x<w.land.max.x-2;x+=.5f){
                var p=new Vector3(x,0,z);var b=Footprint(p);b.Expand(new Vector3(.3f,4,.3f));
                if(props.Any(q=>b.Intersects(q))||sites.Any(s=>Mathf.Abs(s.position.x-x)<1.9f&&Mathf.Abs(s.position.z-z)<2)||growth.candidates.Any(q=>b.Intersects(GrowthLayout.Footprint(q)))||field.candidates.Any(q=>b.Intersects(FieldLayout.Footprint(q))))continue;
                if(new[]{new Vector3(-1.4f,0,-.9f),new Vector3(1.4f,0,-.9f),new Vector3(-1.4f,0,.9f),new Vector3(1.4f,0,.9f)}.Any(o=>!nav.Clear(p+o)))continue;
                possible.Add(p);
            }
            var points=new List<Vector3>();
            foreach(var p in possible.OrderBy(p=>near.Min(q=>(p-q).sqrMagnitude)).ThenBy(p=>p.z).ThenBy(p=>p.x)){
                nav.GardenBlock=Footprint(p);
                if(nav.Find(w.starts[0],Work(p))!=null&&Enumerable.Range(0,2).All(i=>nav.Find(w.starts[0],Seat(p,i))!=null)&&sites.All(s=>nav.Find(w.starts[0],s.position)!=null))points.Add(p);
                nav.GardenBlock=null;if(points.Count>=8)break;
            }
            Debug.Log("GARDEN150_LAYOUT candidates="+points.Count+" first="+points.FirstOrDefault());
            return new GardenLayout{candidates=points.ToArray(),placeholder=growth.store};
        }
        static bool Generated(Transform t){for(;t;t=t.parent)if(t.name.StartsWith("Generated14")||t.name.StartsWith("Generated150"))return true;return false;}
    }
    public sealed partial class AutonomousSimulation
    {
        public GardenState Garden {get;private set;}
        GardenLayout gardenLayout;
        public const float GardenStabilitySeconds=120,GardenWorkSeconds=60;
        bool ProductionConstruction=>Growth!=null&&(Growth.stage==1||Growth.stage==2||Growth.stockpile&&Growth.stage==0&&Growth.pressure>=60)||Field!=null&&(Field.stage==1||Field.stage==2||Growth.stockpile&&Field.stage==0&&Field.pressure>=FieldPressureSeconds);
        public string GardenStatus=>Garden==null?"정원 준비 중":Garden.stage==3?"꽃밭·2인 쉼터 완성":Garden.stage==2?$"정원 조성 중 {Garden.progress/GardenWorkSeconds:P0}":Garden.stage==1?"정원 부지 확보 · 조성 준비":ProductionConstruction?"정원 대기 · 생산시설 우선":Garden.stability>=GardenStabilitySeconds?"공간 부족으로 정원 조성 대기":$"생활 안정 {Garden.stability:F0}/120초 · 식량 12 이상";
        public void ConfigureGarden(GardenLayout layout){gardenLayout=layout;Garden=new GardenState();PlaceGardenSites();}
        bool GardenSiteEnabled(LifeSite s)=>!s.id.StartsWith(GardenLayout.Prefix)||Garden!=null&&(s.kind==SiteKind.Garden?Garden.stage==1||Garden.stage==2:Garden.stage==3);
        void PlaceGardenSites()
        {
            if(Garden==null)return;bool active=Garden.stage>0;
            sites.First(s=>s.id==GardenLayout.Prefix+"work").position=active?GardenLayout.Work(Garden.site):gardenLayout.placeholder;
            for(int i=0;i<2;i++)sites.First(s=>s.id==GardenLayout.Prefix+"rest-"+i).position=active?GardenLayout.Seat(Garden.site,i):gardenLayout.placeholder;
            navigation.GardenBlock=active?(Bounds?)GardenLayout.Footprint(Garden.site):null;
        }
        void CancelGardenPlan()
        {
            if(Garden==null||Garden.stage>=2)return;
            foreach(var r in residents)if(r.action==LifeAction.Garden)Release(r,false);
            Garden.stage=0;Garden.stability=0;Garden.retry=0;Garden.site=Vector3.zero;PlaceGardenSites();
        }
        void AdvanceGarden(float dt)
        {
            if(Garden==null||Garden.stage>=2)return;
            if(AvailableFood<12||ProductionConstruction){CancelGardenPlan();return;}
            if(Night||Raining)return;
            Garden.stability=Mathf.Min(GardenStabilitySeconds,Garden.stability+dt);
            if(Garden.stage==1||Garden.stability<GardenStabilitySeconds||elapsed<Garden.retry)return;
            Garden.retry=elapsed+10;
            foreach(var p in gardenLayout.candidates){
                var old=navigation.GardenBlock;navigation.GardenBlock=GardenLayout.Footprint(p);
                bool valid=residents.All(r=>navigation.Clear(r.position))&&bundles.All(b=>navigation.Clear(b.position))&&Growth.drops.All(b=>navigation.Clear(b.position))&&sites.Where(s=>!s.id.StartsWith(GardenLayout.Prefix)&&FieldSiteEnabled(s)).All(s=>navigation.Find(residents[0].position,s.position)!=null)&&navigation.Find(residents[0].position,GardenLayout.Work(p))!=null&&Enumerable.Range(0,2).All(i=>navigation.Find(residents[0].position,GardenLayout.Seat(p,i))!=null);
                if(valid){Garden.site=p;Garden.stage=1;PlaceGardenSites();break;}navigation.GardenBlock=old;
            }
        }
        void InterruptGarden(LifeResident r){if(r.action==LifeAction.Garden&&GrowthEmergency(r))Release(r,false);}
        bool ChooseGarden(LifeResident r)=>Garden!=null&&(Garden.stage==1||Garden.stage==2)&&(Garden.stage==2||AvailableFood>=12)&&!ProductionConstruction&&!GrowthEmergency(r)&&Assign(r,SiteKind.Garden,LifeAction.Garden,"생활에 여유가 생겨 꽃밭과 쉼터 조성");
        bool CompleteGarden(LifeResident r,float dt)
        {
            if(r.action!=LifeAction.Garden)return false;
            if(Garden.stage==1&&(AvailableFood<12||ProductionConstruction)){CancelGardenPlan();return true;}
            Garden.stage=2;Garden.progress=Mathf.Min(GardenWorkSeconds,Garden.progress+dt);r.fatigue=Mathf.Min(1,r.fatigue+dt*tuning.workFatigue);
            if(Garden.progress>=GardenWorkSeconds){Garden.stage=3;Release(r);}return true;
        }
        void RestoreGarden(GardenState state)
        {
            if(gardenLayout==null){AutonomousSaveCodec.Require(state==null,"Unexpected garden");return;}
            AutonomousSaveCodec.Require(state!=null,"Missing garden");Garden=JsonUtility.FromJson<GardenState>(JsonUtility.ToJson(state));
            if(Garden.stage>0)AutonomousSaveCodec.Require(gardenLayout.candidates.Any(p=>(p-Garden.site).sqrMagnitude<.00001f),"Unknown garden plot");PlaceGardenSites();
        }
        void ValidateGarden()
        {
            var g=Garden;if(g==null){AutonomousSaveCodec.Require(residents.All(r=>r.action!=LifeAction.Garden),"Unexpected gardener");return;}
            Action<bool,string> need=AutonomousSaveCodec.Require;
            need(g.stage>=0&&g.stage<=3&&new[]{g.stability,g.progress,g.retry}.All(AutonomousSaveCodec.Finite)&&AutonomousSaveCodec.Finite(g.site)&&g.stability>=0&&g.stability<=GardenStabilitySeconds&&g.progress>=0&&g.progress<=GardenWorkSeconds&&g.retry>=0,"Invalid garden state");
            need((g.stage==3)==(g.progress==GardenWorkSeconds)&&(g.stage>=2||g.progress==0),"Garden progress mismatch");
            need(g.stage!=1&&g.stage!=2||Growth.stage!=1&&Growth.stage!=2&&Field.stage!=1&&Field.stage!=2,"Concurrent garden construction");
            foreach(var s in sites.Where(s=>s.id.StartsWith(GardenLayout.Prefix)))need(s.owner<0||GardenSiteEnabled(s),"Inactive garden reserved");
            foreach(var r in residents.Where(r=>r.action==LifeAction.Garden))need(r.site>=0&&sites[r.site].kind==SiteKind.Garden&&(g.stage==1||g.stage==2),"Invalid gardener");
        }
    }
}
