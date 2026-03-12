using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiddenDangerSpawner : MonoBehaviour
{
    [Header("AR Mission Prefabs")]
    public GameObject cleanupGearPrefab;  
    public GameObject houseInteriorPrefab; 
    public GameObject kitchenSafetyPrefab; 
    public GameObject disinfectHousePrefab; 

    [Header("Spawn Settings")]
    public Transform arParent;

    [Header("Transition Settings")]
    public GameObject[] objectsToHideInAR;

    private bool missionPlaced = false;
    private MissionMode? lastSpawnedMode = null;
    private GameObject currentSpawnedRoom;

    public void SpawnHiddenDangers(Vector3 spawnPosition)
    {
        MissionMode currentMode = MissionMode.HiddenDanger;
        if (AfterRecoveryARController.Instance != null)
            currentMode = AfterRecoveryARController.Instance.currentMissionMode;

        if (lastSpawnedMode != null && lastSpawnedMode.Value != currentMode)
        {
            missionPlaced = false;
            if (currentSpawnedRoom != null) Destroy(currentSpawnedRoom);
        }

        if (missionPlaced) return;

        GameObject prefabToSpawn = null;
        switch (currentMode)
        {
            case MissionMode.CleanupGear: prefabToSpawn = cleanupGearPrefab; break;
            case MissionMode.HiddenDanger: prefabToSpawn = houseInteriorPrefab; break;
            case MissionMode.KitchenSafety: prefabToSpawn = kitchenSafetyPrefab; break;
            case MissionMode.DisinfectHouse: prefabToSpawn = disinfectHousePrefab; break;
        }

        if (prefabToSpawn == null) return;

        foreach (GameObject obj in objectsToHideInAR) if (obj != null) obj.SetActive(false);

        currentSpawnedRoom = Instantiate(prefabToSpawn, spawnPosition, prefabToSpawn.transform.rotation);
        if (arParent != null) currentSpawnedRoom.transform.SetParent(arParent, true);

        // Fix Visuals
        foreach (var rend in currentSpawnedRoom.GetComponentsInChildren<Renderer>(true))
        {
            rend.enabled = true;
            rend.gameObject.layer = LayerMask.NameToLayer("Default");
        }

        // Register Items - IMPORTANT: This registers all items in the spawned room
        HiddenDangerItem[] items = currentSpawnedRoom.GetComponentsInChildren<HiddenDangerItem>(true);
        Debug.Log($"Spawned {items.Length} items for mode {currentMode}");
        
        foreach (var item in items)
        {
            item.gameObject.SetActive(true);
            if (AfterRecoveryARController.Instance != null)
            {
                AfterRecoveryARController.Instance.RegisterSpawnedDanger(item);
                Debug.Log($"Registered item: {item.gameObject.name} with tag {item.gameObject.tag}");
            }
        }

        missionPlaced = true;
        lastSpawnedMode = currentMode;
    }
}