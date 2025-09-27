import { io, Socket } from "socket.io-client";
import { PlayerInfo } from "../Pages/PlayerInfo/PlayerInfo";
import { Ack, EmitMap } from "../types/Types";
import { GameStateType } from "../Context/SocketContext";

/**
 * Fully-typed safe emitter with a real timeout.
 * Returns only the success data (rejects on timeout or server error).
 */
export function emitSafe<Evt extends keyof EmitMap>(
  event: Evt,
  data: EmitMap[Evt]["payload"],
  timeoutMs = 5000,
  socket: Socket
): Promise<EmitMap[Evt]["response"]> {
  return new Promise((resolve, reject) => {
    let settled = false;

    const t = setTimeout(() => {
      if (settled) return;
      settled = true;
      reject(
        new Error(
          `Ack timeout after ${timeoutMs}ms for event "${String(event)}"`
        )
      );
    }, timeoutMs);

    // If you don't want offline buffering, you can guard with:
    // if (!socket.connected) return reject(new Error("Socket not connected"));

    socket.emit(
      event,
      data,
      (res: Ack<EmitMap[Evt]["response"], EmitMap[Evt]["error"]>) => {
        if (settled) return;
        settled = true;
        clearTimeout(t);

        if (!res?.ok) {
          const msg = (res as { error?: string })?.error ?? "Server error";
          return reject(new Error(msg));
        }
        resolve(res.data);
      }
    );
  });
}
export const JoinRoom = async (playerInfo: PlayerInfo, socket: Socket) => {
  try {
    const res: GameStateType = await emitSafe(
      "join-table",
      { tableId: 1 },
      undefined,
      socket
    );

    return res;
  } catch (e) {
    console.log("Error: " + e);
  }
};
