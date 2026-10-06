using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    [DefaultExecutionOrder(100)]
    public sealed class AutonomousLifeWorld : MonoBehaviour
    {
        public LifeTuning tuning=new LifeTuning();
        public LifeSite[] definitions;
        public Bounds land;
        public Bounds[] obstacles;
        public Vector3[] starts;
        public Transform[] plants,berries,cargo;
        public Transform storageStock;
        public AutonomousSimulation Simulation {get;private set;}
        public AutonomousWeather Weather {get;private set;}=new AutonomousWeather();
        GrowthLayout growthLayout;
        AutonomousGrowthVisual growthVisual;
        FieldLayout fieldLayout;AutonomousFieldVisual fieldVisual;
        FieldLayout FieldLayoutData=>fieldLayout??(fieldLayout=FieldLayout.Create(this,Layout));
        GrowthLayout Layout=>growthLayout??(growthLayout=GrowthLayout.Create(this));
        LifeSite[] Legacy147Sites()=>definitions.Concat(AutonomousWeather.Shelters(this)).Concat(PoultryLayout.Sites()).ToArray();
        LifeSite[] Legacy148Sites()=>Legacy147Sites().Concat(Layout.Sites()).ToArray();
        LifeSite[] Legacy149Sites()=>Legacy148Sites().Concat(FieldLayoutData.Sites()).ToArray();
        PopulationLayout population;GardenLayout gardenLayout;AutonomousGardenVisual gardenVisual;
        PopulationLayout Population=>population??(population=PopulationLayout.Create(this,Layout,FieldLayoutData));
        GardenLayout GardenLayoutData=>gardenLayout??(gardenLayout=GardenLayout.Create(this,Layout,FieldLayoutData,Population.additions));
        LifeSite[] RuntimeSites()=>Legacy149Sites().Concat(Population.additions).Concat(GardenLayoutData.Sites()).ToArray();
        AutonomousSimulation NewSimulation(){var s=new AutonomousSimulation(tuning,RuntimeSites(),Population.starts,new AutonomousNavigation(land,RuntimeObstacles()));s.ConfigureGrowth(Layout);s.ConfigureField(FieldLayoutData);s.ConfigureGarden(GardenLayoutData);return s;}
        public Bounds[] RuntimeObstacles()=>obstacles.Concat(new[]{PoultryLayout.Area}).ToArray();
        AutonomousPoultryVisual poultryVisual;
        AutonomousWeatherVisual weatherVisual;
        AutonomousSeasonVisual seasonVisual;
        public AutonomousSeason Season {get;private set;}=new AutonomousSeason();
        public string WeatherLabel=>Season.Winter&&Weather.Kind==LifeWeather.Rain?"눈":Weather.Label;
        void SyncSeason(){Simulation.Winter=Season.Winter;Simulation.Raining=Weather.Kind==LifeWeather.Rain;if(seasonVisual)seasonVisual.Apply(Season.Weights);}
        FarmLifeDirector adapter;FarmLightingStudy lighting;
        float sampleTime,wallStart,nextLog=60;
        bool observing;
        bool observing140;
        readonly Dictionary<int,GameObject> groundBaskets=new Dictionary<int,GameObject>();
        public void Initialize()
        {
            foreach(var basket in groundBaskets.Values)if(basket)RemoveBasket(basket);
            groundBaskets.Clear();
            adapter=GetComponent<FarmLifeDirector>();lighting=GetComponent<FarmLightingStudy>();
            adapter.enabled=false;lighting.enabled=false;adapter.Playback.SetDayMinutes("10");
            Weather=new AutonomousWeather();Season=new AutonomousSeason();
            Simulation=NewSimulation();PopulationLayout.EnsureVisuals(this);
            if(Application.isPlaying){gardenVisual=GetComponent<AutonomousGardenVisual>();if(!gardenVisual)gardenVisual=gameObject.AddComponent<AutonomousGardenVisual>();gardenVisual.Configure();fieldVisual=GetComponent<AutonomousFieldVisual>();if(!fieldVisual)fieldVisual=gameObject.AddComponent<AutonomousFieldVisual>();fieldVisual.Configure(this);growthVisual=GetComponent<AutonomousGrowthVisual>();if(!growthVisual)growthVisual=gameObject.AddComponent<AutonomousGrowthVisual>();growthVisual.Configure(this,Layout);poultryVisual=GetComponent<AutonomousPoultryVisual>();if(!poultryVisual)poultryVisual=gameObject.AddComponent<AutonomousPoultryVisual>();poultryVisual.Configure(this);weatherVisual=GetComponent<AutonomousWeatherVisual>();if(!weatherVisual)weatherVisual=gameObject.AddComponent<AutonomousWeatherVisual>();weatherVisual.Configure(this);seasonVisual=GetComponent<AutonomousSeasonVisual>();if(!seasonVisual)seasonVisual=gameObject.AddComponent<AutonomousSeasonVisual>();seasonVisual.Configure();seasonVisual.Apply(Season.Weights);}
            adapter.elapsed=0;sampleTime=0;lighting.RestartClock();Present();
        }
        void Start(){Application.runInBackground=true;Application.targetFrameRate=60;lighting=GetComponent<FarmLightingStudy>();lighting.LoadPreferences();Initialize();observing140=Array.IndexOf(Environment.GetCommandLineArgs(),"-observeLife140")>=0;observing=observing140||Array.IndexOf(Environment.GetCommandLineArgs(),"-observeLife139")>=0;wallStart=Time.realtimeSinceStartup;
            var args=Environment.GetCommandLineArgs();int probe=Array.IndexOf(args,"-verifySave142"),directory=Array.IndexOf(args,"-lifeSaveDirectory");
            if((probe>=0||Array.IndexOf(args,"-observeLife143")>=0||Array.IndexOf(args,"-observeLife144")>=0||Array.IndexOf(args,"-observeLife145")>=0||Array.IndexOf(args,"-observeLife147")>=0||Array.IndexOf(args,"-observeLife148")>=0||Array.IndexOf(args,"-observeLife149")>=0||Array.IndexOf(args,"-observeLife150")>=0)&&(directory<0||directory+1>=args.Length||args[directory+1].StartsWith("-"))){Debug.LogError("LIFE142_PROBE_REQUIRES_ISOLATED_DIRECTORY");Application.Quit(1);return;}
            if(!observing){var saving=GetComponent<AutonomousLifeSave>();if(!saving)saving=gameObject.AddComponent<AutonomousLifeSave>();saving.Configure(this);}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-verifySave142")>=0)gameObject.AddComponent<AutonomousSavePlayerProbe>();
            if(Array.IndexOf(args,"-observeLife143")>=0)gameObject.AddComponent<AutonomousLife143Observation>();
            if(Array.IndexOf(args,"-observeLife144")>=0)gameObject.AddComponent<AutonomousLife144Observation>();
            if(Array.IndexOf(args,"-observeLife145")>=0)gameObject.AddComponent<AutonomousLife145Observation>();
            if(Array.IndexOf(args,"-observeLife147")>=0)gameObject.AddComponent<AutonomousLife147Observation>();
            if(Array.IndexOf(args,"-observeLife148")>=0)gameObject.AddComponent<AutonomousLife148Observation>();
            if(Array.IndexOf(args,"-observeLife149")>=0)gameObject.AddComponent<AutonomousLife149Observation>();
            if(Array.IndexOf(args,"-observeLife150")>=0)gameObject.AddComponent<AutonomousLife150Observation>();
        }
        public void Restart(){bool paused=adapter.paused;float rate=adapter.Playback.Rate;int day=adapter.Playback.DayMinutes;Initialize();adapter.Playback.SetDayMinutes(day.ToString());adapter.Playback.SetRate(Array.IndexOf(FarmPlaybackSettings.Rates,rate));adapter.paused=paused;}
        public AutonomousSaveData CaptureSnapshot()
        {
            if(Simulation==null)throw new InvalidOperationException("World not initialized");
            return new AutonomousSaveData{scene=AutonomousSaveCodec.SceneIdentity(this),simulation=Simulation.CaptureSave(),sampleTime=sampleTime,
                hour=lighting.Hour,day=lighting.Day,automatic=lighting.Automatic,dayMinutes=adapter.Playback.DayMinutes,rate=adapter.Playback.Rate,paused=adapter.paused,
                camera=GetComponent<FarmStudyReview>().CaptureView(),weather=Weather.Capture(),season=Season.Capture()};
        }
        public AutonomousSimulation ValidateSnapshot(AutonomousSaveData data)
        {
            AutonomousSaveCodec.Require(data!=null&&data.scene==AutonomousSaveCodec.SceneIdentity(this),"Save scene/tuning mismatch");
            AutonomousSaveCodec.Require(data.day>=1&&data.dayMinutes>=1&&data.dayMinutes<=120&&Array.IndexOf(FarmPlaybackSettings.Rates,data.rate)>=0&&AutonomousSaveCodec.Finite(data.hour)&&data.hour>=0&&data.hour<24&&AutonomousSaveCodec.Finite(data.sampleTime)&&data.sampleTime>=0,"Invalid saved clock");
            FarmStudyReview.ValidateView(data.camera,data.sourceVersion>=150?6:starts.Length);
            AutonomousSaveCodec.Require(data.sourceVersion==142||data.sourceVersion==144||data.sourceVersion==145||data.sourceVersion==146||data.sourceVersion==147||data.sourceVersion==148||data.sourceVersion==149||data.sourceVersion==150,"Invalid source version");
            if(data.sourceVersion>=145)AutonomousSeason.Validate(data.season);
            var state=JsonUtility.FromJson<SimulationSaveData>(JsonUtility.ToJson(data.simulation));
            if(data.sourceVersion<149)state.field=null;if(data.sourceVersion<150)state.garden=null;
            if(data.sourceVersion<146&&state?.residents!=null){foreach(var old in state.residents)if(old?.state!=null){old.state.socialPartner=0;old.state.socialTime=old.state.socialReadyAt=0;}}
            if(data.sourceVersion==142){
                AutonomousSaveCodec.Require(state!=null&&state.residents!=null&&state.residents.All(r=>r!=null&&r.state!=null&&(int)r.state.action<=10),"Invalid legacy action");
                var legacy=new AutonomousSimulation(tuning,definitions,starts,new AutonomousNavigation(land,obstacles));state.growth=null;legacy.RestoreSave(state);
                state=legacy.CaptureSave();state.sites=state.sites.Concat(AutonomousWeather.Shelters(this).Select(s=>new SiteSaveData{id=s.id,owner=-1})).ToArray();
            }else AutonomousWeather.Validate(data.weather);
            if(data.sourceVersion<147){
                var legacy=new AutonomousSimulation(tuning,definitions.Concat(AutonomousWeather.Shelters(this)).ToArray(),starts,new AutonomousNavigation(land,obstacles));state.growth=null;legacy.RestoreSave(state);
                state=legacy.CaptureSave();state.sites=state.sites.Concat(PoultryLayout.Sites().Select(s=>new SiteSaveData{id=s.id,owner=-1})).ToArray();state.poultry=PoultryState.Create();
            }
            if(data.sourceVersion<148){
                var legacy=new AutonomousSimulation(tuning,Legacy147Sites(),starts,new AutonomousNavigation(land,RuntimeObstacles()));state.growth=null;legacy.RestoreSave(state);
                state=legacy.CaptureSave();state.sites=state.sites.Concat(Layout.Sites().Select(s=>new SiteSaveData{id=s.id,owner=-1})).ToArray();state.growth=new GrowthState();
            }
            if(data.sourceVersion<149){
                var legacy=new AutonomousSimulation(tuning,Legacy148Sites(),starts,new AutonomousNavigation(land,RuntimeObstacles()));legacy.ConfigureGrowth(Layout);legacy.RestoreSave(state);
                state=legacy.CaptureSave();state.sites=state.sites.Concat(FieldLayoutData.Sites().Select(s=>new SiteSaveData{id=s.id,owner=-1})).ToArray();state.field=new FieldState();
            }
            var candidate=NewSimulation();
            if(data.sourceVersion<150){var legacy=new AutonomousSimulation(tuning,Legacy149Sites(),starts,new AutonomousNavigation(land,RuntimeObstacles()));legacy.ConfigureGrowth(Layout);legacy.ConfigureField(FieldLayoutData);legacy.RestoreSave(state);state=PopulationLayout.Upgrade(legacy,candidate);}
            candidate.RestoreSave(state);candidate.DayMinutes=data.dayMinutes;candidate.Raining=data.sourceVersion!=142&&data.weather.phase==2;candidate.Winter=data.sourceVersion>=145&&data.season.season==3;return candidate;
        }
        public void RestoreSnapshot(AutonomousSaveData data)
        {
            // Validate into a separate simulation before changing the active world.
            var candidate=ValidateSnapshot(data);
            Simulation=candidate;sampleTime=data.sampleTime;adapter.elapsed=candidate.elapsed;
            Weather=new AutonomousWeather();if(data.sourceVersion!=142)Weather.Restore(data.weather);
            Season=new AutonomousSeason();if(data.sourceVersion>=145)Season.Restore(data.season);SyncSeason();
            adapter.Playback.SetDayMinutes(data.dayMinutes.ToString());adapter.Playback.SetRate(Array.IndexOf(FarmPlaybackSettings.Rates,data.rate));adapter.paused=data.paused;
            lighting.RestoreClock(data.day,data.hour,data.automatic);Present();GetComponent<FarmStudyReview>().RestoreView(data.camera);
        }
        public void Advance(float realSeconds)
        {
            if(Simulation==null)Initialize();
            if(adapter.paused)return;
            float remaining=Mathf.Max(0,realSeconds);
            while(remaining>.000001f)
            {
                float step=Mathf.Min(remaining,.05f/adapter.Playback.Rate);remaining-=step;
                float dt=adapter.Playback.ScaledSeconds(step,false);Weather.Advance(dt);Season.Advance(dt,adapter.Playback.DayMinutes);Simulation.Winter=Season.Winter;Simulation.Raining=Weather.Kind==LifeWeather.Rain;
                lighting.Advance(step,false);
                Simulation.DayMinutes=adapter.Playback.DayMinutes;Simulation.Tick(dt,lighting.Hour);adapter.elapsed+=dt;sampleTime+=dt;
            }
            Present();
            if(seasonVisual)seasonVisual.Apply(Season.Weights);lighting.RefreshEnvironment();if(weatherVisual)weatherVisual.Advance(adapter.Playback.ScaledSeconds(realSeconds,false));
        }
        public void SelectSeason(int index){if(index==4)Season.State.automatic=true;else Season.Select(index);SyncSeason();}
        public void SelectWeather(int index)
        {if(index==3)Weather.Resume();else Weather.Select((LifeWeather)Mathf.Clamp(index,0,2));Simulation.Raining=Weather.Kind==LifeWeather.Rain;lighting.RefreshEnvironment();}
        void Update()
        {
            Advance(Time.unscaledDeltaTime);
            if(observing&&Simulation!=null&&Time.realtimeSinceStartup-wallStart>=nextLog)
            {
                Simulation.Validate();var s=Simulation;string version=observing140?"LIFE140":"LIFE139";
                Debug.Log($"{version}_WALL_SAMPLE wall={Time.realtimeSinceStartup-wallStart:F1} simulated={s.elapsed:F1} day={lighting.Day} food={s.food} produced={s.produced} eaten={s.consumed} home={s.homeArrivals} reserved={s.ReservedProduction} bundles={s.bundles.Count}");nextLog+=60;
                if(Time.realtimeSinceStartup-wallStart>=(observing140?600:1800))
                {Debug.Log(observing140?"LIFE140_REALTIME_10MIN_OK":"LIFE139_REALTIME_30MIN_OK");Application.Quit(0);}
            }
        }
        public void Present()
        {
            if(Simulation==null)return;
            if(growthVisual)growthVisual.Present(Simulation);
            if(fieldVisual)fieldVisual.Present(Simulation);
            if(gardenVisual)gardenVisual.Present(Simulation);
            if(poultryVisual)poultryVisual.Present(Simulation.Poultry,Simulation.elapsed);
            foreach(var r in Simulation.residents)
            {
                var resident=adapter.residents[r.id];resident.root.position=r.position;
                if(r.forward.sqrMagnitude>.01f)resident.root.rotation=Quaternion.LookRotation(r.forward);
                if(!r.sleeping){resident.visual.animator.gameObject.SetActive(true);RestoreSocialGesture(resident.visual);resident.visual.Sample(sampleTime+r.id*.37,r.walkDistance/.60f,r.Moving?1:0);ApplySocialGesture(resident.visual,r);}
                // Returning home is a coarse doorway transition, not a new detailed door animation.
                resident.visual.animator.gameObject.SetActive(!r.sleeping);
                cargo[r.id].gameObject.SetActive(r.cargo>0);
            }
            int pi=0,bi=0;
            foreach(var s in Simulation.sites)
            {
                if(s.kind==SiteKind.Crop&&!s.id.StartsWith(FieldLayout.Prefix)){plants[pi].gameObject.SetActive(s.planted);plants[pi].localScale=Vector3.one*Mathf.Lerp(.18f,1,s.growth);pi++;}
                if(s.kind==SiteKind.Berry){berries[bi].gameObject.SetActive(s.growth>=1);bi++;}
            }
            if(storageStock)storageStock.localScale=new Vector3(1,Mathf.Max(.02f,Simulation.food/(float)Simulation.Capacity),1);
            foreach(int id in groundBaskets.Keys.Where(id=>!Simulation.bundles.Any(b=>b.id==id)).ToArray()){RemoveBasket(groundBaskets[id]);groundBaskets.Remove(id);}
            foreach(var bundle in Simulation.bundles)
            {
                if(!groundBaskets.TryGetValue(bundle.id,out var basket)){
                    basket=Instantiate(cargo[0].gameObject,transform);basket.name="Recoverable food basket "+bundle.id;
                    basket.transform.localScale=cargo[0].localScale;groundBaskets.Add(bundle.id,basket);
                }
                basket.SetActive(true);basket.transform.position=bundle.position+Vector3.up*.12f;basket.transform.rotation=Quaternion.identity;
            }
        }
        public string Summary(int selected)
        {
            if(Simulation==null)return "자율 생활 준비 중";
            string food=Simulation.food<4?"부족함":Simulation.food<12?"조금 있음":"넉넉함";
            if(selected>=0&&selected<Simulation.residents.Length){var r=Simulation.residents[selected];return $"주민 {selected+1} · {AutonomousSimulation.TraitLabel(r.temperament)} · {(r.socialPartner>0?AutonomousSimulation.SocialLabel(r):Season.Winter&&r.action==LifeAction.Shelter?"눈 피하기":AutonomousSimulation.Label(r.action))}\n{(r.socialPartner>0?"이웃과 잠시 여유를 나누는 중":r.reason)}";}
            return $"식량: {food} · 닭 먹이 {Simulation.Poultry?.feed??0} · 둥지 달걀 {Simulation.Poultry?.eggs??0}/6";
        }
        readonly Dictionary<FarmResidentVisual,Transform> socialHeads=new Dictionary<FarmResidentVisual,Transform>();
        readonly Dictionary<Transform,Quaternion> socialBaseRotations=new Dictionary<Transform,Quaternion>();
        void RestoreSocialGesture(FarmResidentVisual visual)
        {
            if(socialHeads.TryGetValue(visual,out var head)&&head&&socialBaseRotations.TryGetValue(head,out var rotation)){
                head.localRotation=rotation;socialBaseRotations.Remove(head);
            }
        }
        void ApplySocialGesture(FarmResidentVisual visual,LifeResident r)
        {
            if(r.socialPartner==0)return;
            if(!socialHeads.TryGetValue(visual,out var head)){
                head=visual.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(s=>s.bones).FirstOrDefault(b=>b&&b.name=="Head");
                socialHeads[visual]=head;
            }
            if(!head)return;
            // A small nod layered after the existing pose sample; the authored clips stay untouched.
            float nod=0,t=r.socialTime;
            if(t<AutonomousSimulation.GreetingSeconds)nod=12*Mathf.Sin(Mathf.PI*t/AutonomousSimulation.GreetingSeconds);
            else if(AutonomousSimulation.Talking(r)){
                float u=t-AutonomousSimulation.GreetingSeconds;
                nod=3*Mathf.Sin(u*Mathf.PI*2/1.2f+r.id*Mathf.PI)*Mathf.Sin(Mathf.PI*u/AutonomousSimulation.ConversationSeconds);
            }
            socialBaseRotations[head]=head.localRotation;
            head.localRotation*=Quaternion.Euler(nod,0,0);
        }
        static void RemoveBasket(GameObject basket){if(Application.isPlaying)Destroy(basket);else DestroyImmediate(basket);}
    }
}
