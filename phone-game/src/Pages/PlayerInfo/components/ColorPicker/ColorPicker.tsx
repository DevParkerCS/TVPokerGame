import { useState } from "react";
import { RgbColorPicker, type RgbColor } from "react-colorful";
import styles from "./ColorPicker.module.scss";
import { PlayerInfo } from "../../PlayerInfo";
import { avatarByName } from "../../../../assets/avatars";

type ColorPickerProps = {
  setPlayerInfo: React.Dispatch<React.SetStateAction<PlayerInfo>>;
  playerInfo: PlayerInfo;
};

export const ColorPicker = ({
  playerInfo,
  setPlayerInfo,
}: ColorPickerProps) => {
  const handleColorChange = (color: RgbColor) => {
    setPlayerInfo({ ...playerInfo, color });
  };

  return (
    <div className={styles.contentWrapper}>
      <h2 className={styles.colorTitle}>Choose Color</h2>
      <div className={styles.flexWrapper}>
        <div className={styles.colorInfoWrapper}>
          <div className={styles.pickerBox}>
            <RgbColorPicker
              className={styles.colorPicker}
              color={playerInfo.color}
              onChange={handleColorChange}
            />
          </div>
          <div
            className={styles.previewBox}
            style={{
              backgroundColor: `rgb(${playerInfo.color.r},${playerInfo.color.g},${playerInfo.color.b})`,
            }}
          >
            <img
              src={
                avatarByName[playerInfo.spriteCode] ?? avatarByName["default"]
              }
              alt=""
            />
          </div>
        </div>

        <div className={styles.inputWrapper}>
          <label className={styles.inputLabel} htmlFor="name-input">
            Name:
          </label>
          <input
            className={styles.nameInput}
            onChange={(e) =>
              setPlayerInfo({ ...playerInfo, name: e.target.value })
            }
            id="name-input"
          ></input>
        </div>
      </div>
    </div>
  );
};
