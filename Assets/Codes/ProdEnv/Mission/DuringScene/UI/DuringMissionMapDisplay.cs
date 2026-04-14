using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Mobile Legends-style minimap display for the During phase.
/// Shows player position, NPC markers, hazard zones, and evacuation route.
/// </summary>
public class DuringMissionMapDisplay : MonoBehaviour
{
    public static DuringMissionMapDisplay Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject mapPanelRoot;
    [SerializeField] private RectTransform mapContainer;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeButton;

    [Header("Map Background")]
    [SerializeField] private Image mapBackgroundImage;
    [SerializeField] private Sprite mapSprite;

    [Header("Player Marker")]
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private Transform playerTransform;

    [Header("NPC Marker")]
    [SerializeField] private RectTransform npcMarker;
    [SerializeField] private Transform npcTransform;

    [Header("Task Markers")]
    [SerializeField] private GameObject taskMarkerPrefab;
    [SerializeField] private Transform taskMarkerContainer;
    [SerializeField] private Color activeTaskColor = Color.yellow;
    [SerializeField] private Color completedTaskColor = Color.green;
    [SerializeField] private Color pendingTaskColor = Color.gray;

    [Header("Evacuation Route")]
    [SerializeField] private RectTransform evacuationMarker;
    [SerializeField] private bool showEvacuationRoute = true;

    [Header("World Bounds (for mapping positions)")]
    [SerializeField] private Vector2 worldMin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 worldMax = new Vector2(50f, 50f);

    [SerializeField, Tooltip("Optional: world-space Transform used as bottom-left corner of the playable area.")]
    private Transform worldMinRef;
    [SerializeField, Tooltip("Optional: world-space Transform used as top-right corner of the playable area.")]
    private Transform worldMaxRef;

    [Header("Marker Position Tuning")]
    [SerializeField] private Vector2 mapPositionOffset = Vector2.zero;
    [SerializeField] private bool invertY = false;
    [SerializeField, Tooltip("Rotate world XZ before mapping to the minimap (degrees). Use this to align markers with an isometric/top-down map texture.")]
    private float mapRotationDegrees = 0f;

    [Header("Auto Bounds")]
    [SerializeField] private bool autoBoundsFromTerrain = true;
    [SerializeField] private bool logBounds = false;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    public UnityEvent OnMapOpened;
    public UnityEvent OnMapClosed;

    private List<RectTransform> spawnedTaskMarkers = new List<RectTransform>();
    private Coroutine animationRoutine;
    private bool isVisible;

    // Internal debug flags so we don't spam the console every frame
    private bool triedFindPlayer;
    private bool triedFindNpc;

    public bool IsVisible => isVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mapPanelRoot != null)
            mapPanelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(HideMap);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HideMap);
    }

    private void Start()
    {
        SetupAutoBounds();
        EnsurePlayerReference();
        EnsureNpcReference();

        // Setup map background
        if (mapBackgroundImage != null && mapSprite != null)
            mapBackgroundImage.sprite = mapSprite;
    }

    private void EnsurePlayerReference()
    {
        if (playerTransform != null)
            return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            triedFindPlayer = false;
            Debug.Log("DuringMissionMapDisplay: Found Player by tag.");
        }
        else if (!triedFindPlayer)
        {
            triedFindPlayer = true;
            Debug.LogWarning("DuringMissionMapDisplay: No GameObject with tag 'Player' found. Player marker will not move.");
        }
    }

    private void EnsureNpcReference()
    {
        if (npcTransform != null)
            return;

        var npc = FindObjectOfType<NPCFollower>();
        if (npc != null)
        {
            npcTransform = npc.transform;
            triedFindNpc = false;
            Debug.Log("DuringMissionMapDisplay: Found NPCFollower instance.");
        }
        else if (!triedFindNpc)
        {
            triedFindNpc = true;
            Debug.LogWarning("DuringMissionMapDisplay: No NPCFollower found in scene. NPC marker will not move.");
        }
    }

    private void SetupAutoBounds()
    {
        // Prefer explicit reference points if provided.
        if (worldMinRef != null && worldMaxRef != null)
        {
            worldMin = new Vector2(worldMinRef.position.x, worldMinRef.position.z);
            worldMax = new Vector2(worldMaxRef.position.x, worldMaxRef.position.z);

            if (logBounds)
            {
                Debug.Log($"DuringMissionMapDisplay: Using Transform bounds Min {worldMin} Max {worldMax}");
            }
            return;
        }

        if (!autoBoundsFromTerrain)
            return;

        var terrain = Terrain.activeTerrain;
        if (terrain == null || terrain.terrainData == null)
            return;

        Vector3 pos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        worldMin = new Vector2(pos.x, pos.z);
        worldMax = new Vector2(pos.x + size.x, pos.z + size.z);

        if (logBounds)
        {
            Debug.Log($"DuringMissionMapDisplay: Auto-set world bounds to Min {worldMin} Max {worldMax}");
        }
    }

    private void LateUpdate()
    {
        // Handle dynamically spawned characters: keep trying to find them by tag/type
        if (playerTransform == null)
            EnsurePlayerReference();

        if (npcTransform == null)
            EnsureNpcReference();

        // Always update markers so the minimap stays live,
        // even if the panel is currently hidden/animated.
        UpdatePlayerMarker();
        UpdateNPCMarker();
    }

    #region Public API

    /// <summary>
    /// Show the minimap with animation.
    /// </summary>
    public void ShowMap()
    {
        if (isVisible) return;

        if (mapPanelRoot == null)
        {
            Debug.LogWarning("DuringMissionMapDisplay: mapPanelRoot not assigned.");
            return;
        }

        isVisible = true;
        mapPanelRoot.SetActive(true);

        RefreshTaskMarkers();

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimateShow());

        OnMapOpened?.Invoke();
        Debug.Log("DuringMissionMapDisplay: Map opened.");
    }

    /// <summary>
    /// Hide the minimap with animation.
    /// </summary>
    public void HideMap()
    {
        if (!isVisible) return;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimateHide());

        OnMapClosed?.Invoke();
        Debug.Log("DuringMissionMapDisplay: Map closed.");
    }

    /// <summary>
    /// Toggle map visibility.
    /// </summary>
    public void ToggleMap()
    {
        if (isVisible)
            HideMap();
        else
            ShowMap();
    }

    /// <summary>
    /// Set map bounds for world-to-map coordinate conversion.
    /// </summary>
    public void SetWorldBounds(Vector2 min, Vector2 max)
    {
        worldMin = min;
        worldMax = max;
    }

    /// <summary>
    /// Refresh task markers based on current mission state.
    /// </summary>
    public void RefreshTaskMarkers()
    {
        if (taskMarkerContainer == null || taskMarkerPrefab == null)
            return;

        // Clear existing markers
        foreach (var marker in spawnedTaskMarkers)
        {
            if (marker != null)
                Destroy(marker.gameObject);
        }
        spawnedTaskMarkers.Clear();

        // Get current mission data
        var manager = DuringMissionManager.Instance;
        if (manager == null || manager.CurrentMission == null)
            return;

        int currentIndex = manager.CurrentTaskIndex;

        for (int i = 0; i < manager.CurrentMission.tasks.Count; i++)
        {
            var task = manager.CurrentMission.tasks[i];
            GameObject markerObj = Instantiate(taskMarkerPrefab, taskMarkerContainer);
            RectTransform markerRect = markerObj.GetComponent<RectTransform>();

            if (markerRect != null)
            {
                // Color based on status
                Image img = markerObj.GetComponent<Image>();
                if (img != null)
                {
                    if (i < currentIndex)
                        img.color = completedTaskColor;
                    else if (i == currentIndex)
                        img.color = activeTaskColor;
                    else
                        img.color = pendingTaskColor;
                }

                // Position marker (placeholder - you'd map actual zone positions)
                // For now, distribute them around the map
                float angle = (float)i / manager.CurrentMission.tasks.Count * Mathf.PI * 2f;
                float radius = 80f;
                markerRect.anchoredPosition = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );

                spawnedTaskMarkers.Add(markerRect);
            }
        }

        // Show/hide evacuation marker
        if (evacuationMarker != null)
        {
            evacuationMarker.gameObject.SetActive(showEvacuationRoute && currentIndex >= manager.TotalTasks - 2);
        }
    }

    #endregion

    #region Position Mapping

    private void UpdatePlayerMarker()
    {
        if (playerMarker == null || playerTransform == null || mapContainer == null)
            return;

        Vector2 mapPos = WorldToMapPosition(playerTransform.position);
        playerMarker.anchoredPosition = mapPos;

        // Rotate marker to match player facing
        float yaw = playerTransform.eulerAngles.y;
        playerMarker.localRotation = Quaternion.Euler(0f, 0f, -yaw);
    }

    private void UpdateNPCMarker()
    {
        if (npcMarker == null || npcTransform == null || mapContainer == null)
            return;

        Vector2 mapPos = WorldToMapPosition(npcTransform.position);
        npcMarker.anchoredPosition = mapPos;
    }

    /// <summary>
    /// Convert world XZ position to map UI position.
    /// </summary>
    private Vector2 WorldToMapPosition(Vector3 worldPos)
    {
        if (mapContainer == null)
            return Vector2.zero;

        Rect mapRect = mapContainer.rect;

        // Optionally rotate world XZ around the world-bounds center so
        // marker movement matches an isometric/top-down map texture.
        Vector2 worldCenter = (worldMin + worldMax) * 0.5f;
        Vector2 local = new Vector2(worldPos.x, worldPos.z) - worldCenter;

        if (Mathf.Abs(mapRotationDegrees) > 0.01f)
        {
            float rad = mapRotationDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float rx = local.x * cos - local.y * sin;
            float ry = local.x * sin + local.y * cos;
            local = new Vector2(rx, ry);
        }

        Vector2 rotatedWorld = worldCenter + local;

        // Normalize position within world bounds (after rotation)
        float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, rotatedWorld.x);
        float ny = Mathf.InverseLerp(worldMin.y, worldMax.y, rotatedWorld.y);

        if (invertY)
            ny = 1f - ny;

        // Map to UI space (centered)
        float mapX = (nx - 0.5f) * mapRect.width;
        float mapY = (ny - 0.5f) * mapRect.height;

        return new Vector2(mapX, mapY) + mapPositionOffset;
    }

    #endregion

    #region Animation

    private IEnumerator AnimateShow()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        mapPanelRoot.transform.localScale = Vector3.one * 0.8f;

        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = showCurve.Evaluate(elapsed / showDuration);

            canvasGroup.alpha = t;
            mapPanelRoot.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        mapPanelRoot.transform.localScale = Vector3.one;
        animationRoutine = null;
    }

    private IEnumerator AnimateHide()
    {
        if (canvasGroup == null)
        {
            isVisible = false;
            if (mapPanelRoot != null)
                mapPanelRoot.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < hideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / hideDuration;

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            mapPanelRoot.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, t);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        isVisible = false;
        mapPanelRoot.SetActive(false);
        animationRoutine = null;
    }

    #endregion
}
