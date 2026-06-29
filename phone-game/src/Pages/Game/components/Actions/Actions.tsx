import { PlayerActionType } from "../../../../types/Types";
import styles from "./Actions.module.scss";

type ActionsProps = {
  onAction: (action: PlayerActionType, amount?: number) => void;
};

export const Actions = ({ onAction }: ActionsProps) => {
  return (
    <div className={styles.actionsWrapper}>
      <button className={styles.actionBtn} onClick={() => onAction("fold")}>
        FOLD
      </button>
      <button className={styles.actionBtn} onClick={() => onAction("check")}>
        CHECK
      </button>
      <button className={styles.actionBtn} onClick={() => onAction("call")}>
        CALL
      </button>
      <button className={styles.actionBtn} onClick={() => onAction("raise", 100)}>
        RAISE
      </button>
    </div>
  );
};
