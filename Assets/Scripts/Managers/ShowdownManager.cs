using pheval;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EvalCard = pheval.Card;

public class ShowdownManager : MonoBehaviour
{
    public SortedDictionary<int, List<PlayerManager>> HandleShowdown(List<PlayerManager> players, CardManager cm)
    {
        SortedDictionary<int, List<PlayerManager>> winners = new();
        string boardStr = string.Concat(cm.boardCards.Select(c => c.ToEvalCode()));

        foreach (var player in players)
        {
            string handStr = boardStr + string.Concat(player.Player.Cards.Select(c => c.ToEvalCode()));

            var ids = EvalCard.Cards(handStr).Select(x => x.id).ToArray(); // 7 ids
            int rank = Eval.Eval7Ids(ids); // lower is better

            if(winners.ContainsKey(rank))
            {
                winners[rank].Add(player);
            }else
            {
                winners[rank] = new List<PlayerManager> { player };
            }
        }

        return winners;
    }
}
