using System.Collections;
using TMPro;
using UnityEngine;

public class MatchCountdown : MonoBehaviour
{
    public TMP_Text countdownText;
    public SpriteRenderer ballSprite;
    public System.Action onCountdownComplete;

    public TMP_Text suddenDeathText; // separate TMP_Text, assign in Inspector, inactive by default in scene
    public float suddenDeathHoldDuration = 1f;
    public float suddenDeathFadeDuration = 0.6f;

    public void PlayCountdown()
    {
        ballSprite.enabled = false;
        countdownText.gameObject.SetActive(true);
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        int[] steps = { 3, 2, 1 };
        foreach (int n in steps)
        {
            countdownText.text = n.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.gameObject.SetActive(false);
        ballSprite.enabled = true;
        AudioManager.Instance.PlayWhistle();

        onCountdownComplete?.Invoke();
    }

    public void PlaySuddenDeathAnnouncement()
    {
        StartCoroutine(SuddenDeathRoutine());
    }

    IEnumerator SuddenDeathRoutine()
    {
        suddenDeathText.gameObject.SetActive(true);
        suddenDeathText.text = "SUDDEN DEATH";

        Color c = suddenDeathText.color;
        suddenDeathText.color = new Color(c.r, c.g, c.b, 1f);

        yield return new WaitForSeconds(suddenDeathHoldDuration);

        float elapsed = 0f;
        while (elapsed < suddenDeathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / suddenDeathFadeDuration);
            suddenDeathText.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        suddenDeathText.gameObject.SetActive(false);
    }
}