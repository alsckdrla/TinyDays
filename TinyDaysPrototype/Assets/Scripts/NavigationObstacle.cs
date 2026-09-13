using UnityEngine;

namespace TinyDays
{
    [ExecuteAlways]
    public sealed class NavigationObstacle : MonoBehaviour
    {
        public bool circle;
        public Vector2 size = Vector2.one;
        public Vector2 center;
        public float radius = .3f;
        public bool road;
        public static int Revision { get; private set; }
        Matrix4x4 previous;
        Vector2 previousSize,previousCenter;
        float previousRadius;
        bool previousCircle,previousRoad;
        void OnEnable() { previous=transform.localToWorldMatrix; Revision++; }
        void OnDisable() { Revision++; }
        void OnValidate() { Revision++; }
        void Update() { CheckForChanges(); }
        public void CheckForChanges()
        {
            if(previous!=transform.localToWorldMatrix||previousSize!=size||previousCenter!=center||previousRadius!=radius||previousCircle!=circle||previousRoad!=road)
            {
                previous=transform.localToWorldMatrix; previousSize=size; previousCenter=center; previousRadius=radius; previousCircle=circle; previousRoad=road; Revision++;
            }
        }
        public bool Contains(Vector3 position,float padding)
        {
            var p=transform.InverseTransformPoint(position);
            var scale=transform.lossyScale;
            var offset=new Vector2((p.x-center.x)*Mathf.Abs(scale.x),(p.z-center.y)*Mathf.Abs(scale.z));
            if(circle) return offset.magnitude<radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z))+padding;
            var half=new Vector2(size.x*Mathf.Abs(scale.x),size.y*Mathf.Abs(scale.z))*.5f;
            var outside=new Vector2(Mathf.Max(0,Mathf.Abs(offset.x)-half.x),Mathf.Max(0,Mathf.Abs(offset.y)-half.y));
            return outside.sqrMagnitude<padding*padding || (Mathf.Abs(offset.x)<=half.x && Mathf.Abs(offset.y)<=half.y);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color=road?Color.yellow:Color.red; Gizmos.matrix=transform.localToWorldMatrix;
            if(circle) Gizmos.DrawWireSphere(new Vector3(center.x,.2f,center.y),radius);
            else Gizmos.DrawWireCube(new Vector3(center.x,.2f,center.y),new Vector3(size.x,.4f,size.y));
        }
    }
}
