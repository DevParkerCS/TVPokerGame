import styles from "./Avatars.module.scss";
import { avatarImgs } from "../../../../assets/avatars";
import { useState } from "react";
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
    setSelectedIndex(selectedIndex !== i ? i : -1);
    console.log(code);
    setPlayerInfo((prev) => ({
      ...prev,
      spriteCode: selectedIndex !== i ? code : "",
    }));
  };

  return (
    <div className={styles.avatarsWrapper}>
      {avatarImgs.map(({ name, src }, i) => (
        <div
          key={name}
          className={`${styles.avatarWrapper} ${
            selectedIndex === i ? styles.selected : ""
          }`}
          onClick={() => handleClick(i, name)}
        >
          <img className={styles.avatarImg} src={src} alt={name} />
        </div>
      ))}
    </div>
  );
};
