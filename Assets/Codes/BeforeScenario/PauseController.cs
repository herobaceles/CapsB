using UnityEngine;

public class PauseController : MonoBehaviour
{
    private bool isPaused;
    private float cachedTimeScale = 1f;

    public void PauseGame()
    {
        if (isPaused) return;
        cachedTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        Time.timeScale = cachedTimeScale <= 0f ? 1f : cachedTimeScale;
        isPaused = false;
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame(); else PauseGame();
    }
}
