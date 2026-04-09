using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a UI arrow on a Canvas that always points toward the
/// world-space target for the current task. The arrow stays on-screen
/// (clamped to the screen edges) so the player is always guided,
/// even when the target is far away.
///
/// Usage:
/// - Add this to a GameObject under a Screen Space Canvas.
/// - Assign a RectTransform (Image) arrowUI that uses an arrow sprite.
/// - Configure mappings from taskId -> world target Transform.
/// - When a mapped task starts, the arrow becomes visible and points
///   toward that target; otherwise it hides.
/// </summary>
public class DuringTaskUIArrowGuide : MonoBehaviour
{
    [Serializable]
    private class TaskPointerMapping
    {
        [Tooltip("Task id from MissionData.tasks[*].taskId.")]
        public string taskId;

        [Tooltip("World-space Transform to point toward for this task (e.g., an empty at the trigger).")]
        public Transform target;
    }

    [Header("UI")]
    [Tooltip("UI arrow RectTransform (usually an Image on the Canvas).")]
    [SerializeField] private RectTransform arrowUI;

    [Tooltip("Optional: Canvas the arrow lives in. If null, the parent Canvas is used.")]
    [SerializeField] private Canvas canvas;

    [Header("Task Pointer Mappings")]
    [SerializeField] private List<TaskPointerMapping> taskPointers = new List<TaskPointerMapping>();

    [Header("Behavior")] 
    [Tooltip("Padding from screen edges in pixels when clamping the arrow position.")]
    [SerializeField] private float screenEdgePadding = 40f;

    private MissionSceneManager missionManager;
    private bool isSubscribed;
    private Transform currentTarget;
    private Camera worldCamera;
    private RectTransform canvasRect;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.transform as RectTransform;

        worldCamera = Camera.main;

        if (arrowUI != null)
            arrowUI.gameObject.SetActive(false);
    }

    private void Start()
    {
        TrySubscribeToMissionManager();
    }

    private void OnDisable()
    {
        if (isSubscribed && missionManager != null)
        {
            missionManager.OnTaskStarted.RemoveListener(HandleTaskStarted);
            isSubscribed = false;
        }
    }

    private void TrySubscribeToMissionManager()
    {
        if (isSubscribed)
            return;

        missionManager = MissionSceneManager.Instance;
        if (missionManager == null)
            return;

        missionManager.OnTaskStarted.AddListener(HandleTaskStarted);
        isSubscribed = true;

        if (missionManager.CurrentTask != null)
        {
            HandleTaskStarted(missionManager.CurrentTask);
        }
    }

    private void HandleTaskStarted(TaskData task)
    {
        currentTarget = null;

        if (arrowUI != null)
            arrowUI.gameObject.SetActive(false);

        if (task == null)
            return;

        string taskId = task.taskId;
        if (string.IsNullOrWhiteSpace(taskId))
            return;

        for (int i = 0; i < taskPointers.Count; i++)
        {
            var mapping = taskPointers[i];
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.taskId) || mapping.target == null)
                continue;

            if (string.Equals(mapping.taskId, taskId, StringComparison.OrdinalIgnoreCase))
            {
                currentTarget = mapping.target;
                break;
            }
        }

        if (currentTarget != null && arrowUI != null)
        {
            arrowUI.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (currentTarget == null || arrowUI == null || canvasRect == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;

        Vector3 worldPos = currentTarget.position;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        // If behind the camera, flip direction so arrow still points roughly toward it.
        if (screenPos.z < 0f)
        {
            screenPos *= -1f;
        }

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir = ((Vector2)screenPos - screenCenter);
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.up;

        dir.Normalize();

        float halfWidth = Screen.width * 0.5f - screenEdgePadding;
        float halfHeight = Screen.height * 0.5f - screenEdgePadding;

        float xFactor = halfWidth / Mathf.Abs(dir.x == 0f ? 0.0001f : dir.x);
        float yFactor = halfHeight / Mathf.Abs(dir.y == 0f ? 0.0001f : dir.y);
        float factor = Mathf.Min(xFactor, yFactor);

        Vector2 clampedScreenPos = screenCenter + dir * factor;

        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            clampedScreenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out anchoredPos);

        arrowUI.anchoredPosition = anchoredPos;

        // Rotate arrow so that its up vector points toward the target.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowUI.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
