using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class CardManager : MonoBehaviour
{
    #region Variables
    private GameManager gameManager;
    public List<Sprite> cardSprites;
    public List<Card> deck;
    public List<Card> gameCards;
    #endregion

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        deck = new();
        CreateDeck();
    }

    public IEnumerator DealCards()
    {
        for (int i = 0; i <= 1; i++)
        {
            for (int offset = 1; offset <= gameManager.Players.Count; offset++)
            {
                int j = (gameManager.dealerIndex + offset) % gameManager.Players.Count;

                if (j != gameManager.dealerIndex)
                    yield return new WaitForSeconds(0.25f);
                else
                    yield return new WaitForSeconds(0.15f);

                Card card = gameCards[^1];
                gameCards.RemoveAt(gameCards.Count - 1);

                gameManager.Players[j].ShowCard(i);
                gameManager.Players[j].Player.AddCard(card);
            }
        }

        List<Card> cards = gameManager.Players[0].Player.Cards;
        Debug.Log($"[{cards[0].rank} {cards[0].suit}], [{cards[1].rank} {cards[1].suit}]");
    }

    public void ResetGameDeck()
    {
        gameCards = deck;
        Util.Shuffle(gameCards);
    }

    private void CreateDeck()
    {
        int index = 0;
        foreach(Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            foreach (Card.Rank rank in System.Enum.GetValues(typeof(Card.Rank)))
            {
                Card newCard = new(suit, rank, cardSprites[index]);
                deck.Add(newCard);
                index++;
            }
        }

        gameCards = deck;
        Util.Shuffle(gameCards);
    }
}
