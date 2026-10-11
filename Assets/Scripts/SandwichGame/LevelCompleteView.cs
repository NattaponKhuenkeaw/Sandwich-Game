using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
namespace SandwichGame
{
    public class LevelCompleteView : MonoBehaviour
    {
        public Text DayLabel;
        public Text SalesValue;
        public Text TargetValue;
        public Text NextTargetValue;
        public Text ReadyLabel;
        public Button NextButton;
        public Button MenuButton;
        public void Show(int day, int sales, int target, int nextTarget, UnityAction onNext)
        {
            DayLabel.text = "DAY " + day;
            SalesValue.text = sales.ToString("N0");
            TargetValue.text = target.ToString("N0");
            NextTargetValue.text = nextTarget.ToString("N0");
            ReadyLabel.text = "READY FOR DAY " + (day + 1) + "?";
            NextButton.onClick.RemoveAllListeners();
            NextButton.onClick.AddListener(onNext);
            MenuButton.onClick.RemoveAllListeners();
            MenuButton.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
            gameObject.SetActive(true);
            NextButton.Select();
        }
        public void Hide() { gameObject.SetActive(false); }
    }
}
