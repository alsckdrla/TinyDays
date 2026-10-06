using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Life;
using TinyDays.Review;

public static class AutonomousLife142Checks
{
    static readonly List<string> rows=new List<string>();
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,message);}
    static AutonomousLifeWorld World()
    {
        EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);
        var world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();world.Initialize();
        world.GetComponent<FarmStudyReview>().ResetCameraToPreset();return world;
    }
    static void RoundTrip(AutonomousLifeWorld world,string label)
    {
        var before=world.CaptureSnapshot();var serialized=AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(before));
        world.RestoreSnapshot(serialized);var after=world.CaptureSnapshot();
        Check(JsonUtility.ToJson(before)==JsonUtility.ToJson(after),"State changed during roundtrip: "+label);
        // A resumed clone must make the same decisions as the uninterrupted state.
        var uninterrupted=world.ValidateSnapshot(before);var resumed=world.ValidateSnapshot(after);
        for(int i=0;i<120;i++){float hour=Mathf.Repeat(before.hour+i*.05f*24/(before.dayMinutes*60),24);uninterrupted.Tick(.05f,hour);resumed.Tick(.05f,hour);uninterrupted.Validate();resumed.Validate();}
        Check(JsonUtility.ToJson(uninterrupted.CaptureSave())==JsonUtility.ToJson(resumed.CaptureSave()),"Continuation mismatch: "+label);
    }
    static void Actions()
    {
        var world=World();world.Simulation.residents[0].fatigue=.9f;world.Simulation.residents[1].hunger=.8f;
        var seen=new HashSet<LifeAction>();bool moving=false,dropped=false,collect=false,blocked=false,sleeping=false;
        for(int i=0;i<9000;i++){
            world.Advance(.2f);var sim=world.Simulation;
            foreach(var r in sim.residents)if(r.action!=LifeAction.None&&((!r.Moving&&r.work>0)||r.action==LifeAction.Deliver||r.action==LifeAction.Collect)&&seen.Add(r.action))RoundTrip(world,r.action.ToString());
            sim=world.Simulation;
            if(!moving&&sim.residents.Any(r=>r.Moving)){RoundTrip(world,"moving");moving=true;sim=world.Simulation;}
            if(!sleeping&&sim.residents.Any(r=>r.sleeping)){RoundTrip(world,"sleeping");sleeping=true;sim=world.Simulation;}
            if(!dropped&&sim.residents.Any(r=>r.cargo>0)){
                sim.CancelWork(sim.residents.First(r=>r.cargo>0).id);RoundTrip(world,"dropped basket/recovery cooldown");dropped=true;sim=world.Simulation;
            }
            if(!collect&&sim.residents.Any(r=>r.action==LifeAction.Collect)){RoundTrip(world,"collect ownership");collect=true;sim=world.Simulation;}
            if(!blocked&&sim.residents.Any(r=>r.Moving&&r.path.Count>r.waypoint)){
                var r=sim.residents.First(r=>r.Moving&&r.path.Count>r.waypoint);var point=r.path.Last();
                if(sim.residents.All(o=>Vector3.Distance(o.position,point)>1.2f)){
                    sim.navigation.TemporaryBlock=new Bounds(point,new Vector3(.6f,2,.6f));world.Advance(.8f);RoundTrip(world,"temporary path block");world.Simulation.navigation.TemporaryBlock=null;blocked=true;
                }
            }
            if(seen.Contains(LifeAction.Rest)&&seen.Contains(LifeAction.Home)&&seen.Contains(LifeAction.Eat)&&seen.Contains(LifeAction.Plant)&&seen.Contains(LifeAction.Harvest)&&seen.Contains(LifeAction.Gather)&&seen.Contains(LifeAction.Deliver)&&seen.Contains(LifeAction.Leisure)&&moving&&sleeping&&dropped&&collect&&blocked)break;
        }
        foreach(var action in new[]{LifeAction.Plant,LifeAction.Harvest,LifeAction.Gather,LifeAction.Deliver,LifeAction.Eat,LifeAction.Rest,LifeAction.Leisure,LifeAction.Home})Check(seen.Contains(action),"Missing action coverage: "+action);
        Check(moving&&sleeping&&dropped&&collect&&blocked,"Missing movement/bundle/block coverage");
        rows.Add("PASS roundtrip + 6s uninterrupted/resumed comparison: "+string.Join(",",seen)+"; moving/sleeping/drop/collect/path-block");
        world.Initialize();var director=world.GetComponent<FarmLifeDirector>();var light=world.GetComponent<FarmLightingStudy>();var camera=world.GetComponent<FarmStudyReview>();
        director.Playback.SetDayMinutes("20");director.Playback.SetRate(3);director.paused=true;light.RestoreClock(7,18.25f,false);
        camera.FocusResident(2);camera.KeyboardMove(new Vector2(.3f,-.4f),.8f);camera.hidden=true;
        RoundTrip(world,"fixed clock/rate/pause/followed camera/hidden menu");var frozen=world.CaptureSnapshot();world.Advance(10);Check(JsonUtility.ToJson(frozen)==JsonUtility.ToJson(world.CaptureSnapshot()),"Pause advanced restored world");
        rows.Add("PASS Day/hour/fixed clock/day length/rate/pause/camera framing/follow/selection/menu hidden");
        world=World();foreach(var site in world.definitions.Where(s=>s.kind==SiteKind.Store).Skip(1))site.kind=SiteKind.Leisure;
        world.Initialize();foreach(var resident in world.Simulation.residents){resident.hunger=.8f;resident.wait=0;}
        world.Advance(.05f);Check(world.Simulation.residents.Any(r=>r.action==LifeAction.Wait),"No queued resident");RoundTrip(world,"same-site queue wait");
        rows.Add("PASS same-site waiting/reservation roundtrip");
    }
    static void Files()
    {
        var world=World();string directory=Path.GetFullPath("Logs/Save142Checks-"+Guid.NewGuid().ToString("N"));
        var repo=new AutonomousSaveRepository(directory,d=>world.ValidateSnapshot(d));var first=world.CaptureSnapshot();repo.Write(first);
        string envelope=AutonomousSaveCodec.Encode(first);Check(envelope.Contains("\"version\": 142"),"Version field missing from encoded save");
        Reject(()=>AutonomousSaveCodec.Decode(envelope.Replace("\"version\": 142","\"version\": 143")),"Unsupported schema accepted");
        Reject(()=>AutonomousSaveCodec.Decode(envelope.Replace("\"version\": 142","\"unused\": 142")),"Unversioned schema accepted");
        Check(File.Exists(repo.Path)&&!File.Exists(repo.Path+".tmp"),"First atomic save");world.Advance(5);var second=world.CaptureSnapshot();repo.Write(second);
        Check(File.Exists(repo.Path+".bak")&&repo.Read(out bool backup).simulation.elapsed==second.simulation.elapsed&&!backup,"Primary save");
        File.WriteAllText(repo.Path,"broken");var recovered=repo.Read(out backup);Check(backup&&recovered.simulation.elapsed==first.simulation.elapsed,"Backup restore");
        repo.Write(second);Check(File.ReadAllText(repo.Path+".bak")==AutonomousSaveCodec.Encode(first)&&Directory.GetFiles(directory,"*.unreadable.*").Length==1,"Corrupt primary preservation");
        File.WriteAllText(repo.Path,"broken");File.WriteAllText(repo.Path+".bak","also broken");Reject(()=>repo.Read(out _),"Double corruption accepted");
        var saving=world.gameObject.AddComponent<AutonomousLifeSave>();saving.Configure(world,directory,true);
        string live=JsonUtility.ToJson(world.CaptureSnapshot());Check(saving.SaveBlocked&&!saving.SaveNow(),"Corrupt save was silently replaced");saving.Poll(60);Check(File.ReadAllText(repo.Path)=="broken"&&JsonUtility.ToJson(world.CaptureSnapshot())==live,"Failed load changed live state");
        Check(saving.SaveNow(true)&&!saving.SaveBlocked&&Directory.GetFiles(directory,"*.bak.unreadable.*").Any(f=>File.ReadAllText(f)=="also broken"),"Confirmed replacement did not preserve corrupt backup");
        saving.StartNewGame();Check(world.Simulation.elapsed==0&&world.GetComponent<FarmLightingStudy>().Day==1&&world.GetComponent<FarmLightingStudy>().Hour==12,"New game restart");
        world.Advance(2);saving.Poll(60);Check(repo.Read(out _).simulation.elapsed==world.Simulation.elapsed,"60 second autosave");
        world.GetComponent<FarmLifeDirector>().paused=true;saving.Poll(60);Check(repo.Read(out _).paused,"Paused setting change not saved");
        world.GetComponent<FarmLifeDirector>().paused=false;world.Advance(1);typeof(AutonomousLifeSave).GetMethod("OnApplicationQuit",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(saving,null);Check(repo.Read(out _).simulation.elapsed==world.Simulation.elapsed,"Quit save failed");
        string good=File.ReadAllText(repo.Path);using(var held=new FileStream(repo.Path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){Check(!saving.SaveNow(),"Locked write unexpectedly succeeded");}Check(File.ReadAllText(repo.Path)==good,"Failed write damaged primary");
        string unchanged=JsonUtility.ToJson(world.CaptureSnapshot());
        var bad=world.CaptureSnapshot();bad.simulation.residents[0].state.hunger=float.NaN;Reject(()=>world.RestoreSnapshot(bad),"Invalid number accepted");Check(JsonUtility.ToJson(world.CaptureSnapshot())==unchanged,"Invalid load changed live state");
        bad=world.CaptureSnapshot();bad.simulation.sites[0].id="wrong";Reject(()=>world.RestoreSnapshot(bad),"Foreign site accepted");
        bad=world.CaptureSnapshot();bad.simulation.counts[0]++;Reject(()=>world.RestoreSnapshot(bad),"Food corruption accepted");
        bad=world.CaptureSnapshot();bad.scene="another scene";Reject(()=>world.RestoreSnapshot(bad),"Foreign scene accepted");
        Check(JsonUtility.ToJson(world.CaptureSnapshot())==unchanged,"Rejected saves changed active state");
        rows.Add("PASS atomic primary/backup, corruption fallback/preservation/explicit replacement, failed load unchanged, locked write, autosave while paused, normal quit, new game, invalid number/ID/food/scene");
        rows.Add("Isolated fixture directory: "+directory);UnityEngine.Object.DestroyImmediate(saving);
    }
    public static void Execute()
    {
        try{Actions();Files();File.WriteAllLines("Docs/AutonomousLife142Verification.txt",rows);AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE142_VERIFY_BUILD_OK");}
        catch(Exception error){Debug.LogException(error);File.WriteAllLines("Docs/AutonomousLife142Verification.txt",rows.Concat(new[]{"FAILED: "+error}));EditorApplication.Exit(1);}
    }
}
