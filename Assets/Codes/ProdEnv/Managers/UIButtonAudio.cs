using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAudio : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISubmitHandler
{
    [Header("Optional Overrides")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;

    [Header("Playback")]
    [SerializeField] private float clickVolume = 1f;
    [SerializeField] private float hoverVolume = 1f;
    [SerializeField] private bool playHoverSound = true;

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHoverSound)
            return;

        AudioManager.Instance?.PlayUiHover(hoverClip, hoverVolume);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClick();
    }

    private void PlayClick()
    {
        AudioManager.Instance?.PlayUiClick(clickClip, clickVolume);
    }
}