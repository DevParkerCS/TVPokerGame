// ** Game State ** //
export type GameStates = Map<string, GameState>;

export type GameState = {
  players: Record<string, PlayerState>;
  pot: PotState;
  board: BoardState;
  curStreet: number;
  curPlayerTurn: number;
  blinds: BlindState;
  lastBetAmt: number;
};

// ** Board State ** //
export type BoardState = {
  flopCards: Card[];
  turnCard: Card | null;
  riverCard: Card | null;
};

// ** Card State **//
export type Suit = "c" | "d" | "h" | "s"; // clubs, diamonds, hearts, spades

export enum Rank {
  Ace = 1,
  Two,
  Three,
  Four,
  Five,
  Six,
  Seven,
  Eight,
  Nine,
  Ten,
  Jack,
  Queen,
  King,
}

export interface Card {
  readonly suit: Suit;
  readonly rank: Rank;
}

// ** Pot State ** //
export type PotState = {
  potAmt: number;
  contributions: Record<string, Contribution>;
};

type Contribution = {
  playerId: string;
  contribution: number;
};

// ** Blind State ** //
type BlindState = {
  curBlindLevel: number;
  blindLevels: BlindLevel[];
};

type BlindLevel = {
  level: number;
  smallBlind: number;
  bigBlind: number;
  Ante: number;
  timeElapsedMinutes: number;
};

// ** Player State ** //
export type PlayerState = {
  name: string;
  socketId: string;
  spriteCode: string;
  cards: Card[];
  balance: number;
  totalBet: number;
  curBet: number;
  hasFolded: boolean;
};
