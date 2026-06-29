import { createContext, useContext, PropsWithChildren, useEffect, useMemo, useState } from "react";
import { io, Socket } from "socket.io-client";

export type CardPayload = {
  rank: string;
  suit: string;
  code: string;
};

export type SocketContextType = {
  gameState: GameStateType;
  setGameState: React.Dispatch<React.SetStateAction<GameStateType>>;
  roomId: string;
  setRoomId: React.Dispatch<React.SetStateAction<string>>;
  holeCards: CardPayload[];
  setHoleCards: React.Dispatch<React.SetStateAction<CardPayload[]>>;
  socket: Socket;
};

export type GameStateType = {
  playerId: string;
  balance: number;
  lastBet: number;
  isPlayerTurn: boolean;
  curBB: number;
  canPlayerRaise: boolean;
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
  const [gameState, setGameState] = useState<GameStateType>({
    playerId: "",
    balance: 0,
    lastBet: 0,
    curBB: 0,
    isPlayerTurn: false,
    canPlayerRaise: false,
  });

  useEffect(() => {
    const onConnect = () => console.log("Connected");
    const onHoleCards = (payload: { cards: CardPayload[] }) => {
      setHoleCards(payload.cards ?? []);
    };

    socket.on("connect", onConnect);
    socket.on("hole-cards", onHoleCards);

    return () => {
      socket.off("connect", onConnect);
      socket.off("hole-cards", onHoleCards);
    };
  }, [socket]);

  const value: SocketContextType = {
    gameState,
    roomId,
    holeCards,
    socket,
    setGameState,
    setRoomId,
    setHoleCards,
  };

  return (
    <SocketContext.Provider value={value}>{children}</SocketContext.Provider>
  );
};
