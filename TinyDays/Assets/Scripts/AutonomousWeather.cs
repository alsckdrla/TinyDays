using System;
using System.Linq;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    public enum LifeWeather { Clear, Cloudy, Rain }
    [Serializable] public sealed class WeatherSaveData
    {
        public int phase; public float elapsed,cloud,rain; public bool automatic=true;
    }
    public sealed class AutonomousWeather
    {
        static readonly float[] Durations={120,30,90,30};
        public WeatherSaveData State {get;private set;}=new WeatherSaveData();
        public LifeWeather Kind=>State.phase==0?LifeWeather.Clear:State.phase==2?LifeWeather.Rain:LifeWeather.Cloudy;
        public string Label=>new[]{"맑음","흐림","비"}[(int)Kind];
        public void Select(LifeWeather kind)
        {State.phase=kind==LifeWeather.Clear?0:kind==LifeWeather.Rain?2:1;State.elapsed=0;State.automatic=false;}
        public void Resume()=>State.automatic=true;
        public void Advance(float seconds)
        {
            if(seconds<=0)return;
            if(State.automatic){State.elapsed+=seconds;while(State.elapsed>=Durations[State.phase]){State.elapsed-=Durations[State.phase];State.phase=(State.phase+1)%4;}}
            State.cloud=Mathf.MoveTowards(State.cloud,Kind==LifeWeather.Clear?0:1,seconds/5);
            State.rain=Mathf.MoveTowards(State.rain,Kind==LifeWeather.Rain?1:0,seconds/5);
        }
        public WeatherSaveData Capture()=>JsonUtility.FromJson<WeatherSaveData>(JsonUtility.ToJson(State));
        public void Restore(WeatherSaveData data){Validate(data);State=JsonUtility.FromJson<WeatherSaveData>(JsonUtility.ToJson(data));}
        public static void Validate(WeatherSaveData d)
        {AutonomousSaveCodec.Require(d!=null&&d.phase>=0&&d.phase<4&&AutonomousSaveCodec.Finite(d.elapsed)&&d.elapsed>=0&&d.elapsed<Durations[d.phase]&&AutonomousSaveCodec.Finite(d.cloud)&&d.cloud>=0&&d.cloud<=1&&AutonomousSaveCodec.Finite(d.rain)&&d.rain>=0&&d.rain<=1,"Invalid weather");}
        public static LifeSite[] Shelters(AutonomousLifeWorld world)
        {
            var nav=new AutonomousNavigation(world.land,world.obstacles);
            return world.definitions.Where(s=>s.kind==SiteKind.Home).Select((s,i)=>new LifeSite{id="shelter-"+i,kind=SiteKind.Shelter,position=nav.Nearby(s.position+new Vector3(1.05f,0,-.25f)),homeResident=-1}).ToArray();
        }
    }

    // Generated runtime presentation. Scene files, user materials and authored models remain untouched.
    sealed class AutonomousWeatherVisual : MonoBehaviour
    {
        AutonomousLifeWorld world;ParticleSystem drops;Material rainMaterial,wood;float emission;
        bool snowMode;Bounds[] rainObstacles;
        readonly System.Random random=new System.Random(144);
        public void Configure(AutonomousLifeWorld target)
        {
            world=target;rainObstacles=target.obstacles.Concat(new[]{new Bounds(new Vector3(10.4f,1,-3.8f),new Vector3(3.5f,2,3))}).ToArray();
            if(!drops){
                wood=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.45f,.34f,.20f)};
                foreach(var site in world.Simulation.sites.Where(s=>s.kind==SiteKind.Shelter)){
                    var root=new GameObject("Weather awning "+site.id);root.transform.SetParent(transform,false);
                    var roof=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(roof.GetComponent<Collider>());roof.name="Shelter roof";roof.transform.SetParent(root.transform,false);
                    roof.transform.position=site.position+Vector3.up*2.05f;roof.transform.localScale=new Vector3(1.65f,.12f,1.65f);roof.GetComponent<Renderer>().sharedMaterial=wood;
                    for(int side=-1;side<=1;side+=2){var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(beam.GetComponent<Collider>());beam.name="Wall bracket";beam.transform.SetParent(root.transform,false);beam.transform.position=site.position+new Vector3(side*.68f,1.87f,.25f);beam.transform.localScale=new Vector3(.09f,.24f,1.05f);beam.GetComponent<Renderer>().sharedMaterial=wood;}
                }
                var g=new GameObject("Weather rain");g.transform.SetParent(transform,false);drops=g.AddComponent<ParticleSystem>();drops.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=drops.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=1800;main.startLifetime=1;main.startSize=.025f;main.startColor=new Color(.78f,.87f,.95f,.5f);
                var em=drops.emission;em.enabled=false;var shape=drops.shape;shape.enabled=false;
                var renderer=drops.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=7;renderer.velocityScale=.015f;
                rainMaterial=new Material(Resources.Load<Shader>("FarmRain"));renderer.sharedMaterial=rainMaterial;
                GetComponent<FarmCameraOcclusion>()?.Initialize(transform);
            }
            drops.Clear();emission=0;
        }
        public void Advance(float seconds)
        {
            if(!drops||seconds<=0)return;
            bool snow=world.Season.Winter;if(snow!=snowMode){drops.Clear();snowMode=snow;var renderer=drops.GetComponent<ParticleSystemRenderer>();renderer.renderMode=snow?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch;}
            drops.Simulate(seconds,false,false,false);emission+=seconds*(snow?170:650)*world.Weather.State.rain;
            int count=Mathf.Min(1000,Mathf.FloorToInt(emission));emission-=count;
            var shelters=world.Simulation.sites.Where(s=>s.kind==SiteKind.Shelter).ToArray();
            for(int i=0;i<count;i++){
                var bounds=world.land;float x=Mathf.Lerp(bounds.min.x,bounds.max.x,(float)random.NextDouble()),z=Mathf.Lerp(bounds.min.z,bounds.max.z,(float)random.NextDouble());
                if(world.Simulation.Growth!=null&&world.Simulation.Growth.stage==3&&Mathf.Abs(x-world.Simulation.Growth.site.x)<1&&Mathf.Abs(z-world.Simulation.Growth.site.z)<.95f)continue;
                if(rainObstacles.Any(b=>x>=b.min.x-.1f&&x<=b.max.x+.1f&&z>=b.min.z-.1f&&z<=b.max.z+.1f)||shelters.Any(s=>Mathf.Abs(x-s.position.x)<.85f&&Mathf.Abs(z-s.position.z)<.85f))continue;
                drops.Emit(new ParticleSystem.EmitParams{position=new Vector3(x,6,z),velocity=snow?new Vector3(.12f,-1.6f,.08f):Vector3.down*9,startLifetime=snow?3.7f:.65f,startSize=snow?.065f:.025f,startColor=new Color(.78f,.87f,.95f,.5f)},1);
            }
        }
        void OnDestroy(){if(rainMaterial)Destroy(rainMaterial);if(wood)Destroy(wood);}
    }
}
