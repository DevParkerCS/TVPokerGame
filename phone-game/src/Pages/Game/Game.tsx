import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import styles from "./Game.module.scss";
import { Actions } from "./components/Actions/Actions";
import { CardPayload, createEmptyGameState, useSocket } from "../../Context/SocketContext";
import { JoinRoom, LeaveTable, SendPlayerAction } from "../../util/SocketUtil";
import { PlayerActionType } from "../../types/Types";
import {
  clearPlayerSession,
  getSavedPlayerSession,
  savePlayerSession,
} from "../../util/PlayerSessionStorage";

const cardImageContext = require.context(
  "../../assets",
  true,
  /\.(png|jpe?g|webp)$/
);

const indexedRanks = ["A", "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K"];
const suits = ["C", "D", "H", "S"];

function codeFromIndexedAsset(path: string) {
  const withoutExtension = path.replace(/^\.\//, "").replace(/\.[^/.]+$/, "");
  const parts = withoutExtension.split("/");
  const fileName = parts[parts.length - 1];
  const folderName = parts.length > 1 ? parts[parts.length - 2].toUpperCase() : "";

  const fileWithSuit = fileName.match(/^(\d\d)_([CDHS])$/i);
  if (fileWithSuit) {
    const rank = indexedRanks[Number(fileWithSuit[1])];
    const suit = fileWithSuit[2].toUpperCase();
    return rank && suits.includes(suit) ? `${rank}${suit}` : undefined;
  }

  const fileInSuitFolder = fileName.match(/^(\d\d)$/);
  if (fileInSuitFolder) {
    const rank = indexedRanks[Number(fileInSuitFolder[1])];
    return rank && suits.includes(folderName) ? `${rank}${folderName}` : undefined;
  }

  return undefined;
}

function codeFromCompactName(path: string) {
  const withoutExtension = path.replace(/^\.\//, "").replace(/\.[^/.]+$/, "");
  const fileName = withoutExtension.split("/").pop() ?? "";
  const compact = fileName.replace(/[^a-z0-9]/gi, "").toUpperCase();

  if (compact.length === 2) {
    const first = compact[0];
    const second = compact[1];
    if (suits.includes(second)) return `${first}${second}`;
    if (suits.includes(first)) return `${second}${first}`;
  }

  return undefined;
}

function codeFromAssetPath(path: string) {
  return codeFromIndexedAsset(path) ?? codeFromCompactName(path);
}

const cardImages: Record<string, string> = cardImageContext.keys().reduce(
  (acc: Record<string, string>, key: string) => {
    const code = codeFromAssetPath(key);
    if (code) {
      acc[code] = cardImageContext(key) as string;
    }
    return acc;
  },
  {}
);

const CardView = ({ card }: { card: CardPayload }) => {
  const code = card.code.toUpperCase();
  const image = cardImages[code];

  return (
    <div className={styles.cardWrapper}>
      {image ? (
        <img className={styles.cardImg} src={image} alt={code} />
      ) : (
        <div className={styles.cardFallback}>{code}</div>
      )}
    </div>
  );
};

function isAllowedAction(action: PlayerActionType, turnState: ReturnType<typeof useSocket>["turnState"]) {
  if (!turnState?.isPlayerTurn) return false;
  if (action === "fold") return turnState.canFold;
  if (action === "check") return turnState.canCheck;
  if (action === "call") return turnState.canCall;
  if (action === "bet") return turnState.canBet;
  if (action === "raise") return turnState.canRaise;
  return false;
}

export const Game = () => {
  const navigate = useNavigate();
  const [cardsShown, setCardsShown] = useState(false);
  const [status, setStatus] = useState("");
  const [actionLocked, setActionLocked] = useState(false);
  const [hasTriedReconnect, setHasTriedReconnect] = useState(false);
  const {
    gameState,
    roomId,
    socket,
    holeCards,
    turnState,
    setRoomId,
    setGameState,
    setHoleCards,
    setTurnState,
  } = useSocket();

  useEffect(() => {
    if (gameState.playerId || hasTriedReconnect) return;

    const savedSession = getSavedPlayerSession();
    if (!savedSession) return;

    setHasTriedReconnect(true);
    setStatus("Reconnecting...");

    JoinRoom(savedSession, socket)
      .then((gameData) => {
        setRoomId(savedSession.roomId);
        setGameState(gameData);
        savePlayerSession({
          ...savedSession,
          playerId: gameData.playerId,
        });
        setStatus("Reconnected");
      })
      .catch(() => {
        clearPlayerSession();
        setStatus("Session expired. Rejoin from the TV room code.");
      });
  }, [gameState.playerId, hasTriedReconnect, setGameState, setRoomId, socket]);

  useEffect(() => {
    setActionLocked(false);
  }, [turnState]);

  useEffect(() => {
    const onHandEnded = (payload?: { message?: string }) => {
      setActionLocked(false);
      setStatus(payload?.message || "Hand complete");
    };
    const onHandReset = (payload?: { message?: string }) => {
      setActionLocked(false);
      setCardsShown(false);
      setStatus(payload?.message || "Waiting for new hand");
    };
    const onHandStarted = (payload?: { message?: string }) => {
      setActionLocked(false);
      setCardsShown(false);
      setStatus(payload?.message || "New hand started");
    };

    socket.on("hand-ended", onHandEnded);
    socket.on("hand-reset", onHandReset);
    socket.on("hand-started", onHandStarted);

    return () => {
      socket.off("hand-ended", onHandEnded);
      socket.off("hand-reset", onHandReset);
      socket.off("hand-started", onHandStarted);
    };
  }, [socket]);

  useEffect(() => {
    if (holeCards.length > 0) {
      setStatus("");
    }
  }, [holeCards.length]);

  const resetPhoneState = () => {
    clearPlayerSession();
    setRoomId("");
    setHoleCards([]);
    setTurnState(null);
    setGameState(createEmptyGameState());
    setCardsShown(false);
    setActionLocked(false);
    setHasTriedReconnect(true);
    setStatus("");
  };

  const handleLeaveTable = async () => {
    const currentRoomId = roomId;
    const currentPlayerId = gameState.playerId;

    resetPhoneState();

    if (currentRoomId && currentPlayerId) {
      try {
        await LeaveTable(currentRoomId, currentPlayerId, socket);
      } catch {
        // Local session clear is the important part. Server-side removal may fail if the room is gone.
      }
    }

    navigate("/");
  };

  const handleAction = async (action: PlayerActionType, amount?: number) => {
    if (actionLocked) {
      setStatus("Action already sent");
      return;
    }

    if (!isAllowedAction(action, turnState)) {
      setStatus(turnState?.currentPlayerName ? `Waiting for ${turnState.currentPlayerName}` : "Waiting for turn state");
      return;
    }

    const actionAmount = action === "bet" || action === "raise" ? amount ?? turnState?.minRaiseTo : amount;
    setActionLocked(true);

    try {
      await SendPlayerAction(roomId, gameState.playerId, action, actionAmount, socket);
      setStatus(`${action.toUpperCase()} sent`);
    } catch (e) {
      setActionLocked(false);
      setStatus("Could not send action");
    }
  };

  return (
    <div className={styles.gameWrapper}>
      <h1 className={styles.title}>TV Poker</h1>
      <p>Room: {roomId}</p>
      <p>{turnState?.isPlayerTurn ? "Your turn" : turnState?.currentPlayerName ? `Waiting for ${turnState.currentPlayerName}` : "Waiting for game state"}</p>
      {turnState && (
        <p>
          Pot: ${turnState.pot} | To call: ${turnState.amountToCall}
        </p>
      )}

      <div className={styles.cardsTray}>
        <div className={styles.cardsWrapper}>
          {cardsShown && holeCards.length > 0 &&
            holeCards.map((card) => <CardView key={card.code} card={card} />)}
          {cardsShown && holeCards.length === 0 && (
            <p className={styles.waitingTxt}>Cards have not been dealt yet.</p>
          )}
        </div>

        <button
          className={styles.showBtn}
          onClick={() => setCardsShown(!cardsShown)}
        >
          {cardsShown ? "Hide Cards" : "Show Cards"}
        </button>
      </div>

      <div className={styles.balanceWrapper}>
        <p className={styles.balanceTitle}>BALANCE</p>
        <p className={styles.balanceTxt}>${gameState.balance.toLocaleString()}</p>
      </div>

      <Actions onAction={handleAction} turnState={turnState} locked={actionLocked} />
      {status && <p>{status}</p>}
      <button className={styles.leaveBtn} onClick={handleLeaveTable}>
        Leave Table
      </button>
    </div>
  );
};
