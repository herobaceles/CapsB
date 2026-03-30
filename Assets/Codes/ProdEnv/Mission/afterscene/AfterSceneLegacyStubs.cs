using UnityEngine;

/// <summary>
/// Lightweight placeholder implementations for legacy AfterScene manager types
/// referenced by AfterSceneController. These exist only to satisfy compile-time
/// references; the new After-mission flow is driven by AfterMissionManager and
/// AfterRecoveryARController.
/// </summary>
public class AfterSceneARManager : MonoBehaviour
{
    public Camera GetCurrentARCamera()
    {
        return null;
    }

    public void StartARForMode(MissionMode mode)
    {
    }

    public void EndARForMode(MissionMode mode)
    {
    }
}

public class AfterSceneMissionTracker : MonoBehaviour
{
    public void HandleDangerRecovered(HiddenDangerItem item)
    {
    }

    public void HandleGenericItemRecovered(GameObject obj)
    {
    }

    public void RecalculateProgress(MissionMode mode)
    {
    }

    public bool IsMissionComplete(MissionMode mode)
    {
        return false;
    }
}

public class AfterSceneDialogueManager : MonoBehaviour
{
}

public class AfterSceneUIManager : MonoBehaviour
{
    [Header("Feedback Icons")]
    [SerializeField] private GameObject greenCheckPrefab;
    [SerializeField] private GameObject redCrossPrefab;

    [Tooltip("Optional parent for spawned feedback icons (e.g. a world-space canvas or AR root). If null, icons are spawned at the scene root.")]
    [SerializeField] private Transform worldSpaceRoot;

    [Tooltip("How long the feedback icon should remain visible before being destroyed.")]
    [SerializeField] private float iconLifetimeSeconds = 0.75f;

    public void ShowFeedbackIconAtWorldPosition(bool isCorrect, Vector3 worldPosition, Camera cam)
    {
        GameObject prefab = isCorrect ? greenCheckPrefab : redCrossPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("AfterSceneUIManager: Feedback prefab is not assigned (" + (isCorrect ? "greenCheck" : "redCross") + ").");
            return;
        }

        Transform parent = worldSpaceRoot != null ? worldSpaceRoot : null;
        GameObject iconInstance = Instantiate(prefab, worldPosition, Quaternion.identity, parent);

        // If a camera is provided, orient the icon to face it so it is
        // clearly visible in AR. This assumes the prefab faces forward
        // along its -Z or +Z axis; adjust as needed in the prefab.
        if (cam != null)
        {
            iconInstance.transform.LookAt(cam.transform);
        }

        if (iconLifetimeSeconds > 0f)
        {
            Destroy(iconInstance, iconLifetimeSeconds);
        }
    }
}

public class AfterSceneItemSpawner : MonoBehaviour
{
}
