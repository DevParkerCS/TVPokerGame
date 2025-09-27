import { Namespace } from "socket.io";

// phone.ts
export function registerPhone(ns: Namespace) {
  ns.on("connection", (socket) => {
    console.log("Phone Connected");
  });
}
