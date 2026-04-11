using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the backpack overlay that lists go-bag contents during the
/// response phase. Includes map access for tutorial and navigation.
/// </summary>
public class DuringGoBagPanel : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform listContainer;
    [SerializeField] private GameObject itemEntryPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI emptyStateLabel;

    [Header("Map Access")]
    [SerializeField] private Button viewMapButton;
    [SerializeField] private DuringMissionMapDisplay mapDisplay;

    private readonly List<GoBagItemSnapshot> snapshotBuffer = new List<GoBagItemSnapshot>();
    private readonly List<DuringGoBagPanelItemView> pooledViews = new List<DuringGoBagPanelItemView>();

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);

        if (viewMapButton != null)
            viewMapButton.onClick.AddListener(OnViewMapClicked);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePanel);

        if (viewMapButton != null)
            viewMapButton.onClick.RemoveListener(OnViewMapClicked);
    }

    public void TogglePanel()
    {
        if (IsVisible)
            HidePanel();
        else
            ShowPanel();
    }

    public void ShowPanel()
    {
        if (panelRoot == null || listContainer == null || itemEntryPrefab == null)
        {
            Debug.LogWarning("DuringGoBagPanel: Missing UI references.");
            return;
        }

        EnsureCanvasGroup(panelRoot);
        RefreshList();
        panelRoot.SetActive(true);
        Debug.Log("DuringGoBagPanel: Opened backpack panel.");
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            Debug.Log("DuringGoBagPanel: Closed backpack panel.");
        }
    }

    private void OnViewMapClicked()
    {
        // Find map display if not assigned
        if (mapDisplay == null)
            mapDisplay = DuringMissionMapDisplay.Instance;

        if (mapDisplay != null)
        {
            mapDisplay.ShowMap();
            // Optionally hide backpack panel when viewing map
            HidePanel();

            Debug.Log("DuringGoBagPanel: Map view opened from backpack.");
        }
        else
        {
            Debug.LogWarning("DuringGoBagPanel: Map display not found.");
        }
    }

    private void RefreshList()
    {
        var inventory = GoBagInventoryState.Instance;
        snapshotBuffer.Clear();
        if (inventory != null)
            inventory.FillSnapshot(snapshotBuffer);

        // Only show items the player actually collected (required items).
        int collectedCount = 0;
        for (int i = 0; i < snapshotBuffer.Count; i++)
        {
            if (snapshotBuffer[i].IsCollected)
                collectedCount++;
        }

        EnsurePoolSize(collectedCount);

        int displayIndex = 0;
        for (int i = 0; i < snapshotBuffer.Count; i++)
        {
            var snapshot = snapshotBuffer[i];
            if (!snapshot.IsCollected)
                continue;

            var view = pooledViews[displayIndex];
            view.gameObject.SetActive(true);
            view.Bind(snapshot);
            displayIndex++;
        }

        for (int i = collectedCount; i < pooledViews.Count; i++)
            pooledViews[i].gameObject.SetActive(false);

        bool hasItems = collectedCount > 0;
        if (emptyStateLabel != null)
        {
            emptyStateLabel.gameObject.SetActive(!hasItems);
            if (!hasItems)
                emptyStateLabel.text = "No go-bag contents available yet.";
        }
    }

    private void EnsurePoolSize(int required)
    {
        while (pooledViews.Count < required)
        {
            var instance = Instantiate(itemEntryPrefab, listContainer);
            var view = instance.GetComponent<DuringGoBagPanelItemView>();
            if (view == null)
            {
                view = instance.AddComponent<DuringGoBagPanelItemView>();
                Debug.LogWarning("DuringGoBagPanel: Item prefab was missing DuringGoBagPanelItemView. Added one at runtime, but assign it in the inspector to avoid this log.");
            }

            // Ensure there is a clickable Button somewhere on the row.
            var button = instance.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
            {
                // Try to find a Button on a child if the prefab moved it.
                button = instance.GetComponentInChildren<UnityEngine.UI.Button>(true);
            }

            if (button == null)
            {
                // As a fallback, add a Button to the root at runtime so clicks still work.
                button = instance.AddComponent<UnityEngine.UI.Button>();

                // Try to hook up a graphic so the Button has proper visuals.
                var graphic = instance.GetComponent<UnityEngine.UI.Graphic>();
                if (graphic == null)
                    graphic = instance.GetComponentInChildren<UnityEngine.UI.Graphic>(true);

                if (graphic != null)
                    button.targetGraphic = graphic;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    string labelText = view != null && view.TryGetLabelText(out var t) ? t : instance.name;
                    Debug.Log($"DuringGoBagPanel: Clicked item '{labelText}'", instance);
                    OnItemClicked(labelText);
                });
            }

            pooledViews.Add(view);
        }
    }

    private void OnItemClicked(string labelText)
    {
        if (string.IsNullOrWhiteSpace(labelText))
            return;

        // Open minimap if the player selects the Map item
        if (string.Equals(labelText.Trim(), "Map", System.StringComparison.OrdinalIgnoreCase))
        {
            if (mapDisplay == null)
                mapDisplay = DuringMissionMapDisplay.Instance;

            if (mapDisplay != null)
            {
                mapDisplay.ShowMap();
                HidePanel();
                Debug.Log("DuringGoBagPanel: Map opened from go-bag item.");
            }
            else
            {
                Debug.LogWarning("DuringGoBagPanel: Map display not found when clicking Map item.");
            }
        }
    }

    private void EnsureCanvasGroup(GameObject root)
    {
        if (root == null) return;

        var cg = root.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = root.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
}
