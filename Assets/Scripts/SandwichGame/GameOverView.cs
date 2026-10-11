using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SandwichGame
{
    public class GameOverView : MonoBehaviour
    {
        public Text DayLabel, SalesValue, TargetValue, ShortfallValue;
        public Button RetryButton, MenuButton;

        public void Show(int day, int sales, int target, UnityAction onRetry)
        {
            DayLabel.text = "DAY " + day;
            SalesValue.text = sales.ToString("N0");
            TargetValue.text = target.ToString("N0");
            ShortfallValue.text = Mathf.Max(0, target - sales).ToString("N0");
            RetryButton.onClick.RemoveAllListeners();
            RetryButton.onClick.AddListener(onRetry);
            MenuButton.onClick.RemoveAllListeners();
            MenuButton.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
            gameObject.SetActive(true);
            RetryButton.Select();
        }

        public void Hide() { gameObject.SetActive(false); }
    }
}
