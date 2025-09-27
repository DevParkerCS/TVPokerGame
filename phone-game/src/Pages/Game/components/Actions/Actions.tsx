import styles from "./Actions.module.scss";

export const Actions = () => {
  return (
    <div className={styles.actionsWrapper}>
      <button className={styles.actionBtn}>FOLD</button>
      <button className={styles.actionBtn}>CHECK</button>
      <button className={styles.actionBtn}>CALL</button>
      {/* <button className={styles.actionBtn}>RAISE</button> */}
    </div>
  );
};
