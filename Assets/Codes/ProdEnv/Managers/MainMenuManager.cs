using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    private const string DefaultResetWarningMessage = "This will erase all saved progress and cannot be undone.";

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
    [SerializeField] private TMP_Text resetWarningText;
    [SerializeField] [TextArea(2, 4)] private string resetWarningMessage = DefaultResetWarningMessage;

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button closeSettingsButton;

    [Header("About")] 
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private Button closeAboutButton;

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

        EnsureResetConfirmationModalExists();

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

        // About panel starts hidden
        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (closeAboutButton != null)
        {
            closeAboutButton.onClick.AddListener(CloseAbout);
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
        if (resetConfirmPanel == null)
        {
            Debug.LogWarning("MainMenuManager: Reset confirmation modal is unavailable; aborting reset to avoid accidental data loss.");
            return;
        }

        resetConfirmPanel.SetActive(true);
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

    private void EnsureResetConfirmationModalExists()
    {
        if (resetConfirmPanel != null)
        {
            ApplyResetWarningMessage();
            return;
        }

        Canvas rootCanvas = FindObjectOfType<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogWarning("MainMenuManager: No Canvas found for reset confirmation modal.");
            return;
        }

        CreateFallbackResetConfirmationModal(rootCanvas.transform);
        ApplyResetWarningMessage();
    }

    private void ApplyResetWarningMessage()
    {
        if (resetWarningText != null)
            resetWarningText.text = string.IsNullOrWhiteSpace(resetWarningMessage) ? DefaultResetWarningMessage : resetWarningMessage;
    }

    private void CreateFallbackResetConfirmationModal(Transform parent)
    {
        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
        if (fontAsset == null)
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        GameObject panelObject = new GameObject("ResetConfirmPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject dialogObject = new GameObject("Dialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogObject.transform.SetParent(panelObject.transform, false);

        RectTransform dialogRect = dialogObject.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.sizeDelta = new Vector2(640f, 320f);
        dialogRect.anchoredPosition = Vector2.zero;

        Image dialogImage = dialogObject.GetComponent<Image>();
        dialogImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        GameObject titleObject = CreateTextObject("Title", dialogObject.transform, fontAsset, 34, FontStyles.Bold);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(32f, -84f);
        titleRect.offsetMax = new Vector2(-32f, -24f);
        TMP_Text titleText = titleObject.GetComponent<TMP_Text>();
        titleText.text = "Reset Progress";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.18f, 0.12f, 0.12f, 1f);

        GameObject messageObject = CreateTextObject("Message", dialogObject.transform, fontAsset, 24, FontStyles.Normal);
        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0f, 0.5f);
        messageRect.anchorMax = new Vector2(1f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.offsetMin = new Vector2(40f, -54f);
        messageRect.offsetMax = new Vector2(-40f, 34f);
        resetWarningText = messageObject.GetComponent<TMP_Text>();
        resetWarningText.alignment = TextAlignmentOptions.Center;
        resetWarningText.enableWordWrapping = true;
        resetWarningText.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        confirmResetButton = CreateButton("ConfirmButton", dialogObject.transform, "Confirm", new Vector2(-110f, -118f), new Color(0.73f, 0.18f, 0.18f, 1f), fontAsset);
        cancelResetButton = CreateButton("CancelButton", dialogObject.transform, "Cancel", new Vector2(110f, -118f), new Color(0.32f, 0.32f, 0.32f, 1f), fontAsset);

        resetConfirmPanel = panelObject;
        resetConfirmPanel.SetActive(false);
    }

    private GameObject CreateTextObject(string objectName, Transform parent, TMP_FontAsset fontAsset, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = fontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.raycastTarget = false;

        return textObject;
    }

    private Button CreateButton(string objectName, Transform parent, string label, Vector2 anchoredPosition, Color backgroundColor, TMP_FontAsset fontAsset)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(180f, 56f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = backgroundColor;

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        buttonObject.GetComponent<Button>().colors = colors;

        GameObject labelObject = CreateTextObject("Label", buttonObject.transform, fontAsset, 24, FontStyles.Bold);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text labelText = labelObject.GetComponent<TMP_Text>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;

        return buttonObject.GetComponent<Button>();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }

    // Called when "About" button is clicked
    public void OpenAbout()
    {
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
        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }
}
