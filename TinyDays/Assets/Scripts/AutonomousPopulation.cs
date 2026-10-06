using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TinyDays.Review;
namespace TinyDays.Life
{
    // Runtime additions keep the authored three-resident scene and its save identity intact.
    public sealed class PopulationLayout
    {
        public LifeSite[] additions;
        public Vector3[] starts;
        public static PopulationLayout Create(AutonomousLifeWorld w,GrowthLayout growth,FieldLayout field)
        {
            var nav=new AutonomousNavigation(w.land,w.RuntimeObstacles());
            var occupied=w.definitions.Concat(AutonomousWeather.Shelters(w)).Concat(PoultryLayout.Sites()).Concat(growth.Sites()).Select(s=>s.position).ToList();
            var additions=new List<LifeSite>();var starts=w.starts.ToList();
            Vector3 Spot(Vector3 near){
                for(float radius=.7f;radius<=4;radius+=.35f)for(int j=0;j<24;j++){
                    var p=near+new Vector3(Mathf.Cos(j*Mathf.PI/12),0,Mathf.Sin(j*Mathf.PI/12))*radius;
                    if(!nav.Clear(p)||occupied.Any(q=>(p-q).sqrMagnitude<.85f*.85f)||growth.candidates.Any(q=>Inside(p,GrowthLayout.Footprint(q)))||field.candidates.Any(q=>Inside(p,FieldLayout.Footprint(q)))||nav.Find(w.starts[0],p)==null)continue;
                    occupied.Add(p);return p;
                }throw new InvalidOperationException("No safe extra home/shelter near "+near);
            }
            for(int i=0;i<3;i++){
                var home=w.definitions.Single(s=>s.kind==SiteKind.Home&&s.homeResident==i);
                var p=Spot(home.position);starts.Add(p);additions.Add(new LifeSite{id="home150-"+(i+3),kind=SiteKind.Home,position=p,homeResident=i+3});
            }
            var shelters=AutonomousWeather.Shelters(w);
            for(int i=0;i<3;i++)additions.Add(new LifeSite{id="shelter150-"+i,kind=SiteKind.Shelter,position=Spot(shelters[i].position)});
            return new PopulationLayout{starts=starts.ToArray(),additions=additions.ToArray()};
        }
        static bool Inside(Vector3 p,Bounds b)=>p.x>b.min.x-.55f&&p.x<b.max.x+.55f&&p.z>b.min.z-.55f&&p.z<b.max.z+.55f;
        public static void EnsureVisuals(AutonomousLifeWorld w)
        {
            var d=w.GetComponent<FarmLifeDirector>();if(d.residents.Length==6)return;
            if(d.residents.Length!=3)throw new InvalidOperationException("Unexpected authored population");
            var actors=d.residents.ToList();var cargo=w.cargo.ToList();
            for(int i=0;i<3;i++){
                var root=UnityEngine.Object.Instantiate(d.residents[i].root,w.transform);root.name="Resident "+(i+4);
                var visual=root.GetComponent<FarmResidentVisual>();visual.animator.gameObject.SetActive(true);
                actors.Add(new FarmLifeDirector.Resident{root=root,visual=visual});cargo.Add(root.Find("Food basket"));
            }
            d.residents=actors.ToArray();w.cargo=cargo.ToArray();
        }
        public static SimulationSaveData Upgrade(AutonomousSimulation old,AutonomousSimulation next)
        {
            var data=old.CaptureSave();var fresh=next.CaptureSave();
            var entries=data.residents.ToList();var sites=data.sites.Concat(fresh.sites.Skip(data.sites.Length)).ToArray();
            for(int i=3;i<6;i++){
                var entry=fresh.residents[i];var r=entry.state;
                // Select an unoccupied reachable spawn without moving any existing actor.
                var near=r.position;bool found=false;
                for(float radius=0;radius<8&&!found;radius+=.35f)for(int j=0;j<24&&!found;j++){
                    var p=near+new Vector3(Mathf.Cos(j*Mathf.PI/12),0,Mathf.Sin(j*Mathf.PI/12))*radius;
                    if(!old.navigation.Clear(p)||entries.Any(e=>(e.state.position-p).sqrMagnitude<.85f*.85f)||old.bundles.Any(b=>(b.position-p).sqrMagnitude<.8f*.8f)||old.Growth.drops.Any(b=>(b.position-p).sqrMagnitude<.8f*.8f)||old.navigation.Find(old.residents[0].position,p)==null)continue;
                    r.position=p;found=true;
                }
                AutonomousSaveCodec.Require(found,"No safe new resident spawn; original save preserved");
                r.hunger=.18f+(i%3)*.08f;r.fatigue=.12f+(i%3)*.09f;r.waitingSince=data.elapsed;
                if(old.Night){int home=Array.FindIndex(next.sites,s=>s.kind==SiteKind.Home&&s.homeResident==i);r.site=home;r.action=LifeAction.Home;r.sleeping=true;r.wait=0;sites[home].owner=i;}
                entries.Add(entry);
            }
            data.residents=entries.ToArray();data.sites=sites;data.garden=new GardenState();return data;
        }
    }
}
