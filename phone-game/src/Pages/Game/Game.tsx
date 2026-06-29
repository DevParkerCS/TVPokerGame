import React, { useState } from "react";
import styles from "./Game.module.scss";
import { Actions } from "./components/Actions/Actions";
import { CardPayload, useSocket } from "../../Context/SocketContext";
import { SendPlayerAction } from "../../util/SocketUtil";
import { PlayerActionType } from "../../types/Types";

const cardImageContext = require.context("../../assets", false, /\.png$/);
const cardImages: Record<string, string> = cardImageContext.keys().reduce(
  (acc: Record<string, string>, key: string) => {
    const code = key.replace("./", "").replace(/\.[^/.]+$/, "").toUpperCase();
    acc[code] = cardImageContext(key) as string;
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

export const Game = () => {
  const [cardsShown, setCardsShown] = useState(false);
  const [status, setStatus] = useState("");
  const { gameState, roomId, socket, holeCards } = useSocket();

  const handleAction = async (action: PlayerActionType, amount?: number) => {
    try {
      await SendPlayerAction(roomId, gameState.playerId, action, amount, socket);
      setStatus(`${action.toUpperCase()} sent`);
    } catch (e) {
      setStatus("Could not send action");
    }
  };

  return (
    <div className={styles.gameWrapper}>
      <h1 className={styles.title}>TV Poker</h1>
      <p>Room: {roomId}</p>

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

      <Actions onAction={handleAction} />
      {status && <p>{status}</p>}
    </div>
  );
};
