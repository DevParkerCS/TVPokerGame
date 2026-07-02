import styles from "./StatusBanner.module.scss";

type StatusBannerProps = {
  message: string;
  isPlayerTurn?: boolean;
};

export const StatusBanner = ({ message, isPlayerTurn = false }: StatusBannerProps) => {
  if (!message) return null;

  return (
    <div className={`${styles.banner} ${isPlayerTurn ? styles.turn : ""}`}>
      <span>{isPlayerTurn ? "Action" : "Update"}</span>
      <p>{message}</p>
    </div>
  );
};
