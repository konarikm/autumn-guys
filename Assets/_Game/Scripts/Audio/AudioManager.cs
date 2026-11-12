using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace AutumnGuys
{
    /// <summary>Every sound effect in the game. Clips live in Resources/Audio/Sfx/&lt;Name&gt;/ (any number of variants).</summary>
    public enum Sfx
    {
        Click,
        Countdown, Go,
        Land, Bounce, Hit,
        Fall, Checkpoint, Finish, NewRecord, GameOver,
        TileShake, TileFall,
        AcornImpact, Wind, Pickup,
        Reveal, Tick, Safe,
    }

    /// <summary>
    /// Persistent audio player. Music: Resources/Audio/Music/&lt;SceneName&gt; (crossfades on scene change, keeps playing
    /// when the same track is requested again). Sound effects: <see cref="Play"/>.
    /// Missing clips are simply silent, so audio can be added gradually.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const string MusicVolumeKey = "AutumnGuys_MusicVolume";
        const string SfxVolumeKey = "AutumnGuys_SfxVolume";
        const int SfxVoices = 12;
        const float FadeTime = 1f;
        const float PausedMusicFactor = 0.4f;
        const float SameSfxCooldown = 0.05f;

        /// <summary>Scenes that share another scene's track (scene name → track name).</summary>
        static readonly Dictionary<string, string> MusicAlias = new Dictionary<string, string>
        {
            { SceneNames.Splash, SceneNames.MainMenu },
        };

        /// <summary>Per-track loudness trim (track name → multiplier on the music volume) to even out mastering.</summary>
        static readonly Dictionary<string, float> MusicTrim = new Dictionary<string, float>
        {
            { SceneNames.LeafPile, 0.35f },
        };

        static AudioManager instance;

        AudioSource[] music;   // two sources for crossfading
        int activeMusic;
        readonly float[] musicTrim = { 1f, 1f };
        float fade = 1f;       // 0→1 progress of the current crossfade
        AudioSource[] voices;
        int nextVoice;
        readonly Dictionary<Sfx, AudioClip[]> clips = new Dictionary<Sfx, AudioClip[]>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.55f);
            set { PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
            set { PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            music = new AudioSource[2];
            for (int i = 0; i < music.Length; i++)
            {
                music[i] = gameObject.AddComponent<AudioSource>();
                music[i].loop = true;
                music[i].playOnAwake = false;
                music[i].volume = 0f;
            }
            voices = new AudioSource[SfxVoices];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            string track = MusicAlias.TryGetValue(scene.name, out var alias) ? alias : scene.name;
            float trim = MusicTrim.TryGetValue(track, out var tr) ? tr : 1f;
            PlayMusic(Resources.Load<AudioClip>("Audio/Music/" + track), trim);
        }

        void PlayMusic(AudioClip clip, float trim)
        {
            var current = music[activeMusic];
            if (clip != null && current.clip == clip && current.isPlaying) return;

            activeMusic = 1 - activeMusic;
            musicTrim[activeMusic] = trim;
            var next = music[activeMusic];
            next.clip = clip;
            next.volume = 0f;
            if (clip != null) next.Play(); else next.Stop();
            fade = 0f;
        }

        void Update()
        {
            // Crossfade (unscaled, so it also runs while the game is paused).
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / FadeTime);
            float target = MusicVolume * (Time.timeScale == 0f ? PausedMusicFactor : 1f);
            int oldIndex = 1 - activeMusic;
            var active = music[activeMusic];
            var old = music[oldIndex];
            active.volume = Mathf.MoveTowards(active.volume, target * musicTrim[activeMusic] * fade, Time.unscaledDeltaTime * 2f);
            old.volume = Mathf.Min(old.volume, target * musicTrim[oldIndex] * (1f - fade));
            if (fade >= 1f && old.isPlaying) old.Stop();
        }

        /// <summary>Plays a sound effect (random variant, slight pitch variation).</summary>
        public static void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (instance == null) return;
            instance.PlayInternal(sfx, volume, pitchJitter);
        }

        void PlayInternal(Sfx sfx, float volume, float pitchJitter)
        {
            // Avoid stacking the same sound many times in one frame (e.g. a whole row of tiles dropping).
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(sfx, out float last) && now - last < SameSfxCooldown) return;

            var variants = GetClips(sfx);
            if (variants.Length == 0) return;
            lastPlayed[sfx] = now;

            var source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            source.clip = variants[Random.Range(0, variants.Length)];
            source.volume = Mathf.Clamp01(volume) * SfxVolume;
            source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            source.Play();
        }

        AudioClip[] GetClips(Sfx sfx)
        {
            if (!clips.TryGetValue(sfx, out var result))
            {
                result = Resources.LoadAll<AudioClip>("Audio/Sfx/" + sfx);
                Array.Sort(result, (a, b) => string.CompareOrdinal(a.name, b.name));
                clips[sfx] = result;
            }
            return result;
        }
    }
}
