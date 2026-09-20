using UnityEngine;

namespace SandwichGame
{
    public class GameAudio : MonoBehaviour
    {
        AudioSource source;
        AudioClip pickup;
        AudioClip drop;
        AudioClip ding;
        AudioClip burn;
        AudioClip serve;
        AudioClip fail;
        AudioClip roundWin;

        public static GameAudio Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            return go.AddComponent<GameAudio>();
        }

        void Awake()
        {
            if (source == null) Build();
        }

        void Build()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            pickup = Tone(520f, 0.07f, 0.18f);
            drop = Tone(220f, 0.09f, 0.16f);
            ding = Chord(880f, 1174f, 0.16f, 0.14f);
            burn = Tone(90f, 0.22f, 0.22f);
            serve = Chord(523f, 784f, 0.22f, 0.16f);
            fail = Tone(160f, 0.28f, 0.2f);
            roundWin = Chord(659f, 988f, 0.32f, 0.16f);
        }

        public void Pickup() => Play(pickup);
        public void Drop() => Play(drop);
        public void Ding() => Play(ding);
        public void Burn() => Play(burn);
        public void Serve() => Play(serve);
        public void Fail() => Play(fail);
        public void RoundWin() => Play(roundWin);

        void Play(AudioClip clip)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip);
        }

        static AudioClip Tone(float freq, float duration, float volume)
        {
            int sampleRate = 44100;
            int count = Mathf.Max(1, (int)(sampleRate * duration));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Clamp01(1f - t / duration);
                env *= Mathf.Clamp01(t / 0.01f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * env;
            }

            var clip = AudioClip.Create("tone", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Chord(float a, float b, float duration, float volume)
        {
            int sampleRate = 44100;
            int count = Mathf.Max(1, (int)(sampleRate * duration));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Clamp01(1f - t / duration);
                env *= Mathf.Clamp01(t / 0.012f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * a * t) + Mathf.Sin(2f * Mathf.PI * b * t)) * 0.5f * volume * env;
            }

            var clip = AudioClip.Create("chord", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
