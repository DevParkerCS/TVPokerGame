using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region Variables
    public int dealerIndex;
    private CardManager cardManager;
    public List<PlayerManager> Players;
    public int roundMinutes = 5;
    private List<Player> PlayersData;
    private int smallBlindIndex = 0;
    private int bigBlindIndex = 0;
    private int curToAct = 0;
    private int lastToAct = 0;
    private int curStreet = 0;
    private int handId = 0;
    private bool isGameStarted = false;
    private bool isEndingRound = false;
    private bool isGameOver = false;
    #endregion
    #region Serialized Fields
    [SerializeField] public List<PlayerManager> PlayerSeats;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Sprite testSprite;
    [SerializeField] private AvatarLibrary avatarLib;
    [SerializeField] private PotManager potManager;
    [SerializeField] private ShowdownManager showdownManager;
    [SerializeField] private SocketManager socketManager;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool useTestPlayers = false;
    [SerializeField] private float handEndDelaySeconds = 5f;
    #endregion

    private void Awake()
    {
        cardManager = GetComponent<CardManager>();
        if (socketManager == null)
            socketManager = FindObjectOfType<SocketManager>();

        PlayersData = new();
        Players = new(PlayerSeats.Count);

        for (int i = 0; i < PlayerSeats.Count; i++)
        {
            PlayerSeats[i].gameObject.SetActive(false);
        }

        if (useTestPlayers)
        {
            for (int i = 0; i < 10; i++)
            {
                Player player = new Player(10000, $"John{i}", "green_mus");
                PlayersData.Add(player);
            }
        }

        UpdateWaitingForPlayersStatus();
    }

    public void AddRemotePlayer(RemotePlayerPayload payload)
    {
        if (isGameStarted)
        {
            Debug.LogWarning("Player joined after the hand started. Add late-join seating later.");
            return;
        }

        if (PlayersData.Any(p => p.ID == payload.playerId))
        {
            UpdateWaitingForPlayersStatus();
            return;
        }

        if (PlayersData.Count >= PlayerSeats.Count)
        {
            Debug.LogWarning("Table is full.");
            UpdateWaitingForPlayersStatus();
            return;
        }

        int startingBalance = payload.balance > 0 ? payload.balance : 10000;
        string spriteCode = string.IsNullOrWhiteSpace(payload.spriteCode) ? "green_mus" : payload.spriteCode;
        Player player = new Player(payload.playerId, startingBalance, payload.name, spriteCode);
        PlayersData.Add(player);

        Debug.Log($"Remote player joined: {payload.name} ({payload.playerId})");
        UpdateWaitingForPlayersStatus();
    }

    public void RemoveRemotePlayer(string playerId)
    {
        if (isGameStarted)
        {
            Debug.LogWarning($"Ignoring leave from {playerId}; game already started.");
            return;
        }

        int removedCount = PlayersData.RemoveAll(p => p.ID == playerId);
        if (removedCount > 0)
            Debug.Log($"Remote player left: {playerId}");

        UpdateWaitingForPlayersStatus();
    }

    public void HandleRemotePlayerAction(string playerId, string action, int amount)
    {
        if (!isGameStarted || isGameOver || isEndingRound || Players.Count == 0 || curToAct < 0 || curToAct >= Players.Count)
            return;

        PlayerManager current = Players[curToAct];
        if (current.Player.ID != playerId)
        {
            Debug.LogWarning($"Ignoring action from {playerId}; current turn is {current.Player.ID}");
            return;
        }

        switch (action)
        {
            case "fold": PlayerFold(); break;
            case "check": PlayerCheck(); break;
            case "call": PlayerCall(); break;
            case "bet": PlayerBet(amount); break;
            case "raise": PlayerRaise(amount); break;
        }
    }

    public void PlayerBet(int amount)
    {
        PlayerManager pm = Players[curToAct];
        Player player = pm.Player;

        if (!CanBetOrRaise(player, amount))
            return;

        BetManager.BetResult result = BetManager.ApplyBet(player, amount);
        if (result.AmountAdded <= 0)
            return;

        if (result.IsFullRaise)
            lastToAct = curToAct;

        pm.DisplayBet(player.CurBet, result.IsAllIn ? BetManager.BetType.AllIn : BetManager.BetType.Bet);

        MoveToNextPlayer();
    }

    public void PlayerRaise(int amount)
    {
        var pm = Players[curToAct];
        var player = pm.Player;

        if (!CanBetOrRaise(player, amount))
            return;

        BetManager.BetResult result = BetManager.ApplyBet(player, amount);
        if (result.AmountAdded <= 0)
            return;

        if (result.IsFullRaise)
            lastToAct = curToAct;

        pm.DisplayBet(player.CurBet, result.IsAllIn ? BetManager.BetType.AllIn : BetManager.BetType.Raise);

        MoveToNextPlayer();
    }

    public void PlayerCall()
    {
        var pm = Players[curToAct];
        var player = pm.Player;

        if (BetManager.lastBetAmt <= player.CurBet)
        {
            PlayerCheck();
            return;
        }

        BetManager.BetResult result = BetManager.ApplyCall(player);
        if (result.AmountAdded <= 0)
            return;

        pm.DisplayBet(player.CurBet, result.IsAllIn ? BetManager.BetType.AllIn : BetManager.BetType.Call);

        MoveToNextPlayer();
    }

    public void PlayerCheck()
    {
        if (BetManager.lastBetAmt > Players[curToAct].Player.CurBet)
            return;

        Players[curToAct].DisplayCheck();
        MoveToNextPlayer();
    }

    public void PlayerFold()
    {
        PlayerManager pm = Players[curToAct];
        Player player = pm.Player;

        player.Fold();
        pm.DisplayFold();

        if (player.TotalBet > 0)
            potManager.AddBetToPot(pm);

        MoveToNextPlayer();
    }

    public void StartGame()
    {
        if (PlayersData.Count < 2)
        {
            Debug.LogWarning("Need at least 2 players to start.");
            SetStatusText($"Need at least 2 players — {BuildPlayerCountText()}");
            return;
        }

        startGameBtn.gameObject.SetActive(false);
        isGameStarted = true;
        isGameOver = false;
        socketManager?.SendGameStarted();
        SetStatusText(string.Empty);
        Util.Shuffle(PlayersData);
        Players.Clear();

        for (int i = 0; i < PlayersData.Count && i < PlayerSeats.Count; i++)
        {
            PlayerSeats[i].gameObject.SetActive(true);
            PlayerSeats[i].Player = PlayersData[i];
            PlayerSeats[i].InitializePlayer();
            if (CanPlayNextHand(PlayerSeats[i].Player))
                Players.Add(PlayerSeats[i]);
        }

        if (Players.Count < 2)
        {
            SetStatusText("Need at least 2 players with chips to start.");
            isGameStarted = false;
            return;
        }

        dealerIndex = UnityEngine.Random.Range(0, Players.Count);
        Players[dealerIndex].ToggleButton();

        StartCoroutine(StartRound());
        StartCoroutine(StartBlinds());
    }

    private void EndRound()
    {
        if (isEndingRound || isGameOver)
            return;

        isEndingRound = true;
        ClearTurnIndicators();
        potManager.AddAllBetsToPot(Players);

        List<PlayerManager> remainingPlayers = Players
            .Where(pm => !pm.Player.HasFolded && pm.Player.Cards.Count == 2)
            .ToList();

        SortedDictionary<int, List<PlayerManager>> winners = new();
        if (remainingPlayers.Count == 1)
        {
            winners[0] = remainingPlayers;
        }
        else
        {
            winners = showdownManager.HandleShowdown(remainingPlayers, cardManager);
        }

        Dictionary<string, int> payouts = potManager.GetWinnersPayout(winners);
        DisplayPayouts(winners, payouts);
        string handEndedMessage = BuildHandEndedMessage(payouts);

        PlayerManager gameWinner = GetGameWinnerIfComplete();
        if (gameWinner != null)
        {
            EndGame(gameWinner);
            return;
        }

        SetStatusText(handEndedMessage);
        SendHandEndedToPhones(handEndedMessage);
        StartCoroutine(ResetAndStartNextHandAfterDelay());
    }

    private void EndGame(PlayerManager winner)
    {
        isGameOver = true;
        isGameStarted = false;
        isEndingRound = false;
        ClearTurnIndicators();

        string gameEndedMessage = winner != null ? $"{winner.Player.PlayerName} wins the game!" : "Game over";
        SetStatusText(gameEndedMessage);
        SendHandEndedToPhones(gameEndedMessage);
        Debug.Log(gameEndedMessage);
    }

    private IEnumerator ResetAndStartNextHandAfterDelay()
    {
        yield return new WaitForSeconds(handEndDelaySeconds);

        if (isGameOver)
            yield break;

        SendHandResetToPhones("Waiting for new hand");
        ResetForNextHand();
        if (isGameOver)
            yield break;

        isEndingRound = false;
        SetStatusText(string.Empty);
        StartCoroutine(StartRound());
    }

    private void ClearTurnIndicators()
    {
        foreach (PlayerManager pm in Players)
        {
            pm.ToggleTurn(false);
        }
    }

    private void SendHandEndedToPhones(string message)
    {
        SendHandLifecycleToPhones("hand-ended", message);
    }

    private void SendHandResetToPhones(string message)
    {
        SendHandLifecycleToPhones("hand-reset", message);
    }

    private void SendHandStartedToPhones(string message)
    {
        SendHandLifecycleToPhones("hand-started", message);
    }

    private void SendHandLifecycleToPhones(string eventName, string message)
    {
        if (socketManager == null)
            return;

        socketManager.SendHandLifecycleToPhones(eventName, handId, message);
    }

    private string BuildHandEndedMessage(Dictionary<string, int> payouts)
    {
        List<string> winnerSummaries = new();

        foreach (var payout in payouts)
        {
            if (payout.Value <= 0)
                continue;

            PlayerManager winner = Players.FirstOrDefault(pm => pm.Player.ID == payout.Key);
            string winnerName = winner != null ? winner.Player.PlayerName : payout.Key;
            winnerSummaries.Add($"{winnerName} wins ${payout.Value}");
        }

        if (winnerSummaries.Count == 0)
            return "Hand complete";

        if (winnerSummaries.Count == 1)
            return winnerSummaries[0];

        return $"Split pot: {string.Join(", ", winnerSummaries)}";
    }

    private void ResetForNextHand()
    {
        string previousDealerPlayerId = GetCurrentDealerPlayerId();
        if (Players.Count > 0 && dealerIndex >= 0 && dealerIndex < Players.Count)
            Players[dealerIndex].ToggleButton();

        cardManager.ResetCards();
        BetManager.ResetBets();

        foreach (PlayerManager seat in PlayerSeats)
        {
            if (!seat.gameObject.activeSelf || seat.Player == null)
                continue;

            seat.ResetAllVisual();
            seat.Player.ResetForNewHand();
            seat.UpdatePlayerBalance();

            if (!CanPlayNextHand(seat.Player))
                seat.DisplayEliminated();
        }

        RefreshActivePlayersForNextHand();

        if (Players.Count < 2)
        {
            EndGame(Players.Count == 1 ? Players[0] : null);
            return;
        }

        int previousDealerIndex = Players.FindIndex(pm => pm.Player.ID == previousDealerPlayerId);
        dealerIndex = previousDealerIndex >= 0 ? (previousDealerIndex + 1) % Players.Count : dealerIndex % Players.Count;
        Players[dealerIndex].ToggleButton();
    }

    private void RefreshActivePlayersForNextHand()
    {
        Players = PlayerSeats
            .Where(seat => seat.gameObject.activeSelf && seat.Player != null && CanPlayNextHand(seat.Player))
            .ToList();
    }

    private bool CanPlayNextHand(Player player)
    {
        return player != null && player.ChipBalance > 0;
    }

    private string GetCurrentDealerPlayerId()
    {
        if (Players.Count == 0 || dealerIndex < 0 || dealerIndex >= Players.Count)
            return string.Empty;

        return Players[dealerIndex].Player.ID;
    }

    private void StartNextStreet()
    {
        curStreet++;
        potManager.AddAllBetsToPot(Players);

        BetManager.ResetBets();

        foreach (var pm in Players)
        {
            pm.ResetForStreet();
            pm.Player.ResetForNewStreet();
        }

        DealCurrentStreet();
        SetStatusText(string.Empty);

        if (ActivePlayerCount() == 1)
        {
            EndRound();
            return;
        }

        if (ActionablePlayerCount() <= 1)
        {
            RunOutBoardAndEndRound();
            return;
        }

        curToAct = FindNextActivePlayer(smallBlindIndex - 1);
        if (curToAct == -1)
        {
            RunOutBoardAndEndRound();
            return;
        }

        lastToAct = curToAct;
        Players[curToAct].ToggleTurn(true);
    }

    private void DealCurrentStreet()
    {
        switch (curStreet)
        {
            case 1: cardManager.DealFlop(); break;
            case 2: cardManager.DealTurn(); break;
            case 3: cardManager.DealRiver(); break;
        }
    }

    private void RunOutBoardAndEndRound()
    {
        while (curStreet < 3)
        {
            curStreet++;
            DealCurrentStreet();
        }
        EndRound();
    }

    private void MoveToNextPlayer()
    {
        Players[curToAct].ToggleTurn(false);

        if (ActivePlayerCount() <= 1)
        {
            EndRound();
            return;
        }

        if (ActionablePlayerCount() == 0 || ShouldRunOutBecauseNoMoreBettingIsPossible())
        {
            RunOutBoardAndEndRound();
            return;
        }

        int nextIndex = FindNextActivePlayer(curToAct);
        bool streetComplete = IsStreetComplete(nextIndex);

        if (streetComplete)
        {
            AdvanceStreetOrEndRound();
            return;
        }

        if (nextIndex == -1)
        {
            RunOutBoardAndEndRound();
            return;
        }

        curToAct = nextIndex;
        Players[curToAct].ToggleTurn(true);
    }

    private void AdvanceStreetOrEndRound()
    {
        if (curStreet < 3 && ActivePlayerCount() > 1)
        {
            if (ActionablePlayerCount() <= 1)
                RunOutBoardAndEndRound();
            else
                StartNextStreet();
        }
        else
        {
            EndRound();
        }
    }

    private bool IsStreetComplete(int nextIndex)
    {
        if (ActivePlayerCount() <= 1)
            return true;

        if (ActionablePlayerCount() == 0)
            return true;

        if (nextIndex == -1)
            return true;

        return nextIndex == lastToAct && AllActionablePlayersHaveMatchedCurrentBet();
    }

    private bool ShouldRunOutBecauseNoMoreBettingIsPossible()
    {
        return ActionablePlayerCount() <= 1 && AllActionablePlayersHaveMatchedCurrentBet();
    }

    private bool AllActionablePlayersHaveMatchedCurrentBet()
    {
        foreach (PlayerManager pm in Players)
        {
            Player player = pm.Player;
            if (!player.HasFolded && player.ChipBalance > 0 && player.CurBet < BetManager.lastBetAmt)
                return false;
        }

        return true;
    }

    private PlayerManager GetGameWinnerIfComplete()
    {
        List<PlayerManager> playersWithChips = PlayerSeats
            .Where(seat => seat.gameObject.activeSelf && seat.Player != null && seat.Player.ChipBalance > 0)
            .ToList();

        return playersWithChips.Count == 1 ? playersWithChips[0] : null;
    }

    private int FindNextActivePlayer(int startIndex)
    {
        int total = Players.Count;
        for (int offset = 1; offset < total; offset++)
        {
            int idx = (startIndex + offset) % total;
            var p = Players[idx].Player;
            if (!p.HasFolded && p.ChipBalance > 0)
                return idx;
        }
        return -1;
    }

    private int ActivePlayerCount()
    {
        int n = 0;
        foreach (var pm in Players)
            if (!pm.Player.HasFolded)
                n++;
        return n;
    }

    private int ActionablePlayerCount()
    {
        int n = 0;
        foreach (var pm in Players)
            if (!pm.Player.HasFolded && pm.Player.ChipBalance > 0)
                n++;
        return n;
    }

    private bool CanBetOrRaise(Player player, int requestedTotalBet)
    {
        if (player.HasFolded || player.ChipBalance <= 0)
            return false;

        int maxTotalBet = player.CurBet + player.ChipBalance;
        int targetTotalBet = Math.Min(requestedTotalBet, maxTotalBet);

        if (targetTotalBet <= player.CurBet)
            return false;

        if (targetTotalBet <= BetManager.lastBetAmt)
            return false;

        int minimumRaiseTo = BetManager.GetMinimumRaiseTo();
        bool isAllIn = targetTotalBet == maxTotalBet;

        return targetTotalBet >= minimumRaiseTo || isAllIn;
    }

    private IEnumerator StartRound()
    {
        if (isGameOver)
            yield break;

        if (Players.Count < 2)
        {
            EndGame(Players.Count == 1 ? Players[0] : null);
            yield break;
        }

        handId++;
        curStreet = 0;
        SendHandStartedToPhones("New hand started");
        yield return StartCoroutine(cardManager.DealCards());
        SendHoleCardsToPhones();
        smallBlindIndex = (dealerIndex + 1) % Players.Count;
        bigBlindIndex = (dealerIndex + 2) % Players.Count;
        curToAct = (dealerIndex + 3) % Players.Count;
        lastToAct = curToAct;

        BetManager.BetResult smallBlindResult = BetManager.PaySmallBlind(Players[smallBlindIndex].Player);
        BetManager.BetResult bigBlindResult = BetManager.PayBigBlind(Players[bigBlindIndex].Player);
        Players[smallBlindIndex].DisplayBet(Players[smallBlindIndex].Player.CurBet, smallBlindResult.IsAllIn ? BetManager.BetType.AllIn : BetManager.BetType.Blind);
        Players[bigBlindIndex].DisplayBet(Players[bigBlindIndex].Player.CurBet, bigBlindResult.IsAllIn ? BetManager.BetType.AllIn : BetManager.BetType.Blind);
        SetStatusText(string.Empty);

        if (ActivePlayerCount() == 1)
        {
            EndRound();
            yield break;
        }

        if (ActionablePlayerCount() == 0 || ShouldRunOutBecauseNoMoreBettingIsPossible())
        {
            RunOutBoardAndEndRound();
            yield break;
        }

        curToAct = FindNextActivePlayer(bigBlindIndex);
        lastToAct = curToAct;
        Players[curToAct].ToggleTurn(true);
    }

    private void SendHoleCardsToPhones()
    {
        if (socketManager == null)
            return;

        foreach (PlayerManager pm in Players)
        {
            if (pm.Player.Cards.Count == 2)
            {
                socketManager.SendHoleCardsToPhone(pm.Player.ID, pm.Player.Cards.ToList());
            }
        }
    }

    private IEnumerator StartBlinds()
    {
        BetManager.GenerateBlinds();
        for(int i = 0; i < roundMinutes; i++)
        {
            yield return new WaitForSeconds(roundMinutes * 60f);
            BetManager.IncreaseBlind();
        }
    }

    private void DisplayPayouts(SortedDictionary<int, List<PlayerManager>> winners, Dictionary<string, int> payouts)
    {
        foreach (var entry in winners)
        {
            foreach (PlayerManager pm in entry.Value)
            {
                pm.UpdatePlayerBalance();
            }
        }
    }

    private void UpdateWaitingForPlayersStatus()
    {
        SetStatusText(BuildPlayerCountText());
    }

    private string BuildPlayerCountText()
    {
        return $"{PlayersData.Count}/{MaxPlayerCount()} players joined";
    }

    private int MaxPlayerCount()
    {
        return PlayerSeats != null && PlayerSeats.Count > 0 ? PlayerSeats.Count : 10;
    }

    private void SetStatusText(string value)
    {
        if (statusText != null)
            statusText.text = value;
    }
}