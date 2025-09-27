import React, { useState } from "react";
import logo from "./logo.svg";
import styles from "./Game.module.scss";
import card from "../../assets/AC.png";
import { Actions } from "./components/Actions/Actions";

export const Game = () => {
  const [cardsShown, setCardsShown] = useState(false);

  return (
    <div className={styles.gameWrapper}>
      <h1 className={styles.title}>TV Poker</h1>

      <div className={styles.cardsTray}>
        <div className={styles.cardsWrapper}>
          <div className={styles.cardWrapper}>
            <img className={styles.cardImg} src={card} />
          </div>
          <div className={styles.cardWrapper}>
            <img className={styles.cardImg} src={card} />
          </div>
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
        <p className={styles.balanceTxt}>$1,500</p>
      </div>

      <Actions />
    </div>
  );
};
