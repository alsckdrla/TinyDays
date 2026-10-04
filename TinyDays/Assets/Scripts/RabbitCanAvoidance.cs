using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review {
    // Finite local detours for the one-resident review, not a village pathfinder.
    public sealed class RabbitCanAvoidance {
        public const float Margin=.05f;
        readonly List<Bounds> solids=new List<Bounds>();
        readonly List<Bounds> body=new List<Bounds>();
        readonly List<Bounds> resting=new List<Bounds>();
        readonly Dictionary<Vector3Int,List<int>> buckets=new Dictionary<Vector3Int,List<int>>();
        readonly int[] visited;
        int queryId;
        readonly Dictionary<Pose,Tuple<bool,string>> restTests=new Dictionary<Pose,Tuple<bool,string>>(),walkTests=new Dictionary<Pose,Tuple<bool,string>>();
        readonly Dictionary<Tuple<Pose,Pose>,Tuple<bool,string>> preparationTests=new Dictionary<Tuple<Pose,Pose>,Tuple<bool,string>>();
        readonly SkinReader[] skinReaders;
        // Per-planner immutable mesh data; never shared across residents, model edits or replans.
        // Keep the same four-weight arithmetic as AdultRabbitSitCoat.World, but avoid copying
        // native mesh arrays/bindposes for every bone at every collision sample.
        sealed class SkinReader {
            readonly Vector3[] vertices,output;
            readonly BoneWeight[] weights;
            readonly Matrix4x4[] bind,matrices;
            readonly Transform[] bones;
            public SkinReader(SkinnedMeshRenderer skin){
                vertices=skin.sharedMesh.vertices;weights=skin.sharedMesh.boneWeights;bind=skin.sharedMesh.bindposes;
                bones=skin.bones;matrices=new Matrix4x4[bones.Length];output=new Vector3[vertices.Length];
            }
            public Vector3[] World(){
                for(int i=0;i<bones.Length;i++)matrices[i]=bones[i].localToWorldMatrix*bind[i];
                for(int i=0;i<vertices.Length;i++){
                    var p=vertices[i];var w=weights[i];
                    output[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                }
                return output;
            }
        }
        static Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x/.5f),Mathf.FloorToInt(p.y/.2f),Mathf.FloorToInt(p.z/.5f));
        public string BlockedBy {get;private set;}
        readonly RabbitHomeLifeReview home;
        readonly Vector3 canPosition;
        public Bounds FlowerBed {get;private set;}
        public RabbitCanAvoidance(RabbitHomeLifeReview h,Transform can){
            home=h;canPosition=can.position;
            var actor=h.resident.transform;
            var bones=actor.GetComponentsInChildren<Transform>();var p=bones.Select(t=>t.localPosition).ToArray();var q=bones.Select(t=>t.localRotation).ToArray();
            var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled&&s.name!="Coat"&&s.name!="Tail").ToArray();
            skinReaders=skins.Select(s=>new SkinReader(s)).ToArray();
            // Preserve evaluated incoming pose while collecting the authored gait envelope.
            try{
                foreach(var skin in skinReaders){
                    for(int i=0;i<bones.Length;i++){bones[i].localPosition=p[i];bones[i].localRotation=q[i];}
                    var incoming=skin.World().Select(actor.InverseTransformPoint).ToArray();
                    var gait=new List<Vector3>();
                    for(int phase=0;phase<16;phase++){
                        h.walkClip.SampleAnimation(h.resident,phase*.8f/16);
                        gait.AddRange(skin.World().Select(actor.InverseTransformPoint));
                    }
                    float low=Mathf.Min(incoming.Min(v=>v.y),gait.Min(v=>v.y)),high=Mathf.Max(incoming.Max(v=>v.y),gait.Max(v=>v.y));
                    // Height slices avoid treating a round head's empty lower corners as solid.
                    for(float y=low;y<=high;y+=.08f){
                        var restLayer=incoming.Where(v=>v.y>=y-.01f&&v.y<=y+.09f).ToArray();
                        var moveLayer=gait.Where(v=>v.y>=y-.01f&&v.y<=y+.09f).ToArray();
                        var all=restLayer.Concat(moveLayer).ToArray();if(all.Length==0)continue;
                        for(float x=all.Min(v=>v.x);x<=all.Max(v=>v.x);x+=.08f){
                            var rest=restLayer.Where(v=>v.x>=x-.01f&&v.x<=x+.09f).ToArray();
                            var moving=moveLayer.Where(v=>v.x>=x-.01f&&v.x<=x+.09f).ToArray();
                            if(rest.Length==0&&moving.Length==0)continue;
                            Func<Vector3[],Bounds> box=vertices=>{var result=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)result.Encapsulate(v);return result;};
                            var a=rest.Length>0?box(rest):new Bounds(box(moving).center,Vector3.zero);
                            var b=moving.Length>0?box(moving):a;
                            resting.Add(a);body.Add(b);
                        }
                    }
                }
            }finally{for(int i=0;i<bones.Length;i++){bones[i].localPosition=p[i];bones[i].localRotation=q[i];}}
            // Most rejected detours fail at the shoes. Test those first instead
            // of walking through every head/ear slice before finding the contact.
            var order=Enumerable.Range(0,body.Count).OrderBy(i=>body[i].center.y).ToArray();
            var sortedBody=order.Select(i=>body[i]).ToArray();var sortedRest=order.Select(i=>resting[i]).ToArray();
            body.Clear();body.AddRange(sortedBody);resting.Clear();resting.AddRange(sortedRest);
            var bed=h.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Watering flower bed");
            if(bed){bool first=true;foreach(var r in bed.GetComponentsInChildren<Renderer>()){if(first){FlowerBed=r.bounds;first=false;}else{var box=FlowerBed;box.Encapsulate(r.bounds);FlowerBed=box;}}if(!first){var box=FlowerBed;box.SetMinMax(new Vector3(box.min.x,h.groundHeight,box.min.z),new Vector3(box.max.x,h.groundHeight+.12f,box.max.z));FlowerBed=box;solids.Add(box);}}
            foreach(var f in h.GetComponentsInChildren<MeshFilter>()){
                if(f.transform.IsChildOf(actor)||!f.sharedMesh||!f.sharedMesh.isReadable)continue;
                var vertices=f.sharedMesh.vertices.Select(f.transform.TransformPoint).ToArray();var triangles=f.sharedMesh.triangles;
                // Triangle boxes keep wall apertures open; ground triangles remain traversable.
                for(int i=0;i<triangles.Length;i+=3){var box=new Bounds(vertices[triangles[i]],Vector3.zero);box.Encapsulate(vertices[triangles[i+1]]);box.Encapsulate(vertices[triangles[i+2]]);
                    if(box.max.y<=h.groundHeight+.02f||box.min.y>h.groundHeight+2.2f)continue;
                    solids.Add(box);
                }
            }
            visited=new int[solids.Count];
            for(int index=0;index<solids.Count;index++){var solid=solids[index];
                var low=Cell(solid.min);var high=Cell(solid.max);
                for(int x=low.x;x<=high.x;x++)for(int y=low.y;y<=high.y;y++)for(int z=low.z;z<=high.z;z++){
                    var key=new Vector3Int(x,y,z);List<int> list;if(!buckets.TryGetValue(key,out list)){list=new List<int>();buckets.Add(key,list);}list.Add(index);
                }
            }
        }
        // Broad phase only: retain every obstacle intersecting the full test AABB.
        // Triangle/SAT and exact vertex checks below still use the unchanged 5cm margin.
        IEnumerable<Bounds> Nearby(Vector3 center,Vector3 extents){
            if(queryId==int.MaxValue){Array.Clear(visited,0,visited.Length);queryId=0;}
            int id=++queryId;var low=Cell(center-extents);var high=Cell(center+extents);
            for(int x=low.x;x<=high.x;x++)for(int y=low.y;y<=high.y;y++)for(int z=low.z;z<=high.z;z++){
                List<int> list;if(!buckets.TryGetValue(new Vector3Int(x,y,z),out list))continue;
                foreach(int index in list){if(visited[index]==id)continue;visited[index]=id;yield return solids[index];}
            }
        }
        // Oriented gait envelopes against obstacle boxes, with a 5cm separation margin.
        public bool Clear(Pose pose,float blend=1){
            if(blend!=0&&blend!=1)return Evaluate(pose,blend);
            // Many candidate curves share exactly the same initial pose. Cache
            // exact values only: no spatial rounding or clearance relaxation.
            var cache=blend==0?restTests:walkTests;Tuple<bool,string> result;
            if(cache.TryGetValue(pose,out result)){BlockedBy=result.Item2;return result.Item1;}
            bool clear=Evaluate(pose,blend);cache.Add(pose,Tuple.Create(clear,BlockedBy));return clear;
        }
        bool Evaluate(Pose pose,float blend){
            var right=pose.rotation*Vector3.right;var forward=pose.rotation*Vector3.forward;
            for(int index=0;index<body.Count;index++){var local=body[index];var rest=resting[index];var center=pose.position+pose.rotation*Vector3.Lerp(rest.center,local.center,blend);var e=Vector3.Lerp(rest.extents,local.extents,blend);
                if(e.sqrMagnitude<.0000001f)continue;
                var worldExtent=new Vector3(Mathf.Abs(right.x)*e.x+Mathf.Abs(forward.x)*e.z+Margin,e.y,Mathf.Abs(right.z)*e.x+Mathf.Abs(forward.z)*e.z+Margin);
                foreach(var solid in Nearby(center,worldExtent)){
                    if(center.y+e.y<solid.min.y||center.y-e.y>solid.max.y)continue;
                    var d=solid.center-center;var s=solid.extents;
                    if(Mathf.Abs(d.x)>Mathf.Abs(right.x)*e.x+Mathf.Abs(forward.x)*e.z+s.x+Margin)continue;
                    if(Mathf.Abs(d.z)>Mathf.Abs(right.z)*e.x+Mathf.Abs(forward.z)*e.z+s.z+Margin)continue;
                    if(Mathf.Abs(Vector3.Dot(d,right))>e.x+Mathf.Abs(right.x)*s.x+Mathf.Abs(right.z)*s.z+Margin)continue;
                    if(Mathf.Abs(Vector3.Dot(d,forward))>e.z+Mathf.Abs(forward.x)*s.x+Mathf.Abs(forward.z)*s.z+Margin)continue;
                    BlockedBy=$"body{index} center={center:F3} extent={e:F3} obstacle={solid}";return false;
                }
            }return true;
        }
        bool ClearTurn(Pose origin,Pose end){for(int i=0;i<=36;i++)if(!Clear(new Pose(Vector3.Lerp(origin.position,end.position,i/36f),Quaternion.Slerp(origin.rotation,end.rotation,i/36f)),0))return false;return true;}
        bool ClearPreparation(Pose origin,Pose end){
            var key=Tuple.Create(origin,end);Tuple<bool,string> result;
            if(preparationTests.TryGetValue(key,out result)){BlockedBy=result.Item2;return result.Item1;}
            bool clear=EvaluatePreparation(origin,end);preparationTests.Add(key,Tuple.Create(clear,BlockedBy));return clear;
        }
        bool EvaluatePreparation(Pose origin,Pose end){
            var actor=home.resident.transform;var bones=actor.GetComponentsInChildren<Transform>();
            var positions=bones.Select(t=>t.localPosition).ToArray();var rotations=bones.Select(t=>t.localRotation).ToArray();
            var root=new Pose(actor.position,actor.rotation);
            try{
                home.idleClip.SampleAnimation(home.resident,0);
                var baseP=bones.Select(t=>t.localPosition).ToArray();var baseQ=bones.Select(t=>t.localRotation).ToArray();
                var support=new RabbitHomeFootwork(home.resident,home.groundHeight){FitStandingReach=true,ExactStepLowering=0};support.Hold();
                support.BeginPreparation(u=>new Pose(Vector3.Lerp(origin.position,end.position,u),Quaternion.Slerp(origin.rotation,end.rotation,u)));
                int count=Mathf.RoundToInt(support.Duration*240);
                for(int tick=0;tick<=count;tick++){
                    for(int i=1;i<bones.Length;i++){bones[i].localPosition=baseP[i];bones[i].localRotation=baseQ[i];}
                    float time=tick/240f;support.PlaceRoot(time);home.walkClip.SampleAnimation(home.resident,time%.8f);support.Apply(time);
                    if(tick%4!=0)continue;
                    foreach(var skin in skinReaders)foreach(var point in skin.World()){
                        foreach(var solid in Nearby(point,new Vector3(Margin,0,Margin))){
                            // Flat-floor contact is intentional; all actual scene obstacles retain 5cm clearance.
                            if(point.y>solid.max.y||point.y<solid.min.y)continue;
                            float dx=Mathf.Max(solid.min.x-point.x,0,point.x-solid.max.x),dz=Mathf.Max(solid.min.z-point.z,0,point.z-solid.max.z);
                            if(dx*dx+dz*dz<Margin*Margin){BlockedBy="staged foot/body preparation near "+solid;return false;}
                        }
                    }
                }
                return support.MaxReachError<=.001f;
            }finally{
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
                actor.SetPositionAndRotation(root.position,root.rotation);
            }
        }
        public bool ClearRoute(RabbitWalkingRoute route){
            try{
                // Check rate-limited body heading as well as the geometric path tangent.
                for(float d=0;d<=route.Length+.14f;d+=.025f){
                    var actual=route.Advance(d==0?0:.025f,.025f/1.15f);
                    // The first IK landing already uses a full foot target even
                    // while the upper-body idle/walk blend is still small.
                    float blend=1;
                    if(!Clear(actual,blend)||!Clear(route.At(d),blend))return false;
                }
                return true;
            }finally{route.ResetProgress();}
        }
        public bool Find(Vector3 target,float arrivalYaw,out RabbitWalkingRoute chosen,out Pose preparation){
            preparationTests.Clear();
            chosen=null;preparation=new Pose(home.resident.transform.position,home.resident.transform.rotation);float best=float.PositiveInfinity;
            var origin=new Pose(home.resident.transform.position,home.resident.transform.rotation);
            var toward=(target-origin.position).normalized;
            var away=origin.position-canPosition;away.y=0;away.Normalize();var sideDirection=Vector3.Cross(Vector3.up,away);
            var candidates=new List<Tuple<float,RabbitWalkingRoute,Pose>>();
            foreach(float turn in new[]{0f,-45f,45f,-90f,90f,180f}){
              foreach(float offset in new[]{0f,-.22f,.22f})foreach(float outward in new[]{0f,.5f}){
                if(offset==0&&outward!=0)continue;
                float yaw=origin.rotation.eulerAngles.y+turn;
                var shift=offset==0?Vector3.zero:away*(Mathf.Abs(offset)*outward)+sideDirection*offset;
                var start=new Pose(origin.position+shift,Quaternion.Euler(0,yaw,0));
                if(!ClearTurn(origin,start))continue;
                foreach(float distance in new[]{0f,.65f}){
                    var first=start.position+start.rotation*Vector3.forward*distance;
                    foreach(float side in new[]{0f,-.65f,.65f}){
                        var middle=Vector3.Lerp(first,target,.45f)+Vector3.Cross(Vector3.up,toward)*side;
                        var nodes=distance==0&&side==0?new[]{target}:distance==0?new[]{middle,target}:new[]{first,middle,target};
                        var route=new RabbitWalkingRoute(start,nodes,arrivalYaw);
                        candidates.Add(Tuple.Create(route.Length+shift.magnitude+Mathf.Abs(turn)*.001f,route,start));
                    }
                }
              }
            }
            // Sorted candidates stop at the first safe route, retaining the exact
            // shortest-candidate result without validating longer alternatives.
            foreach(var candidate in candidates.OrderBy(c=>c.Item1)){
                if(!ClearRoute(candidate.Item2))continue;
                if(!ClearPreparation(origin,candidate.Item3))continue;
                best=candidate.Item1;chosen=candidate.Item2;preparation=candidate.Item3;break;
            }
            Debug.Log($"CAN133_ROUTE {(chosen!=null?"FOUND":"NONE")} length={best} preparation={preparation.position} yaw={preparation.rotation.eulerAngles.y} last={BlockedBy}");return chosen!=null;
        }
    }
}
