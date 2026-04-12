
using UnityEngine;
using UnityEngine.InputSystem;

public class DraggableItem : MonoBehaviour
{
    // Table bounds for clamping
    public Transform tableTransform;
    public Vector3 tableSize = new Vector3(1f, 0.1f, 2f); // Set in inspector or at runtime
    [Tooltip("Minimum world-space movement required for a drag to count as a drop.")]
    public float minDropDistance = 0.05f;
    private Vector3 offset;
    private Camera arCamera;
    private bool isDragging = false;
    private int draggingFingerId = -1;
    private Rigidbody rb;

    // Remember the height at which dragging started so movement stays horizontal.
    private float initialWorldY;
    private float initialLocalY;

    // Record where the drag started so we can ignore simple taps
    // or micro-movements when deciding whether to collect.
    private Vector3 dragStartWorldPos;

    // Whether this item is currently inside the Go Bag's drop zone.
    private bool isInsideBagZone = false;

    // Expose drag state for other systems (e.g., debugging or future logic).
    public bool IsDragging => isDragging;

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

    /// <summary>
    /// Called by the GoBagDropZone trigger to indicate whether this
    /// item is currently overlapping the bag's drop area.
    /// </summary>
    /// <param name="inside">True if inside the bag zone; false otherwise.</param>
    public void SetInsideBagZone(bool inside)
    {
        isInsideBagZone = inside;
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
                initialWorldY = transform.position.y;
                if (tableTransform != null)
                    initialLocalY = tableTransform.InverseTransformPoint(transform.position).y;

                // Use the table's surface as the drag plane
                Vector3 tableUp = tableTransform != null ? tableTransform.up : Vector3.up;
                Plane dragPlane = new Plane(tableUp, transform.position);
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
                dragStartWorldPos = transform.position;
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
        Plane dragPlane = new Plane(tableUp, transform.position);
        float enter = 0f;
        if (dragPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            // Move in X/Z based on hit point, but keep Y fixed at the
            // height where the drag began so the item only moves horizontally.
            Vector3 targetPos = new Vector3(hitPoint.x + offset.x, initialWorldY, hitPoint.z + offset.z);

            // If you later want to keep items strictly on the table,
            // you can reintroduce clamping here using tableSize.

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

        // Only treat this as a valid drop if the item is currently
        // inside the Go Bag's drop zone *and* the player has actually
        // dragged it a meaningful distance (not just a tap/hold).
        if (isInsideBagZone && ARMissionManager.Instance != null)
        {
            float sqrDist = (transform.position - dragStartWorldPos).sqrMagnitude;
            if (sqrDist >= minDropDistance * minDropDistance)
            {
                ARMissionManager.Instance.OnItemDroppedInBag(gameObject);
            }
        }
    }
    }
    