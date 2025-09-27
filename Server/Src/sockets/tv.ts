import type { Namespace } from "socket.io";
import { v4 } from "uuid";
import { GameState } from "../Types/Types";
import { CreateNewGame } from "../Game/Factory";
import { games } from "../State/GameState";
import { nanoid } from "nanoid";

// Tv Socket Events
export function registerTV(ns: Namespace) {
  ns.on("connection", (socket) => {
    socket.on("create-room", async (ack) => {
      try {
        let roomId = "";
        do {
          // Continue creating room ids till new one is created
          roomId = nanoid(6);
        } while (games.has(roomId));
        const Game: GameState = CreateNewGame();

        socket.join(roomId);
        ack({ ok: true, roomId });
      } catch (e) {
        ack({ ok: false, error: String(e) });
      }
    });
  });
}
