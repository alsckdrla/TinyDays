using UnityEngine;

namespace TinyDays.Review
{
    // Review-only modal. The store is independent from IMGUI and PlayerPrefs.
    public sealed class LightingColorPicker
    {
        public bool Open=>lighting!=null;
        public bool EscapeConsumed;
        FarmLightingStudy lighting;
        int time,kind,drag;
        Color before;
        float hue,saturation,value;
        string hex;readonly string[] rgb=new string[3];
        Texture2D sv,hues;
        public void Begin(FarmLightingStudy owner,int t,int k)
        {
            lighting=owner;time=t;kind=k;before=owner.Colors.Get(t,k);Sync(before);owner.Apply(t);GUI.FocusControl(null);
        }
        void Sync(Color c)
        {
            Color.RGBToHSV(c,out hue,out saturation,out value);hex="#"+ColorUtility.ToHtmlStringRGB(c);
            rgb[0]=Mathf.RoundToInt(c.r*255).ToString();rgb[1]=Mathf.RoundToInt(c.g*255).ToString();rgb[2]=Mathf.RoundToInt(c.b*255).ToString();
            RefreshTexture();
        }
        void RefreshTexture()
        {
            if(!sv){sv=new Texture2D(96,96,TextureFormat.RGB24,false);sv.hideFlags=HideFlags.HideAndDontSave;}
            for(int y=0;y<96;y++)for(int x=0;x<96;x++)sv.SetPixel(x,y,Color.HSVToRGB(hue,x/95f,y/95f));sv.Apply();
            if(!hues)
            {
                hues=new Texture2D(1,192,TextureFormat.RGB24,false);hues.hideFlags=HideFlags.HideAndDontSave;
                for(int y=0;y<192;y++)hues.SetPixel(0,y,Color.HSVToRGB(1-y/191f,1,1));hues.Apply();
            }
        }
        void Preview(Color c){lighting.Colors.Set(time,kind,c);lighting.Apply(time);}
        public void Cancel(){if(!Open)return;Preview(before);lighting=null;drag=0;}
        public void Dispose(){Cancel();if(sv)Object.Destroy(sv);if(hues)Object.Destroy(hues);}
        public void Draw(float width,float height)
        {
            if(!Open)return;
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){Cancel();EscapeConsumed=true;Event.current.Use();return;}
            GUI.depth=-100;
            GUI.skin.textField.fontSize=14;
            float w=Mathf.Min(410,width-24),h=Mathf.Min(430,height-24);
            var p=new Rect((width-w)/2,(height-h)/2,w,h);Swatch(p,new Color(.12f,.13f,.12f));GUI.Box(p,"");
            GUI.skin.label.normal.textColor=Color.white;
            GUI.Label(new Rect(p.x+16,p.y+10,w-32,26),FarmLightingStudy.Names[time]+" · "+LightingColors.Labels[kind]);
            float sq=Mathf.Min(205,h-218);
            var square=new Rect(p.x+16,p.y+44,w-80,sq);var strip=new Rect(square.xMax+12,square.y,24,sq);
            GUI.DrawTexture(square,sv);GUI.DrawTexture(strip,hues);
            var e=Event.current;
            if(e.type==EventType.MouseDown&&e.button==0){if(square.Contains(e.mousePosition))drag=1;else if(strip.Contains(e.mousePosition))drag=2;}
            if(drag!=0&&(e.type==EventType.MouseDown||e.type==EventType.MouseDrag))
            {
                GUI.FocusControl(null);
                if(drag==1){saturation=Mathf.Clamp01((e.mousePosition.x-square.x)/square.width);value=1-Mathf.Clamp01((e.mousePosition.y-square.y)/square.height);}
                else hue=Mathf.Clamp01((e.mousePosition.y-strip.y)/strip.height);
                var c=Color.HSVToRGB(hue,saturation,value);float rememberedHue=hue;Sync(c);hue=rememberedHue;RefreshTexture();Preview(c);e.Use();
            }
            if(e.type==EventType.MouseUp)drag=0;
            GUI.Box(new Rect(square.x+saturation*square.width-4,square.y+(1-value)*square.height-4,8,8),"");
            GUI.Box(new Rect(strip.x-2,strip.y+hue*strip.height-2,strip.width+4,4),"");
            float y=square.yMax+10;
            GUI.Label(new Rect(p.x+16,y,45,24),"이전");Swatch(new Rect(p.x+62,y,65,24),before);
            GUI.Label(new Rect(p.x+143,y,45,24),"현재");Swatch(new Rect(p.x+190,y,65,24),lighting.Colors.Get(time,kind));
            y+=32;
            bool edited=false;
            for(int k=0;k<3;k++)
            {
                float x=p.x+16+k*(w-32)/3;GUI.Label(new Rect(x,y,18,24),new[]{"R","G","B"}[k]);
                var s=GUI.TextField(new Rect(x+20,y,(w-32)/3-26,24),rgb[k],3);if(s!=rgb[k]){rgb[k]=s;edited=true;}
            }
            if(edited&&int.TryParse(rgb[0],out int r)&&int.TryParse(rgb[1],out int g)&&int.TryParse(rgb[2],out int b)&&r>=0&&r<=255&&g>=0&&g<=255&&b>=0&&b<=255)
            {var c=new Color(r/255f,g/255f,b/255f);Color.RGBToHSV(c,out hue,out saturation,out value);hex="#"+ColorUtility.ToHtmlStringRGB(c);RefreshTexture();Preview(c);}
            y+=32;GUI.Label(new Rect(p.x+16,y,50,24),"Hex");
            var input=GUI.TextField(new Rect(p.x+68,y,w-84,24),hex,7);
            if(input!=hex){hex=input;if(LightingColors.TryHex(hex,out var c)){Sync(c);Preview(c);}}
            y+=34;
            if(GUI.Button(new Rect(p.x+16,y,w-32,26),"기본색 복원")){var c=LightingColors.Default(time,kind);Sync(c);Preview(c);GUI.FocusControl(null);}
            y+=34;
            if(GUI.Button(new Rect(p.x+16,y,(w-40)/2,28),"취소"))Cancel();
            if(GUI.Button(new Rect(p.x+24+(w-40)/2,y,(w-40)/2,28),"확인")&&Open){lighting.Colors.Save(time,kind);lighting=null;drag=0;}
            GUI.depth=0;
        }
        public static void Swatch(Rect rect,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    }
}
