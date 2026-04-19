using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuAchievementsPanel : MonoBehaviour
{
    private const float DefaultTaskCardWidth = 180f;
    private const float DefaultTaskCardHeight = 180f;

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
        Refresh();

        if (panelRoot != null)
            panelRoot.SetActive(true);
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

        float availableWidth = contentRoot.rect.width;
        if (availableWidth <= 0f)
            availableWidth = 700f;

        float currentY = 0f;
        float currentX = 0f;
        float currentRowHeight = 0f;

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

                if (currentRowHeight > 0f)
                {
                    currentY += currentRowHeight + sectionSpacing;
                    currentX = 0f;
                    currentRowHeight = 0f;
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
                preferredWidth = child.sizeDelta.x > 0f ? child.sizeDelta.x : 180f;

            float preferredHeight = LayoutUtility.GetPreferredHeight(child);
            if (preferredHeight <= 0f)
                preferredHeight = child.sizeDelta.y > 0f ? child.sizeDelta.y : 180f;

            if (currentX > 0f && currentX + preferredWidth > availableWidth)
            {
                currentY += currentRowHeight + itemSpacing;
                currentX = 0f;
                currentRowHeight = 0f;
            }

            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(0f, 1f);
            child.pivot = new Vector2(0f, 1f);
            child.anchoredPosition = new Vector2(currentX, -currentY);
            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);

            currentX += preferredWidth + itemSpacing;
            currentRowHeight = Mathf.Max(currentRowHeight, preferredHeight);
        }

        if (currentRowHeight > 0f)
            currentY += currentRowHeight;

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