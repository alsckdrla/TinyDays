using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TinyDays.Review;
namespace TinyDays.Life
{
    public sealed class AutonomousFieldVisual:MonoBehaviour
    {
        Transform root,soil;Transform[] crops;int previous=-1;
        readonly List<Material> materials=new List<Material>();
        public void Configure(AutonomousLifeWorld w)
        {
            if(root)return;
            root=new GameObject("Generated149 field").transform;root.SetParent(transform,false);
            var earth=Mat("Field149 soil",new Color(.47f,.34f,.22f));var wood=Mat("Field149 stakes",new Color(.60f,.45f,.29f));
            soil=Part("Cultivated soil",new Vector3(0,.045f,0),new Vector3(1.8f,.08f,1.2f),earth);
            foreach(float x in new[]{-.85f,.85f})foreach(float z in new[]{-.55f,.55f})Part("Field boundary stake",new Vector3(x,.14f,z),new Vector3(.055f,.28f,.055f),wood);
            crops=new Transform[2];
            for(int i=0;i<2;i++){crops[i]=Instantiate(w.plants[0],root);crops[i].name="Additional carrots "+i;crops[i].localPosition=FieldLayout.Crop(Vector3.zero,i);}
            root.gameObject.SetActive(false);
        }
        Material Mat(string name,Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=c};materials.Add(m);return m;}
        Transform Part(string name,Vector3 at,Vector3 size,Material mat)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=at;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(Application.isPlaying)Destroy(g.GetComponent<Collider>());else DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
        public void Present(AutonomousSimulation s)
        {
            if(!root||s.Field==null)return;var f=s.Field;root.position=f.site;root.gameObject.SetActive(f.stage>0);
            soil.gameObject.SetActive(f.stage>=2);float width=1.8f*Mathf.Lerp(.05f,1,f.progress/AutonomousSimulation.FieldWorkSeconds);soil.localScale=new Vector3(width,.08f,1.2f);soil.localPosition=new Vector3((width-1.8f)*.5f,.045f,0);
            for(int i=0;i<2;i++){var crop=s.sites.First(x=>x.id==FieldLayout.Prefix+"carrot-"+i);crops[i].gameObject.SetActive(f.stage==3&&crop.planted);crops[i].localScale=Vector3.one*Mathf.Lerp(.18f,1,crop.growth);}
            if(previous!=f.stage){previous=f.stage;GetComponent<AutonomousSeasonVisual>()?.Configure();GetComponent<FarmCameraOcclusion>()?.Initialize(transform);}
        }
        void OnDestroy(){foreach(var m in materials)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}
