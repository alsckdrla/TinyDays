using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TinyDays.Review {
    // Authored single-resident routine; not a planner or autonomous gardening AI.
    public sealed class RabbitWaterLifeFlow : MonoBehaviour {
        public enum Phase { Inside, Exiting, ToCan, Picking, ToFlowers, Watering, ToStorage, Putting, Finished, Held, ToBench, BenchSitting, BenchResting, BenchRising, BenchDeparting }
        public RabbitHomeLifeReview home;
        public Transform storage,approach,work;
        public Phase State {get;private set;}
        public bool Carrying=>home.watering.Carrying;
        public int Completed {get;private set;}
        public bool StopRequested {get;private set;}
        Phase resume;
        float remainder;
        bool includeBench,benchOwned,benchRested;
        Pose canApproach;
        public bool IncludesBench=>includeBench;
        public bool BenchOwned=>benchOwned;
        public float MaxOwnerPositionJump {get;private set;}
        RabbitBenchReview Bench=>home.benchRest;
        RabbitWaterReview Water=>home.watering;
        System.Collections.IEnumerator Start(){
            var args=Environment.GetCommandLineArgs();bool benchSmoke=Array.IndexOf(args,"-waterBenchLifeSmoke")>=0;if(Array.IndexOf(args,"-waterLifeSmoke")<0&&!benchSmoke)yield break;
            home.SelectLifeFlow();StartFlow(benchSmoke);
            for(int i=0;i<60*70&&State!=Phase.Finished;i++){home.Advance(1f/60);if(i%120==0)yield return null;}
            bool pass=State==Phase.Finished&&home.DoorClosed&&Water.MaxGripError<=.01f&&Water.MaxReach<=.001f&&Water.MaxDrift<=.0035f&&Water.MinSole>=-.0005f&&Vector3.Distance(Water.can.position,storage.position)<.001f&&(!benchSmoke||(benchRested&&Bench.State==RabbitBenchReview.TaskState.Standing&&Bench.MaxDrift<=.0035f&&Bench.MinSole>=-.0005f));
            Debug.Log($"{(benchSmoke?"WATER_BENCH_LIFE":"WATER_LIFE")}_PLAYER_{(pass?"OK":"FAILED")} state={State} grip={Water.MaxGripError:F6} drift={Water.MaxDrift:F6} sole={Water.MinSole:F6}. Automatic callbacks, not OS mouse input.");
            int capture=Array.IndexOf(args,"-waterLifeCapture");
            if(capture>=0&&capture+1<args.Length){home.paused=true;yield return new WaitForEndOfFrame();Bench.CapturePlayerCamera(args[capture+1]);yield return null;}
            Application.Quit(pass?0:1);
        }
        public string Label=>State==Phase.ToBench?"벤치로 이동 · 방향 정리":State==Phase.BenchSitting?"벤치에 앉는 중":State==Phase.BenchResting?"벤치에서 6초 휴식":State==Phase.BenchRising?"벤치에서 일어나는 중":State==Phase.BenchDeparting?"벤치에서 약 1m 이동":State==Phase.Inside?"실내 대기":State==Phase.Exiting?"외출 중":State==Phase.ToCan?"물뿌리개로 이동":State==Phase.Picking?"물뿌리개 집기":State==Phase.ToFlowers?"화단으로 운반":State==Phase.Watering?"물 주기":State==Phase.ToStorage?"보관점으로 돌아오기":State==Phase.Putting?"제자리에 내려놓기":State==Phase.Finished?"완료 · 서서 대기":"중단 · 현재 위치 대기";
        public void ResetTask(){
            exitApproachReady=false;
            Water.Shutdown();Bench.Shutdown();MaxOwnerPositionJump=0;benchOwned=benchRested=includeBench=false;State=Phase.Inside;remainder=0;StopRequested=false;Completed=0;
            Water.can.gameObject.SetActive(true);Water.can.SetPositionAndRotation(storage.position,storage.rotation);
        }
        public void Shutdown(){StopRequested=false;remainder=0;Water.Shutdown();if(benchOwned)Bench.Shutdown();benchOwned=false;}
        public void StartFlow(bool withBench=false){
            if(State!=Phase.Inside&&State!=Phase.Finished&&State!=Phase.Held)return;
            StopRequested=false;
            if(State!=Phase.Held)includeBench=withBench;
            if(State==Phase.Held&&benchOwned){Bench.ResumeFlow(resume==Phase.BenchRising||resume==Phase.BenchDeparting);UpdateBenchPhase();return;}
            if(State==Phase.Held&&!Carrying&&resume==Phase.Putting&&includeBench){BeginBench();return;}
            if(benchOwned){Handoff(()=>{Bench.Shutdown();benchOwned=false;Water.EnterCurrentFromPose(false);Water.can.gameObject.SetActive(true);});}
            if(State==Phase.Inside){State=Phase.Exiting;home.RequestExit();return;}
            if(State==Phase.Held&&Carrying){
                if(resume==Phase.Watering){State=Phase.Watering;Water.StartPour();}
                else Travel(resume==Phase.ToStorage?Phase.ToStorage:Phase.ToFlowers);
            }else {if(!Water.Active)Water.EnterCurrent(false);Travel(Phase.ToCan);}
        }
        public bool TryCanApproach(out Pose result){
            var actor=home.resident.transform;var away=actor.position-storage.position;away.y=0;
            if(away.sqrMagnitude<.0001f)away=-actor.forward;
            float yaw=Mathf.Atan2(away.x,away.z)*Mathf.Rad2Deg,best=float.PositiveInfinity;result=default;
            var obstacles=new List<Bounds>();
            foreach(var filter in home.GetComponentsInChildren<MeshFilter>()){
                if(filter.transform.IsChildOf(Water.can)||filter.transform.IsChildOf(actor)||!filter.sharedMesh||!filter.sharedMesh.isReadable)continue;
                var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;
                for(int i=0;i<triangles.Length;i+=3){
                    var b=new Bounds(filter.transform.TransformPoint(vertices[triangles[i]]),Vector3.zero);
                    b.Encapsulate(filter.transform.TransformPoint(vertices[triangles[i+1]]));b.Encapsulate(filter.transform.TransformPoint(vertices[triangles[i+2]]));
                    if(b.max.y>home.groundHeight+.08f&&b.min.y<home.groundHeight+1.5f)obstacles.Add(b);
                }
            }
            foreach(float turn in new[]{0f,22.5f,-22.5f,45f,-45f,67.5f,-67.5f,90f,-90f,112.5f,-112.5f,135f,-135f,157.5f,-157.5f,180f}){
                var outward=Quaternion.Euler(0,yaw+turn,0)*Vector3.forward;
                var target=storage.position+outward*.58f;target.y=home.groundHeight;
                bool blocked=obstacles.Any(b=>target.x>b.min.x-.27f&&target.x<b.max.x+.27f&&target.z>b.min.z-.27f&&target.z<b.max.z+.27f);
                if(blocked)continue;
                var route=new RabbitWalkingRoute(actor,target,Quaternion.LookRotation(-outward).eulerAngles.y);
                for(int i=1;i<=32&&!blocked;i++){
                    var p=route.At(Mathf.Max(0,route.Length-.14f)*i/32).position;
                    blocked=obstacles.Any(b=>p.x>b.min.x-.27f&&p.x<b.max.x+.27f&&p.z>b.min.z-.27f&&p.z<b.max.z+.27f);
                    if(!Carrying){var d=p-storage.position;d.y=0;blocked|=d.magnitude<.46f;}
                }
                if(blocked)continue;
                float score=Vector3.Distance(actor.position,target)+Mathf.Abs(turn)*.001f;
                if(score<best){best=score;result=new Pose(target,Quaternion.LookRotation(-outward));}
            }
            return !float.IsPositiveInfinity(best);
        }
        float WorkReachOffset()=>Mathf.Max(0,Vector3.Dot(storage.position-home.resident.transform.position,home.resident.transform.forward)-.36f);
        bool exitApproachReady;
        internal bool TryExitDestination(out Pose pose){
            exitApproachReady=TryCanApproach(out canApproach);pose=canApproach;return exitApproachReady;
        }
        void Travel(Phase phase){
            if(phase==Phase.ToFlowers){State=phase;Water.BeginTravel(work.position,work.eulerAngles.y);return;}
            if(!TryCanApproach(out canApproach)){resume=phase;State=Phase.Held;return;}
            State=phase;Water.BeginTravel(canApproach.position,canApproach.rotation.eulerAngles.y);
        }
        public void Stop(){
            if(State==Phase.Inside||State==Phase.Finished||State==Phase.Held)return;
            resume=State;StopRequested=true;
            if(benchOwned)Bench.StopTask();else if(State!=Phase.Exiting)Water.StopTask();
        }
        public void Advance(float seconds){
            if(home.paused)return;remainder+=Mathf.Max(0,seconds)*(home.slow?.5f:1);
            while(remainder>=1f/240){remainder-=1f/240;Tick(1f/240);}
        }
        void Tick(float dt){
            if(State!=Phase.Inside&&State!=Phase.Exiting)home.AdvanceDoorOnly(dt);
            if(benchOwned){
                Bench.AdvanceFlow(dt);
                if(State==Phase.Finished||State==Phase.Held)return;
                if(StopRequested&&(Bench.State==RabbitBenchReview.TaskState.Standing||Bench.State==RabbitBenchReview.TaskState.Resting)){
                    StopRequested=false;State=Phase.Held;return;
                }
                UpdateBenchPhase();return;
            }
            if(State==Phase.Inside){home.AdvanceDoorTick(dt);return;}
            if(State==Phase.Exiting){
                home.AdvanceDoorTick(dt);
                if(home.Outside){
                    Water.EnterCurrentFromPose(false);
                    if(StopRequested)State=Phase.Held;
                    else if(exitApproachReady){State=Phase.Picking;Water.BeginPickup(storage.position,storage.rotation,WorkReachOffset());}
                    else Travel(Phase.ToCan);
                }
                return;
            }
            Water.AdvanceFlow(dt);
            if(StopRequested&&Water.State==RabbitWaterReview.TaskState.Ready){
                if(State==Phase.Picking)resume=Phase.ToFlowers;
                StopRequested=false;State=Phase.Held;return;
            }
            if(Water.State!=RabbitWaterReview.TaskState.Ready)return;
            switch(State){
                case Phase.ToCan:State=Phase.Picking;Water.BeginPickup(storage.position,storage.rotation,WorkReachOffset());break;
                case Phase.Picking:Travel(Phase.ToFlowers);break;
                case Phase.ToFlowers:State=Phase.Watering;Water.StartPour();break;
                case Phase.Watering:Travel(Phase.ToStorage);break;
                case Phase.ToStorage:State=Phase.Putting;Water.BeginPutdown(storage.position,storage.rotation,WorkReachOffset());break;
                case Phase.Putting:
                    if(includeBench)BeginBench();
                    else{State=Phase.Finished;Completed++;}break;
            }
        }
        void Handoff(Action changeOwner){
            var bones=home.resident.GetComponentsInChildren<Transform>();var points=bones.Select(t=>t.position).ToArray();
            changeOwner();for(int i=0;i<bones.Length;i++)MaxOwnerPositionJump=Mathf.Max(MaxOwnerPositionJump,Vector3.Distance(points[i],bones[i].position));
        }
        void BeginBench(){
            Handoff(()=>{Water.Shutdown();Water.can.gameObject.SetActive(true);Bench.EnterCurrent();});
            benchOwned=true;benchRested=false;State=Phase.ToBench;Bench.StartConnectedFlow(Water.can);
        }
        void UpdateBenchPhase(){
            switch(Bench.State){
                case RabbitBenchReview.TaskState.Sitting:State=Phase.BenchSitting;break;
                case RabbitBenchReview.TaskState.Resting:benchRested=true;State=Phase.BenchResting;break;
                case RabbitBenchReview.TaskState.Rising:State=Phase.BenchRising;break;
                case RabbitBenchReview.TaskState.Departing:State=Phase.BenchDeparting;break;
                case RabbitBenchReview.TaskState.Standing:
                    if(benchRested){State=Phase.Finished;Completed++;}else State=Phase.ToBench;break;
                default:State=Phase.ToBench;break;
            }
        }
        public void Pose(bool pickup,int index){
            home.SelectLifeFlow();home.resident.transform.SetPositionAndRotation(approach.position,approach.rotation);
            Water.EnterCurrent(!pickup);Water.PoseWork(pickup,index,storage.position,storage.rotation);
            State=pickup?Phase.Picking:Phase.Putting;
        }
        public void DrawPanel(ref float y,float width,GUIStyle button,GUIStyle label){
            GUI.Label(new Rect(0,y,width,50),Label+"\n물뿌리개: "+(Water.Active&&Carrying?"운반 중":"보관/집기")+" · 완료 "+Completed,label);y+=50;
            if(GUI.Button(new Rect(0,y,width,24),"전체 흐름 · 외출해서 물 주기",button)){StartFlow();home.paused=false;}y+=29;
            if(GUI.Button(new Rect(0,y,width,24),"전체 생활 · 물 주기 후 벤치 휴식",button)){StartFlow(true);home.paused=false;}y+=29;
            if(GUI.Button(new Rect(0,y,width,24),"정지 · 현재 위치에서 대기",button))Stop();y+=29;
            if(GUI.Button(new Rect(0,y,width,24),"초기화 · 실내 / 물뿌리개 원위치",button))home.ResetLifeFlow();y+=29;
            if(GUI.Button(new Rect(0,y,width/3-3,24),home.paused?"재개":"일시정지",button))home.paused=!home.paused;
            if(GUI.Button(new Rect(width/3,y,width/3-3,24),"0.5×",button))home.slow=true;
            if(GUI.Button(new Rect(2*width/3,y,width/3-3,24),"1×",button))home.slow=false;y+=29;
            foreach(bool pick in new[]{true,false}){
                GUI.Label(new Rect(0,y,width,24),pick?"집기 · 주요 8포즈":"내려놓기 · 주요 8포즈",label);y+=25;
                for(int i=0;i<8;i++){if(GUI.Button(new Rect((i%4)*(width/4),y+(i/4)*27,width/4-3,24),(i+1).ToString(),button))Pose(pick,i);}y+=56;
            }
            if(GUI.Button(new Rect(0,y,width,24),"물 주기 구도",button))home.ViewWater(130);y+=29;
            foreach(float angle in new[]{0f,45f,90f}){
                int column=angle==0?0:angle==45?1:2;
                if(GUI.Button(new Rect(column*width/3,y,width/3-3,24),column==0?"전체 · 정면":column==1?"전체 · 비스듬":"전체 · 측면",button))home.ViewLifeFlow(angle);
            }y+=29;
            GUI.Label(new Rect(0,y,width,50),"외출 → 물 주기 → 내려놓기"+(includeBench?" → 벤치 휴식":"")+"\n자동 귀가/물 채우기는 하지 않음",label);y+=50;
        }
    }
}
