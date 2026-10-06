using System.Collections.Generic;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    public sealed class PoultryHenVisual:MonoBehaviour {}
    public sealed class AutonomousPoultryVisual:MonoBehaviour
    {
        Transform generated;Transform[] hens,heads,legs,eggModels;Transform feed;
        readonly List<Material> materials=new List<Material>();
        readonly Dictionary<Transform,Vector3> movedGroundDecor=new Dictionary<Transform,Vector3>();
        Mesh round;
        Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.08f);materials.Add(m);return m;}
        Transform Part(Transform parent,string name,Vector3 at,Vector3 size,Material mat,bool sphere=false)
        {
            if(parent==generated)at.z=-10.5f-at.z;
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=at;g.transform.localScale=size;
            var collider=g.GetComponent<Collider>();if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);
            if(sphere)g.GetComponent<MeshFilter>().sharedMesh=round;
            g.GetComponent<Renderer>().sharedMaterial=mat;return g.transform;
        }
        static Mesh RoundMesh()
        {
            var v=new List<Vector3>();var t=new List<int>();const int rings=6,sides=10;
            for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++){float a=j*Mathf.PI/rings,b=i*2*Mathf.PI/sides;v.Add(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b))*.5f);}
            for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+sides+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var m=new Mesh{name="Generated147 low poly rounded shape"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public void Configure(AutonomousLifeWorld world)
        {
            if(generated)return;
            // Only the original generator's tiny ground decorations, never ManualEdits or assets.
            foreach(Transform child in transform){
                if(child.name!="Grass tuft"&&child.name!="Spring flower"&&child.name!="Meadow stone")continue;
                Vector3 p=child.position;
                if(p.x<8.25f||p.x>12.55f||p.z< -8.05f||p.z> -2.45f)continue;
                movedGroundDecor.Add(child,p);p.x=12.85f+(p.x-8.25f)*.12f;child.position=p;
            }
            round=RoundMesh();generated=new GameObject("Generated147 poultry yard").transform;generated.SetParent(transform,false);
            var interior=generated.gameObject.AddComponent<FarmInteriorVolume>();interior.center=new Vector3(10.4f,.8f,-3.8f);interior.size=new Vector3(3,1.6f,2.6f);interior.roofRise=.5f;
            var wood=Mat("Poultry warm timber",new Color(.47f,.31f,.17f));var trim=Mat("Poultry pale timber",new Color(.69f,.52f,.3f));
            var roof=Mat("Roof poultry moss",new Color(.31f,.4f,.32f));var straw=Mat("Poultry straw",new Color(.73f,.59f,.28f));
            var cream=Mat("Poultry feathers cream",new Color(.93f,.85f,.67f));var rust=Mat("Poultry feathers buff",new Color(.65f,.37f,.19f));var gray=Mat("Poultry feathers soft gray",new Color(.65f,.65f,.59f));
            var red=Mat("Poultry comb muted red",new Color(.69f,.2f,.14f));var gold=Mat("Poultry beak and feet",new Color(.84f,.55f,.21f));var black=Mat("Poultry eyes",new Color(.08f,.07f,.055f));
            Part(generated,"Coop base",new Vector3(10.4f,.025f,-6.7f),new Vector3(3,.05f,2.6f),wood);
            Part(generated,"Coop back",new Vector3(10.4f,.85f,-7.9f),new Vector3(3,1.45f,.12f),wood);
            foreach(float x in new[]{8.95f,11.85f})Part(generated,"Coop side",new Vector3(x,.85f,-6.7f),new Vector3(.12f,1.45f,2.5f),wood);
            Part(generated,"Coop front lintel",new Vector3(10.4f,1.26f,-5.45f),new Vector3(3,.5f,.13f),trim);
            foreach(float x in new[]{8.95f,9.7f,10.65f,11.4f,11.85f})Part(generated,"Coop door post",new Vector3(x,.62f,-5.45f),new Vector3(.13f,.95f,.15f),trim);
            for(int i=0;i<2;i++){var r=Part(generated,"Coop pitched roof",new Vector3(9.57f+i*1.66f,1.73f,-6.7f),new Vector3(1.85f,.12f,2.94f),roof);r.localRotation=Quaternion.Euler(0,0,i==0?22:-22);}
            for(int i=0;i<3;i++)Part(generated,"Hen entry step",new Vector3(9.2f+i*.95f,.015f,-5.18f),new Vector3(.5f,.03f,.52f),trim);
            // Low rails keep the new yard clear of existing resident routes.
            foreach(float x in new[]{8.38f,12.42f}){
                for(int i=0;i<4;i++)Part(generated,"Yard post",new Vector3(x,.4f,-5.1f+i*.82f),new Vector3(.09f,.8f,.09f),trim);
                foreach(float y in new[]{.25f,.58f})Part(generated,"Yard side rail",new Vector3(x,y,-3.85f),new Vector3(.065f,.07f,2.6f),wood);
            }
            foreach(float y in new[]{.25f,.58f})Part(generated,"Yard front rail",new Vector3(10.4f,y,-2.6f),new Vector3(4.08f,.07f,.065f),wood);
            for(int i=0;i<5;i++)Part(generated,"Yard front post",new Vector3(8.38f+i*1.01f,.4f,-2.6f),new Vector3(.09f,.8f,.09f),trim);
            Part(generated,"Feed trough",new Vector3(10.1f,.2f,-2.96f),new Vector3(3.5f,.4f,.4f),wood);
            feed=Part(generated,"Feed grain",new Vector3(10.1f,.405f,-2.96f),new Vector3(3.4f,.03f,.3f),straw);
            Part(generated,"Nest collection box",new Vector3(8.53f,.48f,-6.35f),new Vector3(.48f,.72f,1.2f),wood);
            Part(generated,"Nest straw bed",new Vector3(8.5f,.86f,-6.35f),new Vector3(.48f,.07f,1.1f),straw);
            eggModels=new Transform[6];for(int i=0;i<6;i++)eggModels[i]=Part(generated,"Nest egg "+i,new Vector3(8.4f+i%2*.19f,.98f,-6.7f+i/2*.3f),new Vector3(.15f,.22f,.16f),cream,true);
            hens=new Transform[3];heads=new Transform[3];legs=new Transform[6];var colors=new[]{cream,rust,gray};
            for(int i=0;i<3;i++){
                var h=new GameObject("Generated147 hen "+(i+1));h.transform.SetParent(transform,false);h.AddComponent<PoultryHenVisual>();hens[i]=h.transform;
                Part(hens[i],"Round body",new Vector3(0,.42f,0),new Vector3(.49f,.49f,.64f),colors[i],true);
                var tail=Part(hens[i],"Tail",new Vector3(0,.55f,-.3f),new Vector3(.27f,.36f,.22f),colors[i],true);tail.localRotation=Quaternion.Euler(-25,0,0);
                foreach(float x in new[]{-.22f,.22f})Part(hens[i],"Wing",new Vector3(x,.44f,-.02f),new Vector3(.12f,.29f,.4f),colors[i],true);
                heads[i]=new GameObject("Pecking head pivot").transform;heads[i].SetParent(hens[i],false);heads[i].localPosition=new Vector3(0,.54f,.2f);
                Part(heads[i],"Head",new Vector3(0,.12f,.08f),new Vector3(.27f,.31f,.27f),colors[i],true);
                Part(heads[i],"Comb",new Vector3(0,.29f,.06f),new Vector3(.09f,.14f,.18f),red,true);
                Part(heads[i],"Beak",new Vector3(0,.09f,.25f),new Vector3(.12f,.075f,.16f),gold,true);
                foreach(float x in new[]{-.126f,.126f})Part(heads[i],"Eye",new Vector3(x,.17f,.15f),new Vector3(.035f,.045f,.04f),black,true);
                for(int k=0;k<2;k++)legs[i*2+k]=Part(hens[i],"Foot",new Vector3(k==0?-.13f:.13f,.1f,.02f),new Vector3(.08f,.19f,.19f),gold);
            }
            GetComponent<FarmCameraOcclusion>()?.Initialize(transform);Present(world.Simulation.Poultry,world.Simulation.elapsed);
        }
        public void Present(PoultryState state,float time)
        {
            if(!generated||state==null)return;
            feed.gameObject.SetActive(state.feed>0);for(int i=0;i<6;i++)eggModels[i].gameObject.SetActive(i<state.eggs);
            for(int i=0;i<3;i++){
                var h=state.hens[i];hens[i].gameObject.SetActive(!h.inside);hens[i].position=h.position;
                if(h.forward.sqrMagnitude>.001f)hens[i].rotation=Quaternion.LookRotation(h.forward);
                bool moving=(h.target-h.position).sqrMagnitude>.0001f;
                heads[i].localRotation=Quaternion.Euler(!moving?35*Mathf.Max(0,Mathf.Sin(h.idle*4)):Mathf.Sin(time*9+i)*4,0,0);
                for(int k=0;k<2;k++)legs[i*2+k].localPosition=new Vector3(k==0?-.13f:.13f,.1f+(moving?Mathf.Max(0,Mathf.Sin(time*9+i+k*Mathf.PI))*.065f:0),.02f);
            }
        }
        void OnDestroy(){foreach(var pair in movedGroundDecor)if(pair.Key)pair.Key.position=pair.Value;foreach(var m in materials)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}if(round){if(Application.isPlaying)Destroy(round);else DestroyImmediate(round);}}
    }
}
