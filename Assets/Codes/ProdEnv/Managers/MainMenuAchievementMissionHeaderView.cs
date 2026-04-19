using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuAchievementMissionHeaderView : MonoBehaviour
{
    private const float HeaderHeight = 84f;
    private const float IconSize = 56f;

    [SerializeField] private Image missionIconImage;
    [SerializeField] private TMP_Text missionNameText;

    public void Bind(MissionData mission, Sprite fallbackIcon)
    {
        ConfigureLayout();

        if (missionIconImage != null)
        {
            missionIconImage.sprite = mission != null && mission.missionIcon != null ? mission.missionIcon : fallbackIcon;
            missionIconImage.preserveAspect = true;
        }

        if (missionNameText != null)
        {
            missionNameText.text = mission != null ? mission.missionName : "Unknown Mission";
            missionNameText.enableWordWrapping = false;
            missionNameText.overflowMode = TextOverflowModes.Ellipsis;
            missionNameText.alignment = TextAlignmentOptions.Left;
        }
    }

    private void ConfigureLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, HeaderHeight);
            root.localScale = Vector3.one;
        }

        HorizontalLayoutGroup layoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

        layoutGroup.padding = new RectOffset(20, 20, 12, 12);
        layoutGroup.spacing = 16f;
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        LayoutElement rowLayout = GetComponent<LayoutElement>();
        if (rowLayout == null)
            rowLayout = gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = HeaderHeight;

        if (missionIconImage != null)
        {
            RectTransform iconRect = missionIconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);

            LayoutElement iconLayout = missionIconImage.GetComponent<LayoutElement>();
            if (iconLayout == null)
                iconLayout = missionIconImage.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = IconSize;
            iconLayout.preferredHeight = IconSize;
            iconLayout.minWidth = IconSize;
            iconLayout.minHeight = IconSize;
        }

        if (missionNameText != null)
        {
            RectTransform textRect = missionNameText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(600f, 48f);

            LayoutElement textLayout = missionNameText.GetComponent<LayoutElement>();
            if (textLayout == null)
                textLayout = missionNameText.gameObject.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.minWidth = 320f;
        }
    }
}