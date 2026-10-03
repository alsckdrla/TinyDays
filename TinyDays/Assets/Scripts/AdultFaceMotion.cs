using System;
using UnityEngine;

namespace TinyDays.Review
{
    // Independent of body clips: clocks survive clip and life-mode changes.
    public sealed class AdultFaceMotion
    {
        public static readonly double[] Intervals={3.2,3.6,4.0,4.4};
        uint randomState;
        readonly GameObject root;
        SkinnedMeshRenderer skin;
        int shape=-1,last=-1;
        double clock,next,start=-1;
        bool suppressed;
        public float Blink {get;private set;}
        public double Clock=>clock;
        public double Next=>next;
        public void Clear(){Blink=0;Apply();}
        public AdultFaceMotion(GameObject resident,int seed=1701){root=resident;randomState=unchecked((uint)seed);Schedule();}
        int RandomChoice(){randomState=unchecked(randomState*1664525u+1013904223u);return (int)((randomState>>16)%4);}
        void Schedule(){int choice;do{choice=RandomChoice();}while(choice==last);last=choice;next=clock+Intervals[choice];}
        static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
        public static float Curve(double time){if(time<0||time>=.24)return 0;if(time<.08)return Smooth((float)(time/.08));if(time<.11)return 1;return 1-Smooth((float)((time-.11)/.13));}
        public void Advance(double dt,bool inhibit){
            clock+=Math.Max(0,dt);
            if(inhibit){suppressed=true;Blink*=Mathf.Exp(-(float)Math.Max(0,dt)*35);start=-1;return;}
            if(suppressed){suppressed=false;Blink=0;Schedule();}
            while(clock>=next){start=next;double now=clock;clock=next;Schedule();clock=now;}
            Blink=Curve(clock-start);
        }
        public void Apply(float sleep=0){
            if(!skin&&root)foreach(var candidate in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                var mesh=candidate.sharedMesh;if(!mesh)continue;
                for(int i=0;i<mesh.blendShapeCount;i++)if(mesh.GetBlendShapeName(i).EndsWith("SleepEyesClosed",StringComparison.Ordinal)){skin=candidate;shape=i;break;}
                if(skin)break;
            }
            if(skin)skin.SetBlendShapeWeight(shape,100*Mathf.Clamp01(sleep+(1-sleep)*Blink));
        }
    }
}
