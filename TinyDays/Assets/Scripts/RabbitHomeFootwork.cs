using System;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review
{
    // Short, planted-foot corrective steps for a known flat review floor.
    // The original locomotion clips and bone translations are not authored here.
    public sealed class RabbitHomeFootwork
    {
        readonly Transform actor;
        readonly Transform pelvis;
        readonly IdleSupportLeg[] legs=new IdleSupportLeg[2];
        readonly Vector3[] rest=new Vector3[2],target=new Vector3[2];
        readonly Quaternion[] restRotation=new Quaternion[2],rotation=new Quaternion[2];
        readonly Vector3[][] soles=new Vector3[2][];
        readonly float floor;
        readonly float neutralLowering;
        Func<float,Pose> route;
        int steps,segment=-1,swing;
        float duration;
        bool turnOnly;
        Vector3 from,to;
        Quaternion fromRotation,toRotation;
        public bool[] Planted {get;}={true,true};
        public float MaxReachError {get;private set;}
        public string ReachDetail {get;private set;}
        public float Duration=>duration;
        public float RestingPelvisDrop=>neutralLowering;
        static float Travel(float u){u=Mathf.Clamp01(u);const float a=.15f;if(u<a)return .5f*(u-a/Mathf.PI*Mathf.Sin(Mathf.PI*u/a))/(1-a);if(u>1-a)return 1-Travel(1-u);return (u-a*.5f)/(1-a);}
        public float MotionFraction(float time)=>turnOnly?Ease(time/duration):Travel(Mathf.Clamp01(time/duration*(steps+2)/steps));
        public RabbitHomeFootwork(GameObject resident,float floorHeight)
        {
            actor=resident.transform;floor=floorHeight;
            var bones=resident.GetComponentsInChildren<Transform>();
            pelvis=bones.First(t=>t.name=="Pelvis");
            var skin=resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Shoes");
            var m=skin.sharedMesh;var v=m.vertices;var weights=m.boneWeights;
            for(int i=0;i<2;i++){
                string suffix=i==0?"L":"R";
                legs[i]=new IdleSupportLeg{Upper=bones.First(t=>t.name=="Thigh_"+suffix),Lower=bones.First(t=>t.name=="Shin_"+suffix),End=bones.First(t=>t.name=="Foot_"+suffix)};
                rest[i]=actor.InverseTransformPoint(legs[i].End.position);
                restRotation[i]=Quaternion.Inverse(actor.rotation)*legs[i].End.rotation;
                int bi=Array.FindIndex(skin.bones,b=>b==legs[i].End);
                soles[i]=Enumerable.Range(0,v.Length).Where(j=>weights[j].boneIndex0==bi&&weights[j].weight0>.99f).Select(j=>m.bindposes[bi].MultiplyPoint3x4(v[j])).ToArray();
            }
            neutralLowering=Mathf.Max(SoleHeight(0),SoleHeight(1))-.0025f;
            Capture();
            for(int i=0;i<2;i++)target[i]=Ground(i,target[i],rotation[i]);
        }
        static float Ease(float u){u=Mathf.Clamp01(u);return u*u*u*(u*(u*6-15)+10);}
        Vector3 Ground(int i,Vector3 p,Quaternion q)
        {
            float low=soles[i].Min(v=>(q*Vector3.Scale(legs[i].End.lossyScale,v)).y);
            p.y=floor+.0025f-low;return p;
        }
        public void Capture(){for(int i=0;i<2;i++){target[i]=legs[i].End.position;rotation[i]=legs[i].End.rotation;Planted[i]=true;}}
        public void Begin(Func<float,Pose> path,int movingSteps,float secondsPerStep=.28f)
        {
            turnOnly=false;Capture();route=path;steps=Mathf.Max(2,movingSteps);duration=(steps+2)*secondsPerStep;segment=-1;
        }
        public void BeginTurn(float yaw,int footfalls,float secondsPerStep){
            Capture();turnOnly=true;steps=footfalls;duration=steps*secondsPerStep;segment=-1;
            var start=actor.rotation;var end=Quaternion.Euler(0,yaw,0);var position=actor.position;
            route=u=>new Pose(position,Quaternion.Slerp(start,end,u));
        }
        public void PlaceRoot(float time)
        {
            var p=route(MotionFraction(time));
            actor.SetPositionAndRotation(p.position,p.rotation);
        }
        public void Apply(float time)
        {
            int total=turnOnly?steps:steps+2;
            float s=Mathf.Clamp(time/duration*total,0,total-.00001f);
            int index=Mathf.FloorToInt(s);float u=s-index;
            if(segment!=index){
                if(segment>=0){target[swing]=to;rotation[swing]=toRotation;}
                segment=index;swing=index%2;
                // Land ahead of the moving body so the planted interval spans
                // either side of the hip, as it does in the normal walk.
                var p=route(turnOnly?Mathf.Min((index+1f)/(steps-1),1):Travel(Mathf.Min((index+1.5f)/steps,1)));
                from=target[swing];fromRotation=rotation[swing];
                toRotation=p.rotation*restRotation[swing];to=Ground(swing,p.position+p.rotation*rest[swing],toRotation);
            }
            Planted[0]=Planted[1]=true;Planted[swing]=u>=.999f;
            rotation[swing]=Quaternion.Slerp(fromRotation,toRotation,Ease(u));
            target[swing]=Vector3.Lerp(from,to,Ease(u));
            // Even during yaw, use the actual shoe bottom, not its ankle height.
            var level=Ground(swing,target[swing],rotation[swing]);
            target[swing].y=level.y+.022f*Mathf.Pow(Mathf.Sin(Mathf.PI*u),2);
            pelvis.position-=Vector3.up*((turnOnly?.035f:.018f)*Ease(time/.25f)*Ease((duration-time)/.25f));
            Solve();
        }
        public void Hold(){Planted[0]=Planted[1]=true;Solve();}
        void Solve(){
            // Match the source's resting shoe clearance to this floor once;
            // do not ask the knee solver to stretch a straight idle leg.
            pelvis.position-=Vector3.up*neutralLowering;
            for(int i=0;i<2;i++){
                float length=Vector3.Distance(legs[i].Upper.position,legs[i].Lower.position)+Vector3.Distance(legs[i].Lower.position,legs[i].End.position);
                float error=Vector3.Distance(legs[i].Upper.position,target[i])-length;
                if(error>MaxReachError){MaxReachError=error;ReachDetail=$"segment {segment}/{steps} foot {i} actor {actor.position:F3} hip {legs[i].Upper.position:F3} target {target[i]:F3} len {length:F3}";}
                legs[i].Solve(target[i],rotation[i],actor.forward,actor.forward);
            }
        }
        public Vector3 FootPosition(int i)=>legs[i].End.position;
        public float SoleHeight(int i)=>soles[i].Min(v=>legs[i].End.TransformPoint(v).y)-floor;
    }
}
