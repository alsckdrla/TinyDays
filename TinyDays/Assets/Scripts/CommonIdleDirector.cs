using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review {
    // Body-family controller: no animal, ear or accessory names are required.
    public sealed class CommonIdleDirector {
        public enum Stage { Breathing, Variation, Tidying, Rising, Ready, Entering, Leaving }
        public enum Departure { None, Walk, Run }
        public sealed class Variation {
            public string Id; public bool Seated; public AnimationClip Clip;
            // Future authored actions supply a safe planted recovery pose. Never
            // infer an airborne foot's contact from the elapsed recovery timer.
            public AnimationClip Recovery; public float RecoverySeconds=.25f;
            public bool LiftsFeet;
            public bool HeelSupport;
            public Action<float> RecoverFeet;
            public Action<float> RecoverPose;
            public Action<double> BeginRecovery;
            public Func<float> RecoveryDuration;
            public Func<bool> FeetSupported;
            public Action<double> Secondary;
        }
        readonly GameObject actor;
        readonly AnimationClip standBreath,sitBreath,standUp,standReference,sitReference;
        readonly Transform[] bones;
        readonly Vector3[] fromPosition;
        readonly Quaternion[] fromRotation;
        readonly Vector3[] fromVelocity,fromAngularVelocity,bridgePosition,probePosition;
        readonly Quaternion[] bridgeRotation,probeRotation;
        readonly Action<double,float> secondary;
        double entryBreathTime;
        public const float BlendSeconds=.25f;
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
        public AnimationClip CurrentClip => current?.Clip;
        public float VariationDuration => current?.Clip.length??0;
        public bool IsVariation => State==Stage.Entering||State==Stage.Variation||State==Stage.Leaving;

        public CommonIdleDirector(GameObject actor,AnimationClip standing,AnimationClip sitting,AnimationClip rise,
            AnimationClip standingReference,AnimationClip seatedReference,bool seated,int seed=173,Action standingDeparturePose=null,IdleSupportLeg[] supports=null,Action<double,float> secondary=null) {
            this.actor=actor;standBreath=standing;sitBreath=sitting;standUp=rise;
            this.standingDeparturePose=standingDeparturePose;
            this.secondary=secondary;
            this.supports=(supports??Array.Empty<IdleSupportLeg>()).Where(s=>s.Upper&&s.Lower&&s.End).ToArray();
            supportStart=new Vector3[this.supports.Length];supportRotation=new Quaternion[this.supports.Length];
            supportTargets=new Vector3[this.supports.Length];supportTargetRotation=new Quaternion[this.supports.Length];
            standReference=standingReference;sitReference=seatedReference;Seated=seated;
            // Include unweighted root/neck/socket transforms too. Omitting the
            // root would snap its authored floor clearance at the first blend.
            bones=actor.GetComponentsInChildren<Transform>().Where(b=>b!=actor.transform).OrderBy(b=>Depth(b)).ToArray();
            fromPosition=new Vector3[bones.Length];fromRotation=new Quaternion[bones.Length];fromVelocity=new Vector3[bones.Length];fromAngularVelocity=new Vector3[bones.Length];bridgePosition=new Vector3[bones.Length];bridgeRotation=new Quaternion[bones.Length];probePosition=new Vector3[bones.Length];probeRotation=new Quaternion[bones.Length];random=new System.Random(seed);
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
            entryBreathTime=clock;current=v;previous=v.Id;State=Stage.Entering;clock=0;Sample();return true;
        }
        void Capture(){for(int i=0;i<bones.Length;i++){fromPosition[i]=bones[i].localPosition;fromRotation[i]=bones[i].localRotation;fromVelocity[i]=fromAngularVelocity[i]=Vector3.zero;}for(int i=0;i<supports.Length;i++){supportStart[i]=current?.HeelSupport==true?supports[i].End.TransformPoint(supports[i].HeelLocal):supports[i].End.position;supportRotation[i]=supports[i].End.rotation;}}
        void CaptureMotion(){
            double now=clock,step=Math.Min(.001,clock);if(step<1e-7){Capture();return;}
            clock=now-step;Sample();for(int i=0;i<bones.Length;i++){probePosition[i]=bones[i].localPosition;probeRotation[i]=bones[i].localRotation;}
            clock=now;Sample();Capture();
            for(int i=0;i<bones.Length;i++){
                fromVelocity[i]=(fromPosition[i]-probePosition[i])/(float)step;
                var q=fromRotation[i]*Quaternion.Inverse(probeRotation[i]);q.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
                fromAngularVelocity[i]=Mathf.Abs(angle)<1e-5f?Vector3.zero:axis*(angle/(float)step);
            }
        }
        public void Request(Departure departure){
            Pending=departure;
            if(State==Stage.Tidying||State==Stage.Rising||State==Stage.Ready)return;
            double sourceTime=State==Stage.Variation?clock:State==Stage.Leaving?current.Clip.length:0;
            if(IsVariation)CaptureMotion();else Capture();current?.BeginRecovery?.Invoke(sourceTime);State=Stage.Tidying;clock=0;
        }
        public void Cancel(){if(Pending!=Departure.None&&State==Stage.Tidying&&current?.BeginRecovery==null){Capture();clock=0;}Pending=Departure.None;/* Finish the current landing/foot arrangement before changing destination. */}
        public void Rise(){if(!Seated||State==Stage.Rising)return;finishRising=true;Request(Departure.None);}
        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*t*(10+t*(-15+6*t));}
        public void Advance(float delta){
            // Consume boundaries exactly, independent of caller frame rate.
            double remaining=Math.Max(0,delta);UnusedTime=0;
            do {
                double limit=State==Stage.Tidying?Math.Max(.25,current?.RecoveryDuration?.Invoke()??current?.RecoverySeconds??.25):State==Stage.Rising?standUp.length:State==Stage.Variation?current.Clip.length:State==Stage.Entering||State==Stage.Leaving?BlendSeconds:double.PositiveInfinity;
                double step=Math.Min(remaining,Math.Max(0,limit-clock));
                if(State==Stage.Breathing&&Automatic&&CandidateCount>0)step=Math.Min(step,Math.Max(0,wait));
                clock+=step;remaining-=step;
                if(State==Stage.Breathing){wait-=step;if(Automatic&&wait<=0&&CandidateCount>0){var choices=variants.Where(v=>v.Seated==Seated).ToArray();if(choices.Length>1)choices=choices.Where(v=>v.Id!=previous).ToArray();Play(choices[random.Next(choices.Length)].Id);}}
                Sample();
                if(clock+1e-7>=limit){
                    if(State==Stage.Tidying){
                        if(current!=null&&(current.LiftsFeet||current.HeelSupport)&&current.FeetSupported!=null&&!current.FeetSupported()){UnusedTime=remaining;return;}
                        if(Seated&&(Pending!=Departure.None||finishRising)){State=Stage.Rising;clock=0;current=null;}
                        else if(Pending!=Departure.None){State=Stage.Ready;UnusedTime=remaining;return;}
                        else {State=Stage.Breathing;clock=0;current=null;ResetWait();}
                    } else if(State==Stage.Rising){
                        Seated=false;finishRising=false;clock=0;current=null;
                        Capture();State=Stage.Tidying;
                    } else if(State==Stage.Entering){State=Stage.Variation;clock=0;}
                    else if(State==Stage.Variation){State=Stage.Leaving;clock=0;}
                    else if(State==Stage.Leaving){current=null;State=Stage.Breathing;clock=BlendSeconds;ResetWait();}
                    Sample();
                }
                if(remaining<1e-8||State==Stage.Ready)break;
            }while(true);
        }
        public void SetBreathingPhase(double phase){
            if(State!=Stage.Breathing)throw new InvalidOperationException("Breathing phase applies to a breathing state");
            clock=(phase-Math.Floor(phase))*4;Sample();
        }
        public void Sample(){
            if(State==Stage.Breathing)SampleBreath(clock);
            else if(State==Stage.Variation)SampleVariation(clock);
            else if(State==Stage.Entering||State==Stage.Leaving){
                if(State==Stage.Entering)SampleBreath(entryBreathTime+clock);else SampleVariation(current.Clip.length);
                for(int i=0;i<bones.Length;i++){bridgePosition[i]=bones[i].localPosition;bridgeRotation[i]=bones[i].localRotation;}
                if(State==Stage.Entering)SampleVariation(0);else SampleBreath(clock);
                float w=Ease((float)clock/BlendSeconds);
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.LerpUnclamped(bridgePosition[i],bones[i].localPosition,w);bones[i].localRotation=Quaternion.SlerpUnclamped(bridgeRotation[i],bones[i].localRotation,w);}
            }
            else if(State==Stage.Rising)standUp.SampleAnimation(actor,(float)Math.Min(clock,standUp.length));
            else if(State==Stage.Tidying){
                var reference=current?.Recovery??(Seated?sitReference:standReference);
                if(!Seated&&Pending!=Departure.None&&standingDeparturePose!=null)standingDeparturePose();else reference.SampleAnimation(actor,0);
                float duration=(float)Math.Max(.25,current?.RecoveryDuration?.Invoke()??current?.RecoverySeconds??.25),t=Mathf.Clamp01((float)clock/duration),blend=Ease(t);
                float tangent=duration*(t-6*t*t*t+8*t*t*t*t-3*t*t*t*t*t);
                if(current?.BeginRecovery!=null){float u=Mathf.Clamp01((float)clock/.25f);tangent=.25f*(u-6*u*u*u+8*u*u*u*u-3*u*u*u*u*u);}
                for(int i=0;i<supports.Length;i++){var leg=supports[i];bool heel=current?.HeelSupport==true;supportTargetRotation[i]=Quaternion.Slerp(supportRotation[i],leg.End.rotation,blend);var contact=Vector3.Lerp(supportStart[i],heel?leg.End.TransformPoint(leg.HeelLocal):leg.End.position,blend);supportTargets[i]=heel?contact-supportTargetRotation[i]*Vector3.Scale(leg.HeelLocal,leg.End.lossyScale):contact;}
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.LerpUnclamped(fromPosition[i],bones[i].localPosition,blend)+fromVelocity[i]*tangent;var spin=fromAngularVelocity[i];bones[i].localRotation=Quaternion.AngleAxis(spin.magnitude*tangent,spin.sqrMagnitude>1e-12f?spin.normalized:Vector3.up)*Quaternion.SlerpUnclamped(fromRotation[i],bones[i].localRotation,blend);}
                for(int i=0;i<supports.Length;i++)supports[i].Solve(supportTargets[i],supportTargetRotation[i],actor.transform.forward);
                current?.RecoverFeet?.Invoke((float)clock);
                current?.RecoverPose?.Invoke((float)clock);
            }
        }
        void SampleBreath(double time){var clip=Seated?sitBreath:standBreath;clip.SampleAnimation(actor,(float)(time%clip.length));secondary?.Invoke(time,1);}
        void SampleVariation(double time){current.Clip.SampleAnimation(actor,(float)Math.Min(time,current.Clip.length));current.Secondary?.Invoke(time);}
    }

    [Serializable]
    public sealed class IdleSupportLeg {
        public Transform Upper,Lower,End;
        public Vector3 HeelLocal;
        [NonSerialized] public Vector3[] SoleLocal=Array.Empty<Vector3>();
        public void Solve(Vector3 target,Quaternion rotation,Vector3 forward,Vector3? authoredBend=null){
            Vector3 a=Upper.position,b=Lower.position,c=End.position,delta=target-a;
            float l1=Vector3.Distance(a,b),l2=Vector3.Distance(b,c),distance=delta.magnitude;
            if(distance<1e-6f||l1<1e-6f||l2<1e-6f)return;
            Vector3 axis=delta/distance,pole=authoredBend??(b-a);pole-=axis*Vector3.Dot(pole,axis);
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
        Quaternion[] fidgetSource,fidgetApplied;
        bool fidgetAppliedValid;
        public void RestoreFidget(){
            if(!fidgetAppliedValid)return;
            for(int i=0;i<Parts.Length;i++)if(Parts[i].Joint&&Quaternion.Angle(Parts[i].Joint.localRotation,fidgetApplied[i])<.001f)Parts[i].Joint.localRotation=fidgetSource[i];
            fidgetAppliedValid=false;
        }
        public void ApplyFidget(double time,float weight){
            // Constant imported curves can be omitted by Unity. Remove our
            // previous optional offset before sampling another one.
            RestoreFidget();
            if(fidgetSource==null||fidgetSource.Length!=Parts.Length){fidgetSource=new Quaternion[Parts.Length];fidgetApplied=new Quaternion[Parts.Length];}
            for(int i=0;i<Parts.Length;i++)if(Parts[i].Joint)fidgetSource[i]=Parts[i].Joint.localRotation;
            Apply(time,weight);
            for(int i=0;i<Parts.Length;i++)if(Parts[i].Joint)fidgetApplied[i]=Parts[i].Joint.localRotation;
            fidgetAppliedValid=true;
        }
        // Called after a fresh body sample; offsets never accumulate. Missing
        // ears/tails/accessories are simply absent from the profile.
        public void Apply(double time,float weight){foreach(var part in Parts)if(part.Joint)part.Joint.localRotation*=Quaternion.AngleAxis(part.Degrees*Strength*weight*Mathf.Sin((float)time*Mathf.PI*.5f-part.Lag),part.LocalAxis);}
        static float SighEase(float t){t=Mathf.Clamp01(t);return t*t*t*(10+t*(-15+6*t));}
        public void ApplySigh(double time){
            float t=Mathf.Clamp((float)time,0,5);
            foreach(var part in Parts)if(part.Joint){
                float delayed=Mathf.Max(0,t-Mathf.Clamp(part.Lag*.25f,0,.1f));
                float weight=delayed<=1f?.35f*SighEase(delayed):delayed<=2f?.35f-1.35f*SighEase(delayed-1f):-(1-SighEase((delayed-2f)/3f));
                weight*=1-SighEase((t-4.7f)/.3f);
                part.Joint.localRotation*=Quaternion.AngleAxis(part.Degrees*Strength*weight,part.LocalAxis);
            }
        }
    }
}
