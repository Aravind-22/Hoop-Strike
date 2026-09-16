using System;
using System.Collections.Generic;
using UnityEngine;

public enum BracketRound { Round16, Round8, Round4, Final, Complete }

[Serializable]
public class Matchup { public int teamA; public int teamB; public int winner = -1; }

[Serializable]
public class BracketSaveData
{
    public int playerTeamIndex;
    public bool tournamentStarted;
    public BracketRound currentRound;
    public List<int> seeding;
}

public class BracketManager : MonoBehaviour
{
    public static BracketManager Instance { get; private set; }

    public TeamRoster roster;
    public int playerTeamIndex { get; private set; }
    public bool TournamentStarted { get; private set; }
    public BracketRound currentBracketRound { get; private set; }
    public List<Matchup> currentRoundMatchups { get; private set; } = new List<Matchup>();
    public List<int> seeding { get; private set; } = new List<int>(); // current round's teams in bracket order

    public event Action OnBracketAdvanced;
    public event Action<int> OnTournamentWon;
    public event Action OnPlayerKnockedOut;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        TryLoadProgress();
    }

    public void StartTournament(int chosenIndex)
    {
        playerTeamIndex = chosenIndex;
        TournamentStarted = true;
        currentBracketRound = BracketRound.Round16;

        List<int> all = new List<int>();
        for (int i = 0; i < roster.teamNames.Length; i++) all.Add(i);
        all.Remove(chosenIndex);
        Shuffle(all);
        all.Insert(0, chosenIndex);

        seeding = all;
        BuildMatchups(all);
        SaveProgress();
    }

    void BuildMatchups(List<int> teams)
    {
        currentRoundMatchups.Clear();
        for (int i = 0; i < teams.Count; i += 2)
            currentRoundMatchups.Add(new Matchup { teamA = teams[i], teamB = teams[i + 1] });
    }

    void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public Matchup GetPlayerMatchup() =>
        currentRoundMatchups.Find(m => m.teamA == playerTeamIndex || m.teamB == playerTeamIndex);

    public int GetOpponentForPlayer()
    {
        var m = GetPlayerMatchup();
        if (m == null) return -1;
        return m.teamA == playerTeamIndex ? m.teamB : m.teamA;
    }

    // Call once, right after gameover resolves (win or lose)
    public void ReportPlayerMatchResult(bool playerWon)
    {
        var m = GetPlayerMatchup();
        m.winner = playerWon ? playerTeamIndex : GetOpponentForPlayer();

        if (!playerWon)
        {
            currentBracketRound = BracketRound.Complete;
            ClearProgress();
            OnPlayerKnockedOut?.Invoke();
            return;
        }

        SimulateNonPlayerMatchups();
        AdvanceRound();
    }

    void SimulateNonPlayerMatchups()
    {
        foreach (var m in currentRoundMatchups)
        {
            if (m.teamA == playerTeamIndex || m.teamB == playerTeamIndex) continue;
            if (m.winner != -1) continue;
            m.winner = UnityEngine.Random.value < 0.5f ? m.teamA : m.teamB;
        }
    }

    void AdvanceRound()
    {
        List<int> winners = currentRoundMatchups.ConvertAll(m => m.winner);

        if (winners.Count == 1)
        {
            currentBracketRound = BracketRound.Complete;
            ClearProgress();
            OnTournamentWon?.Invoke(winners[0]);
            return;
        }

        currentBracketRound = currentBracketRound switch
        {
            BracketRound.Round16 => BracketRound.Round8,
            BracketRound.Round8 => BracketRound.Round4,
            BracketRound.Round4 => BracketRound.Final,
            _ => BracketRound.Complete
        };

        seeding = winners;
        BuildMatchups(winners);
        SaveProgress();
        OnBracketAdvanced?.Invoke();
    }

    #region Saveprogress
    const string SaveKey = "BracketSave";

    void SaveProgress()
    {
        var data = new BracketSaveData
        {
            playerTeamIndex = playerTeamIndex,
            tournamentStarted = TournamentStarted,
            currentRound = currentBracketRound,
            seeding = seeding
        };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public bool TryLoadProgress()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return false;

        var data = JsonUtility.FromJson<BracketSaveData>(PlayerPrefs.GetString(SaveKey));
        if (!data.tournamentStarted) return false;

        playerTeamIndex = data.playerTeamIndex;
        TournamentStarted = data.tournamentStarted;
        currentBracketRound = data.currentRound;
        seeding = data.seeding;
        BuildMatchups(seeding);
        return true;
    }

    public void ClearProgress()
    {
        PlayerPrefs.DeleteKey(SaveKey);
    }

    #endregion

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.D))
        {
            PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}