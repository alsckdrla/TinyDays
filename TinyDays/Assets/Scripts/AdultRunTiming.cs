using System;
using UnityEngine;

namespace TinyDays.Review
{
    public static class AdultRunTiming
    {
        [Serializable] public sealed class Spec
        {
            public float duration,speed,stance;
            public string[] names;
            public float[] phases;
        }
        static Spec cached;
        public static Spec Data => cached??(cached=JsonUtility.FromJson<Spec>(Resources.Load<TextAsset>("AdultRunTiming").text));
        public static float Phase(int index)=>Data.phases[index%4]+(index>=4?.5f:0);
        public static string Label(int index)=>(index>=4?"R ":"L ")+Data.names[index%4];
    }
}
