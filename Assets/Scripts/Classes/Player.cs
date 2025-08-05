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
    public int CurBet { get; private set; } = 0;
    public bool HasFolded { get; private set; } = false;
    public bool IsTurn { get; set; } = false;
    public string IconCode { get; set; }
    #endregion

    public Player(int chipBalance, string playerName, string iconCode)
    {
        this.ChipBalance = chipBalance;
        this.PlayerName = playerName;
        this.IconCode = iconCode;
        ID = Guid.NewGuid().ToString();
    }

    #region Methods
    public void AddCard(Card card)
    {
        Cards.Add(card);
    }

    public void Fold()
    {
        Cards.Clear();
        HasFolded = true;
    }

    public void Bet(int newTotalBet)
    {
        int toAdd = newTotalBet - CurBet;
        ChipBalance -= toAdd;
        TotalBet += newTotalBet;
        CurBet += newTotalBet;
    }

    public void AllIn()
    {
        TotalBet += ChipBalance;
        CurBet += ChipBalance;
        ChipBalance = 0;
    }

    public void Pay(int amount)
    {
        ChipBalance += amount;
    }

    public void ResetForNewHand()
    {
        TotalBet = 0;
        CurBet = 0;
        HasFolded = false;
        IsTurn = false;
        Cards.Clear();
    }

    public void ResetForNewStreet()
    {
        CurBet = 0;
        IsTurn = false;
    }
    #endregion
}
