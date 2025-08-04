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
    private Pot pot;
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
            Player player = new Player(10000, $"John{i}", testSprite);
            PlayersData.Add(player);
        }

        pot = new();

    }

    public void PlayerBet(int amount)
    {
        PlayerManager pm = Players[curToAct];
        Player player = pm.Player;

        BetManager.ApplyBet(player, amount);
        pm.DisplayBet(amount, BetManager.BetType.Bet);

        MoveToNextPlayer();
    }

    public void PlayerCall()
    {
        PlayerManager pm = Players[curToAct];
        Player player = pm.Player;

        BetManager.ApplyCall(player);
        pm.DisplayBet(BetManager.lastBetAmt, BetManager.BetType.Call);

        MoveToNextPlayer();
    }

    public void PlayerRaise(int amount)
    {
        PlayerManager pm = Players[curToAct];
        Player player = pm.Player;

        BetManager.ApplyBet(player, amount);
        pm.DisplayBet(amount, BetManager.BetType.Raise);

        MoveToNextPlayer();
    }

    public void PlayerCheck()
    {
        PlayerManager pm = Players[curToAct];
        pm.DisplayCheck();

        MoveToNextPlayer();
    }

    public void PlayerFold()
    {

    }

    private void SetActionsForPlayer(Player player)
    {
        if (BetManager.lastBetAmt == 0)
        {
            callBtn.SetActive(false);
            raiseBtn.SetActive(false);
            betBtn.SetActive(true);
            raiseBtn.SetActive(true);
            checkBtn.SetActive(true);
        }
        else
        {
            callBtn.SetActive(true);
            raiseBtn.SetActive(true);
            betBtn.SetActive(false);
            raiseBtn.SetActive(true);

            if (player.TotalBet == BetManager.lastBetAmt)
                checkBtn.SetActive(true);
            else
                checkBtn.SetActive(false);
        }
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

    private void EndRound()
    {
        Players[dealerIndex].ToggleButton();
        dealerIndex = (dealerIndex + 1) % Players.Count;
        cardManager.ResetGameDeck();
        // Display something to notify of next round Maybe shuffle.

        StartRound();
    }

    private void StartNextStreet()
    {
        curStreet++;
        curToAct = smallBlindIndex;
    }

    private void MoveToNextPlayer()
    {
        Players[curToAct].ToggleTurn();

        if(curToAct != lastToAct)
        {
            curToAct = (curToAct + 1) % Players.Count;
            SetActionsForPlayer(Players[curToAct].Player);
            Players[curToAct].ToggleTurn();
        }else if(curStreet != 3)
        {
            for(int i = 0; i < Players.Count; i++)
            {
                Players[i].ResetVisuals();
            }
            StartNextStreet();
        }else
        {
            EndRound();
        }
    }

    private IEnumerator StartRound()
    {
        yield return StartCoroutine(cardManager.DealCards());
        smallBlindIndex = (dealerIndex + 1) % Players.Count;
        bigBlindIndex = (dealerIndex + 2) % Players.Count;
        curToAct = (dealerIndex + 3) % Players.Count;
        lastToAct = bigBlindIndex;

        BetManager.PaySmallBlind(Players[smallBlindIndex].Player);
        BetManager.PayBigBlind(Players[bigBlindIndex].Player);
        Players[smallBlindIndex].DisplayBet(BetManager.GetSmallBlind(), BetManager.BetType.Blind);
        Players[bigBlindIndex].DisplayBet(BetManager.GetBigBlind(), BetManager.BetType.Blind);

        Players[curToAct].ToggleTurn();

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
}
