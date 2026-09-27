using System;
using System.Linq;
using UnityEngine;
namespace TinyDays.Review {
    // Parallel, short recovery of the displaced foot; the support foot stays fixed.
    public sealed class IdleLiftRecovery {
        readonly GameObject actor;readonly AnimationClip clip;readonly IdleSupportLeg[] legs;
        readonly Vector3[] ground,startHeel,velocity,angularVelocity;
        readonly Quaternion[] neutral,startRotation;
        readonly bool[] moving;
        public float Duration=>.4f;
        public Vector3 GroundPoint(int foot)=>ground[foot];
        public bool Fixed(int foot,float time)=>!moving[foot]||time>=Duration;
        public IdleLiftRecovery(GameObject actor,AnimationClip clip,IdleSupportLeg[] legs){
            this.actor=actor;this.clip=clip;this.legs=legs;
            ground=legs.Select(l=>l.End.TransformPoint(l.HeelLocal)).ToArray();neutral=legs.Select(l=>l.End.rotation).ToArray();
            startHeel=new Vector3[legs.Length];startRotation=new Quaternion[legs.Length];velocity=new Vector3[legs.Length];angularVelocity=new Vector3[legs.Length];moving=new bool[legs.Length];
        }
        public void Begin(double sourceTime){
            var bones=actor.GetComponentsInChildren<Transform>();var p=bones.Select(b=>b.localPosition).ToArray();var q=bones.Select(b=>b.localRotation).ToArray();
            for(int i=0;i<legs.Length;i++){startHeel[i]=legs[i].End.TransformPoint(legs[i].HeelLocal);startRotation[i]=legs[i].End.rotation;velocity[i]=angularVelocity[i]=Vector3.zero;}
            double delta=Math.Min(.001,sourceTime);
            if(delta>0){
                clip.SampleAnimation(actor,(float)(sourceTime-delta));
                for(int i=0;i<legs.Length;i++){
                    velocity[i]=(startHeel[i]-legs[i].End.TransformPoint(legs[i].HeelLocal))/(float)delta;
                    var rotation=startRotation[i]*Quaternion.Inverse(legs[i].End.rotation);rotation.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
                    angularVelocity[i]=Mathf.Abs(angle)<1e-5f?Vector3.zero:axis*angle/(float)delta;
                }
            }
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=p[i];bones[i].localRotation=q[i];}
            for(int i=0;i<legs.Length;i++)moving[i]=Vector3.Distance(startHeel[i],ground[i])>.0001f||Quaternion.Angle(startRotation[i],neutral[i])>.1f;
        }
        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*t*(10+t*(-15+6*t));}
        static float Tangent(float time,float duration){float u=Mathf.Clamp01(time/duration);return duration*(u-6*u*u*u+8*u*u*u*u-3*u*u*u*u*u);}
        public void Apply(float time){
            float u=Mathf.Clamp01(time/Duration),e=Ease(u);
            for(int i=0;i<legs.Length;i++){
                var leg=legs[i];Vector3 heel=startHeel[i];Quaternion rotation=startRotation[i];
                if(moving[i]){
                    heel=Vector3.Lerp(startHeel[i],ground[i],e)+velocity[i]*Tangent(time,.1f);
                    float lift=u<.5f?Ease(u*2):1-Ease((u-.5f)*2);
                    heel.y+=.003f*lift;
                    rotation=Quaternion.Slerp(startRotation[i],neutral[i],Ease((u-.1f)/.8f));
                    Vector3 spin=angularVelocity[i];rotation=Quaternion.AngleAxis(spin.magnitude*Tangent(time,.08f),spin.sqrMagnitude>1e-12f?spin.normalized:Vector3.up)*rotation;
                    // Use actual sole points to prevent descending toe momentum
                    // from putting the shoe through the floor. Bone lengths stay fixed.
                    float sole=leg.SoleLocal.Length==0?0:leg.SoleLocal.Min(p=>(rotation*Vector3.Scale(p-leg.HeelLocal,leg.End.lossyScale)).y);
                    heel.y=Mathf.Max(heel.y,ground[i].y-sole);
                }
                Vector3 target=heel-rotation*Vector3.Scale(leg.HeelLocal,leg.End.lossyScale);
                leg.Solve(target,rotation,actor.transform.forward);
            }
        }
        public bool Supported(){return legs.Select((l,i)=>{var heel=l.End.TransformPoint(l.HeelLocal);float y=heel.y-actor.transform.position.y;return Vector3.Distance(heel,ground[i])<=.0035f&&Quaternion.Angle(l.End.rotation,neutral[i])<.15f&&y>=-.0005f&&y<=.005f;}).All(x=>x);}
    }
}
