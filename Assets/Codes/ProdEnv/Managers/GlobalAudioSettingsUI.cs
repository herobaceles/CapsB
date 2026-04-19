using UnityEngine;
using UnityEngine.UI;

public class GlobalAudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Legacy")]
    [SerializeField] private Slider legacyMasterVolumeSlider;
    [SerializeField] private bool hideLegacyMasterSlider = true;

    private AudioManager boundAudioManager;

    private void OnEnable()
    {
        Bind(AudioManager.Instance);
    }

    private void OnDisable()
    {
        Unbind();
    }

    public void Bind(AudioManager audioManager = null)
    {
        if (boundAudioManager == audioManager && boundAudioManager != null)
        {
            Refresh();
            return;
        }

        Unbind();

        boundAudioManager = audioManager != null ? audioManager : AudioManager.Instance;
        if (boundAudioManager == null)
            return;

        boundAudioManager.MusicVolumeChanged += HandleMusicVolumeChanged;
        boundAudioManager.SfxVolumeChanged += HandleSfxVolumeChanged;
        Refresh();
    }

    public void Refresh()
    {
        if (hideLegacyMasterSlider && legacyMasterVolumeSlider != null)
            legacyMasterVolumeSlider.gameObject.SetActive(false);

        if (boundAudioManager == null)
            boundAudioManager = AudioManager.Instance;

        if (boundAudioManager == null)
            return;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.SetValueWithoutNotify(boundAudioManager.GetMusicVolume());
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.SetValueWithoutNotify(boundAudioManager.GetSfxVolume());
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
    }

    private void Unbind()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

        if (boundAudioManager == null)
            return;

        boundAudioManager.MusicVolumeChanged -= HandleMusicVolumeChanged;
        boundAudioManager.SfxVolumeChanged -= HandleSfxVolumeChanged;
        boundAudioManager = null;
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (boundAudioManager == null)
            return;

        boundAudioManager.SetMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (boundAudioManager == null)
            return;

        boundAudioManager.SetSfxVolume(value);
    }

    private void HandleMusicVolumeChanged(float value)
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(value);
    }
}