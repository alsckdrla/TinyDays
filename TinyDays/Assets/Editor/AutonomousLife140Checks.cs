using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Life;
using TinyDays.Review;

public static class AutonomousLife140Checks
{
    static readonly List<string> rows=new List<string>();
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static LifeSite Site(string id,SiteKind kind,float x,float z,int home=-1)=>new LifeSite{id=id,kind=kind,position=new Vector3(x,0,z),homeResident=home};
    static AutonomousSimulation Fixture(int food=9,int count=3,bool traits=true)
    {
        var sites=new List<LifeSite>();
        for(int i=0;i<3;i++){
            sites.Add(Site("crop"+i,SiteKind.Crop,-4+i*2,3));
            sites.Add(Site("berry"+i,SiteKind.Berry,-4+i*2,5));
            sites.Add(Site("store"+i,SiteKind.Store,4+i,1));
            sites.Add(Site("rest"+i,SiteKind.Rest,-4+i*2,-4));
            sites.Add(Site("leisure"+i,SiteKind.Leisure,4+i,-4));
            sites.Add(Site("wait"+i,SiteKind.Wait,-4+i*2,-6));
            sites.Add(Site("home"+i,SiteKind.Home,-4+i*2,7,i));
        }
        var tuning=new LifeTuning{initialFood=food,temperamentsEnabled=traits};
        var sim=new AutonomousSimulation(tuning,sites.ToArray(),Enumerable.Range(0,count).Select(i=>new Vector3(-3+i*3,0,-1)).ToArray(),new AutonomousNavigation(new Bounds(Vector3.zero,new Vector3(30,2,30)),Array.Empty<Bounds>()));
        foreach(var r in sim.residents){r.wait=0;r.hunger=0;r.fatigue=0;}
        return sim;
    }
    static void RunFor(AutonomousSimulation sim,float seconds,float hour=12,float dt=.05f)
    {for(int i=0;i<Mathf.CeilToInt(seconds/dt);i++){sim.Tick(dt,hour);sim.Validate();}}
    static void Reservations()
    {
        var full=Fixture(34);full.tuning.targetFood=36;
        foreach(var site in full.sites.Where(s=>s.kind==SiteKind.Crop)){site.planted=true;site.growth=1;}
        full.Tick(.05f,12);full.Validate();
        Check(full.ReservedProduction==2&&full.residents.Count(r=>r.productionReservation>0)==1,"Two free slots must admit one berry job, not three carrots");
        int worker=Array.FindIndex(full.residents,r=>r.productionReservation>0);full.CancelWork(worker);full.Validate();Check(full.ReservedProduction==0,"Cancelled yield not released");
        RunFor(full,60);Check(full.food<=36&&full.produced==2,"Concurrent capacity exceeded");
        var last=Fixture(1);foreach(var r in last.residents)r.hunger=.9f;last.residents[2].waitingSince=-10;
        last.Tick(.05f,12);last.Validate();
        Check(last.residents.Count(r=>r.mealReservation==1)==1&&last.residents[2].mealReservation==1&&last.AvailableFood==0,"Last meal admission/oldest-first failed");
        last.CancelWork(2);Check(last.AvailableFood==1,"Cancelled meal not returned");last.Validate();
        RunFor(last,60);Check(last.consumed>=1,"Meal recovery failed");
        var empty=Fixture(0);foreach(var r in empty.residents)r.hunger=.9f;RunFor(empty,60);
        Check(empty.produced>0&&empty.consumed>=3,"Empty food did not recover for all three residents");
        rows.Add("Concurrent capacity (34/36), last meal/cancel/oldest-first, empty food/all residents fed: PASS");
    }
    static void Traits()
    {
        foreach(var trait in new[]{LifeTemperament.Farmer,LifeTemperament.Forager,LifeTemperament.Relaxed}){
            var sim=Fixture(9,1);sim.residents[0].temperament=trait;sim.Tick(.05f,12);
            Check(sim.residents[0].action==(trait==LifeTemperament.Forager?LifeAction.Gather:LifeAction.Plant),"Preferred task failed "+trait);
            var urgent=Fixture(0,1);urgent.residents[0].temperament=trait;urgent.Tick(.05f,12);Check(urgent.residents[0].action==LifeAction.Gather,"Urgency lost to taste "+trait);
            var tired=Fixture(9,1);tired.residents[0].temperament=trait;tired.residents[0].fatigue=.65f;tired.Tick(.05f,12);
            Check((tired.residents[0].action==LifeAction.Rest)==(trait==LifeTemperament.Relaxed),"Relaxed rest threshold failed");
            Check(Mathf.Abs(tired.LeisureDuration(tired.residents[0])-(trait==LifeTemperament.Relaxed?15:10))<.001f,"Leisure duration failed");
        }
        var control=Fixture(9,1,false);control.Tick(.05f,12);Check(control.residents[0].temperament==LifeTemperament.Balanced&&control.residents[0].action==LifeAction.Plant,"Traits-off control failed");
        rows.Add("Same-needs traits, urgent override, rest -0.1, leisure x1.5, traits-off control: PASS");
    }
    static void Contention()
    {
        var baseSim=Fixture(9);
        var sites=baseSim.sites.Where(s=>s.kind!=SiteKind.Store||s.id=="store0").ToArray();
        var sim=new AutonomousSimulation(new LifeTuning(),sites,baseSim.residents.Select(r=>r.position).ToArray(),baseSim.navigation);
        foreach(var r in sim.residents){r.wait=0;r.hunger=.9f;r.fatigue=0;}sim.residents[2].waitingSince=-5;
        RunFor(sim,90);Check(sim.residents.All(r=>r.hunger<.6f)&&sim.consumed==3,"Shared meal station starvation");
        var blocked=Fixture(9);blocked.tuning.targetFood=9;foreach(var r in blocked.residents){r.cargo=1;r.wait=0;}blocked.produced=3;
        blocked.navigation.TemporaryBlock=new Bounds(new Vector3(5,0,1),new Vector3(4,2,1));
        RunFor(blocked,2);Check(blocked.bundles.Sum(b=>b.amount)==3,"Blocked delivery must leave visible recoverable stock");RunFor(blocked,13);
        blocked.navigation.TemporaryBlock=null;float[] distances=blocked.residents.Select(r=>r.walkDistance).ToArray();RunFor(blocked,30);
        Check(blocked.residents.All(r=>r.walkDistance>distances[r.id]),"Delivery unblock did not restart within 30s");RunFor(blocked,60);
        Check(blocked.bundles.Count==0&&blocked.residents.All(r=>r.cargo==0),"Delivery not recovered within 90s");
        var homes=Fixture();homes.navigation.TemporaryBlock=new Bounds(new Vector3(-2,0,7),new Vector3(8,2,1));RunFor(homes,15,22);
        Check(homes.homeArrivals==0,"Blocked home teleport");homes.navigation.TemporaryBlock=null;distances=homes.residents.Select(r=>r.walkDistance).ToArray();RunFor(homes,30,22);
        Check(homes.residents.All(r=>r.walkDistance>distances[r.id]),"Home unblock did not restart within 30s");RunFor(homes,60,22);Check(homes.homeArrivals==3,"Home recovery exceeded 90s");
        rows.Add("Single station/3 residents, blocked store and homes: each restarts <=30s and completes <=90s: PASS");
        // A narrow passage with open space at both ends; residents must yield, never reduce clearance.
        var nav=new AutonomousNavigation(new Bounds(Vector3.zero,new Vector3(24,2,24)),new[]{new Bounds(new Vector3(0,0,-2),new Vector3(4,2,2.6f)),new Bounds(new Vector3(0,0,2),new Vector3(4,2,2.6f))});
        var crossing=new AutonomousSimulation(new LifeTuning(),new[]{Site("home0",SiteKind.Home,4,0,0),Site("home1",SiteKind.Home,-4,0,1),Site("home2",SiteKind.Home,4,2,2)},new[]{new Vector3(-4,0,0),new Vector3(4,0,0),new Vector3(-4,0,2)},nav);
        foreach(var r in crossing.residents){r.wait=0;r.hunger=0;}RunFor(crossing,90,22);
        Check(crossing.homeArrivals==3,"Narrow passage failed to resolve: "+string.Join("; ",crossing.residents.Select(r=>$"{r.id} {r.position} {r.action} moving={r.Moving} blocked={r.blockedSeconds}")));rows.Add("Three residents / opposite narrow passage / >=0.595m validation / 90s completion: PASS");
    }
    static void SceneChecks()
    {
        EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();
        foreach(int fps in new[]{30,60,120})foreach(float rate in new[]{.5f,1,4}){
            var sim=new AutonomousSimulation(new LifeTuning(),world.definitions,world.starts,new AutonomousNavigation(world.land,world.obstacles));float dt=rate/fps;
            for(int i=0;i<Mathf.CeilToInt(3600/dt);i++){sim.Tick(dt,Mathf.Repeat(12+i*dt*24/600,24));if(i%fps==0)sim.Validate();}
            sim.Validate();Check(sim.consumed>=18&&sim.homeArrivals>=18&&sim.harvested>0&&sim.gathered>0,"Six-day loop incomplete");
            rows.Add($"6 days {fps}fps {rate}x: food={sim.food} produced={sim.produced} eaten={sim.consumed} homes={sim.homeArrivals} recoveries={sim.recovered}: PASS");
        }
        world.Initialize();var adapter=world.GetComponent<FarmLifeDirector>();var clock=world.GetComponent<FarmLightingStudy>();
        world.Advance(1);world.Simulation.Validate();adapter.paused=true;float before=world.Simulation.elapsed;int reserved=world.Simulation.ReservedProduction;world.Advance(10);
        Check(world.Simulation.elapsed==before&&world.Simulation.ReservedProduction==reserved,"Pause changed reservations");
        adapter.paused=false;adapter.Playback.SetRate(3);world.Advance(2);Check(Mathf.Abs(world.Simulation.elapsed-before-8)<.02f,"Shared 4x rate failed");
        world.Simulation.Validate();world.Restart();Check(world.Simulation.ReservedProduction==0&&world.Simulation.AvailableFood==9&&clock.Day==1,"Reset left reservations");
        var carrier=world.Simulation.residents[0];carrier.cargo=2;world.Simulation.produced+=2;world.Simulation.CancelWork(0);world.Present();
        Check(world.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Recoverable food basket")),"Dropped food has no scene representation");world.Simulation.Validate();world.Restart();
        rows.Add("Shared clock/rate/pause/reset reservations, visible dropped basket: PASS");
        var manual=GameObject.Find("ManualEdits");var probe=new GameObject("Life140ManualProbe");probe.transform.SetParent(manual.transform);probe.transform.localPosition=new Vector3(2,3,4);
        AutonomousLifeBuilder.Build();AutonomousLifeBuilder.Build();probe=GameObject.Find("Life140ManualProbe");Check(probe&&probe.transform.localPosition==new Vector3(2,3,4),"Manual edits not preserved");
        UnityEngine.Object.DestroyImmediate(probe);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());rows.Add("Two regenerations preserve unsaved ManualEdits: PASS");
    }
    public static void Run()
    {
        rows.Clear();rows.Add("v0.140 automated checks; not OS input, wall-clock observation, or user approval.");
        try{Reservations();Traits();Contention();SceneChecks();File.WriteAllLines("Docs/AutonomousLife140Verification.txt",rows);Debug.Log("LIFE140_VERIFY_OK\n"+string.Join("\n",rows));}
        catch(Exception e){Debug.Log(string.Join("\n",rows));Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    public static void Execute(){AutonomousLifeBuilder.Build();Run();AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE140_COMPLETE_OK");}
    public static void Supplementary()
    {
        try{
            var night=Fixture(0);night.Tick(.05f,19.99f);Check(night.ReservedProduction>0,"No pending work at dusk");RunFor(night,90,22);
            Check(night.homeArrivals==3&&night.ReservedProduction==0&&night.Committed==0,"Dusk work/cargo did not settle before home");
            var collect=Fixture(9,1);collect.tuning.targetFood=0;collect.residents[0].cargo=3;collect.produced=3;collect.CancelWork(0);
            for(int i=0;i<1600&&collect.residents[0].collectionReservation==0;i++)collect.Tick(.05f,12);
            Check(collect.residents[0].collectionReservation==3,"Missing recovery reservation");collect.CancelWork(0);collect.Validate();
            Check(collect.ReservedCollections==0&&collect.bundles.Single().owner==-1&&collect.bundles.Single().amount==3,"Recovery cancel lost ownership/stock");
            var traffic=new AutonomousSimulation(new LifeTuning(),new[]{Site("store",SiteKind.Store,4,0),Site("rest",SiteKind.Rest,-4,0)},new[]{new Vector3(-1,0,0),new Vector3(1,0,0)},new AutonomousNavigation(new Bounds(Vector3.zero,new Vector3(20,2,20)),Array.Empty<Bounds>()));
            for(int i=0;i<2;i++){var r=traffic.residents[i];r.wait=0;r.hunger=0;r.fatigue=0;r.site=i;traffic.sites[i].owner=i;r.action=i==0?LifeAction.Deliver:LifeAction.Rest;r.path=new List<Vector3>{traffic.sites[i].position};}
            traffic.residents[0].cargo=1;traffic.produced=1;traffic.residents[1].waitingSince=-100;
            int firstCross=-1;
            for(int i=0;i<1200;i++){traffic.Tick(.05f,12);traffic.Validate();if(firstCross<0){if(traffic.residents[0].position.x>=0)firstCross=0;else if(traffic.residents[1].position.x<=0)firstCross=1;}}
            Check(firstCross==0,"Carrier did not receive right of way over older non-carrier");
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();var nav=new AutonomousNavigation(world.land,world.obstacles);
            Check(world.definitions.All(s=>nav.Clear(s.position)),"Final scene contains inaccessible station");
            File.WriteAllLines("Docs/AutonomousLife140Supplementary.txt",new[]{"v0.140 focused regression checks", "Dusk with reserved work -> delivery -> all home within 90s: PASS", "Cancel recovery -> release owner/capacity, preserve quantity: PASS", "Carrier yields less than older non-carrier in head-on encounter: PASS", "All scene stations clear after final generated obstacles: PASS"});
            Debug.Log("LIFE140_SUPPLEMENTARY_OK");
        }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
}
