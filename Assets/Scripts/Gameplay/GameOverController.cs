using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    public TurnManager turnManager;
    public GameObject gameOverPanel;
    public TMP_Text titleText;      // WIN/LOSE
    public TMP_Text victoryText;    // congrats / better luck
    public GameObject victoryBadge;
    public GameObject victoryCup;
    public Button homeButton;
    public Button nextButton;
    public string mainMenuScene = "MainMenu";

    void Start()
    {
        turnManager.OnMatchEnded += ShowGameOver;
        gameOverPanel.SetActive(false);
        victoryBadge.SetActive(false);
        victoryCup.SetActive(false);

        homeButton.onClick.AddListener(() => { AudioManager.Instance.PlayButtonClick(); SceneManager.LoadScene(mainMenuScene); });
        nextButton.onClick.AddListener(() => { AudioManager.Instance.PlayButtonClick(); SceneManager.LoadScene(mainMenuScene); });
        
    }

    void OnDestroy() => turnManager.OnMatchEnded -= ShowGameOver;

    void ShowGameOver(bool playerWon)
    {
        bool isFinal = BracketManager.Instance.currentBracketRound == BracketRound.Final;
        bool wonFinal = playerWon && isFinal;

        if (wonFinal)
        {
            VFXManager.Instance.PlayConfetti();
            AudioManager.Instance.PlayVictoryFanfare();
            VFXManager.Instance.PlayFlash();
        } 
        else if (playerWon)
        {
            VFXManager.Instance.PlayConfetti();
            AudioManager.Instance.PlayWinJingle();
        } 
        else AudioManager.Instance.PlayLoseJingle();
        
        gameOverPanel.SetActive(true);
        titleText.text = playerWon ? "YOU WON!" : "YOU LOST";
        victoryText.text = playerWon ? "Congratulations!" : "Better luck next time!";

        victoryBadge.SetActive(wonFinal);
        victoryCup.SetActive(wonFinal);
        if (wonFinal)
        {
            StartCoroutine(ScalePunch(victoryBadge.transform));
            StartCoroutine(ScalePunch(victoryCup.transform));
        }

        nextButton.gameObject.SetActive(playerWon && !isFinal);

        BracketManager.Instance.ReportPlayerMatchResult(playerWon);
    }

    IEnumerator ScalePunch(Transform t)
    {
        t.localScale = Vector3.zero;
        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float c1 = 1.70158f, c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(p - 1f, 3) + c1 * Mathf.Pow(p - 1f, 2);
            t.localScale = Vector3.one * ease;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}