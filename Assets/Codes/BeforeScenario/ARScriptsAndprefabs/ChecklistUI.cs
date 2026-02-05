using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChecklistUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public Transform listContainer;
    public GameObject rowPrefab; // prefab must contain a TMP_Text

    private readonly Dictionary<EmergencyItemId, TMP_Text> _rows =
        new Dictionary<EmergencyItemId, TMP_Text>();

    public void BuildChecklist(string title, List<EmergencyItemId> required)
    {
        if (titleText != null)
            titleText.text = title;

        // Clear old
        for (int i = listContainer.childCount - 1; i >= 0; i--)
            Destroy(listContainer.GetChild(i).gameObject);

        _rows.Clear();

        foreach (var id in required)
        {
            GameObject row = Instantiate(rowPrefab, listContainer, false);

            TMP_Text rowText = row.GetComponentInChildren<TMP_Text>(true);
            if (rowText == null)
            {
                Debug.LogError("[ChecklistUI] Row prefab is missing TMP_Text (TextMeshProUGUI).");
                continue;
            }

            rowText.text = $"[ ] {id}";
            // Make checklist text black by default
            rowText.color = Color.black;
            if (titleText != null) titleText.color = Color.black;

            _rows[id] = rowText;

            Debug.Log($"[ChecklistUI] Row created for {id} using TMP_Text: {rowText.name}");
        }
    }

    public void SetChecked(EmergencyItemId id, bool isChecked)
    {
        if (!_rows.TryGetValue(id, out TMP_Text rowText))
        {
            Debug.LogWarning("[ChecklistUI] Tried to check missing row: " + id);
            return;
        }

        rowText.text = isChecked ? $"[✓] {id}" : $"[ ] {id}";
        // Keep the text color black
        rowText.color = Color.black;
        Debug.Log($"[ChecklistUI] SetChecked({id}) => {(isChecked ? "[X]" : "[ ]")}");
    }
}
