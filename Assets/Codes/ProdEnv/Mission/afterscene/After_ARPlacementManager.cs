using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Handles tap-to-place behaviour for After-phase AR houses.
///
/// - Waits for the player to tap on a detected horizontal plane.
/// - Moves the active AR house root to that pose and parents it
///   under the global ARRoot.
/// - Only places once per AR session; after that, the house stays
///   fixed for the duration of the task.
/// </summary>
public class After_ARPlacementManager : MonoBehaviour
{
    [Header("AR Managers")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("UI (Optional)")]
    [Tooltip("Optional UI hint shown while waiting for the player to tap a plane.")]
    [SerializeField] private GameObject placementHintUI;

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject currentHouseRoot;
    private bool waitingForPlacement;
    private bool placed;
    private bool ignoreNextPointerDown;
    private GameObject disabledPlayer;
    private bool disabledPlayerByThisManager;

    // Optional mission/task context for AR guidance dialogue.
    private TaskData currentTask;
    private ProdDialogueManager dialogueManager;
    private bool arScanHintShown;
    private bool arTapHintShown;

    /// <summary>
    /// Fired once when a house has been successfully placed and
    /// activated for the current AR session.
    /// </summary>
    public event Action<GameObject> OnHousePlaced;

    /// <summary>
    /// The most recently placed house root for this session.
    /// </summary>
    public GameObject PlacedHouseRoot { get; private set; }

    /// <summary>
    /// True once the house has been placed for the current AR session.
    /// </summary>
    public bool HasPlaced => placed;

    /// <summary>
    /// Begin placement for the specified AR house root. The house
    /// will be positioned and parented when the player taps a
    /// suitable horizontal plane.
    /// </summary>
    public void BeginPlacement(GameObject houseRoot)
    {
        BeginPlacement(houseRoot, null);
    }

    /// <summary>
    /// Begin placement for the specified AR house root with an
    /// optional TaskData used for AR guidance dialogue.
    /// </summary>
    public void BeginPlacement(GameObject houseRoot, TaskData task)
    {
        currentHouseRoot = houseRoot;
        currentTask = task;
        if (currentHouseRoot == null)
        {
            waitingForPlacement = false;
            placed = false;
            return;
        }

        ResolveManagers();

        // Subscribe to AR state changes so we can disable/restore the player
        if (ARRuntimeContext.Instance != null)
        {
            ARRuntimeContext.Instance.OnARActiveChanged -= HandleARActiveChanged;
            ARRuntimeContext.Instance.OnARActiveChanged += HandleARActiveChanged;
        }

        placed = false;
        waitingForPlacement = true;
        ignoreNextPointerDown = true;

        dialogueManager = ProdDialogueManager.Instance;
        arScanHintShown = false;
        arTapHintShown = false;

        // Ensure the house is hidden until the player taps to
        // place it on a detected horizontal plane.
        currentHouseRoot.SetActive(false);

        if (placementHintUI != null)
            placementHintUI.SetActive(true);

        Debug.Log($"After_ARPlacementManager: BeginPlacement for '{currentHouseRoot.name}'. Waiting for tap on horizontal plane.");
    }

    private void ResolveManagers()
    {
        if (ARRuntimeContext.Instance != null)
        {
            raycastManager = ARRuntimeContext.Instance.ResolveRaycastManager(raycastManager);
            if (planeManager == null)
                planeManager = ARRuntimeContext.Instance.PlaneManager;
        }
    }

    private void Update()
    {
        if (!waitingForPlacement || placed || currentHouseRoot == null)
            return;

        // While waiting for placement, optionally show AR scan and
        // tap-to-place guidance based on the current TaskData.
        TryShowARGuidanceHints();

        if (!TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
            return;

        // Ignore the very first pointer down after AR is enabled so
        // that the tap which starts the AR task (e.g. the button
        // press) does not immediately place the house.
        if (ignoreNextPointerDown)
        {
            ignoreNextPointerDown = false;
            return;
        }

        if (EventSystem.current != null && pointerId >= 0 && EventSystem.current.IsPointerOverGameObject(pointerId))
            return;

        TryPlaceHouse(screenPosition);
    }

    private void TryShowARGuidanceHints()
    {
        if (currentTask == null || dialogueManager == null)
            return;

        // 1) While no plane is yet detected, show scan guidance once.
        if (!arScanHintShown && !dialogueManager.IsDialogueActive &&
            currentTask.arScanForPlaneDialogueRich != null && currentTask.arScanForPlaneDialogueRich.Count > 0)
        {
            arScanHintShown = true;
            dialogueManager.ShowDialogueSequence(currentTask.arScanForPlaneDialogueRich, null);
            return;
        }

        // 2) Once a plane is detectable at the center of the screen,
        //    tell the player to tap to place the AR house.
        if (!arTapHintShown && arScanHintShown && !dialogueManager.IsDialogueActive)
        {
            ResolveManagers();
            if (raycastManager == null)
                return;

            bool didHitPlane = false;
            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            try
            {
                didHitPlane = raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
            }
            catch (ArgumentNullException exception)
            {
                Debug.LogError($"After_ARPlacementManager: Center-screen AR raycast failed while checking for planes. {exception.Message}");
                return;
            }

            if (didHitPlane &&
                currentTask.arTapToPlaceDialogueRich != null && currentTask.arTapToPlaceDialogueRich.Count > 0)
            {
                arTapHintShown = true;
                dialogueManager.ShowDialogueSequence(currentTask.arTapToPlaceDialogueRich, null);
            }
        }
    }

    private bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        screenPosition = default;
        pointerId = -1;

        // Touch input (mobile)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = 0; // primary touch
            return true;
        }

        // Mouse input (editor/testing)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            // pointerId remains -1; we skip UI raycast in that case.
            return true;
        }

        return false;
    }

    private void TryPlaceHouse(Vector2 screenPosition)
    {
        ResolveManagers();

        if (raycastManager == null)
        {
            Debug.LogError("After_ARPlacementManager: raycastManager is null; cannot place house.");
            return;
        }

        if (currentHouseRoot == null)
            return;

        hits.Clear();

        if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.Log("After_ARPlacementManager: Raycast did not hit a plane; ignoring tap.");
            return;
        }

        var hit = hits[0];
        Pose pose = hit.pose;

        // If we have a PlaneManager, ensure the hit plane is horizontal
        // (i.e., a floor/table, not a wall or ceiling).
        if (planeManager != null)
        {
            ARPlane plane = planeManager.GetPlane(hit.trackableId);
            if (plane != null)
            {
                var alignment = plane.alignment;
                bool isHorizontal = alignment == PlaneAlignment.HorizontalUp || alignment == PlaneAlignment.HorizontalDown;
                if (!isHorizontal)
                {
                    Debug.Log("After_ARPlacementManager: Hit non-horizontal plane (likely a wall); waiting for a floor tap.");
                    return;
                }
            }
        }

        Debug.Log($"After_ARPlacementManager: Placing house '{currentHouseRoot.name}' at {pose.position}.");

        // Move house to pose
        Transform houseTransform = currentHouseRoot.transform;
        houseTransform.SetPositionAndRotation(pose.position, pose.rotation);

        // Parent under ARRoot so it lives in AR space.
        if (ARRuntimeContext.Instance != null && ARRuntimeContext.Instance.ARRoot != null)
        {
            houseTransform.SetParent(ARRuntimeContext.Instance.ARRoot.transform, true);
        }

        // Now that the house has a stable pose in AR space,
        // make it visible.
        currentHouseRoot.SetActive(true);

        placed = true;
        waitingForPlacement = false;

        PlacedHouseRoot = currentHouseRoot;

        OnHousePlaced?.Invoke(currentHouseRoot);

        if (placementHintUI != null)
            placementHintUI.SetActive(false);

        // Show AR guidance dialogue once the house has been placed,
        // if configured on the current task.
        if (currentTask != null && dialogueManager != null &&
            currentTask.arGuidanceDialogueRich != null &&
            currentTask.arGuidanceDialogueRich.Count > 0 &&
            !dialogueManager.IsDialogueActive)
        {
            dialogueManager.ShowDialogueSequence(currentTask.arGuidanceDialogueRich, null);
        }
    }

    private void HandleARActiveChanged(bool active)
    {
        if (active)
        {
            // Prevent immediate placement from the tap that started AR
            ignoreNextPointerDown = true;

            GameObject player = null;
            try { player = GameObject.FindGameObjectWithTag("Player"); } catch { }

            if (player != null && player.activeInHierarchy)
            {
                disabledPlayer = player;
                disabledPlayerByThisManager = true;
                player.SetActive(false);

                var cam = FindObjectOfType<IsometricCameraController>();
                if (cam != null)
                    cam.Target = null;

                Debug.Log("After_ARPlacementManager: Disabled player for AR session.");
            }
        }
        else
        {
            if (disabledPlayerByThisManager && disabledPlayer != null)
            {
                disabledPlayer.SetActive(true);
                var cam = FindObjectOfType<IsometricCameraController>();
                if (cam != null)
                {
                    cam.Target = disabledPlayer.transform;
                    cam.SnapToTarget();
                }

                disabledPlayer = null;
                disabledPlayerByThisManager = false;
                Debug.Log("After_ARPlacementManager: Restored player after AR session.");
            }
        }
    }

    private void OnDestroy()
    {
        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.OnARActiveChanged -= HandleARActiveChanged;
    }
}
