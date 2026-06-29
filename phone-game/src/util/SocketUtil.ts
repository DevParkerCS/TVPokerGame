import { Socket } from "socket.io-client";
import type { PlayerInfo } from "../Pages/PlayerInfo/PlayerInfo";
import type { Ack, EmitMap, PlayerActionType } from "../types/Types";
import type { GameStateType } from "../Context/SocketContext";

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
  const res: GameStateType = await emitSafe(
    "join-table",
    playerInfo,
    5000,
    socket
  );

  return res;
};

export const SendPlayerAction = async (
  roomId: string,
  playerId: string,
  action: PlayerActionType,
  amount: number | undefined,
  socket: Socket
) => {
  return emitSafe(
    "player-action",
    { roomId, playerId, action, amount },
    5000,
    socket
  );
};
