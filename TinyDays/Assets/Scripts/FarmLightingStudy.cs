using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TinyDays.Review
{
    // Review day length at 1x. Residents and lighting share the playback rate.
    public sealed class FarmLightingStudy : MonoBehaviour
    {
        public Light sun;
        public Camera reviewCamera;
        public int selected=1;
        public const float DaySeconds=300;
        public float Hour {get;private set;}=12;
        public int Day {get;private set;}=1;
        public bool Automatic {get;private set;}=true;
        static readonly int[] Chronological={3,0,1,2,3};
        static readonly float[] Hours={6,12,18,0};
        static readonly float[] Intensities={.95f,1.35f,1.05f,.38f};
        static readonly Vector3[] Rotations={new Vector3(14,-72,0),new Vector3(48,-32,0),new Vector3(12,108,0),new Vector3(38,-55,0)};
        public static readonly string[] Names={"일출 06:00","낮 12:00","일몰 18:00","밤 00:00"};
        readonly Dictionary<Renderer,Material> originals=new Dictionary<Renderer,Material>();
        readonly List<Material> copies=new List<Material>();
        readonly List<Renderer> windows=new List<Renderer>();
        Material backdrop;
        readonly Dictionary<Renderer,Material> windowMaterials=new Dictionary<Renderer,Material>();
        LightingColors colors;
        public LightingColors Colors=>colors??(colors=new LightingColors());
        public void LoadPreferences(){colors=new LightingColors(k=>PlayerPrefs.GetString(k,""),(k,v)=>{PlayerPrefs.SetString(k,v);PlayerPrefs.Save();});}
        void Start(){LoadPreferences();RestartClock();}
        void Update(){var director=GetComponent<FarmLifeDirector>();Advance(Time.unscaledDeltaTime,director&&director.paused);}
        public void Advance(float seconds,bool paused)
        {
            if(!Automatic||paused)return;
            var director=GetComponent<FarmLifeDirector>();var playback=director?director.Playback:null;
            float step=playback!=null?playback.ScaledSeconds(seconds,false):Mathf.Max(0,seconds);
            float length=playback!=null?playback.DayMinutes*60:DaySeconds;
            float hours=step*24/length;
            Day+=Mathf.FloorToInt((Hour+hours)/24f);
            Hour=Mathf.Repeat(Hour+hours,24);RenderHour();
        }
        public void ResumeClock(){Automatic=true;RenderHour();}
        public void RestartClock(){Day=1;Hour=12;Automatic=true;RenderHour();}
        public void Apply(int index)
        {
            selected=Mathf.Clamp(index,0,3);
            Hour=Hours[selected];Automatic=false;RenderHour();
        }
        Color Ambient(int t,bool ground)
        {
            var sky=Colors.Get(t,2);if(t!=1)return sky*(ground?.5f:.78f);
            var basis=LightingColors.Default(t,2);var c=ground?new Color(.36f,.32f,.25f):new Color(.52f,.55f,.45f);
            return new Color(sky.r*c.r/basis.r,sky.g*c.g/basis.g,sky.b*c.b/basis.b);
        }
        Color CameraBackground(int t)
        {
            var c=Colors.Get(t,0);if(c!=LightingColors.Default(t,0))return c;
            return t==1?new Color(.894f,.871f,.792f):t==3?new Color(.17f,.23f,.35f):c;
        }
        void RenderHour()
        {
            var occlusion=GetComponent<FarmCameraOcclusion>();
            if(originals.Count==0)
            {
                if(occlusion)occlusion.Restore();
                foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
                {
                    var source=renderer.sharedMaterial;
                    bool window=source&&(source.name=="Glass"||source.name=="HouseGlass");
                    if(!window&&renderer.name!="Backdrop")continue;
                    var copy=new Material(source);copy.name=source.name+" Lighting study";originals.Add(renderer,source);copies.Add(copy);
                    renderer.sharedMaterial=copy;
                    if(window){windows.Add(renderer);windowMaterials.Add(renderer,copy);}else {backdrop=copy;backdrop.shader=Resources.Load<Shader>("LightingBackdrop");}
                }
                if(occlusion)occlusion.Initialize(transform);
            }
            int segment=Mathf.FloorToInt(Hour/6)%4,a=Chronological[segment],b=Chronological[segment+1];
            float u=Mathf.SmoothStep(0,1,(Hour%6)/6);
            selected=a;
            Color sky=Color.Lerp(Colors.Get(a,2),Colors.Get(b,2),u),light=Color.Lerp(Colors.Get(a,1),Colors.Get(b,1),u),background=Color.Lerp(Colors.Get(a,0),Colors.Get(b,0),u);
            sun.color=light;sun.intensity=Mathf.Lerp(Intensities[a],Intensities[b],u);sun.transform.rotation=Quaternion.Slerp(Quaternion.Euler(Rotations[a]),Quaternion.Euler(Rotations[b]),u);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=sky;
            RenderSettings.ambientEquatorColor=Color.Lerp(Ambient(a,false),Ambient(b,false),u);
            RenderSettings.ambientGroundColor=Color.Lerp(Ambient(a,true),Ambient(b,true),u);
            reviewCamera.backgroundColor=Color.Lerp(CameraBackground(a),CameraBackground(b),u);
            if(backdrop)backdrop.SetColor("_BaseColor",background);
            float glow=Mathf.Lerp(a==1?0:a==3?1.05f:.4f,b==1?0:b==3?1.05f:.4f,u);
            float windowBlend=Mathf.Lerp(a==1?0:1,b==1?0:1,u);
            foreach(var renderer in windows)
            {
                var mat=windowMaterials[renderer];
                Color tint=WindowTint(renderer.transform);
                mat.color=Color.Lerp(new Color(.263f,.369f,.376f),tint,windowBlend);
                mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",glow>0?tint*glow:Color.black);
            }
            if(occlusion)occlusion.RefreshLightingColors();
        }
        public static Color WindowTint(Transform window)
        {
            // Stable hierarchy identity, never randomized by process, time, or camera position.
            string key="";for(var t=window;t&&t.name!="GeneratedFarmStudy";t=t.parent)key=t.name+"/"+t.GetSiblingIndex()+"/"+key;
            uint hash=2166136261;foreach(char c in key)hash=unchecked((hash^c)*16777619);
            Color[] palette={new Color(1,.64f,.32f),new Color(1,.88f,.48f),new Color(.71f,.89f,.60f),new Color(.62f,.78f,1),new Color(1,.74f,.47f)};
            return palette[hash%(uint)palette.Length];
        }
        void OnDestroy()
        {
            var occlusion=GetComponent<FarmCameraOcclusion>();if(occlusion)occlusion.Restore();
            foreach(var pair in originals)if(pair.Key)pair.Key.sharedMaterial=pair.Value;
            foreach(var copy in copies){if(Application.isPlaying)Destroy(copy);else DestroyImmediate(copy);}
            originals.Clear();copies.Clear();windows.Clear();
        }
    }
}
