using UnityEngine;

public class BagDropZone : MonoBehaviour
{
    [Header("Game Manager (auto-assigned at runtime)")]
    public PlanePlacementAndGameManager manager;

    [Header("Optional visuals")]
    public Renderer bagRenderer;
    public Material normalMaterial;
    public Material highlightMaterial;

    private void OnTriggerEnter(Collider other)
    {
        // Only react to items
        ItemIdentity ident = other.GetComponent<ItemIdentity>();
        if (ident == null) return;

        // Visual highlight
        Highlight(true);

        // Gameplay: tell manager an item entered the bag
        if (manager != null)
        {
            manager.HandleItemDroppedIntoBag(ident, other.gameObject);
        }
        else
        {
            Debug.LogError("[BagDropZone] manager is NULL. Assign it from PlanePlacementAndGameManager after spawning.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<ItemIdentity>() == null) return;
        Highlight(false);
    }

    public void Highlight(bool on)
    {
        if (bagRenderer == null) return;
        if (highlightMaterial == null || normalMaterial == null) return;

        bagRenderer.material = on ? highlightMaterial : normalMaterial;
    }
}
