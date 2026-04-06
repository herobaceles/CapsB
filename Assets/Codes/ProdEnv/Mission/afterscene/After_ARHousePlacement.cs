using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Optional tap-to-place helper for After-scene AR houses.
///
/// NOTE: This script is currently NOT used by default in the
/// project. After-scene AR houses are activated immediately when
/// the AR task starts and are not repositioned via taps.
///
/// If you decide later that you want a tap-to-place flow similar
/// to the Before scene, you can attach this to an AR house root
/// and enable autoStartOnEnable.
/// </summary>
public class After_ARHousePlacement : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Behaviour")]
    [Tooltip("Automatically begin waiting for placement when this object becomes active while AR is running.")]
    [SerializeField] private bool autoStartOnEnable = false;

    [Tooltip("If true, the house will be rotated to face the AR camera's forward direction on placement.")]
    [SerializeField] private bool alignToCameraForward = false;

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool waitingForPlacement;
    private bool placed;

    private void OnEnable()
    {
        // Auto-start placement only when an AR session is actually active.
        if (autoStartOnEnable && AfterRecoveryARController.Instance != null && AfterRecoveryARController.Instance.IsARActive)
        {
            BeginPlacement();
        }
    }

    /// <summary>
    /// Can be called manually (e.g., from AfterRecoveryARController)
    /// to begin placement flow.
    /// </summary>
    public void BeginPlacement()
    {
        ResolveRaycastManager();

        if (raycastManager == null)
        {
            Debug.LogWarning("After_ARHousePlacement: No ARRaycastManager available. Placement will not run.");
            return;
        }

        waitingForPlacement = true;
        placed = false;
        Debug.Log("After_ARHousePlacement: Waiting for tap to place AR house.");
    }

    private void ResolveRaycastManager()
    {
        if (ARRuntimeContext.Instance != null)
            raycastManager = ARRuntimeContext.Instance.ResolveRaycastManager(raycastManager);
    }

    private void Update()
    {
        if (!waitingForPlacement || placed)
            return;

        // Don't run placement if AR session is no longer active.
        if (AfterRecoveryARController.Instance == null || !AfterRecoveryARController.Instance.IsARActive)
            return;

        if (TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
            {
                // Ignore taps that are over UI.
                return;
            }

            TryPlaceHouse(screenPosition);
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
            return true;
        }

        return false;
    }

    private void TryPlaceHouse(Vector2 screenPosition)
    {
        ResolveRaycastManager();

        if (raycastManager == null)
        {
            Debug.LogError("After_ARHousePlacement: ARRaycastManager is null; cannot place house.");
            return;
        }

        try
        {
            if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Debug.Log("After_ARHousePlacement: Raycast did not hit a plane.");
                return;
            }
        }
        catch (System.ArgumentNullException exception)
        {
            Debug.LogError($"After_ARHousePlacement: AR raycast failed. {exception.Message}");
            return;
        }

        Pose pose = hits[0].pose;

        // Move this existing house root to the hit pose.
        Transform t = transform;
        t.position = pose.position;

        if (alignToCameraForward && ARRuntimeContext.Instance != null && ARRuntimeContext.Instance.ARCamera != null)
        {
            Vector3 forward = ARRuntimeContext.Instance.ARCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
        else
        {
            t.rotation = pose.rotation;
        }

        // Ensure the house is parented under the AR root so it lives in the AR space.
        if (ARRuntimeContext.Instance != null && ARRuntimeContext.Instance.ARRoot != null)
        {
            t.SetParent(ARRuntimeContext.Instance.ARRoot.transform, true);
        }

        placed = true;
        waitingForPlacement = false;

        Debug.Log($"After_ARHousePlacement: AR house placed at {pose.position}.");
    }
}
