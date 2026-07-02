import { CardPayload } from "../../../../Context/SocketContext";
import { ActionButton } from "../../../../components/ActionButton/ActionButton";
import { PlayingCard } from "../PlayingCard/PlayingCard";
import styles from "./CardsPanel.module.scss";

type CardsPanelProps = {
  cards: CardPayload[];
  cardsShown: boolean;
  onToggleCards: () => void;
};

export const CardsPanel = ({ cards, cardsShown, onToggleCards }: CardsPanelProps) => {
  const hasCards = cards.length > 0;

  return (
    <section className={styles.cardsPanel}>
      <div className={styles.panelHeader}>
        <div>
          <p>Your hand</p>
          <h2>{cardsShown ? "Hole cards" : "Cards hidden"}</h2>
        </div>
        <span className={styles.privacyBadge}>{cardsShown ? "Private" : "Tap to peek"}</span>
      </div>

      <div className={`${styles.cardStage} ${cardsShown ? styles.revealed : ""}`}>
        {cardsShown && hasCards && cards.map((card) => <PlayingCard key={card.code} card={card} />)}
        {cardsShown && !hasCards && <p className={styles.emptyText}>Cards have not been dealt yet.</p>}
        {!cardsShown && (
          <div className={styles.cardBacks} aria-hidden="true">
            <span />
            <span />
          </div>
        )}
      </div>

      <ActionButton fullWidth variant="secondary" onClick={onToggleCards}>
        {cardsShown ? "Hide Cards" : "Show Cards"}
      </ActionButton>
    </section>
  );
};
