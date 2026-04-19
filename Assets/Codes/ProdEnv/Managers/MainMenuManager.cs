using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;

    [Header("Onboarding")]
    [SerializeField] private OnboardingManager onboardingManager;

    [Header("Reset Confirmation")]
    [SerializeField] private GameObject resetConfirmPanel;
    [SerializeField] private Button confirmResetButton;
    [SerializeField] private Button cancelResetButton;

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GlobalAudioSettingsUI settingsAudioUI;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button closeSettingsButton;

    [Header("About")] 
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private Button closeAboutButton;
    
    [Header("Tutorial")]
    [SerializeField] private OuntroPanelController tutorialPanelController;
    [SerializeField] private Button tutorialButton;

    [Header("Audio")]
    [SerializeField] private AudioClip mainMenuBgmClip;
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip menuOpenSfx;

    private void Start()
    {
        AppSceneLoader.EnsureExists();
        Debug.Log("MainMenuManager: Ready");
        
        // Show main menu panel
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        // Hide loading panel at start
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Auto-find OnboardingManager if not assigned
        if (onboardingManager == null)
        {
            onboardingManager = FindObjectOfType<OnboardingManager>();
            if (onboardingManager != null)
                Debug.Log("MainMenuManager: Found OnboardingManager automatically");
        }

        // Wire reset confirmation UI
        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);
        if (confirmResetButton != null)
            confirmResetButton.onClick.AddListener(ConfirmResetProgress);
        if (cancelResetButton != null)
            cancelResetButton.onClick.AddListener(CancelResetProgress);

        // Settings panel starts hidden
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        RefreshAudioSliders();

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        // About panel starts hidden
        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (closeAboutButton != null)
        {
            closeAboutButton.onClick.AddListener(CloseAbout);
        }
        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(OpenTutorial);
        }

        PlayMenuAudio();
    }

    // Called when "Play" button is clicked
    public void PlayGame()
    {
        PlayButtonClick();
        Debug.Log("MainMenuManager: PlayGame clicked!");

        // If first-time player, start onboarding instead of jumping to gameplay
        if (PlayerData.Instance != null && PlayerData.Instance.IsFirstTimePlaying())
        {
            if (onboardingManager != null)
            {
                onboardingManager.BeginOnboardingFlow();
                return;
            }
            Debug.LogWarning("MainMenuManager: Player is new but OnboardingManager is missing; proceeding to main game.");
        }

        // Continue game (load last mission or main mission scene)
        string sceneName = "MissionManager";
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            LoadSceneAsync(sceneName);
        }
        else
        {
            Debug.LogError($"MainMenuManager: Scene '{sceneName}' not found in Build Settings!");
        }
    }

    // Opens the mission manager scene to start the game.
    public void OpenMissionManager()
    {
        PlayButtonClick();
        const string sceneName = "MissionManager";

        // Check if the scene is loadable first (guard against unguarded loader calls)
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"MainMenuManager: Scene '{sceneName}' not found in Build Settings!");
            return;
        }

        // Scene is loadable; attempt to load via AppSceneLoader if available, otherwise use SceneManager
        AppSceneLoader.EnsureExists();
        if (AppSceneLoader.Instance != null)
        {
            AppSceneLoader.Instance.LoadSceneSingle(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void LoadSceneAsync(string sceneName)
    {
        // Show loading panel
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        AppSceneLoader.EnsureExists();
        AppSceneLoader.Instance.LoadSceneSingleAsync(
            sceneName,
            progress =>
            {
                if (progressBar != null)
                    progressBar.value = progress;

                if (progressText != null)
                    progressText.text = $"{(progress * 100f):0}%";
            },
            null,
            0.5f);
    }

    // Called when "Settings" button is clicked
    public void OpenSettings()
    {
        PlayMenuOpenSound();
        Debug.Log("MainMenuManager: Settings button clicked");

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            RefreshAudioSliders();
        }
    }

    private void CloseSettings()
    {
        PlayButtonClick();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Called when "Quit" button is clicked
    public void QuitGame()
    {
        PlayButtonClick();
        Debug.Log("Quit button clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in editor
#else
        Application.Quit(); // Quit the build
#endif
    }
    // Called when "Reset" button is clicked - shows confirmation if available
    public void ResetProgress()
    {
        PlayButtonClick();
        Debug.Log("MainMenuManager: ResetProgress clicked");
        if (resetConfirmPanel != null)
        {
            resetConfirmPanel.SetActive(true);
            return;
        }

        ConfirmResetProgress();
    }

    private void ConfirmResetProgress()
    {
        PlayButtonClick();
        PlayerData.Instance?.ResetAllData();
        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);

        if (onboardingManager != null)
        {
            // Just reset UI; onboarding will start next time Play is pressed
            onboardingManager.ResetOnboardingUI();
        }
        else
        {
            // Fallback: reload the current scene to ensure UI resets
            AppSceneLoader.EnsureExists();
            AppSceneLoader.Instance.LoadSceneSingle(SceneManager.GetActiveScene().name);
        }
    }

    private void CancelResetProgress()
    {
        PlayButtonClick();
        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(value);
        }
    }

    private void RefreshAudioSliders()
    {
        if (AudioManager.Instance == null)
            return;

        if (settingsAudioUI != null)
        {
            settingsAudioUI.Bind(AudioManager.Instance);
            return;
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetSfxVolume());
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
    }

    private void PlayMenuAudio()
    {
        if (AudioManager.Instance == null)
            return;

        if (mainMenuBgmClip != null)
            AudioManager.Instance.PlayMusicIfDifferent(mainMenuBgmClip);

        if (menuOpenSfx != null)
            AudioManager.Instance.PlaySfx(menuOpenSfx);
    }

    private void PlayButtonClick()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayUiClick(buttonClickSfx);
    }

    private void PlayMenuOpenSound()
    {
        if (AudioManager.Instance == null)
            return;

        if (menuOpenSfx != null)
            AudioManager.Instance.PlaySfx(menuOpenSfx);
        else
            PlayButtonClick();
    }

    // Called when "About" button is clicked
    public void OpenAbout()
    {
        PlayMenuOpenSound();
        Debug.Log("MainMenuManager: About button clicked");

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);

        if (aboutPanel != null)
            aboutPanel.SetActive(true);
    }

    private void CloseAbout()
    {
        PlayButtonClick();
        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        PlayButtonClick();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void OpenTutorial()
    {
        PlayMenuOpenSound();
        Debug.Log("MainMenuManager: Tutorial button clicked");

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);

        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (tutorialPanelController != null)
        {
            tutorialPanelController.StartSequence(OpenMissionManager);
            return;
        }

        Debug.LogWarning("MainMenuManager: Tutorial panel controller not assigned.");
    }
}
