using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PotManager : MonoBehaviour
{
    public Pot pot { get; private set; }
    [SerializeField] private TextMeshProUGUI potTMP;

    private void Awake()
    {
        pot = new Pot();
        potTMP.text = "Pot: 0";
    }

    public void AddAllBetsToPot(List<PlayerManager> players)
    {
        foreach (PlayerManager p in players)
        {
            if(!p.Player.HasFolded)
                pot.AddContribution(p.Player, p.Player.CurBet);
        }

        potTMP.text = "Pot: " + pot.TotalAmt.ToString();
    }

    public void AddBetToPot(PlayerManager p)
    {
        pot.AddContribution(p.Player, p.Player.CurBet);
        potTMP.text = "Pot: " + pot.TotalAmt.ToString();
    }

    public Dictionary<string, int> GetWinnersPayout(SortedDictionary<int, List<PlayerManager>> winners)
    {
        Dictionary<string, int> payouts = pot.PayoutWinners(winners);
        potTMP.text = "Pot: " + pot.TotalAmt;
        return payouts;
    }
}
