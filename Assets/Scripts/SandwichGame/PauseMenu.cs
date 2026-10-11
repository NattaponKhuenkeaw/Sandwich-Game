using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SandwichGame
{
    [DefaultExecutionOrder(-200)]
    public class PauseMenu : MonoBehaviour
    {
        public GameObject Panel;
        public Button ResumeButton, MenuButton;
        public bool IsPaused { get; private set; }
        float previousTimeScale;
        SandwichGameController controller;
        void Awake()
        {
            controller = GetComponentInParent<SandwichGameController>();
            ResumeButton.onClick.AddListener(Resume);
            MenuButton.onClick.AddListener(() => { Resume(); SceneManager.LoadScene("Menu"); });
            if (Panel != null) Panel.SetActive(false);
        }
        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (IsPaused) Resume();
            else if (controller != null && controller.CanPause) Pause();
        }
        public void Pause()
        {
            if (IsPaused) return;
            previousTimeScale = Time.timeScale;
            IsPaused = true;
            Time.timeScale = 0f;
            Panel.SetActive(true);
            SetAudioPaused(true);
            ResumeButton.Select();
        }
        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = previousTimeScale;
            if (Panel != null) Panel.SetActive(false);
            SetAudioPaused(false);
        }
        void SetAudioPaused(bool paused)
        {
            var audio = controller != null ? controller.GetComponentInChildren<GameAudio>(true) : null;
            if (audio != null) audio.SetPaused(paused);
        }
        void OnDisable() { Resume(); }
    }
}
