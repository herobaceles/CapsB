using UnityEngine;

public class DisinfectHouseTrigger : MonoBehaviour
{
    [Tooltip("Turn this off if you don't want the trigger to disappear after being hit.")]
    public bool disableAfterTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        // This will tell us if the trigger is working at all, and what exactly touched it!
        Debug.Log($"DisinfectHouseTrigger touched by: {other.gameObject.name} (Tag: {other.tag})");

        // Check if the object walking into the cube is the Player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Success! Player entered the Disinfect House Trigger!");

            // Tell the AR Controller to start the Disinfect House AR Mode
            if (AfterRecoveryARController.Instance != null)
            {
                AfterRecoveryARController.Instance.StartDisinfectHouseAR();
            }
            else
            {
                Debug.LogError("DisinfectHouseTrigger couldn't find AfterRecoveryARController.Instance!");
            }

            // Hide this cube so the player can't trigger it twice by accident
            if (disableAfterTrigger)
            {
                gameObject.SetActive(false);
            }
        }
    }

}