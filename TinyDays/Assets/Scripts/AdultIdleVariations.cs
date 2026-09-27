using System;
using System.Linq;
using UnityEngine;
namespace TinyDays.Review {
    // Review metadata; body-family playback itself receives clips, not indices.
    public static class AdultIdleVariations {
        public sealed class Definition {
            public int Index; public string Label; public bool Seated; public float[] Times; public string[] Poses;
        }
        public static readonly Definition[] All={
            new Definition{Index=12,Label="서서 한숨",Times=new[]{0,.45f,.85f,1f,1.5f,2f+2f/30,3.8f,5},Poses=new[]{"시작","들이쉬기","정점 앞","정점","내쉬기","한숨 끝","복귀","끝"}},
            new Definition{Index=13,Label="앉아서 한숨",Seated=true,Times=new[]{0,.45f,.85f,1f,1.5f,2f+2f/30,3.8f,5},Poses=new[]{"시작","들이쉬기","정점 앞","정점","내쉬기","한숨 끝","복귀","끝"}},
            new Definition{Index=14,Label="앉아서 발목 까딱",Seated=true,Times=new[]{0,.65f,1.15f,1.65f,2.15f,2.65f,3.15f,4},Poses=new[]{"시작","왼발 1","오른발 1","왼발 2","오른발 2","왼발 3","오른발 3","끝"}},
            new Definition{Index=15,Label="서서 짝발 대기",Times=new[]{0,.75f,2f,4.25f,6.1f,6.85f,8.1f,13},Poses=new[]{"시작","오른발 지지","왼발 까딱","짝발 유지","양발 원위치","왼발 지지","오른발 까딱","끝"}},
            new Definition{Index=16,Label="앉아서 낙서하기",Seated=true,Times=new[]{0,1.2f,2.2f,3.2f,4.3f,5.8f,6.5f,8},Poses=new[]{"시작","손 닿기","작은 원","곡선","두리번","손 보기","마지막 선","복귀"}}
        };
        public static Definition Find(int index)=>Array.Find(All,d=>d.Index==index);
        public static void ConfigureHeels(GameObject actor,IdleSupportLeg[] legs){
            var shoes=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Shoes");
            var points=AdultRabbitSitCoat.World(shoes);var weights=shoes.sharedMesh.boneWeights;
            foreach(var leg in legs){int bone=Array.IndexOf(shoes.bones,leg.End);var indices=Enumerable.Range(0,points.Length).Where(i=>weights[i].boneIndex0==bone&&weights[i].weight0>.99f).ToArray();float floor=indices.Min(i=>points[i].y);int heel=indices.Where(i=>points[i].y<floor+.0001f).OrderBy(i=>Vector3.Dot(points[i],actor.transform.forward)).First();leg.HeelLocal=leg.End.InverseTransformPoint(points[heel]);leg.SoleLocal=indices.Select(i=>leg.End.InverseTransformPoint(points[i])).ToArray();}
        }
        public static bool Supported(GameObject actor,IdleSupportLeg[] legs){
            float floor=actor.transform.position.y;
            return legs.All(l=>{float y=l.End.TransformPoint(l.HeelLocal).y-floor;return y>=-.0005f&&y<=.005f;});
        }
        public static void Secondary(IdleSecondaryMotion profile,double time,float duration){
            float t=(float)time;float envelope=Mathf.SmoothStep(0,1,t/.35f)*Mathf.SmoothStep(0,1,(duration-t)/.35f);
            profile.ApplyFidget(time,.6f*envelope);
        }
    }
}
