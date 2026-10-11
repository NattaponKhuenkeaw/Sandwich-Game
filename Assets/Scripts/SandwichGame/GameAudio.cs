using UnityEngine;

namespace SandwichGame
{
    public class GameAudio : MonoBehaviour
    {
        AudioSource source;
        AudioSource musicSource;
        AudioSource ambienceSource;
        [SerializeField] bool restaurantAmbienceEnabled = false;
        AudioSource cityAmbienceSource;
        [SerializeField] AudioClip cityAmbience;
        [SerializeField] AudioClip restaurantAmbience;
        [SerializeField, Range(0f, 1f)] float ambienceVolume = .15f;
        [SerializeField, Range(0f, 10f)] float restaurantGain = 1.75f;
        [SerializeField, Range(0f, 10f)] float cityGain = 4.666667f;
        [SerializeField] AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] float musicVolume = .15f;
        AudioSource fryingSource;
        AudioSource cookedFryingSource;
        AudioSource burnedFryingSource;
        [SerializeField] AudioClip burnedMeatFrying;
        [SerializeField] AudioClip cookedMeatFrying;
        [SerializeField] AudioClip rawMeatFrying;
        [SerializeField, Range(0f, 1f)] float fryingVolume = .225f;
        [SerializeField] AudioClip customerTimeLow01;
        [SerializeField] AudioClip customerTimeLow02;
        [SerializeField] AudioClip customerReceivedFood01;
        [SerializeField] AudioClip customerReceivedFood02;
        [SerializeField] AudioClip customerNoFood01;
        [SerializeField] AudioClip customerNoFood02;
        [SerializeField, Range(0f, 1f)] float customerVoiceVolume = 0.7f;
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
            AudioSettings.Initialize();
            if (source == null) Build();
        }

        void Build()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = AudioSettings.Effects;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            musicSource.volume = musicVolume * AudioSettings.Music;
            if (backgroundMusic == null) backgroundMusic = Resources.Load<AudioClip>("Audio/BackgroundMusic");
            musicSource.clip = backgroundMusic;
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.loop = true;
            ambienceSource.volume = restaurantAmbienceEnabled ? Mathf.Clamp01(ambienceVolume * restaurantGain) * AudioSettings.Ambience : 0f;
            if (restaurantAmbience == null) restaurantAmbience = Resources.Load<AudioClip>("Audio/RestaurantAmbience");
            ambienceSource.clip = restaurantAmbience;
            cityAmbienceSource = gameObject.AddComponent<AudioSource>();
            cityAmbienceSource.playOnAwake = false;
            cityAmbienceSource.spatialBlend = 0f;
            cityAmbienceSource.loop = true;
            cityAmbienceSource.volume = Mathf.Clamp01(ambienceVolume * cityGain) * AudioSettings.Ambience;
            if (cityAmbience == null) cityAmbience = Resources.Load<AudioClip>("Audio/CityAmbience");
            cityAmbienceSource.clip = cityAmbience;
            fryingSource = gameObject.AddComponent<AudioSource>();
            fryingSource.playOnAwake = false;
            fryingSource.spatialBlend = 0f;
            fryingSource.loop = true;
            fryingSource.volume = fryingVolume * AudioSettings.Effects;
            if (rawMeatFrying == null) rawMeatFrying = Resources.Load<AudioClip>("Audio/RawMeatFrying");
            fryingSource.clip = rawMeatFrying;
            cookedFryingSource = gameObject.AddComponent<AudioSource>();
            cookedFryingSource.playOnAwake = false;
            cookedFryingSource.spatialBlend = 0f;
            cookedFryingSource.loop = true;
            cookedFryingSource.volume = fryingVolume * AudioSettings.Effects;
            if (cookedMeatFrying == null) cookedMeatFrying = Resources.Load<AudioClip>("Audio/CookedMeatFrying");
            cookedFryingSource.clip = cookedMeatFrying;
            burnedFryingSource = gameObject.AddComponent<AudioSource>();
            burnedFryingSource.playOnAwake = false;
            burnedFryingSource.spatialBlend = 0f;
            burnedFryingSource.loop = true;
            burnedFryingSource.volume = fryingVolume * .9f * AudioSettings.Effects;
            if (burnedMeatFrying == null) burnedMeatFrying = Resources.Load<AudioClip>("Audio/BurnedMeatFrying");
            burnedFryingSource.clip = burnedMeatFrying;
            if (customerTimeLow01 == null) customerTimeLow01 = Resources.Load<AudioClip>("Audio/CustomerTimeLow01");
            if (customerTimeLow02 == null) customerTimeLow02 = Resources.Load<AudioClip>("Audio/CustomerTimeLow02");
            if (customerReceivedFood01 == null) customerReceivedFood01 = Resources.Load<AudioClip>("Audio/CustomerReceivedFood01");
            if (customerReceivedFood02 == null) customerReceivedFood02 = Resources.Load<AudioClip>("Audio/CustomerReceivedFood02");
            if (customerNoFood01 == null) customerNoFood01 = Resources.Load<AudioClip>("Audio/CustomerNoFood01");
            if (customerNoFood02 == null) customerNoFood02 = Resources.Load<AudioClip>("Audio/CustomerNoFood02");
            pickup = Tone(520f, 0.07f, 0.18f);
            drop = Tone(220f, 0.09f, 0.16f);
            ding = Chord(880f, 1174f, 0.16f, 0.14f);
            burn = Tone(90f, 0.22f, 0.22f);
            serve = Chord(523f, 784f, 0.22f, 0.16f);
            fail = Tone(160f, 0.28f, 0.2f);
            roundWin = Chord(659f, 988f, 0.32f, 0.16f);
        }

        public void SetRawMeatFrying(bool frying)
        {
            if (fryingSource == null) return;
            fryingSource.volume = fryingVolume * AudioSettings.Effects;
            if (frying && fryingSource.clip != null)
            {
                if (!fryingSource.isPlaying) fryingSource.Play();
            }
            else if (fryingSource.isPlaying) fryingSource.Stop();
        }

        public void SetCookedMeatFrying(bool frying)
        {
            if (cookedFryingSource == null) return;
            cookedFryingSource.volume = fryingVolume * AudioSettings.Effects;
            if (frying && cookedFryingSource.clip != null)
            {
                if (!cookedFryingSource.isPlaying) cookedFryingSource.Play();
            }
            else if (cookedFryingSource.isPlaying) cookedFryingSource.Stop();
        }

        public void SetBurnedMeatFrying(bool frying)
        {
            if (burnedFryingSource == null) return;
            burnedFryingSource.volume = fryingVolume * .9f * AudioSettings.Effects;
            if (frying && burnedFryingSource.clip != null)
            {
                if (!burnedFryingSource.isPlaying) burnedFryingSource.Play();
            }
            else if (burnedFryingSource.isPlaying) burnedFryingSource.Stop();
        }

        void OnEnable()
        {
            if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
            if (restaurantAmbienceEnabled && ambienceSource != null && ambienceSource.clip != null && !ambienceSource.isPlaying)
                ambienceSource.Play();
            if (cityAmbienceSource != null && cityAmbienceSource.clip != null && !cityAmbienceSource.isPlaying)
                cityAmbienceSource.Play();
        }

        void Update()
        {
            if (source != null) source.volume = AudioSettings.Effects;
            if (fryingSource != null) fryingSource.volume = fryingVolume * AudioSettings.Effects;
            if (cookedFryingSource != null) cookedFryingSource.volume = fryingVolume * AudioSettings.Effects;
            if (burnedFryingSource != null) burnedFryingSource.volume = fryingVolume * .9f * AudioSettings.Effects;
            if (fryingSource != null) fryingSource.volume = fryingVolume * AudioSettings.Effects;
            if (cookedFryingSource != null) cookedFryingSource.volume = fryingVolume * AudioSettings.Effects;
            if (burnedFryingSource != null) burnedFryingSource.volume = fryingVolume * .9f * AudioSettings.Effects;
            if (musicSource != null) musicSource.volume = musicVolume * AudioSettings.Music;
            if (ambienceSource != null) ambienceSource.volume = restaurantAmbienceEnabled ? Mathf.Clamp01(ambienceVolume * restaurantGain) * AudioSettings.Ambience : 0f;
            if (!restaurantAmbienceEnabled && ambienceSource != null && ambienceSource.isPlaying) ambienceSource.Stop();
            if (cityAmbienceSource != null) cityAmbienceSource.volume = Mathf.Clamp01(ambienceVolume * cityGain) * AudioSettings.Ambience;
        }

        void OnDisable()
        {
            if (musicSource != null) musicSource.Stop();
            if (ambienceSource != null) ambienceSource.Stop();
            if (cityAmbienceSource != null) cityAmbienceSource.Stop();
            SetRawMeatFrying(false);
            SetCookedMeatFrying(false);
            SetBurnedMeatFrying(false);
        }

        public void Pickup() => Play(pickup);
        public void SetPaused(bool paused)
        {
            foreach (var effectSource in new[] { source, fryingSource, cookedFryingSource, burnedFryingSource })
            {
                if (effectSource == null) continue;
                if (paused) effectSource.Pause();
                else effectSource.UnPause();
            }
        }
        public void Drop() => Play(drop);
        public void Ding() => Play(ding);
        public void Burn() => Play(burn);
        public void Serve()
        {
            if (source == null) return;
            var clip = Random.Range(0, 2) == 0 ? customerReceivedFood01 : customerReceivedFood02;
            if (clip == null) clip = customerReceivedFood01 != null ? customerReceivedFood01 : customerReceivedFood02;
            if (clip != null) source.PlayOneShot(clip, customerVoiceVolume);
            else Play(serve);
        }
        public void Fail() => Play(fail);
        public void CustomerTimeLow()
        {
            if (source == null) return;
            var clip = Random.Range(0, 2) == 0 ? customerTimeLow01 : customerTimeLow02;
            if (clip == null) clip = customerTimeLow01 != null ? customerTimeLow01 : customerTimeLow02;
            if (clip != null) source.PlayOneShot(clip, customerVoiceVolume);
        }

        public void CustomerNoFood()
        {
            if (source == null) return;
            var clip = Random.Range(0, 2) == 0 ? customerNoFood01 : customerNoFood02;
            if (clip == null) clip = customerNoFood01 != null ? customerNoFood01 : customerNoFood02;
            if (clip != null) source.PlayOneShot(clip, customerVoiceVolume);
            else Play(fail);
        }
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
