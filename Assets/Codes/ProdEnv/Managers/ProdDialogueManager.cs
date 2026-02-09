using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ProdDialogueManager : MonoBehaviour
{
    public static ProdDialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image characterPortrait;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonText;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private bool allowSkipTyping = true;

    private Queue<ProdDialogueLine> dialogueQueue = new Queue<ProdDialogueLine>();
    private bool isTyping = false;
    private bool skipRequested = false;
    private Coroutine typingCoroutine;
    private UnityAction onDialogueComplete;

    [System.Serializable]
    public class CharacterPreset
    {
        public string characterId;
        public string displayName;
        public Sprite portrait;
    }

    [Header("Character Presets")]
    [SerializeField] private List<CharacterPreset> characterPresets = new List<CharacterPreset>();
    private Dictionary<string, CharacterPreset> characterLookup = new Dictionary<string, CharacterPreset>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var preset in characterPresets)
        {
            if (!string.IsNullOrEmpty(preset.characterId))
                characterLookup[preset.characterId] = preset;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }

    public void ShowDialogue(string characterName, string message, Sprite portrait = null, UnityAction onComplete = null)
    {
        var line = new ProdDialogueLine
        {
            characterName = characterName,
            message = message,
            portrait = portrait
        };

        dialogueQueue.Clear();
        dialogueQueue.Enqueue(line);
        onDialogueComplete = onComplete;

        ShowDialoguePanel();
        DisplayNextLine();
    }

    public void ShowDialogue(string characterId, string message, UnityAction onComplete = null)
    {
        if (characterLookup.TryGetValue(characterId, out CharacterPreset preset))
        {
            ShowDialogue(preset.displayName, message, preset.portrait, onComplete);
        }
        else
        {
            ShowDialogue(characterId, message, null, onComplete);
        }
    }

    public void ShowDialogueSequence(List<ProdDialogueLine> lines, UnityAction onComplete = null)
    {
        dialogueQueue.Clear();
        foreach (var line in lines)
        {
            dialogueQueue.Enqueue(line);
        }
        onDialogueComplete = onComplete;

        ShowDialoguePanel();
        DisplayNextLine();
    }

    public void ShowProfessorDialogue(string message, UnityAction onComplete = null)
    {
        ShowDialogue("Professor Lingap", message, null, onComplete);
    }

    public ProdDialogueSequenceBuilder CreateSequence()
    {
        return new ProdDialogueSequenceBuilder(this);
    }

    public void HideDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        isTyping = false;
        dialogueQueue.Clear();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

    private void ShowDialoguePanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
    }

    private void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            HideDialogue();
            onDialogueComplete?.Invoke();
            onDialogueComplete = null;
            return;
        }

        ProdDialogueLine line = dialogueQueue.Dequeue();

        if (characterNameText != null)
            characterNameText.text = line.characterName;

        if (characterPortrait != null)
        {
            if (line.portrait != null)
            {
                characterPortrait.sprite = line.portrait;
                characterPortrait.gameObject.SetActive(true);
            }
            else
            {
                characterPortrait.gameObject.SetActive(false);
            }
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.message));
    }

    private IEnumerator TypeText(string message)
    {
        isTyping = true;
        skipRequested = false;
        
        if (dialogueText == null)
        {
            Debug.LogError("ProdDialogueManager: dialogueText is not assigned! Please assign it in the Inspector.");
            isTyping = false;
            yield break;
        }
        
        dialogueText.text = "";

        if (continueButtonText != null)
            continueButtonText.text = "Skip >>";

        foreach (char c in message)
        {
            if (skipRequested)
            {
                dialogueText.text = message;
                break;
            }

            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        if (continueButtonText != null)
            continueButtonText.text = dialogueQueue.Count > 0 ? "Continue >" : "Close";
    }

    private void OnContinueClicked()
    {
        if (isTyping && allowSkipTyping)
        {
            skipRequested = true;
        }
        else if (!isTyping)
        {
            DisplayNextLine();
        }
    }

    public void OnDialoguePanelClicked()
    {
        OnContinueClicked();
    }
}

[System.Serializable]
public class ProdDialogueLine
{
    public string characterName;
    [TextArea(2, 5)]
    public string message;
    public Sprite portrait;

    public ProdDialogueLine() { }

    public ProdDialogueLine(string name, string msg, Sprite img = null)
    {
        characterName = name;
        message = msg;
        portrait = img;
    }
}

public class ProdDialogueSequenceBuilder
{
    private ProdDialogueManager manager;
    private List<ProdDialogueLine> lines = new List<ProdDialogueLine>();
    private UnityAction onComplete;

    public ProdDialogueSequenceBuilder(ProdDialogueManager mgr)
    {
        manager = mgr;
    }

    public ProdDialogueSequenceBuilder AddLine(string characterName, string message, Sprite portrait = null)
    {
        lines.Add(new ProdDialogueLine(characterName, message, portrait));
        return this;
    }

    public ProdDialogueSequenceBuilder AddProfessorLine(string message)
    {
        lines.Add(new ProdDialogueLine("Professor Lingap", message));
        return this;
    }

    public ProdDialogueSequenceBuilder OnComplete(UnityAction callback)
    {
        onComplete = callback;
        return this;
    }

    public void Play()
    {
        manager.ShowDialogueSequence(lines, onComplete);
    }
}
