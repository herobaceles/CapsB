using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight wrapper that runs during the "in-scene" continuation after Level Complete.
/// It re-uses public APIs from `BeforeSceneManager` rather than duplicating resources.
/// Attach this to a GameObject in the same scene (e.g., an empty "DuringGameManager").
/// </summary>
public class DuringGameManager : MonoBehaviour
{
    private BeforeSceneManager before;

    private void Awake()
    {
        before = BeforeSceneManager.Instance;
        if (before == null)
        {
            Debug.LogWarning("[DuringGameManager] BeforeSceneManager.Instance is null. Ensure BeforeSceneManager exists in scene.");
        }
    }

    // Public helper to continue in-scene (no scene unload)
    public void ContinueInScene()
    {
        Debug.Log("[DuringGameManager] ContinueInScene called.");
        // Nothing extra required here; gameplay UI and player movement are managed by BeforeSceneManager completion sequence.
    }

    // Return to main menu via the existing manager
    public void ReturnToMainMenu()
    {
        if (before != null)
        {
            before.GoToMainMenu();
        }
        else
        {
            // Fallback
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Convenience wrappers for other BeforeSceneManager public actions
    public void TogglePause() { before?.TogglePause(); }
    public void Pause() { before?.PauseGame(); }
    public void Resume() { before?.ResumeGame(); }
}
