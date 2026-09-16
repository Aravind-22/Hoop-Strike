using System;
using UnityEngine;

public enum TurnState
{
    PlayerInput,
    PlayerShotResolving,
    AIThinking,
    AIShotResolving,
    TurnTransition,
    SuddenDeathPlayerInput,
    SuddenDeathPlayerResolving,
    SuddenDeathAIThinking,
    SuddenDeathAIResolving,
    MatchComplete
}

public class TurnManager : MonoBehaviour
{
    [Header("References")]
    public BallFlight ball;
    public TrajectoryInput trajectoryInput;
    public AIController aiController;
    public Transform ballStartPoint;
    public MatchCountdown matchCountdown;

    [Header("Match Settings")]
    public int shotsPerPlayer = 5;
    public float turnTransitionDelay = 0.8f;

    public TurnState state { get; private set; }
    public int playerScore { get; private set; }
    public int aiScore { get; private set; }
    public int playerShotsTaken { get; private set; }
    public int aiShotsTaken { get; private set; }

    public event Action<TurnState> OnStateChanged;
    public event Action<int, int> OnScoreChanged;
    public event Action<bool> OnMatchEnded; // true = player won

    private bool suddenDeathPlayerScored;

    public enum KnockoutRound { Round16, Round8, Round4, Final }

    [Header("Ball Start Position Randomization")]
    public float startXMin = -1f;
    public float startXMax = 6f;
    public KnockoutRound currentRound = KnockoutRound.Round16;

    [Header("AI Difficulty Per Round")]
    [Range(0f,1f)] public float difficultyRound16 = 0.3f;
    [Range(0f,1f)] public float difficultyRound8 = 0.55f;
    [Range(0f,1f)] public float difficultyRound4 = 0.75f;
    [Range(0f,1f)] public float difficultyFinal = 0.95f;

    private Vector3 currentBallStartPos;
    public event System.Action<int, bool> OnPlayerShotResult; // shotIndex, scored
    public event System.Action<int, bool> OnAIShotResult;

    void Start()
    {
        ball.OnShotResolved += HandleShotResolved;
        trajectoryInput.OnBallLaunched += NotifyPlayerShotLaunched;

        trajectoryInput.inputEnabled = false; // block input during countdown
        matchCountdown.PlayCountdown();
        matchCountdown.onCountdownComplete = BeginMatch;
    }

    void OnDestroy()
    {
        ball.OnShotResolved -= HandleShotResolved;
        trajectoryInput.OnBallLaunched -= NotifyPlayerShotLaunched;
    }

    public void SetRound(KnockoutRound round)
    {
        currentRound = round;
        aiController.difficulty = round switch
        {
            KnockoutRound.Round16 => difficultyRound16,
            KnockoutRound.Round8 => difficultyRound8,
            KnockoutRound.Round4 => difficultyRound4,
            KnockoutRound.Final => difficultyFinal,
            _ => difficultyRound16
        };
    }

    void BeginMatch()
    {
        playerScore = 0;
        aiScore = 0;
        playerShotsTaken = 0;
        aiShotsTaken = 0;
        ChangeState(TurnState.PlayerInput);
    }

    void ChangeState(TurnState next)
    {
        state = next;
        OnStateChanged?.Invoke(state);

        switch (state)
        {
            case TurnState.PlayerInput:
            case TurnState.SuddenDeathPlayerInput:
                currentBallStartPos = GenerateNewBallStartPos();
                ResetBallForNextShot();
                VFXManager.Instance.PlayBallHighlightPrefab(currentBallStartPos);
                trajectoryInput.inputEnabled = true;
                break;

            case TurnState.PlayerShotResolving:
            case TurnState.AIShotResolving:
            case TurnState.SuddenDeathPlayerResolving:
            case TurnState.SuddenDeathAIResolving:
                trajectoryInput.inputEnabled = false;
                break;

            case TurnState.AIThinking:
            case TurnState.SuddenDeathAIThinking:
                ResetBallForNextShot();
                trajectoryInput.inputEnabled = false;
                aiController.TakeShot(() => ChangeState(
                    state == TurnState.AIThinking ? TurnState.AIShotResolving : TurnState.SuddenDeathAIResolving));
                break;

            case TurnState.TurnTransition:
                Invoke(nameof(AdvanceTurn), turnTransitionDelay);
                break;

            case TurnState.MatchComplete:
                trajectoryInput.inputEnabled = false;
                break;
        }
    }

    void ResetBallForNextShot() => ball.ResetForNextShot(currentBallStartPos);

    float GetYRangeForRound()
    {
        switch (currentRound)
        {
            case KnockoutRound.Round16: return 0.5f;
            case KnockoutRound.Round8: return 1.0f;
            case KnockoutRound.Round4: return 1.5f;
            case KnockoutRound.Final: return 1.5f;
            default: return 0.5f;
        }
    }

    Vector3 GenerateNewBallStartPos()
    {
        float x = UnityEngine.Random.Range(startXMin, startXMax);
        float yRange = GetYRangeForRound();
        float y = UnityEngine.Random.Range(-yRange, yRange);
        return new Vector3(x, y, ballStartPoint.position.z);
    }

    void HandleShotResolved(bool scored)
    {
        switch (state)
        {
            case TurnState.PlayerShotResolving:
                if (scored) 
                { 
                    playerScore++; 
                    AudioManager.Instance.PlayCrowdCheer(); 
                    VFXManager.Instance.PlayScorePopup(new Vector3(-4.85f, 0.5f, 0f));
                }
                else AudioManager.Instance.PlayCrowdGroan();

                OnPlayerShotResult?.Invoke(playerShotsTaken, scored);
                playerShotsTaken++;
                OnScoreChanged?.Invoke(playerScore, aiScore);
                ChangeState(TurnState.TurnTransition);
                break;

            case TurnState.AIShotResolving:
                if (scored) 
                { 
                    aiScore++; 
                    AudioManager.Instance.PlayCrowdCheer(); 
                    VFXManager.Instance.PlayScorePopup(new Vector3(-4.85f, 0.5f, 0f));
                }
                else AudioManager.Instance.PlayCrowdGroan();

                OnAIShotResult?.Invoke(aiShotsTaken, scored);
                aiShotsTaken++;
                OnScoreChanged?.Invoke(playerScore, aiScore);
                ChangeState(TurnState.TurnTransition);
                break;

            case TurnState.SuddenDeathPlayerResolving:
                suddenDeathPlayerScored = scored;
                if (suddenDeathPlayerScored)
                {
                    playerScore++;
                    AudioManager.Instance.PlayCrowdCheer(); 
                    VFXManager.Instance.PlayScorePopup(new Vector3(-4.85f, 0.5f, 0f));
                }
                else AudioManager.Instance.PlayCrowdGroan();

                OnPlayerShotResult?.Invoke(playerShotsTaken, scored);
                playerShotsTaken++;
                OnScoreChanged?.Invoke(playerScore, aiScore);

                ChangeState(TurnState.SuddenDeathAIThinking);
                break;

            case TurnState.SuddenDeathAIResolving:
                bool aiScored = scored;
                
                if (aiScored)
                {
                    aiScore++;   
                    AudioManager.Instance.PlayCrowdCheer(); 
                    VFXManager.Instance.PlayScorePopup(new Vector3(-4.85f, 0.5f, 0f));
                }
                else AudioManager.Instance.PlayCrowdGroan();

                OnAIShotResult?.Invoke(aiShotsTaken, scored);
                aiShotsTaken++;
                OnScoreChanged?.Invoke(playerScore, aiScore);

                if (suddenDeathPlayerScored != aiScored)
                    EndMatch(suddenDeathPlayerScored);
                else
                    Invoke(nameof(StartNextSuddenDeathRound), turnTransitionDelay);
                break;
        }
    }

    void StartNextSuddenDeathRound() => ChangeState(TurnState.SuddenDeathPlayerInput);

    void NotifyPlayerShotLaunched()
    {
        if (state == TurnState.PlayerInput)
            ChangeState(TurnState.PlayerShotResolving);
        else if (state == TurnState.SuddenDeathPlayerInput)
            ChangeState(TurnState.SuddenDeathPlayerResolving);
    }

    void AdvanceTurn()
    {
        bool playerDone = playerShotsTaken >= shotsPerPlayer;
        bool aiDone = aiShotsTaken >= shotsPerPlayer;

        if (playerDone && aiDone)
        {
            if (playerScore != aiScore)
                EndMatch(playerScore > aiScore);
            else
                ChangeState(TurnState.SuddenDeathPlayerInput);
            return;
        }

        if (playerShotsTaken > aiShotsTaken && !aiDone)
            ChangeState(TurnState.AIThinking);
        else if (!playerDone)
            ChangeState(TurnState.PlayerInput);
        else
            ChangeState(TurnState.AIThinking);
    }

    void EndMatch(bool playerWon)
    {
        ChangeState(TurnState.MatchComplete);
        OnMatchEnded?.Invoke(playerWon);
    }
}