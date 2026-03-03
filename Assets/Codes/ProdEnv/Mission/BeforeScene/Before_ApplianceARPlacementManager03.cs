using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Handles AR plane placement for before_03 by spawning the house interior prefab once.
/// </summary>
public class ApplianceARPlacementManager03 : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject houseInteriorPrefab;

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private SecuringAppliancesManager pendingMissionManager;
    private bool waitingForPlacement;
    private bool placed;

    public void BeginPlacement(SecuringAppliancesManager manager)
    {
        pendingMissionManager = manager;
        waitingForPlacement = true;
        placed = false;
        ResolveRaycastManager();
        Debug.Log($"ApplianceARPlacementManager03: BeginPlacement called. Waiting for tap. raycastManager={raycastManager}, prefab={houseInteriorPrefab}");
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

        if (MissionSelectManager.SelectedMission == null)
        {
            Debug.LogWarning("ApplianceARPlacementManager03: SelectedMission is null");
            return;
        }

        if (!string.Equals(MissionSelectManager.SelectedMission.missionId, "before_03", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"ApplianceARPlacementManager03: Wrong mission. Expected 'before_03', got '{MissionSelectManager.SelectedMission.missionId}'");
            return;
        }

        if (TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
        {
            Debug.Log($"ApplianceARPlacementManager03: Pointer down at {screenPosition}");
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
            {
                Debug.Log("ApplianceARPlacementManager03: Pointer over UI, ignoring.");
                return;
            }

            TryPlaceHouse(screenPosition);
        }
    }

    private bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        screenPosition = default;
        pointerId = -1;

        // Check touch input
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = 0; // primary touch
            return true;
        }

        // Check mouse input
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
            Debug.LogError("ApplianceARPlacementManager03: raycastManager is null!");
            return;
        }

        if (houseInteriorPrefab == null)
        {
            Debug.LogError("ApplianceARPlacementManager03: houseInteriorPrefab is null!");
            return;
        }

        Debug.Log($"ApplianceARPlacementManager03: Attempting raycast at {screenPosition}");

        if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.LogWarning("ApplianceARPlacementManager03: Raycast did not hit a plane.");
            return;
        }

        Debug.Log("ApplianceARPlacementManager03: Raycast hit! Spawning house prefab.");
        Pose pose = hits[0].pose;
        GameObject spawnedRoot = Instantiate(houseInteriorPrefab, pose.position, pose.rotation);
        FixSpawnedVisuals(spawnedRoot);
        Debug.Log($"ApplianceARPlacementManager03: House spawned at {pose.position}");

        if (pendingMissionManager != null)
        {
            pendingMissionManager.InitializeFromSpawnedRoot(spawnedRoot);
            pendingMissionManager.StartMissionFromTrigger();
        }

        placed = true;
        waitingForPlacement = false;
    }

    private void FixSpawnedVisuals(GameObject root)
    {
        if (root == null)
            return;

        int defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer < 0)
            defaultLayer = 0;

        ApplyHierarchyState(root.transform, defaultLayer);

        int rendererCount = 0;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            renderer.enabled = true;
            renderer.forceRenderingOff = false;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.gameObject.layer = defaultLayer;
            rendererCount++;
        }

        foreach (var lodGroup in root.GetComponentsInChildren<LODGroup>(true))
        {
            lodGroup.ForceLOD(0);
            lodGroup.enabled = false;
        }

        Debug.Log($"ApplianceARPlacementManager03: Forced visuals active. Renderers={rendererCount}");
    }

    private void ApplyHierarchyState(Transform node, int layer)
    {
        if (node == null)
            return;

        node.gameObject.SetActive(true);
        node.gameObject.layer = layer;

        for (int i = 0; i < node.childCount; i++)
            ApplyHierarchyState(node.GetChild(i), layer);
    }
}