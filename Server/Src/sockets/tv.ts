import type { Namespace } from "socket.io";
import {
  DealPlayerCardsPayload,
  GameState,
  PhoneHandLifecyclePayload,
  PhoneTurnStatePayload,
  PlayerBalancesPayload,
} from "../Types/Types";
import { CreateNewGame } from "../Game/Factory";
import { games } from "../State/GameState";
import { nanoid } from "nanoid";

function cleanRoomId(roomId: string): string {
  return roomId.trim().toUpperCase();
}

const lifecycleEvents = new Set(["hand-reset", "hand-started", "hand-ended"]);

function isGameOverMessage(message: string | undefined): boolean {
  const normalized = (message || "").toLowerCase();
  return normalized.includes("wins the game") || normalized === "game over";
}

// Tv Socket Events
export function registerTV(ns: Namespace) {
  ns.on("connection", (socket) => {
    socket.on("create-room", async (ack) => {
      try {
        let roomId = "";
        do {
          // Continue creating room ids till new one is created
          roomId = nanoid(6).toUpperCase();
        } while (games.has(roomId));

        const game: GameState = CreateNewGame();
        games.set(roomId, game);

        socket.join(roomId);
        socket.data.roomId = roomId;

        ack({ ok: true, roomId });
      } catch (e) {
        ack({ ok: false, error: String(e) });
      }
    });

    socket.on("game-started", (payload: { roomId?: string } | undefined, ack) => {
      try {
        const roomId = cleanRoomId(payload?.roomId || socket.data.roomId || "");
        const game = games.get(roomId);

        if (!game) {
          ack?.({ ok: false, error: "Room not found" });
          return;
        }

        game.isStarted = true;
        ack?.({ ok: true, data: { markedStarted: true } });
      } catch (e) {
        ack?.({ ok: false, error: String(e) });
      }
    });

    socket.on("deal-player-cards", (payload: DealPlayerCardsPayload, ack) => {
      try {
        const roomId = cleanRoomId(payload.roomId || socket.data.roomId || "");
        const game = games.get(roomId);

        if (!game) {
          ack?.({ ok: false, error: "Room not found" });
          return;
        }

        game.isStarted = true;

        if (!payload.playerId || !game.players[payload.playerId]) {
          ack?.({ ok: false, error: "Player not found in room" });
          return;
        }

        game.players[payload.playerId].cards = payload.cards.map((card) => ({
          rank: card.rank as any,
          suit: card.suit.toLowerCase() as any,
        }));

        ns.server.of("/phone").to(payload.playerId).emit("hole-cards", {
          cards: payload.cards,
        });

        ack?.({ ok: true, data: { delivered: true } });
      } catch (e) {
        ack?.({ ok: false, error: String(e) });
      }
    });

    socket.on("sync-player-balances", (payload: PlayerBalancesPayload, ack) => {
      try {
        const roomId = cleanRoomId(payload.roomId || socket.data.roomId || "");
        const game = games.get(roomId);

        if (!game) {
          ack?.({ ok: false, error: "Room not found" });
          return;
        }

        for (const playerBalance of payload.players || []) {
          const player = game.players[playerBalance.playerId];
          if (!player) continue;

          player.balance = playerBalance.balance;
          player.totalBet = playerBalance.totalBet;
          player.curBet = playerBalance.curBet;
          player.hasFolded = playerBalance.hasFolded;

          ns.server.of("/phone").to(playerBalance.playerId).emit("balance-sync", {
            balance: playerBalance.balance,
          });
        }

        ack?.({ ok: true, data: { synced: true } });
      } catch (e) {
        ack?.({ ok: false, error: String(e) });
      }
    });

    socket.on("phone-hand-lifecycle", (payload: PhoneHandLifecyclePayload & { eventName?: string; eventValue?: string }, ack) => {
      try {
        const roomId = cleanRoomId(payload.roomId || socket.data.roomId || "");
        const game = games.get(roomId);
        const event = payload.event || payload.eventName || payload.eventValue;

        if (!game) {
          ack?.({ ok: false, error: "Room not found" });
          return;
        }

        if (!event || !lifecycleEvents.has(event)) {
          ack?.({ ok: false, error: "Unknown hand lifecycle event" });
          return;
        }

        if (event === "hand-reset") {
          game.pot.potAmt = 0;
          game.lastBetAmt = 0;

          for (const player of Object.values(game.players)) {
            player.cards = [];
            player.totalBet = 0;
            player.curBet = 0;
            player.hasFolded = false;
          }
        }

        if (event === "hand-ended" && isGameOverMessage(payload.message)) {
          game.isStarted = false;
        }

        ns.server.of("/phone").to(roomId).emit(event, {
          handId: payload.handId,
          message: payload.message || "",
        });

        ack?.({ ok: true, data: { delivered: true } });
      } catch (e) {
        ack?.({ ok: false, error: String(e) });
      }
    });

    socket.on("phone-turn-state", (payload: PhoneTurnStatePayload, ack) => {
      try {
        const roomId = cleanRoomId(payload.roomId || socket.data.roomId || "");
        const game = games.get(roomId);

        if (!game) {
          ack?.({ ok: false, error: "Room not found" });
          return;
        }

        if (!payload.playerId || !game.players[payload.playerId]) {
          ack?.({ ok: false, error: "Player not found in room" });
          return;
        }

        game.players[payload.playerId].balance = payload.balance;
        game.players[payload.playerId].curBet = payload.playerBet;
        game.lastBetAmt = payload.currentBet;
        game.pot.potAmt = payload.pot;

        ns.server.of("/phone").to(payload.playerId).emit("turn-state", payload);
        ack?.({ ok: true, data: { delivered: true } });
      } catch (e) {
        ack?.({ ok: false, error: String(e) });
      }
    });
  });
}