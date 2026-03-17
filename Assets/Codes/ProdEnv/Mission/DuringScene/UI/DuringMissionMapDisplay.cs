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

        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        // Auto-find NPC if not assigned
        if (npcTransform == null)
        {
            var npc = FindObjectOfType<NPCFollower>();
            if (npc != null)
                npcTransform = npc.transform;
        }

        // Setup map background
        if (mapBackgroundImage != null && mapSprite != null)
            mapBackgroundImage.sprite = mapSprite;
    }

    private void SetupAutoBounds()
    {
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
        if (!isVisible) return;

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

        // Normalize position within world bounds
        float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
        float ny = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.z);

        // Map to UI space (centered)
        float mapX = (nx - 0.5f) * mapRect.width;
        float mapY = (ny - 0.5f) * mapRect.height;

        return new Vector2(mapX, mapY);
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
