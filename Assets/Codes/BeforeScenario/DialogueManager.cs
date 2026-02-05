using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("Typing")]
    [SerializeField] private bool useTyping = true;
    [SerializeField] private float typeSpeed = 0.03f;

    public void ShowDialogue(string line)
    {
        // Intentionally left minimal; Show typing/dialogue logic will be implemented later.
    }

    public void ShowGameplayDialogue(string line)
    {
        // Intentionally left minimal; Show gameplay floating dialogue logic will be implemented later.
    }

    public void StopTypingAndShowFull()
    {
        // Intentionally left minimal; this will stop typing coroutine when implemented.
    }
}
