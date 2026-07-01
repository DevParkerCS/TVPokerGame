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
    #endregion
    #region Serialized Fields
    [SerializeField] public List<PlayerManager> PlayerSeats;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Sprite testSprite;
    [SerializeField] private AvatarLibrary avatarLib;
    [SerializeField] private PotManager potManager;
    [SerializeField] private ShowdownManager showdownManager;
    [SerializeField] private SocketManager socketManager;
    [SerializeField] private TMP_Text streetText;
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

        SetStreetText("WAITING");
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

    public void HandleRemotePlayerAction(string playerId, string action, int amount)
    {
        if (!isGameStarted || isEndingRound || Players.Count == 0 || curToAct < 0 || curToAct >= Players.Count)
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
        SetStatusText(string.Empty);
        Util.Shuffle(PlayersData);
        Players.Clear();

        for (int i = 0; i < PlayersData.Count && i < PlayerSeats.Count; i++)
        {
            PlayerSeats[i].gameObject.SetActive(true);
            PlayerSeats[i].Player = PlayersData[i];
            PlayerSeats[i].InitializePlayer();
            Players.Add(PlayerSeats[i]);
        }

        dealerIndex = UnityEngine.Random.Range(0, Players.Count);
        Players[dealerIndex].ToggleButton();

        StartCoroutine(StartRound());
        StartCoroutine(StartBlinds());
    }

    private void EndRound()
    {
        if (isEndingRound)
            return;

        isEndingRound = true;
        SetStreetText("SHOWDOWN");
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
        SetStatusText(handEndedMessage);
        SendHandEndedToPhones(handEndedMessage);
        StartCoroutine(ResetAndStartNextHandAfterDelay());
    }

    private IEnumerator ResetAndStartNextHandAfterDelay()
    {
        yield return new WaitForSeconds(handEndDelaySeconds);

        ResetForNextHand();
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
        if (socketManager == null)
            return;

        socketManager.SendHandLifecycleToPhones("hand-ended", handId, message);
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
        Players[dealerIndex].ToggleButton();
        dealerIndex = (dealerIndex + 1) % Players.Count;
        Players[dealerIndex].ToggleButton();
        cardManager.ResetCards();
        BetManager.ResetBets();

        for(int i = 0; i < Players.Count; i++)
        {
            Players[i].ResetAllVisual();
            Players[i].Player.ResetForNewHand();
        }
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

        UpdateStreetText();
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
        handId++;
        curStreet = 0;
        SetStreetText("PREFLOP");
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

    private void UpdateStreetText()
    {
        switch (curStreet)
        {
            case 0: SetStreetText("PREFLOP"); break;
            case 1: SetStreetText("FLOP"); break;
            case 2: SetStreetText("TURN"); break;
            case 3: SetStreetText("RIVER"); break;
            default: SetStreetText("SHOWDOWN"); break;
        }
    }

    private void SetStreetText(string value)
    {
        if (streetText != null)
            streetText.text = value;
    }

    private void SetStatusText(string value)
    {
        if (statusText != null)
            statusText.text = value;
    }
}
