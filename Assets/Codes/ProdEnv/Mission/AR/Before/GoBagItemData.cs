using UnityEngine;

namespace BaHanda.AR
{
    /// <summary>
    /// ScriptableObject defining an item for the Go Bag emergency kit.
    /// Create via: Assets > Create > BaHanda > AR > Go Bag Item
    /// </summary>
    [CreateAssetMenu(fileName = "NewGoBagItem", menuName = "BaHanda/AR/Go Bag Item")]
    public class GoBagItemData : ScriptableObject
    {
        [Header("Item Info")]
        public string itemId;
        public string itemName;
        [TextArea(1, 3)]
        public string itemDescription;

        [Header("Visuals")]
        public Sprite itemIcon;
        public GameObject itemPrefab;
        public Color itemColor = Color.white;

        [Header("Requirements")]
        [Tooltip("Is this item required to complete the mission?")]
        public bool isRequired = true;

        [Tooltip("Number of this item needed (usually 1)")]
        public int requiredCount = 1;

        [Header("Rewards")]
        public int pointsPerItem = 10;

        [Header("Hints")]
        [TextArea(1, 2)]
        public string hintText;

        [Header("Audio")]
        public AudioClip pickupSound;
        public AudioClip packSound;
    }
}
