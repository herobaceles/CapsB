using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "BeforeScenario";
    [SerializeField] private string optionsSceneName = "";
    [SerializeField] private string duringSceneName = "DuringScenario";
    [SerializeField] private string afterSceneName = "AfterScenario";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject onboardingPanel;
    [Header("Missions Panel")]
    [SerializeField] private GameObject missionsPanel;
    [SerializeField] private Button missionsMenuButton;
    [SerializeField] private Button missionsBeforeButton;
    [SerializeField] private Button missionsDuringButton;
    [SerializeField] private Button missionsAfterButton;

    [Header("Onboarding UI")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;
    [SerializeField] private Button nextButton;

    [Header("Restart")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button newGameButton;

    [Header("Dialogue UI (hide when form shows)")]
    [SerializeField] private GameObject dialoguePanel; // assign your dialogue panel GameObject here

    [Header("Dialogue Lines")]
    [TextArea(2, 6)]
    [SerializeField] private string[] onboardingLines;

    [Header("Text Animation")]
    [SerializeField] private float typeSpeed = 0.03f;

    private int dialogueIndex = 0;

    // 0 = not selected, 1 = male, 2 = female
    private int selectedGender = 0;

    private Coroutine typeCoroutine;
    private bool isTyping = false;

    private const string HasCompletedOnboardingKey = "HAS_COMPLETED_ONBOARDING";
    private const string PlayerNameKey = "PLAYER_NAME";
    private const string PlayerGenderKey = "PLAYER_GENDER";
    // Save keys used by BeforeSceneManager (read-only here)
    private const string SaveSceneKey = "SAVE_SCENE";
    private const string SaveARMissionCompletedKey = "SAVE_AR_COMPLETED";
    private const string SaveCircuitBreakerCompleteKey = "SAVE_CB_COMPLETE";
    private const string SaveAppliancesCompleteKey = "SAVE_APPL_COMPLETE";
    private const string SaveEvacuationStartedKey = "SAVE_EVAC_STARTED";
    private const string SavePlayerPosXKey = "SAVE_PLAYER_POS_X";

    private void Awake()
    {
// #if UNITY_EDITOR
//         PlayerPrefs.DeleteKey(HasCompletedOnboardingKey);
//         PlayerPrefs.DeleteKey(PlayerNameKey);
//         PlayerPrefs.DeleteKey(PlayerGenderKey);
//         PlayerPrefs.Save();
// #endif

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (onboardingPanel != null) onboardingPanel.SetActive(false);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextPressed);
        }

        if (maleButton != null)
        {
            maleButton.onClick.RemoveAllListeners();
            maleButton.onClick.AddListener(() => SelectGender(1));
        }

        if (femaleButton != null)
        {
            femaleButton.onClick.RemoveAllListeners();
            femaleButton.onClick.AddListener(() => SelectGender(2));
        }

        // Note: Restart button removed; New Game uses RestartGame behaviour

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(PlayGame);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(RestartGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGameFromSave);
        }

        // Missions panel wiring
        if (missionsMenuButton != null)
        {
            missionsMenuButton.onClick.RemoveAllListeners();
            missionsMenuButton.onClick.AddListener(ToggleMissionsPanel);
        }

        if (missionsBeforeButton != null)
        {
            missionsBeforeButton.onClick.RemoveAllListeners();
            missionsBeforeButton.onClick.AddListener(() => LoadMissionScene(gameSceneName));
        }

        if (missionsDuringButton != null)
        {
            missionsDuringButton.onClick.RemoveAllListeners();
            missionsDuringButton.onClick.AddListener(() => LoadMissionScene(duringSceneName));
        }

        if (missionsAfterButton != null)
        {
            missionsAfterButton.onClick.RemoveAllListeners();
            missionsAfterButton.onClick.AddListener(() => LoadMissionScene(afterSceneName));
        }

        // Ensure missions panel hidden by default
        if (missionsPanel != null) missionsPanel.SetActive(false);

        // Update mission button interactability based on saved progress
        UpdateMissionButtonsFromSave();

        // Update main menu (Start / Resume / New Game) visibility
        UpdateMainMenuButtons();

        // Hide form fields initially (they appear after dialogue)
        SetFormVisible(false);

        // Ensure dialogue panel starts visible (while onboarding panel is active, it will be used)
        SetDialogueVisible(true);
    }

    public void PlayGame()
    {
        bool hasCompleted = PlayerPrefs.GetInt(HasCompletedOnboardingKey, 0) == 1;

        if (hasCompleted)
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            StartOnboarding();
        }
    }

    private void StartOnboarding()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (onboardingPanel != null) onboardingPanel.SetActive(true);

        dialogueIndex = 0;
        selectedGender = 0;

        UpdateGenderButtonVisuals();
        SetFormVisible(false);

        // Show dialogue UI again at onboarding start
        SetDialogueVisible(true);

        ShowDialogueLine();
    }

    private void ShowDialogueLine()
    {
        if (dialogueText == null) return;

        if (onboardingLines == null || onboardingLines.Length == 0)
        {
            StartTyping("Welcome!");
            return;
        }

        string line = onboardingLines[Mathf.Clamp(dialogueIndex, 0, onboardingLines.Length - 1)];
        StartTyping(line);
    }

    private void OnNextPressed()
    {
        // If currently typing, skip to full line instead of going next immediately
        if (isTyping)
        {
            if (typeCoroutine != null)
                StopCoroutine(typeCoroutine);

            if (onboardingLines != null && onboardingLines.Length > 0)
            {
                string currentLine = onboardingLines[Mathf.Clamp(dialogueIndex, 0, onboardingLines.Length - 1)];
                if (dialogueText != null) dialogueText.text = currentLine;
            }

            isTyping = false;
            return;
        }

        // Move to next dialogue line
        dialogueIndex++;

        if (onboardingLines == null || onboardingLines.Length == 0)
        {
            ShowFormStep();
            return;
        }

        // If we just reached the last dialogue line, show it and immediately move to the form.
        if (dialogueIndex == onboardingLines.Length - 1)
        {
            ShowDialogueLine();
            ShowFormStep();
            return;
        }

        // If we somehow go past the end, also show the form
        if (dialogueIndex >= onboardingLines.Length)
        {
            ShowFormStep();
            return;
        }

        ShowDialogueLine();
    }

    private void ShowFormStep()
    {
        // Stop typing if it was running
        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }
        isTyping = false;

        // Hide dialogue panel when form is shown
        SetDialogueVisible(false);

        // Clear any leftover dialogue text
        if (dialogueText != null) dialogueText.text = "";

        SetFormVisible(true);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ValidateAndFinishOnboarding);
        }
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(visible);
        }
        else
        {
            // Fallback: at least hide the dialogue text itself if no panel is assigned
            if (dialogueText != null)
                dialogueText.gameObject.SetActive(visible);
        }
    }

    private void SetFormVisible(bool visible)
    {
        if (nameInput != null) nameInput.gameObject.SetActive(visible);
        if (maleButton != null) maleButton.gameObject.SetActive(visible);
        if (femaleButton != null) femaleButton.gameObject.SetActive(visible);
    }

    private void SelectGender(int genderValue)
    {
        selectedGender = genderValue;
        UpdateGenderButtonVisuals();
    }

    private void UpdateGenderButtonVisuals()
    {
        // Simple visible feedback: selected one becomes non-interactable
        if (maleButton != null) maleButton.interactable = selectedGender != 1;
        if (femaleButton != null) femaleButton.interactable = selectedGender != 2;
    }

    private void ValidateAndFinishOnboarding()
    {
        string playerName = nameInput != null ? nameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(playerName))
        {
            // If dialogue panel is hidden, show it to display the error
            SetDialogueVisible(true);

            if (dialogueText != null) dialogueText.text = "Name cannot be empty.";
            return;
        }

        if (selectedGender == 0)
        {
            // If dialogue panel is hidden, show it to display the error
            SetDialogueVisible(true);

            if (dialogueText != null) dialogueText.text = "Please choose Male or Female.";
            return;
        }

        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.SetInt(PlayerGenderKey, selectedGender);
        PlayerPrefs.SetInt(HasCompletedOnboardingKey, 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameSceneName);
    }

    private void ToggleMissionsPanel()
    {
        if (missionsPanel == null) return;
        bool isActive = missionsPanel.activeSelf;
        missionsPanel.SetActive(!isActive);
        if (!isActive) UpdateMissionButtonsFromSave();
    }

    private void UpdateMissionButtonsFromSave()
    {
        // Default: only Before (gameSceneName) unlocked for fresh installs
        bool hasAR = PlayerPrefs.GetInt(SaveARMissionCompletedKey, 0) == 1;
        bool cbComplete = PlayerPrefs.GetInt(SaveCircuitBreakerCompleteKey, 0) == 1;
        bool applComplete = PlayerPrefs.GetInt(SaveAppliancesCompleteKey, 0) == 1;
        bool evacStarted = PlayerPrefs.GetInt(SaveEvacuationStartedKey, 0) == 1;

        // Before always available
        if (missionsBeforeButton != null) missionsBeforeButton.interactable = true;

        // During unlocked after Before scene progression completed
        bool duringUnlocked = (hasAR && cbComplete && applComplete) || evacStarted;
        if (missionsDuringButton != null) missionsDuringButton.interactable = duringUnlocked;

        // After unlocked only when evacuation (level) was completed (use evacStarted as a proxy)
        bool afterUnlocked = evacStarted; // adjust if you have a stricter completion flag
        if (missionsAfterButton != null) missionsAfterButton.interactable = afterUnlocked;
    }

    private void LoadMissionScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Mission scene name not assigned.");
            return;
        }

        // Close missions panel
        if (missionsPanel != null) missionsPanel.SetActive(false);

        // Load the requested scene directly (Before/During/After will load their own scenes).
        SceneManager.LoadScene(sceneName);
    }

    private void ResumeGameFromSave()
    {
        // If there's a saved scene name, load it. Otherwise fallback to normal PlayGame flow.
        string savedScene = PlayerPrefs.GetString(SaveSceneKey, "");
        if (!string.IsNullOrEmpty(savedScene))
        {
            SceneManager.LoadScene(savedScene);
            return;
        }

        // No saved scene -> fallback to starting/ onboarding logic
        PlayGame();
    }

    // Internal: flag and handler for deferred during-scene start
    private bool pendingDuringLoad = false;

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!pendingDuringLoad) return;

        // Only act when we've loaded the gameplay scene
        if (scene.name == gameSceneName)
        {
            pendingDuringLoad = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Find DuringGameManager in the loaded scene and tell it to continue
            var duringMgr = FindObjectOfType<DuringGameManager>();
            if (duringMgr != null)
            {
                duringMgr.ContinueInScene();
            }
            else
            {
                Debug.LogWarning("[MainMenuController] DuringGameManager not found after loading gameplay scene.");
            }
        }
    }

    private void UpdateMainMenuButtons()
    {
        // Determine whether the player has ever played/completed onboarding or has a save
        bool hasCompletedOnboarding = PlayerPrefs.GetInt(HasCompletedOnboardingKey, 0) == 1;
        bool hasSaveScene = PlayerPrefs.HasKey(SaveSceneKey) || PlayerPrefs.HasKey(SavePlayerPosXKey);

        bool hasPlayed = hasCompletedOnboarding || hasSaveScene;

        // If player never played, show only Start button
        if (!hasPlayed)
        {
            if (startButton != null) startButton.gameObject.SetActive(true);
            if (resumeButton != null) resumeButton.gameObject.SetActive(false);
            if (newGameButton != null) newGameButton.gameObject.SetActive(false);
            return;
        }

        // Otherwise show Resume and New Game, hide Start
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (resumeButton != null) resumeButton.gameObject.SetActive(true);
        if (newGameButton != null) newGameButton.gameObject.SetActive(true);
    }

    public void OpenOptions()
    {
        if (!string.IsNullOrEmpty(optionsSceneName))
        {
            SceneManager.LoadScene(optionsSceneName);
        }
        else
        {
            Debug.Log("Options not set up (no optionsSceneName).");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit called.");
    }

    // Resets all saved progress and reloads the current (main menu) scene.
    public void RestartGame()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Reload the current scene to reflect cleared state (onboarding will run again)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void StartTyping(string line)
    {
        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        typeCoroutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            if (dialogueText != null)
                dialogueText.text += line[i];

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }
}
