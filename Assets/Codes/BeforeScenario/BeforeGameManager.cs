using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class BeforeSceneManager : MonoBehaviour
{
    public static BeforeSceneManager Instance { get; private set; }

    private const string PlayerNameKey = "PLAYER_NAME";
    private const string PlayerGenderKey = "PLAYER_GENDER";
    // Save keys for local autosave
    private const string SavePrefix = "SAVE_";
    private const string SaveSceneKey = SavePrefix + "SCENE";
    private const string SavePlayerPosXKey = SavePrefix + "PLAYER_POS_X";
    private const string SavePlayerPosYKey = SavePrefix + "PLAYER_POS_Y";
    private const string SavePlayerPosZKey = SavePrefix + "PLAYER_POS_Z";
    private const string SavePlayerRotXKey = SavePrefix + "PLAYER_ROT_X";
    private const string SavePlayerRotYKey = SavePrefix + "PLAYER_ROT_Y";
    private const string SavePlayerRotZKey = SavePrefix + "PLAYER_ROT_Z";
    private const string SaveARMissionCompletedKey = SavePrefix + "AR_COMPLETED";
    private const string SaveCircuitBreakerFoundKey = SavePrefix + "CB_FOUND";
    private const string SaveCircuitBreakerCompleteKey = SavePrefix + "CB_COMPLETE";
    private const string SaveAppliancesFoundKey = SavePrefix + "APPL_FOUND";
    private const string SaveAppliancesCompleteKey = SavePrefix + "APPL_COMPLETE";
    private const string SaveEvacuationStartedKey = SavePrefix + "EVAC_STARTED";

    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayUIRoot;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;

    [Header("Achievement Banner")]
    [SerializeField] private GameObject achievementBannerRoot;
    [SerializeField] private TMP_Text achievementBannerText;
    [SerializeField] private string achievementBannerMessage = "Achievement Unlocked: Bagged and Ready!";
        [SerializeField] private float achievementBannerSeconds = 2.5f; // Keep the original value
        [Tooltip("Optional: a button on the achievement banner that the player presses to dismiss the banner and continue.")]
        [SerializeField] private Button achievementContinueButton;

    [Header("Circuit Breaker Mission")]
    [SerializeField] private CircuitBreakerQuizUI circuitBreakerQuizUI;
    [SerializeField] private string circuitBreakerFoundMessage = "Good job, {name}! You found the circuit breaker!";
    [SerializeField] private string circuitBreakerAchievementMessage = "Achievement Unlocked: Power Player!";
    [SerializeField] private string nextTaskAfterBreakerMessage = "Now go to the Kitchen area  and find the appliance that is on the floor.";
    [SerializeField] private float circuitBreakerDialogueDuration = 3f;
    [SerializeField] private float delayBeforeQuiz = 0.5f;
    [SerializeField] private bool showNextTaskAfterQuiz = true;

    [Header("Appliances Mission")]
    [SerializeField] private AppliancesQuizUI appliancesQuizUI;
    [SerializeField] private string appliancesFoundMessage = "Great find, {name}! Now let's keep these safe.";
    [SerializeField] private string appliancesAchievementMessage = "Achievement Unlocked: Safety First!";
    [SerializeField] private string nextTaskAfterAppliancesMessage = "";
    [SerializeField] private float appliancesDialogueDuration = 3f;
    [SerializeField] private string levelCompleteBannerMessage = "LEVEL 1 COMPLETE!";

    [Header("Level Complete Decision")]
    [SerializeField] private GameObject levelCompleteDecisionPanel;
    [SerializeField] private Button proceedDuringSceneButton;
    [SerializeField] private Button proceedMainMenuButton;

    [Header("Evacuation Mission (Outside)")]
    [SerializeField] private EvacuationQuizUI evacuationQuizUI;
    [SerializeField] private string evacuationIntroMessage = "Listen... [Sound of heavy rain and wind increasing]. The situation is getting worse. Your local government has just issued an evacuation order. It's time to go.";
    [SerializeField] private string evacuationAchievementMessage = "Achievement Unlocked: Safe Evacuation!";
    [SerializeField] private string evacuationCompleteMessage = "You've successfully evacuated! Stay safe.";
    [SerializeField] private float evacuationDialogueDuration = 4f;

    [Header("Gameplay Floating Dialogue")]
    [SerializeField] private GameObject gameplayDialoguePanel;
    [SerializeField] private TMP_Text gameplayDialogueText;
    [SerializeField] private float gameplayDialogueDuration = 3f;
    [SerializeField] private float delayAfterBanner = 3f;
    [SerializeField] private float delayAfterAchievementBanner = 2f;

    [Header("Player Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform outsideSpawnPoint;
    [SerializeField] private GameObject malePrefab;
    [SerializeField] private GameObject femalePrefab;
    [SerializeField] private bool destroyExistingPlayer = true;
    [SerializeField] private string spawnedPlayerTag = "Player";

    [Header("Stop Player On AR Start")]
    [Tooltip("If your player uses custom movement scripts, drag them here (e.g., PlayerMovement, JoystickController, StarterAssets inputs, etc.). They will be disabled when AR starts.")]
    [SerializeField] private MonoBehaviour[] movementScriptsToDisable;

    [Tooltip("If your Animator uses a Speed float param, it will be set to 0 on AR start.")]
    [SerializeField] private string animatorSpeedParam = "Speed";

    [SerializeField] private bool disableCharacterControllerOnARStart = true;
    [SerializeField] private bool disableNavMeshAgentOnARStart = true;
    [SerializeField] private bool stopRigidbodyOnARStart = true;

    [Header("Joystick Reset")]
    [SerializeField] private GameObject[] joystickObjectsToReset;

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button pauseMainMenuButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Typing")]
    [SerializeField] private bool useTyping = true;
    [SerializeField] private float typeSpeed = 0.03f;

    [Header("Welcome Format")]
    [SerializeField] private string welcomeFormat = "Welcome, {name}! ({gender})";

    [Header("Post-Cutscene Dialogue")]
    [TextArea(3, 8)]
    [SerializeField] private string postCutsceneDialogue =
        "Oh dear! That's a Signal Red warning, {name}. This is serious.";

    [Header("Objective Banner")]
    [SerializeField] private GameObject objectiveBannerRoot;
    [SerializeField] private TMP_Text objectiveBannerText;
    [SerializeField] private string objectiveBannerMessage = "NEW OBJECTIVE: PREPARE FOR THE FLOOD";
    [SerializeField] private float objectiveBannerSeconds = 2f;

    [Header("Cutscene")]
    [SerializeField] private VideoPlayer cutscenePlayer;
    [SerializeField] private GameObject cutsceneUIRoot;

    [Header("Skip Button")]
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject skipButtonRoot;

    [Header("AR Mission")]
    [SerializeField] private GameObject arGameRoot;        // ARGoBag root (AR UI + PlanePlacementAndGameManager)
    [SerializeField] private GameObject arSessionRoot;     // AR Session GameObject (can stay enabled)
    [SerializeField] private GameObject xrOriginRoot;      // XR Origin (Mobile AR) GameObject (can stay enabled)

    [Header("AR Components To Gate Until Trigger")]
    [SerializeField] private Camera arCamera;              // The AR Camera inside XR Origin
    [SerializeField] private AudioListener arAudioListener;
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private ARRaycastManager arRaycastManager;

    [Header("AR Mission Manager (UI Gate)")]
    [Tooltip("Drag the GameObject that has PlanePlacementAndGameManager on it here.")]
    [SerializeField] private PlanePlacementAndGameManager arMissionManager;

    [Header("Non-AR")]
    [SerializeField] private GameObject mainCameraRoot;    // your normal gameplay camera object
    [SerializeField] private GameObject nonARSceneRoot;    // optional: house root etc.

    [Header("Missions & Achievements UI")]
    [SerializeField] private Button missionButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private GameObject pauseButton; // optional: top-left pause button to hide during floating dialogue
    [SerializeField] private GameObject missionsPanelRoot; // panel that lists current missions
    [SerializeField] private TMP_Text missionsPanelText;
    [SerializeField] private GameObject achievementsPanelRoot; // panel that lists achievements
    [SerializeField] private TMP_Text achievementsPanelText;

    [SerializeField] private bool disableGameplayUIWhenARStarts = true;
    [SerializeField] private bool disableGameplayDialogueWhenARStarts = true;

    private enum FlowState
    {
        Welcome,
        PlayingCutscene,
        PostCutsceneDialogue,
        ShowingObjectiveBanner,
        Gameplay
    }

    private FlowState state = FlowState.Welcome;

    private Coroutine typingRoutine;
    private Coroutine gameplayDialogueRoutine;

    // Track per-button visibility so we can restore them after floating dialogue
    private bool missionButtonWasActive;
    private bool achievementsButtonWasActive;
    private bool pauseButtonWasActive;

    private bool isTyping;
    private bool isCutscenePlaying;
    private bool isPaused;
    private float cachedTimeScale = 1f;

    private string currentFullLine = "";
    private string playerName = "";
    private int playerGender = 0;

    private Coroutine firstTaskRoutine;
    private bool firstTaskScheduled;

    // Autosave coroutine handle
    private Coroutine autoSaveRoutine;

    private bool arOpened;
    private bool arMissionCompleted;
    private bool circuitBreakerFound;
    private bool circuitBreakerMissionComplete;
    private bool appliancesMissionFound;
    private bool appliancesUnlocked;
    private bool appliancesMissionCompleted;
    private bool evacuationMissionStarted;
    private bool evacuationIntroShown = false;

    // ✅ Track the spawned player so we can stop/resume them later
    private GameObject spawnedPlayerInstance;
    // Cached spawned player components to avoid repeated GetComponent calls
    private CharacterController spawnedPlayerCC;
    private NavMeshAgent spawnedPlayerAgent;
    private Rigidbody spawnedPlayerRigidbody;
    private Animator spawnedPlayerAnimator;

    // Manager component references (skeletons)
    private CutsceneDialogueManager cutsceneManager;
    private DialogueManager dialogueManager;
    private BannerManager bannerManager;
    private PlayerSpawner playerSpawner;
    private PlayerMovementController playerMovementController;
    private ARController arController;
    private MissionManager missionManager;
    private PauseController pauseController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        nextButton?.onClick.AddListener(OnNextPressed);
        skipButton?.onClick.AddListener(SkipCutscene);

        if (pauseResumeButton != null)
            pauseResumeButton.onClick.AddListener(ResumeGame);

        if (pauseMainMenuButton != null)
            pauseMainMenuButton.onClick.AddListener(GoToMainMenu);

        // Missions / Achievements button wiring
        if (missionButton != null)
            missionButton.onClick.AddListener(ToggleMissionsPanel);

        if (achievementsButton != null)
            achievementsButton.onClick.AddListener(ToggleAchievementsPanel);

        // Ensure mission/achievement panels are hidden at start
        if (missionsPanelRoot != null) missionsPanelRoot.SetActive(false);
        if (achievementsPanelRoot != null) achievementsPanelRoot.SetActive(false);

        // Ensure decision panel hidden and buttons wired
        if (levelCompleteDecisionPanel != null) levelCompleteDecisionPanel.SetActive(false);
        if (proceedDuringSceneButton != null)
        {
            proceedDuringSceneButton.onClick.RemoveAllListeners();
            proceedDuringSceneButton.onClick.AddListener(OnProceedDuringSceneClicked);
        }
        if (proceedMainMenuButton != null)
        {
            proceedMainMenuButton.onClick.RemoveAllListeners();
            proceedMainMenuButton.onClick.AddListener(OnProceedToMainMenuClicked);
        }

        gameplayUIRoot?.SetActive(false);
        gameplayDialoguePanel?.SetActive(false);
        objectiveBannerRoot?.SetActive(false);
        cutsceneUIRoot?.SetActive(false);
        ShowSkip(false);

        // ✅ Keep AR Session / XR Origin enabled (XR Simulation stability), but DO NOT let AR render or detect until trigger hits.
        // Create/get manager component skeletons and wire them to preserve inspector compatibility.
        cutsceneManager = GetComponent<CutsceneDialogueManager>(); if (cutsceneManager == null) cutsceneManager = gameObject.AddComponent<CutsceneDialogueManager>();
        dialogueManager = GetComponent<DialogueManager>(); if (dialogueManager == null) dialogueManager = gameObject.AddComponent<DialogueManager>();
        bannerManager = GetComponent<BannerManager>(); if (bannerManager == null) bannerManager = gameObject.AddComponent<BannerManager>();
        playerSpawner = GetComponent<PlayerSpawner>(); if (playerSpawner == null) playerSpawner = gameObject.AddComponent<PlayerSpawner>();
        playerMovementController = GetComponent<PlayerMovementController>(); if (playerMovementController == null) playerMovementController = gameObject.AddComponent<PlayerMovementController>();
        arController = GetComponent<ARController>(); if (arController == null) arController = gameObject.AddComponent<ARController>();
        missionManager = GetComponent<MissionManager>(); if (missionManager == null) missionManager = gameObject.AddComponent<MissionManager>();
        pauseController = GetComponent<PauseController>(); if (pauseController == null) pauseController = gameObject.AddComponent<PauseController>();

        // Wire ARController with serialized AR components so its Start/Stop methods operate on the same objects
        if (arController != null)
        {
            arController.arCamera = arCamera;
            arController.arAudioListener = arAudioListener;
            arController.arPlaneManager = arPlaneManager;
            arController.arRaycastManager = arRaycastManager;
            arController.arGameRoot = arGameRoot;
        }

        // Wire BannerManager with existing serialized banner fields
        if (bannerManager != null)
            bannerManager.Initialize(objectiveBannerRoot, objectiveBannerText, achievementBannerRoot, achievementBannerText);

        // Gate AR until trigger using ARController if available
        if (arController != null)
            arController.GateARUntilTrigger();
        else
            GateARUntilTrigger();

        // Ensure AR UI is OFF at boot (even if AR manager object is enabled in hierarchy)
        if (arMissionManager != null)
            arMissionManager.SetARUIActivePublic(false);
    }

    private void GateARUntilTrigger()
    {
        // Prefer ARController if available
        if (arController != null)
        {
            arController.GateARUntilTrigger();
            return;
        }

        // AR gameplay root OFF
        if (arGameRoot != null) arGameRoot.SetActive(false);

        // AR camera OFF (rendering + audio)
        if (arCamera != null)
        {
            arCamera.enabled = false;
            arCamera.gameObject.SetActive(false);
            Debug.Log("[BeforeSceneManager] AR camera deactivated (GateARUntilTrigger).");
        }
        if (arAudioListener != null) arAudioListener.enabled = false;

        // AR detection OFF (so nothing runs until trigger)
        if (arPlaneManager != null) arPlaneManager.enabled = false;
        if (arRaycastManager != null) arRaycastManager.enabled = false;
    }

    private void Start()
    {
        LoadFromPrefs();
        // Load saved gameplay state (missions, player transform)
        LoadGameState();
        // If a saved gameplay state exists, resume directly into gameplay
        bool hasSavedPlayer = PlayerPrefs.HasKey(SavePlayerPosXKey);
        bool anyMissionProgress = arMissionCompleted || circuitBreakerFound || circuitBreakerMissionComplete || appliancesMissionFound || appliancesMissionCompleted || evacuationMissionStarted;

        if (hasSavedPlayer || anyMissionProgress)
        {
            // Skip welcome/cutscene and go to gameplay
            StartGameplay();
        }
        else
        {
            ShowWelcome();
        }
    }

    private void LoadFromPrefs()
    {
        playerName = PlayerPrefs.GetString(PlayerNameKey, "");
        playerGender = PlayerPrefs.GetInt(PlayerGenderKey, 0);
    }

    private string ResolveTokens(string text)
    {
        string nameSafe = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
        string genderText = playerGender == 1 ? "Male" : playerGender == 2 ? "Female" : "Unspecified";

        return (text ?? "")
            .Replace("{name}", nameSafe)
            .Replace("{gender}", genderText);
    }

    private void OnNextPressed()
    {
        if (isTyping)
        {
            StopTypingAndShowFull();
            return;
        }

        if (state == FlowState.Welcome)
            PlayCutscene();
        else if (state == FlowState.PostCutsceneDialogue)
            StartObjectiveBanner();
    }

    private void ShowWelcome()
    {
        dialoguePanel?.SetActive(true);
        ShowDialogueLine(ResolveTokens(welcomeFormat));
    }

    private void ShowPostCutsceneDialogue()
    {
        dialoguePanel?.SetActive(true);
        ShowDialogueLine(ResolveTokens(postCutsceneDialogue));
        state = FlowState.PostCutsceneDialogue;
    }

    private void ShowDialogueLine(string line)
    {
        currentFullLine = line;

        if (!useTyping)
        {
            dialogueText.text = line;
            return;
        }

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;

        // Use TextMeshPro's maxVisibleCharacters to avoid string concatenation allocations.
        dialogueText.text = line ?? "";
        dialogueText.maxVisibleCharacters = 0;

        int total = dialogueText.text.Length;
        for (int i = 0; i < total; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;
            yield return new WaitForSeconds(typeSpeed);
        }

        // Ensure full text is visible at the end
        dialogueText.maxVisibleCharacters = total;
        isTyping = false;
    }

    private void StopTypingAndShowFull()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        dialogueText.text = currentFullLine;
        isTyping = false;
    }

    private void PlayCutscene()
    {
        dialoguePanel?.SetActive(false);
        cutsceneUIRoot?.SetActive(true);
        ShowSkip(true);

        isCutscenePlaying = true;
        state = FlowState.PlayingCutscene;

        cutscenePlayer?.Play();
    }

    private void SkipCutscene()
    {
        if (!isCutscenePlaying) return;

        cutscenePlayer?.Stop();
        FinishCutscene();
    }

    private void FinishCutscene()
    {
        isCutscenePlaying = false;
        cutsceneUIRoot?.SetActive(false);
        ShowSkip(false);
        ShowPostCutsceneDialogue();
    }

    private void StartObjectiveBanner()
    {
        dialoguePanel?.SetActive(false);
        objectiveBannerRoot?.SetActive(true);
        objectiveBannerText.text = objectiveBannerMessage;

        StartCoroutine(ObjectiveBannerRoutine());
    }

    private IEnumerator ObjectiveBannerRoutine()
    {
        yield return new WaitForSeconds(objectiveBannerSeconds);
        objectiveBannerRoot?.SetActive(false);
        StartGameplay();
    }

    private void StartGameplay()
    {
        state = FlowState.Gameplay;

        gameplayUIRoot?.SetActive(true);
        SpawnPlayerByGender();

        ScheduleFirstTaskLine();

        // start periodic autosave while playing
        if (autoSaveRoutine == null)
            autoSaveRoutine = StartCoroutine(PeriodicAutoSave());
    }

    private IEnumerator PeriodicAutoSave()
    {
        const float interval = 10f; // seconds
        while (true)
        {
            yield return new WaitForSeconds(interval);
            SaveGameState();
        }
    }

    private void StopAutoSave()
    {
        if (autoSaveRoutine != null)
        {
            StopCoroutine(autoSaveRoutine);
            autoSaveRoutine = null;
        }
    }

    private void ScheduleFirstTaskLine()
    {
        // If we've already scheduled the first-task hint, skip
        if (firstTaskScheduled) return;

        // If player has mission progress (resumed), do not show the initial "find backpack" hint.
        bool anyMissionProgress = arMissionCompleted || circuitBreakerFound || circuitBreakerMissionComplete || appliancesMissionFound || appliancesMissionCompleted || evacuationMissionStarted;
        if (anyMissionProgress) return;

        firstTaskScheduled = true;

        if (firstTaskRoutine != null)
            StopCoroutine(firstTaskRoutine);

        firstTaskRoutine = StartCoroutine(DelayedGameplayDialogue());
    }

    private IEnumerator DelayedGameplayDialogue()
    {
        yield return new WaitForSeconds(delayAfterBanner);
        // Show a context-aware next-task hint (keeps behavior consistent after refactor)
        string hint = GetNextMissionHint();
        if (!string.IsNullOrEmpty(hint))
            ShowGameplayDialogue(hint);
    }

    // Returns a short, resolved hint for the next actionable mission (used by floating dialogue)
    private string GetNextMissionHint()
    {
        if (!arMissionCompleted)
            return "Start the AR GoBag mission to collect the go-bag. Approach the GoBag station and follow the AR prompts.";

        if (!circuitBreakerFound)
            return "Now your first task is to find the table that has the backpack.";

        if (!circuitBreakerMissionComplete)
            return "Complete the circuit breaker quiz to unlock the next task.";

        if (!appliancesMissionFound)
            return string.IsNullOrEmpty(nextTaskAfterBreakerMessage) ? "Find the appliance that is on the floor in the Kitchen area." : ResolveTokens(nextTaskAfterBreakerMessage);

        if (!appliancesMissionCompleted)
            return "Complete the appliances quiz to secure the appliance.";

        if (!evacuationMissionStarted)
            return string.IsNullOrEmpty(evacuationIntroMessage) ? "Go outside to begin the evacuation mission." : ResolveTokens(evacuationIntroMessage);

        return "";
    }

    public void ShowGameplayDialogue(string line)
    {
        if (gameplayDialogueRoutine != null)
            StopCoroutine(gameplayDialogueRoutine);

        // Hide only the top buttons (mission, achievements, pause) to avoid overlapping the floating dialogue.
        // When `gameplayUIRoot` is inactive (e.g., during a quiz), the buttons will report inactive.
        // In that case assume they *should* be visible when gameplay UI is restored, so mark them
        // as previously active so we re-enable them after the floating dialogue completes.
        if (missionButton != null)
        {
            missionButtonWasActive = (gameplayUIRoot != null && !gameplayUIRoot.activeInHierarchy) ? true : missionButton.gameObject.activeSelf;
            if (missionButtonWasActive) missionButton.gameObject.SetActive(false);
        }

        if (achievementsButton != null)
        {
            achievementsButtonWasActive = (gameplayUIRoot != null && !gameplayUIRoot.activeInHierarchy) ? true : achievementsButton.gameObject.activeSelf;
            if (achievementsButtonWasActive) achievementsButton.gameObject.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButtonWasActive = (gameplayUIRoot != null && !gameplayUIRoot.activeInHierarchy) ? true : pauseButton.activeSelf;
            if (pauseButtonWasActive) pauseButton.SetActive(false);
        }

        gameplayDialogueRoutine = StartCoroutine(GameplayDialogueRoutine(line));
    }

    private IEnumerator GameplayDialogueRoutine(string line)
    {
        gameplayDialoguePanel.SetActive(true);
        gameplayDialogueText.text = ResolveTokens(line);

        yield return new WaitForSeconds(gameplayDialogueDuration);

        gameplayDialoguePanel.SetActive(false);

        // Restore only those top buttons we hid earlier
        if (missionButton != null && missionButtonWasActive)
        {
            missionButton.gameObject.SetActive(true);
            missionButtonWasActive = false;
        }

        if (achievementsButton != null && achievementsButtonWasActive)
        {
            achievementsButton.gameObject.SetActive(true);
            achievementsButtonWasActive = false;
        }

        if (pauseButton != null && pauseButtonWasActive)
        {
            pauseButton.SetActive(true);
            pauseButtonWasActive = false;
        }
    }

    private void ShowSkip(bool show)
    {
        skipButtonRoot?.SetActive(show);
    }

    // --- Missions & Achievements UI ---
    public void ToggleMissionsPanel()
    {
        if (missionsPanelRoot == null) return;
        bool isActive = missionsPanelRoot.activeSelf;
        if (!isActive) PopulateMissionsPanel();
        missionsPanelRoot.SetActive(!isActive);
    }

    public void ToggleAchievementsPanel()
    {
        if (achievementsPanelRoot == null) return;
        bool isActive = achievementsPanelRoot.activeSelf;
        if (!isActive) PopulateAchievementsPanel();
        achievementsPanelRoot.SetActive(!isActive);
    }

    private void PopulateMissionsPanel()
    {
        if (missionsPanelText == null) return;
        // Show only the single next actionable mission (priority order: AR -> Find breaker -> Finish breaker quiz -> Find appliances -> Finish appliances quiz -> Evacuation)
        string title = "Current Mission";
        string body = "";

        if (!arMissionCompleted)
        {
            title = "AR Mission";
            body = "Start the AR GoBag mission to collect the go-bag. Approach the GoBag station and follow the AR prompts.";
        }
        else if (!circuitBreakerFound)
        {
            title = "Circuit Breaker";
            body = "Find the circuit breaker near the door.";
        }
        else if (!circuitBreakerMissionComplete)
        {
            title = "Circuit Breaker";
            body = "Complete the circuit breaker quiz to unlock the next task.";
        }
        else if (!appliancesMissionFound)
        {
            title = "Appliances";
            // prefer inspector-configured follow-up message if provided, otherwise give a clear instruction
            body = string.IsNullOrEmpty(nextTaskAfterBreakerMessage) ? "Find the appliance that is on the floor in the Kitchen area." : ResolveTokens(nextTaskAfterBreakerMessage);
        }
        else if (!appliancesMissionCompleted)
        {
            title = "Appliances";
            body = "Complete the appliances quiz to secure the appliance.";
        }
        else if (!evacuationMissionStarted)
        {
            title = "Evacuation";
            body = string.IsNullOrEmpty(evacuationIntroMessage) ? "Go outside to begin the evacuation mission." : ResolveTokens(evacuationIntroMessage);
        }
        else
        {
            title = "All Missions";
            body = "All missions completed.";
        }

        // Build final display
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(title + ":");
        sb.AppendLine();
        sb.AppendLine(ResolveTokens(body));

        missionsPanelText.text = sb.ToString();
    }

    private void PopulateAchievementsPanel()
    {
        if (achievementsPanelText == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Achievements:");
        sb.AppendLine();

        if (arMissionCompleted) sb.AppendLine("- AR Mission Completed");
        if (circuitBreakerMissionComplete) sb.AppendLine("- " + ResolveTokens(circuitBreakerAchievementMessage));
        if (appliancesMissionCompleted) sb.AppendLine("- " + ResolveTokens(appliancesAchievementMessage));
        if (appliancesMissionCompleted && circuitBreakerMissionComplete && arMissionCompleted) sb.AppendLine("- " + ResolveTokens(levelCompleteBannerMessage));

        if (sb.Length <= 20)
            sb.AppendLine("No achievements yet. Keep playing!");

        achievementsPanelText.text = sb.ToString();
    }

    private void SpawnPlayerByGender()
    {
        GameObject prefab = playerGender == 2 ? femalePrefab : malePrefab;

        if (prefab == null)
        {
            Debug.LogError("[BeforeSceneManager] Spawn prefab is not assigned for selected gender.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[BeforeSceneManager] Spawn point is not assigned.");
            spawnedPlayerInstance = Instantiate(prefab);
            return;
        }

        if (destroyExistingPlayer)
        {
            GameObject existing = GameObject.FindGameObjectWithTag(spawnedPlayerTag);
            if (existing != null) Destroy(existing);
        }

        // Instantiate first (at spawn point as a best-effort)
        spawnedPlayerInstance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        // We'll compute a corrected world position that accounts for CharacterController center/height
        Vector3 correctedPosition = spawnPoint.position;

        // If there's a CharacterController, adjust so the bottom of the capsule sits at the spawn
        CharacterController cc = spawnedPlayerInstance.GetComponent<CharacterController>();
        if (cc != null)
        {
            // Compute world offset of the controller center
            Vector3 worldCenterOffset = spawnedPlayerInstance.transform.TransformVector(cc.center);
            float halfHeight = cc.height * 0.5f;

            // Bottom offset = center (in world) minus halfHeight upward
            Vector3 bottomOffset = worldCenterOffset - Vector3.up * halfHeight;

            // Move the transform so the bottom of the capsule aligns with spawn point
            correctedPosition = spawnPoint.position - bottomOffset;

            // Temporarily disable controller while repositioning
            cc.enabled = false;
            spawnedPlayerInstance.transform.SetPositionAndRotation(correctedPosition, spawnPoint.rotation);
            cc.enabled = true;
        }
        else
        {
            // No character controller: force transform directly
            spawnedPlayerInstance.transform.SetPositionAndRotation(correctedPosition, spawnPoint.rotation);
        }

        // Cache frequently used components to avoid repeated GetComponent calls
        spawnedPlayerAgent = spawnedPlayerInstance.GetComponent<NavMeshAgent>();
        if (spawnedPlayerAgent != null)
        {
            spawnedPlayerAgent.ResetPath();
            Vector3 agentWarpPos = correctedPosition - Vector3.up * spawnedPlayerAgent.baseOffset;
            spawnedPlayerAgent.Warp(agentWarpPos);
            spawnedPlayerAgent.updatePosition = true;
        }

        spawnedPlayerRigidbody = spawnedPlayerInstance.GetComponent<Rigidbody>();
        if (spawnedPlayerRigidbody != null)
        {
            // Avoid setting velocity on kinematic bodies (Unity warns about this).
            if (!spawnedPlayerRigidbody.isKinematic)
            {
                spawnedPlayerRigidbody.velocity = Vector3.zero;
                spawnedPlayerRigidbody.angularVelocity = Vector3.zero;
            }

            // Setting position is safe; use it to place the rigidbody at spawn.
            spawnedPlayerRigidbody.position = correctedPosition;
        }

        spawnedPlayerAnimator = spawnedPlayerInstance.GetComponentInChildren<Animator>();
        if (spawnedPlayerAnimator != null)
        {
            spawnedPlayerAnimator.applyRootMotion = false;
        }

        if (!string.IsNullOrEmpty(spawnedPlayerTag))
        {
            spawnedPlayerInstance.tag = spawnedPlayerTag;
        }

        // Ensure animator speed is set if present
        // Set animator speed if present
        spawnedPlayerCC = spawnedPlayerInstance.GetComponent<CharacterController>();
        if (spawnedPlayerAnimator != null && !string.IsNullOrEmpty(animatorSpeedParam))
        {
            if (HasAnimatorParam(spawnedPlayerAnimator, animatorSpeedParam))
                spawnedPlayerAnimator.SetFloat(animatorSpeedParam, 0f);
        }

        // Apply saved player transform if available
        ApplySavedPlayerTransformIfAny();
    }

    private void StopPlayerMovementForAR()
    {
        GameObject player = spawnedPlayerInstance != null
            ? spawnedPlayerInstance
            : GameObject.FindGameObjectWithTag(spawnedPlayerTag);

        if (player == null) return;
        // Disable custom movement scripts (use cached array if possible)
        if (movementScriptsToDisable != null)
        {
            for (int i = 0; i < movementScriptsToDisable.Length; i++)
            {
                if (movementScriptsToDisable[i] != null)
                    movementScriptsToDisable[i].enabled = false;
            }
        }

        // CharacterController
        CharacterController cc = (player == spawnedPlayerInstance && spawnedPlayerCC != null) ? spawnedPlayerCC : player.GetComponent<CharacterController>();
        if (disableCharacterControllerOnARStart && cc != null) cc.enabled = false;

        // NavMeshAgent
        NavMeshAgent agent = (player == spawnedPlayerInstance && spawnedPlayerAgent != null) ? spawnedPlayerAgent : player.GetComponent<NavMeshAgent>();
        if (disableNavMeshAgentOnARStart && agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        // Rigidbody
        Rigidbody rb = (player == spawnedPlayerInstance && spawnedPlayerRigidbody != null) ? spawnedPlayerRigidbody : player.GetComponent<Rigidbody>();
        if (stopRigidbodyOnARStart && rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        // Animator speed
        Animator anim = (player == spawnedPlayerInstance && spawnedPlayerAnimator != null) ? spawnedPlayerAnimator : player.GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrEmpty(animatorSpeedParam))
        {
            if (HasAnimatorParam(anim, animatorSpeedParam))
                anim.SetFloat(animatorSpeedParam, 0f);
        }
    }

    // ✅ NEW: Resume movement after AR is done
    private void ResumePlayerMovementAfterAR()
    {
        GameObject player = spawnedPlayerInstance != null
            ? spawnedPlayerInstance
            : GameObject.FindGameObjectWithTag(spawnedPlayerTag);

        if (player == null) return;

        // Re-enable custom movement scripts
        if (movementScriptsToDisable != null)
        {
            for (int i = 0; i < movementScriptsToDisable.Length; i++)
            {
                if (movementScriptsToDisable[i] != null)
                    movementScriptsToDisable[i].enabled = true;
            }
        }

        // CharacterController
        CharacterController cc = (player == spawnedPlayerInstance && spawnedPlayerCC != null) ? spawnedPlayerCC : player.GetComponent<CharacterController>();
        if (disableCharacterControllerOnARStart && cc != null) cc.enabled = true;

        // NavMeshAgent
        NavMeshAgent agent = (player == spawnedPlayerInstance && spawnedPlayerAgent != null) ? spawnedPlayerAgent : player.GetComponent<NavMeshAgent>();
        if (disableNavMeshAgentOnARStart && agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        // Wake Rigidbody (if slept)
        Rigidbody rb = (player == spawnedPlayerInstance && spawnedPlayerRigidbody != null) ? spawnedPlayerRigidbody : player.GetComponent<Rigidbody>();
        if (stopRigidbodyOnARStart && rb != null)
        {
            rb.WakeUp();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;
        cachedTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;

        StopPlayerMovementForAR();

        ShowPausePanel(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        Time.timeScale = cachedTimeScale <= 0f ? 1f : cachedTimeScale;
        isPaused = false;

        ResumePlayerMovementAfterAR();

        ShowPausePanel(false);
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    private void ShowPausePanel(bool show)
    {
        if (pausePanel != null)
            pausePanel.SetActive(show);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        // Ensure progress is saved before leaving to main menu
        SaveGameState();
        StopAutoSave();

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("[BeforeSceneManager] Main menu scene name is empty.");
        }
    }

    private void ResetJoysticks()
    {
        if (joystickObjectsToReset == null || joystickObjectsToReset.Length == 0)
            return;

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null)
            return;

        // Create one PointerEventData and reuse it for all joysticks to reduce allocations
        var pointerEventData = new PointerEventData(currentEventSystem);
        for (int i = 0; i < joystickObjectsToReset.Length; i++)
        {
            GameObject joystickObj = joystickObjectsToReset[i];
            if (joystickObj == null) continue;
            ExecuteEvents.Execute<IPointerUpHandler>(joystickObj, pointerEventData,
                (handler, data) => handler.OnPointerUp(pointerEventData));
        }
    }

    private bool HasAnimatorParam(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;

        for (int i = 0; i < animator.parameters.Length; i++)
        {
            if (animator.parameters[i].name == paramName)
                return true;
        }
        return false;
    }

    // ✅ Trigger calls this
    public void StartARGameFromTrigger()
    {
        if (arOpened || arMissionCompleted) return;
        arOpened = true;

        Debug.Log("[BeforeSceneManager] Trigger hit -> switching to AR.");

        StopPlayerMovementForAR();

        ResetJoysticks();

        // Turn OFF non-AR rendering
        if (mainCameraRoot != null) mainCameraRoot.SetActive(false);
        if (nonARSceneRoot != null) nonARSceneRoot.SetActive(false);

        // Turn ON AR rendering + detection via ARController when available
        if (arController != null)
        {
            arController.StartAR();
        }
        else
        {
            if (arCamera != null)
            {
                arCamera.gameObject.SetActive(true);
                arCamera.enabled = true;
                Debug.Log("[BeforeSceneManager] AR camera activated (StartARGameFromTrigger).");
            }
            if (arAudioListener != null) arAudioListener.enabled = true;

            if (arPlaneManager != null) arPlaneManager.enabled = true;
            if (arRaycastManager != null) arRaycastManager.enabled = true;
        }

        // Turn ON AR gameplay/UI root
        if (arGameRoot != null) arGameRoot.SetActive(true);

        // ✅ Force AR UI ON (does not rely on OnEnable timing)
        if (arMissionManager != null)
            arMissionManager.SetARUIActivePublic(true);

        if (disableGameplayUIWhenARStarts && gameplayUIRoot != null)
            gameplayUIRoot.SetActive(false);

        if (disableGameplayDialogueWhenARStarts && gameplayDialoguePanel != null)
            gameplayDialoguePanel.SetActive(false);
    }

    public void ReturnFromARAndShowAchievement()
    {
        arMissionCompleted = true;

        // persist immediately
        SaveGameState();


        // Decision handlers are defined at class scope (moved to below OnApplicationQuit)
        // Turn OFF AR gameplay/UI
        if (arMissionManager != null)
            arMissionManager.SetARUIActivePublic(false);

        if (arGameRoot != null) arGameRoot.SetActive(false);

        // Turn OFF AR detection + rendering
        if (arController != null)
        {
            arController.StopAR();
        }
        else
        {
            if (arPlaneManager != null) arPlaneManager.enabled = false;
            if (arRaycastManager != null) arRaycastManager.enabled = false;

            if (arCamera != null)
            {
                arCamera.enabled = false;
                arCamera.gameObject.SetActive(false);
                Debug.Log("[BeforeSceneManager] AR camera deactivated (ReturnFromARAndShowAchievement).");
            }
            if (arAudioListener != null) arAudioListener.enabled = false;
        }

        // Turn ON non-AR rendering
        if (mainCameraRoot != null) mainCameraRoot.SetActive(true);
        if (nonARSceneRoot != null) nonARSceneRoot.SetActive(true);

        // Optionally bring back gameplay UI
        if (gameplayUIRoot != null) gameplayUIRoot.SetActive(true);

        // Show achievement banner
        ShowAchievementBanner();
    }

    private void ShowAchievementBanner()
    {
        if (achievementBannerRoot == null || achievementBannerText == null) return;

        achievementBannerText.text = ResolveTokens(achievementBannerMessage);
        achievementBannerRoot.SetActive(true);

        StartCoroutine(AchievementBannerRoutine());
    }

    private IEnumerator AchievementBannerRoutine()
    {
            // Wait until player presses the continue button (or fallback to auto-close after achievementBannerSeconds)
            yield return StartCoroutine(WaitForAchievementBannerClickWithTimeout());

            // ✅ Player can walk again after the banner is dismissed
            ResumePlayerMovementAfterAR();

            // Wait a moment before showing the follow-up hint
            yield return new WaitForSeconds(delayAfterAchievementBanner);

            // Show the next gameplay hint after the achievement banner
            ShowGameplayDialogue("Now your next  task is to find the cuircuit breaker");

            // Clarify the location after the first hint finishes showing
            yield return new WaitForSeconds(gameplayDialogueDuration);
            ShowGameplayDialogue("It's beside the door.");
    }

        private bool achievementContinueClicked;

        private void OnAchievementContinuePressed()
        {
            achievementContinueClicked = true;
        }

        private IEnumerator WaitForAchievementBannerClickWithTimeout()
        {
            if (achievementBannerRoot == null || achievementBannerText == null)
                yield break;

            achievementContinueClicked = false;

            if (achievementContinueButton != null)
            {
                achievementContinueButton.onClick.RemoveListener(OnAchievementContinuePressed);
                achievementContinueButton.onClick.AddListener(OnAchievementContinuePressed);
                achievementContinueButton.gameObject.SetActive(true);
            }

            // Wait indefinitely until the player clicks the continue button.
            // If no button is assigned, accept any screen tap/click as the proceed action.
            while (!achievementContinueClicked)
            {
                if (achievementContinueButton == null)
                {
                    if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
                    {
                        achievementContinueClicked = true;
                        break;
                    }
                }

                yield return null;
            }

            if (achievementContinueButton != null)
            {
                achievementContinueButton.onClick.RemoveListener(OnAchievementContinuePressed);
                achievementContinueButton.gameObject.SetActive(false);
            }

            if (achievementBannerRoot != null)
                achievementBannerRoot.SetActive(false);
        }

    // ✅ PUBLIC: Call this when player enters the circuit breaker trigger
    public void OnCircuitBreakerFound()
    {
        // Gate: only allow after AR mission completed
        if (!arMissionCompleted)
        {
            Debug.Log("[BeforeSceneManager] Circuit breaker mission locked until AR mission is completed.");
            return;
        }

        if (circuitBreakerFound) return;
        circuitBreakerFound = true;

        // persist progress
        SaveGameState();

        Debug.Log("[BeforeSceneManager] Circuit breaker found!");

        // Stop player movement
        StopPlayerMovementForAR();
        ResetJoysticks();

        // (no teleport here) keep player inside until they actually exit via the outside trigger

        // (No teleport here) — player should only be teleported when they actually go outside or choose to proceed.


        // ✅ Disable gameplay UI (joystick, etc.)
        if (gameplayUIRoot != null)
        {
            gameplayUIRoot.SetActive(false);
            Debug.Log("[BeforeSceneManager] Gameplay UI disabled for circuit breaker mission");
        }

        // Start the sequence: dialogue → quiz → achievement → next task
        StartCoroutine(CircuitBreakerMissionSequence());
    }

    private IEnumerator CircuitBreakerMissionSequence()
    {
        // Step 1: Show "Good job! You found the breaker" dialogue
        ShowGameplayDialogue(ResolveTokens(circuitBreakerFoundMessage));
        yield return new WaitForSeconds(circuitBreakerDialogueDuration);

        // Small delay before quiz
        yield return new WaitForSeconds(delayBeforeQuiz);

        // Step 2: Show the quiz UI
        if (circuitBreakerQuizUI != null)
        {
            Debug.Log("[BeforeSceneManager] Showing quiz UI...");
            circuitBreakerQuizUI.ShowQuiz();
        }
        else
        {
            Debug.LogError("[BeforeSceneManager] ❌ CircuitBreakerQuizUI is not assigned!");
            // Fallback: continue to achievement
            StartCoroutine(CircuitBreakerFoundSequence());
        }

        // (Quiz completion will call OnCircuitBreakerQuizCompleted)
    }

    // ✅ PUBLIC: Called by CircuitBreakerQuizUI when quiz is completed
    public void OnCircuitBreakerQuizCompleted(bool success)
    {
        if (!success)
        {
            Debug.LogWarning("[BeforeSceneManager] Quiz failed, but allowing continuation.");
        }

        // Show success dialogue and achievement
        StartCoroutine(CircuitBreakerFoundSequence());
    }

    private IEnumerator CircuitBreakerFoundSequence()
    {
        // Mark circuit breaker mission complete and unlock next mission
        circuitBreakerMissionComplete = true;
        appliancesUnlocked = true;

        // persist progress
        SaveGameState();

        // Show achievement banner
        if (achievementBannerRoot != null && achievementBannerText != null)
        {
            achievementBannerText.text = ResolveTokens(circuitBreakerAchievementMessage);
            achievementBannerRoot.SetActive(true);
            yield return StartCoroutine(WaitForAchievementBannerClickWithTimeout());
        }

        // ✅ Reset joysticks before re-enabling UI
        ResetJoysticks();

        // ✅ Re-enable gameplay UI after quiz completion
        if (gameplayUIRoot != null)
        {
            gameplayUIRoot.SetActive(true);
            Debug.Log("[BeforeSceneManager] Gameplay UI re-enabled after quiz");
        }

        // Resume player movement
        ResumePlayerMovementAfterAR();

        // Small delay before next instruction
        yield return new WaitForSeconds(delayAfterAchievementBanner);

        // Show next task (optional)
        if (showNextTaskAfterQuiz && !string.IsNullOrEmpty(nextTaskAfterBreakerMessage))
        {
            ShowGameplayDialogue(ResolveTokens(nextTaskAfterBreakerMessage));
        }
    }

    // ✅ PUBLIC: Call this when player enters the appliances mission trigger
    public void OnAppliancesMissionFound()
    {
        // Gate: only allow after circuit breaker mission completed/unlocked
        if (!appliancesUnlocked)
        {
            Debug.Log("[BeforeSceneManager] Appliances mission is locked until circuit breaker mission completes.");
            return;
        }

        if (appliancesMissionCompleted)
        {
            Debug.Log("[BeforeSceneManager] Appliances mission already completed.");
            return;
        }

        if (appliancesMissionFound) return;
        appliancesMissionFound = true;

        // persist progress
        SaveGameState();

        Debug.Log("[BeforeSceneManager] Appliances mission triggered!");

        // Stop player movement
        StopPlayerMovementForAR();
        ResetJoysticks();

        // Disable gameplay UI (joystick, etc.)
        if (gameplayUIRoot != null)
        {
            gameplayUIRoot.SetActive(false);
            Debug.Log("[BeforeSceneManager] Gameplay UI disabled for appliances mission");
        }

        // Start the sequence: dialogue → quiz → achievement → next task
        StartCoroutine(AppliancesMissionSequence());
    }

    private IEnumerator AppliancesMissionSequence()
    {
        // Step 1: Show "Great find!" dialogue
        ShowGameplayDialogue(ResolveTokens(appliancesFoundMessage));
        yield return new WaitForSeconds(appliancesDialogueDuration);

        // Small delay before quiz
        yield return new WaitForSeconds(delayBeforeQuiz);

        // Step 2: Show the quiz UI
        if (appliancesQuizUI != null)
        {
            Debug.Log("[BeforeSceneManager] Showing appliances quiz UI...");
            appliancesQuizUI.ShowQuiz();
        }
        else
        {
            Debug.LogError("[BeforeSceneManager] ❌ AppliancesQuizUI is not assigned!");
            // Fallback: continue to achievement
            StartCoroutine(AppliancesFoundSequence());
        }

        // (Quiz completion will call OnAppliancesQuizCompleted)
    }

    // ✅ PUBLIC: Called by AppliancesQuizUI when quiz is completed
    public void OnAppliancesQuizCompleted(bool success)
    {
        if (!success)
        {
            Debug.LogWarning("[BeforeSceneManager] Quiz failed, but allowing continuation.");
        }

        // Show success dialogue and achievement
        StartCoroutine(AppliancesFoundSequence());
    }

    private IEnumerator AppliancesFoundSequence()
    {
        appliancesMissionCompleted = true;

        // persist progress
        SaveGameState();

        // Show achievement banner
        if (achievementBannerRoot != null && achievementBannerText != null)
        {
            // Use the configured appliances achievement message (keeps inspector-configurable)
            achievementBannerText.text = ResolveTokens(appliancesAchievementMessage);
            achievementBannerRoot.SetActive(true);
            yield return StartCoroutine(WaitForAchievementBannerClickWithTimeout());
        }

        // After the achievement banner, show the evacuation floating dialogue (only once)
        if (!evacuationIntroShown)
        {
            ShowGameplayDialogue(ResolveTokens(evacuationIntroMessage));
            evacuationIntroShown = true;
            yield return new WaitForSeconds(evacuationDialogueDuration);
        }

        // Reset joysticks before re-enabling UI
        ResetJoysticks();

        // Re-enable gameplay UI
        if (gameplayUIRoot != null)
        {
            gameplayUIRoot.SetActive(true);
            Debug.Log("[BeforeSceneManager] Gameplay UI re-enabled after appliances quiz");
        }

        // Resume player movement
        ResumePlayerMovementAfterAR();

        // Small delay before next instruction
        yield return new WaitForSeconds(delayAfterAchievementBanner);

        // Optionally show the configured completion message (kept for compatibility)
        if (!string.IsNullOrEmpty(nextTaskAfterAppliancesMessage))
        {
            ShowGameplayDialogue(ResolveTokens(nextTaskAfterAppliancesMessage));
            yield return new WaitForSeconds(appliancesDialogueDuration);
        }

        // TODO: Trigger next mission or game progression
    }

    public bool CanStartEvacuation()
    {
        return arMissionCompleted && circuitBreakerMissionComplete && appliancesMissionCompleted && !evacuationMissionStarted;
    }

    // ✅ PUBLIC: Called when player goes through the door to outside
    public void OnPlayerWentOutside()
    {
        // Gate: only allow after AR, circuit breaker, and appliances missions completed
        if (!CanStartEvacuation())
        {
            Debug.Log("[BeforeSceneManager] Outside mission locked until previous missions are completed or already started.");
            return;
        }

        evacuationMissionStarted = true;

        // persist progress
        SaveGameState();

        Debug.Log("[BeforeSceneManager] Player went outside!");
        
        // Stop player movement
        StopPlayerMovementForAR();
        ResetJoysticks();

        // Disable gameplay UI
        if (gameplayUIRoot != null)
        {
            gameplayUIRoot.SetActive(false);
            Debug.Log("[BeforeSceneManager] Gameplay UI disabled for evacuation quiz");
        }

        // Teleport player to outside spawn immediately now that they're outside
        TeleportPlayerToOutsideSpawn();

        // Start evacuation sequence
        StartCoroutine(EvacuationMissionSequence());
    }

    private IEnumerator EvacuationMissionSequence()
    {
        // Step 1: Show evacuation warning dialogue (only if not already shown earlier)
        if (!evacuationIntroShown)
        {
            ShowGameplayDialogue(ResolveTokens(evacuationIntroMessage));
            evacuationIntroShown = true;
            yield return new WaitForSeconds(evacuationDialogueDuration);
        }

        // Small delay before quiz
        yield return new WaitForSeconds(delayBeforeQuiz);

        // Step 2: Show the evacuation quiz UI
        if (evacuationQuizUI != null)
        {
            Debug.Log("[BeforeSceneManager] Showing evacuation quiz UI...");
            evacuationQuizUI.ShowQuiz();
        }
        else
        {
            Debug.LogError("[BeforeSceneManager] ❌ EvacuationQuizUI is not assigned!");
            // Fallback: continue to completion
            StartCoroutine(EvacuationCompleteSequence());
        }

        // (Quiz completion will call OnEvacuationQuizCompleted)
    }

    // ✅ PUBLIC: Called by EvacuationQuizUI when quiz is completed
    public void OnEvacuationQuizCompleted(bool success)
    {
        if (!success)
        {
            Debug.LogWarning("[BeforeSceneManager] Evacuation quiz failed, but allowing continuation.");
        }

        // Show completion sequence
        StartCoroutine(EvacuationCompleteSequence());
    }

    private IEnumerator EvacuationCompleteSequence()
    {
        // First: show the explanatory completion message (e.g., why they evacuated)
        if (!string.IsNullOrEmpty(evacuationCompleteMessage))
        {
            ShowGameplayDialogue(ResolveTokens(evacuationCompleteMessage));
            yield return new WaitForSeconds(evacuationDialogueDuration);
        }

        // Show the single final achievement banner that indicates level completion
        if (achievementBannerRoot != null && achievementBannerText != null)
        {
            achievementBannerText.text = ResolveTokens(levelCompleteBannerMessage);
            achievementBannerRoot.SetActive(true);
            yield return StartCoroutine(WaitForAchievementBannerClickWithTimeout());
        }

        // Only after the player dismisses the achievement banner show the decision panel
        if (levelCompleteDecisionPanel != null)
        {
            levelCompleteDecisionPanel.SetActive(true);
        }

        // Re-enable gameplay UI and resume movement so player can inspect the outside scene
        if (gameplayUIRoot != null)
        {
            gameplayUIRoot.SetActive(true);
            Debug.Log("[BeforeSceneManager] Gameplay UI re-enabled after evacuation");
        }

        ResumePlayerMovementAfterAR();

        // Small delay for UX polish
        yield return new WaitForSeconds(delayAfterAchievementBanner);

        // Persist final state
        Debug.Log("[BeforeSceneManager] 🎉 GAME COMPLETE! All missions finished.");
        SaveGameState();
    }

    private void OnApplicationQuit()
    {
        SaveGameState();
        StopAutoSave();
    }

    // Decision handlers for Level Complete panel (class scope)
    private void OnProceedDuringSceneClicked()
    {
        if (levelCompleteDecisionPanel != null) levelCompleteDecisionPanel.SetActive(false);
        // Continue in current scene - player already teleported when they went outside
        Debug.Log("[BeforeSceneManager] Player chose to proceed in current scene.");
    }

    private void TeleportPlayerToOutsideSpawn()
    {
        if (outsideSpawnPoint == null)
        {
            Debug.LogWarning("[BeforeSceneManager] outsideSpawnPoint is not assigned. Cannot teleport player.");
            return;
        }

        GameObject player = spawnedPlayerInstance != null ? spawnedPlayerInstance : GameObject.FindGameObjectWithTag(spawnedPlayerTag);
        if (player == null)
        {
            Debug.LogWarning("[BeforeSceneManager] No player instance found to teleport.");
            return;
        }

        // Cache components (prefer cached refs when available)
        CharacterController cc = (player == spawnedPlayerInstance && spawnedPlayerCC != null) ? spawnedPlayerCC : player.GetComponent<CharacterController>();
        NavMeshAgent agent = (player == spawnedPlayerInstance && spawnedPlayerAgent != null) ? spawnedPlayerAgent : player.GetComponent<NavMeshAgent>();
        Rigidbody rb = (player == spawnedPlayerInstance && spawnedPlayerRigidbody != null) ? spawnedPlayerRigidbody : player.GetComponent<Rigidbody>();
        Animator anim = (player == spawnedPlayerInstance && spawnedPlayerAnimator != null) ? spawnedPlayerAnimator : player.GetComponentInChildren<Animator>();

        // Disable controller/agent while moving
        if (cc != null) cc.enabled = false;
        if (agent != null) agent.enabled = false;

        // Apply transform
        player.transform.SetPositionAndRotation(outsideSpawnPoint.position, outsideSpawnPoint.rotation);

        // NavMeshAgent warp (respect baseOffset)
        if (agent != null)
        {
            Vector3 agentWarpPos = outsideSpawnPoint.position - Vector3.up * agent.baseOffset;
            agent.Warp(agentWarpPos);
            agent.ResetPath();
            agent.enabled = true;
        }

        // Rigidbody adjustments
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.position = outsideSpawnPoint.position;
            rb.Sleep();
        }

        // Re-enable CharacterController
        if (cc != null) cc.enabled = true;

        // Reset animator speed param if present
        if (anim != null && !string.IsNullOrEmpty(animatorSpeedParam) && HasAnimatorParam(anim, animatorSpeedParam))
        {
            anim.SetFloat(animatorSpeedParam, 0f);
        }

        Debug.Log("[BeforeSceneManager] Teleported player to outside spawn.");

        // Persist the new player transform
        SaveGameState();
    }

    private void OnProceedToMainMenuClicked()
    {
        if (levelCompleteDecisionPanel != null) levelCompleteDecisionPanel.SetActive(false);
        Debug.Log("[BeforeSceneManager] Player chose to return to Main Menu.");
        GoToMainMenu();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGameState();
    }

    private void SaveGameState()
    {
        try
        {
            // Save mission flags
            PlayerPrefs.SetInt(SaveARMissionCompletedKey, arMissionCompleted ? 1 : 0);
            PlayerPrefs.SetInt(SaveCircuitBreakerFoundKey, circuitBreakerFound ? 1 : 0);
            PlayerPrefs.SetInt(SaveCircuitBreakerCompleteKey, circuitBreakerMissionComplete ? 1 : 0);
            PlayerPrefs.SetInt(SaveAppliancesFoundKey, appliancesMissionFound ? 1 : 0);
            PlayerPrefs.SetInt(SaveAppliancesCompleteKey, appliancesMissionCompleted ? 1 : 0);
            PlayerPrefs.SetInt(SaveEvacuationStartedKey, evacuationMissionStarted ? 1 : 0);

            // Save current scene name
            PlayerPrefs.SetString(SaveSceneKey, SceneManager.GetActiveScene().name);

            // Save player transform if available
            GameObject player = spawnedPlayerInstance != null ? spawnedPlayerInstance : GameObject.FindGameObjectWithTag(spawnedPlayerTag);
            if (player != null)
            {
                Vector3 p = player.transform.position;
                Vector3 r = player.transform.eulerAngles;
                PlayerPrefs.SetFloat(SavePlayerPosXKey, p.x);
                PlayerPrefs.SetFloat(SavePlayerPosYKey, p.y);
                PlayerPrefs.SetFloat(SavePlayerPosZKey, p.z);
                PlayerPrefs.SetFloat(SavePlayerRotXKey, r.x);
                PlayerPrefs.SetFloat(SavePlayerRotYKey, r.y);
                PlayerPrefs.SetFloat(SavePlayerRotZKey, r.z);
            }

            PlayerPrefs.Save();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[BeforeSceneManager] Failed to save game state: " + ex.Message);
        }
    }

    private void LoadGameState()
    {
        // Load mission flags
        arMissionCompleted = PlayerPrefs.GetInt(SaveARMissionCompletedKey, arMissionCompleted ? 1 : 0) == 1;
        circuitBreakerFound = PlayerPrefs.GetInt(SaveCircuitBreakerFoundKey, circuitBreakerFound ? 1 : 0) == 1;
        circuitBreakerMissionComplete = PlayerPrefs.GetInt(SaveCircuitBreakerCompleteKey, circuitBreakerMissionComplete ? 1 : 0) == 1;
        appliancesMissionFound = PlayerPrefs.GetInt(SaveAppliancesFoundKey, appliancesMissionFound ? 1 : 0) == 1;
        appliancesMissionCompleted = PlayerPrefs.GetInt(SaveAppliancesCompleteKey, appliancesMissionCompleted ? 1 : 0) == 1;
        evacuationMissionStarted = PlayerPrefs.GetInt(SaveEvacuationStartedKey, evacuationMissionStarted ? 1 : 0) == 1;

        // Restore derived/locked state that isn't directly saved.
        // If the circuit breaker mission was completed in a previous session,
        // ensure the appliances mission is unlocked so the player can progress.
        appliancesUnlocked = circuitBreakerMissionComplete;
    }

    private void ApplySavedPlayerTransformIfAny()
    {
        // If there's a saved player position, apply it
        if (!PlayerPrefs.HasKey(SavePlayerPosXKey)) return;

        if (spawnedPlayerInstance == null) return;

        float px = PlayerPrefs.GetFloat(SavePlayerPosXKey, spawnedPlayerInstance.transform.position.x);
        float py = PlayerPrefs.GetFloat(SavePlayerPosYKey, spawnedPlayerInstance.transform.position.y);
        float pz = PlayerPrefs.GetFloat(SavePlayerPosZKey, spawnedPlayerInstance.transform.position.z);
        float rx = PlayerPrefs.GetFloat(SavePlayerRotXKey, spawnedPlayerInstance.transform.eulerAngles.x);
        float ry = PlayerPrefs.GetFloat(SavePlayerRotYKey, spawnedPlayerInstance.transform.eulerAngles.y);
        float rz = PlayerPrefs.GetFloat(SavePlayerRotZKey, spawnedPlayerInstance.transform.eulerAngles.z);

        spawnedPlayerInstance.transform.position = new Vector3(px, py, pz);
        spawnedPlayerInstance.transform.rotation = Quaternion.Euler(rx, ry, rz);
    }
}
