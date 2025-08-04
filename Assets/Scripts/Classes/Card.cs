using UnityEngine;

public class Card
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades }
    public enum Rank { Ace = 1,Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

    public Suit suit;
    public Rank rank;
    public Sprite sprite;

    public Card(Suit suit, Rank rank, Sprite sprite)
    {
        this.suit = suit;
        this.rank = rank;
        this.sprite = sprite;
    }

    public override string ToString()
    {
        return $"{rank} of {suit}";
    }
}