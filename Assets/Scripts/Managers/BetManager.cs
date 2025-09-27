using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public static class BetManager
{
    private static List<BlindLevel> blindLevels;
    private static int curBlindLevel = 0;
    public static int lastBetAmt = 0;
    public enum BetType {Blind, Bet, Call, Raise, AllIn};

    #region Public methods
    public static void GenerateBlinds()
    {
        blindLevels = Util.GenerateBlindLevels(10, 15000, 25, 5, 120);
        curBlindLevel = 0;
        lastBetAmt = blindLevels[curBlindLevel].BigBlind;
    }
    public static void PaySmallBlind(Player player)
    {
        player.Bet(blindLevels[curBlindLevel].SmallBlind);
    }
    public static void PayBigBlind(Player player)
    {
        player.Bet(blindLevels[curBlindLevel].BigBlind);
        lastBetAmt = blindLevels[curBlindLevel].BigBlind;
    }

    public static void ApplyBet(Player player, int amount)
    {
        player.Bet(amount);
        lastBetAmt = amount;
    }

    public static void ApplyCall(Player player)
    {
        player.Bet(lastBetAmt);
    }

    public static void IncreaseBlind()
    {
        curBlindLevel++;
    }

    public static void MoveChipsToPot(List<PlayerManager> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            
        }
    }

    public static void ResetBets()
    {
        lastBetAmt = 0;
    }

    public static int GetSmallBlind() { return blindLevels[curBlindLevel].SmallBlind; }
    public static int GetBigBlind() { return blindLevels[curBlindLevel].BigBlind; }
    #endregion
}
