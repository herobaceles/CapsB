using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Task-bound spawner for After-phase AR recovery sessions.
///
/// Instantiates hidden danger / mud pile prefabs at designated spawn points and tracks
/// how many the player has resolved. When all required dangers are cleared, reports
/// completion to AfterRecoveryARController, which in turn notifies AfterMissionManager.
///
/// Activated by AfterRecoveryARController — never ticks independently.
/// Does not talk to MissionSceneManager directly; mission progression is owned
/// exclusively by AfterMissionManager.
/// </summary>
public class HiddenDangerSpawner : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector fields
    // -----------------------------------------------------------------------

    [Header("Spawn Configuration")]
    [SerializeField] private GameObject[] dangerPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Objective Tracking")]
    [Tooltip("Must match an ObjectiveData.objectiveId defined in the active MissionData task.")]
    [SerializeField] private string objectiveId;
    [Tooltip("Required clears to finish the task. 0 = use spawnPoints.Length automatically.")]
    [SerializeField] private int requiredCount;

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private int foundCount;
    private bool sessionActive;

    // -----------------------------------------------------------------------
    // Public API (called by AfterRecoveryARController)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Instantiates danger prefabs at all spawn points and begins tracking interactions.
    /// </summary>
    public void StartSpawning()
    {
        if (sessionActive)
        {
            Debug.LogWarning("HiddenDangerSpawner: StartSpawning called while a session is already active.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("HiddenDangerSpawner: No spawn points assigned.");
            return;
        }

        if (dangerPrefabs == null || dangerPrefabs.Length == 0)
        {
            Debug.LogError("HiddenDangerSpawner: No danger prefabs assigned.");
            return;
        }

        sessionActive = true;
        foundCount = 0;
        spawnedObjects.Clear();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;

            GameObject prefab = dangerPrefabs[i % dangerPrefabs.Length];
            if (prefab == null) continue;

            GameObject spawned = Instantiate(prefab, spawnPoints[i].position, spawnPoints[i].rotation);
            spawnedObjects.Add(spawned);

            // Hook up any supported interactable types so they
            // can report back when resolved.
            var hiddenItem = spawned.GetComponent<HiddenDangerItem>();
            if (hiddenItem != null)
            {
                hiddenItem.OnRecovered += OnHiddenDangerItemRecovered;
            }

            var mudPile = spawned.GetComponent<MudPileInteraction>();
            if (mudPile != null)
            {
                mudPile.OnCleaned += OnMudPileCleaned;
            }
        }

        if (requiredCount <= 0)
            requiredCount = spawnedObjects.Count;

        Debug.Log($"HiddenDangerSpawner: Spawned {spawnedObjects.Count} danger object(s). Required to clear: {requiredCount}.");
    }

    /// <summary>
    /// Destroys all spawned objects and resets session state.
    /// Called by AfterRecoveryARController.DisableAR().
    /// </summary>
    public void StopSpawning()
    {
        sessionActive = false;

        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedObjects.Clear();
        Debug.Log("HiddenDangerSpawner: Session stopped and spawned objects destroyed.");
    }

    // -----------------------------------------------------------------------
    // Event handlers from spawned interactables
    // -----------------------------------------------------------------------

    private void OnHiddenDangerItemRecovered(HiddenDangerItem item)
    {
        OnDangerFound();
    }

    private void OnMudPileCleaned(MudPileInteraction mud)
    {
        OnDangerFound();
    }

    // -----------------------------------------------------------------------
    // Called by spawned MudPileInteraction instances
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reports that a spawned danger has been resolved by the player.
    /// Forwards progress to AfterRecoveryARController, which decides when to
    /// end the AR session.
    /// </summary>
    public void OnDangerFound()
    {
        if (!sessionActive) return;

        foundCount++;
        Debug.Log($"HiddenDangerSpawner: Danger cleared {foundCount}/{requiredCount}.");

        AfterRecoveryARController.Instance?.OnHiddenDangerCleared(foundCount, requiredCount);
    }
}
