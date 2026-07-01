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

// ** Socket Payloads ** //
export type ColorState = {
  r: number;
  g: number;
  b: number;
};

export type JoinTablePayload = {
  roomId: string;
  name: string;
  spriteCode: string;
  color: ColorState;
  playerId?: string;
};

export type PlayerJoinedPayload = JoinTablePayload & {
  playerId: string;
  balance: number;
};

export type PlayerActionType = "fold" | "check" | "call" | "bet" | "raise";

export type PlayerActionPayload = {
  roomId: string;
  playerId: string;
  action: PlayerActionType;
  amount?: number;
};

export type CardPayload = {
  rank: string;
  suit: string;
  code: string;
};

export type DealPlayerCardsPayload = {
  roomId: string;
  playerId: string;
  cards: CardPayload[];
};

export type HoleCardsPayload = {
  cards: CardPayload[];
};

export type PhoneHandLifecycleEvent = "hand-reset" | "hand-started" | "hand-ended";

export type PhoneHandLifecyclePayload = {
  roomId: string;
  event: PhoneHandLifecycleEvent;
  handId: number;
  message?: string;
};

export type PhoneTurnStatePayload = {
  roomId: string;
  playerId: string;
  isPlayerTurn: boolean;
  currentPlayerId: string;
  currentPlayerName: string;
  balance: number;
  pot: number;
  currentBet: number;
  playerBet: number;
  amountToCall: number;
  minRaiseTo: number;
  canFold: boolean;
  canCheck: boolean;
  canCall: boolean;
  canBet: boolean;
  canRaise: boolean;
};

export type PhoneGameState = {
  playerId: string;
  balance: number;
  lastBet: number;
  isPlayerTurn: boolean;
  curBB: number;
  canPlayerRaise: boolean;
};

export type Ack<TData, TErr = string> =
  | { ok: true; data: TData }
  | { ok: false; error: TErr };

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
  color?: ColorState;
  cards: Card[];
  balance: number;
  totalBet: number;
  curBet: number;
  hasFolded: boolean;
};
