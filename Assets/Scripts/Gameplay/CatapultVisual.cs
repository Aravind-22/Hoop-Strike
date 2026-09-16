using UnityEngine;

public class CatapultVisual : MonoBehaviour
{
    [Header("Refs")]
    public Transform ballAnchor; // ball's start position transform
    public SpriteRenderer ropeRenderer; // the stretched Square sprite
    public SpriteRenderer ringRenderer;
    public Transform catapult;

    [Header("Band Shape")]
    public float bandThickness = 0.15f;
    public float minLength = 0.1f; // length when idle/no drag

    [Header("Shake")]
    public float maxShakeAmount = 0.08f;
    public float shakeFrequency = 25f;

    [Header("Color")]
    public Color lowPowerColor = Color.green;
    public Color highPowerColor = Color.red;
    public float maxDragForColorScale = 3f; // drag magnitude at which color reaches full red

    private Vector3 baseBandPos;
    private bool active;

    [Header("Rope Scale")]
    public float ropeScaleYMin = 0.15f;
    public float ropeScaleYMax = 0.45f;
    [Header("Rope Local X Offset (tied to scale)")]
    public float ropeLocalXMin = 0.12f; // local X when scaleY = ropeScaleYMin
    public float ropeLocalXMax = 1.21f; // local X when scaleY = ropeScaleYMax

    public Transform ringTransform;

    [Header("Stretch SFX")]
    public AudioSource stretchAudioSource; // separate dedicated AudioSource
    public AudioClip stretchClip;
    public float minPitch = 0.85f;
    public float maxPitch = 1.4f;

    void Awake()
    {
        catapult.gameObject.SetActive(false);
    }

    public void Show()
    {
        active = true;
        catapult.gameObject.SetActive(true);

        if (stretchClip != null)
        {
            stretchAudioSource.clip = stretchClip;
            stretchAudioSource.loop = true; // still loop the underlying clip so it doesn't run out mid-drag, but...
            stretchAudioSource.pitch = minPitch;
            stretchAudioSource.Play();
        }
    }

    public void Hide()
    {
        active = false;
        catapult.gameObject.SetActive(false);
        stretchAudioSource.Stop();
    }

    // call every frame while dragging, pass the same dragDelta TrajectoryInput already computes
    public void UpdateBand(Vector2 dragDelta)
    {
        if (!active) return;

        float pullX = Mathf.Max(dragDelta.x, 0f);
        float angle = Mathf.Atan2(-dragDelta.y, pullX) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, -90f, 90f);
        catapult.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float dragMagnitude = dragDelta.magnitude;

        float shakeT = Mathf.Clamp01(dragMagnitude / maxDragForColorScale);
        float shakeAlongLength = Random.Range(-1f, 1f) * maxShakeAmount * shakeT;
        float shakeAcross = Random.Range(-1f, 1f) * (maxShakeAmount * 0.4f) * shakeT;
        Vector3 shakeOffset = catapult.transform.up * shakeAlongLength + catapult.transform.right * shakeAcross;

        catapult.transform.position = ballAnchor.position + shakeOffset;

        // rope scale Y and local X both driven by the same drag-strength interpolant
        float scaleT = Mathf.Clamp01(dragMagnitude / maxDragForColorScale);
        float ropeScaleY = Mathf.Lerp(ropeScaleYMin, ropeScaleYMax, scaleT);
        float ropeLocalX = Mathf.Lerp(ropeLocalXMin, ropeLocalXMax, scaleT);

        Vector3 ropeLocalPos = ropeRenderer.transform.localPosition;
        ropeRenderer.transform.localPosition = new Vector3(ropeLocalX, ropeLocalPos.y, ropeLocalPos.z);
        ropeRenderer.transform.localScale = new Vector3(ropeRenderer.transform.localScale.x, ropeScaleY, 1f);

        // ring: positioned at rope's current far edge
        float ropeLocalLength = ropeRenderer.sprite.bounds.size.y * ropeRenderer.transform.lossyScale.y;
        ringTransform.position = ropeRenderer.transform.position + ropeRenderer.transform.up * -ropeLocalLength * 0.7f;
        ringTransform.rotation = catapult.transform.rotation;

        float colorT = Mathf.Clamp01(dragMagnitude / maxDragForColorScale);
        ropeRenderer.color = Color.Lerp(lowPowerColor, highPowerColor, colorT);
        ringRenderer.color = Color.Lerp(lowPowerColor, highPowerColor, colorT);

        if (stretchAudioSource.isPlaying)
            stretchAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, scaleT);
    }
}