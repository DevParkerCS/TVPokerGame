using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    #endregion
    #region Serialized Fields
    [SerializeField] public List<PlayerManager> PlayerSeats;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Sprite testSprite;
    [SerializeField] private GameObject foldBtn;
    [SerializeField] private GameObject checkBtn;
    [SerializeField] private GameObject betBtn;
    [SerializeField] private GameObject callBtn;
    [SerializeField] private GameObject raiseBtn;
    [SerializeField] private AvatarLibrary avatarLib;
    [SerializeField] private PotManager potManager;
    [SerializeField] private ShowdownManager showdownManager;
    #endregion

    private void Awake()
    {
        cardManager = GetComponent<CardManager>();
        PlayersData = new();
        Players = new(PlayersData.Count);

        for (int i = 0; i < PlayerSeats.Count; i++)
        {
            PlayerSeats[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < 10; i++)
        {
            Player player = new Player(10000, $"John{i}", "green_mus");
            PlayersData.Add(player);
        }
    }

    public void PlayerBet(int amount)
    {
        PlayerManager pm = Players[curToAct];
        Player player = pm.Player;

        BetManager.ApplyBet(player, amount);          // updates lastBetAmt
        lastToAct = curToAct;                         // opener becomes closer
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

    // TODO: Implement player folding
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

    private void SetActionsForPlayer(Player player)
    {
        bool noBetYet = BetManager.lastBetAmt == 0;
        bool playerIsAllIn = player.ChipBalance == 0;
        bool playerMatchedBet = player.TotalBet == BetManager.lastBetAmt;
        Debug.Log($"No Bet {noBetYet}, All In {playerIsAllIn}, Matched Bet {playerMatchedBet}");

        betBtn.SetActive(noBetYet && !playerIsAllIn);
        raiseBtn.SetActive(!noBetYet && !playerIsAllIn);
        callBtn.SetActive(!noBetYet && !playerMatchedBet && !playerIsAllIn);
        checkBtn.SetActive(noBetYet || playerMatchedBet);

        // You’ll still want a Fold button visible at all times
    }

    public void StartGame()
    {
        startGameBtn.gameObject.SetActive(false);
        Util.Shuffle(PlayersData);

        for (int i = 0; i < PlayersData.Count; i++)
        {
            PlayerSeats[i].gameObject.SetActive(true);
            PlayerSeats[i].Player = PlayersData[i];
            PlayerSeats[i].InitializePlayer();
            Players.Add(PlayerSeats[i]);
        }

        dealerIndex = Random.Range(0, Players.Count);
        Players[dealerIndex].ToggleButton();

        // Display info that game is starting

        StartCoroutine(StartRound());
        StartCoroutine(StartBlinds());
    }

    // TODO: Fix this
    private void EndRound()
    {
        potManager.AddAllBetsToPot(Players);
        SortedDictionary<int, List<PlayerManager>> winners = showdownManager.HandleShowdown(Players, cardManager);
        Dictionary<string, int> payouts = potManager.GetWinnersPayout(winners);

        DisplayPayouts(winners, payouts);

        Players[dealerIndex].ToggleButton();
        dealerIndex = (dealerIndex + 1) % Players.Count;
        Players[dealerIndex].ToggleButton();
        cardManager.ResetCards();
        BetManager.ResetBets();
        // Display something to notify of next round Maybe shuffle.
       
        for(int i = 0; i < Players.Count; i++)
        {
            Players[i].ResetAllVisual();
            Players[i].Player.ResetForNewHand();
        }

        StartCoroutine(StartRound());
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

        switch (curStreet)
        {
            case 1: cardManager.DealFlop(); break;
            case 2: cardManager.DealTurn(); break;
            case 3: cardManager.DealRiver(); break;
        }


        curToAct = FindNextActivePlayer(smallBlindIndex - 1);
        if (curToAct == -1 || ActivePlayerCount() == 1)
        {
            EndRound();
            return;
        }

        lastToAct = curToAct;

        SetActionsForPlayer(Players[curToAct].Player);
        Players[curToAct].ToggleTurn(true);
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
        SetActionsForPlayer(Players[curToAct].Player);
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
        return -1;   // everyone else folded or broke
    }

    // Helper: quick count of players still contesting the pot
    private int ActivePlayerCount()
    {
        int n = 0;
        foreach (var pm in Players)
            if (!pm.Player.HasFolded && pm.Player.ChipBalance > 0)
                n++;
        return n;
    }

    private IEnumerator StartRound()
    {
        curStreet = 0;
        yield return StartCoroutine(cardManager.DealCards());
        smallBlindIndex = (dealerIndex + 1) % Players.Count;
        bigBlindIndex = (dealerIndex + 2) % Players.Count;
        curToAct = (dealerIndex + 3) % Players.Count;
        lastToAct = curToAct;

        BetManager.PaySmallBlind(Players[smallBlindIndex].Player);
        BetManager.PayBigBlind(Players[bigBlindIndex].Player);
        Players[smallBlindIndex].DisplayBet(BetManager.GetSmallBlind(), BetManager.BetType.Blind);
        Players[bigBlindIndex].DisplayBet(BetManager.GetBigBlind(), BetManager.BetType.Blind);

        Players[curToAct].ToggleTurn(true);

        SetActionsForPlayer(Players[curToAct].Player);
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
