using UnityEngine;
using System.Linq;
// If the pheval files declare a namespace, it's "pheval"
using pheval;
using EvalCard = pheval.Card;

public class EvaluatorSmokeTest : MonoBehaviour
{
    // Two simple hole-card sets + a shared board (lowercase ranks: 2-9,t,j,q,k,a; suits: c,d,h,s)
    [SerializeField] private string board = "2c7d9h9s2d"; // 2♣ 7♦ 9♥ 9♠ 2♦
    [SerializeField] private string p1 = "ahad";       // A♥ A♦
    [SerializeField] private string p2 = "khkd";       // K♥ K♦

    void Start()
    {
        var p1Ids = EvalCard.Cards(p1 + board).Select(c => c.id).ToArray();
        var p2Ids = EvalCard.Cards(p2 + board).Select(c => c.id).ToArray();

        int r1 = Eval.Eval7Ids(p1Ids);
        int r2 = Eval.Eval7Ids(p2Ids);

        Debug.Log($"Board: {EvalCard.CardsToString(EvalCard.Cards(board))}");
        Debug.Log($"P1: {EvalCard.CardsToString(EvalCard.Cards(p1))} -> {Rank.DescribeRank(r1)} ({Rank.DescribeRankCategory(r1)}) [rank {r1}]");
        Debug.Log($"P2: {EvalCard.CardsToString(EvalCard.Cards(p2))} -> {Rank.DescribeRank(r2)} ({Rank.DescribeRankCategory(r2)}) [rank {r2}]");

        // In this evaluator, LOWER rank value = stronger hand.
        if (r1 < r2) Debug.Log("Result: P1 wins");
        else if (r1 > r2) Debug.Log("Result: P2 wins");
        else Debug.Log("Result: Tie");
    }
}
