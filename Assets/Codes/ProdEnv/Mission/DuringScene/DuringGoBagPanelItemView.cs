using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DuringGoBagPanelItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject collectedMarker;
    [SerializeField] private Color collectedColor = Color.green;
    [SerializeField] private Color pendingColor = Color.white;

    private static readonly string[] iconKeywords = { "icon", "thumbnail", "item" };
    private static readonly string[] markerKeywords = { "check", "marker", "status" };
    private bool missingReferenceLogged;

    private void Awake()
    {
        EnsureReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            EnsureReferences();
    }
#endif

    public void Bind(GoBagItemSnapshot data)
    {
        EnsureReferences();

        if (nameLabel != null)
        {
            nameLabel.text = string.IsNullOrEmpty(data.ItemName) ? "Unknown Item" : data.ItemName;
            nameLabel.color = data.IsCollected ? collectedColor : pendingColor;
        }

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }

        if (collectedMarker != null)
            collectedMarker.SetActive(data.IsCollected);
    }

    public bool TryGetLabelText(out string text)
    {
        EnsureReferences();
        if (nameLabel != null)
        {
            text = nameLabel.text;
            return true;
        }
        text = null;
        return false;
    }

    private void EnsureReferences()
    {
        if (nameLabel == null)
            nameLabel = GetComponentInChildren<TMP_Text>(true);

        if (iconImage == null)
            iconImage = FindIconImage();

        if (collectedMarker == null)
            collectedMarker = FindMarkerObject();

        if (!missingReferenceLogged && nameLabel == null)
        {
            missingReferenceLogged = true;
            Debug.LogWarning($"{nameof(DuringGoBagPanelItemView)} on {name} is missing a text label reference. Assign it in the prefab to control the exact visuals.", this);
        }
    }

    private Image FindIconImage()
    {
        var images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            var candidate = images[i];
            if (candidate == null)
                continue;

            var lower = candidate.name.ToLowerInvariant();
            for (int k = 0; k < iconKeywords.Length; k++)
            {
                if (lower.Contains(iconKeywords[k]))
                    return candidate;
            }
        }

        for (int i = 0; i < images.Length; i++)
        {
            var fallback = images[i];
            if (fallback != null && fallback.gameObject != gameObject)
                return fallback;
        }

        return null;
    }

    private GameObject FindMarkerObject()
    {
        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            var candidate = transforms[i];
            if (candidate == null || candidate == transform)
                continue;

            var lower = candidate.name.ToLowerInvariant();
            for (int k = 0; k < markerKeywords.Length; k++)
            {
                if (lower.Contains(markerKeywords[k]))
                    return candidate.gameObject;
            }
        }

        return null;
    }
}
