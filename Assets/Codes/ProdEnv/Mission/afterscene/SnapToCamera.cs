using UnityEngine;

public class SnapToCamera : MonoBehaviour
{
    [Header("Position on Screen")]
    [Tooltip("Adjust this to move the item Left/Right, Up/Down, and Forward from the camera.")]
    public Vector3 offsetPosition = new Vector3(0.3f, -0.3f, 0.6f); 
    
    [Tooltip("Adjust this to rotate the item so it faces the right way.")]
    public Vector3 offsetRotation = new Vector3(0, 0, 0);

    private void OnEnable()
    {
        // 1. Find the AR Camera (which is tagged as MainCamera in your Boot scene)
        Camera arCam = Camera.main; 

        // --- NEW FIX: Fallback if the MainCamera tag is missing ---
        if (arCam == null)
        {
            arCam = FindObjectOfType<Camera>();
        }

        if (arCam != null)
        {
            // 2. Make this object a child of the AR Camera
            transform.SetParent(arCam.transform);

            // 3. Set its position and rotation so it sits exactly where we want on the screen
            transform.localPosition = offsetPosition;
            transform.localEulerAngles = offsetRotation;
        }
        else
        {
            Debug.LogError("SnapToCamera: Could not find any Camera!");
        }
    }
}