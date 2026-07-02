import { useEffect, useMemo, useState } from "react";
import { TurnStateType } from "../../../../Context/SocketContext";
import { PlayerActionType } from "../../../../types/Types";
import { ActionButton } from "../../../../components/ActionButton/ActionButton";
import styles from "./Actions.module.scss";

type ActionsProps = {
  onAction: (action: PlayerActionType, amount?: number) => void;
  turnState?: TurnStateType | null;
  locked?: boolean;
};

function clampAmount(value: number, min: number, max: number) {
  if (Number.isNaN(value)) return min;
  return Math.min(Math.max(value, min), max);
}

export const Actions = ({ onAction, turnState, locked = false }: ActionsProps) => {
  const isTurn = (turnState?.isPlayerTurn ?? false) && !locked;
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
  const amountLabel = turnState?.canBet ? "Bet" : "Raise to";

  return (
    <section className={styles.actionsWrapper}>
      <div className={styles.actionsHeader}>
        <p>Controls</p>
        <h2>{turnState?.isPlayerTurn ? "Make your move" : "Waiting"}</h2>
      </div>

      <div className={styles.mainActions}>
        <ActionButton variant="danger" disabled={!isTurn || !turnState?.canFold} onClick={() => onAction("fold")}>
          Fold
        </ActionButton>
        <ActionButton variant="secondary" disabled={!isTurn || !turnState?.canCheck} onClick={() => onAction("check")}>
          Check
        </ActionButton>
        <ActionButton disabled={!isTurn || !turnState?.canCall} onClick={() => onAction("call")}>
          {turnState?.amountToCall ? `Call $${turnState.amountToCall}` : "Call"}
        </ActionButton>
      </div>

      {canChooseAmount && (
        <div className={styles.amountActions}>
          <div className={styles.amountTopline}>
            <span>{amountLabel}</span>
            <strong>${actionAmount.toLocaleString()}</strong>
          </div>
          <div className={styles.amountControls}>
            <button className={styles.amountBtn} disabled={locked} onClick={() => updateAmount(actionAmount - stepAmount)}>
              −
            </button>
            <input
              className={styles.amountInput}
              type="number"
              min={minAmount}
              max={maxAmount}
              step={stepAmount}
              disabled={locked}
              value={actionAmount}
              onChange={(e) => updateAmount(Number(e.target.value))}
            />
            <button className={styles.amountBtn} disabled={locked} onClick={() => updateAmount(actionAmount + stepAmount)}>
              +
            </button>
          </div>
          <ActionButton
            fullWidth
            disabled={locked || actionAmount < minAmount}
            onClick={() => onAction(amountAction, actionAmount)}
          >
            {amountLabel} ${actionAmount.toLocaleString()}
          </ActionButton>
        </div>
      )}
    </section>
  );
};
