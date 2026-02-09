using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BaHanda.AR
{
    /// <summary>
    /// Single item in the objective checklist UI.
    /// Shows item name, icon, and completion status.
    /// </summary>
    public class ARObjectiveItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image checkmark;
        [SerializeField] private Image background;
        [SerializeField] private GameObject strikethrough;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color incompleteTextColor = Color.white;
        [SerializeField] private Color completeTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color incompleteBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color completeBackgroundColor = new Color(0.1f, 0.3f, 0.1f, 0.8f);

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string completeTrigger = "Complete";

        // State
        private string itemId;
        private bool isCompleted = false;
        private bool isRequired = true;

        // Properties
        public string ItemId => itemId;
        public bool IsCompleted => isCompleted;
        public bool IsRequired => isRequired;

        /// <summary>
        /// Setup the item UI
        /// </summary>
        public void Setup(string id, string name, Sprite icon, bool required = true)
        {
            itemId = id;
            isRequired = required;

            if (itemNameText != null)
            {
                itemNameText.text = name;
                itemNameText.fontStyle = required ? FontStyles.Normal : FontStyles.Italic;
            }

            if (itemIcon != null)
            {
                if (icon != null)
                {
                    itemIcon.sprite = icon;
                    itemIcon.gameObject.SetActive(true);
                }
                else
                {
                    itemIcon.gameObject.SetActive(false);
                }
            }

            SetCompleted(false);
        }

        /// <summary>
        /// Set completion status
        /// </summary>
        public void SetCompleted(bool completed)
        {
            isCompleted = completed;

            // Checkmark
            if (checkmark != null)
            {
                checkmark.gameObject.SetActive(completed);
            }

            // Strikethrough
            if (strikethrough != null)
            {
                strikethrough.SetActive(completed);
            }

            // Text color
            if (itemNameText != null)
            {
                itemNameText.color = completed ? completeTextColor : incompleteTextColor;
            }

            // Background
            if (background != null)
            {
                background.color = completed ? completeBackgroundColor : incompleteBackgroundColor;
            }

            // Fade if complete
            if (canvasGroup != null)
            {
                canvasGroup.alpha = completed ? 0.7f : 1f;
            }

            // Play animation
            if (completed && animator != null && !string.IsNullOrEmpty(completeTrigger))
            {
                animator.SetTrigger(completeTrigger);
            }
        }

        /// <summary>
        /// Animate a highlight effect (for hints)
        /// </summary>
        public void Highlight()
        {
            if (animator != null)
            {
                animator.SetTrigger("Highlight");
            }
        }
    }
}
