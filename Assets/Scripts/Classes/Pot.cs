using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Pot
{
    public int TotalAmt { get; private set; } = 0;
    private Dictionary<string, int> contributions = new();

    public void AddContribution(Player player, int amount) { 
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
        Dictionary<string, int> contributionsCpy = new(contributions);
        Debug.Log($"Total Contributions: {TotalAmt}");

        foreach (var entry in winners)
        {
            // Create a working copy of the current tied group
            List<PlayerManager> tiedGroup = new(entry.Value);

            // Pay them out in layers based on their minimum contribution
            while (tiedGroup.Count > 0 && contributionsCpy.Values.Any(v => v > 0))
            {
                // Get the lowest remaining contribution from players in the group
                int minEligible = tiedGroup.Min(p => contributionsCpy.ContainsKey(p.Player.ID) ? contributionsCpy[p.Player.ID] : 0);
                if (minEligible == 0) break;

                int sidePot = 0;

                // Take minEligible from all contributors
                foreach (var key in contributionsCpy.Keys.ToList())
                {
                    int available = contributionsCpy[key];
                    int take = Math.Min(available, minEligible);
                    contributionsCpy[key] -= take;
                    sidePot += take;
                }

                // Distribute side pot to tied players
                int share = sidePot / tiedGroup.Count;
                int leftover = sidePot % tiedGroup.Count;

                foreach (var player in tiedGroup)
                {
                    Debug.Log($"Player : {player.Player.PlayerName} Earned : {share}");
                    PayoutPlayer(player.Player, share, payouts);
                }

                for (int i = 0; i < leftover; i++)
                {
                    var player = tiedGroup[i];
                    PayoutPlayer(player.Player, 1, payouts);
                }

                // Remove players who have now received all they are eligible for
                tiedGroup = tiedGroup
                    .Where(p => contributionsCpy.ContainsKey(p.Player.ID) && contributionsCpy[p.Player.ID] > 0)
                    .ToList();
            }
        }

        TotalAmt = 0;
        return payouts;
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
    }
}
