using System;
using System.Linq;
using UnityEngine;

public class PhoneTurnStateReporter : MonoBehaviour
{
    private SocketManager socketManager;
    private GameManager gameManager;
    private PotManager potManager;
    private float nextSendAt;
    private string lastSignature = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateReporter()
    {
        GameObject existing = GameObject.Find(nameof(PhoneTurnStateReporter));
        if (existing != null)
            return;

        GameObject reporter = new GameObject(nameof(PhoneTurnStateReporter));
        reporter.AddComponent<PhoneTurnStateReporter>();
        DontDestroyOnLoad(reporter);
    }

    private void Update()
    {
        if (Time.time < nextSendAt)
            return;

        nextSendAt = Time.time + 0.25f;
        EnsureReferences();
        SendTurnStatesIfChanged();
    }

    public void ForceSendTurnStates()
    {
        lastSignature = string.Empty;
        EnsureReferences();
        SendTurnStatesIfChanged();
    }

    private void EnsureReferences()
    {
        if (socketManager == null)
            socketManager = FindObjectOfType<SocketManager>();

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (potManager == null)
            potManager = FindObjectOfType<PotManager>();
    }

    private void SendTurnStatesIfChanged()
    {
        if (socketManager == null || gameManager == null || gameManager.Players == null || gameManager.Players.Count == 0)
            return;

        PlayerManager current = gameManager.Players.FirstOrDefault(pm => pm != null && pm.Player != null && pm.IsTurn);
        if (current == null)
            return;

        int potAmount = potManager != null && potManager.pot != null ? potManager.pot.TotalAmt : 0;
        string signature = BuildSignature(current, potAmount);

        if (signature == lastSignature)
            return;

        lastSignature = signature;
        SendStates(current, potAmount);
    }

    private string BuildSignature(PlayerManager current, int potAmount)
    {
        string playerState = string.Join(",", gameManager.Players.Select(pm =>
            $"{pm.Player.ID}:{pm.Player.ChipBalance}:{pm.Player.CurBet}:{pm.Player.HasFolded}"));

        return $"{current.Player.ID}|{BetManager.lastBetAmt}|{potAmount}|{playerState}";
    }

    private void SendStates(PlayerManager current, int potAmount)
    {
        int minRaiseTo = SafeGetMinimumRaiseTo();

        foreach (PlayerManager pm in gameManager.Players)
        {
            Player player = pm.Player;
            bool isTurn = player.ID == current.Player.ID;
            bool noBetYet = BetManager.lastBetAmt == 0;
            bool playerIsAllIn = player.ChipBalance == 0;
            int maxTotalBet = player.CurBet + player.ChipBalance;
            int rawAmountToCall = Math.Max(0, BetManager.lastBetAmt - player.CurBet);
            int amountToCall = Math.Min(rawAmountToCall, player.ChipBalance);
            bool callWouldPutPlayerAllIn = rawAmountToCall >= player.ChipBalance;
            bool hasChipsForMinRaise = maxTotalBet >= minRaiseTo;

            socketManager.SendTurnStateToPhone(new PhoneTurnStatePayload
            {
                playerId = player.ID,
                isPlayerTurn = isTurn,
                currentPlayerId = current.Player.ID,
                currentPlayerName = current.Player.PlayerName,
                balance = player.ChipBalance,
                pot = potAmount,
                currentBet = BetManager.lastBetAmt,
                playerBet = player.CurBet,
                amountToCall = amountToCall,
                minRaiseTo = minRaiseTo,
                canFold = isTurn && !player.HasFolded,
                canCheck = isTurn && rawAmountToCall == 0,
                canCall = isTurn && rawAmountToCall > 0 && !playerIsAllIn,
                canBet = isTurn && noBetYet && !playerIsAllIn && player.ChipBalance >= minRaiseTo,
                canRaise = isTurn && !noBetYet && !playerIsAllIn && !callWouldPutPlayerAllIn && hasChipsForMinRaise
            });
        }
    }

    private int SafeGetMinimumRaiseTo()
    {
        try
        {
            return BetManager.GetMinimumRaiseTo();
        }
        catch
        {
            return 0;
        }
    }
}
