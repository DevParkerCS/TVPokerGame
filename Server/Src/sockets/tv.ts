import type { Namespace } from "socket.io";
import {
  DealPlayerCardsPayload,
  GameState,
  PhoneHandLifecyclePayload,
  PhoneTurnStatePayload,
} from "../Types/Types";
import { CreateNewGame } from "../Game/Factory";
import { games } from "../State/GameState";
import { nanoid } from "nanoid";

function cleanRoomId(roomId: string): string {
  return roomId.trim().toUpperCase();
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

    socket.on("deal-player-cards", (payload: DealPlayerCardsPayload, ack) => {
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

    socket.on("phone-hand-lifecycle", (payload: PhoneHandLifecyclePayload & { eventName?: string; eventValue?: string }, ack) => {
      try {
        const roomId = cleanRoomId(payload.roomId || socket.data.roomId || "");
        const game = games.get(roomId);
        const event = payload.event || payload.eventName || payload.eventValue;

        if (!game) {
          ack?.({ ok: false, error: "Room not found" });
          return;
        }

        if (event !== "hand-reset" && event !== "hand-started") {
          ack?.({ ok: false, error: "Unknown hand lifecycle event" });
          return;
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

        ns.server.of("/phone").to(payload.playerId).emit("turn-state", payload);
        ack?.({ ok: true, data: { delivered: true } });
      } catch (e) {
        ack?.({ ok: false, error: String(e) });
      }
    });
  });
}
