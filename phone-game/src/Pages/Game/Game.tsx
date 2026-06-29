import React, { useState } from "react";
import styles from "./Game.module.scss";
import { Actions } from "./components/Actions/Actions";
import { CardPayload, useSocket } from "../../Context/SocketContext";
import { SendPlayerAction } from "../../util/SocketUtil";
import { PlayerActionType } from "../../types/Types";

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
  const [cardsShown, setCardsShown] = useState(false);
  const [status, setStatus] = useState("");
  const { gameState, roomId, socket, holeCards, turnState } = useSocket();

  const handleAction = async (action: PlayerActionType, amount?: number) => {
    if (!isAllowedAction(action, turnState)) {
      setStatus(turnState?.currentPlayerName ? `Waiting for ${turnState.currentPlayerName}` : "Waiting for turn state");
      return;
    }

    const actionAmount = action === "raise" ? turnState?.minRaiseTo ?? amount : amount;

    try {
      await SendPlayerAction(roomId, gameState.playerId, action, actionAmount, socket);
      setStatus(`${action.toUpperCase()} sent`);
    } catch (e) {
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

      <Actions onAction={handleAction} turnState={turnState} />
      {status && <p>{status}</p>}
    </div>
  );
};
