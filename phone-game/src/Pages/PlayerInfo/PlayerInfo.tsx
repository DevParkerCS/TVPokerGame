import { useState } from "react";
import { useSocket } from "../../Context/SocketContext";
import styles from "./PlayerInfo.module.scss";
import { Avatars } from "./components/Avatars/Avatars";
import { ColorPicker } from "./components/ColorPicker/ColorPicker";
import { JoinRoom } from "../../util/SocketUtil";

export type PlayerInfo = {
  name: string;
  spriteCode: string;
  color: ColorType;
};

export type ColorType = {
  r: number;
  g: number;
  b: number;
};

export const PlayerInfo = () => {
  const socketContext = useSocket();
  const [stepIndex, setStepIndex] = useState(0);
  const [playerInfo, setPlayerInfo] = useState<PlayerInfo>({
    name: "",
    spriteCode: "",
    color: { r: 0, g: 168, b: 132 },
  });
  const [selectedIndex, setSelectedIndex] = useState(-1);

  const handleClick = async () => {
    switch (stepIndex) {
      case 0:
        if (playerInfo.spriteCode !== "") {
          setStepIndex(stepIndex + 1);
        }
        break;
      case 1:
        if (playerInfo.name !== "" && playerInfo.color) {
          try {
            const gameData = await JoinRoom(playerInfo, socketContext.socket);
            if (gameData) {
              socketContext.setGameState(gameData);
            }
          } catch (e) {
            console.log("Error Joining Room.  Please Try Again");
          }
        }
        break;
      case 2:
    }
  };

  const handlePlayerNameChange = () => {};

  return (
    <div className={styles.contentWrapper}>
      <div className={styles.txtWrapper}>
        <h1 className={styles.joinTitle}>Join TV Poker</h1>
        <p className={styles.playersTxt}>Players: 3/10</p>
      </div>

      {stepIndex === 0 && (
        <div className={styles.chooseAvatarWrapper}>
          <h2 className={styles.avatarTitle}>Choose Avatar</h2>
          <Avatars
            setPlayerInfo={setPlayerInfo}
            selectedIndex={selectedIndex}
            setSelectedIndex={setSelectedIndex}
          />
        </div>
      )}

      {stepIndex === 1 && (
        <ColorPicker setPlayerInfo={setPlayerInfo} playerInfo={playerInfo} />
      )}

      <div className={styles.btnsWrapper}>
        {stepIndex === 1 && (
          <button
            className={styles.continueBtn}
            onClick={() => setStepIndex(stepIndex - 1)}
          >
            Previous
          </button>
        )}
        <button className={styles.continueBtn} onClick={handleClick}>
          {stepIndex === 1 ? "Join" : "Continue"}
        </button>
      </div>
    </div>
  );
};
