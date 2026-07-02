import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import styles from "./Game.module.scss";
import { Actions } from "./components/Actions/Actions";
import { createEmptyGameState, TurnStateType, useSocket } from "../../Context/SocketContext";
import { JoinRoom, LeaveTable, SendPlayerAction } from "../../util/SocketUtil";
import { PlayerActionType } from "../../types/Types";
import {
  clearPlayerSession,
  getSavedPlayerSession,
  savePlayerSession,
} from "../../util/PlayerSessionStorage";
import { PhoneShell } from "../../components/PhoneShell/PhoneShell";
import { ActionButton } from "../../components/ActionButton/ActionButton";
import { CardsPanel } from "./components/CardsPanel/CardsPanel";
import { GameStats } from "./components/GameStats/GameStats";
import { StatusBanner } from "./components/StatusBanner/StatusBanner";

function isAllowedAction(action: PlayerActionType, turnState: TurnStateType | null) {
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
    } catch {
      setActionLocked(false);
      setStatus("Could not send action");
    }
  };

  const footer = (
    <div className={styles.footerActions}>
      <ActionButton variant="ghost" onClick={handleLeaveTable}>
        Leave Table
      </ActionButton>
    </div>
  );

  return (
    <PhoneShell
      variant="game"
      title="Table control"
      subtitle="Keep your cards private, watch the TV table, and make your move when the action hits you."
      footer={footer}
    >
      <div className={styles.gameContent}>
        <GameStats roomId={roomId} balance={gameState.balance} turnState={turnState} />
        <StatusBanner message={status} isPlayerTurn={turnState?.isPlayerTurn} />
        <div className={styles.tableGrid}>
          <CardsPanel
            cards={holeCards}
            cardsShown={cardsShown}
            onToggleCards={() => setCardsShown(!cardsShown)}
          />
          <Actions onAction={handleAction} turnState={turnState} locked={actionLocked} />
        </div>
      </div>
    </PhoneShell>
  );
};
