using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PotManager : MonoBehaviour
{
    public Pot pot { get; private set; }
    [SerializeField] private TextMeshProUGUI potTMP;

    private void Awake()
    {
        ResetPot();
    }

    public void AddAllBetsToPot(List<PlayerManager> players)
    {
        foreach (PlayerManager p in players)
        {
            if(!p.Player.HasFolded)
                pot.AddContribution(p.Player, p.Player.CurBet);
        }

        UpdatePotText();
    }

    public void AddBetToPot(PlayerManager p)
    {
        pot.AddContribution(p.Player, p.Player.CurBet);
        UpdatePotText();
    }

    public Dictionary<string, int> GetWinnersPayout(SortedDictionary<int, List<PlayerManager>> winners)
    {
        Dictionary<string, int> payouts = pot.PayoutWinners(winners);
        UpdatePotText();
        return payouts;
    }

    public void ResetPot()
    {
        pot = new Pot();
        UpdatePotText();
    }

    private void UpdatePotText()
    {
        if (potTMP != null)
            potTMP.text = "Pot: " + pot.TotalAmt;
    }
}