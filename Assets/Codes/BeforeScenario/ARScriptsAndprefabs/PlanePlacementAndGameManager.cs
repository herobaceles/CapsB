using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

public class PlanePlacementAndGameManager : MonoBehaviour
{
    [Header("AR")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Prefabs")]
    public GameObject goBagStationPrefab;
    public List<GameObject> spawnableItemPrefabs;

    [Header("Round Config")]
    public List<EmergencyItemId> requiredItems = new()
    {
        EmergencyItemId.Water,
        EmergencyItemId.FirstAidKit,
        EmergencyItemId.Flashlight,
        EmergencyItemId.Batteries,
        EmergencyItemId.EmergencyWhistle,
        EmergencyItemId.PowerBank
    };

    public int totalItemsToSpawn = 12;
    public float spawnRadius = 1.2f;
    public float itemYLift = 0.02f;

    [Header("UI")]
    [Tooltip("Parent/root GameObject that contains ALL AR UI for this mission (scan text, feedback, checklist, results panel, etc.).")]
    public GameObject arUIRoot;

    [Tooltip("Either assign a Text component directly, or assign the parent Panel GameObject that contains the scan Text.")]
    public Text scanMessageText;
    [Tooltip("Optional: assign the panel GameObject that contains the scan message Text (preferred if text is nested inside a panel).")]
    public GameObject scanMessagePanel;
    public Text feedbackText;
    public ChecklistUI checklistUI;
    [Header("AR Instruction")]
    [Tooltip("Short instructional text shown when the AR mission UI first appears.")]
    public string arInstructionMessage = "Welcome to AR: move your device slowly to scan the room, then tap a flat surface to place the station.";
    [Tooltip("How long (seconds) to show the AR instruction before reverting to scan prompts.")]
    public float arInstructionDuration = 4f;
    [Tooltip("If true, show the AR instruction when the AR UI activates.")]
    public bool showARInstructionOnStart = true;

    [Header("Results UI")]
    public GameObject resultsPanel;
    public Text resultsText;
    public Button playAgainButton;
    public Button nextButton;

    private GameObject _stationInstance;
    private BagDropZone _bagDropZone;

    private readonly List<GameObject> _spawnedItems = new();
    private readonly HashSet<EmergencyItemId> _packedCorrect = new();

    private int _wrongPackedCount;

    private static readonly List<ARRaycastHit> Hits = new();

    private bool _bagArmed = false;
    private bool _isShowingARInstruction = false;
    private Coroutine _arInstructionRoutine;

    // ✅ Ensure UI is OFF before anything runs
    private void Awake()
    {
        SetARUIActive(false);
    }

    private void Start()
    {
        // Start() will run when enabled.
        if (feedbackText != null) feedbackText.text = "";
        if (resultsPanel != null) resultsPanel.SetActive(false);

        if (checklistUI != null)
        {
            checklistUI.BuildChecklist("Pack these items:", requiredItems);
            foreach (var id in requiredItems)
                checklistUI.SetChecked(id, false);
            // Start hidden; we'll show it once items are spawned
            checklistUI.gameObject.SetActive(false);
        }

        // If user assigned a panel instead of a Text, try to resolve the Text inside it
        if (scanMessageText == null && scanMessagePanel != null)
        {
            scanMessageText = scanMessagePanel.GetComponentInChildren<Text>(true);
            if (scanMessageText == null)
                Debug.LogWarning("[AR GoBag] scanMessagePanel assigned but no Text component found under it.");
        }

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(PlayAgain);

        // ✅ NEXT button -> return to BeforeSceneManager and show achievement banner
        if (nextButton != null)
            nextButton.onClick.AddListener(Next);
    }

    private void Update()
    {
        if (_stationInstance != null) return;

        UpdateScanUI();

        Vector2? screenPos = GetPointerScreenPosition();
        if (screenPos.HasValue)
            TryPlaceAtScreenPosition(screenPos.Value);
    }

    // ================= UI GATING =================

    // ✅ Call this from BeforeSceneManager when the trigger starts the AR mission.
    public void SetARUIActivePublic(bool active)
    {
        SetARUIActive(active);
    }

    private void SetARUIActive(bool active)
    {
        if (arUIRoot != null)
        {
            arUIRoot.SetActive(active);
            if (!active && resultsPanel != null) resultsPanel.SetActive(false);
            return;
        }

        // Fallback: if you didn't assign arUIRoot, we toggle known UI pieces.
        if (scanMessagePanel != null)
            scanMessagePanel.SetActive(active);
        else if (scanMessageText != null)
            scanMessageText.gameObject.SetActive(active);
        if (feedbackText != null) feedbackText.gameObject.SetActive(active);
        // Only deactivate checklist on UI hide; do not auto-activate it here.
        if (!active && checklistUI != null) checklistUI.gameObject.SetActive(false);

        if (!active && resultsPanel != null) resultsPanel.SetActive(false);
        if (!active && checklistUI != null) checklistUI.gameObject.SetActive(false);

        // When AR UI becomes active, optionally show a short instruction first
        if (active && showARInstructionOnStart && _stationInstance == null)
        {
            if (_arInstructionRoutine != null) StopCoroutine(_arInstructionRoutine);
            _arInstructionRoutine = StartCoroutine(ShowARInstructionThenResumeScanning());
        }
        else if (!active)
        {
            // stop any running instruction when UI hides
            if (_arInstructionRoutine != null)
            {
                StopCoroutine(_arInstructionRoutine);
                _arInstructionRoutine = null;
            }
            _isShowingARInstruction = false;
        }
    }

    // ================= INPUT =================

    private Vector2? GetPointerScreenPosition()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return Mouse.current.position.ReadValue();

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return null;
    }

    private void TryPlaceAtScreenPosition(Vector2 screenPos)
    {
        if (raycastManager == null) return;

        if (!raycastManager.Raycast(screenPos, Hits, TrackableType.PlaneWithinPolygon))
            return;

        Pose pose = Hits[0].pose;
        PlaceStation(pose.position);
    }

    // ================= PLACEMENT =================

    private void UpdateScanUI()
    {
        if (planeManager == null) return;

        string msg = planeManager.trackables.count > 0
            ? "Tap a flat surface to start."
            : "Move your phone to scan a flat surface.";

        if (scanMessageText != null)
            scanMessageText.text = msg;

        // Ensure the panel (if assigned) is visible while message is set
        if (scanMessagePanel != null)
            scanMessagePanel.SetActive(true);
        else if (scanMessageText != null)
            scanMessageText.gameObject.SetActive(true);
    }

    private void PlaceStation(Vector3 position)
    {
        if (scanMessageText != null) scanMessageText.text = "";
        // Hide the panel when we clear the scan message
        if (scanMessagePanel != null)
            scanMessagePanel.SetActive(false);
        else if (scanMessageText != null)
            scanMessageText.gameObject.SetActive(false);
        if (feedbackText != null) feedbackText.text = "";

        if (goBagStationPrefab == null || arCamera == null) return;

        Vector3 forward = arCamera.transform.forward;
        forward.y = 0f;

        _stationInstance = Instantiate(
            goBagStationPrefab,
            position,
            forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : Quaternion.identity
        );

        // Find BagDropZone and wire manager reference
        _bagDropZone = _stationInstance.GetComponentInChildren<BagDropZone>();
        if (_bagDropZone != null)
        {
            _bagDropZone.manager = this;
        }
        else
        {
            Debug.LogError("[AR GoBag] No BagDropZone found under GoBagStation prefab.");
        }

        SpawnItemsAroundStation(position);
        SetPlanesActive(false);

        // Arm AFTER spawn so items overlapping at spawn don't auto-pack
        _bagArmed = false;
        Invoke(nameof(ArmBag), 0.25f);
    }

    private IEnumerator ShowARInstructionThenResumeScanning()
    {
        _isShowingARInstruction = true;

        if (scanMessageText != null)
            scanMessageText.text = arInstructionMessage;

        if (scanMessagePanel != null)
            scanMessagePanel.SetActive(true);
        else if (scanMessageText != null)
            scanMessageText.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < arInstructionDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        _isShowingARInstruction = false;
        _arInstructionRoutine = null;

        // Update the scan UI immediately
        UpdateScanUI();
    }

    private void ArmBag()
    {
        _bagArmed = true;
    }

    // ================= SPAWNING =================

    private void SpawnItemsAroundStation(Vector3 stationPos)
    {
        ClearSpawnedItems();

        if (spawnableItemPrefabs == null || spawnableItemPrefabs.Count == 0)
        {
            Debug.LogError("[AR GoBag] No spawnableItemPrefabs assigned.");
            return;
        }

        List<GameObject> finalSpawnList = new();

        // Ensure required items are present
        foreach (var req in requiredItems)
        {
            GameObject prefab = FindPrefabForItem(req);
            if (prefab != null)
                finalSpawnList.Add(prefab);
            else
                Debug.LogError("[AR GoBag] Missing prefab for required item: " + req);
        }

        // Fill remaining with random items (distractors only)
        while (finalSpawnList.Count < totalItemsToSpawn)
        {
            GameObject randomPrefab =
                spawnableItemPrefabs[Random.Range(0, spawnableItemPrefabs.Count)];

            if (randomPrefab == null)
                continue;

            ItemIdentity ident = randomPrefab.GetComponent<ItemIdentity>();
            if (ident == null)
                continue;

            // Only add distractors (not required items)
            if (requiredItems.Contains(ident.itemId))
                continue;

            finalSpawnList.Add(randomPrefab);
        }

        // Spawn in a circle
        for (int i = 0; i < finalSpawnList.Count; i++)
        {
            float angle = (i / (float)finalSpawnList.Count) * Mathf.PI * 2f;
            Vector3 offset = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            Vector3 spawnPos =
                stationPos +
                offset * spawnRadius +
                Vector3.up * itemYLift;

            GameObject item = Instantiate(finalSpawnList[i], spawnPos, Quaternion.identity);
            _spawnedItems.Add(item);

            var drag = item.GetComponent<DraggableItem>();
            if (drag != null)
            {
                drag.raycastManager = raycastManager;
                drag.arCamera = arCamera;
            }
        }

        // Now that items are spawned, show the checklist so the player can follow it
        if (checklistUI != null)
            checklistUI.gameObject.SetActive(true);
    }

    private GameObject FindPrefabForItem(EmergencyItemId id)
    {
        foreach (var prefab in spawnableItemPrefabs)
        {
            if (prefab == null) continue;

            var ident = prefab.GetComponent<ItemIdentity>();
            if (ident != null && ident.itemId == id)
                return prefab;
        }
        return null;
    }

    // ================= DROP CALLBACK (called from BagDropZone) =================

    public void HandleItemDroppedIntoBag(ItemIdentity item, GameObject obj)
    {
        if (!_bagArmed) return;
        if (item == null || obj == null) return;

        if (requiredItems.Contains(item.itemId))
        {
            if (_packedCorrect.Add(item.itemId))
            {
                if (checklistUI != null) checklistUI.SetChecked(item.itemId, true);
                if (feedbackText != null) feedbackText.text = "Correct item packed!";
            }

            Destroy(obj);

            if (_packedCorrect.Count == requiredItems.Count)
                ShowResults();
        }
        else
        {
            _wrongPackedCount++;
            if (feedbackText != null) feedbackText.text = "Wrong item — not needed.";
        }
    }

    // ================= RESULTS =================

    private void ShowResults()
    {
        if (resultsPanel != null) resultsPanel.SetActive(true);

        if (resultsText != null)
        {
            resultsText.text =
                $"All required items packed!\n\n" +
                $"Correct: {_packedCorrect.Count}\n" +
                $"Wrong: {_wrongPackedCount}";
        }
    }

    private void PlayAgain()
    {
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (feedbackText != null) feedbackText.text = "";

        _packedCorrect.Clear();
        _wrongPackedCount = 0;

        if (checklistUI != null)
        {
            foreach (var id in requiredItems)
                checklistUI.SetChecked(id, false);
        }

        if (_stationInstance != null)
            SpawnItemsAroundStation(_stationInstance.transform.position);

        _bagArmed = false;
        Invoke(nameof(ArmBag), 0.25f);
    }

    // ✅ NEXT: return to BeforeSceneManager (non-AR) and show achievement banner
    private void Next()
    {
        if (resultsPanel != null) resultsPanel.SetActive(false);

        if (BeforeSceneManager.Instance != null)
        {
            BeforeSceneManager.Instance.ReturnFromARAndShowAchievement();
        }
        else
        {
            Debug.LogError("[AR GoBag] BeforeSceneManager.Instance is null. Ensure BeforeSceneManager exists in the scene.");
        }
    }

    private void ClearSpawnedItems()
    {
        foreach (var go in _spawnedItems)
        {
            if (go != null) Destroy(go);
        }
        _spawnedItems.Clear();
    }

    private void SetPlanesActive(bool active)
    {
        if (planeManager == null) return;

        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(active);

        planeManager.enabled = active;
    }
}
