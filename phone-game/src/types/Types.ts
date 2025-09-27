import { GameStateType } from "../Context/SocketContext";

/** Generic ack envelope shared by client & server */
export type Ack<TData, TErr = string> =
  | { ok: true; data: TData }
  | { ok: false; error: TErr };

/**
 * Describe every client→server event once,
 * and you get payload/response types everywhere.
 */
export interface EmitMap {
  "join-table": {
    payload: { tableId: number };
    response: GameStateType;
    error: string;
  };
  "chat:send": {
    payload: { text: string };
    response: { id: string; at: string };
    error: string;
  };
  // add more events here...
}

/** Optional: events the server emits to the client */
export interface ServerToClientEvents {
  "chat:message": (msg: {
    id: string;
    text: string;
    from: string;
    at: string;
  }) => void;
  // ...
}

/** Build the client→server function signatures from EmitMap */
export type ClientToServerEvents = {
  [K in keyof EmitMap]: (
    payload: EmitMap[K]["payload"],
    ack: (res: Ack<EmitMap[K]["response"], EmitMap[K]["error"]>) => void
  ) => void;
};
