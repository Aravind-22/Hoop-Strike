using System;
using DG.Tweening;
using UnityEngine;

public enum SlideDirection { FromLeft, FromRight, FromTop, FromBottom }

public class UISlideIn : MonoBehaviour
{
    public SlideDirection direction = SlideDirection.FromRight;
    public float distance = 600f;
    public float duration = 0.5f;
    public float delay = 0f;
    public Ease ease = Ease.OutCubic;
    public bool playOnEnable = true;

    private RectTransform rt;
    private Vector2 restingPos;
    private bool initialized;
    private Tween activeTween;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        restingPos = rt.anchoredPosition; // captured ONCE, never recalculated from live position again
        initialized = true;
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    void OnDisable()
    {
        activeTween?.Kill();
        if (initialized)
            rt.anchoredPosition = restingPos; // always snap back to true rest position, never leave it mid-offset
    }

    public void Play(Action onComplete = null)
    {
        activeTween?.Kill();

        Vector2 offset = direction switch
        {
            SlideDirection.FromLeft => Vector2.left * distance,
            SlideDirection.FromRight => Vector2.right * distance,
            SlideDirection.FromTop => Vector2.up * distance,
            SlideDirection.FromBottom => Vector2.down * distance,
            _ => Vector2.zero
        };

        rt.anchoredPosition = restingPos + offset;

        activeTween = rt.DOAnchorPos(restingPos, duration)
            .SetEase(ease)
            .SetDelay(delay)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }
}