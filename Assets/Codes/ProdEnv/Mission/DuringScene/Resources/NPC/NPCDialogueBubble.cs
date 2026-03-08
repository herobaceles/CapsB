using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NPCDialogueBubble : MonoBehaviour
{
    [Header("Positioning")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool followPosition = true;
    [SerializeField] private float followSmoothness = 12f;
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool keepUpright = true;

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text textLabel;

    [Header("Display")]
    [SerializeField] private float defaultDuration = 3f;
    [SerializeField] private float fadeDuration = 0.2f;

    private readonly Queue<DialogueEntry> queue = new Queue<DialogueEntry>();
    private Coroutine playbackRoutine;
    private Camera cachedCamera;

    private void Awake()
    {
        if (followTarget == null && transform.parent != null)
            followTarget = transform.parent;

        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);

        if (canvasGroup == null)
        {
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);
            if (canvasGroup == null && canvas != null)
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        if (textLabel == null)
            textLabel = GetComponentInChildren<TMP_Text>(true);

        SetVisibleImmediate(false);
    }

    private void OnEnable()
    {
        cachedCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (followPosition && followTarget != null)
        {
            Vector3 desired = followTarget.position + worldOffset;
            transform.position = followSmoothness <= 0f
                ? desired
                : Vector3.Lerp(transform.position, desired, followSmoothness * Time.deltaTime);
        }

        if (faceCamera)
        {
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
                cachedCamera = Camera.main;

            if (cachedCamera != null)
            {
                Vector3 toCamera = cachedCamera.transform.position - transform.position;
                if (keepUpright)
                    toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
            }
        }
    }

    public void ShowLine(string text, float duration = -1f)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        float displayDuration = duration > 0f ? duration : defaultDuration;
        queue.Enqueue(new DialogueEntry(text, displayDuration));

        if (playbackRoutine == null)
            playbackRoutine = StartCoroutine(PlayQueue());
    }

    public void HideImmediate()
    {
        queue.Clear();
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }
        SetVisibleImmediate(false);
    }

    private IEnumerator PlayQueue()
    {
        while (queue.Count > 0)
        {
            DialogueEntry entry = queue.Dequeue();
            if (textLabel != null)
                textLabel.text = entry.Text;

            yield return FadeTo(1f);
            yield return new WaitForSeconds(entry.Duration);
            yield return FadeTo(0f);
        }

        playbackRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (canvasGroup == null || fadeDuration <= 0f)
        {
            SetVisibleImmediate(targetAlpha > 0.99f);
            yield break;
        }

        if (targetAlpha > 0f)
            canvasGroup.gameObject.SetActive(true);

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
            canvasGroup.gameObject.SetActive(false);
    }

    private void SetVisibleImmediate(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.gameObject.SetActive(visible);
        }

        if (canvas != null)
            canvas.enabled = true; // keep enabled so layout updates even when hidden
    }

    private readonly struct DialogueEntry
    {
        public readonly string Text;
        public readonly float Duration;

        public DialogueEntry(string text, float duration)
        {
            Text = text;
            Duration = duration;
        }
    }
}
