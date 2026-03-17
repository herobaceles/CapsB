using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class OnboardingManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject onboardingPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private GameObject genderSelectPanel;

    [Header("Name Input")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Text nameErrorText;
    [SerializeField] private Button submitNameButton;

    [Header("Gender Selection")]
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;

    [Header("Dialogue Sequences (optional assets)")]
    [SerializeField] private System.Collections.Generic.List<DialogueLineData> introDialogueRich;
    [SerializeField] private System.Collections.Generic.List<DialogueLineData> afterNameDialogueRich;
    [SerializeField] private System.Collections.Generic.List<DialogueLineData> afterGenderDialogueRich;

    [Header("Settings")]
    [SerializeField] private int minNameLength = 2;
    [SerializeField] private int maxNameLength = 20;
    [SerializeField] private string missionSceneName = "MissionManager";

    private string enteredName;
    private PlayerData.Gender selectedGender = PlayerData.Gender.NotSpecified;
    private bool onboardingStarted;

    private void Start()
    {
        SetupButtons();
        // Do not auto-start onboarding; main menu controls when to begin
        onboardingPanel?.SetActive(false);
        nameInputPanel?.SetActive(false);
        genderSelectPanel?.SetActive(false);
    }

    // Entry point triggered by MainMenuManager when Play is pressed
    public void BeginOnboardingFlow()
    {
        if (onboardingStarted) return;
        if (PlayerData.Instance != null && !PlayerData.Instance.IsFirstTimePlaying())
        {
            SkipToMainMenu();
            return;
        }
        StartOnboarding();
    }

    private void SetupButtons()
    {
        submitNameButton?.onClick.AddListener(OnSubmitName);
        maleButton?.onClick.AddListener(() => OnSelectGender(PlayerData.Gender.Male));
        femaleButton?.onClick.AddListener(() => OnSelectGender(PlayerData.Gender.Female));
    }

    private void StartOnboarding()
    {
        if (onboardingStarted) return;
        onboardingStarted = true;

        HideAllPanels();
        onboardingPanel?.SetActive(true);

        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        yield return new WaitForSeconds(0.5f);

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager != null)
        {
            // Prefer rich dialogue authored in assets; if none, skip intro dialogue
            if (introDialogueRich != null && introDialogueRich.Count > 0)
            {
                dialogueManager.ShowDialogueSequence(introDialogueRich, ShowNameInput);
            }
            else
            {
                Debug.LogWarning("OnboardingManager: introDialogueRich is empty; skipping intro dialogue.");
                ShowNameInput();
            }
        }
        else
        {
            ShowNameInput();
        }
    }

    private void ShowNameInput()
    {
        nameInputPanel?.SetActive(true);

        if (nameErrorText != null)
            nameErrorText.gameObject.SetActive(false);

        if (nameInputField != null)
        {
            nameInputField.text = string.Empty;
            nameInputField.Select();
        }
    }

    private void OnSubmitName()
    {
        if (nameInputField == null) return;

        string name = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Please enter your name");
            return;
        }

        if (name.Length < minNameLength)
        {
            ShowError($"Name must be at least {minNameLength} characters");
            return;
        }

        if (name.Length > maxNameLength)
        {
            ShowError($"Name must be less than {maxNameLength} characters");
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z\s\-]+$"))
        {
            ShowError("Name can only contain letters, spaces, and hyphens");
            return;
        }

        enteredName = name;
        nameInputPanel?.SetActive(false);

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager != null)
        {
            if (afterNameDialogueRich != null && afterNameDialogueRich.Count > 0)
            {
                var placeholders = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"{name}", enteredName}
                };

                dialogueManager.ShowDialogueSequence(afterNameDialogueRich, ShowGenderSelection, placeholders);
            }
            else
            {
                Debug.LogWarning("OnboardingManager: afterNameDialogueRich is empty; skipping after-name dialogue.");
                ShowGenderSelection();
            }
        }
        else
        {
            ShowGenderSelection();
        }
    }

    private void ShowError(string message)
    {
        if (nameErrorText == null) return;

        nameErrorText.text = message;
        nameErrorText.gameObject.SetActive(true);
    }

    private void ShowGenderSelection()
    {
        genderSelectPanel?.SetActive(true);
    }

    private void OnSelectGender(PlayerData.Gender gender)
    {
        selectedGender = gender;
        genderSelectPanel?.SetActive(false);

        string characterType = gender == PlayerData.Gender.Male ? "male" : "female";

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager != null)
        {
            if (afterGenderDialogueRich != null && afterGenderDialogueRich.Count > 0)
            {
                var placeholders = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"{name}", enteredName},
                    {"{characterType}", characterType}
                };

                dialogueManager.ShowDialogueSequence(afterGenderDialogueRich, CompleteOnboarding, placeholders);
            }
            else
            {
                Debug.LogWarning("OnboardingManager: afterGenderDialogueRich is empty; skipping after-gender dialogue.");
                CompleteOnboarding();
            }
        }
        else
        {
            CompleteOnboarding();
        }
    }

    private void CompleteOnboarding()
    {
        PlayerData.Instance?.SaveOnboardingData(enteredName, selectedGender);
        StartCoroutine(LoadMissionScene());
    }

    private IEnumerator LoadMissionScene()
    {
        yield return new WaitForSeconds(0.3f);

        string sceneName = string.IsNullOrEmpty(missionSceneName) ? "MissionManager" : missionSceneName;
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            AppSceneLoader.EnsureExists();
            AppSceneLoader.Instance.LoadSceneSingle(sceneName);
        }
        else
        {
            Debug.LogError($"OnboardingManager: Scene '{sceneName}' not found. Showing main menu instead.");
            HideAllPanels();
            mainMenuPanel?.SetActive(true);
        }
    }

    private void HideAllPanels()
    {
        onboardingPanel?.SetActive(false);
        nameInputPanel?.SetActive(false);
        genderSelectPanel?.SetActive(false);
        mainMenuPanel?.SetActive(false);
    }

    private void SkipToMainMenu()
    {
        HideAllPanels();
        mainMenuPanel?.SetActive(true);
    }

    public void ResetAndRestartOnboarding()
    {
        PlayerData.Instance?.ResetAllData();
        onboardingStarted = false;
        StartOnboarding();
    }

    // Reset state and UI without starting onboarding; use when clearing progress from main menu
    public void ResetOnboardingUI()
    {
        onboardingStarted = false;
        StopAllCoroutines();
        HideAllPanels();
        mainMenuPanel?.SetActive(true);
    }
}
