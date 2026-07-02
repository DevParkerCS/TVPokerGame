import { TurnStateType } from "../../../../Context/SocketContext";
import styles from "./GameStats.module.scss";

type GameStatsProps = {
  roomId: string;
  balance: number;
  turnState: TurnStateType | null;
};

export const GameStats = ({ roomId, balance, turnState }: GameStatsProps) => {
  const turnText = turnState?.isPlayerTurn
    ? "Your turn"
    : turnState?.currentPlayerName
      ? `Waiting for ${turnState.currentPlayerName}`
      : "Waiting for game state";

  return (
    <section className={styles.statsGrid}>
      <div className={`${styles.statCard} ${turnState?.isPlayerTurn ? styles.hot : ""}`}>
        <span>Status</span>
        <strong>{turnText}</strong>
      </div>
      <div className={styles.statCard}>
        <span>Balance</span>
        <strong>${balance.toLocaleString()}</strong>
      </div>
      <div className={styles.statCard}>
        <span>Pot</span>
        <strong>${(turnState?.pot ?? 0).toLocaleString()}</strong>
      </div>
      <div className={styles.statCard}>
        <span>To call</span>
        <strong>${(turnState?.amountToCall ?? 0).toLocaleString()}</strong>
      </div>
      <div className={styles.roomCard}>
        <span>Room</span>
        <strong>{roomId || "—"}</strong>
      </div>
    </section>
  );
};
