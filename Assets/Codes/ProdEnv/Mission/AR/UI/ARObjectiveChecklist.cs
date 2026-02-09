using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BaHanda.AR;

/// <summary>
/// UI checklist showing objectives/items to collect.
/// Reusable across different AR missions.
/// </summary>
public class ARObjectiveChecklist : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform itemListContainer;
    [SerializeField] private GameObject checklistItemPrefab;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressBar;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string itemCompleteTrigger = "ItemComplete";
    [SerializeField] private string allCompleteTrigger = "AllComplete";

    [Header("Audio")]
    [SerializeField] private AudioClip itemCheckSound;
    [SerializeField] private AudioClip allCompleteSound;

    // State
    private Dictionary<string, ARObjectiveItemUI> itemUIMap = new Dictionary<string, ARObjectiveItemUI>();
    private int totalItems = 0;
    private int collectedItems = 0;

    // Properties
    public int TotalItems => totalItems;
    public int CollectedItems => collectedItems;
    public float Progress => totalItems > 0 ? (float)collectedItems / totalItems : 0f;
    public bool IsComplete => collectedItems >= totalItems && totalItems > 0;

    /// <summary>
    /// Initialize with Go Bag items
    /// </summary>
    public void Initialize(GoBagItemData[] items)
    {
        ClearList();

        if (items == null) return;

        totalItems = 0;
        collectedItems = 0;

        foreach (var item in items)
        {
            if (item == null) continue;
            if (item.isRequired) totalItems++;

            CreateChecklistItem(item.itemId, item.itemName, item.itemIcon, item.isRequired);
        }

        UpdateProgress();
    }

    /// <summary>
    /// Initialize with generic objectives
    /// </summary>
    public void Initialize(ObjectiveData[] objectives)
    {
        ClearList();

        if (objectives == null) return;

        totalItems = objectives.Length;
        collectedItems = 0;

        foreach (var obj in objectives)
        {
            if (obj == null) continue;
            CreateChecklistItem(obj.objectiveId, obj.description, null, true);
        }

        UpdateProgress();
    }

    private void CreateChecklistItem(string id, string name, Sprite icon, bool isRequired)
    {
        if (checklistItemPrefab == null || itemListContainer == null) return;

        GameObject itemObj = Instantiate(checklistItemPrefab, itemListContainer);
        ARObjectiveItemUI itemUI = itemObj.GetComponent<ARObjectiveItemUI>();

        if (itemUI != null)
        {
            itemUI.Setup(id, name, icon, isRequired);
            itemUIMap[id] = itemUI;
        }
        else
        {
            Debug.LogWarning("ARObjectiveChecklist: Prefab missing ARObjectiveItemUI component");
        }
    }

    /// <summary>
    /// Mark an item as collected/completed
    /// </summary>
    public void MarkItemCollected(string itemId)
    {
        if (!itemUIMap.TryGetValue(itemId, out ARObjectiveItemUI itemUI))
        {
            Debug.LogWarning($"ARObjectiveChecklist: Item not found - {itemId}");
            return;
        }

        if (itemUI.IsCompleted) return;

        itemUI.SetCompleted(true);
        collectedItems++;

        // Play effects
        PlaySound(itemCheckSound);
        if (animator != null && !string.IsNullOrEmpty(itemCompleteTrigger))
        {
            animator.SetTrigger(itemCompleteTrigger);
        }

        UpdateProgress();

        // Check if all complete
        if (IsComplete)
        {
            OnAllItemsComplete();
        }
    }

    private void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text = $"{collectedItems}/{totalItems}";
        }

        if (progressBar != null)
        {
            progressBar.value = Progress;
        }
    }

    private void OnAllItemsComplete()
    {
        PlaySound(allCompleteSound);

        if (animator != null && !string.IsNullOrEmpty(allCompleteTrigger))
        {
            animator.SetTrigger(allCompleteTrigger);
        }
    }

    /// <summary>
    /// Reset all items to uncollected
    /// </summary>
    public void ResetAll()
    {
        collectedItems = 0;

        foreach (var kvp in itemUIMap)
        {
            kvp.Value.SetCompleted(false);
        }

        UpdateProgress();
    }

    private void ClearList()
    {
        if (itemListContainer != null)
        {
            foreach (Transform child in itemListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        itemUIMap.Clear();
        totalItems = 0;
        collectedItems = 0;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    /// <summary>
    /// Set custom header text
    /// </summary>
    public void SetHeader(string text)
    {
        if (headerText != null)
        {
            headerText.text = text;
        }
    }
}
