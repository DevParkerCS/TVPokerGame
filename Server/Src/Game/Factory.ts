import { GameState } from "../Types/Types";

export const CreateNewGame = (): GameState => {
  const gameState: GameState = {
    blinds: {
      blindLevels: [],
      curBlindLevel: 0,
    },
    board: {
      flopCards: [],
      riverCard: null,
      turnCard: null,
    },
    players: {},
    pot: {
      contributions: {},
      potAmt: 0,
    },
    curPlayerTurn: 0,
    curStreet: 0,
    lastBetAmt: 0,
  };

  return gameState;
};
