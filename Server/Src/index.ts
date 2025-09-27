import express from "express";
import { createServer } from "http";
import { Server } from "socket.io";
import { GameState, GameStates } from "./Types/Types";
import { v4 as uuid } from "uuid";
import { CreateNewGame } from "./Game/Factory";
import { registerTV } from "./sockets/tv";
import { registerPhone } from "./sockets/phone";

const PORT = Number(process.env.PORT || process.argv[2] || 5757);

const app = express();
const httpServer = createServer(app);
const io = new Server(httpServer, {
  cors: { origin: "*" }, // relax for LAN/dev; tighten later if needed
});
registerTV(io.of("/tv"));
registerPhone(io.of("/phone"));

httpServer.listen(PORT, "0.0.0.0", () => {
  console.log(`[server] listening on ${PORT}`);
  // Helpful for Unity to detect readiness if you want to parse logs
  console.log(`READY:${PORT}`);
});
