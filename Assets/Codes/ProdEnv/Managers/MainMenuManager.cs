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
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button closeSettingsButton;

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

        // Wire settings UI
        if (masterVolumeSlider != null)
        {
            // Initialize slider from current audio settings if available
            if (AudioManager.Instance != null)
            {
                masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
            }

            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }
    }

    // Called when "Play" button is clicked
    public void PlayGame()
    {
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
        Debug.Log("MainMenuManager: Settings button clicked");

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);

            // Refresh slider from current volume in case it changed elsewhere
            if (masterVolumeSlider != null && AudioManager.Instance != null)
            {
                masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
            }
        }
    }

    private void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Called when "Quit" button is clicked
    public void QuitGame()
    {
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
        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }
}
