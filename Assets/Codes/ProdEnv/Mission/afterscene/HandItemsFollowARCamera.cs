using UnityEngine;

/// <summary>
/// Keeps this object (e.g. spray bottle or cleaning rag) attached
/// to the active AR camera with a configurable local offset.
/// Attach this to the hand-held props in the DisinfectHouse AR scene.
/// </summary>
public class HandItemsFollowARCamera : MonoBehaviour
{
    [Header("Position Relative To AR Camera")]
    [Tooltip("Local position relative to the AR camera.")]
    public Vector3 localPosition = new Vector3(0.25f, -0.2f, 0.5f);

    [Tooltip("Local rotation relative to the AR camera.")]
    public Vector3 localEulerAngles = Vector3.zero;

    private Transform originalParent;

    private void OnEnable()
    {
        originalParent = transform.parent;

        Camera arCamera = null;
        if (ARRuntimeContext.Instance != null)
        {
            arCamera = ARRuntimeContext.Instance.ResolveARCamera();
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
            Debug.LogWarning("HandItemsFollowARCamera: No AR or main camera found. Cannot attach hand item.");
            return;
        }

        transform.SetParent(arCamera.transform, false);
        transform.localPosition = localPosition;
        transform.localEulerAngles = localEulerAngles;
    }

    private void OnDisable()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
        }
    }
}
