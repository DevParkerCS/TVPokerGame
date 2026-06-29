import { useEffect, useMemo, useState } from "react";
import { TurnStateType } from "../../../../Context/SocketContext";
import { PlayerActionType } from "../../../../types/Types";
import styles from "./Actions.module.scss";

type ActionsProps = {
  onAction: (action: PlayerActionType, amount?: number) => void;
  turnState?: TurnStateType | null;
};

function clampAmount(value: number, min: number, max: number) {
  if (Number.isNaN(value)) return min;
  return Math.min(Math.max(value, min), max);
}

export const Actions = ({ onAction, turnState }: ActionsProps) => {
  const isTurn = turnState?.isPlayerTurn ?? false;
  const canChooseAmount = isTurn && (!!turnState?.canBet || !!turnState?.canRaise);
  const minAmount = Math.max(0, turnState?.minRaiseTo ?? 0);
  const maxAmount = Math.max(minAmount, (turnState?.playerBet ?? 0) + (turnState?.balance ?? 0));
  const stepAmount = useMemo(() => {
    if (!turnState) return 1;
    return Math.max(1, turnState.minRaiseTo - turnState.currentBet || turnState.minRaiseTo || 1);
  }, [turnState]);
  const [actionAmount, setActionAmount] = useState(minAmount || stepAmount);

  useEffect(() => {
    setActionAmount(minAmount || stepAmount);
  }, [minAmount, stepAmount, turnState?.currentPlayerId]);

  const updateAmount = (value: number) => {
    setActionAmount(clampAmount(value, minAmount, maxAmount));
  };

  const amountAction: PlayerActionType = turnState?.canBet ? "bet" : "raise";
  const amountLabel = turnState?.canBet ? "BET" : "RAISE TO";

  return (
    <div className={styles.actionsWrapper}>
      <div className={styles.mainActions}>
        <button className={styles.actionBtn} disabled={!isTurn || !turnState?.canFold} onClick={() => onAction("fold")}>
          FOLD
        </button>
        <button className={styles.actionBtn} disabled={!isTurn || !turnState?.canCheck} onClick={() => onAction("check")}>
          CHECK
        </button>
        <button className={styles.actionBtn} disabled={!isTurn || !turnState?.canCall} onClick={() => onAction("call")}>
          {turnState?.amountToCall ? `CALL $${turnState.amountToCall}` : "CALL"}
        </button>
      </div>

      {canChooseAmount && (
        <div className={styles.amountActions}>
          <label className={styles.amountLabel}>{amountLabel}</label>
          <div className={styles.amountControls}>
            <button className={styles.amountBtn} onClick={() => updateAmount(actionAmount - stepAmount)}>
              -
            </button>
            <input
              className={styles.amountInput}
              type="number"
              min={minAmount}
              max={maxAmount}
              step={stepAmount}
              value={actionAmount}
              onChange={(e) => updateAmount(Number(e.target.value))}
            />
            <button className={styles.amountBtn} onClick={() => updateAmount(actionAmount + stepAmount)}>
              +
            </button>
          </div>
          <button
            className={styles.actionBtn}
            disabled={actionAmount < minAmount}
            onClick={() => onAction(amountAction, actionAmount)}
          >
            {amountLabel} ${actionAmount}
          </button>
        </div>
      )}
    </div>
  );
};
