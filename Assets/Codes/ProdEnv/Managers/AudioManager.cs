using UnityEngine;
using System;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;

public static class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    private static void FindMissingScripts()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            // Only check objects that are part of a scene (ignore prefabs in the Project)
            if (!go.scene.IsValid())
                continue;

            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null)
                {
                    Debug.LogWarning(
                        $"Missing script on GameObject '{go.name}' in scene '{go.scene.name}'",
                        go
                    );
                }
            }
        }
    }
}
#endif

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public event Action<float> MusicVolumeChanged;
    public event Action<float> SfxVolumeChanged;

    [Header("Audio Sources")]
    [FormerlySerializedAs("bgmSource")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Default UI Sounds")]
    [SerializeField] private AudioClip defaultButtonClickSfx;
    [SerializeField] private AudioClip defaultButtonHoverSfx;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [FormerlySerializedAs("bgmVolume")]
    [SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string LegacyBgmVolumeKey = "BgmVolume";
    private const string SfxVolumeKey = "SfxVolume";

    private bool isInitialized;

    public AudioSource MusicSource => musicSource;
    public AudioSource SfxSource => sfxSource;
    public AudioClip CurrentMusicClip => musicSource != null ? musicSource.clip : null;

    public AudioSource bgmSource => musicSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        ApplyVolumeSettings();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        EnsureAudioSources();
        ApplyVolumeSettings();
    }

    public void Initialize()
    {
        EnsureAudioSources();

        if (isInitialized)
        {
            ApplyVolumeSettings();
            return;
        }

        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
        musicVolume = PlayerPrefs.HasKey(MusicVolumeKey)
            ? PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume)
            : PlayerPrefs.GetFloat(LegacyBgmVolumeKey, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);

        ApplyVolumeSettings();

        isInitialized = true;
        Debug.Log("AudioManager Initialized");
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
            musicSource = GetOrCreateAudioSource("Music Source", true, 0);

        if (sfxSource == null)
            sfxSource = GetOrCreateAudioSource("SFX Source", false, 1);
    }

    private AudioSource GetOrCreateAudioSource(string childName, bool loop, int priority)
    {
        Transform child = transform.Find(childName);
        AudioSource source = child != null ? child.GetComponent<AudioSource>() : null;

        if (source == null)
        {
            GameObject childObject = child != null ? child.gameObject : new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            source = childObject.GetComponent<AudioSource>();
            if (source == null)
                source = childObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.priority = priority == 0 ? 64 : 128;
        source.ignoreListenerPause = false;
        return source;
    }

    private void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Play a sound effect
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        PlaySfx(clip);
    }

    /// <summary>
    /// Play a sound effect with custom volume
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume)
    {
        PlaySfx(clip, volume);
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        EnsureAudioSources();

        float outputVolume = Mathf.Clamp01(masterVolume * sfxVolume) * Mathf.Clamp01(volumeScale);

        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, outputVolume);
            return;
        }

        AudioSource.PlayClipAtPoint(
            clip,
            Camera.main != null ? Camera.main.transform.position : Vector3.zero,
            outputVolume);
    }

    public void PlayUiClick(AudioClip clip = null, float volumeScale = 1f)
    {
        PlaySfx(clip != null ? clip : defaultButtonClickSfx, volumeScale);
    }

    public void PlayUiHover(AudioClip clip = null, float volumeScale = 1f)
    {
        PlaySfx(clip != null ? clip : defaultButtonHoverSfx, volumeScale);
    }

    /// <summary>
    /// Set overall master volume (0-1). Affects both BGM and SFX.
    /// </summary>
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        SaveVolume(MasterVolumeKey, masterVolume);
        ApplyVolumeSettings();
    }

    /// <summary>
    /// Set background music volume (0-1), multiplied by master volume.
    /// </summary>
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        SaveVolume(MusicVolumeKey, musicVolume);
        SaveVolume(LegacyBgmVolumeKey, musicVolume);
        ApplyVolumeSettings();
        MusicVolumeChanged?.Invoke(musicVolume);
    }

    /// <summary>
    /// Set sound effects volume (0-1), multiplied by master volume.
    /// </summary>
    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        SaveVolume(SfxVolumeKey, sfxVolume);
        ApplyVolumeSettings();
        SfxVolumeChanged?.Invoke(sfxVolume);
    }

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSfxVolume() => sfxVolume;
    public float GetBgmVolume() => musicVolume;

    public void SetBgmVolume(float value)
    {
        SetMusicVolume(value);
    }

    /// <summary>
    /// Apply current volume settings to underlying AudioSources.
    /// </summary>
    private void ApplyVolumeSettings()
    {
        EnsureAudioSources();

        float masterMusic = Mathf.Clamp01(masterVolume * musicVolume);
        float masterSfx = Mathf.Clamp01(masterVolume * sfxVolume);

        if (musicSource != null)
        {
            musicSource.volume = masterMusic;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = masterSfx;
        }
    }

    /// <summary>
    /// Set the background music clip at runtime and optionally start playing it.
    /// </summary>
    /// <param name="clip">Audio clip to use for BGM.</param>
    /// <param name="playImmediately">If true, starts playback when a non-null clip is assigned.</param>
    /// <param name="loop">If true, the BGM source will loop.</param>
    public void SetBgmClip(AudioClip clip, bool playImmediately = true, bool loop = true)
    {
        EnsureAudioSources();

        if (musicSource == null)
            return;

        bool shouldRestart = musicSource.clip != clip;

        musicSource.loop = loop;
        musicSource.clip = clip;

        ApplyVolumeSettings();

        if (playImmediately && clip != null)
        {
            if (shouldRestart || !musicSource.isPlaying)
                musicSource.Play();
        }
        else if (clip == null)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Convenience helper to play a given BGM clip with looping enabled.
    /// </summary>
    public void PlayBgm(AudioClip clip)
    {
        PlayMusic(clip);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        SetBgmClip(clip, true, loop);
    }

    /// <summary>
    /// Plays the requested BGM clip only when it differs from the one already assigned.
    /// Useful for scene entry points that should not restart identical looping music.
    /// </summary>
    public void PlayBgmIfDifferent(AudioClip clip)
    {
        PlayMusicIfDifferent(clip, true);
    }

    public void PlayMusicIfDifferent(AudioClip clip, bool loop = true)
    {
        EnsureAudioSources();

        if (musicSource == null || clip == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            ApplyVolumeSettings();
            return;
        }

        SetBgmClip(clip, true, loop);
    }

    public void StopMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
    }

    /// <summary>
    /// Restart playback of the currently assigned BGM clip, if any.
    /// </summary>
    public void RestartCurrentBgm()
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        ApplyVolumeSettings();
        musicSource.Play();
    }
}
