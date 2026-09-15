using UnityEngine;

namespace TinyDays.Review
{
    // Authored empty interior, separate from render meshes and physics/camera movement.
    public sealed class FarmInteriorVolume : MonoBehaviour
    {
        public Vector3 center, size;
        public float roofRise;
        public bool Contains(Vector3 world)
        {
            Vector3 p=transform.InverseTransformPoint(world)-center;
            if(Mathf.Abs(p.x)>size.x*.5f||Mathf.Abs(p.z)>size.z*.5f||p.y< -size.y*.5f)return false;
            float top=size.y*.5f+roofRise*(1-Mathf.Abs(p.x)/(size.x*.5f));
            return p.y<=top;
        }
    }
}
