using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CutsceneDialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;

    public void ShowWelcome(string text)
    {
        // Intentionally left minimal; logic will be implemented during refactor.
    }

    public void PlayCutscene()
    {
        // Intentionally left minimal; logic will be implemented during refactor.
    }

    public void SkipCutscene()
    {
        // Intentionally left minimal; logic will be implemented during refactor.
    }

    public void ShowPostCutsceneDialogue(string text)
    {
        // Intentionally left minimal; logic will be implemented during refactor.
    }
}
