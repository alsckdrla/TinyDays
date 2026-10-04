using UnityEngine;

namespace TinyDays.Review {
    // A distance-parameterized, small curved route. Locomotion retains its own
    // gait clock, acceleration, landing and foot gathering; routes never reset it.
    public sealed class RabbitWalkingRoute {
        const int Samples=256;
        readonly Vector3[] points=new Vector3[Samples+1];
        readonly float[] lengths=new float[Samples+1];
        readonly float startYaw,endYaw;
        readonly bool approachHeading;
        readonly float approachDelta;
        public Pose Goal {get;}
        float heading,headingVelocity;
        bool stopped;
        public void StopTurning(){stopped=true;headingVelocity=0;}
        internal void ResetProgress(){Distance=0;heading=startYaw;headingVelocity=0;stopped=false;}
        public float SpeedScale=>Mathf.Lerp(1,.3f,Mathf.InverseLerp(15,80,Mathf.Abs(Mathf.DeltaAngle(heading,At(Mathf.Min(Length,Distance+.16f)).rotation.eulerAngles.y))));
        public float Distance {get;private set;}
        public float Length=>lengths[Samples];
        public float Remaining=>Mathf.Max(0,Length-Distance);
        public RabbitWalkingRoute(Transform actor,Vector3 target,float arrivalYaw):this(actor,target,arrivalYaw,false){}
        // A single distance clock across local avoidance waypoints: no intermediate stop.
        internal RabbitWalkingRoute(Pose start,Vector3[] waypoints,float arrivalYaw){
            startYaw=start.rotation.eulerAngles.y;endYaw=arrivalYaw;heading=startYaw;
            var nodes=new Vector3[waypoints.Length+2];nodes[0]=start.position;
            for(int i=0;i<waypoints.Length-1;i++)nodes[i+1]=new Vector3(waypoints[i].x,start.position.y,waypoints[i].z);
            var end=waypoints[waypoints.Length-1];end.y=start.position.y;
            Goal=new Pose(end,Quaternion.Euler(0,arrivalYaw,0));
            nodes[nodes.Length-1]=end;
            nodes[nodes.Length-2]=end-Goal.rotation*Vector3.forward*.65f;
            nodes[nodes.Length-1]+=Goal.rotation*Vector3.forward*.14f;
            var tangents=new Vector3[nodes.Length];
            tangents[0]=start.rotation*Vector3.forward;
            tangents[nodes.Length-1]=Goal.rotation*Vector3.forward;
            for(int i=1;i<nodes.Length-1;i++)tangents[i]=(nodes[i+1]-nodes[i-1]).normalized;
            tangents[nodes.Length-2]=Goal.rotation*Vector3.forward;
            for(int i=0;i<=Samples;i++){
                float span=i/(float)Samples*(nodes.Length-1);int j=Mathf.Min(nodes.Length-2,Mathf.FloorToInt(span));float u=span-j;
                float d=Vector3.Distance(nodes[j],nodes[j+1]);float handle=Mathf.Min(.35f,d*.3f);
                var a=nodes[j]+tangents[j]*handle;var b=nodes[j+1]-tangents[j+1]*handle;float v=1-u;
                points[i]=v*v*v*nodes[j]+3*v*v*u*a+3*v*u*u*b+u*u*u*nodes[j+1];
                if(i>0)lengths[i]=lengths[i-1]+Vector3.Distance(points[i-1],points[i]);
            }
        }
        internal RabbitWalkingRoute(Transform actor,Vector3 target,float arrivalYaw,bool stableApproach){
            var start=actor.position;target.y=start.y;
            startYaw=actor.eulerAngles.y;endYaw=arrivalYaw;
            Goal=new Pose(target,Quaternion.Euler(0,arrivalYaw,0));
            heading=startYaw;
            target+=Quaternion.Euler(0,arrivalYaw,0)*Vector3.forward*.14f;
            float distance=Vector3.Distance(start,target);
            float handle=Mathf.Min(.6f,distance*.35f);
            var a=start+actor.forward*handle;
            var endForward=Quaternion.Euler(0,arrivalYaw,0)*Vector3.forward;
            var entry=Goal.position-endForward*.45f;
            var b=target-endForward*handle;
            var direction=(target-start).normalized;
            float travelYaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            approachHeading=stableApproach||Mathf.Abs(Mathf.DeltaAngle(travelYaw,endYaw))>35;
            if(approachHeading)b=entry-endForward*.28f;
            approachDelta=Mathf.DeltaAngle(startYaw,endYaw);
            if(Mathf.Abs(approachDelta)>179)approachDelta=Mathf.Sign(Mathf.DeltaAngle(startYaw,travelYaw))*180;
            // An opposed heading needs a small lateral arc rather than a cusp
            // where the path tangent would flip by 180 degrees in one frame.
            float opposed=Mathf.Clamp01((-Vector3.Dot(actor.forward,direction)-.25f)/.75f);
            var side=Vector3.Cross(Vector3.up,direction);
            if(Mathf.Abs(Mathf.DeltaAngle(startYaw,travelYaw))>179&&arrivalYaw-startYaw>0)side=-side;
            for(int i=0;i<=Samples;i++){
                float u=i/(float)Samples,v=1-u;
                if(approachHeading){
                    float t=Mathf.Min(1,u/.72f),s=1-t;
                    points[i]=u<.72f?s*s*s*start+3*s*s*t*a+3*s*t*t*b+t*t*t*entry:Vector3.Lerp(entry,target,(u-.72f)/.28f);
                }else points[i]=v*v*v*start+3*v*v*u*a+3*v*u*u*b+u*u*u*target
                    +side*(opposed*Mathf.Min(.4f,distance*.3f)*Mathf.Pow(Mathf.Sin(Mathf.PI*u),2));
                if(i>0)lengths[i]=lengths[i-1]+Vector3.Distance(points[i-1],points[i]);
            }
        }
        public Pose Advance(float distance,float dt){
            Distance+=Mathf.Max(0,distance);var pose=At(Distance);
            if(!stopped){float wanted=Mathf.SmoothDampAngle(heading,pose.rotation.eulerAngles.y,ref headingVelocity,Remaining<.4f?.045f:.10f,100,dt);heading=Mathf.MoveTowardsAngle(heading,wanted,110*dt);}
            return new Pose(pose.position,Quaternion.Euler(0,heading,0));
        }
        public Pose Predict(float distance,float speed){
            float h=heading,v=headingVelocity,progress=0;
            float duration=distance/Mathf.Max(.25f,speed);int steps=Mathf.Max(1,Mathf.CeilToInt(duration/.02f));float dt=duration/steps;
            for(int i=1;i<=steps;i++){
                progress=Distance+distance*i/steps;var pose=At(progress);
                float wanted=Mathf.SmoothDampAngle(h,pose.rotation.eulerAngles.y,ref v,Length-progress<.4f?.045f:.10f,100,Mathf.Max(.00001f,dt));h=Mathf.MoveTowardsAngle(h,wanted,110*dt);
            }
            return new Pose(At(Distance+distance).position,Quaternion.Euler(0,h,0));
        }
        public Pose At(float distance){
            if(distance>Length)return new Pose(points[Samples]+Quaternion.Euler(0,endYaw,0)*Vector3.forward*(distance-Length),Quaternion.Euler(0,endYaw,0));
            distance=Mathf.Clamp(distance,0,Length);int lo=0,hi=Samples;
            while(hi-lo>1){int mid=(lo+hi)/2;if(lengths[mid]<distance)lo=mid;else hi=mid;}
            float u=Mathf.InverseLerp(lengths[lo],lengths[hi],distance);
            var position=Vector3.Lerp(points[lo],points[hi],u);
            var tangent=points[hi]-points[lo];float yaw=tangent.sqrMagnitude>1e-10f?Mathf.Atan2(tangent.x,tangent.z)*Mathf.Rad2Deg:endYaw;
            // The endpoint heading settles during deceleration, not in a separate
            // stationary turn after the walk has finished.
            float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(0,Length-.55f),Mathf.Max(.15f,Length-.27f),distance));
            yaw=Mathf.LerpAngle(yaw,endYaw,blend);
            if(distance<.00001f)yaw=startYaw;
            return new Pose(position,Quaternion.Euler(0,yaw,0));
        }
    }
}
