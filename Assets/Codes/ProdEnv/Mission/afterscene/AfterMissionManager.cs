using UnityEngine;

/// <summary>
/// Manages the overall After Recovery Phase state.
/// Implements a singleton pattern for easy access from other scripts.
/// </summary>
public class AfterMissionManager : MonoBehaviour
{
    // Singleton instance
    public static AfterMissionManager Instance { get; private set; }

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // The dialog system will handle the start of the scene.
        // The quizzes are safely started by walking into the QuizZoneTrigger.
        // The AR is now triggered dynamically (either by walking trigger or directly from the QuizManager).
        Debug.Log("AfterMissionManager: Scene Started. Waiting for player to trigger zones or quizzes.");
    }

    /// <summary>
    /// Called by the AfterRecoveryARController when all AR items (dangers or cleanup gear) are recovered.
    /// </summary>
    public void CompleteRecoveryMission()
    {
        Debug.Log("AfterMissionManager: AR Recovery mission complete! All required items found.");
        
        // --- NEW SAFETY FIX ---
        // Clear the saved mission ID so it doesn't get stuck if the player restarts the scene directly!
        if (PlayerPrefs.HasKey("SelectedMissionID"))
        {
            PlayerPrefs.DeleteKey("SelectedMissionID");
            PlayerPrefs.Save();
        }
        
        // TODO: Show final victory UI, update save data, or load the next scene here!
    }
}