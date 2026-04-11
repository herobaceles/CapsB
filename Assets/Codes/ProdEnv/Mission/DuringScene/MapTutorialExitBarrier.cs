using System.Collections;
using UnityEngine;

/// <summary>
/// Barrier used in the map tutorial task ("tutorial_open_map").
/// Starts as a closed door blocking the player, then plays an
/// opening rotation animation when the player opens the map.
/// </summary>
public class MapTutorialExitBarrier : MonoBehaviour
{
    [Header("Door Setup")]
    [Tooltip("Root object that blocks the player (usually the door GameObject with collider). Defaults to this GameObject if not assigned.")]
    [SerializeField] private GameObject barrierRoot;

    [Tooltip("Transform that should rotate like a door. Defaults to this transform if not assigned.")]
    [SerializeField] private Transform doorTransform;

    [Header("Door Animation")]
    [Tooltip("How many degrees to rotate the door around its local Y axis when opening.")]
    [SerializeField] private float openAngle = 90f;

    [Tooltip("Duration of the door opening animation in seconds.")]
    [SerializeField] private float openDuration = 0.75f;

    private Quaternion closedRotation;
    private bool isOpen;
    private bool isAnimating;

    private Collider[] barrierColliders;

    private void Awake()
    {
        if (barrierRoot == null)
            barrierRoot = gameObject;

        if (doorTransform == null)
            doorTransform = transform;

        closedRotation = doorTransform.localRotation;

        if (barrierRoot != null)
            barrierRoot.SetActive(true);

        // Cache colliders once so we can disable/enable them on open/close.
        if (barrierRoot != null)
        {
            barrierColliders = barrierRoot.GetComponentsInChildren<Collider>(true);
        }
    }

    private void OnEnable()
    {
        // Subscribe to map-view events once the mission manager is available
        if (DuringMissionManager.Instance != null)
        {
            DuringMissionManager.Instance.OnMapViewed.AddListener(HandleMapViewed);
        }
    }

    private void OnDisable()
    {
        if (DuringMissionManager.Instance != null)
        {
            DuringMissionManager.Instance.OnMapViewed.RemoveListener(HandleMapViewed);
        }
    }

    private void HandleMapViewed()
    {
        var mgr = DuringMissionManager.Instance;
        if (mgr == null)
            return;

        // Only react during the map tutorial task
        var currentTask = mgr.CurrentTask;
        if (currentTask == null || string.IsNullOrWhiteSpace(currentTask.taskId))
            return;

        if (!string.Equals(currentTask.taskId, "tutorial_open_map", System.StringComparison.OrdinalIgnoreCase))
            return;

        if (isOpen || isAnimating)
            return;

        if (barrierRoot != null && !barrierRoot.activeSelf)
            barrierRoot.SetActive(true);

        StartCoroutine(OpenDoorRoutine());
    }

    private IEnumerator OpenDoorRoutine()
    {
        isAnimating = true;
        isOpen = true;

        Quaternion startRot = doorTransform.localRotation;
        Quaternion targetRot = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            doorTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        doorTransform.localRotation = targetRot;

        // After opening, optionally disable colliders so the player can
        // pass through even if the door model still overlaps.
        if (barrierColliders != null)
        {
            foreach (var c in barrierColliders)
            {
                if (c != null)
                    c.enabled = false;
            }
        }

        isAnimating = false;

        Debug.Log("MapTutorialExitBarrier: Door opened after viewing map in tutorial_open_map.");
    }

    /// <summary>
    /// Instantly snaps the door back to its closed rotation and re-enables
    /// colliders, without playing the closing animation. This is a simpler
    /// option that can be called from an existing trigger via the Inspector.
    /// </summary>
    public void ResetDoorInstant()
    {
        // Stop any in-progress animation.
        StopAllCoroutines();

        doorTransform.localRotation = closedRotation;

        if (barrierColliders != null)
        {
            foreach (var c in barrierColliders)
            {
                if (c != null)
                    c.enabled = true;
            }
        }

        isOpen = false;
        isAnimating = false;

        Debug.Log("MapTutorialExitBarrier: Door reset instantly to closed state.");
    }

    /// <summary>
    /// Closes the tutorial door again, re-enabling its colliders after the
    /// closing animation finishes. Intended to be called by an external
    /// trigger when the player exits through the doorway.
    /// </summary>
    public void CloseDoor()
    {
        if (!isOpen || isAnimating)
            return;

        StartCoroutine(CloseDoorRoutine());
    }

    private IEnumerator CloseDoorRoutine()
    {
        isAnimating = true;

        Quaternion startRot = doorTransform.localRotation;
        Quaternion targetRot = closedRotation;

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            doorTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        doorTransform.localRotation = targetRot;

        // Re-enable colliders so the doorway blocks the player again if desired.
        if (barrierColliders != null)
        {
            foreach (var c in barrierColliders)
            {
                if (c != null)
                    c.enabled = true;
            }
        }

        isOpen = false;
        isAnimating = false;

        Debug.Log("MapTutorialExitBarrier: Door closed after player exited in tutorial_open_map.");
    }
}
