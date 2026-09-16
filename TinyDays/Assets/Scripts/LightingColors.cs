using System;
using UnityEngine;

namespace TinyDays.Review
{
    public sealed class LightingColors
    {
        public static readonly string[] Labels={"배경","햇빛","주변광"};
        readonly Color[,] values=new Color[4,3];
        readonly Func<string,string> read;
        readonly Action<string,string> write;
        public LightingColors(Func<string,string> read=null,Action<string,string> write=null)
        {
            this.read=read;this.write=write;
            for(int t=0;t<4;t++)for(int k=0;k<3;k++)
                values[t,k]=TryHex(read?.Invoke(Key(t,k)),out var c)?c:Default(t,k);
        }
        static string Key(int t,int k)=>"TinyDays.Lighting.v044."+t+"."+k;
        public Color Get(int t,int k)=>values[t,k];
        public void Set(int t,int k,Color c){values[t,k]=new Color(Mathf.Clamp01(c.r),Mathf.Clamp01(c.g),Mathf.Clamp01(c.b),1);}
        public void Save(int t,int k)=>write?.Invoke(Key(t,k),ColorUtility.ToHtmlStringRGB(Get(t,k)));
        public static bool TryHex(string s,out Color c)
        {
            c=Color.black;if(string.IsNullOrEmpty(s))return false;
            s=s.Trim().TrimStart('#');return s.Length==6&&ColorUtility.TryParseHtmlString("#"+s,out c);
        }
        public static Color Default(int t,int k)
        {
            if(k==0){TryHex(new[]{"8A4636","FFF2C6","A66A59","0C183B"}[t],out var c);return c;}
            if(k==1)return new[]{new Color(1,.79f,.65f),new Color(1,.93f,.79f),new Color(1,.84f,.70f),new Color(.63f,.74f,1)}[t];
            return new[]{new Color(.65f,.59f,.57f),new Color(.68f,.75f,.79f),new Color(.70f,.65f,.62f),new Color(.28f,.36f,.53f)}[t];
        }
    }
}
