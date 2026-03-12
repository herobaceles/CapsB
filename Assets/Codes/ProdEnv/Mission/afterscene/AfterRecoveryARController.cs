using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils; 

public enum MissionMode { HiddenDanger, CleanupGear, KitchenSafety, DisinfectHouse }

public class AfterRecoveryARController : MonoBehaviour
{
    public static AfterRecoveryARController Instance;

    public static string PendingNextMissionID = "";

    [Header("Main Environment Setup")]
    [Tooltip("Drag your main House/Environment object here so the script never deactivates it.")]
    public GameObject houseInteriorEnvironment; 

    [Header("Mission Flow Selection")]
    [Tooltip("Check this box if testing Kitchen Safety in Editor.")]
    public bool isMission2_KitchenSafety = false;
    [Tooltip("Check this box if testing Disinfect House in Editor.")]
    public bool isMission3_DisinfectHouse = false;

    [Header("Mission House Prefabs (From Project Folder)")]
    public GameObject housePrefabHiddenDanger;
    public GameObject housePrefabCleanupGear; 
    public GameObject housePrefabKitchen;
    public GameObject housePrefabDisinfect;

    // These will hold the actual spawned versions in the scene
    private GameObject spawnedHouseHiddenDanger;
    private GameObject spawnedHouseCleanupGear;
    private GameObject spawnedHouseKitchen;
    private GameObject spawnedHouseDisinfect;

    [Header("Mission Settings")]
    public MissionMode currentMissionMode;
    // THIS IS THE GLOBAL SETTING
    public int totalRequiredItems = 6; 

    // --- BULLETPROOF POINT TRACKING ---
    private HashSet<int> uniqueRecoveredItems = new HashSet<int>();
    private int genericRecoveredCount = 0;
    private int recoveredCount = 0;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); 
            return;
        }
        Instance = this;

        if (houseInteriorEnvironment != null) houseInteriorEnvironment.SetActive(true);

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

        // REMOVED: Don't instantiate here to avoid duplicates
    }

    private void Start()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "Edward");
        string savedID = "hiddendangermission";

        if (!string.IsNullOrEmpty(PendingNextMissionID))
        {
            savedID = PendingNextMissionID;
            PendingNextMissionID = ""; 
        }
        else if (MissionSelectManager.SelectedMission != null)
        {
            savedID = MissionSelectManager.SelectedMission.missionId.ToLower().Trim();
        }

        if (isMission2_KitchenSafety) savedID = "safeitemsmission";
        else if (isMission3_DisinfectHouse) savedID = "disinfectmission";
        
        Debug.Log("<color=cyan><b>AR Controller:</b></color> Loading Mission ID: " + savedID);
        
        if (savedID.Contains("safe"))
        {
            isMission2_KitchenSafety = true;
            isMission3_DisinfectHouse = false;
            currentMissionMode = MissionMode.KitchenSafety;
        }
        else if (savedID.Contains("disinfect"))
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

        if (questionText != null)
            originalQuestion = questionText.text;

        ARTapDetector arTap = FindObjectOfType<ARTapDetector>(true);
        HiddenDangerSpawner spawner = FindObjectOfType<HiddenDangerSpawner>(true);

        if (arTap != null && spawner != null)
        {
            arTap.SetHiddenDangerSpawner(spawner);
        }

        if (ProdDialogueManager.Instance != null)
        {
            if (joystickUI != null) joystickUI.SetActive(false);
            
            ProdDialogueSequenceBuilder sequence = ProdDialogueManager.Instance.CreateSequence();

            if (currentMissionMode == MissionMode.DisinfectHouse)
            {
                sequence.AddProfessorLine("The water is gone, but it left behind bacteria and mold.")
                        .AddProfessorLine("We need to disinfect the high-touch surfaces to make the house safe.")
                        .AddProfessorLine("Grab the spray and the rag. Let's get to work.");
            }
            else if (currentMissionMode == MissionMode.KitchenSafety)
            {
                sequence.AddProfessorLine("Now, the kitchen. This is critical. Floodwater is toxic. We have to know what's safe to eat and drink.")
                        .AddProfessorLine("Let's use the AR scanner to sort the safe from the unsafe. Your health depends on it.");
            }
            else
            {
                sequence.AddProfessorLine("The storm has passed, " + playerName + ".")
                        .AddProfessorLine("It's time to go home. But be careful.")
                        .AddProfessorLine("The flood leaves behind many hidden dangers.")
                        .AddProfessorLine("This is your final level: The Recovery.");
            }

            sequence.OnComplete(() => StartExplorationPhase()).Play();
        }
        else
        {
            StartExplorationPhase(); 
        }
    }

    public void StartExplorationPhase()
    {
        if (joystickUI != null) joystickUI.SetActive(true);
        if (playerController != null) playerController.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.SetActive(true);

        if (houseInteriorEnvironment != null) houseInteriorEnvironment.SetActive(true);

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

    public void StartCleanupGearAR() { EnableARRecovery(MissionMode.CleanupGear); }

    public void StartKitchenSafetyAR()
    {
        if (questionText != null) questionText.text = "Tap only the items that are SAFE.";
        EnableARRecovery(MissionMode.KitchenSafety);
    }

    public void StartDisinfectHouseAR() { EnableARRecovery(MissionMode.DisinfectHouse); }

    public void EnableARRecovery() { EnableARRecovery(currentMissionMode); }

    public void EnableARRecovery(MissionMode mode)
    {
        currentMissionMode = mode;
        
        uniqueRecoveredItems.Clear(); 
        genericRecoveredCount = 0;
        recoveredCount = 0;
        
        if (houseInteriorEnvironment != null) houseInteriorEnvironment.SetActive(true);
        if (arSpawnWall != null) arSpawnWall.SetActive(true);

        // Only instantiate if not already created
        if (mode == MissionMode.HiddenDanger && spawnedHouseHiddenDanger == null && housePrefabHiddenDanger != null)
        {
            spawnedHouseHiddenDanger = Instantiate(housePrefabHiddenDanger, Vector3.zero, Quaternion.identity);
            Debug.Log("Instantiated HiddenDanger house");
        }
        if (mode == MissionMode.CleanupGear && spawnedHouseCleanupGear == null && housePrefabCleanupGear != null)
        {
            spawnedHouseCleanupGear = Instantiate(housePrefabCleanupGear, Vector3.zero, Quaternion.identity);
            Debug.Log("Instantiated CleanupGear house");
        }
        if (mode == MissionMode.KitchenSafety && spawnedHouseKitchen == null && housePrefabKitchen != null)
        {
            spawnedHouseKitchen = Instantiate(housePrefabKitchen, Vector3.zero, Quaternion.identity);
            Debug.Log("Instantiated KitchenSafety house");
        }
        if (mode == MissionMode.DisinfectHouse && spawnedHouseDisinfect == null && housePrefabDisinfect != null)
        {
            spawnedHouseDisinfect = Instantiate(housePrefabDisinfect, Vector3.zero, Quaternion.identity);
            Debug.Log("Instantiated DisinfectHouse house");
        }

        // Deactivate all houses first
        if (spawnedHouseHiddenDanger != null) spawnedHouseHiddenDanger.SetActive(false);
        if (spawnedHouseCleanupGear != null) spawnedHouseCleanupGear.SetActive(false);
        if (spawnedHouseKitchen != null) spawnedHouseKitchen.SetActive(false);
        if (spawnedHouseDisinfect != null) spawnedHouseDisinfect.SetActive(false);

        // Activate only the current mission house
        if (mode == MissionMode.HiddenDanger && spawnedHouseHiddenDanger != null) 
            spawnedHouseHiddenDanger.SetActive(true);
        if (mode == MissionMode.CleanupGear && spawnedHouseCleanupGear != null) 
            spawnedHouseCleanupGear.SetActive(true);
        if (mode == MissionMode.KitchenSafety && spawnedHouseKitchen != null) 
            spawnedHouseKitchen.SetActive(true);
        if (mode == MissionMode.DisinfectHouse && spawnedHouseDisinfect != null) 
            spawnedHouseDisinfect.SetActive(true);

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
        if (bootOrigin != null) bootOrigin.transform.position = spawnPosition;

        UpdateSpawnerPosition();

        float timeout = 3.0f;
        while (timeout > 0f)
        {
            var arCamera = ARRuntimeContext.Instance != null ? ARRuntimeContext.Instance.ResolveARCamera() : null;
            if (arCamera != null && arCamera.gameObject.activeInHierarchy && arCamera.enabled)
            {
                UpdateSpawnerPosition();
                if (playerController != null) playerController.SetActive(false);
                if (gameplayCamera != null) gameplayCamera.SetActive(false);
                if (houseInteriorEnvironment != null) houseInteriorEnvironment.SetActive(true);

                ARCameraBinder binder = FindObjectOfType<ARCameraBinder>();
                if (binder != null) binder.RebindCamera();
                yield break;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (playerController != null) playerController.SetActive(false);
        if (gameplayCamera != null) gameplayCamera.SetActive(false);
        if (houseInteriorEnvironment != null) houseInteriorEnvironment.SetActive(true);
    }

    public void RegisterSpawnedDanger(HiddenDangerItem dangerItem)
    {
        dangerItem.OnRecovered -= HandleDangerRecovered;
        dangerItem.OnRecovered += HandleDangerRecovered;
    }

    public void HandleItemRecovered(GameObject recoveredObject)
    {
        // Only increment count if the object has the correct tag
        if (recoveredObject == null)
        {
            Debug.LogWarning("HandleItemRecovered: recoveredObject is null!");
            return;
        }
        
        Debug.LogWarning($"HandleItemRecovered: Object '{recoveredObject.name}' with tag '{recoveredObject.tag}' was recovered. Current mission mode: {currentMissionMode}");
        
        if (recoveredObject.tag != "CleanupItem")
        {
            Debug.LogWarning($"HandleItemRecovered: Object '{recoveredObject.name}' ignored because tag is not 'CleanupItem'");
            return;
        }
        
        genericRecoveredCount++;
        Debug.LogWarning($"HandleItemRecovered: Count incremented to {genericRecoveredCount}");
        CheckMissionProgress();
    }

    private void HandleDangerRecovered(HiddenDangerItem recoveredItem)
    {
        if (recoveredItem != null)
        {
            recoveredItem.OnRecovered -= HandleDangerRecovered;
            uniqueRecoveredItems.Add(recoveredItem.GetInstanceID());
            Debug.LogWarning($"HandleDangerRecovered: Added item {recoveredItem.name} with ID {recoveredItem.GetInstanceID()}. Total unique items: {uniqueRecoveredItems.Count}");
        }
        CheckMissionProgress();
    }

    private void CheckMissionProgress()
    {
        if (currentMissionMode == MissionMode.HiddenDanger)
        {
            recoveredCount = uniqueRecoveredItems.Count;
        }
        else
        {
            recoveredCount = uniqueRecoveredItems.Count + genericRecoveredCount;
        }

        Debug.LogWarning($"<color=yellow>Progress: {recoveredCount} / {totalRequiredItems} (Mode: {currentMissionMode})</color>");

        if (recoveredCount >= totalRequiredItems)
        {
            Debug.LogWarning($"<color=green>Mission Complete! Required: {totalRequiredItems}, Recovered: {recoveredCount}</color>");
            
            if (arSpawnWall != null) arSpawnWall.SetActive(false);
            if (sprayBottleProp != null) sprayBottleProp.SetActive(false);
            if (cleaningRagProp != null) cleaningRagProp.SetActive(false);
            if (disinfectButtonUI != null) disinfectButtonUI.SetActive(false);

            if (ARRuntimeContext.Instance != null) ARRuntimeContext.Instance.SetARActive(false);
            if (gameplayCamera != null) gameplayCamera.SetActive(true);
            if (playerController != null) playerController.SetActive(true);
            if (joystickUI != null) joystickUI.SetActive(true);
            
            if (houseInteriorEnvironment != null) houseInteriorEnvironment.SetActive(true);

            // Handle mission completion based on current mode
            if (currentMissionMode == MissionMode.CleanupGear)
            {
                // After CleanupGear, go to HiddenDanger drag-and-drop mission
                currentMissionMode = MissionMode.HiddenDanger; 
                ShowTransitionStory();
            }
            else if (currentMissionMode == MissionMode.KitchenSafety)
            {
                // Kitchen Safety mission complete - go to next
                ShowKitchenOutroStory();
            }
            else if (currentMissionMode == MissionMode.DisinfectHouse)
            {
                // Disinfect mission complete - show final
                ShowDisinfectOutroStory();
            }
            else if (currentMissionMode == MissionMode.HiddenDanger)
            {
                // Hidden Danger drag-and-drop mission complete
                ShowHiddenDangerOutroStory();
            }
        }
    }

    private void ShowTransitionStory()
    {
        if (joystickUI != null) joystickUI.SetActive(false);
        if (ProdDialogueManager.Instance != null)
        {
            ProdDialogueManager.Instance.CreateSequence()
                .AddProfessorLine("Excellent! You have secured your protective gear. Now, carefully explore the house to clear the hidden dangers.")
                .OnComplete(() => StartExplorationPhase())
                .Play();
        }
        else StartExplorationPhase();
    }

    private void ShowHiddenDangerOutroStory()
    {
        if (joystickUI != null) joystickUI.SetActive(false);
        if (ProdDialogueManager.Instance != null)
        {
            ProdDialogueManager.Instance.CreateSequence()
                .AddProfessorLine("Great job! You have safely removed the animals and immediate hazards. The house is much safer now.")
                .OnComplete(() => {
                    if (joystickUI != null) joystickUI.SetActive(true);
                    if (playerController != null) playerController.SetActive(true);
                    if (missionCompleteBanner != null) missionCompleteBanner.SetActive(false); 
                    if (achievementBackground != null) achievementBackground.SetActive(true);
                    
                    // Move to next mission (Kitchen Safety)
                    PendingNextMissionID = "safeitemsmission";
                })
                .Play();
        }
    }

    private void ShowKitchenOutroStory()
    {
        if (joystickUI != null) joystickUI.SetActive(false);
        if (ProdDialogueManager.Instance != null)
        {
            ProdDialogueManager.Instance.CreateSequence()
                .AddProfessorLine("Excellent! Undamaged canned goods are safe if you wash the can first. And sealed water is our only safe drinking source. Everything else... THROW IT AWAY. When in doubt, throw it out.")
                .OnComplete(() => {
                    if (joystickUI != null) joystickUI.SetActive(true);
                    if (playerController != null) playerController.SetActive(true);
                    if (missionCompleteBanner != null) missionCompleteBanner.SetActive(false); 
                    if (achievementBackground != null) achievementBackground.SetActive(true);
                    
                    // Move to next mission (Disinfect)
                    PendingNextMissionID = "disinfectmission";
                })
                .Play();
        }
    }

    private void ShowDisinfectOutroStory()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "Edward");
        if (joystickUI != null) joystickUI.SetActive(false);
        if (ProdDialogueManager.Instance != null)
        {
            ProdDialogueManager.Instance.CreateSequence()
                .AddProfessorLine("Great work! Bleach and water remove bacteria left by floodwater. Your home is finally safe again.")
                .AddProfessorLine("You did it, " + playerName + ". You faced the warning, survived the storm, and recovered from the aftermath.")
                .AddProfessorLine("Disasters are a part of life, but with the right knowledge and preparation, we can be stronger than any storm.")
                .AddProfessorLine("You are no longer just " + playerName + "... you are truly BaHanda!")
                .OnComplete(() => ShowFinalGameCompleteUI())
                .Play();
        }
        else ShowFinalGameCompleteUI();
    }

    private void ShowFinalGameCompleteUI()
    {
        if (joystickUI != null) joystickUI.SetActive(true); 
        if (playerController != null) playerController.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.SetActive(true);
        if (missionCompleteBanner != null) missionCompleteBanner.SetActive(false);
        if (achievementBackground != null) achievementBackground.SetActive(true);
    }

    public void ContinueToGame()
    {
        string currentID = "hiddendangermission";
        if (isMission3_DisinfectHouse) currentID = "disinfectmission";
        else if (isMission2_KitchenSafety) currentID = "safeitemsmission";

        if (currentID == "hiddendangermission")
        {
            PendingNextMissionID = "safeitemsmission"; 
            RestartLevel();
        }
        else if (currentID == "safeitemsmission")
        {
            PendingNextMissionID = "disinfectmission"; 
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