using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{
    public GameObject pausePanel;
    public Button resumeButton;
    public Button homeButton;
    public string mainMenuScene = "MainMenu";
    public TrajectoryInput trajectoryInput;

    void Start()
    {
        resumeButton.onClick.AddListener(() => { AudioManager.Instance.PlayButtonClick(); Resume(); });
        homeButton.onClick.AddListener(() => { AudioManager.Instance.PlayButtonClick(); GoHome(); });
    }

    void Resume()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        trajectoryInput.inputEnabled = true;
    }

    void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}