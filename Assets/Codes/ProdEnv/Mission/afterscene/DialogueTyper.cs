using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class DialogueTyper : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private GameObject nextButton; // The 'Next' button in your DialoguePanel

    private string fullText = "The storm has passed, Edward.\nIt's time to go home. But be careful.\nThe flood leaves behind many hidden dangers.\nThis is your final level: The Recovery.";

    public void StartIntroDialogue()
    {
        dialogueText.text = "";
        if (nextButton != null) nextButton.SetActive(false);
        StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        foreach (char letter in fullText.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        if (nextButton != null) nextButton.SetActive(true);
    }
}