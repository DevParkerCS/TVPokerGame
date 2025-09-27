import { createContext, useContext, PropsWithChildren, useState } from "react";
import { io, Socket } from "socket.io-client";

export type SocketContextType = {
  gameState: GameStateType;
  setGameState: React.Dispatch<React.SetStateAction<GameStateType>>;
  socket: Socket;
};

export type GameStateType = {
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
  const socket = io(`${protocol}//${hostname}:8000/phone`, {
    transports: ["websocket"],
  });
  const [gameState, setGameState] = useState<GameStateType>({
    balance: 0,
    lastBet: 0,
    curBB: 0,
    isPlayerTurn: false,
    canPlayerRaise: false,
  });

  socket.on("connect", () => {
    console.log("Connected");
  });

  const value: SocketContextType = { gameState, socket, setGameState };

  return (
    <SocketContext.Provider value={value}>{children}</SocketContext.Provider>
  );
};
