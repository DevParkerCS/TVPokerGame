using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pot
{
    public int TotalAmt { get; private set; } = 0;
    private Dictionary<string, int> contributions = new();

    public void AddContribution(Player player, int amount) {
        if (amount <= 0) return;

        if(contributions.ContainsKey(player.ID))
        {
            contributions[player.ID] += amount;
        }else
        {
            contributions.Add(player.ID, amount);
        }
        Debug.Log($"Adding {amount} to pot");
        TotalAmt += amount;
    }

    public Dictionary<string, int> PayoutWinners(SortedDictionary<int, List<PlayerManager>> winners)
    {
        Dictionary<string, int> payouts = new();
        List<int> contributionLevels = contributions.Values
            .Where(v => v > 0)
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        Debug.Log($"Total Contributions: {TotalAmt}");

        int previousLevel = 0;
        foreach (int level in contributionLevels)
        {
            int sidePot = GetSidePotAmount(previousLevel, level);
            previousLevel = level;

            if (sidePot <= 0)
                continue;

            List<PlayerManager> eligibleWinners = GetBestEligibleWinners(winners, level);
            if (eligibleWinners.Count == 0)
                continue;

            PaySidePot(eligibleWinners, sidePot, payouts);
        }

        Reset();
        return payouts;
    }

    private int GetSidePotAmount(int previousLevel, int currentLevel)
    {
        int sidePot = 0;

        foreach (int contribution in contributions.Values)
        {
            int cappedContribution = Math.Min(contribution, currentLevel);
            int slice = cappedContribution - previousLevel;

            if (slice > 0)
                sidePot += slice;
        }

        return sidePot;
    }

    private List<PlayerManager> GetBestEligibleWinners(SortedDictionary<int, List<PlayerManager>> winners, int contributionLevel)
    {
        foreach (var entry in winners)
        {
            List<PlayerManager> eligible = entry.Value
                .Where(pm => contributions.ContainsKey(pm.Player.ID) && contributions[pm.Player.ID] >= contributionLevel)
                .ToList();

            if (eligible.Count > 0)
                return eligible;
        }

        return new List<PlayerManager>();
    }

    private void PaySidePot(List<PlayerManager> winners, int sidePot, Dictionary<string, int> payouts)
    {
        int share = sidePot / winners.Count;
        int leftover = sidePot % winners.Count;

        foreach (var winner in winners)
        {
            Debug.Log($"Player : {winner.Player.PlayerName} Earned : {share}");
            PayoutPlayer(winner.Player, share, payouts);
        }

        for (int i = 0; i < leftover; i++)
        {
            PayoutPlayer(winners[i].Player, 1, payouts);
        }
    }

    private void PayoutPlayer(Player player, int amount, Dictionary<string, int> payouts)
    {
        if (amount <= 0) return;

        player.Pay(amount);
        TotalAmt -= amount;

        if (payouts.ContainsKey(player.ID))
            payouts[player.ID] += amount;
        else
            payouts[player.ID] = amount;
    }

    public void Reset()
    {
        TotalAmt = 0;
        contributions.Clear();
    }
}
