// Card.ts (server)

import { Card, Rank, Suit } from "../Types/Types";

// --- Evaluator helpers (match your C# logic) ---
export const evalRankChar = (rank: Rank): string => {
  switch (rank) {
    case Rank.Ace:
      return "a";
    case Rank.King:
      return "k";
    case Rank.Queen:
      return "q";
    case Rank.Jack:
      return "j";
    case Rank.Ten:
      return "t";
    default:
      return String(rank); // 2..9 -> '2'..'9'
  }
};

export const evalSuitChar = (suit: Suit): string => suit; // already 'c','d','h','s'

export const toEvalCode = (c: Card): string =>
  `${evalRankChar(c.rank)}${evalSuitChar(c.suit)}`;

export const toEvalString = (cards: Iterable<Card>): string =>
  Array.from(cards, toEvalCode).join("");

// Optional: convenience helpers
export const makeCard = (rank: Rank, suit: Suit): Card =>
  Object.freeze({ rank, suit });

export const rankName = (rank: Rank): string => Rank[rank]; // "Ace", "Two", ...

export const suitName = (suit: Suit): string => {
  switch (suit) {
    case "c":
      return "Clubs";
    case "d":
      return "Diamonds";
    case "h":
      return "Hearts";
    case "s":
      return "Spades";
  }
};

export const cardToString = (c: Card): string =>
  `${rankName(c.rank)} of ${suitName(c.suit)}`;
