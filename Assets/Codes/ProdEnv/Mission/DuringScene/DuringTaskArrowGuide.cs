using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a specific arrow (SpriteRenderer GameObject) for each mission task
/// in the During scene and can optionally drive a UI arrow on the Canvas
/// that always points toward the active world arrow.
///
/// You configure mappings from taskId -> world arrow object in the
/// inspector. When a task starts, the previous world arrow is hidden and
/// the arrow for the new task (if any) is activated. If a UI arrow is
/// assigned, it stays on-screen (clamped to the screen edges) and points
/// toward the active arrow's world position so the player is always guided,
/// even when far away.
///
/// Usage:
/// - Place all arrow GameObjects in the scene and keep them disabled.
/// - Add this component to a manager object (e.g., "TaskArrowGuide").
/// - For each task you want to guide, add a mapping with the taskId and
///   the corresponding arrow GameObject.
/// </summary>
public class DuringTaskArrowGuide : MonoBehaviour
{
    [Serializable]
    private class TaskArrowMapping
    {
        [Tooltip("Task id from MissionData.tasks[*].taskId.")]
        public string taskId;

        [Tooltip("Arrow GameObject (with SpriteRenderer) to enable for this task.")]
        public GameObject arrowObject;
    }

    [Header("Task Arrow Mappings")]
    [SerializeField] private List<TaskArrowMapping> taskArrows = new List<TaskArrowMapping>();

    [Header("Optional UI Pointer")]
    [Tooltip("UI arrow RectTransform (usually an Image on the HUD Canvas).")]
    [SerializeField] private RectTransform uiArrow;

    [Tooltip("Optional: Canvas the UI arrow lives in. If null, parent Canvas is used.")]
    [SerializeField] private Canvas uiCanvas;

    [Tooltip("Padding from screen edges in pixels when clamping the UI arrow position.")]
    [SerializeField] private float screenEdgePadding = 40f;

    private MissionSceneManager missionManager;
    private GameObject currentArrow;
    private bool isSubscribed;

    private Camera worldCamera;
    private RectTransform canvasRect;

    private void OnEnable()
    {
        // Ensure all configured arrows start hidden so their visibility is
        // controlled entirely by the active task, regardless of their
        // initial scene state.
        for (int i = 0; i < taskArrows.Count; i++)
        {
            var mapping = taskArrows[i];
            if (mapping != null && mapping.arrowObject != null)
            {
                mapping.arrowObject.SetActive(false);
            }
        }

        if (uiArrow != null)
        {
            uiArrow.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (uiCanvas == null && uiArrow != null)
        {
            uiCanvas = uiArrow.GetComponentInParent<Canvas>();
        }

        if (uiCanvas != null)
        {
            canvasRect = uiCanvas.transform as RectTransform;
        }

        worldCamera = Camera.main;

        // Subscribe after all Awake calls have run so MissionSceneManager
        // has had a chance to assign Instance.
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
    }

    private void HandleTaskStarted(TaskData task)
    {
        if (task == null)
            return;

        // Hide previous arrow
        if (currentArrow != null)
        {
            currentArrow.SetActive(false);
            currentArrow = null;
        }

        // Find mapping for this task id
        string taskId = task.taskId;
        if (string.IsNullOrWhiteSpace(taskId))
            return;

        for (int i = 0; i < taskArrows.Count; i++)
        {
            var mapping = taskArrows[i];
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.taskId))
                continue;

            if (string.Equals(mapping.taskId, taskId, System.StringComparison.OrdinalIgnoreCase))
            {
                if (mapping.arrowObject != null)
                {
                    mapping.arrowObject.SetActive(true);
                    currentArrow = mapping.arrowObject;
                }
                break;
            }
        }

        // Toggle UI arrow based on whether we have an active world arrow.
        if (uiArrow != null)
        {
            uiArrow.gameObject.SetActive(currentArrow != null);
        }
    }

    private void Update()
    {
        if (uiArrow == null || currentArrow == null || canvasRect == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;

        Vector3 worldPos = currentArrow.transform.position;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        // If behind the camera, flip so the arrow still points roughly toward it.
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
            uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera,
            out anchoredPos);

        uiArrow.anchoredPosition = anchoredPos;

        // Rotate arrow so that its up vector points toward the target.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        uiArrow.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
