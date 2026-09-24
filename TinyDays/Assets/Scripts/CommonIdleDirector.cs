using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review {
    // Body-family controller: no animal, ear or accessory names are required.
    public sealed class CommonIdleDirector {
        public enum Stage { Breathing, Variation, Tidying, Rising, Ready }
        public enum Departure { None, Walk, Run }
        public sealed class Variation {
            public string Id; public bool Seated; public AnimationClip Clip;
            // Future authored actions supply a safe planted recovery pose. Never
            // infer an airborne foot's contact from the elapsed recovery timer.
            public AnimationClip Recovery; public float RecoverySeconds=.25f;
            public bool LiftsFeet;
            public Action<float> RecoverFeet;
            public Func<bool> FeetSupported;
        }
        readonly GameObject actor;
        readonly AnimationClip standBreath,sitBreath,standUp,standReference,sitReference;
        readonly Transform[] bones;
        readonly Vector3[] fromPosition;
        readonly Quaternion[] fromRotation;
        readonly System.Random random;
        readonly Action standingDeparturePose;
        readonly IdleSupportLeg[] supports;
        readonly Vector3[] supportStart;
        readonly Quaternion[] supportRotation;
        readonly Vector3[] supportTargets;
        readonly Quaternion[] supportTargetRotation;
        readonly List<Variation> variants=new List<Variation>();
        Variation current;
        string previous;
        double clock,wait;
        bool finishRising;
        public Stage State {get;private set;}
        public Departure Pending {get;private set;}
        public bool Seated {get;private set;}
        public bool Automatic=true;
        public double Time => clock;
        public double Wait => wait;
        public double UnusedTime {get;private set;}
        public int CandidateCount => variants.Count(v=>v.Seated==Seated);
        public IEnumerable<string> Candidates => variants.Where(v=>v.Seated==Seated).Select(v=>v.Id);
        public string CurrentId => current?.Id??"기본 호흡";

        public CommonIdleDirector(GameObject actor,AnimationClip standing,AnimationClip sitting,AnimationClip rise,
            AnimationClip standingReference,AnimationClip seatedReference,bool seated,int seed=173,Action standingDeparturePose=null,IdleSupportLeg[] supports=null) {
            this.actor=actor;standBreath=standing;sitBreath=sitting;standUp=rise;
            this.standingDeparturePose=standingDeparturePose;
            this.supports=(supports??Array.Empty<IdleSupportLeg>()).Where(s=>s.Upper&&s.Lower&&s.End).ToArray();
            supportStart=new Vector3[this.supports.Length];supportRotation=new Quaternion[this.supports.Length];
            supportTargets=new Vector3[this.supports.Length];supportTargetRotation=new Quaternion[this.supports.Length];
            standReference=standingReference;sitReference=seatedReference;Seated=seated;
            // Include unweighted root/neck/socket transforms too. Omitting the
            // root would snap its authored floor clearance at the first blend.
            bones=actor.GetComponentsInChildren<Transform>().Where(b=>b!=actor.transform).OrderBy(b=>Depth(b)).ToArray();
            fromPosition=new Vector3[bones.Length];fromRotation=new Quaternion[bones.Length];random=new System.Random(seed);
            State=Stage.Breathing;ResetWait();Sample();
        }
        static int Depth(Transform t){int n=0;while(t.parent){n++;t=t.parent;}return n;}
        void ResetWait(){wait=6+random.NextDouble()*6;}
        public void Register(Variation variation){
            if(variation==null||!variation.Clip||string.IsNullOrEmpty(variation.Id)||variants.Any(v=>v.Id==variation.Id))throw new ArgumentException("Completed unique clip required");
            if(variation.LiftsFeet&&(variation.RecoverFeet==null||variation.FeetSupported==null))throw new ArgumentException("Lifted feet require authored recovery and measured contact adapters");
            variants.Add(variation);
        }
        public bool Play(string id){
            if(State!=Stage.Breathing)return false;
            var v=variants.Find(x=>x.Id==id&&x.Seated==Seated);if(v==null)return false;
            current=v;previous=v.Id;State=Stage.Variation;clock=0;Sample();return true;
        }
        void Capture(){for(int i=0;i<bones.Length;i++){fromPosition[i]=bones[i].localPosition;fromRotation[i]=bones[i].localRotation;}for(int i=0;i<supports.Length;i++){supportStart[i]=supports[i].End.position;supportRotation[i]=supports[i].End.rotation;}}
        public void Request(Departure departure){
            Pending=departure;
            if(State==Stage.Tidying||State==Stage.Rising||State==Stage.Ready)return;
            Capture();State=Stage.Tidying;clock=0;
        }
        public void Cancel(){if(Pending!=Departure.None&&State==Stage.Tidying){Capture();clock=0;}Pending=Departure.None;/* A started rise must still reach safe standing. */}
        public void Rise(){if(!Seated||State==Stage.Rising)return;finishRising=true;Request(Departure.None);}
        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*t*(10+t*(-15+6*t));}
        public void Advance(float delta){
            // Consume boundaries exactly, independent of caller frame rate.
            double remaining=Math.Max(0,delta);UnusedTime=0;
            do {
                double limit=State==Stage.Tidying?Math.Max(.25,current?.RecoverySeconds??.25):State==Stage.Rising?standUp.length:State==Stage.Variation?current.Clip.length:double.PositiveInfinity;
                double step=Math.Min(remaining,Math.Max(0,limit-clock));
                if(State==Stage.Breathing&&Automatic&&CandidateCount>0)step=Math.Min(step,Math.Max(0,wait));
                clock+=step;remaining-=step;
                if(State==Stage.Breathing){wait-=step;if(Automatic&&wait<=0&&CandidateCount>0){var choices=variants.Where(v=>v.Seated==Seated).ToArray();if(choices.Length>1)choices=choices.Where(v=>v.Id!=previous).ToArray();Play(choices[random.Next(choices.Length)].Id);}}
                Sample();
                if(clock+1e-7>=limit){
                    if(State==Stage.Tidying){
                        if(current!=null&&current.LiftsFeet&&!current.FeetSupported()){UnusedTime=remaining;return;}
                        if(Seated&&(Pending!=Departure.None||finishRising)){State=Stage.Rising;clock=0;current=null;}
                        else if(Pending!=Departure.None){State=Stage.Ready;UnusedTime=remaining;return;}
                        else {State=Stage.Breathing;clock=0;current=null;ResetWait();}
                    } else if(State==Stage.Rising){
                        Seated=false;finishRising=false;clock=0;current=null;
                        Capture();State=Stage.Tidying;
                    } else if(State==Stage.Variation){current=null;State=Stage.Breathing;clock=0;ResetWait();}
                    Sample();
                }
                if(remaining<1e-8||State==Stage.Ready)break;
            }while(true);
        }
        public void Sample(){
            if(State==Stage.Breathing){var clip=Seated?sitBreath:standBreath;clip.SampleAnimation(actor,(float)(clock%clip.length));}
            else if(State==Stage.Variation)current.Clip.SampleAnimation(actor,(float)clock);
            else if(State==Stage.Rising)standUp.SampleAnimation(actor,(float)Math.Min(clock,standUp.length));
            else if(State==Stage.Tidying){
                var reference=current?.Recovery??(Seated?sitReference:standReference);
                if(!Seated&&Pending!=Departure.None&&standingDeparturePose!=null)standingDeparturePose();else reference.SampleAnimation(actor,0);
                float blend=Ease((float)(clock/Math.Max(.25,current?.RecoverySeconds??.25)));
                for(int i=0;i<supports.Length;i++){supportTargets[i]=Vector3.Lerp(supportStart[i],supports[i].End.position,blend);supportTargetRotation[i]=Quaternion.Slerp(supportRotation[i],supports[i].End.rotation,blend);}
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.LerpUnclamped(fromPosition[i],bones[i].localPosition,blend);bones[i].localRotation=Quaternion.SlerpUnclamped(fromRotation[i],bones[i].localRotation,blend);}
                for(int i=0;i<supports.Length;i++)supports[i].Solve(supportTargets[i],supportTargetRotation[i],actor.transform.forward);
                current?.RecoverFeet?.Invoke((float)clock);
            }
        }
    }

    [Serializable]
    public sealed class IdleSupportLeg {
        public Transform Upper,Lower,End;
        public void Solve(Vector3 target,Quaternion rotation,Vector3 forward){
            Vector3 a=Upper.position,b=Lower.position,c=End.position,delta=target-a;
            float l1=Vector3.Distance(a,b),l2=Vector3.Distance(b,c),distance=delta.magnitude;
            if(distance<1e-6f||l1<1e-6f||l2<1e-6f)return;
            Vector3 axis=delta/distance,pole=b-a;pole-=axis*Vector3.Dot(pole,axis);
            if(pole.sqrMagnitude<1e-8f)pole=forward-axis*Vector3.Dot(forward,axis);
            float d=Mathf.Clamp(distance,Mathf.Abs(l1-l2)+1e-6f,l1+l2-1e-6f);
            float along=(l1*l1-l2*l2+d*d)/(2*d);
            Vector3 knee=a+axis*along+pole.normalized*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            Upper.rotation=Quaternion.FromToRotation(b-a,knee-a)*Upper.rotation;
            Lower.rotation=Quaternion.FromToRotation(End.position-Lower.position,target-Lower.position)*Lower.rotation;
            End.rotation=rotation;
        }
    }

    [Serializable]
    public sealed class IdleSecondaryMotion {
        [Serializable] public sealed class Part {public Transform Joint;public Vector3 LocalAxis=Vector3.right;public float Degrees=.35f;public float Lag=.25f;}
        public string BodyFamily="AdultStandard_v2";
        public float Strength=1;
        public Part[] Parts=Array.Empty<Part>();
        public IdleSupportLeg[] Supports=Array.Empty<IdleSupportLeg>();
        // Called after a fresh body sample; offsets never accumulate. Missing
        // ears/tails/accessories are simply absent from the profile.
        public void Apply(double time,float weight){foreach(var part in Parts)if(part.Joint)part.Joint.localRotation*=Quaternion.AngleAxis(part.Degrees*Strength*weight*Mathf.Sin((float)time*Mathf.PI*.5f-part.Lag),part.LocalAxis);}
    }
}
