using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Card
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades }
    public enum Rank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

    public Suit suit;
    public Rank rank;
    public Sprite sprite;

    public Card(Suit suit, Rank rank, Sprite sprite)
    {
        this.suit = suit;
        this.rank = rank;
        this.sprite = sprite;
    }

    // --- Evaluator helpers ---
    public char EvalRankChar()
    {
        switch (rank)
        {
            case Rank.Ace: return 'a';
            case Rank.King: return 'k';
            case Rank.Queen: return 'q';
            case Rank.Jack: return 'j';
            case Rank.Ten: return 't';
            case Rank.Two:
            case Rank.Three:
            case Rank.Four:
            case Rank.Five:
            case Rank.Six:
            case Rank.Seven:
            case Rank.Eight:
            case Rank.Nine:
                return (char)('0' + (int)rank); // 2..9 -> '2'..'9'
            default:
                return '?';
        }
    }

    public char EvalSuitChar()
    {
        switch (suit)
        {
            case Suit.Clubs: return 'c';
            case Suit.Diamonds: return 'd';
            case Suit.Hearts: return 'h';
            case Suit.Spades: return 's';
            default: return '?';
        }
    }

    public string ToEvalCode() => $"{EvalRankChar()}{EvalSuitChar()}";

    public static string ToEvalString(IEnumerable<Card> cards) =>
        string.Concat(cards.Select(c => c.ToEvalCode()));

    public override string ToString() => $"{rank} of {suit}";
}
