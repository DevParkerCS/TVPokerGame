import React, { useState } from "react";
import styles from "./Game.module.scss";
import card from "../../assets/AC.png";
import { Actions } from "./components/Actions/Actions";
import { useSocket } from "../../Context/SocketContext";
import { SendPlayerAction } from "../../util/SocketUtil";
import { PlayerActionType } from "../../types/Types";

export const Game = () => {
  const [cardsShown, setCardsShown] = useState(false);
  const [status, setStatus] = useState("");
  const { gameState, roomId, socket } = useSocket();

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
          {cardsShown && (
            <>
              <div className={styles.cardWrapper}>
                <img className={styles.cardImg} src={card} alt="card one" />
              </div>
              <div className={styles.cardWrapper}>
                <img className={styles.cardImg} src={card} alt="card two" />
              </div>
            </>
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
