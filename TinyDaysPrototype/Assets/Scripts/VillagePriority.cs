using UnityEngine;

namespace TinyDays
{
    public enum VillagePriorityMode { Production, Balanced, Leisure }

    public sealed class VillagePriority : MonoBehaviour
    {
        public VillagePriorityMode mode = VillagePriorityMode.Balanced;
        public float WorkMultiplier => mode==VillagePriorityMode.Production ? 1.6f : mode==VillagePriorityMode.Leisure ? .6f : 1f;
        public float CarryMultiplier => WorkMultiplier;
        public float RestMultiplier => mode==VillagePriorityMode.Production ? .45f : mode==VillagePriorityMode.Leisure ? 1.4f : 1f;
        public float AppreciateMultiplier => RestMultiplier;
        public void Set(VillagePriorityMode value) { mode=value; }
    }
}
