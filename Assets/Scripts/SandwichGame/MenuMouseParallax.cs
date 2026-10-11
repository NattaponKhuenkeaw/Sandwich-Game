using UnityEngine;
using UnityEngine.InputSystem;
namespace SandwichGame
{
    [DefaultExecutionOrder(-100)]
    public class MenuMouseParallax : MonoBehaviour
    {
        public Camera MenuCamera;
        public RectTransform Logo;
        public Vector2 CameraMovement = new Vector2(0.035f, 0.025f);
        public Vector2 CameraTilt = new Vector2(0.45f, 0.65f);
        public Vector2 LogoMovement = new Vector2(8f, 5f);
        [Min(0.01f)] public float SmoothTime = 0.2f;
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        Vector2 logoPosition;
        Vector2 offset;
        bool initialized;

        void OnEnable()
        {
            if (MenuCamera != null)
            {
                cameraPosition = MenuCamera.transform.localPosition;
                cameraRotation = MenuCamera.transform.localRotation;
            }
            if (Logo != null) logoPosition = Logo.anchoredPosition;
            offset = Vector2.zero;
            initialized = true;
        }

        void LateUpdate()
        {
            Vector2 target = Vector2.zero;
            var mouse = Mouse.current;
            if (Application.isFocused && mouse != null && Screen.width > 0 && Screen.height > 0)
            {
                Vector2 position = mouse.position.ReadValue();
                if (position.x >= 0 && position.y >= 0 && position.x <= Screen.width && position.y <= Screen.height)
                    target = new Vector2(position.x / Screen.width * 2f - 1f, position.y / Screen.height * 2f - 1f);
            }
            float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(.01f, SmoothTime));
            offset = Vector2.Lerp(offset, target, blend);
            if (MenuCamera != null)
            {
                MenuCamera.transform.localPosition = cameraPosition + cameraRotation * new Vector3(offset.x * CameraMovement.x, offset.y * CameraMovement.y, 0f);
                MenuCamera.transform.localRotation = cameraRotation * Quaternion.Euler(-offset.y * CameraTilt.x, offset.x * CameraTilt.y, 0f);
            }
            if (Logo != null) Logo.anchoredPosition = logoPosition + Vector2.Scale(offset, LogoMovement);
        }

        void OnDisable()
        {
            if (!initialized) return;
            if (MenuCamera != null)
            {
                MenuCamera.transform.localPosition = cameraPosition;
                MenuCamera.transform.localRotation = cameraRotation;
            }
            if (Logo != null) Logo.anchoredPosition = logoPosition;
            initialized = false;
        }
    }
}
