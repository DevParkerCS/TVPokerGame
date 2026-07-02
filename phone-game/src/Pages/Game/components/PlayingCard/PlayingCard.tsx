import { CardPayload } from "../../../../Context/SocketContext";
import { getCardImage } from "../../../../util/cardImages";
import styles from "./PlayingCard.module.scss";

type PlayingCardProps = {
  card: CardPayload;
};

export const PlayingCard = ({ card }: PlayingCardProps) => {
  const code = card.code.toUpperCase();
  const image = getCardImage(code);
  const isRed = code.includes("H") || code.includes("D");

  return (
    <div className={`${styles.card} ${isRed ? styles.red : styles.black}`}>
      {image ? (
        <img className={styles.cardImg} src={image} alt={code} />
      ) : (
        <div className={styles.cardFallback}>{code}</div>
      )}
    </div>
  );
};
