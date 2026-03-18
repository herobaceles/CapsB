using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple facial animator for dialogue portraits. Handles optional blinking and mouth
/// frame cycling while a character is talking. Attach this to the same GameObject
/// as the portrait Image (left/right portrait or single portrait).
/// </summary>
public class PortraitFaceAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image portraitImage;

    [Header("Blink Settings")]
    [SerializeField] private bool enableBlink = true;
    [SerializeField] private float blinkIntervalMin = 3f;
    [SerializeField] private float blinkIntervalMax = 6f;
    [SerializeField] private float blinkDuration = 0.12f;

    [Header("Talking Settings")]
    [SerializeField] private bool enableTalkingMouth = true;
    [SerializeField] private float talkingFrameInterval = 0.08f;

    private Sprite idleSprite;
    private Sprite blinkSprite;
    private readonly List<Sprite> talkingSprites = new List<Sprite>();

    private bool isTalking;
    private Coroutine blinkCoroutine;
    private Coroutine talkingCoroutine;

    private void Awake()
    {
        if (portraitImage == null)
            portraitImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        StartBlinkRoutineIfNeeded();
    }

    private void OnDisable()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (talkingCoroutine != null)
        {
            StopCoroutine(talkingCoroutine);
            talkingCoroutine = null;
        }
    }

    /// <summary>
    /// Configure sprites for the current expression. Idle is the base portrait,
    /// blink is the frame used for eye closing, and talkingFrames are cycled to
    /// simulate a moving mouth while talking.
    /// </summary>
    public void SetExpressionSprites(Sprite idle, Sprite blink, Sprite[] talkingFrames)
    {
        idleSprite = idle;
        blinkSprite = blink;

        talkingSprites.Clear();
        if (talkingFrames != null)
        {
            for (int i = 0; i < talkingFrames.Length; i++)
            {
                var s = talkingFrames[i];
                if (s != null)
                    talkingSprites.Add(s);
            }
        }

        if (portraitImage != null && idleSprite != null)
            portraitImage.sprite = idleSprite;

        // Restart blink routine so new blink sprite takes effect
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        StartBlinkRoutineIfNeeded();
    }

    /// <summary>
    /// Enable or disable mouth animation while the character is talking.
    /// ProdDialogueManager calls this when a line starts/finishes typing.
    /// </summary>
    public void SetTalking(bool talking)
    {
        if (isTalking == talking)
            return;

        isTalking = talking;

        if (isTalking)
        {
            // Do not attempt to start coroutines if this component or its
            // GameObject is currently inactive in the hierarchy.
            if (!enabled || !gameObject.activeInHierarchy)
            {
                isTalking = false;
                return;
            }

            if (talkingCoroutine != null)
            {
                StopCoroutine(talkingCoroutine);
                talkingCoroutine = null;
            }

            if (talkingSprites.Count > 0)
                talkingCoroutine = StartCoroutine(TalkingRoutine());
        }
        else
        {
            if (talkingCoroutine != null)
            {
                StopCoroutine(talkingCoroutine);
                talkingCoroutine = null;
            }

            if (portraitImage != null && idleSprite != null)
                portraitImage.sprite = idleSprite;
        }
    }

    private void StartBlinkRoutineIfNeeded()
    {
        if (!enableBlink || blinkSprite == null || portraitImage == null)
            return;

        if (blinkCoroutine != null)
            return;

        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        while (enabled && gameObject.activeInHierarchy)
        {
            float waitTime = Random.Range(blinkIntervalMin, blinkIntervalMax);
            yield return new WaitForSeconds(waitTime);

            if (portraitImage == null || blinkSprite == null)
                continue;

            // Temporarily override whatever frame is currently shown (idle or talking),
            // then restore it after the blink so blinking can happen even while talking.
            var original = portraitImage.sprite;
            portraitImage.sprite = blinkSprite;
            yield return new WaitForSeconds(blinkDuration);

            if (portraitImage != null)
                portraitImage.sprite = original;
        }
    }

    private IEnumerator TalkingRoutine()
    {
        if (portraitImage == null || talkingSprites.Count == 0)
            yield break;

        int index = 0;
        var wait = new WaitForSeconds(talkingFrameInterval);

        while (isTalking && portraitImage != null && gameObject.activeInHierarchy)
        {
            var sprite = talkingSprites[index];
            if (sprite != null)
                portraitImage.sprite = sprite;

            index = (index + 1) % talkingSprites.Count;
            yield return wait;
        }

        if (portraitImage != null && idleSprite != null)
            portraitImage.sprite = idleSprite;
    }
}
