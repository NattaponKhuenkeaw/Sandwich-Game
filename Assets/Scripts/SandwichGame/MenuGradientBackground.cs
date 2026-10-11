using UnityEngine;
namespace SandwichGame
{
    [ExecuteAlways]
    public class MenuGradientBackground : MonoBehaviour
    {
        public Camera MenuCamera;
        void LateUpdate()
        {
            if (MenuCamera == null) return;
            float distance = Mathf.Min(50f, MenuCamera.farClipPlane * .8f);
            transform.SetPositionAndRotation(MenuCamera.transform.position + MenuCamera.transform.forward * distance, MenuCamera.transform.rotation);
            float height = MenuCamera.orthographic ? MenuCamera.orthographicSize * 2f : 2f * distance * Mathf.Tan(MenuCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            transform.localScale = new Vector3(height * MenuCamera.aspect * 1.02f, height * 1.02f, 1f);
        }
    }
}
