using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private bool isGameStarted = false;
    #endregion
    #region Serialized Fields
    [SerializeField] public List<PlayerManager> PlayerSeats;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Sprite testSprite;
    [SerializeField] private AvatarLibrary avatarLib;
    [SerializeField] private PotManager potManager;
    [SerializeField] private ShowdownManager showdownManager;
    [SerializeField] private SocketManager socketManager;
    [SerializeField] private bool useTestPlayers = false;
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
    }

    public void AddRemotePlayer(RemotePlayerPayload payload)
    {
        if (isGameStarted)
        {
            Debug.LogWarning("Player joined after the hand started. Add late-join seating later.");
            return;
        }

        if (PlayersData.Any(p => p.ID == payload.playerId))
            return;

        if (PlayersData.Count >= PlayerSeats.Count)
        {
            Debug.LogWarning("Table is full.");
            return;
        }

        int startingBalance = payload.balance > 0 ? payload.balance : 10000;
        string spriteCode = string.IsNullOrWhiteSpace(payload.spriteCode) ? "green_mus" : payload.spriteCode;
        Player player = new Player(payload.playerId, startingBalance, payload.name, spriteCode);
        PlayersData.Add(player);

        Debug.Log($"Remote player joined: {payload.name} ({payload.playerId})");
    }

    public void HandleRemotePlayerAction(string playerId, string action, int amount)
    {
        if (!isGameStarted || Players.Count == 0 || curToAct < 0 || curToAct >= Players.Count)
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

        BetManager.ApplyBet(player, amount);
        lastToAct = curToAct;
        pm.DisplayBet(amount, BetManager.BetType.Bet);

        MoveToNextPlayer();
    }

    public void PlayerRaise(int amount)
    {
        var pm = Players[curToAct];
        var player = pm.Player;

        BetManager.ApplyBet(player, amount);
        lastToAct = curToAct;
        pm.DisplayBet(amount, BetManager.BetType.Raise);

        MoveToNextPlayer();
    }

    public void PlayerCall()
    {
        var pm = Players[curToAct];
        var player = pm.Player;

        BetManager.ApplyCall(player);
        pm.DisplayBet(BetManager.lastBetAmt, BetManager.BetType.Call);

        MoveToNextPlayer();
    }

    public void PlayerCheck()
    {
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
            return;
        }

        startGameBtn.gameObject.SetActive(false);
        isGameStarted = true;
        Util.Shuffle(PlayersData);
        Players.Clear();

        for (int i = 0; i < PlayersData.Count && i < PlayerSeats.Count; i++)
        {
            PlayerSeats[i].gameObject.SetActive(true);
            PlayerSeats[i].Player = PlayersData[i];
            PlayerSeats[i].InitializePlayer();
            Players.Add(PlayerSeats[i]);
        }

        dealerIndex = Random.Range(0, Players.Count);
        Players[dealerIndex].ToggleButton();

        StartCoroutine(StartRound());
        StartCoroutine(StartBlinds());
    }

    private void EndRound()
    {
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
        ResetForNextHand();
        StartCoroutine(StartRound());
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

        curToAct = FindNextActivePlayer(smallBlindIndex - 1);
        if (curToAct == -1)
        {
            if (ActivePlayerCount() > 1)
                RunOutBoardAndEndRound();
            else
                EndRound();
            return;
        }

        if (ActivePlayerCount() == 1)
        {
            EndRound();
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

        int nextIndex = FindNextActivePlayer(curToAct);
        bool streetComplete = nextIndex == -1 || nextIndex == lastToAct;

        if (streetComplete)
        {
            if (curStreet < 3 && ActivePlayerCount() > 1)
            {
                StartNextStreet();
            }
            else
            {
                EndRound();
            }
            return;
        }

        curToAct = nextIndex;
        Players[curToAct].ToggleTurn(true);
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

    private IEnumerator StartRound()
    {
        curStreet = 0;
        yield return StartCoroutine(cardManager.DealCards());
        SendHoleCardsToPhones();
        smallBlindIndex = (dealerIndex + 1) % Players.Count;
        bigBlindIndex = (dealerIndex + 2) % Players.Count;
        curToAct = (dealerIndex + 3) % Players.Count;
        lastToAct = curToAct;

        BetManager.PaySmallBlind(Players[smallBlindIndex].Player);
        BetManager.PayBigBlind(Players[bigBlindIndex].Player);
        Players[smallBlindIndex].DisplayBet(BetManager.GetSmallBlind(), BetManager.BetType.Blind);
        Players[bigBlindIndex].DisplayBet(BetManager.GetBigBlind(), BetManager.BetType.Blind);

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
}
