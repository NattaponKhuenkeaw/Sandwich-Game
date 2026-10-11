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
        public bool TimeLowVoicePlayed { get; set; }
        public Transform Body;
        public Image TimerFill;
        public Text OrderLabel;
        public Image OrderIcon;
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
            TimeLowVoicePlayed = false;
            WaitLeft = wait;
            RefreshBubble();
        }

        public void RepairBubble()
        {
            var ui = GetComponentInChildren<CustomerOrderUI>(true);
            if (ui != null)
            {
                Bubble = ui.Canvas;
                OrderIcon = ui.OrderIcon;
                OrderLabel = ui.Quantity;
                TimerFill = ui.TimerFill;
            }
            if (Bubble == null) return;
            if (Bubble.worldCamera == null) Bubble.worldCamera = Camera.main;
            Bubble.overrideSorting = true;
            Bubble.sortingOrder = 20;
        }

        public void StyleOrderBubble()
        {
            if (Bubble == null || OrderLabel == null || TimerFill == null) return;
            if (Bubble.GetComponent<CustomerOrderUI>() != null) return;
            Bubble.transform.localScale = Vector3.one * 0.0044f;
            var rounded = Resources.Load<Sprite>("OrderPanelRounded");
            if (rounded == null) return;
            var background = Bubble.GetComponentsInChildren<Image>(true);
            Image panel = null;
            foreach (var image in background)
                if (image.transform.parent == Bubble.transform && image.name == "Img" && image != OrderIcon && image.rectTransform.sizeDelta.y > 50f)
                { panel = image; break; }
            if (panel == null) return;
            Image Layer(string name, Vector2 size, Vector2 position, Color color)
            {
                var child = Bubble.transform.Find(name);
                var image = child != null ? child.GetComponent<Image>() : null;
                if (image == null)
                {
                    var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(Bubble.transform, false);
                    image = go.GetComponent<Image>();
                }
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
                image.color = color;
                image.raycastTarget = false;
                image.rectTransform.sizeDelta = size;
                image.rectTransform.anchoredPosition = position;
                return image;
            }
            var shadow = Layer("OrderPanelShadow", new Vector2(230f, 108f), new Vector2(3f, -5f), new Color(0.12f, 0.07f, 0.03f, 0.24f));
            var frame = Layer("OrderPanelFrame", new Vector2(226f, 106f), Vector2.zero, new Color(0.42f, 0.25f, 0.12f, 0.92f));
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            panel.color = new Color(1f, 0.96f, 0.86f, 0.88f);
            panel.raycastTarget = false;
            panel.rectTransform.sizeDelta = new Vector2(220f, 100f);
            panel.rectTransform.anchoredPosition = Vector2.zero;
            shadow.transform.SetAsFirstSibling();
            frame.transform.SetSiblingIndex(1);
            panel.transform.SetSiblingIndex(2);
            var timerBack = TimerFill.transform.parent.GetComponent<Image>();
            if (timerBack != null)
            {
                timerBack.sprite = rounded;
                timerBack.type = Image.Type.Sliced;
                timerBack.color = new Color(0.28f, 0.18f, 0.1f, 0.2f);
                timerBack.rectTransform.sizeDelta = new Vector2(178f, 10f);
                timerBack.rectTransform.anchoredPosition = new Vector2(0f, -33f);
                timerBack.raycastTarget = false;
            }
            TimerFill.raycastTarget = false;
            OrderLabel.fontStyle = FontStyle.Bold;
            OrderLabel.fontSize = 34;
            OrderLabel.color = new Color(0.28f, 0.15f, 0.07f, 1f);
            OrderLabel.raycastTarget = false;
        }

        public void FaceCamera(Camera cam)
        {
            if (Bubble == null || cam == null) return;
            var t = Bubble.transform;
            t.LookAt(t.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
            if (Bubble.worldCamera == null)
                Bubble.worldCamera = cam;
        }

        public void EnsureOrderIcon()
        {
            if (OrderLabel == null) return;
            if (Bubble != null && Bubble.GetComponent<CustomerOrderUI>() != null) return;
            if (OrderIcon == null)
            {
                var existing = OrderLabel.transform.parent.Find("BurgerOrderIcon");
                if (existing != null) OrderIcon = existing.GetComponent<Image>();
                if (OrderIcon == null)
                {
                    var go = new GameObject("BurgerOrderIcon", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(OrderLabel.transform.parent, false);
                    OrderIcon = go.GetComponent<Image>();
                }
            }
            OrderIcon.sprite = Resources.Load<Sprite>("BurgerOrder");
            OrderIcon.color = Color.white;
            OrderIcon.preserveAspect = true;
            OrderIcon.raycastTarget = false;
            OrderIcon.rectTransform.sizeDelta = new Vector2(72f, 64f);
            OrderIcon.rectTransform.anchoredPosition = new Vector2(-35f, 10f);
            OrderLabel.rectTransform.sizeDelta = new Vector2(100f, 50f);
            OrderLabel.rectTransform.anchoredPosition = new Vector2(45f, 10f);
            if (Bubble.transform.Find("OrderPanelFrame") == null) StyleOrderBubble();
        }

        public void RefreshBubble()
        {
            EnsureOrderIcon();
            if (OrderIcon != null) OrderIcon.gameObject.SetActive(Remaining > 0);
            if (OrderLabel != null)
            {
                if (Bubble == null || Bubble.GetComponent<CustomerOrderUI>() == null)
                    OrderLabel.rectTransform.anchoredPosition = Remaining <= 0 ? new Vector2(0f, 10f) : new Vector2(45f, 10f);
                OrderLabel.text = Remaining <= 0
                    ? "ขอบคุณ!"
                    : "x" + Remaining;
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
