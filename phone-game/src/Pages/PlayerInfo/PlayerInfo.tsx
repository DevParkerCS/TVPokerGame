import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useSocket } from "../../Context/SocketContext";
import styles from "./PlayerInfo.module.scss";
import { Avatars } from "./components/Avatars/Avatars";
import { ColorPicker } from "./components/ColorPicker/ColorPicker";
import { JoinRoom } from "../../util/SocketUtil";
import { savePlayerSession } from "../../util/PlayerSessionStorage";

const GAME_ALREADY_STARTED_ERROR = "Game has already started";

export type PlayerInfo = {
  roomId: string;
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
  const navigate = useNavigate();
  const [stepIndex, setStepIndex] = useState(0);
  const [error, setError] = useState("");
  const [playerInfo, setPlayerInfo] = useState<PlayerInfo>({
    roomId: "",
    name: "",
    spriteCode: "",
    color: { r: 0, g: 168, b: 132 },
  });
  const [selectedIndex, setSelectedIndex] = useState(-1);

  const handleClick = async () => {
    setError("");

    switch (stepIndex) {
      case 0:
        if (playerInfo.spriteCode !== "") {
          setStepIndex(stepIndex + 1);
        } else {
          setError("Choose an avatar first.");
        }
        break;
      case 1:
        if (playerInfo.name !== "" && playerInfo.roomId !== "") {
          try {
            const cleanedInfo = {
              ...playerInfo,
              roomId: playerInfo.roomId.trim().toUpperCase(),
              name: playerInfo.name.trim(),
            };
            const gameData = await JoinRoom(cleanedInfo, socketContext.socket);
            socketContext.setRoomId(cleanedInfo.roomId);
            socketContext.setGameState(gameData);
            savePlayerSession({
              ...cleanedInfo,
              playerId: gameData.playerId,
            });
            navigate("/game");
          } catch (e) {
            const message = e instanceof Error ? e.message : "";
            if (message === GAME_ALREADY_STARTED_ERROR) {
              setError("This game has already started. Wait for the next game to join.");
            } else {
              setError("Could not join room. Check the code and try again.");
            }
          }
        } else {
          setError("Enter your name and the room code from the TV.");
        }
        break;
    }
  };

  return (
    <div className={styles.contentWrapper}>
      <div className={styles.txtWrapper}>
        <h1 className={styles.joinTitle}>Join TV Poker</h1>
        <p className={styles.playersTxt}>Enter the room code shown on the TV.</p>
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
        <>
          <ColorPicker setPlayerInfo={setPlayerInfo} playerInfo={playerInfo} />
          <div className={styles.inputWrapper}>
            <label className={styles.inputLabel} htmlFor="room-input">
              Room Code:
            </label>
            <input
              className={styles.nameInput}
              value={playerInfo.roomId}
              onChange={(e) =>
                setPlayerInfo({
                  ...playerInfo,
                  roomId: e.target.value.toUpperCase(),
                })
              }
              id="room-input"
            />
          </div>
        </>
      )}

      {error && <p className={styles.playersTxt}>{error}</p>}

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