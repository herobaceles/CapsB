using UnityEngine;

/// <summary>
/// Data for a single dialogue line that can be authored in assets
/// (missions, onboarding sequences, etc.). This is converted at
/// runtime into ProdDialogueLine for the ProdDialogueManager.
/// </summary>
[System.Serializable]
public class DialogueLineData
{
    [Header("Speaker")]
    [Tooltip("Optional character ID to look up in ProdDialogueManager character presets (e.g., 'professor_lingap').")]
    public string characterId;

    [Tooltip("Optional display name override. If empty, preset Display Name is used.")]
    public string characterName;

    [Tooltip("Side of the screen where this speaker should appear. Auto uses preset default.")]
    public DialogueSpeakerSide side = DialogueSpeakerSide.Auto;

    [Header("Visuals")]
    [Tooltip("Expression/pose ID used to pick a sprite from the character preset (e.g., 'explaining', 'sad', 'approved').")]
    public string expressionId;

    [Tooltip("Optional explicit portrait sprite override. If set, this is used instead of preset expressions.")]
    public Sprite portraitOverride;

    [Tooltip("Optional background image for this line. If null, the previous background is kept.")]
    public Sprite backgroundSprite;

    [Tooltip("If true, clears the dialogue background before displaying this line (use to hide a banner set by a previous line).")]
    public bool clearBackground;

    [Header("Text")]
    [TextArea(2, 5)]
    public string message;
}
