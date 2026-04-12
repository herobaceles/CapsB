using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using System.Reflection;

public class ARMissionManager : MonoBehaviour
{
    // --- Breaker AR Placement ---
    [Header("Breaker AR Placement")]
    public GameObject breakerPrefabToPlace; // Assign in inspector (breaker prefab)
    private bool allowBreakerPlacement = false;
    [Header("Achievements UI")]
    public GameObject achievementsPanel; // Assign panel in inspector
    public TMPro.TextMeshProUGUI achievementText; // Assign achievement text child
    public UnityEngine.UI.Button proceedButton; // Assign proceed button child
    public UnityEngine.UI.Button replayButton; // Assign replay button child
    // Simple movement lock for player
    private bool movementLocked = false;
    [Header("Feedback UI")]
    public GameObject feedbackPanel; // Assign the panel GameObject in the Canvas
    public TMPro.TextMeshProUGUI feedbackText; // Assign the TextMeshProUGUI child in the inspector
    [Header("Feedback Texts")]
    public string correctFeedbackText = "Correct";
    public string wrongFeedbackText = "Wrong";
    [Header("Item Types")]
    public List<string> requiredItemNames = new List<string>(); // Names of required items
    public List<string> notRequiredItemNames = new List<string>(); // Names of not required items
    public GameObject feedbackUIPrefab; // Assign a UI prefab for correct/unknown feedback (optional)
    public GameObject wrongItemUIPrefab; // Assign a UI prefab for wrong item feedback (optional)

    [Header("Item List UI")]
    public GameObject itemListPanel; // Assign the panel GameObject in the Canvas for item list
    public Transform itemListContainer; // Assign a container (e.g., VerticalLayoutGroup) for item UI elements
    public GameObject itemListItemPrefab; // Assign a prefab for item UI (should have a TextMeshProUGUI for name)

    [Header("Go Bag Inventory Sync")]
    public List<GoBagItemDefinition> goBagItemDefinitions = new List<GoBagItemDefinition>();

    private readonly List<GoBagItemDefinition> fallbackGoBagDefinitions = new List<GoBagItemDefinition>();
    private bool goBagInventoryInitialized = false;

    // Call this from DraggableItem when dropped into backpack
    public void OnItemDroppedInBag(GameObject item)
    {
        string itemName = item.name.Replace("(Clone)", "").Trim();
        if (requiredItemNames.Contains(itemName))
        {
            ShowFeedbackPanel(correctFeedbackText);
            MarkItemCollected(itemName);
            ItemCollected();
            Destroy(item);
        }
        else if (notRequiredItemNames.Contains(itemName))
        {
            ShowFeedbackPanel(wrongFeedbackText);
            if (PreparingGoBagManager.Instance != null)
                PreparingGoBagManager.Instance.ShowWrongItemDialogue();
            Destroy(item);
        }
        else
        {
            ShowFeedbackPanel("Unknown Item");
            Destroy(item);
        }
    }

    private void ShowFeedbackPanel(string message)
    {
        Debug.Log(message);
        if (feedbackPanel != null && feedbackText != null)
        {
            feedbackPanel.SetActive(true);
            feedbackText.text = message;
            CancelInvoke(nameof(HideFeedbackPanel));
            Invoke(nameof(HideFeedbackPanel), 1.5f);
        }
    }

    private void HideFeedbackPanel()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }
    public static ARMissionManager Instance;

    [Header("AR")]
    public ARRaycastManager raycastManager;

    [Header("Prefabs")]
    public GameObject tablePrefab;
    public GameObject bagPrefab;
    public GameObject[] itemPrefabs;

    [Header("Go Bag Slot Spawns")]
    public bool useTableItemSlots = true;
    [Tooltip("Child transform name on the table prefab that contains item slot transforms.")]
    public string itemSlotsRootName = "ItemSlots";

    [Header("UI")]
    public GameObject missionCompleteUI;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject spawnedTable;
    private GameObject spawnedBag;
    private readonly List<Transform> cachedItemSlots = new List<Transform>();
    
    // Last spawned breaker instance for the circuit breaker AR task so
    // we can reliably destroy it on reset without relying on tags/names.
    private GameObject spawnedBreaker;

    private int collectedItems = 0;
    private int totalItems;

    private bool missionPlaced = false;
    private bool arGuidanceShown = false;
    private bool arScanHintShown = false;
    private bool arTapHintShown = false;
    private bool breakerArScanHintShown = false;
    private bool breakerArTapHintShown = false;
    private bool breakerArGuidanceShown = false;

    void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private ARRaycastManager ResolveRaycastManager()
    {
        if (ARRuntimeContext.Instance != null)
            raycastManager = ARRuntimeContext.Instance.ResolveRaycastManager(raycastManager);

        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>(true);

        if (!IsRaycastManagerReady(raycastManager))
            return null;

        return raycastManager;
    }

    private bool IsRaycastManagerReady(ARRaycastManager manager)
    {
        if (manager == null)
            return false;

        var xrOrigin = manager.GetComponent<XROrigin>() ?? manager.GetComponentInParent<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("ARMissionManager: ARRaycastManager is not under an XROrigin.");
            return false;
        }

        if (xrOrigin.Camera == null)
        {
            Debug.LogError("ARMissionManager: XROrigin camera is null.");
            return false;
        }

        if (xrOrigin.TrackablesParent == null)
        {
            if (!TryEnsureTrackablesParent(xrOrigin))
            {
                Debug.LogError("ARMissionManager: XROrigin Trackables Parent is null.");
                return false;
            }

            Debug.LogWarning("ARMissionManager: Repaired missing XROrigin Trackables Parent at runtime.");
        }

        return true;
    }

    private bool TryEnsureTrackablesParent(XROrigin xrOrigin)
    {
        if (xrOrigin == null)
            return false;

        if (xrOrigin.TrackablesParent != null)
            return true;

        var trackables = new GameObject("Trackables").transform;
        trackables.SetParent(xrOrigin.transform, false);
        trackables.localPosition = Vector3.zero;
        trackables.localRotation = Quaternion.identity;
        trackables.localScale = Vector3.one;

        var type = xrOrigin.GetType();
        var property = type.GetProperty("TrackablesParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null)
        {
            var setter = property.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(xrOrigin, new object[] { trackables });
                return xrOrigin.TrackablesParent != null;
            }
        }

        var backingField = type.GetField("<TrackablesParent>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? type.GetField("m_TrackablesParent", BindingFlags.Instance | BindingFlags.NonPublic);

        if (backingField != null)
        {
            backingField.SetValue(xrOrigin, trackables);
            return xrOrigin.TrackablesParent != null;
        }

        return false;
    }

    void Update()
    {
        // Only run for Go Bag or Breaker mission
        if (BeforeMissionManager.Instance == null || MissionSelectManager.SelectedMission == null)
            return;

        // Only drive AR placement and hints while an AR mission is active.
        if (!BeforeMissionManager.Instance.IsARMissionActive)
            return;

        TryInitializeGoBagInventory();

        string missionId = MissionSelectManager.SelectedMission.missionId;

        // For the circuit breaker mission, completion is now
        // triggered automatically from the breaker switch, so
        // hide the generic Proceed button to remove that step.
        if (proceedButton != null)
        {
            bool isBreakerMission = string.Equals(missionId, "before_02", System.StringComparison.OrdinalIgnoreCase);
            proceedButton.gameObject.SetActive(!isBreakerMission);
        }

        // Go Bag AR logic
        if (missionId == "before_01")
        {
            UpdateGoBagArHints();

            // Prevent player movement if locked
            if (movementLocked)
                return;

            if (missionPlaced)
                return;

            // Use Unity Input System for both mouse and touch
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Prevent AR placement if pointer is over UI
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;
                TryPlaceMission(Mouse.current.position.ReadValue());
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                // Prevent AR placement if touch is over UI
                int fingerId = 0; // primary touch
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(fingerId))
                    return;
                TryPlaceMission(Touchscreen.current.primaryTouch.position.ReadValue());
            }
            return;
        }

        // Breaker AR logic (tap to place breaker prefab + AR guidance)
        if (missionId == "before_02")
        {
            UpdateBreakerArHints();

            if (!allowBreakerPlacement || breakerPrefabToPlace == null)
                return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;
                TryPlaceBreaker(Mouse.current.position.ReadValue());
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                int fingerId = 0;
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(fingerId))
                    return;
                TryPlaceBreaker(Touchscreen.current.primaryTouch.position.ReadValue());
            }
        }
    }

    public void EnableBreakerPlacement(GameObject breakerPrefab)
    {
        breakerPrefabToPlace = breakerPrefab;
        allowBreakerPlacement = true;

        breakerArScanHintShown = false;
        breakerArTapHintShown = false;
        breakerArGuidanceShown = false;
    }

    /// <summary>
    /// Reset the circuit breaker AR placement by removing any spawned
    /// breaker instance and clearing breaker-specific hint state. This
    /// is called from BreakerTaskManager when the player presses the
    /// Restart button for the breaker task.
    /// </summary>
    public void ResetBreakerPlacement()
    {
        // Destroy the last spawned breaker (if any) so it disappears
        // from the AR plane.
        if (spawnedBreaker != null)
        {
            Destroy(spawnedBreaker);
            spawnedBreaker = null;
        }

        // As an extra safety net, also destroy any remaining objects
        // tagged as "Breaker" in case a prefab was not tracked.
        var breakers = GameObject.FindGameObjectsWithTag("Breaker");
        for (int i = 0; i < breakers.Length; i++)
        {
            var obj = breakers[i];
            if (obj != null)
                Destroy(obj);
        }

        // Clear breaker AR hint flags so guidance can show again on
        // the next placement cycle.
        breakerArScanHintShown = false;
        breakerArTapHintShown = false;
        breakerArGuidanceShown = false;

        // Disable placement until BreakerTaskManager calls
        // EnableBreakerPlacement again as part of StartBreakerTask.
        allowBreakerPlacement = false;
    }

    void TryPlaceBreaker(Vector2 screenPosition)
    {
        var activeRaycastManager = ResolveRaycastManager();
        if (activeRaycastManager == null)
        {
            Debug.LogError("ARMissionManager: No ARRaycastManager available. Ensure Boot ARCoreRoot has XR Origin + ARRaycastManager and ARBootstrapPersistent is assigned.");
            return;
        }

        bool didHitPlane = false;
        try
        {
            didHitPlane = activeRaycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon);
        }
        catch (System.ArgumentNullException exception)
        {
            Debug.LogError($"ARMissionManager: AR raycast failed due to invalid XROrigin/camera wiring. {exception.Message}");
            return;
        }

        if (didHitPlane)
        {
            Pose hitPose = hits[0].pose;
            // Set rotation to x=0, y=90, z=0
            Quaternion spawnRotation = Quaternion.Euler(0, 90, 0);

            // Destroy any previously spawned breaker instance before
            // creating a new one, to avoid duplicates after resets.
            if (spawnedBreaker != null)
            {
                Destroy(spawnedBreaker);
                spawnedBreaker = null;
            }

            spawnedBreaker = Instantiate(breakerPrefabToPlace, hitPose.position, spawnRotation);
            allowBreakerPlacement = false;
            Debug.Log("Breaker prefab placed in AR.");

            // Show breaker-specific AR guidance dialogue authored on the
            // Mission_Before_02 asset for the breaker task.
            TryShowBreakerArGuidanceDialogue();
        }
    }

    void TryPlaceMission(Vector2 touchPosition)
    {
        var activeRaycastManager = ResolveRaycastManager();
        if (activeRaycastManager == null)
        {
            Debug.LogError("ARMissionManager: No ARRaycastManager available. Ensure Boot ARCoreRoot has XR Origin + ARRaycastManager and ARBootstrapPersistent is assigned.");
            return;
        }

        bool didHitPlane = false;
        try
        {
            didHitPlane = activeRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon);
        }
        catch (System.ArgumentNullException exception)
        {
            Debug.LogError($"ARMissionManager: AR raycast failed due to invalid XROrigin/camera wiring. {exception.Message}");
            return;
        }

        if (didHitPlane)
        {
            Pose hitPose = hits[0].pose;

            // Spawn table
            spawnedTable = Instantiate(tablePrefab, hitPose.position, hitPose.rotation);
            CacheTableItemSlots();

            // Determine table bounds and height
            float tableHeight = 0.5f;
            Vector3 tableCenter = spawnedTable.transform.position;
            Vector3 tableSize = new Vector3(0.5f, 0.5f, 0.5f); // Default size
            Renderer tableRenderer = spawnedTable.GetComponentInChildren<Renderer>();
            if (tableRenderer != null)
            {
                tableHeight = tableRenderer.bounds.size.y;
                tableSize = tableRenderer.bounds.size;
            }

            // Spawn bag at the center of the table
            Vector3 bagPosition = tableCenter + Vector3.up * (tableHeight + 0.05f);
            spawnedBag = Instantiate(bagPrefab, bagPosition, Quaternion.identity);

            // Spawn items on the table using slots.
            SpawnItemsOnTable(tableSize);

            // Show item list panel
            if (itemListPanel != null)
                itemListPanel.SetActive(true);
            PopulateItemListUI();

            // Lock movement
            movementLocked = true;

            missionPlaced = true;

            Debug.Log("Table, Bag, and Items Spawned");

            // Show AR guidance dialogue (from the corresponding mission task) so
            // the player knows what to do while in the AR session.
            TryShowArGuidanceDialogue();
        }
    }

    private void UpdateGoBagArHints()
    {
        if (missionPlaced)
            return;

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager == null)
            return;

        var mission = MissionSelectManager.SelectedMission;
        if (mission == null || mission.tasks == null)
            return;

        const string PreparingGoBagTaskId = "before_01_prepare_go_bag";
        TaskData targetTask = null;
        for (int i = 0; i < mission.tasks.Count; i++)
        {
            var t = mission.tasks[i];
            if (t != null && t.taskId == PreparingGoBagTaskId)
            {
                targetTask = t;
                break;
            }
        }

        if (targetTask == null)
            return;

        // 1) If no plane yet, tell the player how to scan the floor.
        if (!arScanHintShown && !dialogueManager.IsDialogueActive &&
            targetTask.arScanForPlaneDialogueRich != null && targetTask.arScanForPlaneDialogueRich.Count > 0)
        {
            arScanHintShown = true;
            dialogueManager.ShowDialogueSequence(targetTask.arScanForPlaneDialogueRich, null);
            return;
        }

        // 2) Once planes are detectable under the center of the screen, tell the
        //    player to tap to place the Go Bag table.
        if (!arTapHintShown && arScanHintShown && !dialogueManager.IsDialogueActive)
        {
            var activeRaycastManager = ResolveRaycastManager();
            if (activeRaycastManager == null)
                return;

            bool didHitPlane = false;
            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            try
            {
                didHitPlane = activeRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
            }
            catch (System.ArgumentNullException exception)
            {
                Debug.LogError($"ARMissionManager: Center-screen AR raycast failed while checking for planes. {exception.Message}");
                return;
            }

            if (didHitPlane &&
                targetTask.arTapToPlaceDialogueRich != null && targetTask.arTapToPlaceDialogueRich.Count > 0)
            {
                arTapHintShown = true;
                dialogueManager.ShowDialogueSequence(targetTask.arTapToPlaceDialogueRich, null);
            }
        }
    }

    private void UpdateBreakerArHints()
    {
        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager == null)
            return;

        var mission = MissionSelectManager.SelectedMission;
        if (mission == null || mission.tasks == null)
            return;

        const string BreakerTaskId = "before_02_secure_circuit_breaker";
        TaskData targetTask = null;
        for (int i = 0; i < mission.tasks.Count; i++)
        {
            var t = mission.tasks[i];
            if (t != null && t.taskId == BreakerTaskId)
            {
                targetTask = t;
                break;
            }
        }

        if (targetTask == null)
            return;

        // 1) If no plane yet, tell the player how to scan for it.
        if (!breakerArScanHintShown && !dialogueManager.IsDialogueActive &&
            targetTask.arScanForPlaneDialogueRich != null && targetTask.arScanForPlaneDialogueRich.Count > 0)
        {
            breakerArScanHintShown = true;
            dialogueManager.ShowDialogueSequence(targetTask.arScanForPlaneDialogueRich, null);
            return;
        }

        // 2) Once planes are detectable under the center of the screen, tell the
        //    player to tap to place the breaker.
        if (!breakerArTapHintShown && breakerArScanHintShown && !dialogueManager.IsDialogueActive)
        {
            var activeRaycastManager = ResolveRaycastManager();
            if (activeRaycastManager == null)
                return;

            bool didHitPlane = false;
            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            try
            {
                didHitPlane = activeRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
            }
            catch (System.ArgumentNullException exception)
            {
                Debug.LogError($"ARMissionManager: Center-screen AR raycast failed while checking for planes. {exception.Message}");
                return;
            }

            if (didHitPlane &&
                targetTask.arTapToPlaceDialogueRich != null && targetTask.arTapToPlaceDialogueRich.Count > 0)
            {
                breakerArTapHintShown = true;
                dialogueManager.ShowDialogueSequence(targetTask.arTapToPlaceDialogueRich, null);
            }
        }
    }

    private void TryShowArGuidanceDialogue()
    {
        if (arGuidanceShown)
            return;

        var mission = MissionSelectManager.SelectedMission;
        if (mission == null || mission.tasks == null)
            return;

        // Reuse the same task id as the main Preparing Go Bag task so
        // guidance is authored on that task asset.
        const string PreparingGoBagTaskId = "before_01_prepare_go_bag";
        TaskData targetTask = null;
        for (int i = 0; i < mission.tasks.Count; i++)
        {
            var t = mission.tasks[i];
            if (t != null && t.taskId == PreparingGoBagTaskId)
            {
                targetTask = t;
                break;
            }
        }

        if (targetTask == null || targetTask.arGuidanceDialogueRich == null || targetTask.arGuidanceDialogueRich.Count == 0)
            return;

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager == null)
            return;

        arGuidanceShown = true;

        // While guidance is showing, keep movement locked; unlock when it finishes.
        movementLocked = true;
        dialogueManager.ShowDialogueSequence(targetTask.arGuidanceDialogueRich, () =>
        {
            movementLocked = false;
        });
    }

    private void TryShowBreakerArGuidanceDialogue()
    {
        if (breakerArGuidanceShown)
            return;

        var mission = MissionSelectManager.SelectedMission;
        if (mission == null || mission.tasks == null)
            return;

        const string BreakerTaskId = "before_02_secure_circuit_breaker";
        TaskData targetTask = null;
        for (int i = 0; i < mission.tasks.Count; i++)
        {
            var t = mission.tasks[i];
            if (t != null && t.taskId == BreakerTaskId)
            {
                targetTask = t;
                break;
            }
        }

        if (targetTask == null || targetTask.arGuidanceDialogueRich == null || targetTask.arGuidanceDialogueRich.Count == 0)
            return;

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager == null)
            return;

        breakerArGuidanceShown = true;

        dialogueManager.ShowDialogueSequence(targetTask.arGuidanceDialogueRich, null);
    }

    // Spawns items on the table, avoiding the bag and trying to
    // avoid overlap between items. If a good random position
    // cannot be found, a safe fallback ring position is used so
    // that all items still appear.
    void SpawnItemsOnTable(Vector3 tableSize)
    {
        totalItems = requiredItemNames.Count;

        if (!useTableItemSlots)
        {
            Debug.LogWarning("ARMissionManager: Table item slots are disabled. No items will be spawned.");
            return;
        }

        if (cachedItemSlots.Count == 0)
        {
            Debug.LogWarning("ARMissionManager: Item slot spawning enabled, but no slots were found. Skipping item spawn.");
            return;
        }

        SpawnItemsAtSlots(tableSize);
    }

    private void SpawnItemsAtSlots(Vector3 tableSize)
    {
        int itemCount = itemPrefabs != null ? itemPrefabs.Length : 0;
        int slotCount = cachedItemSlots.Count;
        int spawnCount = Mathf.Min(itemCount, slotCount);

        if (itemCount > slotCount)
        {
            Debug.LogWarning($"ARMissionManager: Only {slotCount} item slots found for {itemCount} items. Extra items will not be spawned.");
        }

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject item = itemPrefabs[i];
            if (item == null)
                continue;

            Transform slot = cachedItemSlots[i];
            if (slot == null)
                continue;

            GameObject spawnedItem = Instantiate(item, slot.position, slot.rotation);
            if (spawnedTable != null)
                spawnedItem.transform.SetParent(spawnedTable.transform, true);
            ConfigureDraggableItem(spawnedItem, tableSize);
        }
    }

    private void ConfigureDraggableItem(GameObject spawnedItem, Vector3 tableSize)
    {
        if (spawnedItem == null)
            return;

        var body = spawnedItem.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.useGravity = false;
            body.isKinematic = true;
        }

        var draggable = spawnedItem.GetComponent<DraggableItem>();
        if (draggable == null)
            return;

        draggable.tableTransform = spawnedTable != null ? spawnedTable.transform : null;
        draggable.tableSize = tableSize;
    }

    private void CacheTableItemSlots()
    {
        cachedItemSlots.Clear();

        if (spawnedTable == null || string.IsNullOrWhiteSpace(itemSlotsRootName))
            return;

        Transform slotsRoot = null;
        var allTransforms = spawnedTable.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            var t = allTransforms[i];
            if (t != null && string.Equals(t.name, itemSlotsRootName, StringComparison.Ordinal))
            {
                slotsRoot = t;
                break;
            }
        }

        if (slotsRoot == null)
            return;

        foreach (Transform child in slotsRoot)
        {
            if (child != null)
                cachedItemSlots.Add(child);
        }

        cachedItemSlots.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int nameCompare = string.Compare(a.name, b.name, StringComparison.Ordinal);
            if (nameCompare != 0)
                return nameCompare;
            return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
        });
    }


    // Populate the item list UI with required items
    void PopulateItemListUI()
    {
        if (itemListContainer == null || itemListItemPrefab == null)
            return;
        // Remove old children
        foreach (Transform child in itemListContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (string itemName in requiredItemNames)
        {
            GameObject itemUI = Instantiate(itemListItemPrefab, itemListContainer);
            var text = itemUI.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
                text.text = itemName;
            // Add a checkmark image (optional, must be part of prefab)
            var check = itemUI.transform.Find("Checkmark");
            if (check != null)
                check.gameObject.SetActive(false);
            // Store item name in the object for lookup
            itemUI.name = "ItemUI_" + itemName;
        }
    }

    // Mark collected item in the list UI
    void MarkItemCollected(string itemName)
    {
        if (itemListContainer == null)
            return;
        foreach (Transform child in itemListContainer)
        {
            var text = child.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null && text.text == itemName)
            {
                // Show checkmark (must be part of prefab)
                var check = child.transform.Find("Checkmark");
                if (check != null)
                    check.gameObject.SetActive(true);
                // Optionally, change text color
                text.color = Color.green;
                break;
            }
        }

        GoBagInventoryState.Instance.MarkItemCollected(itemName);
    }


    void ItemCollected()
    {
        collectedItems++;
        if (collectedItems >= totalItems)
        {
            MissionComplete();
        }
    }

    void MissionComplete()
    {
        Debug.Log("AR Mission Complete");

        // Hide item list panel
        if (itemListPanel != null)
            itemListPanel.SetActive(false);

        // Show achievements panel
        if (achievementsPanel != null)
            achievementsPanel.SetActive(true);
        if (achievementText != null)
        {
            var missionId = MissionSelectManager.SelectedMission != null
                ? MissionSelectManager.SelectedMission.missionId
                : null;

            if (string.Equals(missionId, "before_01", System.StringComparison.OrdinalIgnoreCase))
                achievementText.text = "Preparing Go Bag Complete!";
            else
                achievementText.text = "Mission Complete!";
        }

        // Unlock movement
        movementLocked = false;
    }

    private bool hasProceeded = false;

    // Call this from Proceed button
    public void ProceedToWorld()
    {
        if (hasProceeded) return;
        hasProceeded = true;

        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
        if (missionCompleteUI != null)
            missionCompleteUI.SetActive(false);
        if (BeforeMissionManager.Instance != null)
            BeforeMissionManager.Instance.EndARMission();

        // Show completion dialogue AFTER returning to normal camera so the UI is visible
        if (BeforeMissionManager.Instance != null && MissionSelectManager.SelectedMission != null)
        {
            var missionId = MissionSelectManager.SelectedMission.missionId;
            if (missionId == "before_01" && PreparingGoBagManager.Instance != null)
                PreparingGoBagManager.Instance.ShowCompletionDialogueAndAchievement();
            else if (missionId == "before_02" && BreakerTaskManager.Instance != null)
                BreakerTaskManager.Instance.CompleteBreakerTask();
        }
    }

    // Call this from Replay button
    public void ReplayARMission()
    {
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);

        // Destroy spawned table, bag, and items
        if (spawnedTable != null)
            Destroy(spawnedTable);
        if (spawnedBag != null)
            Destroy(spawnedBag);
        cachedItemSlots.Clear();
        foreach (var obj in GameObject.FindGameObjectsWithTag("EmergencyItem"))
            Destroy(obj);

        // Reset AR flow state so that hints and placement work like
        // a fresh run each time the mission is replayed.
        movementLocked = false;

        var selectedMission = MissionSelectManager.SelectedMission;
        var selectedMissionId = selectedMission != null ? selectedMission.missionId : null;

        if (string.Equals(selectedMissionId, "before_01", System.StringComparison.OrdinalIgnoreCase))
        {
            // Go Bag AR mission
            arGuidanceShown = false;
            arScanHintShown = false;
            arTapHintShown = false;
        }
        else if (string.Equals(selectedMissionId, "before_02", System.StringComparison.OrdinalIgnoreCase))
        {
            // Breaker AR mission
            breakerArScanHintShown = false;
            breakerArTapHintShown = false;
            breakerArGuidanceShown = false;
        }

        // Reset mission state
        collectedItems = 0;
        missionPlaced = false;
        if (missionCompleteUI != null)
            missionCompleteUI.SetActive(false);

        // Optionally, reset the item list UI
        PopulateItemListUI();

        goBagInventoryInitialized = false;
        var inventory = GoBagInventoryState.Instance;
        inventory.ResetProgress();
        inventory.SaveToDisk();
        TryInitializeGoBagInventory();
    }

    private void TryInitializeGoBagInventory()
    {
        if (goBagInventoryInitialized)
            return;

        if (!IsGoBagMissionSelected())
            return;

        var inventory = GoBagInventoryState.Instance;
        var missionId = MissionSelectManager.SelectedMission?.missionId;
        inventory.SetActiveMissionId(missionId);
        inventory.ApplyDefinitions(GetActiveGoBagDefinitions(), true);
        goBagInventoryInitialized = true;
    }

    private bool IsGoBagMissionSelected()
    {
        return MissionSelectManager.SelectedMission != null &&
               MissionSelectManager.SelectedMission.missionId == "before_01";
    }

    private IEnumerable<GoBagItemDefinition> GetActiveGoBagDefinitions()
    {
        if (goBagItemDefinitions != null && goBagItemDefinitions.Count > 0)
            return goBagItemDefinitions;

        fallbackGoBagDefinitions.Clear();
        for (int i = 0; i < requiredItemNames.Count; i++)
        {
            var name = requiredItemNames[i];
            if (string.IsNullOrWhiteSpace(name))
                continue;

            fallbackGoBagDefinitions.Add(new GoBagItemDefinition
            {
                itemName = name.Trim(),
                icon = null
            });
        }

        return fallbackGoBagDefinitions;
    }
}
