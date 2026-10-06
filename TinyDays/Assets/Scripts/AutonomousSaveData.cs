using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

namespace TinyDays.Life
{
    [Serializable] public sealed class RejectedSiteData { public int site;public float until; }
    [Serializable] public sealed class ResidentSaveData { public LifeResident state;public int bundleId=-1;public RejectedSiteData[] rejected; }
    [Serializable] public sealed class SiteSaveData { public string id;public int owner;public float growth;public bool planted; }
    [Serializable] public sealed class SimulationSaveData
    {
        public float elapsed,hour;public int[] counts;public int nextBundleId;
        public ResidentSaveData[] residents;public SiteSaveData[] sites;public FoodBundle[] bundles;
        public PoultryState poultry;public GrowthState growth;public FieldState field;public GardenState garden;
        public bool hasBlock;public Bounds block;
    }
    [Serializable] public sealed class CameraSaveData
    {
        public int view,focus,selected;public bool following,hidden;
        public Vector3 pivot,followOffset;public float yaw,pitch,distance,baseDistance,height,size;
    }
    [Serializable] public sealed class AutonomousSaveData
    {
        public string scene;public SimulationSaveData simulation;public CameraSaveData camera;
        public float sampleTime,hour,rate;public int day,dayMinutes;public bool automatic,paused;
        public WeatherSaveData weather;
        public SeasonSaveData season;
        [NonSerialized] public int sourceVersion=150;
    }
    [Serializable] sealed class SaveEnvelope { public int version;public string payload,checksum; }
    [Serializable] sealed class SceneSaveIdentity { public LifeTuning tuning;public LifeSite[] sites;public Vector3[] starts;public Bounds land;public Bounds[] obstacles; }
    public static class AutonomousSaveCodec
    {
        public static string Hash(string value)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");}
        public static string SceneIdentity(AutonomousLifeWorld world)=>Hash(JsonUtility.ToJson(new SceneSaveIdentity{tuning=world.tuning,sites=world.definitions,starts=world.starts,land=world.land,obstacles=world.obstacles}));
        public static string Encode(AutonomousSaveData data)
        {string payload=JsonUtility.ToJson(data);return JsonUtility.ToJson(new SaveEnvelope{version=data.sourceVersion,payload=payload,checksum=Hash(payload)},true);}
        public static AutonomousSaveData Decode(string json)
        {
            if(string.IsNullOrEmpty(json)||json.Length>4*1024*1024)throw new InvalidDataException("Invalid save size");
            var envelope=JsonUtility.FromJson<SaveEnvelope>(json);
            if(envelope==null||(envelope.version!=142&&envelope.version!=144&&envelope.version!=145&&envelope.version!=146&&envelope.version!=147&&envelope.version!=148&&envelope.version!=149&&envelope.version!=150)||string.IsNullOrEmpty(envelope.payload)||envelope.checksum!=Hash(envelope.payload))throw new InvalidDataException("Invalid save version/checksum");
            var result=JsonUtility.FromJson<AutonomousSaveData>(envelope.payload);
            if(result==null)throw new InvalidDataException("Missing save payload");result.sourceVersion=envelope.version;return result;
        }
        public static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
        public static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
    }
    public sealed partial class AutonomousSimulation
    {
        public SimulationSaveData CaptureSave()
        {
            Validate();return new SimulationSaveData{
                elapsed=elapsed,hour=Hour,nextBundleId=nextBundleId,garden=Garden==null?null:JsonUtility.FromJson<GardenState>(JsonUtility.ToJson(Garden)),field=Field==null?null:JsonUtility.FromJson<FieldState>(JsonUtility.ToJson(Field)),growth=Growth==null?null:JsonUtility.FromJson<GrowthState>(JsonUtility.ToJson(Growth)),poultry=Poultry==null?null:JsonUtility.FromJson<PoultryState>(JsonUtility.ToJson(Poultry)),
                counts=new[]{food,produced,consumed,planted,harvested,gathered,meals,rests,homeArrivals,recovered},
                sites=sites.Select(s=>new SiteSaveData{id=s.id,owner=s.owner,growth=s.growth,planted=s.planted}).ToArray(),
                residents=residents.Select(r=>new ResidentSaveData{state=JsonUtility.FromJson<LifeResident>(JsonUtility.ToJson(r)),bundleId=r.bundle?.id??-1,rejected=r.rejected.OrderBy(p=>p.Key).Select(p=>new RejectedSiteData{site=p.Key,until=p.Value}).ToArray()}).ToArray(),
                bundles=bundles.Select(b=>JsonUtility.FromJson<FoodBundle>(JsonUtility.ToJson(b))).ToArray(),
                hasBlock=navigation.TemporaryBlock.HasValue,block=navigation.TemporaryBlock??default
            };
        }
        public void RestoreSave(SimulationSaveData data)
        {
            Action<bool,string> need=AutonomousSaveCodec.Require;
            need(data!=null&&data.counts!=null&&data.counts.Length==10&&data.counts.All(n=>n>=0),"Invalid counters");
            need(AutonomousSaveCodec.Finite(data.elapsed)&&data.elapsed>=0&&AutonomousSaveCodec.Finite(data.hour)&&data.hour>=0&&data.hour<24,"Invalid simulation clock");
            need(data.sites!=null&&data.sites.Length==sites.Length&&data.residents!=null&&data.residents.Length==residents.Length&&data.bundles!=null,"Scene population mismatch");
            bool hasPoultry=sites.Any(s=>s.kind==SiteKind.Feeder);
            need(!hasPoultry||data.poultry!=null&&data.poultry.hens!=null,"Poultry scope mismatch");
            Poultry=hasPoultry?JsonUtility.FromJson<PoultryState>(JsonUtility.ToJson(data.poultry)):null;
            RestoreGrowth(data.growth);RestoreField(data.field);RestoreGarden(data.garden);
            food=data.counts[0];produced=data.counts[1];consumed=data.counts[2];planted=data.counts[3];harvested=data.counts[4];gathered=data.counts[5];meals=data.counts[6];rests=data.counts[7];homeArrivals=data.counts[8];recovered=data.counts[9];elapsed=data.elapsed;Hour=data.hour;nextBundleId=data.nextBundleId;
            need(data.nextBundleId>=0&&data.bundles.Select(b=>b.id).Distinct().Count()==data.bundles.Length,"Invalid bundle identities");
            bundles.Clear();foreach(var b in data.bundles){need(b!=null&&b.id>=0&&b.id<nextBundleId&&b.owner>=-1&&b.owner<residents.Length&&AutonomousSaveCodec.Finite(b.position)&&AutonomousSaveCodec.Finite(b.retryAfter),"Invalid bundle");bundles.Add(JsonUtility.FromJson<FoodBundle>(JsonUtility.ToJson(b)));}
            for(int i=0;i<sites.Length;i++){
                var d=data.sites[i];need(d!=null&&d.id==sites[i].id&&d.owner>=-1&&d.owner<residents.Length&&AutonomousSaveCodec.Finite(d.growth)&&d.growth>=0&&d.growth<=1,"Invalid site");
                sites[i].owner=d.owner;sites[i].growth=d.growth;sites[i].planted=d.planted;
            }
            for(int i=0;i<residents.Length;i++){
                var d=data.residents[i];need(d!=null&&d.state!=null&&d.state.id==i,"Invalid resident identity");var r=d.state;
                need(Enum.IsDefined(typeof(LifeAction),r.action)&&Enum.IsDefined(typeof(LifeTemperament),r.temperament)&&r.site>=-1&&r.site<sites.Length&&r.lastLeisureSite>=-1&&r.lastLeisureSite<sites.Length,"Invalid resident action");
                need(AutonomousSaveCodec.Finite(r.position)&&AutonomousSaveCodec.Finite(r.forward)&&new[]{r.hunger,r.fatigue,r.work,r.wait,r.blockedSeconds,r.walkDistance,r.waitingSince,r.nextRepath}.All(AutonomousSaveCodec.Finite),"Invalid resident number");
                need(r.hunger>=0&&r.hunger<=1&&r.fatigue>=0&&r.fatigue<=1&&r.work>=0&&r.blockedSeconds>=0&&r.walkDistance>=0&&r.completed>=0&&r.productionReservation>=0&&r.mealReservation>=0&&r.collectionReservation>=0,"Invalid resident progress");
                need(r.waypoint>=0&&(r.path==null?r.waypoint==0:r.waypoint<=r.path.Count&&r.path.All(AutonomousSaveCodec.Finite)),"Invalid movement path");
                need(d.rejected!=null&&d.rejected.All(p=>p!=null&&p.site>=0&&p.site<sites.Length&&AutonomousSaveCodec.Finite(p.until))&&d.rejected.Select(p=>p.site).Distinct().Count()==d.rejected.Length,"Invalid recovery state");
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(r),residents[i]);residents[i].rejected.Clear();foreach(var p in d.rejected)residents[i].rejected.Add(p.site,p.until);
                need(d.bundleId>=-1&&(d.bundleId<0||bundles.Any(b=>b.id==d.bundleId)),"Missing bundle reference");residents[i].bundle=d.bundleId<0?null:bundles.Single(b=>b.id==d.bundleId);
                need((r.action==LifeAction.None?r.site==-1:(r.action==LifeAction.Collect||r.action==LifeAction.WoodRecover)?r.site==-1:r.site>=0)&&((r.action==LifeAction.Collect)==(residents[i].bundle!=null)),"Invalid action ownership");
            }
            if(data.hasBlock)need(AutonomousSaveCodec.Finite(data.block.center)&&AutonomousSaveCodec.Finite(data.block.size)&&data.block.size.x>=0&&data.block.size.y>=0&&data.block.size.z>=0,"Invalid blocked area");
            navigation.TemporaryBlock=data.hasBlock?(Bounds?)data.block:null;Validate();
        }
    }
}
