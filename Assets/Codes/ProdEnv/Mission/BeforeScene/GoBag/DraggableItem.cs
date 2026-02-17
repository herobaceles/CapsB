
using UnityEngine;
using UnityEngine.InputSystem;

public class DraggableItem : MonoBehaviour
{
    // Table bounds for clamping
    public Transform tableTransform;
    public Vector3 tableSize = new Vector3(1f, 0.1f, 2f); // Set in inspector or at runtime
    private Vector3 offset;
    private Camera arCamera;
    private bool isDragging = false;
    private int draggingFingerId = -1;
    private Rigidbody rb;

    void Start()
    {
        arCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        if (arCamera == null)
        {
            Debug.LogError("DraggableItem: No camera tagged as MainCamera found!");
        }
    }

    void OnMouseDown()
    {
        // No longer used with new Input System
    }

    void OnMouseDrag()
    {
        // No longer used with new Input System
    }

    void OnMouseUp()
    {
        // No longer used with new Input System
    }

    Vector3 GetMouseWorldPos()
    {
        // No longer used with new Input System
        return Vector3.zero;
    }

    void Update()
    {
        if (arCamera == null)
            return;
        // Mouse drag (Editor/Desktop)
        if (Mouse.current != null)
        {
            var mouse = Mouse.current;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                TryBeginDrag(mouse.position.ReadValue(), -1);
            }
            else if (mouse.leftButton.isPressed && isDragging && draggingFingerId == -1)
            {
                DragTo(mouse.position.ReadValue());
            }
            else if (mouse.leftButton.wasReleasedThisFrame && isDragging && draggingFingerId == -1)
            {
                EndDrag();
            }
        }

        // Touch drag (Mobile)
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    TryBeginDrag(touch.position.ReadValue(), touch.touchId.ReadValue());
                }
                else if (touch.press.isPressed && isDragging && draggingFingerId == touch.touchId.ReadValue())
                {
                    DragTo(touch.position.ReadValue());
                }
                else if (touch.press.wasReleasedThisFrame && isDragging && draggingFingerId == touch.touchId.ReadValue())
                {
                    EndDrag();
                }
            }
        }
    }

    void TryBeginDrag(Vector2 screenPos, int fingerId)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log($"DraggableItem: Raycast hit {hit.transform.name}");
            if (hit.transform == transform)
            {
                // Use the table's surface as the drag plane
                Vector3 tableUp = tableTransform != null ? tableTransform.up : Vector3.up;
                Plane dragPlane = new Plane(tableUp, tableTransform != null ? tableTransform.position : transform.position);
                float enter = 0f;
                if (dragPlane.Raycast(ray, out enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    // Only offset Y (vertical) so the item doesn't jump
                    offset = transform.position - new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
                }
                else
                {
                    offset = Vector3.zero;
                }
                isDragging = true;
                draggingFingerId = fingerId;
                Debug.Log("DraggableItem: Begin drag");
            }
        }
        else
        {
            Debug.Log("DraggableItem: Raycast did not hit any object");
        }
    }

    void DragTo(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        // Use the table's surface as the drag plane
        Vector3 tableUp = tableTransform != null ? tableTransform.up : Vector3.up;
        Plane dragPlane = new Plane(tableUp, tableTransform != null ? tableTransform.position : transform.position);
        float enter = 0f;
        if (dragPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            // Keep the dragged object's Y (vertical) position unless dragging vertically
            Vector3 targetPos = new Vector3(hitPoint.x + offset.x, hitPoint.y + offset.y, hitPoint.z + offset.z);

            // Clamp to table bounds if tableTransform is set
            if (tableTransform != null)
            {
                Vector3 local = tableTransform.InverseTransformPoint(targetPos);
                Vector3 half = tableSize * 0.5f;
                local.x = Mathf.Clamp(local.x, -half.x, half.x);
                local.z = Mathf.Clamp(local.z, -half.z, half.z);
                local.y = Mathf.Clamp(local.y, 0f, tableSize.y + 0.5f);
                targetPos = tableTransform.TransformPoint(local);
            }

            if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(targetPos);
            }
            else
            {
                transform.position = targetPos;
            }
            Debug.Log($"DraggableItem: Dragging to {targetPos}");
        }
    }

    void EndDrag()
    {
        isDragging = false;
        draggingFingerId = -1;
        Debug.Log("DraggableItem: End drag");
    }
    }
    