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
    [SerializeField] private Image characterPortrait; // Single-portrait fallback
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonText;

    [Header("Ace Attorney-style Layout")]
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private Image centerPortrait;
    [SerializeField] private Image dialogueBackgroundImage;

    [Header("Portrait Visual Settings")]
    [SerializeField] private float activePortraitScale = 1.0f;
    [SerializeField] private float inactivePortraitScale = 0.9f;
    [SerializeField] [Range(0f, 1f)] private float inactivePortraitAlpha = 0.5f;
    [SerializeField] private float talkingScaleAmplitude = 0.05f;
    [SerializeField] private float talkingScaleSpeed = 6f;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private bool allowSkipTyping = true;

    private Queue<ProdDialogueLine> dialogueQueue = new Queue<ProdDialogueLine>();
    private bool isTyping = false;
    private bool skipRequested = false;
    private Coroutine typingCoroutine;
    private UnityAction onDialogueComplete;

    // Runtime state for portraits/backgrounds
    private Image activePortraitImage;
    private Image inactivePortraitImage;
    private Coroutine talkingAnimationCoroutine;

    [System.Serializable]
    public class CharacterPreset
    {
        public string characterId;
        public string displayName;
        public Sprite portrait;
        public DialogueSpeakerSide defaultSide = DialogueSpeakerSide.Left;

        [System.Serializable]
        public class CharacterExpression
        {
            public string expressionId;
            public Sprite sprite; // idle/base sprite for this expression

            [Header("Optional facial animation")]
            public Sprite blinkSprite; // frame used when blinking
            public List<Sprite> talkingSprites = new List<Sprite>(); // frames cycled while talking
        }

        public List<CharacterExpression> expressions = new List<CharacterExpression>();

        public Sprite GetPortrait(string expressionId)
        {
            if (string.IsNullOrEmpty(expressionId))
                return portrait;

            if (expressions != null)
            {
                for (int i = 0; i < expressions.Count; i++)
                {
                    var expr = expressions[i];
                    if (expr != null && !string.IsNullOrEmpty(expr.expressionId) && expr.sprite != null &&
                        string.Equals(expr.expressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return expr.sprite;
                    }
                }
            }

            return portrait;
        }

        /// <summary>
        /// Returns the sprites used for facial animation for a given expression: idle/base, blink frame,
        /// and talking frames. Falls back to the preset's default portrait when specific frames are missing.
        /// </summary>
        public void GetExpressionSprites(string expressionId, out Sprite idleSprite, out Sprite blinkSprite, out Sprite[] talkingFrames)
        {
            idleSprite = portrait;
            blinkSprite = null;
            talkingFrames = null;

            if (expressions == null || expressions.Count == 0)
                return;

            CharacterExpression match = null;

            if (!string.IsNullOrEmpty(expressionId))
            {
                for (int i = 0; i < expressions.Count; i++)
                {
                    var expr = expressions[i];
                    if (expr != null && !string.IsNullOrEmpty(expr.expressionId) &&
                        string.Equals(expr.expressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        match = expr;
                        break;
                    }
                }
            }

            if (match == null)
                return;

            if (match.sprite != null)
                idleSprite = match.sprite;

            if (match.blinkSprite != null)
                blinkSprite = match.blinkSprite;

            if (match.talkingSprites != null && match.talkingSprites.Count > 0)
                talkingFrames = match.talkingSprites.ToArray();
        }
    }

    [Header("Character Presets")]
    [SerializeField] private List<CharacterPreset> characterPresets = new List<CharacterPreset>();
    private Dictionary<string, CharacterPreset> characterLookup = new Dictionary<string, CharacterPreset>();

    /// <summary>
    /// Returns a portrait sprite for the given character and expression, using the
    /// configured CharacterPresets. If no matching preset or sprite is found,
    /// null is returned.
    /// </summary>
    public Sprite GetPortraitForCharacter(string characterId, string expressionId)
    {
        if (string.IsNullOrEmpty(characterId))
            return null;

        if (!characterLookup.TryGetValue(characterId, out CharacterPreset preset) || preset == null)
            return null;

        return preset.GetPortrait(expressionId);
    }

    /// <summary>
    /// Returns expression sprites (idle, blink, talking frames) for the given character
    /// and expression id, using the configured CharacterPresets. This mirrors the logic
    /// used for dialogue portraits so other UIs (like quiz panels) can reuse it.
    /// </summary>
    public void GetExpressionSpritesForCharacter(string characterId, string expressionId,
        out Sprite idleSprite, out Sprite blinkSprite, out Sprite[] talkingFrames)
    {
        idleSprite = null;
        blinkSprite = null;
        talkingFrames = null;

        if (string.IsNullOrEmpty(characterId))
            return;

        if (!characterLookup.TryGetValue(characterId, out CharacterPreset preset) || preset == null)
            return;

        // Start with the preset's base portrait as idle.
        idleSprite = preset.portrait;

        Sprite exprIdle;
        Sprite exprBlink;
        Sprite[] exprTalking;

        preset.GetExpressionSprites(expressionId, out exprIdle, out exprBlink, out exprTalking);

        if (exprIdle != null)
            idleSprite = exprIdle;

        blinkSprite = exprBlink;
        talkingFrames = exprTalking;

        // Fallback: if there are no talking frames for the requested expression,
        // use the first expression on this preset that has talking sprites.
        if ((talkingFrames == null || talkingFrames.Length == 0) && preset.expressions != null)
        {
            for (int i = 0; i < preset.expressions.Count; i++)
            {
                var expr = preset.expressions[i];
                if (expr == null || expr.talkingSprites == null || expr.talkingSprites.Count == 0)
                    continue;

                if (idleSprite == null)
                {
                    idleSprite = expr.sprite != null ? expr.sprite : preset.portrait;
                }

                if (blinkSprite == null)
                {
                    blinkSprite = expr.blinkSprite;
                }

                talkingFrames = expr.talkingSprites.ToArray();
                break;
            }
        }
    }

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

    /// <summary>
    /// Convenience overload that plays a dialogue sequence defined as DialogueLineData entries
    /// (typically authored inside mission or onboarding assets). This converts each data line
    /// into a ProdDialogueLine and then uses the existing queue-based flow.
    /// </summary>
    public void ShowDialogueSequence(System.Collections.Generic.IList<DialogueLineData> dataLines, UnityAction onComplete = null)
    {
        ShowDialogueSequence(dataLines, onComplete, null);
    }

    /// <summary>
    /// Plays a dialogue sequence defined as DialogueLineData entries, with optional placeholder
    /// replacement in the message text (e.g., {name}, {missionName}). Placeholders are simple
    /// string replacements applied before building ProdDialogueLine instances.
    /// </summary>
    public void ShowDialogueSequence(System.Collections.Generic.IList<DialogueLineData> dataLines, UnityAction onComplete, System.Collections.Generic.IDictionary<string, string> placeholders)
    {
        if (dataLines == null || dataLines.Count == 0)
        {
            onDialogueComplete = null;
            onComplete?.Invoke();
            return;
        }

        var built = new List<ProdDialogueLine>(dataLines.Count);
        for (int i = 0; i < dataLines.Count; i++)
        {
            var data = dataLines[i];
            if (data == null)
                continue;

            string finalMessage = data.message;
            if (!string.IsNullOrEmpty(finalMessage) && placeholders != null)
            {
                foreach (var kvp in placeholders)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                        continue;

                    var replacement = kvp.Value ?? string.Empty;
                    finalMessage = finalMessage.Replace(kvp.Key, replacement);
                }
            }

            var line = new ProdDialogueLine
            {
                characterId = data.characterId,
                characterName = data.characterName,
                message = finalMessage,
                expressionId = data.expressionId,
                side = data.side,
                portrait = data.portraitOverride,
                backgroundSprite = data.backgroundSprite,
                clearBackground = data.clearBackground
            };

            built.Add(line);
        }

        if (built.Count == 0)
        {
            onDialogueComplete = null;
            onComplete?.Invoke();
            return;
        }

        ShowDialogueSequence(built, onComplete);
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

        // When a dialogue sequence finishes, clear any leftover background
        // so the next dialogue starts from a clean state.
        if (dialogueBackgroundImage != null)
        {
            dialogueBackgroundImage.sprite = null;
            dialogueBackgroundImage.gameObject.SetActive(false);
        }
    }

    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

    /// <summary>
    /// Finds UI references dynamically. Useful when manager persists across scenes.
    /// </summary>
    public void RefreshUIReferences()
    {
        // Try to find dialogue panel by various names
        if (dialoguePanel == null)
        {
            string[] panelNames = { "DialoguePanel", "Dialogue Panel", "DialogPanel", "DialogueUI" };
            foreach (var name in panelNames)
            {
                var panel = GameObject.Find(name);
                if (panel != null)
                {
                    dialoguePanel = panel;
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
        
        if (leftPortrait == null)
        {
            leftPortrait = FindComponentByNames<Image>(dialoguePanel.transform, "LeftPortrait", "Left Portrait", "SpeakerLeft", "LeftCharacter", "Left Avatar");
        }

        if (rightPortrait == null)
        {
            rightPortrait = FindComponentByNames<Image>(dialoguePanel.transform, "RightPortrait", "Right Portrait", "SpeakerRight", "RightCharacter", "Right Avatar");
        }

        if (centerPortrait == null)
        {
            centerPortrait = FindComponentByNames<Image>(dialoguePanel.transform, "CenterPortrait", "Center Portrait", "SpeakerCenter", "CenterCharacter", "Center Avatar");
        }

        if (characterPortrait == null)
        {
            characterPortrait = FindComponentByNames<Image>(dialoguePanel.transform, "CharacterPortrait", "Portrait", "Character Portrait", "Avatar");
        }
        
        if (dialogueBackgroundImage == null)
        {
            dialogueBackgroundImage = FindComponentByNames<Image>(dialoguePanel.transform, "DialogueBackground", "Dialogue Background", "Background", "BG", "Backdrop");
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

    private CharacterPreset GetPresetForLine(ProdDialogueLine line)
    {
        if (line == null)
            return null;

        // Prefer explicit characterId on the line
        if (!string.IsNullOrEmpty(line.characterId))
        {
            if (characterLookup.TryGetValue(line.characterId, out var byId))
                return byId;
        }

        // Try matching by display name
        if (!string.IsNullOrEmpty(line.characterName))
        {
            for (int i = 0; i < characterPresets.Count; i++)
            {
                var preset = characterPresets[i];
                if (preset != null && !string.IsNullOrEmpty(preset.displayName) &&
                    string.Equals(preset.displayName, line.characterName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return preset;
                }
            }

            // Fallback: treat name as an ID
            if (characterLookup.TryGetValue(line.characterName, out var byNameAsId))
                return byNameAsId;
        }

        return null;
    }

    private void EnsureAutoExpressionForLine(ProdDialogueLine line, CharacterPreset preset)
    {
        if (line == null || preset == null)
            return;

        // Only auto-adjust when no expression was explicitly set
        if (!string.IsNullOrEmpty(line.expressionId))
            return;

        // Currently only auto-drive expressions for Professor Lingap (or whatever id you use)
        if (string.IsNullOrEmpty(preset.characterId))
            return;

        // Match your configured professor_lingap id
        if (!string.Equals(preset.characterId, "professor_lingap", System.StringComparison.OrdinalIgnoreCase))
            return;

        line.expressionId = GetAutoExpressionIdFromMessage(line.message);
    }

    private string GetAutoExpressionIdFromMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return "explaining";

        string lower = message.ToLowerInvariant();

        // Positive / praise -> approved (thumbs up)
        if (ContainsAny(lower, "great", "excellent", "nice", "correct", "well done", "good job", "awesome"))
            return "approved";

        // Warnings / concern -> sad
        if (ContainsAny(lower, "oh no", "unfortunately", "careful", "danger", "warning", "risk"))
            return "sad";

        // Default teaching/explaining tone
        return "explaining";
    }

    private bool ContainsAny(string text, params string[] keywords)
    {
        if (string.IsNullOrEmpty(text) || keywords == null)
            return false;

        for (int i = 0; i < keywords.Length; i++)
        {
            var k = keywords[i];
            if (!string.IsNullOrEmpty(k) && text.Contains(k.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private void ApplyBackground(ProdDialogueLine line)
    {
        if (dialogueBackgroundImage == null || line == null)
            return;

        // Optionally clear any existing background before applying a new one.
        if (line.clearBackground)
        {
            dialogueBackgroundImage.sprite = null;
            dialogueBackgroundImage.gameObject.SetActive(false);
        }

        Sprite newSprite = null;

        if (line.backgroundSprite != null)
        {
            newSprite = line.backgroundSprite;
        }

        // If you later add a lookup table for backgroundId, resolve it here

        if (newSprite == null)
            return; // Keep current background

        if (dialogueBackgroundImage.sprite == newSprite)
            return;

        dialogueBackgroundImage.sprite = newSprite;
        dialogueBackgroundImage.gameObject.SetActive(true);
    }

    private void ConfigurePortraitsForLine(ProdDialogueLine line)
    {
        if (line == null)
            return;

        var preset = GetPresetForLine(line);
        Sprite portraitSprite = line.portrait;

        if (portraitSprite == null && preset != null)
        {
            // For center side, default to a dedicated 'center' expression when none is set.
            if (line.side == DialogueSpeakerSide.Center && string.IsNullOrEmpty(line.expressionId))
            {
                line.expressionId = "center";
            }
            else
            {
                // If no expression was specified, choose one automatically based on message tone
                EnsureAutoExpressionForLine(line, preset);
            }

            portraitSprite = preset.GetPortrait(line.expressionId);
        }

        // Multi-portrait layout if left/right/center slots are wired
        if (leftPortrait != null || rightPortrait != null || centerPortrait != null)
        {
            DialogueSpeakerSide side = line.side;
            if (side == DialogueSpeakerSide.Auto && preset != null)
            {
                side = preset.defaultSide;
            }
            if (side == DialogueSpeakerSide.Auto)
            {
                side = DialogueSpeakerSide.Left;
            }

            // Fallback if requested side has no portrait image assigned
            if (side == DialogueSpeakerSide.Center && centerPortrait == null)
            {
                side = DialogueSpeakerSide.Left;
            }

            Image secondInactive = null;

            if (side == DialogueSpeakerSide.Right)
            {
                activePortraitImage = rightPortrait;
                inactivePortraitImage = leftPortrait;
                secondInactive = centerPortrait;
            }
            else if (side == DialogueSpeakerSide.Center)
            {
                activePortraitImage = centerPortrait;
                inactivePortraitImage = leftPortrait;
                secondInactive = rightPortrait;
            }
            else
            {
                // Default to left
                activePortraitImage = leftPortrait;
                inactivePortraitImage = rightPortrait;
                secondInactive = centerPortrait;
            }

            if (activePortraitImage != null)
            {
                if (portraitSprite != null)
                {
                    activePortraitImage.sprite = portraitSprite;
                    activePortraitImage.gameObject.SetActive(true);
                }
                else
                {
                    activePortraitImage.gameObject.SetActive(false);
                }
            }

            // Keep the other portrait visible but de-emphasized if it already has a sprite
            if (inactivePortraitImage != null && inactivePortraitImage.sprite != null)
            {
                inactivePortraitImage.gameObject.SetActive(true);
            }

            // Ensure any third portrait is hidden so only the active (and possibly one inactive) is shown
            if (secondInactive != null && secondInactive != activePortraitImage)
            {
                secondInactive.gameObject.SetActive(false);
            }

            UpdatePortraitHighlighting();
        }
        else
        {
            // Fallback: legacy single-portrait layout
            activePortraitImage = characterPortrait;
            inactivePortraitImage = null;

            if (characterPortrait != null)
            {
                if (portraitSprite != null)
                {
                    characterPortrait.sprite = portraitSprite;
                    characterPortrait.gameObject.SetActive(true);
                }
                else
                {
                    characterPortrait.gameObject.SetActive(false);
                }
            }
        }

        // Configure optional face animator on the active portrait (for blinking/talking mouth)
        ConfigureFaceAnimatorForLine(line, preset, portraitSprite);
    }

    private void ConfigureFaceAnimatorForLine(ProdDialogueLine line, CharacterPreset preset, Sprite portraitSprite)
    {
        if (activePortraitImage == null)
            return;

        var faceAnimator = activePortraitImage.GetComponent<PortraitFaceAnimator>();
        if (faceAnimator == null)
        {
            // Automatically attach a PortraitFaceAnimator so any portrait slot
            // (left/right/center) can blink and move its mouth without extra setup.
            faceAnimator = activePortraitImage.gameObject.AddComponent<PortraitFaceAnimator>();
        }

        if (faceAnimator == null)
            return;

        Sprite idle = portraitSprite;
        Sprite blink = null;
        Sprite[] talkingFrames = null;

        if (preset != null)
        {
            Sprite exprIdle;
            Sprite exprBlink;
            Sprite[] exprTalking;

            // For center side, default to using the dedicated 'center' expression
            // when no explicit expressionId was provided on the line.
            string exprId = line != null ? line.expressionId : null;
            if (line != null && line.side == DialogueSpeakerSide.Center && string.IsNullOrEmpty(exprId))
            {
                exprId = "center";
            }

            preset.GetExpressionSprites(exprId, out exprIdle, out exprBlink, out exprTalking);

            if (idle == null)
                idle = exprIdle;

            blink = exprBlink;
            talkingFrames = exprTalking;

            // Fallback: if no talking frames were found for the requested expression,
            // use the first expression on this preset that has talking sprites.
            if ((talkingFrames == null || talkingFrames.Length == 0) && preset.expressions != null)
            {
                for (int i = 0; i < preset.expressions.Count; i++)
                {
                    var expr = preset.expressions[i];
                    if (expr == null || expr.talkingSprites == null || expr.talkingSprites.Count == 0)
                        continue;

                    if (idle == null)
                    {
                        idle = expr.sprite != null ? expr.sprite : preset.portrait;
                    }

                    if (blink == null)
                    {
                        blink = expr.blinkSprite;
                    }

                    talkingFrames = expr.talkingSprites.ToArray();
                    break;
                }
            }
        }

        faceAnimator.SetExpressionSprites(idle, blink, talkingFrames);
    }

    private void UpdatePortraitHighlighting()
    {
        if (activePortraitImage != null)
        {
            var c = activePortraitImage.color;
            activePortraitImage.color = new Color(c.r, c.g, c.b, 1f);
            if (activePortraitImage.rectTransform != null)
            {
                activePortraitImage.rectTransform.localScale = Vector3.one * activePortraitScale;
            }
        }

        if (inactivePortraitImage != null)
        {
            // Hide the non-speaking portrait completely so only the active side is visible
            inactivePortraitImage.gameObject.SetActive(false);
        }
    }

    private void StartPortraitTalkingAnimation()
    {
        var faceAnimator = GetActiveFaceAnimator();
        if (faceAnimator != null)
        {
            faceAnimator.SetTalking(true);
        }
    }

    private void StopPortraitTalkingAnimation()
    {
        var faceAnimator = GetActiveFaceAnimator();
        if (faceAnimator != null)
        {
            faceAnimator.SetTalking(false);
        }
    }

    private PortraitFaceAnimator GetActiveFaceAnimator()
    {
        if (activePortraitImage == null)
            return null;

        return activePortraitImage.GetComponent<PortraitFaceAnimator>();
    }

    private IEnumerator PortraitTalkingCoroutine(RectTransform target)
    {
        if (target == null)
            yield break;

        var baseScale = Vector3.one * activePortraitScale;
        float t = 0f;

        while (isTyping && target != null)
        {
            float offset = Mathf.Sin(t * talkingScaleSpeed) * talkingScaleAmplitude;
            target.localScale = baseScale * (1f + offset);
            t += Time.deltaTime;
            yield return null;
        }

        if (target != null)
            target.localScale = baseScale;
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

        // Resolve display name: prefer preset's Display Name when available
        string nameToShow = line.characterName;
        var presetForName = GetPresetForLine(line);
        if (presetForName != null && !string.IsNullOrEmpty(presetForName.displayName))
        {
            nameToShow = presetForName.displayName;
        }

        if (characterNameText != null)
            characterNameText.text = nameToShow;

        ApplyBackground(line);
        ConfigurePortraitsForLine(line);

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

        StartPortraitTalkingAnimation();

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

        StopPortraitTalkingAnimation();
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

    // Optional advanced presentation fields
    public string characterId;
    public string expressionId;
    public DialogueSpeakerSide side = DialogueSpeakerSide.Auto;
    public Sprite backgroundSprite;
    public string backgroundId;
    public bool clearBackground;

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

    // NOTE: Keep this in sync with the characterId configured for Professor Lingap
    private const string ProfessorCharacterId = "professor_lingap";

    public ProdDialogueSequenceBuilder(ProdDialogueManager mgr)
    {
        manager = mgr;
    }

    public ProdDialogueSequenceBuilder AddLine(string characterName, string message, Sprite portrait = null)
    {
        lines.Add(new ProdDialogueLine(characterName, message, portrait));
        return this;
    }

    public ProdDialogueSequenceBuilder AddProfessorLine(string message, string expressionId = null, Sprite backgroundSprite = null, DialogueSpeakerSide side = DialogueSpeakerSide.Auto, bool clearBackground = false)
    {
        var line = new ProdDialogueLine("Professor Lingap", message)
        {
            characterId = ProfessorCharacterId,
            expressionId = expressionId,
            side = side,
            backgroundSprite = backgroundSprite,
            clearBackground = clearBackground
        };

        lines.Add(line);
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

public enum DialogueSpeakerSide
{
    Auto = 0,
    Left = 1,
    Right = 2,
    Center = 3
}
