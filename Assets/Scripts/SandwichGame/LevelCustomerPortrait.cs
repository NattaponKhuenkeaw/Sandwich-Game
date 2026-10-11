using UnityEngine;
namespace SandwichGame
{
    [ExecuteAlways, DefaultExecutionOrder(100), RequireComponent(typeof(Camera))]
    public class LevelCustomerPortrait : MonoBehaviour
    {
        public Transform Subject;
        void LateUpdate() { FramePortrait(); }
        public void FramePortrait()
        {
            if (Subject == null) return;
            var renderers = Subject.GetComponentsInChildren<Renderer>();
            bool found = false; Bounds bounds = default;
            foreach (var renderer in renderers)
                if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
            if (!found || bounds.size.y <= 0f) return;
            var camera = GetComponent<Camera>();
            Vector3 direction = transform.parent.forward;
            Vector3 up = transform.parent.up;
            Vector3 focus = bounds.center + up * bounds.size.y * .15f;
            float distance = Mathf.Max(.01f, bounds.size.magnitude * 3f);
            transform.SetPositionAndRotation(focus - direction * distance, Quaternion.LookRotation(direction, up));
            camera.orthographic = true;
            camera.orthographicSize = bounds.size.y * .43f;
            camera.nearClipPlane = Mathf.Max(.00001f, distance * .01f);
            camera.farClipPlane = distance * 2f;
        }
    }
}
