using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public static class BetManager
{
    public class BetResult
    {
        public int PreviousCurrentBet;
        public int PreviousPlayerBet;
        public int NewPlayerBet;
        public int AmountAdded;
        public bool IsAllIn;
        public bool IncreasedCurrentBet;
        public bool IsFullRaise;
        public bool IsShortAllIn;
    }

    private static List<BlindLevel> blindLevels;
    private static int curBlindLevel = 0;
    public static int lastBetAmt = 0;
    private static int lastFullBetAmt = 0;
    private static int lastRaiseAmt = 0;
    public enum BetType {Blind, Bet, Call, Raise, AllIn};

    #region Public methods
    public static void GenerateBlinds()
    {
        blindLevels = Util.GenerateBlindLevels(10, 15000, 25, 5, 120);
        curBlindLevel = 0;
        lastBetAmt = 0;
        lastFullBetAmt = 0;
        lastRaiseAmt = GetBigBlind();
    }

    public static BetResult PaySmallBlind(Player player)
    {
        return ApplyBlind(player, blindLevels[curBlindLevel].SmallBlind);
    }

    public static BetResult PayBigBlind(Player player)
    {
        BetResult result = ApplyBlind(player, blindLevels[curBlindLevel].BigBlind);
        lastBetAmt = Math.Max(lastBetAmt, player.CurBet);

        if (player.CurBet >= blindLevels[curBlindLevel].BigBlind)
        {
            lastFullBetAmt = player.CurBet;
            lastRaiseAmt = blindLevels[curBlindLevel].BigBlind;
        }

        return result;
    }

    public static BetResult ApplyBet(Player player, int requestedTotalBet)
    {
        int previousCurrentBet = lastBetAmt;
        int previousPlayerBet = player.CurBet;
        int maxTotalBet = player.CurBet + player.ChipBalance;
        int targetTotalBet = Clamp(requestedTotalBet, player.CurBet, maxTotalBet);
        int minimumRaiseTo = GetMinimumRaiseTo();

        player.Bet(targetTotalBet);

        bool increasedCurrentBet = player.CurBet > previousCurrentBet;
        bool isFullRaise = increasedCurrentBet && player.CurBet >= minimumRaiseTo;

        if (increasedCurrentBet)
            lastBetAmt = player.CurBet;

        if (isFullRaise)
        {
            lastRaiseAmt = player.CurBet - previousCurrentBet;
            lastFullBetAmt = player.CurBet;
        }

        return BuildResult(player, previousCurrentBet, previousPlayerBet, increasedCurrentBet, isFullRaise);
    }

    public static BetResult ApplyCall(Player player)
    {
        int previousCurrentBet = lastBetAmt;
        int previousPlayerBet = player.CurBet;
        int maxTotalBet = player.CurBet + player.ChipBalance;
        int targetTotalBet = Math.Min(lastBetAmt, maxTotalBet);

        player.Bet(targetTotalBet);

        return BuildResult(player, previousCurrentBet, previousPlayerBet, false, false);
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
        lastFullBetAmt = 0;
        lastRaiseAmt = blindLevels != null && blindLevels.Count > curBlindLevel ? GetBigBlind() : 0;
    }

    public static int GetSmallBlind() { return blindLevels[curBlindLevel].SmallBlind; }
    public static int GetBigBlind() { return blindLevels[curBlindLevel].BigBlind; }

    public static int GetMinimumRaiseTo()
    {
        int bigBlind = GetBigBlind();

        if (lastBetAmt == 0)
            return bigBlind;

        if (lastFullBetAmt == 0)
            return bigBlind;

        return lastFullBetAmt + Math.Max(lastRaiseAmt, bigBlind);
    }
    #endregion

    private static BetResult ApplyBlind(Player player, int blindAmount)
    {
        int previousCurrentBet = lastBetAmt;
        int previousPlayerBet = player.CurBet;
        player.Bet(blindAmount);
        bool increasedCurrentBet = player.CurBet > previousCurrentBet;

        if (increasedCurrentBet)
            lastBetAmt = player.CurBet;

        return BuildResult(player, previousCurrentBet, previousPlayerBet, increasedCurrentBet, false);
    }

    private static BetResult BuildResult(Player player, int previousCurrentBet, int previousPlayerBet, bool increasedCurrentBet, bool isFullRaise)
    {
        bool isAllIn = player.ChipBalance == 0;

        return new BetResult
        {
            PreviousCurrentBet = previousCurrentBet,
            PreviousPlayerBet = previousPlayerBet,
            NewPlayerBet = player.CurBet,
            AmountAdded = player.CurBet - previousPlayerBet,
            IsAllIn = isAllIn,
            IncreasedCurrentBet = increasedCurrentBet,
            IsFullRaise = isFullRaise,
            IsShortAllIn = isAllIn && increasedCurrentBet && !isFullRaise
        };
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
