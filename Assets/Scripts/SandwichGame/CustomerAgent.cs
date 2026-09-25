using UnityEngine;
using UnityEngine.UI;

namespace SandwichGame
{
    [ExecuteAlways]
    public class CustomerAgent : MonoBehaviour
    {
        public int SlotIndex;
        public int Wanted;
        public int Received;
        public float WaitLeft;
        public bool Busy;
        public Transform Body;
        public Image TimerFill;
        public Text OrderLabel;
        public Canvas Bubble;

        public int Remaining => Mathf.Max(0, Wanted - Received);

        void OnEnable()
        {
            RepairBubble();
        }

        void Awake()
        {
            RepairBubble();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            RepairBubble();
        }
#endif

        public void SetOrder(int wanted, float wait)
        {
            Wanted = Mathf.Clamp(wanted, 1, GameConstants.MaxHamburgersPerOrder);
            Received = 0;
            WaitLeft = wait;
            RefreshBubble();
        }

        public void RepairBubble()
        {
            var sprite = UiUtil.White();
            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                img.sprite = sprite;

                var cr = img.canvasRenderer;
                if (cr != null) cr.cullTransparentMesh = false;

                var color = img.color;
                if (color.a < 1f && color.r > 0.85f && color.g > 0.85f && color.b > 0.85f)
                    img.color = new Color(color.r, color.g, color.b, 1f);
            }

            if (Bubble != null)
            {
                if (Bubble.worldCamera == null)
                    Bubble.worldCamera = Camera.main;
                Bubble.overrideSorting = true;
                Bubble.sortingOrder = 20;
            }
        }

        public void FaceCamera(Camera cam)
        {
            if (Bubble == null || cam == null) return;
            var t = Bubble.transform;
            t.LookAt(t.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
            if (Bubble.worldCamera == null)
                Bubble.worldCamera = cam;
        }

        public void RefreshBubble()
        {
            if (OrderLabel != null)
            {
                OrderLabel.text = Remaining <= 0
                    ? "ขอบคุณ!"
                    : "แฮมเบอร์เกอร์ x" + Remaining;
            }

            if (TimerFill != null)
            {
                float t = GameConstants.CustomerWait <= 0.01f ? 0f : WaitLeft / GameConstants.CustomerWait;
                TimerFill.fillAmount = Mathf.Clamp01(t);
                TimerFill.color = t < 0.28f
                    ? new Color(0.86f, 0.22f, 0.22f)
                    : t < 0.55f
                        ? new Color(0.92f, 0.62f, 0.18f)
                        : new Color(0.28f, 0.72f, 0.38f);
            }
        }
    }
}
