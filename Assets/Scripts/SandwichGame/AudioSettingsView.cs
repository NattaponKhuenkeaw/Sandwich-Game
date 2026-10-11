using UnityEngine;
using UnityEngine.UI;

namespace SandwichGame
{
    public class AudioSettingsView : MonoBehaviour
    {
        public Slider Music, Ambience, Effects;
        public Text MusicValue, AmbienceValue, EffectsValue;
        void OnEnable()
        {
            Bind(Music, MusicValue, "MusicVolume", AudioSettings.Music);
            Bind(Ambience, AmbienceValue, "AmbienceVolume", AudioSettings.Ambience);
            Bind(Effects, EffectsValue, "EffectsVolume", AudioSettings.Effects);
        }
        static void Bind(Slider slider, Text label, string key, float value)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.SetValueWithoutNotify(value);
            label.text = Mathf.RoundToInt(value * 100) + "%";
            slider.onValueChanged.AddListener(v =>
            {
                AudioSettings.Set(key, v);
                label.text = Mathf.RoundToInt(v * 100) + "%";
            });
        }
    }
}
