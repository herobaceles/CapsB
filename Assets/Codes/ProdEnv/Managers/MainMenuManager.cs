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

    private void Start()
    {
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
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        else
        {
            Debug.LogError($"MainMenuManager: Scene '{sceneName}' not found in Build Settings!");
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Show loading panel
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // Start async loading
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Update progress while loading
        while (!asyncLoad.isDone)
        {
            // Progress goes from 0 to 0.9 while loading, then jumps to 1 when activated
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Update progress bar
            if (progressBar != null)
                progressBar.value = progress;

            // Update progress text
            if (progressText != null)
                progressText.text = $"{(progress * 100f):0}%";

            // When loading is complete (progress reaches 0.9), activate the scene
            if (asyncLoad.progress >= 0.9f)
            {
                // Optional: wait a moment so player sees 100%
                if (progressBar != null)
                    progressBar.value = 1f;
                if (progressText != null)
                    progressText.text = "100%";

                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // Called when "Settings" button is clicked
    public void OpenSettings()
    {
        // Here you can open a settings panel or scene
        // Example: activate a UI panel
        Debug.Log("Settings button clicked");
        // SettingsPanel.SetActive(true); // If you have a panel
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void CancelResetProgress()
    {
        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);
    }
}
