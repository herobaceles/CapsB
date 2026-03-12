using UnityEngine;

public class QuizZoneTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string playerTag = "Player";
    
    [Header("UI to Start")]
    [Tooltip("Drag StructuraldamageQuiz here")]
    public GameObject firstQuizPanel; 

    private bool hasTriggered = false;

    private void Start()
    {
        // Fixes the Auto-Start bug: Forces the first quiz to be hidden 
        // the moment the scene loads so it cannot break the sequence.
        if (firstQuizPanel != null)
        {
            firstQuizPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger if it's the player and hasn't triggered yet
        if (!hasTriggered && other.CompareTag(playerTag))
        {
            if (firstQuizPanel != null)
            {
                firstQuizPanel.SetActive(true);
                hasTriggered = true;
                
                // Hide the Cube so the player knows the 'mission' started
                if (GetComponent<MeshRenderer>() != null)
                {
                    GetComponent<MeshRenderer>().enabled = false; 
                }
                
                Debug.Log("First Quiz Started: Structural Damage");
            }
        }
    }
}