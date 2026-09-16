using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUIManager : MonoBehaviour
{
    public TurnManager turnManager;
    public TrajectoryInput trajectoryInput;
    public MatchCountdown matchCountdown;

    public TMP_Text turnText;
    public TMP_Text playerScoreText;
    public TMP_Text aiScoreText;
    public Image playerFlagImage;
    public Image aiFlagImage;
    public TMP_Text playerCountryName;
    public TMP_Text aiCountryName;
    public Image[] playerShotIndicators; // 5
    public Image[] aiShotIndicators;     // 5

    public Color defaultColor = Color.white;
    public Color scoredColor = Color.green;
    public Color missColor = Color.red;

    public Button pauseButton;
    public GameObject pausePanel;

    public RectTransform turnBGPanel;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float stayDuration = 1.2f;
    [SerializeField] private float hiddenOffsetY = 150f;

    private Vector2 restingPos;
    private bool initialized;
    private Sequence turnSequence;
    bool suddenDeathAnnounced;

    void Awake()
    {
        restingPos = turnBGPanel.anchoredPosition;
        initialized = true;
    }

    void Start()
    {
        AudioManager.Instance.OnEnteredGameplayScene();
        turnManager.OnScoreChanged += UpdateScores;
        turnManager.OnStateChanged += UpdateTurnText;
        turnManager.OnStateChanged += HandleSuddenDeathStart;
        turnManager.OnPlayerShotResult += (i, s) => SetIndicator(playerShotIndicators, i, s);
        turnManager.OnAIShotResult += (i, s) => SetIndicator(aiShotIndicators, i, s);

        pauseButton.onClick.AddListener(() => 
        { 
            AudioManager.Instance.PlayButtonClick(); 
            OnPauseClicked(); 
        });

        var roster = BracketManager.Instance.roster;
        int playerIdx = BracketManager.Instance.playerTeamIndex;
        int aiIdx = BracketManager.Instance.GetOpponentForPlayer();

        playerFlagImage.sprite = roster.teamFlags[playerIdx];
        playerCountryName.text = roster.teamNames[playerIdx];
        aiFlagImage.sprite = roster.teamFlags[aiIdx];
        aiCountryName.text = roster.teamNames[aiIdx];

        foreach (var img in playerShotIndicators) img.color = defaultColor;
        foreach (var img in aiShotIndicators) img.color = defaultColor;

        UpdateScores(0, 0);
    }

    void OnDestroy()
    {
        turnManager.OnScoreChanged -= UpdateScores;
        turnManager.OnStateChanged -= UpdateTurnText;
        turnManager.OnStateChanged -= HandleSuddenDeathStart;
    }

    void OnPauseClicked()
    {
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        trajectoryInput.inputEnabled = false;
        // trajectoryInput.CancelActiveDrag(); // new method below — kills any in-progress drag cleanly
    }

    // void SetIndicator(Image[] arr, int index, bool scored)
    // {
    //     if (index < 0 || index >= arr.Length) return;
    //     arr[index].color = scored ? scoredColor : missColor;
    // }

    void SetIndicator(Image[] arr, int index, bool scored)
    {
        int displayIndex = index < arr.Length ? index : (index - arr.Length) % arr.Length;
        arr[displayIndex].color = scored ? scoredColor : missColor;
    }

    void UpdateScores(int p, int a)
    {
        playerScoreText.text = p.ToString();
        aiScoreText.text = a.ToString();
    }

    void UpdateTurnText(TurnState state)
    {
        string newText = state switch
        {
            TurnState.PlayerInput or TurnState.SuddenDeathPlayerInput => "YOUR TURN",
            TurnState.AIThinking or TurnState.SuddenDeathAIThinking => "AI TURN",
            _ => null
        };

        if (newText == null) return; // ignore states that shouldn't trigger the panel

        AnimateTurnPanel(newText);
    }

    void AnimateTurnPanel(string newText)
    {
        if (!initialized) return;

        turnSequence?.Kill();
        turnSequence = DOTween.Sequence();

        Vector2 hiddenPos = restingPos + Vector2.down * hiddenOffsetY;

        // if panel is already up (mid-cycle interrupt), send it down first before swapping text
        turnSequence.Append(turnBGPanel.DOAnchorPos(hiddenPos, slideDuration * 0.6f).SetEase(Ease.InCubic).SetUpdate(true));
        turnSequence.AppendCallback(() => turnText.text = newText);
        turnSequence.Append(turnBGPanel.DOAnchorPos(restingPos, slideDuration).SetEase(Ease.OutBack).SetUpdate(true));
        turnSequence.AppendInterval(stayDuration);
        turnSequence.Append(turnBGPanel.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InCubic).SetUpdate(true));
    }

    void HandleSuddenDeathStart(TurnState state)
    {
        if (state != TurnState.SuddenDeathPlayerInput || suddenDeathAnnounced) return;

        suddenDeathAnnounced = true;
        foreach (var img in playerShotIndicators) img.color = defaultColor;
        foreach (var img in aiShotIndicators) img.color = defaultColor;

        matchCountdown.PlaySuddenDeathAnnouncement();
    }
}