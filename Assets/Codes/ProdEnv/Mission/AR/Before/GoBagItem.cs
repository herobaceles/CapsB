using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;

namespace BaHanda.AR
{
    /// <summary>
    /// Draggable item for Go Bag packing.
    /// Handles touch/drag input and collision with drop zone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GoBagItem : MonoBehaviour
    {
        [Header("Item Data")]
        [SerializeField] private GoBagItemData itemData;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject highlightEffect;
        [SerializeField] private GameObject selectedEffect;
        [SerializeField] private ParticleSystem collectParticles;
        [SerializeField] private float dragHeight = 0.15f;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float floatAmplitude = 0.02f;

        [Header("Physics")]
        [SerializeField] private bool useGravityWhenReleased = false;
        [SerializeField] private float returnSpeed = 5f;

        // State
        private bool isDragging = false;
        private bool isCollected = false;
        private Vector3 dragOffset;
        private float dragDistance;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float floatTimer = 0f;

        // Components
        private Camera arCamera;
        private Rigidbody rb;
        private Collider itemCollider;
        private ARSessionController arSession;

        // Events
        public event Action<GoBagItem> OnItemPickedUp;
        public event Action<GoBagItem> OnItemDropped;
        public event Action<GoBagItem> OnItemCollected;

        // Properties
        public GoBagItemData ItemData => itemData;
        public bool IsCollected => isCollected;
        public bool IsDragging => isDragging;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            itemCollider = GetComponent<Collider>();

            // Ensure collider is trigger for overlap detection
            if (itemCollider != null)
                itemCollider.isTrigger = false;

            // Disable gravity initially
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Hide effects
            if (highlightEffect != null) highlightEffect.SetActive(false);
            if (selectedEffect != null) selectedEffect.SetActive(false);
        }

        private void Start()
        {
            arSession = ARSessionController.Instance;
            arCamera = arSession?.ARCamera ?? Camera.main;

            originalPosition = transform.position;
            originalRotation = transform.rotation;

            // Random float phase
            floatTimer = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>
        /// Initialize with item data
        /// </summary>
        public void Initialize(GoBagItemData data)
        {
            itemData = data;
        }

        private void Update()
        {
            if (isCollected) return;

            // Floating animation when not dragging
            if (!isDragging)
            {
                floatTimer += Time.deltaTime * floatSpeed;
                Vector3 pos = transform.position;
                pos.y = originalPosition.y + Mathf.Sin(floatTimer) * floatAmplitude;
                transform.position = pos;
            }

            HandleInput();
        }

        private void HandleInput()
        {
            // Touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        TryStartDrag(touch.position);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (isDragging) UpdateDrag(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (isDragging) EndDrag();
                        break;
                }
            }
            // Mouse input (for editor testing)
            else
            {
                if (Input.GetMouseButtonDown(0))
                    TryStartDrag(Input.mousePosition);
                else if (Input.GetMouseButton(0) && isDragging)
                    UpdateDrag(Input.mousePosition);
                else if (Input.GetMouseButtonUp(0) && isDragging)
                    EndDrag();
            }
        }

        private void TryStartDrag(Vector2 screenPos)
        {
            if (arCamera == null) arCamera = Camera.main;
            if (arCamera == null) return;

            Ray ray = arCamera.ScreenPointToRay(screenPos);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    StartDrag(screenPos, hit.point);
                }
            }
        }

        private void StartDrag(Vector2 screenPos, Vector3 hitPoint)
        {
            isDragging = true;
            dragDistance = Vector3.Distance(arCamera.transform.position, transform.position);
            dragOffset = transform.position - hitPoint;

            // Lift item
            Vector3 liftedPos = transform.position;
            liftedPos.y += dragHeight;
            transform.position = liftedPos;

            // Show selection effect
            if (selectedEffect != null) selectedEffect.SetActive(true);
            if (highlightEffect != null) highlightEffect.SetActive(false);

            // Play pickup sound
            if (itemData?.pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(itemData.pickupSound, transform.position);
            }

            OnItemPickedUp?.Invoke(this);
            Debug.Log($"GoBagItem: Started dragging {itemData?.itemName ?? name}");
        }

        private void UpdateDrag(Vector2 screenPos)
        {
            if (arCamera == null) return;

            Vector3 worldPos = arCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, dragDistance)
            );

            // Apply offset and maintain lifted height
            Vector3 targetPos = worldPos + dragOffset;
            targetPos.y = transform.position.y; // Keep current height

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 15f);
        }

        private void EndDrag()
        {
            isDragging = false;

            // Hide selection effect
            if (selectedEffect != null) selectedEffect.SetActive(false);

            OnItemDropped?.Invoke(this);
            Debug.Log($"GoBagItem: Stopped dragging {itemData?.itemName ?? name}");

            // Check if over drop zone handled by OnTriggerEnter
            if (!isCollected && useGravityWhenReleased)
            {
                ReturnToOriginalPosition();
            }
        }

        private void ReturnToOriginalPosition()
        {
            // Smoothly return to original position
            StartCoroutine(ReturnToPositionCoroutine());
        }

        private System.Collections.IEnumerator ReturnToPositionCoroutine()
        {
            while (Vector3.Distance(transform.position, originalPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, originalPosition, Time.deltaTime * returnSpeed);
                transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation, Time.deltaTime * returnSpeed);
                yield return null;
            }

            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }

        /// <summary>
        /// Called when item successfully dropped in backpack
        /// </summary>
        public void Collect()
        {
            if (isCollected) return;

            isCollected = true;
            isDragging = false;

            Debug.Log($"GoBagItem: Collected {itemData?.itemName ?? name}");

            // Play effects
            if (collectParticles != null)
            {
                collectParticles.transform.SetParent(null);
                collectParticles.Play();
                Destroy(collectParticles.gameObject, collectParticles.main.duration);
            }

            if (itemData?.packSound != null)
            {
                AudioSource.PlayClipAtPoint(itemData.packSound, transform.position);
            }

            OnItemCollected?.Invoke(this);

            // Shrink and destroy
            StartCoroutine(CollectAnimation());
        }

        private System.Collections.IEnumerator CollectAnimation()
        {
            Vector3 startScale = transform.localScale;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if entering drop zone while dragging
            GoBagDropZone dropZone = other.GetComponent<GoBagDropZone>();
            if (dropZone != null)
            {
                if (highlightEffect != null && !isDragging)
                    highlightEffect.SetActive(true);

                if (isDragging)
                {
                    // Immediately collect if dropped in zone
                    dropZone.ReceiveItem(this);
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // If released while inside drop zone
            GoBagDropZone dropZone = other.GetComponent<GoBagDropZone>();
            if (dropZone != null && !isDragging && !isCollected)
            {
                dropZone.ReceiveItem(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            GoBagDropZone dropZone = other.GetComponent<GoBagDropZone>();
            if (dropZone != null)
            {
                if (highlightEffect != null)
                    highlightEffect.SetActive(false);
            }
        }
    }
}
