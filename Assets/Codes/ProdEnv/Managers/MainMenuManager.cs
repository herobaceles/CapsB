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
    }

    // Called when "Play" button is clicked
    public void PlayGame()
    {
        Debug.Log("MainMenuManager: PlayGame clicked!");
        
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
}
