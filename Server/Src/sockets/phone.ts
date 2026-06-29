import { Namespace } from "socket.io";
import { games } from "../State/GameState";
import { JoinTablePayload, PhoneGameState, PlayerJoinedPayload } from "../Types/Types";

const STARTING_BALANCE = 10000;

function cleanRoomId(roomId: string): string {
  return roomId.trim().toUpperCase();
}

// phone.ts
export function registerPhone(ns: Namespace) {
  ns.on("connection", (socket) => {
    console.log("Phone Connected");

    socket.on("join-table", (payload: JoinTablePayload, ack) => {
      try {
        const roomId = cleanRoomId(payload.roomId);
        const game = games.get(roomId);

        if (!game) {
          ack({ ok: false, error: "Room not found" });
          return;
        }

        const playerId = socket.id;
        const playerJoined: PlayerJoinedPayload = {
          roomId,
          playerId,
          name: payload.name.trim(),
          spriteCode: payload.spriteCode,
          color: payload.color,
          balance: STARTING_BALANCE,
        };

        game.players[playerId] = {
          name: playerJoined.name,
          socketId: socket.id,
          spriteCode: playerJoined.spriteCode,
          color: playerJoined.color,
          cards: [],
          balance: STARTING_BALANCE,
          totalBet: 0,
          curBet: 0,
          hasFolded: false,
        };

        socket.join(roomId);
        socket.data.roomId = roomId;
        socket.data.playerId = playerId;

        ns.server.of("/tv").to(roomId).emit("player-joined", playerJoined);

        const phoneGameState: PhoneGameState = {
          playerId,
          balance: STARTING_BALANCE,
          lastBet: game.lastBetAmt,
          isPlayerTurn: false,
          curBB: 0,
          canPlayerRaise: false,
        };

        ack({ ok: true, data: phoneGameState });
      } catch (e) {
        ack({ ok: false, error: String(e) });
      }
    });
  });
}
