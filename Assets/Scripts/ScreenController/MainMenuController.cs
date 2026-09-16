using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject teamSelectionPanel;
    public GameObject knockoutScreenPanel;
    public Button playButton;

    void Start()
    {
        SoundSettings.ApplyOnLoad();
        AudioManager.Instance.OnEnteredMainMenuScene();

        bool resumingTournament = BracketManager.Instance != null
            && BracketManager.Instance.TournamentStarted
            && BracketManager.Instance.currentBracketRound != BracketRound.Complete;

        if (resumingTournament)
        {
            mainMenuPanel.SetActive(false);
            teamSelectionPanel.SetActive(false);
            knockoutScreenPanel.SetActive(true);
        }
        else
        {
            mainMenuPanel.SetActive(true);
            teamSelectionPanel.SetActive(false);
            knockoutScreenPanel.SetActive(false);
        }

        playButton.onClick.AddListener(() => { AudioManager.Instance.PlayButtonClick(); OnPlay(); });
    }

    void OnPlay()
    {
        mainMenuPanel.SetActive(false);
        teamSelectionPanel.SetActive(true);
    }
}