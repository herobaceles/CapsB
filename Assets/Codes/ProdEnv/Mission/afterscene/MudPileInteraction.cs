using UnityEngine;

/// <summary>
/// Tap-interactable component placed on mud pile / hidden danger prefabs.
///
/// Initialized at spawn time by HiddenDangerSpawner.StartSpawning() via Initialize().
/// OnMouseDown() responds to both mouse clicks (editor/desktop) and mobile first-touch
/// (Unity maps the primary touch to mouse events automatically).
///
/// On interaction: plays a clean-up effect, disables its own collider to prevent
/// re-activation, and reports completion to HiddenDangerSpawner.
/// HiddenDangerSpawner is the single aggregation point — it forwards to
/// MissionSceneManager.UpdateObjective() and decides when to end the AR session.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MudPileInteraction : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector fields
    // -----------------------------------------------------------------------

    [Header("Feedback")]
    [SerializeField] private GameObject cleanEffect;
    [SerializeField] private AudioClip interactSound;

    // -----------------------------------------------------------------------
    // Private state (injected by HiddenDangerSpawner)
    // -----------------------------------------------------------------------

    private HiddenDangerSpawner parentSpawner;
    private string objectiveId;
    private bool cleaned;

    // -----------------------------------------------------------------------
    // Initialization
    // -----------------------------------------------------------------------

    /// <summary>
    /// Called by HiddenDangerSpawner immediately after Instantiate.
    /// Provides the spawner back-reference and the objective ID for this session.
    /// </summary>
    public void Initialize(HiddenDangerSpawner spawner, string objective)
    {
        parentSpawner = spawner;
        objectiveId   = objective;
        cleaned       = false;
    }

    // -----------------------------------------------------------------------
    // Interaction
    // -----------------------------------------------------------------------

    private void OnMouseDown()
    {
        Interact();
    }

    /// <summary>
    /// Programmatic entry point — can also be wired to a UI EventTrigger or AR gesture event.
    /// </summary>
    public void Interact()
    {
        if (cleaned) return;

        cleaned = true;

        // Disable collider immediately so follow-up taps are ignored
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        PlayCleanEffect();

        // Notify spawner — it handles objective update and session completion check
        parentSpawner?.OnDangerFound();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private void PlayCleanEffect()
    {
        if (cleanEffect != null)
        {
            cleanEffect.SetActive(true);
        }
        else
        {
            // Fallback: hide renderer so the pile visually disappears
            var rend = GetComponent<Renderer>();
            if (rend != null)
                rend.enabled = false;
        }

        if (interactSound != null)
        {
            var src = GetComponent<AudioSource>();
            if (src != null)
                src.PlayOneShot(interactSound);
            else
                AudioSource.PlayClipAtPoint(interactSound, transform.position);
        }
    }
}
