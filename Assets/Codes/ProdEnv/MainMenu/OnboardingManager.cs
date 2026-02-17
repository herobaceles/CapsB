using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("Settings")]
    [SerializeField] private int minNameLength = 2;
    [SerializeField] private int maxNameLength = 20;

    private string enteredName;
    private PlayerData.Gender selectedGender = PlayerData.Gender.NotSpecified;
    private bool onboardingStarted;

    private void Start()
    {
        SetupButtons();

        if (PlayerData.Instance != null && PlayerData.Instance.IsFirstTimePlaying())
        {
            StartOnboarding();
        }
        else
        {
            SkipToMainMenu();
        }
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

        ProdDialogueManager.Instance.CreateSequence()
            .AddProfessorLine("Hello there! My name is Professor Lingap, and I'll be your guide on this journey.")
            .AddProfessorLine("'BaHanda' means 'Flood Ready,' and that's exactly what we're going to become!")
            .AddProfessorLine("Before we start our training, tell me a little about yourself.")
            .AddProfessorLine("First, what should I call you?")
            .OnComplete(ShowNameInput)
            .Play();
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

        ProdDialogueManager.Instance.CreateSequence()
            .AddProfessorLine($"Nice to meet you, {enteredName}!")
            .AddProfessorLine("Now, please select your character.")
            .OnComplete(ShowGenderSelection)
            .Play();
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

        ProdDialogueManager.Instance.CreateSequence()
            .AddProfessorLine($"Great choice! Your {characterType} character is ready.")
            .AddProfessorLine($"Welcome aboard, {enteredName}!")
            .AddProfessorLine("You're about to learn important skills that could save lives during a flood emergency.")
            .AddProfessorLine("Are you ready to become BaHanda? Let's begin!")
            .OnComplete(CompleteOnboarding)
            .Play();
    }

    private void CompleteOnboarding()
    {
        PlayerData.Instance?.SaveOnboardingData(enteredName, selectedGender);
        StartCoroutine(TransitionToMainMenu());
    }

    private IEnumerator TransitionToMainMenu()
    {
        yield return new WaitForSeconds(0.3f);

        HideAllPanels();
        mainMenuPanel?.SetActive(true);
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
}
