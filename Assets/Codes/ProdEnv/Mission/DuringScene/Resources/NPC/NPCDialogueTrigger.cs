using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("NPC Reference")]
    [SerializeField] private NPCFollower npcFollower;

    [Header("Dialogue")]
    [SerializeField] private List<string> dialogueLines = new List<string>();
    [SerializeField] private float perLineDuration = 3f;

    [Header("Triggering")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Reset()
    {
        // Try to auto-wire references when the component is first added
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;

        if (npcFollower == null)
            npcFollower = GetComponentInParent<NPCFollower>();
    }

    private void Awake()
    {
        if (npcFollower == null)
            npcFollower = GetComponentInParent<NPCFollower>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
        if (triggerOnce && hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        PlayDialogue();
    }

    public void PlayDialogue()
    {
        if (dialogueLines.Count == 0 || npcFollower == null)
            return;

        foreach (string line in dialogueLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
                npcFollower.SpeakLine(line, perLineDuration);
        }

        hasTriggered = true;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
