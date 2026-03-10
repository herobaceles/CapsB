using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiddenDangerSpawner : MonoBehaviour
{
    [Header("AR Mission References")]
    [Tooltip("Drag your AR_HiddenDangerHouse prefab here")]
    public GameObject houseInteriorPrefab; 
    
    [Tooltip("Drag your AR_CleanupGearHouse prefab here")]
    public GameObject cleanupGearPrefab; 

    // --- NEW: Added Kitchen Safety Prefab Slot ---
    [Tooltip("Drag your NEW AR_KitchenSafetyHouse prefab here")]
    public GameObject kitchenSafetyPrefab; 

    [Header("Spawn Settings")]
    public Transform arParent;

    [Header("Transition Settings (Hide the 3D World)")]
    [Tooltip("Drag the scene's HouseInterior, Player/Boy, and any old UI here to hide them when AR starts")]
    public GameObject[] objectsToHideInAR;

    private bool missionPlaced = false;

    // --- Track the currently spawned house to destroy it between phases ---
    private MissionMode? lastSpawnedMode = null;
    private GameObject currentSpawnedRoom;

    public void SpawnHiddenDangers(Vector3 spawnPosition)
    {
        // Determine which mission we are playing
        MissionMode currentMode = MissionMode.HiddenDanger;
        if (AfterRecoveryARController.Instance != null)
        {
            currentMode = AfterRecoveryARController.Instance.currentMissionMode;
        }

        // --- NEW FIX: If we changed modes, destroy the old house and allow spawning again! ---
        if (lastSpawnedMode != null && lastSpawnedMode.Value != currentMode)
        {
            missionPlaced = false;
            if (currentSpawnedRoom != null)
            {
                Destroy(currentSpawnedRoom);
            }
        }

        if (missionPlaced) return;

        // Select the correct prefab based on the mission mode
        GameObject prefabToSpawn = null;
        if (currentMode == MissionMode.HiddenDanger)
            prefabToSpawn = houseInteriorPrefab;
        else if (currentMode == MissionMode.CleanupGear)
            prefabToSpawn = cleanupGearPrefab;
        else if (currentMode == MissionMode.KitchenSafety)
            prefabToSpawn = kitchenSafetyPrefab; // Uses the new slot!

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"HiddenDangerSpawner: Prefab for {currentMode} is not set!");
            return;
        }

        // 1. Hide the old 3D World from the Inspector list (if assigned)
        foreach (GameObject obj in objectsToHideInAR)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"HiddenDangerSpawner: Successfully hid {obj.name} for AR mode.");
            }
        }

        // --- BULLETPROOF FAILSAFE ---
        GameObject oldHouse = GameObject.Find("HouseInterior");
        if (oldHouse != null)
        {
            oldHouse.SetActive(false);
            Debug.Log("HiddenDangerSpawner: Failsafe triggered - Auto-hid HouseInterior.");
        }

        GameObject playerBoy = GameObject.Find("Boy");
        if (playerBoy != null)
        {
            playerBoy.SetActive(false);
            Debug.Log("HiddenDangerSpawner: Failsafe triggered - Auto-hid Boy.");
        }
        // -----------------------------

        // 2. Spawn the correct AR House Environment (and save a reference to it)
        currentSpawnedRoom = Instantiate(prefabToSpawn, spawnPosition, prefabToSpawn.transform.rotation);
        
        if (arParent != null) 
            currentSpawnedRoom.transform.SetParent(arParent, true);

        // 3. Safely fix visuals
        FixSpawnedVisuals(currentSpawnedRoom);

        // 4. Automatically find all Hidden Dangers and FORCE them to be visible
        HiddenDangerItem[] dangersInRoom = currentSpawnedRoom.GetComponentsInChildren<HiddenDangerItem>(true);
        int dangerCount = 0;

        foreach (HiddenDangerItem danger in dangersInRoom)
        {
            danger.gameObject.SetActive(true);
            foreach (var rend in danger.GetComponentsInChildren<Renderer>(true))
            {
                if (rend.sharedMaterial != null) rend.enabled = true;
            }

            if (AfterRecoveryARController.Instance != null)
            {
                AfterRecoveryARController.Instance.RegisterSpawnedDanger(danger);
                dangerCount++;
            }
        }

        // 5. Force specific items on by exact name dynamically based on the mission mode!
        string[] exactNames;
        if (currentMode == MissionMode.HiddenDanger)
        {
            exactNames = new string[] { "idle rat", "snake", "bucket" };
        }
        else if (currentMode == MissionMode.CleanupGear)
        {
            exactNames = new string[] { "boots", "gloves", "mask", "bucket" }; 
        }
        else // Kitchen Safety Mode
        {
            // The exact names of your Kitchen items
            exactNames = new string[] { "canned goods", "sealed water", "open jar", "cardboard box" };
        }

        foreach (string itemName in exactNames)
        {
            // Improved search so it finds the items even if they are nested
            Transform item = null;
            foreach (Transform child in currentSpawnedRoom.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower() == itemName.ToLower())
                {
                    item = child;
                    break;
                }
            }

            if (item != null)
            {
                item.gameObject.SetActive(true);
                foreach (var rend in item.GetComponentsInChildren<Renderer>(true))
                {
                    rend.enabled = true;
                }
            }
        }

        missionPlaced = true;
        lastSpawnedMode = currentMode; // Save this mode so we know what to check against next time
        Debug.Log($"HiddenDangerSpawner: {currentMode} AR House spawned! Found {dangerCount} items inside.");
    }

    private void FixSpawnedVisuals(GameObject root)
    {
        if (root == null) return;

        int defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer < 0) defaultLayer = 0;

        ApplyHierarchyState(root.transform, defaultLayer);

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            renderer.gameObject.layer = defaultLayer;
        }

        foreach (var lodGroup in root.GetComponentsInChildren<LODGroup>(true))
        {
            lodGroup.ForceLOD(0);
            lodGroup.enabled = false;
        }
    }

    private void ApplyHierarchyState(Transform node, int layer)
    {
        if (node == null) return;
        node.gameObject.layer = layer;
        for (int i = 0; i < node.childCount; i++)
        {
            ApplyHierarchyState(node.GetChild(i), layer);
        }
    }
}