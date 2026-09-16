// NetRippleEffect.cs — attach directly to your existing Net sprite GameObject
using DG.Tweening;
using UnityEngine;

public class NetRippleEffect : MonoBehaviour
{
    public float rippleScaleY = 0.85f;
    public float duration = 0.15f;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void PlayRipple()
    {
        transform.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOScaleY(originalScale.y * rippleScaleY, duration).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScaleY(originalScale.y, duration).SetEase(Ease.InOutSine));
    }
}