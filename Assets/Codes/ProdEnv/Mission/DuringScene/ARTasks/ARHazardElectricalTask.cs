using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// During-phase AR task for isolating a downed electrical hazard using the real-world camera.
/// When started, this task enables AR, lets the player place a hazard zone on a detected plane,
/// and then drag virtual cones around it. Completion occurs when enough cones are placed in the
/// safe ring around the hazard.
/// </summary>
public class ARHazardElectricalTask : ARTaskBase
{
    [Header("AR Setup")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private Camera arCamera;
    [SerializeField] private GameObject hazardPrefab;

    [Header("Gameplay Camera")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private bool disableGameplayCameraInAR = true;

    [Header("Gameplay HUD To Hide")]
    [SerializeField] private GameObject[] gameplayUIRoots;

    [Header("Player Control")]
    [SerializeField] private IsometricPlayerController playerController;

    [Header("Cone Placement Settings")]
    [Tooltip("Inner radius around the hazard where cones are NOT allowed (keeps players back from the live wires).")]
    [SerializeField] private float innerSafeRadius = 1.0f;
    [Tooltip("Outer radius around the hazard where cones are still considered part of the barrier.")]
    [SerializeField] private float outerSafeRadius = 3.0f;
    [Tooltip("How many cones must be correctly placed to complete the task. If 0, all cones under the prefab are required.")]
    [SerializeField] private int requiredCones = 0;
    [Tooltip("Physics layer mask used when selecting cones with a raycast.")]
    [SerializeField] private LayerMask coneLayerMask = ~0;

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject spawnedHazard;
    private HazardConeAR[] cones;
    private HazardConeAR activeCone;
    private Vector3 activeConeOriginalPosition;
    private bool hazardPlaced;
    private bool[] previousUIStates;
    private bool previousMovementEnabled;

    private void OnValidate()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (playerController == null)
            playerController = FindObjectOfType<IsometricPlayerController>();
    }

    public override void StartTask()
    {
        // Lock player movement before AR view appears
        if (playerController == null)
            playerController = FindObjectOfType<IsometricPlayerController>();

        if (playerController != null)
        {
            previousMovementEnabled = playerController.IsMovementEnabled;
            playerController.SetMovementEnabled(false);
        }

        base.StartTask();
    }

    protected override void OnTaskShow()
    {
        hazardPlaced = false;
        activeCone = null;
        cones = null;

        // Hide gameplay HUD
        if (gameplayUIRoots != null && gameplayUIRoots.Length > 0)
        {
            if (previousUIStates == null || previousUIStates.Length != gameplayUIRoots.Length)
                previousUIStates = new bool[gameplayUIRoots.Length];

            for (int i = 0; i < gameplayUIRoots.Length; i++)
            {
                var root = gameplayUIRoots[i];
                if (root == null) continue;

                previousUIStates[i] = root.activeSelf;
                root.SetActive(false);
            }
        }

        // Disable gameplay camera while AR is active
        if (disableGameplayCameraInAR && gameplayCamera != null)
        {
            gameplayCamera.gameObject.SetActive(false);
        }

        // Enable AR session and resolve AR components
        if (ARRuntimeContext.Instance != null)
        {
            ARRuntimeContext.Instance.SetARActive(true);
        }

        ResolveARComponents();

        if (raycastManager == null || arCamera == null)
        {
            Debug.LogError("ARHazardElectricalTask: AR components are not ready (raycastManager or arCamera is null). Failing task.");
            FailTask("AR not available");
        }
    }

    protected override void OnTaskHide()
    {
        // Disable AR session
        if (ARRuntimeContext.Instance != null)
        {
            ARRuntimeContext.Instance.SetARActive(false);
        }

        // Destroy spawned hazard instance
        if (spawnedHazard != null)
        {
            Destroy(spawnedHazard);
            spawnedHazard = null;
        }

        cones = null;
        activeCone = null;
        hazardPlaced = false;
        hits.Clear();

        // Restore gameplay camera
        if (disableGameplayCameraInAR && gameplayCamera != null)
        {
            gameplayCamera.gameObject.SetActive(true);
        }

        // Restore HUD
        if (gameplayUIRoots != null && gameplayUIRoots.Length > 0)
        {
            for (int i = 0; i < gameplayUIRoots.Length; i++)
            {
                var root = gameplayUIRoots[i];
                if (root == null) continue;

                bool wasActive = previousUIStates != null && i < previousUIStates.Length
                    ? previousUIStates[i]
                    : true;

                root.SetActive(wasActive);
            }
        }

        // Restore player movement
        if (playerController != null)
        {
            playerController.SetMovementEnabled(previousMovementEnabled);
        }
    }

    private void Update()
    {
        // Only handle input while this AR task is active
        if (!isActive)
            return;

        HandleInput();
    }

    private void HandleInput()
    {
        // First phase: place the hazard on a detected plane
        if (!hazardPlaced)
        {
            if (TryGetPointerDown(out Vector2 screenPos, out int pointerId))
            {
                if (EventSystem.current != null)
                {
                    if (pointerId >= 0)
                    {
                        if (EventSystem.current.IsPointerOverGameObject(pointerId))
                            return;
                    }
                    else
                    {
                        if (EventSystem.current.IsPointerOverGameObject())
                            return;
                    }
                }

                TryPlaceHazard(screenPos);
            }
            return;
        }

        // Second phase: drag cones around the hazard
        if (activeCone == null)
        {
            // Try to start dragging a cone
            if (TryGetPointerDown(out Vector2 screenPos, out int pointerId))
            {
                if (EventSystem.current != null)
                {
                    if (pointerId >= 0)
                    {
                        if (EventSystem.current.IsPointerOverGameObject(pointerId))
                            return;
                    }
                    else
                    {
                        if (EventSystem.current.IsPointerOverGameObject())
                            return;
                    }
                }

                TryBeginDragCone(screenPos);
            }
        }
        else
        {
            // Continue dragging or end drag
            if (TryGetPointerUp())
            {
                EndDragCone();
            }
            else if (TryGetPointerPosition(out Vector2 screenPos, out _))
            {
                UpdateDragCone(screenPos);
            }
        }
    }

    private void ResolveARComponents()
    {
        if (ARRuntimeContext.Instance != null)
        {
            raycastManager = ARRuntimeContext.Instance.ResolveRaycastManager(raycastManager);
            arCamera = ARRuntimeContext.Instance.ResolveARCamera(arCamera);
        }

        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>(true);

        if (arCamera == null)
            arCamera = Camera.main;
    }

    private bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        screenPosition = default;
        pointerId = -1;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = 0; // primary touch
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    private bool TryGetPointerPosition(out Vector2 screenPosition, out int pointerId)
    {
        screenPosition = default;
        pointerId = -1;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = 0;
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    private bool TryGetPointerUp()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            return true;

        return false;
    }

    private void TryPlaceHazard(Vector2 screenPosition)
    {
        if (hazardPrefab == null)
        {
            Debug.LogError("ARHazardElectricalTask: hazardPrefab is not assigned.");
            return;
        }

        ResolveARComponents();

        if (raycastManager == null)
        {
            Debug.LogError("ARHazardElectricalTask: No ARRaycastManager available.");
            return;
        }

        bool didHitPlane = false;
        try
        {
            didHitPlane = raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon);
        }
        catch (System.ArgumentNullException ex)
        {
            Debug.LogError($"ARHazardElectricalTask: AR raycast failed - {ex.Message}");
            return;
        }

        if (!didHitPlane || hits.Count == 0)
        {
            Debug.Log("ARHazardElectricalTask: No plane hit for hazard placement.");
            return;
        }

        Pose pose = hits[0].pose;
        spawnedHazard = Instantiate(hazardPrefab, pose.position, pose.rotation);

        // Cache cones under the spawned hazard
        cones = spawnedHazard.GetComponentsInChildren<HazardConeAR>();
        if (cones != null)
        {
            for (int i = 0; i < cones.Length; i++)
            {
                if (cones[i] == null) continue;
                cones[i].isPlaced = false;
                cones[i].originalPosition = cones[i].transform.position;
            }
        }

        hazardPlaced = true;
        Debug.Log("ARHazardElectricalTask: Hazard placed in AR.");
    }

    private void TryBeginDragCone(Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            ResolveARComponents();
            if (arCamera == null)
            {
                Debug.LogError("ARHazardElectricalTask: AR camera not available for cone selection.");
                return;
            }
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, coneLayerMask))
        {
            var cone = hit.collider != null ? hit.collider.GetComponentInParent<HazardConeAR>() : null;
            if (cone != null)
            {
                activeCone = cone;
                activeConeOriginalPosition = cone.transform.position;
            }
        }
    }

    private void UpdateDragCone(Vector2 screenPosition)
    {
        if (activeCone == null)
            return;

        ResolveARComponents();
        if (raycastManager == null)
            return;

        bool didHitPlane = false;
        try
        {
            didHitPlane = raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon);
        }
        catch (System.ArgumentNullException ex)
        {
            Debug.LogError($"ARHazardElectricalTask: AR raycast failed during drag - {ex.Message}");
            return;
        }

        if (!didHitPlane || hits.Count == 0)
            return;

        Pose pose = hits[0].pose;
        activeCone.transform.position = pose.position;
    }

    private void EndDragCone()
    {
        if (activeCone == null || spawnedHazard == null)
            return;

        Vector3 hazardPos = spawnedHazard.transform.position;
        Vector3 conePos = activeCone.transform.position;

        Vector3 hazardFlat = new Vector3(hazardPos.x, 0f, hazardPos.z);
        Vector3 coneFlat = new Vector3(conePos.x, 0f, conePos.z);

        float distance = Vector3.Distance(hazardFlat, coneFlat);

        if (distance >= innerSafeRadius && distance <= outerSafeRadius)
        {
            activeCone.isPlaced = true;
        }
        else
        {
            // Snap back if outside safe ring
            activeCone.isPlaced = false;
            activeCone.transform.position = activeConeOriginalPosition;
        }

        activeCone = null;

        CheckConesCompletion();
    }

    private void CheckConesCompletion()
    {
        if (!ValidateCompletion())
            return;

        Debug.Log("ARHazardElectricalTask: All required cones placed. Completing task.");
        CheckCompletion();
    }

    protected override bool ValidateCompletion()
    {
        if (!hazardPlaced || cones == null || cones.Length == 0)
            return false;

        int placed = 0;
        for (int i = 0; i < cones.Length; i++)
        {
            var cone = cones[i];
            if (cone != null && cone.isPlaced)
                placed++;
        }

        int required = requiredCones > 0 ? Mathf.Min(requiredCones, cones.Length) : cones.Length;
        return placed >= required;
    }
}

/// <summary>
/// Marker component for AR hazard cones used in ARHazardElectricalTask.
/// Attach this to each cone object within the hazardPrefab so they can
/// be selected and tracked for placement.
/// </summary>
public class HazardConeAR : MonoBehaviour
{
    [HideInInspector] public bool isPlaced;
    [HideInInspector] public Vector3 originalPosition;
}
