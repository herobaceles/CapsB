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

    private bool IsInDisinfectHouseAR()
    {
        var afterAR = AfterRecoveryARController.Instance;
        return afterAR != null && afterAR.IsARActive && afterAR.CurrentMissionMode == MissionMode.DisinfectHouse;
    }

    private void OnEnable()
    {
        // Ensure this helper only runs while the legacy After
        // recovery AR controller is active in DisinfectHouse mode.
        // This prevents the spray bottle / cleaning rag from
        // leaking into other missions or AR sessions that also
        // use ARRuntimeContext / ARCamera.
        if (!IsInDisinfectHouseAR())
        {
            // Immediately disable this object so it does not
            // remain floating in front of a non-After AR camera
            // or in normal gameplay scenes.
            gameObject.SetActive(false);
            return;
        }

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

    private void Update()
    {
        // Safety net: if AR is no longer active in DisinfectHouse
        // mode (e.g. scene changed, mission switched, AR stopped),
        // force-disable this item so it cannot stay visible in
        // other missions or UI screens.
        if (gameObject.activeSelf && !IsInDisinfectHouseAR())
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // In edit-time hierarchy changes Unity may call OnDisable while
        // objects are already in an activation/deactivation pass. Guard
        // with Application.isPlaying to avoid warnings when changing
        // scenes or toggling objects in the editor.
        if (!Application.isPlaying)
            return;

        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
        }
    }
}
