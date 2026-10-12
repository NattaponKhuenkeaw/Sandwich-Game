using UnityEngine;
using UnityEngine.UI;

namespace SandwichGame
{
    public class GameplayHudView : MonoBehaviour
    {
        public Text Day, TimeLabel, Sales;
        public Image Progress;
        public Image[] Hearts;
        public Button PauseButton;
        void Awake()
        {
            PauseButton.onClick.AddListener(() =>
            {
                var controller = GetComponentInParent<SandwichGameController>();
                if (controller != null && controller.CanPause)
                {
                    var pause = controller.GetComponentInChildren<PauseMenu>(true);
                    if (pause != null) pause.Pause();
                }
            });
        }
        public void Refresh(int day, float timeLeft, int sales, int target, int lives)
        {
            Day.text = day.ToString();
            int seconds = Mathf.CeilToInt(Mathf.Max(0, timeLeft));
            TimeLabel.text = seconds / 60 + ":" + (seconds % 60).ToString("00");
            TimeLabel.color = seconds <= 15 ? new Color(.88f, .19f, .08f) : new Color(.22f, .08f, .03f);
            Sales.text = sales.ToString("N0") + " / " + target.ToString("N0");
            Progress.fillAmount = target > 0 ? Mathf.Clamp01((float)sales / target) : 0;
            Progress.color = sales >= target ? new Color(.32f, .74f, .2f) : new Color(1f, .66f, .12f);
            for (int i = 0; i < Hearts.Length; i++) Hearts[i].color = i < lives ? Color.white : new Color(.4f, .3f, .25f, .35f);
        }
    }
}
