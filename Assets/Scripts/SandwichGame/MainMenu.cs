using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SandwichGame
{
    public class MainMenu : MonoBehaviour
    {
        public GameObject MainPanel;
        public GameObject SettingsPanel;
        public GameObject HowToPlayPanel;
        public Slider VolumeSlider;
        [SerializeField, Range(0f, 1f)] float musicVolume = .15f;
        AudioSource musicSource;
        public void Play()
        {
            SceneManager.LoadScene("SampleScene");
        }
        void Start()
        {
            AudioSettings.Initialize();
            var music = new GameObject("Menu Background Music");
            music.transform.SetParent(transform, false);
            musicSource = music.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.clip = Resources.Load<AudioClip>("Audio/BackgroundMusic");
            musicSource.volume = musicVolume * AudioSettings.Music;
            if (musicSource.clip != null) musicSource.Play();
            if (VolumeSlider != null) VolumeSlider.SetValueWithoutNotify(AudioListener.volume);
            ShowMain();
        }
        void Update()
        {
            if (musicSource != null) musicSource.volume = musicVolume * AudioSettings.Music;
        }
        public void ShowMain() { MainPanel.SetActive(true); SettingsPanel.SetActive(false); HowToPlayPanel.SetActive(false); }
        public void ShowSettings() { MainPanel.SetActive(false); SettingsPanel.SetActive(true); HowToPlayPanel.SetActive(false); }
        public void ShowHowToPlay() { MainPanel.SetActive(false); SettingsPanel.SetActive(false); HowToPlayPanel.SetActive(true); }
        public void SetVolume(float volume) { AudioListener.volume = volume; PlayerPrefs.SetFloat("MasterVolume", volume); PlayerPrefs.Save(); }
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
