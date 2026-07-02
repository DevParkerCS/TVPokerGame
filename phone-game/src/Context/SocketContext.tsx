import { createContext, useContext, PropsWithChildren, useEffect, useMemo, useState } from "react";
import { io, Socket } from "socket.io-client";

export type CardPayload = {
  rank: string;
  suit: string;
  code: string;
};

export type TurnStateType = {
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

export type GameStateType = {
  playerId: string;
  balance: number;
  lastBet: number;
  isPlayerTurn: boolean;
  curBB: number;
  canPlayerRaise: boolean;
};

export const createEmptyGameState = (): GameStateType => ({
  playerId: "",
  balance: 0,
  lastBet: 0,
  curBB: 0,
  isPlayerTurn: false,
  canPlayerRaise: false,
});

export type SocketContextType = {
  gameState: GameStateType;
  setGameState: React.Dispatch<React.SetStateAction<GameStateType>>;
  roomId: string;
  setRoomId: React.Dispatch<React.SetStateAction<string>>;
  holeCards: CardPayload[];
  setHoleCards: React.Dispatch<React.SetStateAction<CardPayload[]>>;
  turnState: TurnStateType | null;
  setTurnState: React.Dispatch<React.SetStateAction<TurnStateType | null>>;
  socket: Socket;
};

export const SocketContext = createContext<SocketContextType | undefined>(
  undefined
);

export function useSocket(): SocketContextType {
  const ctx = useContext(SocketContext);
  if (!ctx) {
    throw new Error("useGameSession must be used within <GameSessionProvider>");
  }
  return ctx;
}

export const SocketContextProvider = ({ children }: PropsWithChildren) => {
  const { protocol, hostname } = window.location;
  const socket = useMemo(
    () =>
      io(`${protocol}//${hostname}:5757/phone`, {
        transports: ["websocket"],
      }),
    [protocol, hostname]
  );

  const [roomId, setRoomId] = useState("");
  const [holeCards, setHoleCards] = useState<CardPayload[]>([]);
  const [turnState, setTurnState] = useState<TurnStateType | null>(null);
  const [gameState, setGameState] = useState<GameStateType>(createEmptyGameState());

  useEffect(() => {
    const onConnect = () => console.log("Connected", socket.id);
    const onHoleCards = (payload: { cards: CardPayload[] }) => {
      console.log("Received hole-cards", payload.cards);
      setHoleCards(payload.cards ?? []);
    };
    const onTurnState = (payload: TurnStateType) => {
      console.log("Received turn-state", payload);
      setTurnState(payload);
      setGameState((prev) => ({
        ...prev,
        balance: payload.balance,
        lastBet: payload.currentBet,
        isPlayerTurn: payload.isPlayerTurn,
        canPlayerRaise: payload.canRaise,
      }));
    };
    const onHandEnded = () => {
      console.log("Received hand-ended");
      setTurnState(null);
      setGameState((prev) => ({
        ...prev,
        isPlayerTurn: false,
        canPlayerRaise: false,
      }));
    };
    const onHandReset = () => {
      console.log("Received hand-reset");
      setTurnState(null);
      setGameState((prev) => ({
        ...prev,
        lastBet: 0,
        isPlayerTurn: false,
        canPlayerRaise: false,
      }));
    };
    const onHandStarted = () => {
      console.log("Received hand-started");
      setTurnState(null);
      setGameState((prev) => ({
        ...prev,
        lastBet: 0,
        isPlayerTurn: false,
        canPlayerRaise: false,
      }));
    };

    socket.on("connect", onConnect);
    socket.on("hole-cards", onHoleCards);
    socket.on("turn-state", onTurnState);
    socket.on("hand-ended", onHandEnded);
    socket.on("hand-reset", onHandReset);
    socket.on("hand-started", onHandStarted);

    return () => {
      socket.off("connect", onConnect);
      socket.off("hole-cards", onHoleCards);
      socket.off("turn-state", onTurnState);
      socket.off("hand-ended", onHandEnded);
      socket.off("hand-reset", onHandReset);
      socket.off("hand-started", onHandStarted);
    };
  }, [socket]);

  const value: SocketContextType = {
    gameState,
    roomId,
    holeCards,
    turnState,
    socket,
    setGameState,
    setRoomId,
    setHoleCards,
    setTurnState,
  };

  return (
    <SocketContext.Provider value={value}>{children}</SocketContext.Provider>
  );
};