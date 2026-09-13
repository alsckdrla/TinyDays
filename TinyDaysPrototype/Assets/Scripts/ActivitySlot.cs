using System.Collections.Generic;
using UnityEngine;
namespace TinyDays
{
    public sealed class ActivitySlot : MonoBehaviour
    {
        static readonly HashSet<ActivitySlot> live = new HashSet<ActivitySlot>();
        public string slotId;
        public ResidentAction action;
        public Transform lookTarget;
        public Vector3 carryDestination;
        public CropPlot cropPlot;
        [HideInInspector] public int reservedBy = -1;
        [HideInInspector] public bool blocked;
        public bool IsAvailable => !blocked && reservedBy < 0 && gameObject.activeInHierarchy;
        internal static IEnumerable<ActivitySlot> Live => live;
        void OnEnable() { live.Add(this); }
        void OnDisable() { live.Remove(this); reservedBy=-1; }
    }

}
