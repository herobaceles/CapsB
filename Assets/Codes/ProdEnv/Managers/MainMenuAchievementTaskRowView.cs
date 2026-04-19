using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuAchievementTaskRowView : MonoBehaviour
{
    private const float CardWidth = 180f;
    private const float CardHeight = 180f;
    private const float IconSize = 64f;

    private static readonly Color CompletedTextColor = Color.white;
    private static readonly Color IncompleteTextColor = new Color(0.62f, 0.62f, 0.62f, 1f);
    private static readonly Color CompletedIconColor = Color.white;
    private static readonly Color IncompleteIconColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [SerializeField] private Image taskIconImage;
    [SerializeField] private TMP_Text taskNameText;
    [SerializeField] private TMP_Text taskStatusText;
    [SerializeField] private Image backgroundImage;

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
            taskNameText.alignment = TextAlignmentOptions.Left;
        }

        if (taskStatusText != null)
        {
            taskStatusText.text = isCompleted ? "COMPLETED" : "INCOMPLETE";
            taskStatusText.color = isCompleted ? new Color(0.54f, 0.9f, 0.63f, 1f) : IncompleteTextColor;
            taskStatusText.enableWordWrapping = false;
            taskStatusText.alignment = TextAlignmentOptions.Center;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isCompleted
                ? new Color(0.11f, 0.16f, 0.2f, 0.82f)
                : new Color(0.08f, 0.08f, 0.08f, 0.72f);
        }
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
        }

        if (taskIconImage != null)
        {
            RectTransform iconRect = taskIconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            iconRect.anchoredPosition = new Vector2(0f, -32f);

            LayoutElement iconLayout = taskIconImage.GetComponent<LayoutElement>();
            if (iconLayout == null)
                iconLayout = taskIconImage.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = IconSize;
            iconLayout.preferredHeight = IconSize;
            iconLayout.minWidth = IconSize;
            iconLayout.minHeight = IconSize;
            iconLayout.ignoreLayout = true;
        }

        if (taskNameText != null)
        {
            RectTransform nameRect = taskNameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.sizeDelta = new Vector2(140f, 72f);
            nameRect.anchoredPosition = new Vector2(0f, 10f);

            LayoutElement nameLayout = taskNameText.GetComponent<LayoutElement>();
            if (nameLayout == null)
                nameLayout = taskNameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.ignoreLayout = true;

            taskNameText.fontSize = 20f;
            taskNameText.alignment = TextAlignmentOptions.Center;
        }

        if (taskStatusText != null)
        {
            RectTransform statusRect = taskStatusText.rectTransform;
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(0.5f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.sizeDelta = new Vector2(140f, 28f);
            statusRect.anchoredPosition = new Vector2(0f, 18f);

            LayoutElement statusLayout = taskStatusText.GetComponent<LayoutElement>();
            if (statusLayout == null)
                statusLayout = taskStatusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.ignoreLayout = true;

            taskStatusText.fontSize = 16f;
        }
    }
}