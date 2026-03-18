using UnityEngine;
using UnityEngine.UI;

public class DisinfectButton : MonoBehaviour
{
    [Header("Optional Polish")]
    [Tooltip("Drag the Spray Bottle's Particle System here if you have one!")]
    public ParticleSystem sprayEffect;
    
    [Tooltip("Add an AudioSource with a spray sound if you want!")]
    public AudioSource spraySound;

    // This remembers which mud pile the player is currently "holding"
    private MudPileInteraction heldMud;

    // --- NEW METHOD ADDED HERE ---
    // Link your Main Menu's "Disinfect Mission" button to this function!
    public void SetDisinfectMissionID()
    {
        PlayerPrefs.SetString("SelectedMissionID", "disinfectmission");
        PlayerPrefs.Save();
        Debug.Log("Mission ID set to: disinfectmission");
    }
    // -----------------------------

    // This gets called by your ARTapDetector when the player taps a mud pile
    public void ShowButtonForMud(MudPileInteraction tappedMud)
    {
        heldMud = tappedMud;
        gameObject.SetActive(true); // Reveal the Disinfect Button
    }

    // This method will be triggered when the in-game UI Button is pressed
    public void CleanHeldMud()
    {
        // 1. Play the effects if we assigned them
        if (sprayEffect != null) sprayEffect.Play();
        if (spraySound != null) spraySound.Play();

        // 2. Clean the specific mud we are holding
        if (heldMud != null)
        {
            heldMud.CleanPile(); 
            heldMud = null; // Clear the reference so we don't clean it twice
        }

        // 3. Hide the button again until the player taps another mud pile
        gameObject.SetActive(false);
    }
}