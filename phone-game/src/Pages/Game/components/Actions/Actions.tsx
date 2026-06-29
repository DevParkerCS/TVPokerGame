import { TurnStateType } from "../../../../Context/SocketContext";
import { PlayerActionType } from "../../../../types/Types";
import styles from "./Actions.module.scss";

type ActionsProps = {
  onAction: (action: PlayerActionType, amount?: number) => void;
  turnState?: TurnStateType | null;
};

export const Actions = ({ onAction, turnState }: ActionsProps) => {
  const isTurn = turnState?.isPlayerTurn ?? false;
  const raiseTo = turnState?.minRaiseTo ?? 100;

  return (
    <div className={styles.actionsWrapper}>
      <button className={styles.actionBtn} disabled={!isTurn || !turnState?.canFold} onClick={() => onAction("fold")}>
        FOLD
      </button>
      <button className={styles.actionBtn} disabled={!isTurn || !turnState?.canCheck} onClick={() => onAction("check")}>
        CHECK
      </button>
      <button className={styles.actionBtn} disabled={!isTurn || !turnState?.canCall} onClick={() => onAction("call")}>
        {turnState?.amountToCall ? `CALL $${turnState.amountToCall}` : "CALL"}
      </button>
      <button className={styles.actionBtn} disabled={!isTurn || !turnState?.canRaise} onClick={() => onAction("raise", raiseTo)}>
        RAISE
      </button>
    </div>
  );
};
