using System;
using System.Collections.Generic;
using UnityEngine;
public class Player
{
    #region Public Variables
    public string ID;
    public int ChipBalance { get; set; } = 0;
    public List<Card> Cards { get;} = new List<Card>();
    public string PlayerName { get; set; }
    public int TotalBet { get; private set; } = 0;
    public bool HasFolded { get; private set; } = false;
    public bool IsTurn { get; set; } = false;
    public Sprite IconSprite { get; set; }
    #endregion

    public Player(int chipBalance, string playerName, Sprite iconSprite)
    {
        this.ChipBalance = chipBalance;
        this.PlayerName = playerName;
        this.IconSprite = iconSprite;
        ID = Guid.NewGuid().ToString();
    }

    #region Methods
    public void AddCard(Card card)
    {
        Cards.Add(card);
    }

    public void FoldCards()
    {
        Cards.Clear();
        HasFolded = true;
    }

    public void Bet(int newTotalBet)
    {
        int toAdd = newTotalBet - TotalBet;
        ChipBalance -= toAdd;
        TotalBet = newTotalBet;
    }

    public void AllIn()
    {
        TotalBet += ChipBalance;
        ChipBalance = 0;
    }

    public void Pay(int amount)
    {
        ChipBalance += amount;
    }

    public void ResetForNewHand()
    {
        TotalBet = 0;
        HasFolded = false;
        IsTurn = false;
        Cards.Clear();
    }
    #endregion
}
