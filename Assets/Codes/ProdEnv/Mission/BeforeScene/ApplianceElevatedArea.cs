using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ApplianceElevatedArea : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private string areaName = "Elevated Area";
    [SerializeField] private Transform snapPoint;

    private ApplianceSecureItem currentOccupant;

    public string AreaName => string.IsNullOrWhiteSpace(areaName) ? name : areaName;
    public ApplianceSecureItem CurrentOccupant => currentOccupant;
    public bool IsOccupied => currentOccupant != null;

    public bool IsOccupiedByOther(ApplianceSecureItem item)
    {
        return currentOccupant != null && currentOccupant != item;
    }

    public bool CanAccept(ApplianceSecureItem item)
    {
        if (item == null)
            return false;

        return currentOccupant == null || currentOccupant == item;
    }

    public bool Place(ApplianceSecureItem item)
    {
        if (!CanAccept(item))
            return false;

        if (item.CurrentArea != null && item.CurrentArea != this)
            item.CurrentArea.Clear(item);

        GetPlacementPose(out Vector3 targetPosition, out Quaternion targetRotation);
        item.PlaceAt(targetPosition, targetRotation);

        currentOccupant = item;
        item.AssignArea(this);
        return true;
    }

    public void Clear(ApplianceSecureItem item = null)
    {
        if (currentOccupant == null)
            return;

        if (item != null && currentOccupant != item)
            return;

        var previous = currentOccupant;
        currentOccupant = null;
        previous.AssignArea(null);
    }

    private void GetPlacementPose(out Vector3 position, out Quaternion rotation)
    {
        var target = snapPoint != null ? snapPoint : transform;
        position = target.position;
        rotation = target.rotation;
    }
}