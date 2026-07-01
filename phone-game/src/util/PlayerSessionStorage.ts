import type { PlayerInfo } from "../Pages/PlayerInfo/PlayerInfo";

const PLAYER_SESSION_KEY = "tv-poker-player-session";

export type SavedPlayerSession = PlayerInfo & {
  playerId: string;
};

export function savePlayerSession(session: SavedPlayerSession) {
  localStorage.setItem(PLAYER_SESSION_KEY, JSON.stringify(session));
}

export function getSavedPlayerSession(): SavedPlayerSession | null {
  const rawSession = localStorage.getItem(PLAYER_SESSION_KEY);
  if (!rawSession) return null;

  try {
    const session = JSON.parse(rawSession) as SavedPlayerSession;

    if (!session.playerId || !session.roomId || !session.name || !session.spriteCode) {
      clearPlayerSession();
      return null;
    }

    return session;
  } catch {
    clearPlayerSession();
    return null;
  }
}

export function clearPlayerSession() {
  localStorage.removeItem(PLAYER_SESSION_KEY);
}
