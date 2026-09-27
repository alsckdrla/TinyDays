using System;
using System.Linq;
using UnityEngine;
namespace TinyDays.Review {
    // Optional body-family hand adapter. No animal/accessory names are required.
    public sealed class IdleHandRecovery {
        readonly GameObject actor;readonly AnimationClip clip;readonly IdleSupportLeg arm;
        Vector3 start,velocity;Quaternion rotation;
        public const float Duration=.3f;
        public IdleHandRecovery(GameObject actor,AnimationClip clip,Transform shoulder,Transform elbow,Transform hand){
            this.actor=actor;this.clip=clip;arm=new IdleSupportLeg{Upper=shoulder,Lower=elbow,End=hand};
        }
        public void Begin(double time){
            start=arm.End.position;rotation=arm.End.rotation;velocity=Vector3.zero;
            float dt=(float)Math.Min(.001,time);if(dt<=0)return;
            var bones=actor.GetComponentsInChildren<Transform>();var p=bones.Select(b=>b.localPosition).ToArray();var q=bones.Select(b=>b.localRotation).ToArray();
            clip.SampleAnimation(actor,(float)time-dt);velocity=(start-arm.End.position)/dt;
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=p[i];bones[i].localRotation=q[i];}
        }
        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*t*(10+t*(-15+6*t));}
        public void Apply(float time){
            // The director has already blended torso and arms towards the knee.
            // Add a floor-clearing bow, with inherited hand velocity fading out.
            float u=Mathf.Clamp01(time/Duration),w=Ease(u);
            var target=arm.End.position;
            float k=Duration*(u-6*u*u*u+8*u*u*u*u-3*u*u*u*u*u);
            target=Vector3.Lerp(start,target,w)+velocity*k;
            target.y+=.04f*16*u*u*(1-u)*(1-u);
            arm.Solve(target,Quaternion.Slerp(rotation,arm.End.rotation,w),actor.transform.right);
        }
    }
}
