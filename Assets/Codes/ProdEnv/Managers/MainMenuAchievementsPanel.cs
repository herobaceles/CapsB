using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuAchievementsPanel : MonoBehaviour
{
    private const float DefaultTaskCardWidth = 250f;
    private const float DefaultTaskCardHeight = 235f;
    private const float TopContentMargin = 20f;

    [SerializeField] private float itemSpacing = 12f;
    [SerializeField] private float sectionSpacing = 20f;
    [SerializeField] private bool showMissionHeaders = false;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Content")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private MainMenuAchievementMissionHeaderView missionHeaderTemplate;
    [SerializeField] private MainMenuAchievementTaskRowView taskRowTemplate;
    [SerializeField] private Sprite fallbackIcon;

    public void SetCloseHandler(UnityAction handler)
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveAllListeners();
        if (handler != null)
            closeButton.onClick.AddListener(handler);
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        Canvas.ForceUpdateCanvases();
        Refresh();

        if (contentRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        if (contentRoot == null || missionHeaderTemplate == null || taskRowTemplate == null)
            return;

        ConfigureContentLayout();
        ClearRuntimeChildren();

        IReadOnlyList<MissionData> missions = MissionCatalog.LoadMissions();
        foreach (MissionData mission in missions
                     .Where(mission => mission != null)
                     .OrderBy(mission => mission.phase)
                     .ThenBy(mission => mission.sortOrder))
        {
            if (showMissionHeaders)
            {
                MainMenuAchievementMissionHeaderView header = Instantiate(missionHeaderTemplate, contentRoot);
                header.gameObject.SetActive(true);
                header.Bind(mission, fallbackIcon);
            }

            if (mission.tasks == null)
                continue;

            foreach (TaskData task in mission.tasks)
            {
                if (task == null)
                    continue;

                MainMenuAchievementTaskRowView row = Instantiate(taskRowTemplate, contentRoot);
                row.gameObject.SetActive(true);
                row.Bind(task, MissionSceneManager.IsTaskCompleted(mission.missionId, task.taskId), mission.missionIcon, fallbackIcon);
            }
        }

        ArrangeRuntimeChildren();
    }

    private void Awake()
    {
        if (missionHeaderTemplate != null)
            missionHeaderTemplate.gameObject.SetActive(false);

        if (taskRowTemplate != null)
            taskRowTemplate.gameObject.SetActive(false);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ClearRuntimeChildren()
    {
        for (int index = contentRoot.childCount - 1; index >= 0; index--)
        {
            Transform child = contentRoot.GetChild(index);
            if (child == missionHeaderTemplate.transform || child == taskRowTemplate.transform)
                continue;

            Destroy(child.gameObject);
        }
    }

    private void ArrangeRuntimeChildren()
    {
        if (contentRoot == null)
            return;

        RectTransform viewport = contentRoot.parent as RectTransform;
        float availableWidth = viewport != null ? viewport.rect.width : contentRoot.rect.width;
        availableWidth = Mathf.Max(availableWidth, contentRoot.rect.width);
        if (availableWidth <= 0f)
            availableWidth = 700f;

        HorizontalLayoutGroup contentLayoutGroup = contentRoot.GetComponent<HorizontalLayoutGroup>();
        float leftPadding = contentLayoutGroup != null ? contentLayoutGroup.padding.left : 0f;
        float rightPadding = contentLayoutGroup != null ? contentLayoutGroup.padding.right : 0f;
        float usableWidth = Mathf.Max(0f, availableWidth - leftPadding - rightPadding);

        float currentY = TopContentMargin;
        float currentRowHeight = 0f;
        List<RectTransform> rowChildren = new List<RectTransform>();
        List<float> rowWidths = new List<float>();

        void FlushTaskRow()
        {
            if (rowChildren.Count == 0)
                return;

            float totalRowWidth = 0f;
            for (int rowIndex = 0; rowIndex < rowWidths.Count; rowIndex++)
                totalRowWidth += rowWidths[rowIndex];

            float remainingWidth = Mathf.Max(0f, usableWidth - totalRowWidth);
            float slotSpacing = Mathf.Max(itemSpacing, remainingWidth / (rowChildren.Count + 1));
            float currentX = leftPadding + slotSpacing;
            for (int rowIndex = 0; rowIndex < rowChildren.Count; rowIndex++)
            {
                RectTransform rowChild = rowChildren[rowIndex];
                float childWidth = rowWidths[rowIndex];

                rowChild.anchorMin = new Vector2(0f, 1f);
                rowChild.anchorMax = new Vector2(0f, 1f);
                rowChild.pivot = new Vector2(0f, 1f);
                rowChild.anchoredPosition = new Vector2(currentX, -currentY);
                rowChild.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, childWidth);

                currentX += childWidth + slotSpacing;
            }

            currentY += currentRowHeight;
            rowChildren.Clear();
            rowWidths.Clear();
            currentRowHeight = 0f;
        }

        for (int index = 0; index < contentRoot.childCount; index++)
        {
            RectTransform child = contentRoot.GetChild(index) as RectTransform;
            if (child == null)
                continue;

            if (child == missionHeaderTemplate.transform || child == taskRowTemplate.transform)
                continue;

            bool isMissionHeader = child.GetComponent<MainMenuAchievementMissionHeaderView>() != null;

            if (isMissionHeader)
            {
                if (!showMissionHeaders)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                if (rowChildren.Count > 0)
                {
                    FlushTaskRow();
                    currentY += sectionSpacing;
                }

                child.anchorMin = new Vector2(0f, 1f);
                child.anchorMax = new Vector2(1f, 1f);
                child.pivot = new Vector2(0.5f, 1f);
                child.anchoredPosition = new Vector2(0f, -currentY);

                float headerHeight = LayoutUtility.GetPreferredHeight(child);
                if (headerHeight <= 0f)
                    headerHeight = child.sizeDelta.y > 0f ? child.sizeDelta.y : 84f;

                child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, headerHeight);
                currentY += headerHeight + itemSpacing;
                continue;
            }

            float preferredWidth = LayoutUtility.GetPreferredWidth(child);
            if (preferredWidth <= 0f)
                preferredWidth = child.sizeDelta.x > 0f ? child.sizeDelta.x : DefaultTaskCardWidth;

            float preferredHeight = LayoutUtility.GetPreferredHeight(child);
            if (preferredHeight <= 0f)
                preferredHeight = child.sizeDelta.y > 0f ? child.sizeDelta.y : DefaultTaskCardHeight;

            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);

            float projectedRowWidth = preferredWidth;
            for (int rowIndex = 0; rowIndex < rowWidths.Count; rowIndex++)
                projectedRowWidth += rowWidths[rowIndex];

            int projectedItemCount = rowChildren.Count + 1;
            projectedRowWidth += itemSpacing * (projectedItemCount + 1);

            if (rowChildren.Count > 0 && projectedRowWidth > usableWidth)
            {
                FlushTaskRow();
                currentY += itemSpacing;
            }

            rowChildren.Add(child);
            rowWidths.Add(preferredWidth);
            currentRowHeight = Mathf.Max(currentRowHeight, preferredHeight);
        }

        FlushTaskRow();

        contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentY);
    }

    private void ConfigureContentLayout()
    {
        if (contentRoot == null)
            return;

        ContentSizeFitter contentSizeFitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
            contentSizeFitter.enabled = false;

        VerticalLayoutGroup verticalLayoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
            verticalLayoutGroup.enabled = false;

        HorizontalLayoutGroup horizontalLayoutGroup = contentRoot.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayoutGroup != null)
            horizontalLayoutGroup.enabled = false;

        GridLayoutGroup gridLayoutGroup = contentRoot.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup != null)
            gridLayoutGroup.enabled = false;
    }
}