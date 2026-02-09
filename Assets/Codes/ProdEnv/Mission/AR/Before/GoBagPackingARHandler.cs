using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BaHanda.AR
{
    /// <summary>
    /// AR handler for the "Prepare Emergency Kit Go Bag" mission.
    /// Spawns items and backpack on detected plane, tracks collection progress.
    /// </summary>
    public class GoBagPackingARHandler : ARMissionBase
    {
        [Header("Go Bag Configuration")]
        [SerializeField] private GoBagItemData[] requiredItems;

        [Header("Prefabs")]
        [SerializeField] private GameObject backpackPrefab;
        [SerializeField] private float backpackDistance = 1.0f;
        [SerializeField] private float itemSpawnRadius = 0.5f;
        [SerializeField] private float itemSpawnDelay = 0.2f;

        // State
        private GoBagDropZone backpack;
        private List<GoBagItem> spawnedItems = new List<GoBagItem>();
        private HashSet<string> collectedItemIds = new HashSet<string>();
        private int totalRequiredItems = 0;

        // Properties
        public override float Progress
        {
            get
            {
                if (totalRequiredItems <= 0) return 0f;
                return (float)collectedItemIds.Count / totalRequiredItems;
            }
        }

        public int CollectedCount => collectedItemIds.Count;
        public int TotalRequired => totalRequiredItems;

        protected override void Start()
        {
            base.Start();

            // Count required items
            if (requiredItems != null)
            {
                totalRequiredItems = 0;
                foreach (var item in requiredItems)
                {
                    if (item != null && item.isRequired)
                        totalRequiredItems++;
                }
            }
        }

        #region Spawning

        protected override IEnumerator SpawnObjectsOnPlane(ARPlane plane)
        {
            Camera arCam = arSessionController?.ARCamera ?? Camera.main;
            if (arCam == null)
            {
                Debug.LogError("GoBagPackingARHandler: No AR camera available");
                yield break;
            }

            // Calculate spawn center in front of camera
            Vector3 forward = arCam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 spawnCenter = arCam.transform.position + forward * backpackDistance;
            spawnCenter.y = plane.center.y + spawnHeightOffset;

            // Spawn backpack first
            yield return StartCoroutine(SpawnBackpack(spawnCenter));

            // Spawn items around the backpack
            yield return StartCoroutine(SpawnItems(spawnCenter, plane));

            // Initialize checklist
            if (objectiveChecklist != null)
            {
                objectiveChecklist.Initialize(requiredItems);
            }
        }

        private IEnumerator SpawnBackpack(Vector3 position)
        {
            if (backpackPrefab == null)
            {
                Debug.LogError("GoBagPackingARHandler: Backpack prefab not assigned!");
                yield break;
            }

            GameObject backpackObj = Instantiate(backpackPrefab, position, Quaternion.identity);
            RegisterSpawnedObject(backpackObj);

            backpack = backpackObj.GetComponent<GoBagDropZone>();
            if (backpack == null)
            {
                backpack = backpackObj.AddComponent<GoBagDropZone>();
            }

            // Subscribe to events
            backpack.OnItemDropped += OnItemCollectedInBackpack;

            Debug.Log("GoBagPackingARHandler: Backpack spawned");
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator SpawnItems(Vector3 center, ARPlane plane)
        {
            if (requiredItems == null || requiredItems.Length == 0)
            {
                Debug.LogWarning("GoBagPackingARHandler: No items configured!");
                yield break;
            }

            float angleStep = 360f / requiredItems.Length;
            float currentAngle = 0f;

            foreach (var itemData in requiredItems)
            {
                if (itemData == null || itemData.itemPrefab == null)
                {
                    currentAngle += angleStep;
                    continue;
                }

                // Calculate position in circle around backpack
                float radians = currentAngle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(radians) * itemSpawnRadius,
                    0.1f,
                    Mathf.Sin(radians) * itemSpawnRadius
                );

                Vector3 spawnPos = center + offset;
                spawnPos.y = plane.center.y + spawnHeightOffset + 0.1f;

                // Spawn the item
                GameObject itemObj = Instantiate(itemData.itemPrefab, spawnPos, Quaternion.identity);
                RegisterSpawnedObject(itemObj);

                GoBagItem item = itemObj.GetComponent<GoBagItem>();
                if (item == null)
                {
                    item = itemObj.AddComponent<GoBagItem>();
                }

                item.Initialize(itemData);
                item.OnItemCollected += OnItemCollectedEvent;
                spawnedItems.Add(item);

                Debug.Log($"GoBagPackingARHandler: Spawned {itemData.itemName}");

                currentAngle += angleStep;
                yield return new WaitForSeconds(itemSpawnDelay);
            }

            Debug.Log($"GoBagPackingARHandler: Spawned {spawnedItems.Count} items");
        }

        #endregion

        #region Item Collection

        private void OnItemCollectedEvent(GoBagItem item)
        {
            // Already handled by OnItemCollectedInBackpack
        }

        private void OnItemCollectedInBackpack(GoBagItem item)
        {
            if (item == null || item.ItemData == null) return;

            string itemId = item.ItemData.itemId;

            // Already collected?
            if (collectedItemIds.Contains(itemId)) return;

            collectedItemIds.Add(itemId);
            spawnedItems.Remove(item);

            Debug.Log($"GoBagPackingARHandler: Collected {item.ItemData.itemName} ({collectedItemIds.Count}/{totalRequiredItems})");

            // Update checklist
            if (objectiveChecklist != null)
            {
                objectiveChecklist.MarkItemCollected(itemId);
            }

            // Play sound
            PlaySound(objectiveCompleteSound);

            // Notify progress
            NotifyProgressChanged();

            // Check completion
            if (collectedItemIds.Count >= totalRequiredItems)
            {
                CompleteAR();
            }
        }

        #endregion

        #region Overrides

        protected override void ResetObjectives()
        {
            collectedItemIds.Clear();
            spawnedItems.Clear();

            if (objectiveChecklist != null)
            {
                objectiveChecklist.ResetAll();
            }
        }

        protected override string GetStartInstruction()
        {
            return "Starting Go Bag preparation...";
        }

        protected override string GetSpawningInstruction()
        {
            return "Placing emergency items...";
        }

        protected override string GetMainInstruction()
        {
            return "Drag emergency items into your Go Bag!";
        }

        protected override string GetCompleteInstruction()
        {
            return "Excellent! Your emergency Go Bag is ready!";
        }

        public override void EndAR()
        {
            // Unsubscribe from backpack events
            if (backpack != null)
            {
                backpack.OnItemDropped -= OnItemCollectedInBackpack;
            }

            // Unsubscribe from item events
            foreach (var item in spawnedItems)
            {
                if (item != null)
                {
                    item.OnItemCollected -= OnItemCollectedEvent;
                }
            }

            spawnedItems.Clear();
            backpack = null;

            base.EndAR();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if a specific item has been collected
        /// </summary>
        public bool IsItemCollected(string itemId)
        {
            return collectedItemIds.Contains(itemId);
        }

        /// <summary>
        /// Get list of uncollected items
        /// </summary>
        public List<GoBagItemData> GetUncollectedItems()
        {
            List<GoBagItemData> uncollected = new List<GoBagItemData>();

            if (requiredItems != null)
            {
                foreach (var item in requiredItems)
                {
                    if (item != null && !collectedItemIds.Contains(item.itemId))
                    {
                        uncollected.Add(item);
                    }
                }
            }

            return uncollected;
        }

        #endregion
    }
}
