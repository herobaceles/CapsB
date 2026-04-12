using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoBagDropZone : MonoBehaviour
{
    // Detect when a draggable item enters the bag's trigger
    private void OnTriggerEnter(Collider other)
    {
        // Mark draggable items as being inside the bag zone; actual
        // collection is handled when the drag ends on the item.
        var draggable = GetDraggableItem(other);
        if (draggable != null)
        {
            draggable.SetInsideBagZone(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Clear the inside-bag flag when the item leaves the zone.
        var draggable = GetDraggableItem(other);
        if (draggable != null)
        {
            draggable.SetInsideBagZone(false);
        }
    }

    private DraggableItem GetDraggableItem(Collider other)
    {
        if (other == null)
            return null;

        // Prefer the Rigidbody root if present, since items typically
        // have both Rigidbody and collider on the same GameObject.
        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.GetComponent<DraggableItem>();
        }

        return other.GetComponent<DraggableItem>();
    }
}
