using UnityEngine;

namespace TinyDays.Review
{
    // Static model review only. This is not locomotion or an autonomous resident.
    [ExecuteAlways]
    public sealed class RabbitPoseDisplay : MonoBehaviour
    {
        public AnimationClip pose;
        void OnEnable() { Apply(); }
        public void Apply()
        {
            if (pose != null) pose.SampleAnimation(gameObject, 0f);
        }
    }
}
