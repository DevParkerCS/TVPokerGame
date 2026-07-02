import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useSocket } from "../../Context/SocketContext";
import styles from "./PlayerInfo.module.scss";
import { Avatars } from "./components/Avatars/Avatars";
import { ColorPicker } from "./components/ColorPicker/ColorPicker";
import { JoinRoom } from "../../util/SocketUtil";
import { savePlayerSession } from "../../util/PlayerSessionStorage";
import { PhoneShell } from "../../components/PhoneShell/PhoneShell";
import { ActionButton } from "../../components/ActionButton/ActionButton";

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

  const isAvatarStep = stepIndex === 0;
  const title = isAvatarStep ? "Pick your table vibe" : "Lock in your seat";
  const subtitle = isAvatarStep
    ? "Choose an avatar that will show up on the TV table. Big personality encouraged."
    : "Choose your color, enter your name, then punch in the room code from the TV.";

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

  const footer = (
    <div className={styles.footerActions}>
      {!isAvatarStep && (
        <ActionButton variant="ghost" onClick={() => setStepIndex(stepIndex - 1)}>
          Back
        </ActionButton>
      )}
      <ActionButton fullWidth={isAvatarStep} onClick={handleClick}>
        {isAvatarStep ? "Choose Avatar" : "Join Table"}
      </ActionButton>
    </div>
  );

  return (
    <PhoneShell title={title} subtitle={subtitle} footer={footer}>
      <div className={styles.joinContent}>
        <div className={styles.progressRail} aria-label="Join progress">
          <span className={styles.activeDot}>Avatar</span>
          <span className={!isAvatarStep ? styles.activeDot : ""}>Seat</span>
        </div>

        {isAvatarStep ? (
          <section className={styles.panel}>
            <div className={styles.sectionHeader}>
              <p>Step 1</p>
              <h2>Choose your character</h2>
            </div>
            <Avatars
              setPlayerInfo={setPlayerInfo}
              selectedIndex={selectedIndex}
              setSelectedIndex={setSelectedIndex}
            />
          </section>
        ) : (
          <section className={styles.panel}>
            <div className={styles.sectionHeader}>
              <p>Step 2</p>
              <h2>Make it yours</h2>
            </div>
            <ColorPicker setPlayerInfo={setPlayerInfo} playerInfo={playerInfo} />
            <div className={styles.roomCard}>
              <label className={styles.inputLabel} htmlFor="room-input">
                Room code
              </label>
              <input
                className={styles.nameInput}
                value={playerInfo.roomId}
                placeholder="ABC123"
                autoCapitalize="characters"
                onChange={(e) =>
                  setPlayerInfo({
                    ...playerInfo,
                    roomId: e.target.value.toUpperCase(),
                  })
                }
                id="room-input"
              />
            </div>
          </section>
        )}

        {error && <p className={styles.errorText}>{error}</p>}
      </div>
    </PhoneShell>
  );
};
