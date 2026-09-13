using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays
{
    public enum ResidentAction { None, Work, Carry, Rest, Appreciate }

    public sealed class ActivityCoordinator : MonoBehaviour
    {
        readonly List<ActivitySlot> slots = new List<ActivitySlot>();
        public IReadOnlyList<ActivitySlot> Slots { get { Refresh(); return slots; } }
        void Awake() { Refresh(); }
        void Start() { Refresh(); }
        public void Refresh()
        {
            slots.Clear();
            // Slots register as they enable, avoiding hierarchy-discovery timing during scene rebuild.
            slots.AddRange(ActivitySlot.Live.Where(slot=>slot&&slot.transform.IsChildOf(transform)));
        }
        public bool TryReserve(ResidentAction action, int residentId, System.Random random, out ActivitySlot slot, System.Func<ActivitySlot,bool> allowed=null)
        {
            Refresh();
            var candidates = slots.Where(s=>s.action==action && s.IsAvailable && (allowed==null||allowed(s))).ToList();
            if (candidates.Count == 0) { slot=null; return false; }
            slot=candidates[random.Next(candidates.Count)]; slot.reservedBy=residentId; return true;
        }
        public void Release(ActivitySlot slot, int residentId)
        {
            if (slot && slot.reservedBy==residentId) slot.reservedBy=-1;
        }
        public bool IsStillReserved(ActivitySlot slot, int residentId) => slot && !slot.blocked && slot.reservedBy==residentId && slot.gameObject.activeInHierarchy;
        public int ReservedCount { get { Refresh(); return slots.Count(s=>s.reservedBy>=0); } }
    }
}
