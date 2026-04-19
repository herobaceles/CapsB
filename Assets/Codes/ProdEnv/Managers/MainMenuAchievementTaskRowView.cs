using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuAchievementTaskRowView : MonoBehaviour
{
    private const float CardWidth = 250f;
    private const float CardHeight = 260f;
    private const float IconSize = 104f;

    private static readonly Color CompletedTextColor = new Color(0.97f, 0.94f, 0.88f, 1f);
    private static readonly Color IncompleteTextColor = new Color(0.91f, 0.86f, 0.78f, 1f);
    private static readonly Color CompletedIconColor = new Color(0.99f, 0.96f, 0.9f, 1f);
    private static readonly Color IncompleteIconColor = new Color(0.86f, 0.8f, 0.72f, 0.92f);
    private static readonly Color CompletedCardColor = new Color(0.27f, 0.24f, 0.18f, 0.94f);
    private static readonly Color IncompleteCardColor = new Color(0.31f, 0.28f, 0.22f, 0.92f);
    private static readonly Color CompletedBorderColor = new Color(0.73f, 0.66f, 0.46f, 0.62f);
    private static readonly Color IncompleteBorderColor = new Color(0.62f, 0.54f, 0.39f, 0.42f);
    private static readonly Color CompletedStatusBadgeColor = new Color(0.4f, 0.48f, 0.26f, 0.96f);
    private static readonly Color IncompleteStatusBadgeColor = new Color(0.46f, 0.38f, 0.25f, 0.94f);
    private static readonly Color CompletedStatusTextColor = new Color(0.94f, 0.96f, 0.82f, 1f);
    private static readonly Color IncompleteStatusTextColor = new Color(0.95f, 0.88f, 0.74f, 0.94f);
    private static readonly Color TextOutlineColor = new Color(0.09f, 0.06f, 0.03f, 0.95f);

    [SerializeField] private Image taskIconImage;
    [SerializeField] private TMP_Text taskNameText;
    [SerializeField] private TMP_Text taskStatusText;
    [SerializeField] private Image backgroundImage;

    private Image statusBadgeImage;

    public void Bind(TaskData task, bool isCompleted, Sprite missionFallbackIcon, Sprite genericFallbackIcon)
    {
        ConfigureLayout();

        Sprite icon = task != null && task.taskIcon != null
            ? task.taskIcon
            : (missionFallbackIcon != null ? missionFallbackIcon : genericFallbackIcon);

        if (taskIconImage != null)
        {
            taskIconImage.sprite = icon;
            taskIconImage.color = isCompleted ? CompletedIconColor : IncompleteIconColor;
            taskIconImage.preserveAspect = true;
        }

        if (taskNameText != null)
        {
            taskNameText.text = task != null ? task.taskName : "Unknown Task";
            taskNameText.color = isCompleted ? CompletedTextColor : IncompleteTextColor;
            taskNameText.enableWordWrapping = true;
            taskNameText.overflowMode = TextOverflowModes.Ellipsis;
            taskNameText.alignment = TextAlignmentOptions.Center;
        }

        if (taskStatusText != null)
        {
            taskStatusText.text = isCompleted ? "COMPLETED" : "INCOMPLETE";
            taskStatusText.color = isCompleted ? CompletedStatusTextColor : IncompleteStatusTextColor;
            taskStatusText.enableWordWrapping = false;
            taskStatusText.alignment = TextAlignmentOptions.Center;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isCompleted ? CompletedCardColor : IncompleteCardColor;
        }

        if (statusBadgeImage != null)
        {
            statusBadgeImage.color = isCompleted ? CompletedStatusBadgeColor : IncompleteStatusBadgeColor;
        }

        ConfigureCardDecor(isCompleted);
        ApplyReadableTextStyle(taskNameText, 0.2f);
        ApplyReadableTextStyle(taskStatusText, 0.18f);
    }

    private void ConfigureLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(CardWidth, CardHeight);
            root.localScale = Vector3.one;
        }

        LayoutElement rowLayout = GetComponent<LayoutElement>();
        if (rowLayout == null)
            rowLayout = gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredWidth = CardWidth;
        rowLayout.preferredHeight = CardHeight;
        rowLayout.minWidth = CardWidth;
        rowLayout.minHeight = CardHeight;

        if (backgroundImage != null && backgroundImage.transform != transform)
        {
            RectTransform backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundRect.SetAsFirstSibling();

            LayoutElement backgroundLayout = backgroundImage.GetComponent<LayoutElement>();
            if (backgroundLayout == null)
                backgroundLayout = backgroundImage.gameObject.AddComponent<LayoutElement>();
            backgroundLayout.ignoreLayout = true;

            Outline backgroundOutline = backgroundImage.GetComponent<Outline>();
            if (backgroundOutline == null)
                backgroundOutline = backgroundImage.gameObject.AddComponent<Outline>();
            backgroundOutline.effectDistance = new Vector2(2f, -2f);

            Shadow backgroundShadow = backgroundImage.GetComponent<Shadow>();
            if (backgroundShadow == null)
                backgroundShadow = backgroundImage.gameObject.AddComponent<Shadow>();
            backgroundShadow.effectDistance = new Vector2(0f, -6f);
            backgroundShadow.effectColor = new Color(0.09f, 0.06f, 0.03f, 0.32f);
        }

        if (taskIconImage != null)
        {
            RectTransform iconRect = taskIconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            iconRect.anchoredPosition = new Vector2(0f, -62f);

            LayoutElement iconLayout = taskIconImage.GetComponent<LayoutElement>();
            if (iconLayout == null)
                iconLayout = taskIconImage.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = IconSize;
            iconLayout.preferredHeight = IconSize;
            iconLayout.minWidth = IconSize;
            iconLayout.minHeight = IconSize;
            iconLayout.ignoreLayout = true;

            Outline iconOutline = taskIconImage.GetComponent<Outline>();
            if (iconOutline == null)
                iconOutline = taskIconImage.gameObject.AddComponent<Outline>();
            iconOutline.effectColor = new Color(0.17f, 0.12f, 0.07f, 0.45f);
            iconOutline.effectDistance = new Vector2(1f, -1f);
        }

        if (taskNameText != null)
        {
            RectTransform nameRect = taskNameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.sizeDelta = new Vector2(214f, 72f);
            nameRect.anchoredPosition = new Vector2(0f, -24f);

            LayoutElement nameLayout = taskNameText.GetComponent<LayoutElement>();
            if (nameLayout == null)
                nameLayout = taskNameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.ignoreLayout = true;

            taskNameText.fontSize = 24f;
            taskNameText.fontSizeMin = 18f;
            taskNameText.fontSizeMax = 24f;
            taskNameText.enableAutoSizing = true;
            taskNameText.fontStyle = FontStyles.Bold;
            taskNameText.lineSpacing = -2f;
            taskNameText.margin = new Vector4(14f, 8f, 14f, 6f);
            taskNameText.alignment = TextAlignmentOptions.Center;
        }

        EnsureStatusBadge();

        if (taskStatusText != null)
        {
            RectTransform statusRect = taskStatusText.rectTransform;
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(0.5f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.sizeDelta = new Vector2(182f, 32f);
            statusRect.anchoredPosition = new Vector2(0f, 34f);

            LayoutElement statusLayout = taskStatusText.GetComponent<LayoutElement>();
            if (statusLayout == null)
                statusLayout = taskStatusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.ignoreLayout = true;

            taskStatusText.fontSize = 18f;
            taskStatusText.fontStyle = FontStyles.Bold;
            taskStatusText.characterSpacing = 3f;
            taskStatusText.margin = new Vector4(12f, 5f, 12f, 5f);
        }
    }

    private void ConfigureCardDecor(bool isCompleted)
    {
        if (backgroundImage == null)
            return;

        Outline backgroundOutline = backgroundImage.GetComponent<Outline>();
        if (backgroundOutline != null)
            backgroundOutline.effectColor = isCompleted ? CompletedBorderColor : IncompleteBorderColor;
    }

    private void ApplyReadableTextStyle(TMP_Text text, float outlineWidth)
    {
        if (text == null)
            return;

        Material fontMaterial = text.fontMaterial;
        if (fontMaterial == null)
            return;

        if (fontMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
            fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, TextOutlineColor);

        if (fontMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth))
            fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);

        if (fontMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor))
            fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.12f, 0.08f, 0.04f, 0.52f));

        if (fontMaterial.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
            fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);

        if (fontMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
            fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.4f);

        if (fontMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
            fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.4f);
    }

    private void EnsureStatusBadge()
    {
        if (taskStatusText == null)
            return;

        if (statusBadgeImage == null)
        {
            Transform existingBadge = transform.Find("StatusBadge");
            if (existingBadge != null)
                statusBadgeImage = existingBadge.GetComponent<Image>();
        }

        if (statusBadgeImage == null)
        {
            GameObject badgeObject = new GameObject("StatusBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObject.transform.SetParent(transform, false);
            statusBadgeImage = badgeObject.GetComponent<Image>();
            statusBadgeImage.raycastTarget = false;
        }

        RectTransform badgeRect = statusBadgeImage.rectTransform;
        badgeRect.anchorMin = new Vector2(0.5f, 0f);
        badgeRect.anchorMax = new Vector2(0.5f, 0f);
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.sizeDelta = new Vector2(194f, 38f);
        badgeRect.anchoredPosition = new Vector2(0f, 34f);
        badgeRect.SetSiblingIndex(Mathf.Max(0, taskStatusText.transform.GetSiblingIndex()));

        LayoutElement badgeLayout = statusBadgeImage.GetComponent<LayoutElement>();
        if (badgeLayout == null)
            badgeLayout = statusBadgeImage.gameObject.AddComponent<LayoutElement>();
        badgeLayout.ignoreLayout = true;

        Outline badgeOutline = statusBadgeImage.GetComponent<Outline>();
        if (badgeOutline == null)
            badgeOutline = statusBadgeImage.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        badgeOutline.effectDistance = new Vector2(1f, -1f);
    }
}