using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils; 

public enum MissionMode { HiddenDanger, CleanupGear, KitchenSafety, DisinfectHouse }

public class AfterRecoveryARController : MonoBehaviour
{
    public static AfterRecoveryARController Instance;

    [Header("Mission Flow Selection")]
    [Tooltip("Check this box if testing Kitchen Safety in Editor.")]
    public bool isMission2_KitchenSafety = false;
    [Tooltip("Check this box if testing Disinfect House in Editor.")]
    public bool isMission3_DisinfectHouse = false;

    [Header("Mission House Roots")]
    public GameObject houseRootHiddenDanger;
    public GameObject houseRootCleanupGear; 
    public GameObject houseRootKitchen;
    public GameObject houseRootDisinfect;

    [Header("Mission Settings")]
    public MissionMode currentMissionMode;
    private int totalRequiredItems = 2; 

    [Header("Mission Triggers")]
    [SerializeField] private GameObject arTriggerHiddenDanger;
    [SerializeField] private GameObject arTriggerKitchenSafety; 
    [SerializeField] private GameObject arTriggerDisinfectHouse; 
    [SerializeField] private GameObject quizZoneTrigger; 

    [Header("AR Spawn Coordinates")]
    private Vector3 spawnPosition = new Vector3(0f, 0f, 0f);
    private Vector3 kitchenItemSpawn = new Vector3(-5f, 0f, 1.64f);
    private Vector3 defaultItemSpawn = new Vector3(-5f, 0f, 3.05f);

    [Header("Feedback Sprites")]
    [SerializeField] private GameObject greenCheckSprite;
    [SerializeField] private GameObject redXSprite;
    private GameObject activeFeedbackInstance;

    [Header("Quiz Panels")]
    [SerializeField] private GameObject structuralDamageQuiz;
    [SerializeField] private GameObject hiddenDangersQuiz;
    [SerializeField] private GameObject protectiveGearQuiz;
    [SerializeField] private GameObject finalFeedbackPanel;

    [Header("Intro Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueContentText;
    [SerializeField] private GameObject dialogueNextButton;
    [SerializeField] private GameObject dialoguePreviousButton;

    [Header("Victory UI")]
    [SerializeField] private GameObject missionCompleteBanner;
    [SerializeField] private GameObject achievementBackground; 

    [Header("Quiz Text UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private float feedbackDuration = 3f;

    [Header("Disable When AR Starts")]
    [SerializeField] private GameObject arTriggerToDisable;
    [SerializeField] private GameObject playerController;
    [SerializeField] private GameObject gameplayCamera;
    [SerializeField] private GameObject joystickUI; 

    [Header("Disable After AR Ends")]
    [Tooltip("Drag the invisible wall/plane you click to spawn items here. It will disable when the AR mission ends.")]
    [SerializeField] private GameObject arSpawnWall; 

    [Header("Disinfect Mission Props")]
    [SerializeField] private GameObject sprayBottleProp;
    [SerializeField] private GameObject cleaningRagProp;
    [SerializeField] private GameObject disinfectButtonUI; 

    private int currentQuizIndex = 0;
    private string originalQuestion;
    private int recoveredCount = 0;
    private string[] introLines;
    private int currentDialogueIndex = 0;
    private Coroutine typingCoroutine;
    private bool isOutroDialogue = false; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); 
            return;
        }
        Instance = this;

        if (structuralDamageQuiz != null) structuralDamageQuiz.SetActive(false);
        if (hiddenDangersQuiz != null) hiddenDangersQuiz.SetActive(false);
        if (protectiveGearQuiz != null) protectiveGearQuiz.SetActive(false);
        if (finalFeedbackPanel != null) finalFeedbackPanel.SetActive(false);
        if (missionCompleteBanner != null) missionCompleteBanner.SetActive(false);
        if (achievementBackground != null) achievementBackground.SetActive(false);
        
        if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(false);
        if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(false);
        if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(false);
        if (arTriggerToDisable != null) arTriggerToDisable.SetActive(false);
        if (quizZoneTrigger != null) quizZoneTrigger.SetActive(false); 

        if (sprayBottleProp != null) sprayBottleProp.SetActive(false);
        if (cleaningRagProp != null) cleaningRagProp.SetActive(false);
        if (disinfectButtonUI != null) disinfectButtonUI.SetActive(false);
    }

    private void Start()
    {
        isOutroDialogue = false;

        string selectedMission = PlayerPrefs.GetString("SelectedMissionID", "hiddendangermission").ToLower().Trim();
        string playerName = PlayerPrefs.GetString("PlayerName", "Edward");
        
        Debug.Log("--- LOADING MISSION ID: '" + selectedMission + "' ---");
        
        if (selectedMission == "safeitemsmission")
        {
            isMission2_KitchenSafety = true;
            isMission3_DisinfectHouse = false;
            currentMissionMode = MissionMode.KitchenSafety;
        }
        else if (selectedMission == "disinfectmission")
        {
            isMission2_KitchenSafety = false;
            isMission3_DisinfectHouse = true;
            currentMissionMode = MissionMode.DisinfectHouse;
        }
        else 
        {
            isMission2_KitchenSafety = false;
            isMission3_DisinfectHouse = false;
            currentMissionMode = MissionMode.HiddenDanger;
        }

        if (isMission3_DisinfectHouse)
        {
            introLines = new string[] {
                "The water is gone, but it left behind bacteria and mold.",
                "We need to disinfect the high-touch surfaces to make the house safe.",
                "Grab the spray and the rag. Let's get to work."
            };
        }
        else if (isMission2_KitchenSafety)
        {
            introLines = new string[] {
                "Now, the kitchen. This is critical. Floodwater is toxic. We have to know what's safe to eat and drink.",
                "Let's use the AR scanner to sort the safe from the unsafe. Your health depends on it."
            };
        }
        else
        {
            introLines = new string[] {
                "The storm has passed, " + playerName + ".",
                "It's time to go home. But be careful.",
                "The flood leaves behind many hidden dangers.",
                "This is your final level: The Recovery."
            };
        }

        if (questionText != null)
            originalQuestion = questionText.text;

        ARTapDetector arTap = FindObjectOfType<ARTapDetector>(true);
        HiddenDangerSpawner spawner = FindObjectOfType<HiddenDangerSpawner>(true);

        if (arTap != null && spawner != null)
        {
            arTap.SetHiddenDangerSpawner(spawner);
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (joystickUI != null) joystickUI.SetActive(false);
            if (playerController != null) playerController.SetActive(false);
            
            currentDialogueIndex = 0;
            UpdateDialogueView();
        }
        else
        {
            StartExplorationPhase(); 
        }
    }

    public void ShowNextLine()
    {
        if (currentDialogueIndex < introLines.Length - 1)
        {
            currentDialogueIndex++;
            UpdateDialogueView();
        }
        else
        {
            if (isOutroDialogue)
            {
                if (isMission3_DisinfectHouse)
                {
                    ShowFinalGameCompleteUI();
                }
                else
                {
                    // FIX: Disable MissionCompleteUI and show ONLY AchievementsPanel
                    if (missionCompleteBanner != null) missionCompleteBanner.SetActive(false); 
                    if (achievementBackground != null) achievementBackground.SetActive(true);
                    if (dialoguePanel != null) dialoguePanel.SetActive(false);
                }
            }
            else
            {
                StartExplorationPhase();
            }
        }
    }

    public void ShowPreviousLine()
    {
        if (currentDialogueIndex > 0)
        {
            currentDialogueIndex--;
            UpdateDialogueView();
        }
    }

    private void UpdateDialogueView()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeIntroDialogue(introLines[currentDialogueIndex]));

        if (dialoguePreviousButton != null)
            dialoguePreviousButton.SetActive(currentDialogueIndex > 0);
    }

    private IEnumerator TypeIntroDialogue(string message)
    {
        if (dialogueContentText == null) yield break;
        if (dialogueNextButton != null) dialogueNextButton.SetActive(false);

        dialogueContentText.text = "";
        foreach (char letter in message.ToCharArray())
        {
            dialogueContentText.text += letter;
            yield return new WaitForSecondsRealtime(0.04f); 
        }

        if (dialogueNextButton != null) dialogueNextButton.SetActive(true);
    }

    public void StartExplorationPhase()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (joystickUI != null) joystickUI.SetActive(true);
        if (playerController != null) playerController.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.SetActive(true);

        if (playerController != null)
        {
            playerController.transform.position = spawnPosition;
        }

        if (isMission3_DisinfectHouse)
        {
            if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(true);
            if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(false);
            if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(false);
            if (quizZoneTrigger != null) quizZoneTrigger.SetActive(false);
        }
        else if (isMission2_KitchenSafety)
        {
            if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(true);
            if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(false);
            if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(false);
            if (quizZoneTrigger != null) quizZoneTrigger.SetActive(false);
        }
        else
        {
            if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(true);
            if (quizZoneTrigger != null) quizZoneTrigger.SetActive(true);
            if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(false);
            if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(false);
        }
    }

    public Vector3 GetCorrectedJoystickInput(float inputX, float inputY)
    {
        if (SceneManager.GetActiveScene().name == "AfterMission")
            return new Vector3(-inputX, 0f, -inputY);
        return new Vector3(inputX, 0f, inputY);
    }

    public void OnCorrectAnswer()
    {
        string correctMsg = "Correct!";
        if (currentQuizIndex == 0)
            correctMsg = "Exactly. We must check for deep cracks or a shifting foundation. The house seems stable for now. Okay, let's go in, but carefully.";
        else if (currentQuizIndex == 1)
            correctMsg = "Exactly! Flood waters drive animals inside.";
        else if (currentQuizIndex == 2)
            correctMsg = "Correct! Safety gear is essential to prevent infection.";

        TriggerFeedback(true); 

        StopAllCoroutines();
        StartCoroutine(ShowFeedbackAndTransition(correctMsg, true));
    }

    public void OnWrongAnswer()
    {
        string wrongMsg = "Incorrect. Try again!";
        if (currentQuizIndex == 0)
            wrongMsg = "Not quite. Foundation cracks are the most dangerous. Try again!";
        else if (currentQuizIndex == 1)
            wrongMsg = "Wrong. Wild animals often seek shelter in flood debris. Try again!";
        else if (currentQuizIndex == 2)
            wrongMsg = "No. Standard clothes won't protect you from toxins. Try again!";

        TriggerFeedback(false); 

        StopAllCoroutines();
        StartCoroutine(ShowFeedbackAndTransition(wrongMsg, false));
    }

    public void TriggerFeedback(bool isCorrect)
    {
        SpawnFeedbackVisual(isCorrect ? greenCheckSprite : redXSprite);
    }

    public void TriggerFeedback(bool isCorrect, Vector3 itemPosition)
    {
        SpawnFeedbackVisualAtPosition(isCorrect ? greenCheckSprite : redXSprite, itemPosition);
    }

    private void SpawnFeedbackVisual(GameObject sourceSprite)
    {
        if (sourceSprite == null) return;
        if (activeFeedbackInstance != null) Destroy(activeFeedbackInstance);

        Camera arCam = Camera.main;
        if (ARRuntimeContext.Instance != null && ARRuntimeContext.Instance.ResolveARCamera() != null)
        {
            arCam = ARRuntimeContext.Instance.ResolveARCamera();
        }

        if (arCam != null)
        {
            activeFeedbackInstance = Instantiate(sourceSprite, arCam.transform);
            activeFeedbackInstance.SetActive(true);
            activeFeedbackInstance.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            activeFeedbackInstance.transform.localRotation = Quaternion.identity;
            activeFeedbackInstance.transform.localScale = sourceSprite.transform.localScale;
            
            Destroy(activeFeedbackInstance, feedbackDuration);
        }
    }

    private void SpawnFeedbackVisualAtPosition(GameObject sourceSprite, Vector3 itemPosition)
    {
        if (sourceSprite == null) return;
        if (activeFeedbackInstance != null) Destroy(activeFeedbackInstance);

        activeFeedbackInstance = Instantiate(sourceSprite, itemPosition, Quaternion.identity);
        activeFeedbackInstance.SetActive(true);
        activeFeedbackInstance.transform.localScale = sourceSprite.transform.localScale;

        Camera mainCam = Camera.main;
        if (ARRuntimeContext.Instance != null && ARRuntimeContext.Instance.ResolveARCamera() != null)
        {
            mainCam = ARRuntimeContext.Instance.ResolveARCamera();
        }
        
        if (mainCam != null)
        {
            activeFeedbackInstance.transform.rotation = mainCam.transform.rotation;
        }

        Destroy(activeFeedbackInstance, feedbackDuration);
    }

    private IEnumerator ShowFeedbackAndTransition(string message, bool isCorrect)
    {
        if (questionText != null) questionText.text = message;
        yield return new WaitForSeconds(feedbackDuration);

        if (isCorrect) ContinueToNextQuiz();
        else if (questionText != null) questionText.text = originalQuestion;
    }

    private void ContinueToNextQuiz()
    {
        currentQuizIndex++;
        if (currentQuizIndex == 1)
        {
            if (structuralDamageQuiz != null) structuralDamageQuiz.SetActive(false);
            if (hiddenDangersQuiz != null) hiddenDangersQuiz.SetActive(true);
            UpdateOriginalQuestionText(hiddenDangersQuiz);
        }
        else if (currentQuizIndex == 2)
        {
            if (hiddenDangersQuiz != null) hiddenDangersQuiz.SetActive(false);
            if (protectiveGearQuiz != null) protectiveGearQuiz.SetActive(true);
            UpdateOriginalQuestionText(protectiveGearQuiz);
        }
        else
        {
            if (protectiveGearQuiz != null) protectiveGearQuiz.SetActive(false);
            if (finalFeedbackPanel != null) finalFeedbackPanel.SetActive(true);
        }
    }

    private void UpdateOriginalQuestionText(GameObject activePanel)
    {
        if (activePanel != null)
        {
            TMP_Text newQText = activePanel.GetComponentInChildren<TMP_Text>();
            if (newQText != null)
            {
                questionText = newQText;
                originalQuestion = newQText.text;
            }
        }
    }

    public void StartCleanupGearAR()
    {
        EnableARRecovery(MissionMode.CleanupGear);
    }

    public void StartKitchenSafetyAR()
    {
        if (questionText != null) questionText.text = "Tap only the items that are SAFE.";
        EnableARRecovery(MissionMode.KitchenSafety);
    }

    public void StartDisinfectHouseAR()
    {
        EnableARRecovery(MissionMode.DisinfectHouse);
    }

    public void EnableARRecovery()
    {
        EnableARRecovery(currentMissionMode);
    }

    public void EnableARRecovery(MissionMode mode)
    {
        currentMissionMode = mode;
        recoveredCount = 0;
        
        if (mode == MissionMode.KitchenSafety) totalRequiredItems = 2; 
        else if (mode == MissionMode.HiddenDanger) totalRequiredItems = 3;
        else if (mode == MissionMode.DisinfectHouse) totalRequiredItems = 6; 
        else totalRequiredItems = 3; 

        bool isDisinfect = (mode == MissionMode.DisinfectHouse);
        if (sprayBottleProp != null) sprayBottleProp.SetActive(isDisinfect);
        if (cleaningRagProp != null) cleaningRagProp.SetActive(isDisinfect);
        if (disinfectButtonUI != null) disinfectButtonUI.SetActive(false);

        if (finalFeedbackPanel != null) finalFeedbackPanel.SetActive(false);
        if (joystickUI != null) joystickUI.SetActive(false); 
        
        if (arTriggerToDisable != null) arTriggerToDisable.SetActive(false);
        if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(false);
        if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(false);
        if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(false);
        if (quizZoneTrigger != null) quizZoneTrigger.SetActive(false);
        
        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(true);

        StartCoroutine(DisableGameplayCameraWhenARReady());
    }

    private void UpdateSpawnerPosition()
    {
        HiddenDangerSpawner spawner = FindObjectOfType<HiddenDangerSpawner>(true);
        if (spawner != null)
        {
            if (currentMissionMode == MissionMode.KitchenSafety)
                spawner.transform.position = kitchenItemSpawn;
            else
                spawner.transform.position = defaultItemSpawn;
        }
    }

    private IEnumerator DisableGameplayCameraWhenARReady()
    {
        XROrigin bootOrigin = FindObjectOfType<XROrigin>();
        if (bootOrigin != null)
        {
            bootOrigin.transform.position = spawnPosition;
        }

        UpdateSpawnerPosition();

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Wall") || obj.name.Contains("Occluder"))
            {
                MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }

        float timeout = 3.0f;
        while (timeout > 0f)
        {
            var arCamera = ARRuntimeContext.Instance != null ? ARRuntimeContext.Instance.ResolveARCamera() : null;
            if (arCamera != null && arCamera.gameObject.activeInHierarchy && arCamera.enabled)
            {
                UpdateSpawnerPosition();
                if (playerController != null) playerController.SetActive(false);
                if (gameplayCamera != null) gameplayCamera.SetActive(false);
                
                ARCameraBinder binder = FindObjectOfType<ARCameraBinder>();
                if (binder != null) binder.RebindCamera();
                yield break;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (playerController != null) playerController.SetActive(false);
        if (gameplayCamera != null) gameplayCamera.SetActive(false);
    }

    public void RegisterSpawnedDanger(HiddenDangerItem dangerItem)
    {
        dangerItem.OnRecovered += HandleDangerRecovered;
    }

    public void HandleItemRecovered()
    {
        recoveredCount++;
        CheckMissionProgress();
    }

    private void HandleDangerRecovered(HiddenDangerItem recoveredItem)
    {
        recoveredItem.OnRecovered -= HandleDangerRecovered;
        recoveredCount++;
        CheckMissionProgress();
    }

    private void CheckMissionProgress()
    {
        if (recoveredCount >= totalRequiredItems)
        {
            isOutroDialogue = true; 
            
            // FIX: Deactivate the AR Spawn Wall immediately when the AR phase ends
            if (arSpawnWall != null) arSpawnWall.SetActive(false);

            if (sprayBottleProp != null) sprayBottleProp.SetActive(false);
            if (cleaningRagProp != null) cleaningRagProp.SetActive(false);
            if (disinfectButtonUI != null) disinfectButtonUI.SetActive(false);

            if (ARRuntimeContext.Instance != null) ARRuntimeContext.Instance.SetARActive(false);
            
            if (gameplayCamera != null) gameplayCamera.SetActive(true);

            if (currentMissionMode == MissionMode.CleanupGear || currentMissionMode == MissionMode.HiddenDanger)
            {
                ShowTransitionStory();
            }
            else if (currentMissionMode == MissionMode.KitchenSafety)
            {
                ShowKitchenOutroStory();
            }
            else if (currentMissionMode == MissionMode.DisinfectHouse)
            {
                ShowDisinfectOutroStory();
            }
        }
    }

    private void ShowTransitionStory()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (joystickUI != null) joystickUI.SetActive(false);
        if (playerController != null) playerController.SetActive(false);

        introLines = new string[] { "Excellent! You have secured the immediate dangers. But there is more to do to make this house a home again." };
        currentDialogueIndex = 0;
        UpdateDialogueView();
    }

    private void ShowKitchenOutroStory()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (joystickUI != null) joystickUI.SetActive(false);
        if (playerController != null) playerController.SetActive(false);

        introLines = new string[] { "Excellent! Undamaged canned goods are safe if you wash the can first. And sealed water is our only safe drinking source. Everything else... THROW IT AWAY. When in doubt, throw it out." };
        currentDialogueIndex = 0;
        UpdateDialogueView();
    }

    private void ShowDisinfectOutroStory()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "Edward");
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (joystickUI != null) joystickUI.SetActive(false);
        if (playerController != null) playerController.SetActive(false);

        introLines = new string[] { 
            "Great work! Bleach and water remove bacteria left by floodwater. Your home is finally safe again.",
            "You did it, " + playerName + ". You faced the warning, survived the storm, and recovered from the aftermath.",
            "Disasters are a part of life, but with the right knowledge and preparation, we can be stronger than any storm.",
            "You are no longer just " + playerName + "... you are truly BaHanda!"
        };
        currentDialogueIndex = 0;
        UpdateDialogueView();
    }

    private void ShowFinalGameCompleteUI()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (joystickUI != null) joystickUI.SetActive(false); 
        if (playerController != null) playerController.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.SetActive(true);

        // FIX: Ensure missionCompleteBanner stays disabled, even at the very end of the final level.
        if (missionCompleteBanner != null)
        {
            missionCompleteBanner.SetActive(false);
        }
        
        // FIX: Show ONLY the achievement background
        if (achievementBackground != null) achievementBackground.SetActive(true);
    }

    public void ContinueToGame()
    {
        string currentID = PlayerPrefs.GetString("SelectedMissionID", "hiddendangermission").ToLower().Trim();

        if (currentID == "hiddendangermission")
        {
            PlayerPrefs.SetString("SelectedMissionID", "safeitemsmission");
            PlayerPrefs.Save();
            RestartLevel();
        }
        else if (currentID == "safeitemsmission")
        {
            PlayerPrefs.SetString("SelectedMissionID", "disinfectmission");
            PlayerPrefs.Save();
            RestartLevel();
        }
        else
        {
            if (missionCompleteBanner != null) missionCompleteBanner.SetActive(false);
            if (achievementBackground != null) achievementBackground.SetActive(false);
            FinalizeMission();
        }
    }

    public void FinalizeMission()
    {
        if (AfterMissionManager.Instance != null)
            AfterMissionManager.Instance.CompleteRecoveryMission();
    }
    
    public void RestartLevel()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}