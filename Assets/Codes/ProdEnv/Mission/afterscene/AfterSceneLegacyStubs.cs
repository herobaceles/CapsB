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
    public void ShowFeedbackIconAtWorldPosition(bool isCorrect, Vector3 worldPosition, Camera cam)
    {
    }
}

public class AfterSceneItemSpawner : MonoBehaviour
{
}
