using UnityEngine;

[CreateAssetMenu(menuName = "AR Game/Item Data", fileName = "NewItemData")]
public class ItemData : ScriptableObject
{
    public string itemId = "item_001";
    public string displayName = "Item";
    public Sprite icon;
}
 