using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GoBagItemDefinition
{
    public string itemName;
    public Sprite icon;
}

public readonly struct GoBagItemSnapshot
{
    public GoBagItemSnapshot(string itemName, Sprite icon, bool isCollected)
    {
        ItemName = itemName;
        Icon = icon;
        IsCollected = isCollected;
    }

    public string ItemName { get; }
    public Sprite Icon { get; }
    public bool IsCollected { get; }
}

/// <summary>
/// Persists Go Bag checklist progress across scenes so the During phase
/// can surface the packed items from the earlier mission.
/// </summary>
public class GoBagInventoryState : MonoBehaviour
{
    [SerializeField] private string defaultMissionId = "before_01";

    private const string PlayerPrefsKeyPrefix = "GoBagInventory.";

    private static GoBagInventoryState instance;
    private static readonly IEqualityComparer<string> comparer = StringComparer.OrdinalIgnoreCase;

    private readonly List<ItemRecord> items = new List<ItemRecord>();
    private readonly Dictionary<string, ItemRecord> lookup = new Dictionary<string, ItemRecord>(comparer);
    private string activeMissionId;
    private bool hasSyncedFromDisk;

    private class ItemRecord
    {
        public string Name;
        public Sprite Icon;
        public bool Collected;
    }

    [Serializable]
    private class PersistedState
    {
        public List<PersistedItem> items = new List<PersistedItem>();
    }

    [Serializable]
    private class PersistedItem
    {
        public string name;
        public bool collected;
    }

    public static GoBagInventoryState Instance
    {
        get
        {
            if (instance == null)
                instance = FindOrCreateInstance();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrWhiteSpace(activeMissionId))
            activeMissionId = string.IsNullOrWhiteSpace(defaultMissionId) ? null : defaultMissionId.Trim();
    }

    private static GoBagInventoryState FindOrCreateInstance()
    {
        var existing = FindObjectOfType<GoBagInventoryState>();
        if (existing != null)
            return existing;

        var container = new GameObject("GoBagInventoryState");
        return container.AddComponent<GoBagInventoryState>();
    }

    /// <summary>
    /// Registers the list of go-bag items shown to the player.
    /// </summary>
    public void ApplyDefinitions(IEnumerable<GoBagItemDefinition> definitions, bool resetProgress = true)
    {
        if (definitions == null)
        {
            items.Clear();
            lookup.Clear();
            SaveToDisk();
            return;
        }

        Dictionary<string, bool> previousFlags = null;
        if (!resetProgress && items.Count > 0)
        {
            previousFlags = new Dictionary<string, bool>(comparer);
            foreach (var record in items)
                previousFlags[record.Name] = record.Collected;
        }

        items.Clear();
        lookup.Clear();

        var seen = new HashSet<string>(comparer);
        foreach (var definition in definitions)
        {
            var trimmedName = definition.itemName?.Trim();
            if (string.IsNullOrEmpty(trimmedName) || !seen.Add(trimmedName))
                continue;

            bool wasCollected = false;
            if (!resetProgress && previousFlags != null && previousFlags.TryGetValue(trimmedName, out var storedFlag))
                wasCollected = storedFlag;

            var record = new ItemRecord
            {
                Name = trimmedName,
                Icon = definition.icon,
                Collected = wasCollected
            };

            items.Add(record);
            lookup[trimmedName] = record;
        }

        if (!resetProgress)
            LoadFromDisk(createMissingEntries: false);
        else
            SaveToDisk();
    }

    public void ResetProgress()
    {
        for (int i = 0; i < items.Count; i++)
            items[i].Collected = false;

        SaveToDisk();
    }

    public void MarkItemCollected(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return;

        if (lookup.TryGetValue(itemName.Trim(), out var record))
        {
            record.Collected = true;
            SaveToDisk();
        }
    }

    public void MarkItemMissing(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return;

        if (lookup.TryGetValue(itemName.Trim(), out var record))
        {
            record.Collected = false;
            SaveToDisk();
        }
    }

    public bool HasItems => items.Count > 0;

    public void FillSnapshot(List<GoBagItemSnapshot> destination)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        EnsureSyncedFromDisk();

        destination.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            var record = items[i];
            destination.Add(new GoBagItemSnapshot(record.Name, record.Icon, record.Collected));
        }
    }

    public void SetActiveMissionId(string missionId)
    {
        var normalized = string.IsNullOrWhiteSpace(missionId) ? null : missionId.Trim();
        if (string.Equals(activeMissionId, normalized, StringComparison.OrdinalIgnoreCase))
            return;

        activeMissionId = normalized ?? (string.IsNullOrWhiteSpace(defaultMissionId) ? null : defaultMissionId.Trim());
        hasSyncedFromDisk = false;
    }

    public void SaveToDisk()
    {
        var key = GetPrefsKey();
        if (string.IsNullOrEmpty(key))
            return;

        var payload = new PersistedState();
        for (int i = 0; i < items.Count; i++)
        {
            var record = items[i];
            payload.items.Add(new PersistedItem
            {
                name = record.Name,
                collected = record.Collected
            });
        }

        var json = JsonUtility.ToJson(payload);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        hasSyncedFromDisk = true;
    }

    public bool LoadFromDisk(bool createMissingEntries = true)
    {
        var key = GetPrefsKey();
        if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key))
            return false;

        var json = PlayerPrefs.GetString(key);
        if (string.IsNullOrEmpty(json))
            return false;

        PersistedState payload;
        try
        {
            payload = JsonUtility.FromJson<PersistedState>(json);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!ApplyPersistedState(payload, createMissingEntries))
            return false;

        hasSyncedFromDisk = true;
        return true;
    }

    private void EnsureSyncedFromDisk()
    {
        if (hasSyncedFromDisk)
            return;

        LoadFromDisk(createMissingEntries: items.Count == 0);
        hasSyncedFromDisk = true;
    }

    private bool ApplyPersistedState(PersistedState payload, bool createMissingEntries)
    {
        if (payload?.items == null || payload.items.Count == 0)
            return false;

        var updated = false;
        for (int i = 0; i < payload.items.Count; i++)
        {
            var entry = payload.items[i];
            if (string.IsNullOrWhiteSpace(entry.name))
                continue;

            var trimmed = entry.name.Trim();
            if (lookup.TryGetValue(trimmed, out var record))
            {
                record.Collected = entry.collected;
                updated = true;
            }
            else if (createMissingEntries)
            {
                var newRecord = new ItemRecord
                {
                    Name = trimmed,
                    Icon = null,
                    Collected = entry.collected
                };

                items.Add(newRecord);
                lookup[trimmed] = newRecord;
                updated = true;
            }
        }

        return updated;
    }

    private string GetPrefsKey()
    {
        var missionId = string.IsNullOrWhiteSpace(activeMissionId) ? null : activeMissionId.Trim();
        if (string.IsNullOrEmpty(missionId))
            return null;

        return PlayerPrefsKeyPrefix + missionId;
    }
}
