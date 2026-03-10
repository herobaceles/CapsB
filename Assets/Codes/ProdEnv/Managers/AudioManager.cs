using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private const string MasterVolumeKey = "MasterVolume";
    private const string BgmVolumeKey = "BgmVolume";
    private const string SfxVolumeKey = "SfxVolume";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        // Load saved volume settings
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

        ApplyVolumeSettings();

        if (bgmSource != null)
        {
            bgmSource.Play();
        }
        Debug.Log("AudioManager Initialized");
    }

    /// <summary>
    /// Play a sound effect
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    /// <summary>
    /// Play a sound effect with custom volume
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null) return;

        if (sfxSource != null)
        {
            // Final volume is master * sfx * per-call volume
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(masterVolume * sfxVolume) * volume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(
                clip,
                Camera.main != null ? Camera.main.transform.position : Vector3.zero,
                Mathf.Clamp01(masterVolume * sfxVolume) * volume);
        }
    }

    /// <summary>
    /// Set overall master volume (0-1). Affects both BGM and SFX.
    /// </summary>
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();
        ApplyVolumeSettings();
    }

    /// <summary>
    /// Set background music volume (0-1), multiplied by master volume.
    /// </summary>
    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.Save();
        ApplyVolumeSettings();
    }

    /// <summary>
    /// Set sound effects volume (0-1), multiplied by master volume.
    /// </summary>
    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
        ApplyVolumeSettings();
    }

    public float GetMasterVolume() => masterVolume;
    public float GetBgmVolume() => bgmVolume;
    public float GetSfxVolume() => sfxVolume;

    /// <summary>
    /// Apply current volume settings to underlying AudioSources.
    /// </summary>
    private void ApplyVolumeSettings()
    {
        float masterBgm = Mathf.Clamp01(masterVolume * bgmVolume);
        float masterSfx = Mathf.Clamp01(masterVolume * sfxVolume);

        if (bgmSource != null)
        {
            bgmSource.volume = masterBgm;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = masterSfx;
        }
    }
}
