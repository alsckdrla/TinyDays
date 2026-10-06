using System;
using System.Collections.Generic;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    public enum LifeSeason { Spring, Summer, Autumn, Winter }
    [Serializable] public sealed class SeasonSaveData
    {
        public int season; public float progress, transition=10; public bool automatic=true;
        public Vector4 from=new Vector4(1,0,0,0);
    }
    public sealed class AutonomousSeason
    {
        public SeasonSaveData State {get;private set;}=new SeasonSaveData();
        public bool Winter=>State.season==3;
        public string Label=>new[]{"봄","여름","가을","겨울"}[State.season];
        static Vector4 Unit(int i){var v=Vector4.zero;v[i]=1;return v;}
        public Vector4 Weights=>Vector4.Lerp(State.from,Unit(State.season),Mathf.SmoothStep(0,1,State.transition/10));
        public void Select(int season){State.from=Weights;State.season=Mathf.Clamp(season,0,3);State.progress=0;State.transition=0;State.automatic=false;}
        public void Advance(float seconds,int dayMinutes)
        {
            if(seconds<=0)return;
            State.transition=Mathf.Min(10,State.transition+seconds);
            if(!State.automatic)return;
            State.progress+=seconds/(dayMinutes*60f*3);
            while(State.progress>=1){var blend=Weights;State.progress-=1;State.season=(State.season+1)%4;State.from=blend;State.transition=0;}
        }
        public SeasonSaveData Capture()=>JsonUtility.FromJson<SeasonSaveData>(JsonUtility.ToJson(State));
        public void Restore(SeasonSaveData d){Validate(d);State=JsonUtility.FromJson<SeasonSaveData>(JsonUtility.ToJson(d));}
        public static void Validate(SeasonSaveData d)
        {
            AutonomousSaveCodec.Require(d!=null&&d.season>=0&&d.season<4&&AutonomousSaveCodec.Finite(d.progress)&&d.progress>=0&&d.progress<1&&AutonomousSaveCodec.Finite(d.transition)&&d.transition>=0&&d.transition<=10,"Invalid season");
            float total=0;for(int i=0;i<4;i++){AutonomousSaveCodec.Require(AutonomousSaveCodec.Finite(d.from[i])&&d.from[i]>=0&&d.from[i]<=1,"Invalid season blend");total+=d.from[i];}
            AutonomousSaveCodec.Require(Mathf.Abs(total-1)<.001f,"Invalid season weights");
        }
    }

    // Runtime material copies only. Authored meshes/materials and window lighting stay intact.
    public sealed class AutonomousSeasonVisual:MonoBehaviour
    {
        sealed class Entry { public Renderer renderer;public Material[] original,copies;public Color[] colors;public bool[] vegetation,snow; }
        readonly List<Entry> entries=new List<Entry>();
        public void Configure()
        {
            
            GetComponent<FarmCameraOcclusion>()?.Restore();
            foreach(var r in GetComponentsInChildren<MeshRenderer>(true)){
                if(entries.Exists(e=>e.renderer==r))continue;
                if(r.GetComponentInParent<FarmResidentVisual>())continue;
                var original=r.sharedMaterials;var copies=(Material[])original.Clone();var e=new Entry{renderer=r,original=original,copies=copies,colors=new Color[copies.Length],vegetation=new bool[copies.Length],snow=new bool[copies.Length]};bool any=false;
                for(int i=0;i<copies.Length;i++){
                    var m=original[i];if(!m)continue;string n=m.name;
                    bool leaves=n=="Leaves"||n=="LeavesLight"||n=="Leaf"||n=="LeafLight";
                    bool grass=n=="Grass"||r.name=="Grass tuft"||r.transform.parent&&r.transform.parent.name.Contains("GrassCluster");
                    bool flower=n=="Flower"||n=="FlowerGold"||n=="FlowerLavender"||n=="Pink"||r.name=="Spring flower";
                    bool soil=n=="Field149 soil"||n=="Garden150 soil"||n=="Garden150 wood";
                    bool roof=n.StartsWith("Roof")||n.StartsWith("HouseRoof")||n.StartsWith("Slate")||n.StartsWith("Straw")||r.name=="Shelter roof";
                    if(!leaves&&!grass&&!flower&&!roof&&!soil)continue;
                    any=true;e.vegetation[i]=leaves||grass||flower;e.snow[i]=leaves||grass||roof||soil;e.colors[i]=m.color;
                    copies[i]=new Material(m){shader=Resources.Load<Shader>("FarmSeason"),name=m.name+" Season"};
                    copies[i].SetFloat("_SnowAmount",0);
                }
                if(any){r.sharedMaterials=copies;entries.Add(e);}
            }
            GetComponent<FarmCameraOcclusion>()?.Initialize(transform);
        }
        public void Apply(Vector4 w)
        {
            foreach(var e in entries)for(int i=0;i<e.copies.Length;i++)if(e.copies[i]!=e.original[i]){
                Color c=e.colors[i];if(e.vegetation[i]){
                    Color summer=new Color(c.r*.72f,c.g*.92f,c.b*.70f,c.a);
                    Color autumn=new Color(Mathf.Min(1,c.r*1.18f),c.g*.78f,c.b*.55f,c.a);
                    Color winter=Color.Lerp(c,new Color(.49f,.48f,.39f),.65f);
                    c=c*w.x+summer*w.y+autumn*w.z+winter*w.w;
                }
                e.copies[i].color=c;e.copies[i].SetFloat("_SnowAmount",e.snow[i]?w.w:0);
            }
            GetComponent<FarmCameraOcclusion>()?.RefreshLightingColors();
        }
        void OnDestroy(){GetComponent<FarmCameraOcclusion>()?.Restore();foreach(var e in entries){if(e.renderer)e.renderer.sharedMaterials=e.original;for(int i=0;i<e.copies.Length;i++)if(e.copies[i]!=e.original[i]){if(Application.isPlaying)Destroy(e.copies[i]);else DestroyImmediate(e.copies[i]);}}}
    }
}
