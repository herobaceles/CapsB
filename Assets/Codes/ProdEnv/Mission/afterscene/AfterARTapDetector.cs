using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central AR tap detector for the After-phase AR missions.
///
/// This uses the active AR camera (from ARRuntimeContext) to raycast
/// into the AR house and forward taps to interaction scripts such as
/// HiddenDangerItem and MudPileInteraction. This is much more reliable
/// on mobile AR than relying on OnMouseDown.
///
/// Attach this to any always-active GameObject in the After scene
/// (for example, the same GameObject as AfterRecoveryARController),
/// and optionally override the AR camera or raycast layer mask.
/// </summary>
public class AfterARTapDetector : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("Optional explicit AR camera. If not set, the detector will try ARRuntimeContext.ResolveARCamera().")]
    [SerializeField] private Camera arCameraOverride;
    [Tooltip("Layers to raycast against when detecting AR taps.")]
    [SerializeField] private LayerMask raycastLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private float maxDistance = 20f;

    [Header("Behaviour")] 
    [Tooltip("Only process taps while AfterRecoveryARController reports AR as active.")]
    [SerializeField] private bool requireARActive = true;

    [Header("Hidden Danger Drag")]
    [Tooltip("Optional explicit bucket targets. If assigned, distance to these is used; otherwise falls back to tag-based detection.")]
    [SerializeField] private Transform[] bucketTargets;
    [Tooltip("Tag used by the Hidden Danger bucket object when no explicit targets are assigned.")]
    [SerializeField] private string bucketTag = "Bucket";
    [Tooltip("World-space radius around the dragged item used to detect the bucket on drop.")]
    [SerializeField] private float bucketCatchRadius = 0.4f;

    [Tooltip("How far to pull hazards toward the AR camera when drag starts.")]
    [SerializeField] private float dragPullTowardsCamera = 0.25f;

    private HiddenDangerItem currentDraggedItem;
    private Vector3 currentDragOffset;
    private float currentDragDepth;
    private Vector3 originalItemPosition;

    private void Update()
    {
        if (requireARActive)
        {
            if (AfterRecoveryARController.Instance == null || !AfterRecoveryARController.Instance.IsARActive)
                return;
        }

        // Touch input (mobile) via new Input System
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                Vector2 pos = touch.position.ReadValue();
                BeginPointerInteraction(pos);
                return;
            }

            if (touch.press.isPressed && currentDraggedItem != null)
            {
                Vector2 pos = touch.position.ReadValue();
                ContinueDrag(pos);
                return;
            }

            if (touch.press.wasReleasedThisFrame)
            {
                Vector2 pos = touch.position.ReadValue();
                EndPointerInteraction(pos);
                return;
            }
        }

        // Mouse input (editor/testing) via new Input System
        if (Mouse.current != null)
        {
            var button = Mouse.current.leftButton;

            if (button.wasPressedThisFrame)
            {
                Vector2 pos = Mouse.current.position.ReadValue();
                BeginPointerInteraction(pos);
                return;
            }

            if (button.isPressed && currentDraggedItem != null)
            {
                Vector2 pos = Mouse.current.position.ReadValue();
                ContinueDrag(pos);
                return;
            }

            if (button.wasReleasedThisFrame)
            {
                Vector2 pos = Mouse.current.position.ReadValue();
                EndPointerInteraction(pos);
                return;
            }
        }
    }

    private void BeginPointerInteraction(Vector3 screenPosition)
    {
        Camera cam = ResolveARCamera();
        if (cam == null)
        {
            Debug.LogWarning("AfterARTapDetector: No AR camera available to raycast from.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastLayers))
        {
            return;
        }

        // First, try HiddenDangerItem
        HiddenDangerItem hiddenItem = hit.collider.GetComponentInParent<HiddenDangerItem>();
        if (hiddenItem != null)
        {
            // Behaviour by mission mode:
            // - HiddenDanger mode: non-stationary hazards (snake, rat, etc.)
            //   are drag-to-bucket only. We NEVER call OnTappedFromAR here;
            //   only StartDrag + drop.
            // - Any other mode (CleanupGear, KitchenSafety, DisinfectHouse):
            //   use normal tap behaviour via OnTappedFromAR so CleanupItem
            //   still disappears on tap.

            MissionMode mode = AfterRecoveryARController.Instance != null
                ? AfterRecoveryARController.Instance.CurrentMissionMode
                : MissionMode.HiddenDanger;

            if (mode == MissionMode.HiddenDanger && !hiddenItem.isStationaryFeedbackOnly)
            {
                StartDrag(cam, hiddenItem, hit.point);
                return;
            }

            hiddenItem.OnTappedFromAR();
            return;
        }

        // Then, try mud piles (CleanupGear disinfect tasks)
        MudPileInteraction mudPile = hit.collider.GetComponentInParent<MudPileInteraction>();
        if (mudPile != null)
        {
            mudPile.PickUpMud(cam);
            return;
        }
    }

    private void ContinueDrag(Vector3 screenPosition)
    {
        if (currentDraggedItem == null)
            return;

        Camera cam = ResolveARCamera();
        if (cam == null)
            return;

        Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, currentDragDepth);
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPoint) + currentDragOffset;
        currentDraggedItem.transform.position = worldPos;
    }

    private void EndPointerInteraction(Vector3 screenPosition)
    {
        if (currentDraggedItem == null)
            return;

        // On release, check if the dragged hazard is close enough to the
        // bucket. Prefer explicit bucket targets if configured; otherwise
        // fall back to tag-based detection.
        Vector3 itemPos = currentDraggedItem.transform.position;

        bool droppedInBucket = false;

        // Prefer explicit bucket targets that are actually tagged as Bucket.
        if (bucketTargets != null && bucketTargets.Length > 0)
        {
            for (int i = 0; i < bucketTargets.Length; i++)
            {
                Transform t = bucketTargets[i];
                if (t == null) continue;

                // Ignore any entries that are not real bucket transforms
                // (e.g. misconfigured hazards) or that point to the dragged
                // item itself.
                if (!t.CompareTag(bucketTag))
                    continue;
                if (currentDraggedItem != null && t == currentDraggedItem.transform)
                    continue;

                if (Vector3.Distance(itemPos, t.position) <= bucketCatchRadius)
                {
                    droppedInBucket = true;
                    break;
                }
            }
        }

        // If no matching bucket targets were found, fall back to an
        // overlap query for any collider tagged as the bucket.
        if (!droppedInBucket)
        {
            Collider[] hits = Physics.OverlapSphere(itemPos, bucketCatchRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].CompareTag(bucketTag))
                {
                    droppedInBucket = true;
                    break;
                }
            }
        }

        if (droppedInBucket)
        {
            // Optional: trigger feedback via the central AR controller.
            if (AfterRecoveryARController.Instance != null)
            {
                AfterRecoveryARController.Instance.TriggerFeedback(true, itemPos);
            }

            currentDraggedItem.Recover();
        }
        currentDraggedItem = null;
    }

    private void StartDrag(Camera cam, HiddenDangerItem item, Vector3 hitPoint)
    {
        if (item == null)
            return;

        currentDraggedItem = item;

        // Start from the item's current position, then gently pull it a bit
        // toward the AR camera so it doesn't jump away from the player when
        // dragging begins.
        Vector3 startWorldPos = item.transform.position;
        if (cam != null && dragPullTowardsCamera > 0f)
        {
            Vector3 toCamera = (cam.transform.position - startWorldPos).normalized;
            startWorldPos += toCamera * dragPullTowardsCamera;
        }

        item.transform.position = startWorldPos;
        originalItemPosition = startWorldPos;

        // Lock dragging to the depth of this adjusted position so that
        // subsequent ScreenToWorldPoint calls keep the hazard at a
        // comfortable distance from the camera.
        Vector3 itemScreenPos = cam.WorldToScreenPoint(startWorldPos);
        currentDragDepth = Mathf.Max(0.1f, itemScreenPos.z);
        currentDragOffset = Vector3.zero;
    }

    private Camera ResolveARCamera()
    {
        if (arCameraOverride != null)
            return arCameraOverride;

        if (ARRuntimeContext.Instance != null)
        {
            Camera arCam = ARRuntimeContext.Instance.ResolveARCamera();
            if (arCam != null)
                return arCam;
        }

        // Fallback to main camera if needed (for editor/testing)
        return Camera.main;
    }
}
