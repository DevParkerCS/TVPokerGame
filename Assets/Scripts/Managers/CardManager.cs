using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    #region Variables
    private GameManager gameManager;
    public List<Sprite> cardSprites;
    public List<Card> deck;
    public List<Card> gameCards;
    public List<Card> boardCards;
    #endregion

    #region Serialized Fields
    [SerializeField] private GameObject flopCardsObj;
    [SerializeField] private GameObject TurnCardObj;
    [SerializeField] private GameObject riverCardObj;
    #endregion

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        boardCards = new();
        deck = new();
        CreateDeck();
        ResetCards();
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

    public void DealFlop()
    {
        for (int i = 0; i < flopCardsObj.transform.childCount; i++)
        {
            Image flopImg = flopCardsObj.transform.GetChild(i).GetComponent<Image>();
            Card card = gameCards[^1];
            gameCards.RemoveAt(gameCards.Count - 1);
            boardCards.Add(card);
            flopImg.sprite = card.sprite;
            flopImg.color = Color.white;
        }
    }

    public void DealTurn()
    {
        Image turnImg = TurnCardObj.GetComponent<Image>();
        Card card = gameCards[^1];
        gameCards.RemoveAt(gameCards.Count - 1);
        boardCards.Add(card);
        turnImg.sprite = card.sprite;
        turnImg.color = Color.white;
    }

    public void DealRiver()
    {
        Image riverImg = riverCardObj.GetComponent<Image>();
        Card card = gameCards[^1];
        gameCards.RemoveAt(gameCards.Count - 1);
        boardCards.Add(card);
        riverImg.sprite = card.sprite;
        riverImg.color = Color.white;
    }

    public void ResetCards()
    {
        Debug.Log($"Deck Count: {deck.Count}, Cards Count: {gameCards.Count}");
        gameCards = deck.ToList<Card>(); 
        Debug.Log($"After Deck Count: {deck.Count}, Cards Count: {gameCards.Count}");

        Util.Shuffle(gameCards);
        for (int i = 0; i < flopCardsObj.transform.childCount; i++)
        {
            Image flopImg = flopCardsObj.transform.GetChild(i).GetComponent<Image>();
            flopImg.sprite = null;
            flopImg.color = new(0, 0, 0, 0);
        }
        Image turnImg = TurnCardObj.GetComponent<Image>();
        turnImg.sprite = null;
        turnImg.color = new(0, 0, 0, 0);

        Image riverImg = riverCardObj.GetComponent<Image>();
        riverImg.sprite = null;
        riverImg.color = new(0, 0, 0, 0);
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

        gameCards = deck.ToList<Card>();
        Util.Shuffle(gameCards);
    }
}
