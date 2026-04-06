using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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
            // If existing instance has no UI refs but we do, replace it
            if (Instance.dialogueText == null && this.dialogueText != null)
            {
                Destroy(Instance.gameObject);
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // Transfer our references to existing instance if it needs them
                if (Instance.dialogueText == null && this.dialogueText != null)
                {
                    Instance.SetUIReferences(dialoguePanel, dialogueText, characterNameText, characterPortrait, continueButton);
                }
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When a new scene loads, try to find UI references if we lost them
        if (dialogueText == null || dialoguePanel == null)
        {
            RefreshUIReferences();
        }
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

    // Expose the underlying dialogue panel for external systems that
    // need to explicitly hide/show it (e.g., when transitioning to
    // quiz UI right after an intro sequence).
    public GameObject DialoguePanel => dialoguePanel;

    /// <summary>
    /// Finds UI references dynamically. Useful when manager persists across scenes.
    /// </summary>
    public void RefreshUIReferences()
    {
        // Try to find dialogue panel by various names
        if (dialoguePanel == null)
        {
            string[] panelNames = { "DialoguePanel", "Dialogue Panel", "DialogPanel", "DialogueUI" };

            // First, try the simple active-object search.
            foreach (var name in panelNames)
            {
                var panel = GameObject.Find(name);
                if (panel != null)
                {
                    dialoguePanel = panel;
                    break;
                }
            }

            // If still not found, search all loaded GameObjects (including inactive)
            if (dialoguePanel == null)
            {
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (!obj.scene.IsValid() || !obj.scene.isLoaded)
                        continue;

                    foreach (var name in panelNames)
                    {
                        if (obj.name == name)
                        {
                            dialoguePanel = obj;
                            break;
                        }
                    }

                    if (dialoguePanel != null)
                        break;
                }
            }
        }

        if (dialoguePanel == null)
        {
            Debug.LogWarning("ProdDialogueManager: Could not find DialoguePanel in scene.");
            return;
        }

        // Find TMP_Text components - search by name first, then by hierarchy position
        var allTexts = dialoguePanel.GetComponentsInChildren<TMP_Text>(true);
        
        if (dialogueText == null)
        {
            dialogueText = FindComponentByNames<TMP_Text>(dialoguePanel.transform, "DialogueText", "Dialogue Text", "DialogText", "Message", "Text");
            // Fallback: find the largest text component (usually the dialogue area)
            if (dialogueText == null && allTexts.Length > 0)
            {
                TMP_Text largest = null;
                float maxSize = 0;
                foreach (var txt in allTexts)
                {
                    var rect = txt.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        float size = rect.rect.width * rect.rect.height;
                        if (size > maxSize)
                        {
                            maxSize = size;
                            largest = txt;
                        }
                    }
                }
                dialogueText = largest;
            }
        }
        
        if (characterNameText == null)
        {
            characterNameText = FindComponentByNames<TMP_Text>(dialoguePanel.transform, "CharacterName", "Character Name", "Name", "SpeakerName");
        }
        
        if (characterPortrait == null)
        {
            characterPortrait = FindComponentByNames<Image>(dialoguePanel.transform, "CharacterPortrait", "Portrait", "Character Portrait", "Avatar");
        }
        
        if (continueButton == null)
        {
            continueButton = FindComponentByNames<Button>(dialoguePanel.transform, "ContinueButton", "Continue Button", "NextButton", "Continue");
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
                continueButton.onClick.AddListener(OnContinueClicked);
                continueButtonText = continueButton.GetComponentInChildren<TMP_Text>();
            }
        }

        Debug.Log($"ProdDialogueManager: RefreshUIReferences - Panel: {dialoguePanel != null}, Text: {dialogueText != null}, Name: {characterNameText != null}");
    }

    private T FindComponentByNames<T>(Transform parent, params string[] names) where T : Component
    {
        foreach (var name in names)
        {
            var found = parent.Find(name);
            if (found != null)
            {
                var component = found.GetComponent<T>();
                if (component != null)
                    return component;
            }
        }
        
        // Also search recursively with contains match
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            foreach (var name in names)
            {
                if (child.name.Contains(name) || name.Contains(child.name))
                {
                    var component = child.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Manually assign UI references (call from scene setup if needed)
    /// </summary>
    public void SetUIReferences(GameObject panel, TMP_Text dialogueTxt, TMP_Text nameTxt, Image portrait, Button continueBtn)
    {
        dialoguePanel = panel;
        dialogueText = dialogueTxt;
        characterNameText = nameTxt;
        characterPortrait = portrait;
        continueButton = continueBtn;
        
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
            continueButtonText = continueButton.GetComponentInChildren<TMP_Text>();
        }
    }

    private void ShowDialoguePanel()
    {
        // Auto-refresh UI references if null (handles DontDestroyOnLoad across scenes)
        if (dialoguePanel == null || dialogueText == null)
        {
            RefreshUIReferences();
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        else
            Debug.LogWarning("ProdDialogueManager: DialoguePanel not found! Create a GameObject named 'DialoguePanel' or assign manually.");
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
        
        // Try to find dialogueText if null
        if (dialogueText == null)
        {
            RefreshUIReferences();
            
            if (dialogueText == null)
            {
                Debug.LogError("ProdDialogueManager: dialogueText not found! Ensure DialoguePanel has a child named 'DialogueText' with TMP_Text component.");
                isTyping = false;
                yield break;
            }
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

/// <summary>
    /// Converts DialogueLineData list to ProdDialogueLine list with optional placeholder substitution.
    /// </summary>
    private List<ProdDialogueLine> ConvertAndApplyPlaceholders(
        System.Collections.Generic.IList<DialogueLineData> lines,
        System.Collections.Generic.Dictionary<string, string> placeholders = null)
    {
        var converted = new List<ProdDialogueLine>();
        if (lines == null || lines.Count == 0)
            return converted;

        foreach (var lineData in lines)
        {
            if (lineData == null)
                continue;

            // Determine character display name
            string displayName = lineData.characterName;
            if (string.IsNullOrEmpty(displayName))
            {
                if (!string.IsNullOrEmpty(lineData.characterId) &&
                    characterLookup.TryGetValue(lineData.characterId, out CharacterPreset preset))
                {
                    displayName = preset.displayName;
                }
                else
                {
                    displayName = !string.IsNullOrEmpty(lineData.characterId)
                        ? lineData.characterId
                        : "Narrator";
                }
            }

            // Determine portrait sprite
            Sprite portraitSprite = lineData.portraitOverride;
            if (portraitSprite == null && !string.IsNullOrEmpty(lineData.characterId))
            {
                if (characterLookup.TryGetValue(lineData.characterId, out CharacterPreset preset))
                {
                    portraitSprite = preset.portrait;
                }
            }

            // Apply placeholder substitutions to message
            string message = lineData.message ?? string.Empty;
            if (placeholders != null)
            {
                foreach (var kvp in placeholders)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                        message = message.Replace(kvp.Key, kvp.Value ?? string.Empty);
                }
            }

            converted.Add(new ProdDialogueLine
            {
                characterName = displayName,
                message = message,
                portrait = portraitSprite
            });
        }

        return converted;
    }

    /// <summary>
    /// Shows a sequence of DialogueLineData lines without placeholder substitution.
    /// </summary>
    public void ShowDialogueSequence(
        System.Collections.Generic.List<DialogueLineData> lines,
        UnityAction onComplete = null)
    {
        var converted = ConvertAndApplyPlaceholders(lines, null);
        if (converted == null || converted.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        ShowDialogueSequence(converted, onComplete);
    }

    /// <summary>
    /// Shows a sequence of DialogueLineData lines with optional placeholder substitution.
    /// </summary>
    public void ShowDialogueSequence(
        System.Collections.Generic.List<DialogueLineData> lines,
        UnityAction onComplete,
        System.Collections.Generic.Dictionary<string, string> placeholders)
    {
        var converted = ConvertAndApplyPlaceholders(lines, placeholders);
        if (converted == null || converted.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        ShowDialogueSequence(converted, onComplete);
    }

    /// <summary>
    /// Retrieves the portrait sprite for a character by ID and optional expression.
    /// Currently ignores expressionId and returns the base preset portrait.
    /// </summary>
    public Sprite GetPortraitForCharacter(string characterId, string expressionId = null)
    {
        if (!string.IsNullOrEmpty(characterId) &&
            characterLookup.TryGetValue(characterId, out CharacterPreset preset))
        {
            return preset.portrait;
        }

        return null;
    }

    /// <summary>
    /// Retrieves expression sprites (idle, blink, talking) for a character.
    /// For now, uses the base portrait as idle and leaves blink/talking null.
    /// </summary>
    public void GetExpressionSpritesForCharacter(
        string characterId,
        string expressionId,
        out Sprite idle,
        out Sprite blink,
        out Sprite[] talkingFrames)
    {
        idle = null;
        blink = null;
        talkingFrames = null;

        if (!string.IsNullOrEmpty(characterId) &&
            characterLookup.TryGetValue(characterId, out CharacterPreset preset))
        {
            idle = preset.portrait;
        }
    }
}
