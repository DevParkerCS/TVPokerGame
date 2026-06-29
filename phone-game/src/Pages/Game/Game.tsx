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

const suitAliases: Record<string, string> = {
  c: "C",
  club: "C",
  clubs: "C",
  d: "D",
  diamond: "D",
  diamonds: "D",
  h: "H",
  heart: "H",
  hearts: "H",
  s: "S",
  spade: "S",
  spades: "S",
};

const rankAliases: Record<string, string> = {
  a: "A",
  ace: "A",
  "1": "A",
  "01": "A",
  k: "K",
  king: "K",
  "13": "K",
  q: "Q",
  queen: "Q",
  "12": "Q",
  j: "J",
  jack: "J",
  "11": "J",
  t: "T",
  ten: "T",
  "10": "T",
  "2": "2",
  "02": "2",
  two: "2",
  "3": "3",
  "03": "3",
  three: "3",
  "4": "4",
  "04": "4",
  four: "4",
  "5": "5",
  "05": "5",
  five: "5",
  "6": "6",
  "06": "6",
  six: "6",
  "7": "7",
  "07": "7",
  seven: "7",
  "8": "8",
  "08": "8",
  eight: "8",
  "9": "9",
  "09": "9",
  nine: "9",
};

function normalizeToken(value: string) {
  return value.toLowerCase().trim();
}

function normalizeRank(value: string) {
  return rankAliases[normalizeToken(value)];
}

function normalizeSuit(value: string) {
  return suitAliases[normalizeToken(value)];
}

function compactCodeFromText(value: string) {
  const compact = value.replace(/[^a-z0-9]/gi, "").toUpperCase();
  const rankFirst = compact.match(/^(A|K|Q|J|T|10|[2-9])(C|D|H|S)$/);
  if (rankFirst) return `${rankFirst[1]}${rankFirst[2]}`;

  const suitFirst = compact.match(/^(C|D|H|S)(A|K|Q|J|T|10|[2-9])$/);
  if (suitFirst) return `${suitFirst[2]}${suitFirst[1]}`;

  return undefined;
}

function codeFromAssetPath(path: string) {
  const withoutExtension = path.replace(/^\.\//, "").replace(/\.[^/.]+$/, "");
  const pathParts = withoutExtension.split("/");
  const basename = pathParts[pathParts.length - 1];

  const directCode = compactCodeFromText(basename) ?? compactCodeFromText(withoutExtension);
  if (directCode) return directCode;

  const tokens = withoutExtension
    .split(/[^a-z0-9]+/i)
    .map(normalizeToken)
    .filter(Boolean);

  const rank = tokens.map(normalizeRank).find(Boolean);
  const suit = tokens.map(normalizeSuit).find(Boolean);

  return rank && suit ? `${rank}${suit}` : undefined;
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
