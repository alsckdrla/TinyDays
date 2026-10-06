using System.Collections.Generic;
using UnityEngine;
using TinyDays.Review;
namespace TinyDays.Life
{
    public sealed class AutonomousGrowthVisual:MonoBehaviour
    {
        Transform root,building,frame,walls,stock;Transform[] sources,carried;
        int previousStage=-1,previousShape=-1;
        readonly Dictionary<int,Transform> drops=new Dictionary<int,Transform>();
        readonly List<Material> materials=new List<Material>();Material wood,trim,roof;
        Transform Part(Transform parent,string name,Vector3 p,Vector3 scale,Material mat)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;var c=g.GetComponent<Collider>();if(Application.isPlaying)Destroy(c);else DestroyImmediate(c);g.GetComponent<Renderer>().sharedMaterial=mat;return g.transform;}
        Material Mat(string name,Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=c};materials.Add(m);return m;}
        Transform Group(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
        Transform Logs(Transform parent,string name,Vector3 at)
        {var g=Group(parent,name);g.localPosition=at;for(int i=0;i<6;i++)Part(g,"Timber",new Vector3((i%3-1)*.13f,.075f+i/3*.13f,0),new Vector3(.11f,.11f,.6f),i%2==0?wood:trim);return g;}
        public void Configure(AutonomousLifeWorld w,GrowthLayout layout)
        {
            if(root)return;root=Group(transform,"Generated148 growth");
            wood=Mat("Expansion timber",new Color(.47f,.32f,.19f));trim=Mat("Expansion pale wood",new Color(.67f,.49f,.29f));roof=Mat("Roof expansion",new Color(.32f,.39f,.31f));
            sources=new[]{Logs(root,"Fallen branches 0",layout.sources[0]),Logs(root,"Fallen branches 1",layout.sources[1])};
            stock=Logs(root,"Stored timber",layout.store+Vector3.right*.45f);carried=new Transform[3];
            for(int i=0;i<3;i++){carried[i]=Logs(w.cargo[i].parent,"Carried timber",w.cargo[i].localPosition);carried[i].localScale=Vector3.one*.7f;}
            building=Group(root,"Warehouse annex");frame=Group(building,"Frame");walls=Group(building,"Finished walls");
            foreach(float x in new[]{-.85f,.85f})foreach(float z in new[]{-.75f,.75f}){Part(building,"Plot stake",new Vector3(x,.18f,z),new Vector3(.09f,.36f,.09f),trim);Part(frame,"Post",new Vector3(x,.72f,z),new Vector3(.12f,1.44f,.12f),wood);}
            Part(frame,"Beam",new Vector3(0,1.45f,-.75f),new Vector3(1.9f,.12f,.12f),trim);Part(frame,"Rear beam",new Vector3(0,1.45f,.75f),new Vector3(1.9f,.12f,.12f),trim);
            Part(walls,"Back wall",new Vector3(0,.7f,.75f),new Vector3(1.7f,1.4f,.1f),wood);
            Part(walls,"Left wall",new Vector3(-.85f,.7f,0),new Vector3(.1f,1.4f,1.5f),wood);Part(walls,"Right wall",new Vector3(.85f,.7f,0),new Vector3(.1f,1.4f,1.5f),wood);
            Part(walls,"Front left",new Vector3(-.61f,.7f,-.75f),new Vector3(.38f,1.4f,.1f),wood);Part(walls,"Front right",new Vector3(.61f,.7f,-.75f),new Vector3(.38f,1.4f,.1f),wood);
            Part(walls,"Door",new Vector3(0,.64f,-.72f),new Vector3(.8f,1.28f,.065f),trim);Part(walls,"Handle",new Vector3(.25f,.66f,-.762f),new Vector3(.06f,.14f,.035f),wood);
            var r=Part(walls,"Sloping roof",new Vector3(0,1.52f,0),new Vector3(2,.14f,1.9f),roof);r.localRotation=Quaternion.Euler(-8,0,0);
            var interior=building.gameObject.AddComponent<FarmInteriorVolume>();interior.center=new Vector3(0,.72f,0);interior.size=new Vector3(1.8f,1.5f,1.6f);interior.roofRise=.2f;
            building.gameObject.SetActive(false);
        }
        public void Present(AutonomousSimulation s)
        {
            if(!root||s.Growth==null)return;var g=s.Growth;
            for(int i=0;i<2;i++){sources[i].gameObject.SetActive(g.sources[i]>0);sources[i].localScale=new Vector3(1,Mathf.Max(.1f,g.sources[i]/6f),1);}
            stock.gameObject.SetActive(g.wood>0);stock.localScale=new Vector3(1,Mathf.Max(.1f,g.wood/6f),1);
            for(int i=0;i<3;i++)carried[i].gameObject.SetActive(s.residents[i].woodCargo>0);
            var remove=new List<int>();foreach(var pair in drops)if(!g.drops.Exists(b=>b.id==pair.Key)){if(Application.isPlaying)Destroy(pair.Value.gameObject);else DestroyImmediate(pair.Value.gameObject);remove.Add(pair.Key);}foreach(int id in remove)drops.Remove(id);
            foreach(var b in g.drops){if(!drops.TryGetValue(b.id,out var t)){t=Logs(root,"Recoverable timber "+b.id,b.position);drops.Add(b.id,t);}t.position=b.position;}
            int shape=g.stage==3?2:g.stage==2?1:0;
            building.position=g.site;building.gameObject.SetActive(g.stage>0);frame.gameObject.SetActive(shape>=1);walls.gameObject.SetActive(shape>=2);
            if(previousStage!=g.stage||previousShape!=shape){previousStage=g.stage;previousShape=shape;GetComponent<AutonomousSeasonVisual>()?.Configure();GetComponent<FarmCameraOcclusion>()?.Initialize(transform);}
        }
        void OnDestroy(){foreach(var m in materials)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}
