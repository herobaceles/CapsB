using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DraggableItem : MonoBehaviour
{
    [HideInInspector] public ARRaycastManager raycastManager;
    [HideInInspector] public Camera arCamera;

    private Rigidbody _rb;
    private bool _dragging;

    private static readonly List<ARRaycastHit> Hits = new();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    private void Update()
    {
        if (raycastManager == null || arCamera == null)
            return;

        Vector2? screenPos = GetPointerScreenPosition();

        if (screenPos.HasValue)
        {
            if (!_dragging)
            {
                TryBeginDrag(screenPos.Value);
            }
            else
            {
                Drag(screenPos.Value);
            }
        }
        else
        {
            _dragging = false;
        }
    }

    // ================= INPUT =================

    private Vector2? GetPointerScreenPosition()
    {
        // Mouse (Editor / XR Simulation)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return Mouse.current.position.ReadValue();

        // Touch (Mobile)
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return null;
    }

    // ================= DRAG =================

    private void TryBeginDrag(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                _dragging = true;
            }
        }
    }

    private void Drag(Vector2 screenPos)
    {
        if (!raycastManager.Raycast(screenPos, Hits, TrackableType.PlaneWithinPolygon))
            return;

        Pose pose = Hits[0].pose;
        transform.position = pose.position;
    }
}
