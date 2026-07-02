import type { Dispatch, SetStateAction } from "react";
import { RgbColorPicker, type RgbColor } from "react-colorful";
import styles from "./ColorPicker.module.scss";
import { PlayerInfo } from "../../PlayerInfo";
import { avatarByName } from "../../../../assets/avatars";

type ColorPickerProps = {
  setPlayerInfo: Dispatch<SetStateAction<PlayerInfo>>;
  playerInfo: PlayerInfo;
};

export const ColorPicker = ({
  playerInfo,
  setPlayerInfo,
}: ColorPickerProps) => {
  const handleColorChange = (color: RgbColor) => {
    setPlayerInfo({ ...playerInfo, color });
  };

  const rgbValue = `rgb(${playerInfo.color.r}, ${playerInfo.color.g}, ${playerInfo.color.b})`;

  return (
    <div className={styles.contentWrapper}>
      <div className={styles.previewColumn}>
        <div
          className={styles.previewBox}
          style={{ backgroundColor: rgbValue }}
        >
          <img
            src={avatarByName[playerInfo.spriteCode] ?? avatarByName["default"]}
            alt="Selected avatar preview"
          />
        </div>
        <div className={styles.colorReadout}>
          <span>Table color</span>
          <strong>{rgbValue}</strong>
        </div>
      </div>

      <div className={styles.controlsColumn}>
        <div className={styles.pickerBox}>
          <RgbColorPicker
            className={styles.colorPicker}
            color={playerInfo.color}
            onChange={handleColorChange}
          />
        </div>

        <div className={styles.inputWrapper}>
          <label className={styles.inputLabel} htmlFor="name-input">
            Player name
          </label>
          <input
            className={styles.nameInput}
            value={playerInfo.name}
            placeholder="Big Slick"
            maxLength={18}
            onChange={(e) =>
              setPlayerInfo({ ...playerInfo, name: e.target.value })
            }
            id="name-input"
          />
        </div>
      </div>
    </div>
  );
};
