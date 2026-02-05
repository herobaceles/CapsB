using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARController : MonoBehaviour
{
    [Header("AR Components")]
    public Camera arCamera;
    public AudioListener arAudioListener;
    public ARPlaneManager arPlaneManager;
    public ARRaycastManager arRaycastManager;
    public GameObject arGameRoot;

    public bool IsAROpen { get; private set; }
    public bool MissionCompleted { get; private set; }

    public void GateARUntilTrigger()
    {
        if (arGameRoot != null) arGameRoot.SetActive(false);

        if (arCamera != null)
        {
            arCamera.enabled = false;
            arCamera.gameObject.SetActive(false);
        }

        if (arAudioListener != null) arAudioListener.enabled = false;

        if (arPlaneManager != null) arPlaneManager.enabled = false;
        if (arRaycastManager != null) arRaycastManager.enabled = false;
    }

    public void StartAR()
    {
        IsAROpen = true;
        if (arCamera != null)
        {
            arCamera.gameObject.SetActive(true);
            arCamera.enabled = true;
        }
        if (arAudioListener != null) arAudioListener.enabled = true;
        if (arPlaneManager != null) arPlaneManager.enabled = true;
        if (arRaycastManager != null) arRaycastManager.enabled = true;
        if (arGameRoot != null) arGameRoot.SetActive(true);
    }

    public void StopAR()
    {
        IsAROpen = false;
        if (arPlaneManager != null) arPlaneManager.enabled = false;
        if (arRaycastManager != null) arRaycastManager.enabled = false;
        if (arCamera != null)
        {
            arCamera.enabled = false;
            arCamera.gameObject.SetActive(false);
        }
        if (arAudioListener != null) arAudioListener.enabled = false;
        if (arGameRoot != null) arGameRoot.SetActive(false);
    }
}
