using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARTapDetector : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private HiddenDangerSpawner hiddenDangerSpawner; 

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // --- AR Drag & Drop Variables ---
    private HiddenDangerItem draggedItem;
    private float dragDepth;

    void Start()
    {
        if (ARRuntimeContext.Instance != null)
            arRaycastManager = ARRuntimeContext.Instance.ResolveRaycastManager(arRaycastManager);
        else if (arRaycastManager == null)
            arRaycastManager = FindObjectOfType<ARRaycastManager>(true);

        if (hiddenDangerSpawner == null)
            hiddenDangerSpawner = FindObjectOfType<HiddenDangerSpawner>(true);
    }

    void Update()
    {
        if (arRaycastManager == null)
        {
            if (ARRuntimeContext.Instance != null)
                arRaycastManager = ARRuntimeContext.Instance.ResolveRaycastManager(arRaycastManager);
            if (arRaycastManager == null) return;
        }

        bool isKitchenMission = false;
        bool isDisinfectMission = false;

        if (AfterRecoveryARController.Instance != null)
        {
            isKitchenMission = AfterRecoveryARController.Instance.currentMissionMode == MissionMode.KitchenSafety;
            isDisinfectMission = AfterRecoveryARController.Instance.currentMissionMode == MissionMode.DisinfectHouse;
        }

        // --- Touch Input (For Mobile) ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // Prevent tapping through UI
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                if (touch.phase == TouchPhase.Began)
                {
                    Debug.LogWarning("ARTapDetector: Tap BLOCKED by UI! An invisible Canvas or Text panel is blocking the screen. Uncheck 'Raycast Target' on your background UI elements.");
                }
                return;
            }

            if (touch.phase == TouchPhase.Began)
            {
                if (isKitchenMission)
                {
                    bool hitItem = TryTagItem(touch.position);
                    if (!hitItem) DetectSpawnTap(touch.position);
                }
                else if (isDisinfectMission)
                {
                    // If in Disinfect mode, try to pick up mud!
                    TryCleanMud(touch.position);
                }
                else
                {
                    bool itemHandled = TryPickup(touch.position);
                    if (!itemHandled) DetectSpawnTap(touch.position);
                }
            }
            else if (touch.phase == TouchPhase.Moved && draggedItem != null && !isKitchenMission && !isDisinfectMission)
            {
                DragItem(touch.position);
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && draggedItem != null && !isKitchenMission && !isDisinfectMission)
            {
                TryDropItem(touch.position);
            }
        }
        // --- Mouse Input (For Editor Testing) ---
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Prevent clicking through UI
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.LogWarning("ARTapDetector: Mouse Click BLOCKED by UI! An invisible Canvas or Text panel is blocking the screen. Uncheck 'Raycast Target' on your background UI elements.");
                    return;
                }

                if (isKitchenMission)
                {
                    bool hitItem = TryTagItem(Input.mousePosition);
                    if (!hitItem) DetectSpawnTap(Input.mousePosition);
                }
                else if (isDisinfectMission)
                {
                    // If in Disinfect mode, try to pick up mud!
                    TryCleanMud(Input.mousePosition);
                }
                else
                {
                    bool itemHandled = TryPickup(Input.mousePosition);
                    if (!itemHandled) DetectSpawnTap(Input.mousePosition);
                }
            }
            else if (Input.GetMouseButton(0) && draggedItem != null && !isKitchenMission && !isDisinfectMission)
            {
                DragItem(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && draggedItem != null && !isKitchenMission && !isDisinfectMission)
            {
                TryDropItem(Input.mousePosition);
            }
        }
    }

    private Camera GetARCamera()
    {
        Camera currentCam = null;
        if (ARRuntimeContext.Instance != null)
            currentCam = ARRuntimeContext.Instance.ResolveARCamera();
        
        if (currentCam == null)
            currentCam = Camera.main; 

        return currentCam;
    }

    // ==========================================
    // DISINFECT HOUSE LOGIC
    // ==========================================
    bool TryCleanMud(Vector2 screenPosition)
    {
        Camera currentCam = GetARCamera();
        if (currentCam == null) 
        {
            Debug.LogError("TryCleanMud: Failed because no Camera was found!");
            return false;
        }

        Ray ray = currentCam.ScreenPointToRay(screenPosition);
        RaycastHit[] physicsHits = Physics.RaycastAll(ray, 50f);
        
        Debug.Log($"TryCleanMud: Clicked the screen! Raycast shot forward and hit {physicsHits.Length} objects.");

        foreach (RaycastHit hit in physicsHits)
        {
            Debug.Log($"TryCleanMud: The raycast touched -> {hit.collider.gameObject.name}");

            // Try to find the MudPileInteraction script on the object we tapped
            MudPileInteraction mud = hit.collider.GetComponent<MudPileInteraction>();
            if (mud == null) 
            {
                mud = hit.collider.GetComponentInParent<MudPileInteraction>();
            }

            // If we hit a mud pile AND it's not already being held
            if (mud != null && !mud.isHeld)
            {
                Debug.Log("TryCleanMud: SUCCESS! The mud script was found. Picking up mud and waking up button...");
                
                // 1. Pick it up (snap to camera)
                mud.PickUpMud(currentCam); 
                
                // 2. Find the Disinfect Button in the scene and show it
                DisinfectButton disinfectBtn = FindObjectOfType<DisinfectButton>(true);
                if (disinfectBtn != null)
                {
                    disinfectBtn.ShowButtonForMud(mud);
                }
                else
                {
                    Debug.LogWarning("TryCleanMud: Mud was clicked, but DisinfectButton could not be found in the scene!");
                }

                return true;
            }
        }
        return false;
    }

    // ==========================================
    // KITCHEN SAFETY LOGIC
    // ==========================================
    bool TryTagItem(Vector2 screenPosition)
    {
        Camera currentCam = GetARCamera();
        if (currentCam == null) return false;

        Ray ray = currentCam.ScreenPointToRay(screenPosition);
        RaycastHit[] physicsHits = Physics.RaycastAll(ray, 50f);
        
        foreach (RaycastHit hit in physicsHits)
        {
            HiddenDangerItem tappedItem = hit.collider.GetComponentInParent<HiddenDangerItem>();
            
            if (tappedItem != null && !tappedItem.IsRecovered)
            {
                bool isSafe = hit.collider.CompareTag("SafeItem") || tappedItem.gameObject.CompareTag("SafeItem");
                bool isUnsafe = hit.collider.CompareTag("UnsafeItem") || tappedItem.gameObject.CompareTag("UnsafeItem");

                if (isSafe)
                {
                    if (AfterRecoveryARController.Instance != null)
                        AfterRecoveryARController.Instance.TriggerFeedback(true, tappedItem.transform.position); 
                    
                    tappedItem.Recover(); 
                    return true;
                }
                else if (isUnsafe)
                {
                    if (AfterRecoveryARController.Instance != null)
                        AfterRecoveryARController.Instance.TriggerFeedback(false, tappedItem.transform.position); 
                    
                    return true;
                }
            }
        }
        return false; 
    }

    // ==========================================
    // DRAG AND DROP LOGIC
    // ==========================================

    bool TryPickup(Vector2 screenPosition)
    {
        Camera currentCam = GetARCamera();
        if (currentCam == null) return false;

        Ray ray = currentCam.ScreenPointToRay(screenPosition);
        RaycastHit[] physicsHits = Physics.RaycastAll(ray, 50f);
        
        foreach (RaycastHit hit in physicsHits)
        {
            HiddenDangerItem tappedItem = hit.collider.GetComponentInParent<HiddenDangerItem>();
            if (tappedItem != null && !tappedItem.IsRecovered)
            {
                // For Hidden Danger mission, we want to drag, not recover on pickup
                if (AfterRecoveryARController.Instance != null && 
                    AfterRecoveryARController.Instance.currentMissionMode == MissionMode.HiddenDanger)
                {
                    draggedItem = tappedItem;
                    dragDepth = hit.distance;
                    return true;
                }
                else
                {
                    // For CleanupGear and other missions, we should NOT recover here
                    // The OnMouseDown in HiddenDangerItem.cs already handles recovery
                    // Just return true to indicate we hit an item
                    Debug.Log($"ARTapDetector: Item '{tappedItem.name}' detected in {AfterRecoveryARController.Instance.currentMissionMode} mode - letting OnMouseDown handle it");
                    return true;
                }
            }
        }
        return false;
    }

    void DragItem(Vector2 screenPosition)
    {
        Camera currentCam = GetARCamera();
        if (currentCam == null || draggedItem == null) return;

        Vector3 targetPosition = currentCam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, dragDepth));
        draggedItem.transform.position = Vector3.Lerp(draggedItem.transform.position, targetPosition, Time.deltaTime * 15f);
    }

    void TryDropItem(Vector2 screenPosition)
    {
        if (draggedItem == null) return;

        Camera currentCam = GetARCamera();
        if (currentCam == null) 
        {
            draggedItem = null;
            return;
        }

        Ray ray = currentCam.ScreenPointToRay(screenPosition);
        RaycastHit[] physicsHits = Physics.RaycastAll(ray, 50f);
        
        foreach (RaycastHit hit in physicsHits)
        {
            if (hit.collider.gameObject.name.ToLower().Contains("bucket"))
            {
                // Show green check at the bucket position
                if (AfterRecoveryARController.Instance != null)
                {
                    AfterRecoveryARController.Instance.TriggerFeedback(true, hit.collider.transform.position);
                }
                
                // Recover the item (this will call HandleItemRecovered)
                draggedItem.Recover();
                break;
            }
        }

        draggedItem = null; 
    }

    // ==========================================
    // SPAWN LOGIC FIX
    // ==========================================
    void DetectSpawnTap(Vector2 screenPosition)
    {
        bool tapDetected = false;

        // 1. Try AR Raycast first (detects AR Planes on Mobile)
        if (arRaycastManager != null && arRaycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            tapDetected = true;
        }
        // 2. Fallback to normal Physics Raycast (detects standard invisible wall in Editor)
        else
        {
            Camera currentCam = GetARCamera();
            if (currentCam != null)
            {
                Ray ray = currentCam.ScreenPointToRay(screenPosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 50f))
                {
                    tapDetected = true;
                }
            }
        }

        if (tapDetected)
        {
            if (hiddenDangerSpawner == null)
                hiddenDangerSpawner = FindObjectOfType<HiddenDangerSpawner>(true);

            if (hiddenDangerSpawner != null)
            {
                // FIX: Ignore the mouse click coordinates! Always force it exactly to 0, 0, 0.
                hiddenDangerSpawner.SpawnHiddenDangers(Vector3.zero);
            }
        }
    }

    public void SetHiddenDangerSpawner(HiddenDangerSpawner spawner)
    {
        hiddenDangerSpawner = spawner;
    }
}