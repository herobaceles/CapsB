using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mission phases - Before, During, After disaster
/// </summary>
public enum MissionPhase
{
    Before,     // Preparation phase (packing, securing, planning)
    During,     // Response phase (evacuation, rescue, survival)
    After       // Recovery phase (first aid, cleanup, rebuilding)
}

/// <summary>
/// ScriptableObject that defines a mission and its tasks.
/// Create via: Assets > Create > BaHanda > Mission Data
/// </summary>
[CreateAssetMenu(fileName = "NewMission", menuName = "BaHanda/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Info")]
    public string missionId;
    public string missionName;
    [TextArea(2, 4)]
    public string missionDescription;
    public Sprite missionIcon;

    [Header("Phase & Scene")]
    public MissionPhase phase = MissionPhase.Before;
    [Tooltip("Scene to load for this mission (e.g., BeforeMission, DuringMission, AfterMission)")]
    public string missionSceneName;

    [Header("Unlock Requirements")]
    public bool isLocked = false;
    [Tooltip("Mission ID that must be completed to unlock this mission")]
    public string requiredMissionId;
    [Tooltip("Order within the phase (for sorting in UI)")]
    public int sortOrder = 0;

    [Header("Tasks")]
    public List<TaskData> tasks = new List<TaskData>();

    [Header("Intro Dialogue")]
    [HideInInspector, TextArea(2, 3)]
    public string[] introDialogue;

    [Tooltip("Rich intro dialogue with per-line character, expression, and background settings.")]
    public List<DialogueLineData> introDialogueRich = new List<DialogueLineData>();

    [Header("Start Quiz (Optional)")]
    public MissionQuizData startQuiz;
    
    [Tooltip("If non-empty, defines an ordered sequence of start quizzes for this mission. If empty, the single startQuiz field is used instead.")]
    public List<MissionQuizData> startQuizSequence = new List<MissionQuizData>();

    [Header("Timer (Optional)")]
    [Tooltip("Enable a mission-wide countdown. If enabled, scene managers like DuringMissionManager can enforce this time limit.")]
    public bool useMissionTimer = false;

    [Tooltip("Total mission time in seconds when a mission-wide timer is used.")]
    public float missionTimeLimitSeconds = 0f;

    [Header("Completion")]
    [TextArea(2, 4)]
    public string completionMessage = "Mission Complete! Great job!";

    [Header("Unlocks On Complete")]
    [Tooltip("Mission ID to unlock when this mission is completed")]
    public string unlocksMissionId;
}

[System.Serializable]
public class MissionQuizData
{
    [TextArea(2, 4)]
    public string question;

    [Tooltip("Multiple choice options shown to the player")]
    public string[] options = new string[3];

    [Tooltip("Optional per-option feedback for incorrect answers. Index should match the option index; leave empty to use the default wrong-answer message.")]
    [TextArea(2, 4)]
    public string[] wrongOptionFeedback = new string[3];

    [Tooltip("Zero-based index of the correct option")]
    public int correctOptionIndex = 0;

    [Header("Visuals (Optional)")]
    [Tooltip("Optional sprites for each option button (0,1,2). If missing, placeholder is used.")]
    public Sprite[] optionSprites = new Sprite[3];

    [Tooltip("Fallback sprite when a specific option sprite is not provided")]
    public Sprite placeholderSprite;

    [Header("Question Speaker (Optional)")]
    [Tooltip("Character ID of the speaker asking the quiz question (must match a CharacterPreset.characterId in ProdDialogueManager).")]
    public string questionCharacterId;

    [Tooltip("Expression ID to use for the quiz question speaker (e.g., 'explaining', 'happy').")]
    public string questionExpressionId;

    [Tooltip("If set, this sprite overrides character presets and is used as the quiz question portrait.")]
    public Sprite questionPortraitOverride;

    [Header("Post-Quiz Dialogue (Optional)")]
    [HideInInspector, TextArea(2, 3)]
    [Tooltip("Legacy simple dialogue lines shown after the player selects the correct answer in the start quiz.")]
    public string[] correctAnswerDialogue;

    [Tooltip("Rich post-quiz dialogue with per-line character, expression, side, and background settings.")]
    public List<DialogueLineData> correctAnswerDialogueRich = new List<DialogueLineData>();
}

/// <summary>
/// Defines a single task within a mission.
/// </summary>
[System.Serializable]
public class TaskData
{
    [Header("Task Info")]
    public string taskId;
    public string taskName;
    [TextArea(2, 4)]
    public string taskDescription;
    public Sprite taskIcon;

    [Header("Task Type")]
    public TaskType taskType = TaskType.Trigger;
    public bool isOptional = false;

    [Header("Dialogue")]
    public bool showDialogueOnStart = true;
    [HideInInspector, TextArea(2, 3)]
    public string[] startDialogue;
    [Tooltip("Rich start dialogue for this task.")]
    public List<DialogueLineData> startDialogueRich = new List<DialogueLineData>();
    
    public bool showDialogueOnComplete = true;
    [HideInInspector, TextArea(2, 3)]
    public string[] completeDialogue;
    [Tooltip("Rich completion dialogue for this task.")]
    public List<DialogueLineData> completeDialogueRich = new List<DialogueLineData>();

    [Header("AR Guidance (Optional)")]
    [Tooltip("Dialogue that explains how to scan/detect AR planes (e.g., move device to look at the floor).")]
    public List<DialogueLineData> arScanForPlaneDialogueRich = new List<DialogueLineData>();

    [Tooltip("Dialogue shown once a plane is detected, instructing the player to tap to place the AR content.")]
    public List<DialogueLineData> arTapToPlaceDialogueRich = new List<DialogueLineData>();

    [Tooltip("General dialogue shown while the player is in an AR session for this task (e.g., how to interact with AR objects).")]
    public List<DialogueLineData> arGuidanceDialogueRich = new List<DialogueLineData>();

    [Header("Quiz (Optional)")]
    [Tooltip("Optional quiz gate for this specific task. Used by scene managers that support per-task quizzes.")]
    public MissionQuizData taskStartQuiz;

    [Header("Objectives (for multi-objective tasks)")]
    public List<ObjectiveData> objectives = new List<ObjectiveData>();

}

/// <summary>
/// Defines an objective within a task (for complex tasks with multiple steps).
/// </summary>
[System.Serializable]
public class ObjectiveData
{
    public string objectiveId;
    public string description;
    public int requiredCount = 1;
    [HideInInspector] public bool isCompleted = false;
    [HideInInspector] public int currentCount = 0;
}

/// <summary>
/// Types of tasks available.
/// </summary>
public enum TaskType
{
    Trigger,        // Complete by entering a trigger zone
    Interact,       // Complete by interacting with an object
    Collect,        // Collect items
    Escort,         // Escort/protect someone
    Timer,          // Complete within time limit
    Dialogue,       // Complete a dialogue sequence
    Custom          // Custom logic handled by code
}
