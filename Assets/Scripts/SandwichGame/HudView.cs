using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SandwichGame
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] GameplayHudView gameplayHud;
        [SerializeField] Text hintText;
        [SerializeField] Text bannerText;
        [SerializeField] Text floatingText;
        [SerializeField] GameObject overlay;
        [SerializeField] Text overlayTitle;
        [SerializeField] Text overlayBody;
        [SerializeField] Button overlayButton;
        [SerializeField] Text overlayButtonLabel;
        float bannerUntil;
        float floatingUntil;
        [SerializeField] Vector3 floatingStart;

        public void EnsureBuilt()
        {
            if (overlay != null) return;
            Build();
        }

        public static HudView Create()
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HudView>();
            hud.Build();
            return hud;
        }

        public void ShowGameOver(int score, int round, UnityAction onRetry)
        {
            overlay.SetActive(true);
            overlayTitle.text = "แพ้แล้ว";
            overlayBody.text = "รอบ " + round + "\nคะแนน " + score;
            overlayButtonLabel.text = "เล่นอีกครั้ง";
            overlayButton.onClick.RemoveAllListeners();
            overlayButton.onClick.AddListener(onRetry);
        }

        public void HideOverlay()
        {
            overlay.SetActive(false);
        }

        public void Refresh(int round, float timeLeft, int score, int target, int lives)
        {
            gameplayHud.Refresh(round, timeLeft, score, target, lives);
        }

        public void ShowBanner(string message, float seconds)
        {
            bannerText.text = message;
            bannerUntil = Time.unscaledTime + seconds;
            bannerText.gameObject.SetActive(true);
        }

        public void FloatScore(string message)
        {
            floatingText.text = message;
            floatingText.gameObject.SetActive(true);
            floatingText.rectTransform.anchoredPosition = floatingStart;
            floatingUntil = Time.unscaledTime + 1.1f;
        }

        void Update()
        {
            if (bannerText != null && bannerText.gameObject.activeSelf && Time.unscaledTime > bannerUntil)
                bannerText.gameObject.SetActive(false);

            if (floatingText != null && floatingText.gameObject.activeSelf)
            {
                var rt = floatingText.rectTransform;
                rt.anchoredPosition += new Vector2(0f, 70f * Time.unscaledDeltaTime);
                if (Time.unscaledTime > floatingUntil)
                    floatingText.gameObject.SetActive(false);
            }

            if (hintText != null)
                hintText.enabled = overlay == null || !overlay.activeSelf;
        }

        void Build()
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            gameplayHud = Instantiate(Resources.Load<GameplayHudView>("UI/GameplayHud"), canvasGo.transform);

            hintText = Label(canvasGo.transform, new Vector2(0f, 48f), 1400f, "คลิกค้างเนื้อ → เตา 10 วินาที → จาน + ขนมปัง → ส่งลูกค้า", TextAnchor.MiddleCenter, 26);
            var hintRt = hintText.rectTransform;
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 48f);

            bannerText = Label(canvasGo.transform, Vector2.zero, 900f, "", TextAnchor.MiddleCenter, 48);
            bannerText.gameObject.SetActive(false);

            floatingText = Label(canvasGo.transform, new Vector2(0f, 80f), 400f, "+100", TextAnchor.MiddleCenter, 44);
            floatingText.color = new Color(1f, 0.92f, 0.35f);
            floatingText.gameObject.SetActive(false);
            floatingStart = new Vector3(0f, 80f, 0f);

            overlay = Panel(canvasGo.transform, Vector2.zero, new Vector2(2400f, 1400f), new Color(0.06f, 0.05f, 0.04f, 0.82f)).gameObject;
            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            overlayTitle = Label(overlay.transform, new Vector2(0f, 140f), 900f, "Sandwich Game", TextAnchor.MiddleCenter, 64);
            overlayBody = Label(overlay.transform, new Vector2(0f, 10f), 980f, "", TextAnchor.MiddleCenter, 32);
            overlayBody.color = new Color(0.92f, 0.88f, 0.80f);

            var buttonGo = Panel(overlay.transform, new Vector2(0f, -160f), new Vector2(360f, 84f), new Color(0.86f, 0.42f, 0.22f));
            buttonGo.GetComponent<Image>().raycastTarget = true;
            overlayButton = buttonGo.AddComponent<Button>();
            var colors = overlayButton.colors;
            colors.highlightedColor = new Color(0.95f, 0.52f, 0.28f);
            colors.pressedColor = new Color(0.70f, 0.30f, 0.16f);
            overlayButton.colors = colors;
            overlayButtonLabel = Label(buttonGo.transform, Vector2.zero, 340f, "เล่นอีกครั้ง", TextAnchor.MiddleCenter, 36);
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        static GameObject Panel(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = UiUtil.White();
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return go;
        }

        static Text Label(Transform parent, Vector2 pos, float width, string text, TextAnchor align, int size = 32)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = FontUtil.Thai();
            label.text = text;
            label.fontSize = size;
            label.alignment = align;
            label.color = new Color(0.96f, 0.94f, 0.90f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            var rt = label.rectTransform;
            rt.sizeDelta = new Vector2(width, 70f);
            rt.anchoredPosition = pos;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return label;
        }
    }
}
