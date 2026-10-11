using UnityEngine;

namespace SandwichGame
{
    public static class AudioSettings
    {
        public static void Initialize()
        {
            float previousMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
            foreach (string key in new[] { "MusicVolume", "AmbienceVolume", "EffectsVolume" })
                if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetFloat(key, previousMaster);
            AudioListener.volume = 1f;
            PlayerPrefs.Save();
        }
        public static float Music => PlayerPrefs.GetFloat("MusicVolume", 1f);
        public static float Ambience => PlayerPrefs.GetFloat("AmbienceVolume", 1f);
        public static float Effects => PlayerPrefs.GetFloat("EffectsVolume", 1f);
        public static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}
