import styles from "./Avatars.module.scss";
import { avatarImgs } from "../../../../assets/avatars";
import { PlayerInfo } from "../../PlayerInfo";

type AvatarsProps = {
  setPlayerInfo: React.Dispatch<React.SetStateAction<PlayerInfo>>;
  setSelectedIndex: React.Dispatch<React.SetStateAction<number>>;
  selectedIndex: number;
};

export const Avatars = ({
  setPlayerInfo,
  selectedIndex,
  setSelectedIndex,
}: AvatarsProps) => {
  const handleClick = (i: number, code: string) => {
    const isSelected = selectedIndex === i;
    setSelectedIndex(isSelected ? -1 : i);
    setPlayerInfo((prev) => ({
      ...prev,
      spriteCode: isSelected ? "" : code,
    }));
  };

  return (
    <div className={styles.avatarsWrapper}>
      {avatarImgs.map(({ name, src }, i) => (
        <button
          type="button"
          key={name}
          className={`${styles.avatarWrapper} ${
            selectedIndex === i ? styles.selected : ""
          }`}
          onClick={() => handleClick(i, name)}
          aria-pressed={selectedIndex === i}
          aria-label={`Choose ${name} avatar`}
        >
          <img className={styles.avatarImg} src={src} alt="" />
        </button>
      ))}
    </div>
  );
};
