using System;
using UnityEngine;

namespace TinyDays.Review
{
    // Authored circulation, not autonomous AI, production, or navigation.
    public sealed class FarmLifeDirector : MonoBehaviour
    {
        [Serializable] public class Resident
        {
            public Transform root;
            public FarmResidentVisual visual;
            public double offset;
        }
        public Resident[] residents;
        public Vector3[] route;
        public float[] distances, stations, waits;
        public string[] places;
        public float speed=.195f;
        public double elapsed;
        public bool paused;
        public double Period
        {
            get { double total=distances[distances.Length-1]/speed+stations.Length*.5; foreach(float wait in waits)total+=wait;return total; }
        }
        public struct Pose
        {
            public Vector3 position, forward;
            public float weight, walkTime;
            public int station;
            public bool moving;
        }
        public Vector3 Point(float d)
        {
            d=Mathf.Repeat(d,distances[distances.Length-1]);
            int i=Array.BinarySearch(distances,d);
            if(i<0)i=~i-1;
            i=Mathf.Clamp(i,0,route.Length-2);
            return Vector3.Lerp(route[i],route[i+1],Mathf.InverseLerp(distances[i],distances[i+1],d));
        }
        public Pose Resolve(double time)
        {
            double t=((time%Period)+Period)%Period;
            for(int i=0;i<stations.Length;i++)
            {
                float start=stations[i], end=i+1<stations.Length?stations[i+1]:distances[distances.Length-1];
                if(t<waits[i])return new Pose{position=Point(start),forward=(Point(start+.2f)-Point(start)).normalized,station=i};
                t-=waits[i];
                float duration=(end-start)/speed+.5f;
                if(t<duration)
                {
                    float u=(float)t, travel;
                    if(u<.5f)travel=speed*u*u;
                    else if(u>duration-.5f){float remaining=duration-u;travel=end-start-speed*remaining*remaining;}
                    else travel=speed*(u-.25f);
                    float d=start+travel;
                    return new Pose{position=Point(d),forward=(Point(d+.2f)-Point(d)).normalized,
                        weight=Mathf.SmoothStep(0,1,Mathf.Min(u,duration-u)/.3f),
                        walkTime=travel/speed,station=i,moving=true};
                }
                t-=duration;
            }
            throw new InvalidOperationException("Invalid farm review timeline");
        }
        public void Sample(double time)
        {
            foreach(var resident in residents)
            {
                Pose p=Resolve(time+resident.offset);
                resident.root.localPosition=p.position;
                resident.root.localRotation=Quaternion.LookRotation(p.forward,Vector3.up);
                resident.visual.Sample(time+resident.offset,p.walkTime,p.weight);
            }
        }
        public void Restart(){elapsed=0;Sample(0);}
        void Update(){if(!paused)elapsed+=Time.unscaledDeltaTime;Sample(elapsed);}
    }
}
