using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ApplianceSecureItem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string applianceName = "Appliance";
    [SerializeField] private float requiredFloodHeight = 0.8f;

    [Header("Visuals")]
    [SerializeField] private Renderer[] outlineRenderers;
    [SerializeField] private Color unsafeColor = Color.red;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color safeColor = Color.green;

    [Header("Events")]
    public UnityAction OnSecuredChanged;
    public UnityAction OnIllegalMove;

    private bool isSelected;
    private bool lastSecuredState;
    private ApplianceElevatedArea currentArea;

    public string ApplianceName => string.IsNullOrWhiteSpace(applianceName) ? name : applianceName;
    public ApplianceElevatedArea CurrentArea => currentArea;
    public bool IsSecured { get; private set; }
    public bool IsUnplugged => true;

    private void Start()
    {
        EvaluateSecure();
        UpdateVisual();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisual();
    }

    public bool PlaceOnArea(ApplianceElevatedArea area)
    {
        if (area == null)
            return false;

        bool placed = area.Place(this);
        if (!placed)
            OnIllegalMove?.Invoke();

        return placed;
    }

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    public void AssignArea(ApplianceElevatedArea area)
    {
        currentArea = area;
        EvaluateSecure();
    }

    public void SetRequiredFloodHeight(float height)
    {
        requiredFloodHeight = height;
        EvaluateSecure();
    }

    private void EvaluateSecure()
    {
        bool securedNow = currentArea != null;
        IsSecured = securedNow;

        if (securedNow != lastSecuredState)
        {
            lastSecuredState = securedNow;
            OnSecuredChanged?.Invoke();
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        Color color = unsafeColor;
        if (IsSecured)
            color = safeColor;
        else if (isSelected)
            color = selectedColor;

        if (outlineRenderers == null)
            return;

        foreach (var rendererRef in outlineRenderers)
        {
            if (rendererRef == null || rendererRef.material == null)
                continue;

            rendererRef.material.color = color;
        }
    }
}
