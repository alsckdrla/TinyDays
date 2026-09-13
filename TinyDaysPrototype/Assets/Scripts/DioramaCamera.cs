using UnityEngine;

namespace TinyDays
{
    [RequireComponent(typeof(Camera))]
    public sealed class DioramaCamera : MonoBehaviour
    {
        public float moveSpeed = 8, zoomSpeed = 12, minZoom = 6, maxZoom = 19;
        public Vector2 panLimits = new Vector2(8, 7);
        Vector3 home;
        float homeZoom;
        Camera view;
        public void Awake() { view = GetComponent<Camera>(); home = transform.position; homeZoom = view.orthographicSize; }
        void Update()
        {
            Pan(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), Time.unscaledDeltaTime);
            Zoom(Input.mouseScrollDelta.y);
            if (Input.GetKeyDown(KeyCode.Home)) ResetView();
        }
        public void Pan(Vector2 direction, float delta)
        {
            var right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            var forward = Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized;
            var p = transform.position + (right * direction.x + forward * direction.y) * (moveSpeed * delta);
            p.x = Mathf.Clamp(p.x, home.x - panLimits.x, home.x + panLimits.x);
            p.z = Mathf.Clamp(p.z, home.z - panLimits.y, home.z + panLimits.y);
            transform.position = p;
        }
        public void Zoom(float scroll) { view.orthographicSize = Mathf.Clamp(view.orthographicSize - scroll * zoomSpeed * .1f, minZoom, maxZoom); }
        public void ResetView() { transform.position = home; view.orthographicSize = homeZoom; }
    }
}
