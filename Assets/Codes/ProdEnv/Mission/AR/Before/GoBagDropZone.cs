using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace BaHanda.AR
{
    /// <summary>
    /// Drop zone for the Go Bag (backpack).
    /// Receives items dragged into it and triggers collection.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GoBagDropZone : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private GameObject idleVisual;
        [SerializeField] private GameObject readyVisual;
        [SerializeField] private GameObject fullVisual;
        [SerializeField] private ParticleSystem receiveParticles;
        [SerializeField] private Animator animator;
        [SerializeField] private string receiveAnimTrigger = "ItemReceived";

        [Header("Audio")]
        [SerializeField] private AudioClip itemReceivedSound;
        [SerializeField] private AudioClip bagFullSound;

        [Header("Capacity")]
        [SerializeField] private int maxCapacity = 10;

        [Header("Events")]
        public UnityEvent<GoBagItem> OnItemReceived;
        public UnityEvent OnBagFull;

        // State
        private Collider zoneCollider;
        private int itemsReceived = 0;
        private bool isFull = false;
        private HashSet<string> receivedItemIds = new HashSet<string>();

        // Events
        public event Action<GoBagItem> OnItemDropped;
        public event Action OnCapacityReached;

        // Properties
        public int ItemsReceived => itemsReceived;
        public bool IsFull => isFull;
        public bool HasItem(string itemId) => receivedItemIds.Contains(itemId);

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            UpdateVisuals(DropZoneState.Idle);
        }

        /// <summary>
        /// Receive an item into the bag
        /// </summary>
        public void ReceiveItem(GoBagItem item)
        {
            if (item == null || item.IsCollected) return;
            if (isFull)
            {
                Debug.Log("GoBagDropZone: Bag is full!");
                return;
            }

            string itemId = item.ItemData?.itemId ?? item.name;

            // Prevent duplicates
            if (receivedItemIds.Contains(itemId))
            {
                Debug.Log($"GoBagDropZone: Already have {itemId}");
                return;
            }

            // Accept the item
            receivedItemIds.Add(itemId);
            itemsReceived++;

            Debug.Log($"GoBagDropZone: Received {item.ItemData?.itemName ?? item.name} ({itemsReceived} items)");

            // Trigger item collection
            item.Collect();

            // Play effects
            PlayReceiveEffects();

            // Notify listeners
            OnItemReceived?.Invoke(item);
            OnItemDropped?.Invoke(item);

            // Check capacity
            if (itemsReceived >= maxCapacity)
            {
                isFull = true;
                UpdateVisuals(DropZoneState.Full);
                PlaySound(bagFullSound);
                OnBagFull?.Invoke();
                OnCapacityReached?.Invoke();
            }
        }

        private void PlayReceiveEffects()
        {
            // Play animation
            if (animator != null && !string.IsNullOrEmpty(receiveAnimTrigger))
            {
                animator.SetTrigger(receiveAnimTrigger);
            }

            // Play particles
            if (receiveParticles != null)
            {
                receiveParticles.Play();
            }

            // Play sound
            PlaySound(itemReceivedSound);
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
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        private void UpdateVisuals(DropZoneState state)
        {
            if (idleVisual != null) idleVisual.SetActive(state == DropZoneState.Idle);
            if (readyVisual != null) readyVisual.SetActive(state == DropZoneState.Ready);
            if (fullVisual != null) fullVisual.SetActive(state == DropZoneState.Full);
        }

        private void OnTriggerEnter(Collider other)
        {
            GoBagItem item = other.GetComponent<GoBagItem>();
            if (item != null && !isFull)
            {
                UpdateVisuals(DropZoneState.Ready);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            GoBagItem item = other.GetComponent<GoBagItem>();
            if (item != null && !isFull)
            {
                UpdateVisuals(DropZoneState.Idle);
            }
        }

        /// <summary>
        /// Reset the drop zone
        /// </summary>
        public void Reset()
        {
            itemsReceived = 0;
            isFull = false;
            receivedItemIds.Clear();
            UpdateVisuals(DropZoneState.Idle);
        }

        private enum DropZoneState
        {
            Idle,
            Ready,
            Full
        }
    }
}
