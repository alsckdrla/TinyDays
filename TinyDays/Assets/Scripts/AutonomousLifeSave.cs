using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TinyDays.Life
{
    // The repository can be exercised against an isolated directory without touching player saves.
    public sealed class AutonomousSaveRepository
    {
        public readonly string Path;
        readonly Action<AutonomousSaveData> validate;
        public AutonomousSaveRepository(string directory,Action<AutonomousSaveData> validate)
        {Path=System.IO.Path.Combine(directory,"AutonomousLife142.json");this.validate=validate;}
        public bool Exists=>File.Exists(Path)||File.Exists(Path+".bak");
        public AutonomousSaveData Read(out bool backup)
        {
            Exception last=null;backup=false;
            foreach(string candidate in new[]{Path,Path+".bak"}){
                if(!File.Exists(candidate))continue;
                try{var data=AutonomousSaveCodec.Decode(File.ReadAllText(candidate,Encoding.UTF8));validate(data);backup=candidate!=Path;return data;}
                catch(Exception error){last=error;}
            }
            throw new InvalidDataException("No valid save",last);
        }
        public void Write(AutonomousSaveData data)
        {
            validate(data);string json=AutonomousSaveCodec.Encode(data);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temporary=Path+".tmp";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){
                byte[] bytes=Encoding.UTF8.GetBytes(json);stream.Write(bytes,0,bytes.Length);stream.Flush(true);
            }
            validate(AutonomousSaveCodec.Decode(File.ReadAllText(temporary,Encoding.UTF8)));
            if(!File.Exists(Path)){File.Move(temporary,Path);return;}
            bool primaryValid=true;int primaryLegacy=0;
            try{var previous=AutonomousSaveCodec.Decode(File.ReadAllText(Path,Encoding.UTF8));validate(previous);primaryLegacy=previous.sourceVersion<150?previous.sourceVersion:0;}catch{primaryValid=false;}
            if(primaryLegacy>0&&!File.Exists(Path+".v"+primaryLegacy))File.Copy(Path,Path+".v"+primaryLegacy,false);
            if(!primaryValid)File.Copy(Path,Path+".unreadable."+DateTime.UtcNow.Ticks,false);
            if(File.Exists(Path+".bak")){
                bool backupValid=true;int backupLegacy=0;try{var previous=AutonomousSaveCodec.Decode(File.ReadAllText(Path+".bak",Encoding.UTF8));validate(previous);backupLegacy=previous.sourceVersion<150?previous.sourceVersion:0;}catch{backupValid=false;}
                if(backupLegacy>0&&!File.Exists(Path+".v"+backupLegacy))File.Copy(Path+".bak",Path+".v"+backupLegacy,false);
                if(!backupValid)File.Copy(Path+".bak",Path+".bak.unreadable."+DateTime.UtcNow.Ticks,false);
            }
            // Never replace a good backup with the corrupt primary during recovery.
            File.Replace(temporary,Path,primaryValid?Path+".bak":null);
        }
    }
    [DefaultExecutionOrder(200)]
    public sealed class AutonomousLifeSave : MonoBehaviour
    {
        AutonomousLifeWorld world;AutonomousSaveRepository repository;
        float sinceSave,noticeUntil;string lastPayload,status;bool ready;
        public bool SaveBlocked {get;private set;}
        public string Status {get=>status??"";private set{status=value;noticeUntil=Time.realtimeSinceStartup+8;}}
        public string Notice=>SaveBlocked||Time.realtimeSinceStartup<noticeUntil?Status:"";
        public string SavePath=>repository?.Path;
        public bool HasSave=>repository!=null&&repository.Exists;
        public void Configure(AutonomousLifeWorld target,string directory=null,bool load=true)
        {
            world=target;repository=new AutonomousSaveRepository(directory??DefaultDirectory(),d=>world.ValidateSnapshot(d));
            ready=true;sinceSave=0;
            if(load&&repository.Exists)LoadNow();else Status="아직 저장 없음";
        }
        static string DefaultDirectory()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-lifeSaveDirectory");
            return index>=0&&index+1<args.Length?System.IO.Path.GetFullPath(args[index+1]):System.IO.Path.Combine(Application.persistentDataPath,"AutonomousLife");
        }
        public bool SaveNow(bool confirmed=false)
        {
            if(!ready)return false;
            if(SaveBlocked&&!confirmed){Status="읽을 수 없는 저장이 있습니다. 저장하려면 확인이 필요합니다.";return false;}
            try{
                var data=world.CaptureSnapshot();repository.Write(data);lastPayload=JsonUtility.ToJson(data);
                sinceSave=0;SaveBlocked=false;Status="저장 완료 · "+DateTime.Now.ToString("HH:mm:ss");return true;
            }catch(Exception error){Status="저장 실패 · 기존 저장은 유지됩니다.";Debug.LogWarning("LIFE142_SAVE_FAILED "+error.Message);return false;}
        }
        public bool LoadNow()
        {
            if(!ready)return false;
            if(!repository.Exists){Status="불러올 저장이 없습니다.";return false;}
            try{
                var data=repository.Read(out bool backup);world.RestoreSnapshot(data);lastPayload=JsonUtility.ToJson(world.CaptureSnapshot());
                SaveBlocked=false;sinceSave=0;Status=backup?"백업에서 복원했습니다.":"불러오기 완료";return true;
            }catch(Exception error){SaveBlocked=true;Status="저장을 읽을 수 없습니다. 현재 상태와 파일을 보존했습니다.";Debug.LogWarning("LIFE142_LOAD_FAILED "+error.Message);return false;}
        }
        public void StartNewGame()
        {world.Restart();world.GetComponent<TinyDays.Review.FarmStudyReview>().ShowOverview();SaveNow(true);}
        public void Poll(float seconds)
        {
            if(!ready||SaveBlocked)return;
            sinceSave+=Mathf.Max(0,seconds);if(sinceSave<60)return;sinceSave=0;
            try{if(JsonUtility.ToJson(world.CaptureSnapshot())!=lastPayload)SaveNow();}
            catch(Exception error){Status="자동 저장 실패 · 현재 생활은 유지됩니다.";Debug.LogWarning("LIFE142_AUTOSAVE_FAILED "+error.Message);}
        }
        void Update(){Poll(Time.unscaledDeltaTime);}
        void OnApplicationQuit(){if(ready&&!SaveBlocked)SaveNow();}
    }
}
