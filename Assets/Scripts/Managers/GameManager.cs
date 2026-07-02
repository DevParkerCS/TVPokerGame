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
    private const int DefaultStartingChipBalance = 10000;

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
    private bool isHandActive = false;
    private Coroutine blindTimerCoroutine;
    #endregion

    #region Serialized Fields
    [SerializeField] public List<PlayerManager> PlayerSeats;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Button restartGameBtn;
    [SerializeField] private Sprite testSprite;
    [SerializeField] private AvatarLibrary avatarLib;
    [SerializeField] private PotManager potManager;
    [SerializeField] private ShowdownManager showdownManager;
    [SerializeField] private SocketManager socketManager;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool useTestPlayers = false;
    [SerializeField] private int startingChipBalance = DefaultStartingChipBalance;
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
                Player player = new Player(startingChipBalance, $"John{i}", "green_mus");
                PlayersData.Add(player);
            }
        }

        SetRestartButtonVisible(false);
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

        int startingBalance = payload.balance > 0 ? payload.balance : startingChipBalance;
        string spriteCode = string.IsNullOrWhiteSpace(payload.spriteCode) ? "green_mus" : payload.spriteCode;
        Player player = new Player(payload.playerId, startingBalance, payload.name, spriteCode);
        PlayersData.Add(player);

        Debug.Log($"Remote player joined: {payload.name} ({payload.playerId})");
        UpdateWaitingForPlayersStatus();
    }

    public void RemoveRemotePlayer(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        PlayersData.RemoveAll(p => p.ID == playerId);

        if (!isGameStarted)
        {
            Debug.Log($"Remote player left before game start: {playerId}");
            UpdateWaitingForPlayersStatus();
            return;
        }

        PlayerManager leavingPlayer = FindSeatByPlayerId(playerId);
        if (leavingPlayer == null || leavingPlayer.Player == null)
        {
            Debug.LogWarning($"Leave requested for unknown player: {playerId}");
            RefreshActivePlayersForNextHand();
            return;
        }

        leavingPlayer.Player.MarkLeavingTable();
        Debug.Log($"Remote player leaving table: {leavingPlayer.Player.PlayerName} ({playerId})");

        if (isGameOver || isEndingRound || !isHandActive)
        {
            leavingPlayer.DisplayEliminated();
            RefreshActivePlayersForNextHand();
            SyncPlayerBalancesToServer();
            MaybeEndGameAfterLeave();
            return;
        }

        HandleLeavingPlayerDuringHand(leavingPlayer);
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

        if (current.Player.IsLeavingTable)
        {
            ForceFoldLeavingPlayer(current);
            MoveToNextPlayer();
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

    public void RestartGame()
    {
        if (PlayersData.Count < 2)
        {
            Debug.LogWarning("Need at least 2 players to restart.");
            SetStatusText($"Need at least 2 players — {BuildPlayerCountText()}");
            return;
        }

        StopAllCoroutines();
        blindTimerCoroutine = null;

        isGameStarted = true;
        isGameOver = false;
        isEndingRound = false;
        isHandActive = false;
        handId = 0;
        curStreet = 0;
        smallBlindIndex = 0;
        bigBlindIndex = 0;
        curToAct = 0;
        lastToAct = 0;

        SetRestartButtonVisible(false);
        if (startGameBtn != null)
            startGameBtn.gameObject.SetActive(false);

        ClearTurnIndicators();
        cardManager.ResetCards();
        potManager.ResetPot();
        ResetAllPlayersForNewGame();
        RefreshActivePlayersForNextHand();

        if (Players.Count < 2)
        {
            SetStatusText("Need at least 2 players with chips to restart.");
            isGameStarted = false;
            return;
        }

        dealerIndex = UnityEngine.Random.Range(0, Players.Count);
        Players[dealerIndex].ToggleButton();

        SyncPlayerBalancesToServer();
        SendHandResetToPhones("New game starting");
        socketManager?.SendGameStarted();
        SetStatusText(string.Empty);

        StartBlindTimer();
        StartCoroutine(StartRound());
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
        PlayerManager pm = Players[curToAct];
        Player player = pm.Player;

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
        PlayerManager pm = Players[curToAct];
        Player player = pm.Player;

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
        SetRestartButtonVisible(false);
        isGameStarted = true;
        isGameOver = false;
        isEndingRound = false;
        isHandActive = false;
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

        StartBlindTimer();
        StartCoroutine(StartRound());
    }

    private void EndRound()
    {
        if (isEndingRound || isGameOver)
            return;

        isEndingRound = true;
        isHandActive = false;
        ClearTurnIndicators();
        potManager.AddAllBetsToPot(Players);

        List<PlayerManager> remainingPlayers = Players
            .Where(pm => !pm.Player.HasFolded && pm.Player.Cards.Count == 2 && !pm.Player.IsLeavingTable)
            .ToList();

        SortedDictionary<int, List<PlayerManager>> winners = new();
        if (remainingPlayers.Count == 1)
            winners[0] = remainingPlayers;
        else
            winners = showdownManager.HandleShowdown(remainingPlayers, cardManager);

        Dictionary<string, int> payouts = potManager.GetWinnersPayout(winners);
        DisplayPayouts(winners, payouts);
        SyncPlayerBalancesToServer();
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
        isHandActive = false;
        ClearTurnIndicators();
        SetRestartButtonVisible(true);

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
            pm.ToggleTurn(false);
    }

    private void HandleLeavingPlayerDuringHand(PlayerManager leavingPlayer)
    {
        bool wasCurrentPlayer = IsCurrentPlayer(leavingPlayer);

        ForceFoldLeavingPlayer(leavingPlayer);
        SyncPlayerBalancesToServer();

        if (wasCurrentPlayer)
        {
            MoveToNextPlayer();
            return;
        }

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

        ForceSendTurnStatesToPhones();
    }

    private void ForceFoldLeavingPlayer(PlayerManager pm)
    {
        if (pm == null || pm.Player == null)
            return;

        Player player = pm.Player;
        player.MarkLeavingTable();

        if (!player.HasFolded)
        {
            player.Fold();
            if (player.TotalBet > 0)
                potManager.AddBetToPot(pm);
        }

        int foldedIndex = Players.IndexOf(pm);
        if (foldedIndex >= 0)
            NormalizeLastToActAfterAction(foldedIndex);

        pm.DisplayEliminated();
    }

    private bool IsCurrentPlayer(PlayerManager pm)
    {
        return pm != null &&
               pm.Player != null &&
               Players.Count > 0 &&
               curToAct >= 0 &&
               curToAct < Players.Count &&
               Players[curToAct].Player.ID == pm.Player.ID;
    }

    private void MaybeEndGameAfterLeave()
    {
        if (!isGameStarted || isGameOver)
            return;

        if (Players.Count < 2)
            EndGame(Players.Count == 1 ? Players[0] : null);
        else
            ForceSendTurnStatesToPhones();
    }

    private PlayerManager FindSeatByPlayerId(string playerId)
    {
        return PlayerSeats.FirstOrDefault(seat => seat.gameObject.activeSelf && seat.Player != null && seat.Player.ID == playerId);
    }

    private void ForceSendTurnStatesToPhones()
    {
        PhoneTurnStateReporter reporter = FindObjectOfType<PhoneTurnStateReporter>();
        reporter?.ForceSendTurnStates();
    }

    private void ResetAllPlayersForNewGame()
    {
        foreach (PlayerManager seat in PlayerSeats)
        {
            if (!seat.gameObject.activeSelf || seat.Player == null)
                continue;

            if (seat.Player.IsLeavingTable)
            {
                seat.DisplayEliminated();
                continue;
            }

            seat.Player.ResetForNewGame(startingChipBalance);
            seat.ResetAllVisual();
            seat.InitializePlayer();
            seat.UpdatePlayerBalance();
        }
    }

    private void SetRestartButtonVisible(bool visible)
    {
        if (restartGameBtn == null)
            return;

        restartGameBtn.gameObject.SetActive(visible);
        restartGameBtn.interactable = visible;
    }

    private void SyncPlayerBalancesToServer()
    {
        if (socketManager == null)
            return;

        socketManager.SendPlayerBalancesToServer(PlayerSeats);
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
        potManager.ResetPot();
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
        return player != null && player.ChipBalance > 0 && !player.IsLeavingTable;
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

        foreach (PlayerManager pm in Players)
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
        int actedIndex = curToAct;
        Players[actedIndex].ToggleTurn(false);
        NormalizeLastToActAfterAction(actedIndex);

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

    private void NormalizeLastToActAfterAction(int actedIndex)
    {
        if (actedIndex != lastToAct)
            return;

        if (IsActionablePlayerAtIndex(lastToAct))
            return;

        int nextActionable = FindNextActivePlayer(actedIndex);
        if (nextActionable != -1)
            lastToAct = nextActionable;
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

        bool allMatched = AllActionablePlayersHaveMatchedCurrentBet();
        if (!allMatched)
            return false;

        if (!IsActionablePlayerAtIndex(lastToAct))
        {
            int nextActionable = FindNextActivePlayer(lastToAct);
            if (nextActionable == -1)
                return true;

            lastToAct = nextActionable;
        }

        return nextIndex == lastToAct;
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
            if (!IsActionablePlayer(player))
                continue;

            if (player.CurBet < BetManager.lastBetAmt)
                return false;
        }

        return true;
    }

    private PlayerManager GetGameWinnerIfComplete()
    {
        List<PlayerManager> playersWithChips = PlayerSeats
            .Where(seat => seat.gameObject.activeSelf && seat.Player != null && CanPlayNextHand(seat.Player))
            .ToList();

        return playersWithChips.Count == 1 ? playersWithChips[0] : null;
    }

    private int FindNextActivePlayer(int startIndex)
    {
        int total = Players.Count;
        for (int offset = 1; offset < total; offset++)
        {
            int idx = (startIndex + offset) % total;
            Player player = Players[idx].Player;
            if (IsActionablePlayer(player))
                return idx;
        }
        return -1;
    }

    private bool IsActionablePlayerAtIndex(int index)
    {
        return index >= 0 && index < Players.Count && IsActionablePlayer(Players[index].Player);
    }

    private bool IsActionablePlayer(Player player)
    {
        return player != null && !player.HasFolded && player.ChipBalance > 0 && !player.IsLeavingTable;
    }

    private int ActivePlayerCount()
    {
        int n = 0;
        foreach (PlayerManager pm in Players)
            if (pm.Player != null && !pm.Player.HasFolded && !pm.Player.IsLeavingTable)
                n++;
        return n;
    }

    private int ActionablePlayerCount()
    {
        int n = 0;
        foreach (PlayerManager pm in Players)
            if (IsActionablePlayer(pm.Player))
                n++;
        return n;
    }

    private bool CanBetOrRaise(Player player, int requestedTotalBet)
    {
        if (!IsActionablePlayer(player))
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

        isHandActive = true;
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
            if (pm.Player.Cards.Count == 2 && !pm.Player.IsLeavingTable)
                socketManager.SendHoleCardsToPhone(pm.Player.ID, pm.Player.Cards.ToList());
        }
    }

    private void StartBlindTimer()
    {
        BetManager.GenerateBlinds();

        if (blindTimerCoroutine != null)
            StopCoroutine(blindTimerCoroutine);

        blindTimerCoroutine = StartCoroutine(AdvanceBlindsOverTime());
    }

    private IEnumerator AdvanceBlindsOverTime()
    {
        for (int i = 0; i < roundMinutes; i++)
        {
            yield return new WaitForSeconds(roundMinutes * 60f);
            BetManager.IncreaseBlind();
        }

        blindTimerCoroutine = null;
    }

    private void DisplayPayouts(SortedDictionary<int, List<PlayerManager>> winners, Dictionary<string, int> payouts)
    {
        foreach (var entry in winners)
        {
            foreach (PlayerManager pm in entry.Value)
                pm.UpdatePlayerBalance();
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