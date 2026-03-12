using UnityEngine;

public class AfterMissionManager : MonoBehaviour
{
    public static AfterMissionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void CompleteRecoveryMission()
    {
        Debug.Log("Mission Complete Logic Goes Here!");
        // Your actual manager logic goes here...
    }
}