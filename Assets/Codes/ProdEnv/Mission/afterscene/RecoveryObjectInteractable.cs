using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RecoveryObjectInteractable : MonoBehaviour
{
    [Header("Note: Use HiddenDangerItem.cs instead for AR Mission!")]
    [SerializeField] private AfterRecoveryARController controller;

    /// <summary>
    /// Called when the player taps the object in AR.
    /// </summary>
    public void Recover()
    {
        // We removed the old ReportRecovered() method. 
        // If this object needs to count towards the mission, replace this script 
        // on your prefab with the new "HiddenDangerItem.cs" script.
        Debug.Log($"RecoveryObjectInteractable ({name}): Recovered! (Legacy script triggered)");

        // Hide the object
        gameObject.SetActive(false);
    }
}