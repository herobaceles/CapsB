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
    [TextArea(2, 3)]
    public string[] introDialogue;

    [Header("Start Quiz (Optional)")]
    public MissionQuizData startQuiz;

    [Header("Completion")]
    [TextArea(2, 4)]
    public string completionMessage = "Mission Complete! Great job!";

    [Header("Unlocks On Complete")]
    [Tooltip("Mission ID to unlock when this mission is completed")]
    public string unlocksMissionId;

    /// <summary>
    /// Automatically called when values are changed in the Inspector.
    /// Helps prevent empty or generic IDs.
    /// </summary>
    private void OnValidate()
    {
        // If the missionId is empty, default it to the name of the file
        if (string.IsNullOrEmpty(missionId))
        {
            missionId = name;
        }
    }
}

[System.Serializable]
public class MissionQuizData
{
    [TextArea(2, 4)]
    public string question;

    [Tooltip("Multiple choice options shown to the player")]
    public string[] options = new string[3];

    [Tooltip("Zero-based index of the correct option")]
    public int correctOptionIndex = 0;

    [Header("Visuals (Optional)")]
    [Tooltip("Optional sprites for each option button (0,1,2). If missing, placeholder is used.")]
    public Sprite[] optionSprites = new Sprite[3];

    [Tooltip("Fallback sprite when a specific option sprite is not provided")]
    public Sprite placeholderSprite;
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
    [TextArea(2, 3)]
    public string[] startDialogue;
    
    public bool showDialogueOnComplete = true;
    [TextArea(2, 3)]
    public string[] completeDialogue;

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