import { createContext, useContext, PropsWithChildren, useMemo, useState } from "react";
import { io, Socket } from "socket.io-client";

export type SocketContextType = {
  gameState: GameStateType;
  setGameState: React.Dispatch<React.SetStateAction<GameStateType>>;
  roomId: string;
  setRoomId: React.Dispatch<React.SetStateAction<string>>;
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
  const [gameState, setGameState] = useState<GameStateType>({
    playerId: "",
    balance: 0,
    lastBet: 0,
    curBB: 0,
    isPlayerTurn: false,
    canPlayerRaise: false,
  });

  socket.on("connect", () => {
    console.log("Connected");
  });

  const value: SocketContextType = { gameState, roomId, socket, setGameState, setRoomId };

  return (
    <SocketContext.Provider value={value}>{children}</SocketContext.Provider>
  );
};
