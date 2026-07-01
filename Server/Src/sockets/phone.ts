import { Namespace } from "socket.io";
import { games } from "../State/GameState";
import {
  CardPayload,
  JoinTablePayload,
  PhoneGameState,
  PlayerActionPayload,
  PlayerJoinedPayload,
} from "../Types/Types";

const STARTING_BALANCE = 10000;

function cleanRoomId(roomId: string): string {
  return roomId.trim().toUpperCase();
}

function toCardPayload(card: { rank: string; suit: string }): CardPayload {
  const rank = String(card.rank).toUpperCase();
  const suit = String(card.suit).toUpperCase();
  return {
    rank,
    suit,
    code: `${rank}${suit}`,
  };
}

// phone.ts
export function registerPhone(ns: Namespace) {
  ns.on("connection", (socket) => {
    console.log("Phone Connected", socket.id);

    socket.on("join-table", (payload: JoinTablePayload, ack) => {
      try {
        const roomId = cleanRoomId(payload.roomId);
        const game = games.get(roomId);

        if (!game) {
          ack({ ok: false, error: "Room not found" });
          return;
        }

        const requestedPlayerId = payload.playerId?.trim();
        const isReconnect = !!requestedPlayerId && !!game.players[requestedPlayerId];
        const playerId = isReconnect ? requestedPlayerId : socket.id;
        const playerName = payload.name.trim();

        if (isReconnect) {
          game.players[playerId].socketId = socket.id;
          game.players[playerId].name = playerName || game.players[playerId].name;
          game.players[playerId].spriteCode = payload.spriteCode || game.players[playerId].spriteCode;
          game.players[playerId].color = payload.color || game.players[playerId].color;
        } else {
          game.players[playerId] = {
            name: playerName,
            socketId: socket.id,
            spriteCode: payload.spriteCode,
            color: payload.color,
            cards: [],
            balance: STARTING_BALANCE,
            totalBet: 0,
            curBet: 0,
            hasFolded: false,
          };
        }

        socket.join(roomId);
        socket.join(playerId);
        socket.data.roomId = roomId;
        socket.data.playerId = playerId;

        console.log(`Phone ${playerId} ${isReconnect ? "reconnected to" : "joined"} room ${roomId}`);

        if (!isReconnect) {
          const playerJoined: PlayerJoinedPayload = {
            roomId,
            playerId,
            name: game.players[playerId].name,
            spriteCode: game.players[playerId].spriteCode,
            color: game.players[playerId].color,
            balance: STARTING_BALANCE,
          };

          ns.server.of("/tv").to(roomId).emit("player-joined", playerJoined);
        }

        const phoneGameState: PhoneGameState = {
          playerId,
          balance: game.players[playerId].balance,
          lastBet: game.lastBetAmt,
          isPlayerTurn: false,
          curBB: 0,
          canPlayerRaise: false,
        };

        ack({ ok: true, data: phoneGameState });

        if (game.players[playerId].cards.length > 0) {
          socket.emit("hole-cards", {
            cards: game.players[playerId].cards.map(toCardPayload),
          });
        }
      } catch (e) {
        ack({ ok: false, error: String(e) });
      }
    });

    socket.on("player-action", (payload: PlayerActionPayload, ack) => {
      try {
        const roomId = cleanRoomId(payload.roomId || socket.data.roomId);
        const playerId = payload.playerId || socket.data.playerId;
        const game = games.get(roomId);

        if (!game) {
          ack({ ok: false, error: "Room not found" });
          return;
        }

        if (!playerId || !game.players[playerId]) {
          ack({ ok: false, error: "Player not found in room" });
          return;
        }

        ns.server.of("/tv").to(roomId).emit("player-action", {
          playerId,
          action: payload.action,
          amount: payload.amount || 0,
        });

        ack({ ok: true, data: { accepted: true } });
      } catch (e) {
        ack({ ok: false, error: String(e) });
      }
    });
  });
}
