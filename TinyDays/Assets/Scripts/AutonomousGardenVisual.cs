using System.Collections.Generic;
using UnityEngine;
using TinyDays.Review;
namespace TinyDays.Life
{
    public sealed class AutonomousGardenVisual:MonoBehaviour
    {
        Transform root,ground,finished;int previous=-1;readonly List<Material> materials=new List<Material>();
        public void Configure()
        {
            if(root)return;root=new GameObject("Generated150 garden").transform;root.SetParent(transform,false);
            var wood=Mat("Garden150 wood",new Color(.59f,.43f,.27f));var soil=Mat("Garden150 soil",new Color(.49f,.37f,.26f));
            var leaf=Mat("Leaf",new Color(.48f,.61f,.32f));var pink=Mat("Flower",new Color(.88f,.62f,.66f));var gold=Mat("FlowerGold",new Color(.96f,.81f,.47f));
            ground=Part(root,"Prepared garden ground",PrimitiveType.Cube,new Vector3(0,.025f,0),new Vector3(2.4f,.045f,1.4f),soil);
            foreach(float x in new[]{-1.15f,1.15f})foreach(float z in new[]{-.65f,.65f})Part(root,"Garden stake",PrimitiveType.Cube,new Vector3(x,.15f,z),new Vector3(.05f,.3f,.05f),wood);
            finished=new GameObject("Flowers and shared bench").transform;finished.SetParent(root,false);
            Part(finished,"Garden bench seat",PrimitiveType.Cube,new Vector3(0,.42f,-.35f),new Vector3(1.8f,.09f,.45f),wood);
            Part(finished,"Garden bench back",PrimitiveType.Cube,new Vector3(0,.74f,-.12f),new Vector3(1.8f,.25f,.08f),wood);
            foreach(float x in new[]{-.7f,.7f})Part(finished,"Garden bench leg",PrimitiveType.Cube,new Vector3(x,.22f,-.35f),new Vector3(.09f,.42f,.33f),wood);
            for(int i=0;i<9;i++){
                var p=new Vector3(-.96f+(i%5)*.46f,.11f,.26f+(i/5)*.29f);
                Part(finished,"Garden leaves",PrimitiveType.Sphere,p,new Vector3(.27f,.18f,.25f),leaf);
                Part(finished,"Garden blossom",PrimitiveType.Sphere,p+Vector3.up*.14f,new Vector3(.19f,.12f,.19f),i%2==0?pink:gold);
            }
            root.gameObject.SetActive(false);
        }
        Material Mat(string name,Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=c};materials.Add(m);return m;}
        static Transform Part(Transform parent,string name,PrimitiveType type,Vector3 at,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=at;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(Application.isPlaying)Destroy(g.GetComponent<Collider>());else DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
        public void Present(AutonomousSimulation s){if(!root||s.Garden==null)return;var g=s.Garden;root.position=g.site;root.gameObject.SetActive(g.stage>0);ground.gameObject.SetActive(g.stage>=2);ground.localScale=new Vector3(2.4f*Mathf.Lerp(.08f,1,g.progress/60),.045f,1.4f);finished.gameObject.SetActive(g.stage==3);if(previous!=g.stage){previous=g.stage;GetComponent<AutonomousSeasonVisual>()?.Configure();GetComponent<FarmCameraOcclusion>()?.Initialize(transform);}}
        void OnDestroy(){foreach(var m in materials)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}
