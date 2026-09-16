using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIPulseScale : MonoBehaviour
{
    public float scaleMax = 1.08f;
    public float duration = 0.6f;
    public Ease ease = Ease.InOutSine;

    [Tooltip("If assigned, pulse only runs while this Button is interactable.")]
    public Button gateButton;

    private Tween pulseTween;
    private bool wasInteractable;

    void OnEnable()
    {
        transform.localScale = Vector3.one;
        wasInteractable = gateButton == null || gateButton.interactable;
        if (wasInteractable) StartPulse();
    }

    void OnDisable()
    {
        pulseTween?.Kill();
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        if (gateButton == null) return;

        bool nowInteractable = gateButton.interactable;
        if (nowInteractable == wasInteractable) return;

        wasInteractable = nowInteractable;
        if (nowInteractable) StartPulse();
        else StopPulse();
    }

    void StartPulse()
    {
        pulseTween?.Kill();
        pulseTween = transform.DOScale(scaleMax, duration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    void StopPulse()
    {
        pulseTween?.Kill();
        transform.DOScale(1f, 0.2f).SetUpdate(true);
    }
}