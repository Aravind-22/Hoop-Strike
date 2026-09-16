using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("VFX Prefabs (SpriteRenderer GameObjects)")]
    public GameObject impactSparkPrefab;
    public GameObject smokePrefab;
    public GameObject floorShadowPrefab;
    public GameObject scoreHighlightPrefab;
    public GameObject floorDustPrefab;
    public GameObject scorePopupPrefab;
    public GameObject ballHighlightPrefab;

    [Header("Pool Sizes")]
    public int poolSizePerEffect = 2;

    [Header("Score Popup Text")]
    public float popupRiseDistance = 1f;
    public float popupDuration = 0.8f;

    public GameObject fireworksVFX;
    public GameObject confettiVFX;
    public GameObject flashVFX;

    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PrewarmPool(impactSparkPrefab);
        PrewarmPool(smokePrefab);
        PrewarmPool(floorShadowPrefab);
        PrewarmPool(scoreHighlightPrefab);
        PrewarmPool(floorDustPrefab);
        PrewarmPool(scorePopupPrefab);
        PrewarmPool(ballHighlightPrefab);
    }

    void PrewarmPool(GameObject prefab)
    {
        if (prefab == null) return;
        Queue<GameObject> pool = new Queue<GameObject>();
        for (int i = 0; i < poolSizePerEffect; i++)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.SetActive(false);
            pool.Enqueue(instance);
        }
        pools[prefab] = pool;
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab)) PrewarmPool(prefab);

        Queue<GameObject> pool = pools[prefab];
        GameObject instance = null;

        while (pool.Count > 0)
        {
            GameObject candidate = pool.Dequeue();
            if (candidate != null) { instance = candidate; break; } // skip destroyed refs instead of crashing
        }

        if (instance == null)
            instance = Instantiate(prefab, transform); // pool exhausted or all corrupted, make a fresh one

        pool.Enqueue(instance);
        return instance;
    }

    public void PlayImpactSpark(Vector3 pos) => SpawnFadeOut(impactSparkPrefab, pos, 0.3f);
    public void PlaySmoke(Vector3 pos) => SpawnFadeOut(smokePrefab, pos, 0.5f);
    public void PlayScoreHighlight(Vector3 pos) => SpawnFadeOut(scoreHighlightPrefab, pos, 0.4f);
    public void PlayBallHighlightPrefab(Vector3 pos) => SpawnFadeOut(ballHighlightPrefab, pos, 1.4f);
    public void PlayFloorDust(Vector3 pos) => SpawnFadeOut(floorDustPrefab, pos, 0.3f);

    public GameObject ShowFloorShadow(Vector3 pos)
    {
        if (floorShadowPrefab == null) return null;
        GameObject instance = GetFromPool(floorShadowPrefab);
        instance.transform.position = pos;
        ResetSpriteAlpha(instance);
        instance.SetActive(true);
        return instance;
    }

    public void HideFloorShadow(GameObject instance)
    {
        if (instance != null) instance.SetActive(false);
    }

    public void PlayScorePopup(Vector3 worldPos, int points = 1)
    {
        if (scorePopupPrefab == null) return;
        GameObject instance = GetFromPool(scorePopupPrefab);
        instance.transform.position = worldPos;
        TMP_Text text = instance.GetComponentInChildren<TMP_Text>();
        if (text != null) { text.text = "+" + points; SetTextAlpha(text, 1f); }
        instance.SetActive(true);
        StartCoroutine(RiseAndFade(instance, popupDuration));
    }

    void SpawnFadeOut(GameObject prefab, Vector3 pos, float duration)
    {
        if (prefab == null) return;
        GameObject instance = GetFromPool(prefab);
        instance.transform.position = pos;
        ResetSpriteAlpha(instance);
        instance.SetActive(true);
        StartCoroutine(FadeAndDeactivate(instance, duration));
    }

    void ResetSpriteAlpha(GameObject instance)
    {
        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    void SetTextAlpha(TMP_Text text, float alpha)
    {
        Color c = text.color;
        text.color = new Color(c.r, c.g, c.b, alpha);
    }

    IEnumerator FadeAndDeactivate(GameObject instance, float duration)
    {
        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        Color startColor = sr != null ? sr.color : Color.white;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }
        instance.SetActive(false);
    }

    IEnumerator RiseAndFade(GameObject instance, float duration)
    {
        TMP_Text text = instance.GetComponentInChildren<TMP_Text>();
        Color startColor = text != null ? text.color : Color.white;
        Vector3 startPos = instance.transform.position;
        Vector3 endPos = startPos + Vector3.up * popupRiseDistance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            instance.transform.position = Vector3.Lerp(startPos, endPos, t);
            if (text != null)
                text.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
        instance.SetActive(false);
    }

    IEnumerator DeactivateAfter(GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        instance.SetActive(false);
    }

    public void PlayFireworks()
    {
        if (fireworksVFX == null) return;
        fireworksVFX.SetActive(true);
        CancelInvoke(nameof(StopFireworks)); // avoid stacking if triggered again before the 2s window ends
        Invoke(nameof(StopFireworks), 2f);
    }

    void StopFireworks() => fireworksVFX.SetActive(false);
    void StopConfetti() => confettiVFX.SetActive(false);

    public void PlayConfetti()
    {
        if (confettiVFX == null) return;
        confettiVFX.SetActive(true);
        CancelInvoke(nameof(StopConfetti)); // avoid stacking if triggered again before the 2s window ends
        Invoke(nameof(StopConfetti), 4f);
    }

    public void PlayFlash()
    {
        if (flashVFX == null) return;
        flashVFX.SetActive(true);
    }
}