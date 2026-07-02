const cardImageContext = require.context(
  "../assets",
  true,
  /\.(png|jpe?g|webp)$/
);

const indexedRanks = ["A", "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K"];
const suits = ["C", "D", "H", "S"];

function codeFromIndexedAsset(path: string) {
  const withoutExtension = path.replace(/^\.\//, "").replace(/\.[^/.]+$/, "");
  const parts = withoutExtension.split("/");
  const fileName = parts[parts.length - 1];
  const folderName = parts.length > 1 ? parts[parts.length - 2].toUpperCase() : "";

  const fileWithSuit = fileName.match(/^(\d\d)_([CDHS])$/i);
  if (fileWithSuit) {
    const rank = indexedRanks[Number(fileWithSuit[1])];
    const suit = fileWithSuit[2].toUpperCase();
    return rank && suits.includes(suit) ? `${rank}${suit}` : undefined;
  }

  const fileInSuitFolder = fileName.match(/^(\d\d)$/);
  if (fileInSuitFolder) {
    const rank = indexedRanks[Number(fileInSuitFolder[1])];
    return rank && suits.includes(folderName) ? `${rank}${folderName}` : undefined;
  }

  return undefined;
}

function codeFromCompactName(path: string) {
  const withoutExtension = path.replace(/^\.\//, "").replace(/\.[^/.]+$/, "");
  const fileName = withoutExtension.split("/").pop() ?? "";
  const compact = fileName.replace(/[^a-z0-9]/gi, "").toUpperCase();

  if (compact.length === 2) {
    const first = compact[0];
    const second = compact[1];
    if (suits.includes(second)) return `${first}${second}`;
    if (suits.includes(first)) return `${second}${first}`;
  }

  return undefined;
}

function codeFromAssetPath(path: string) {
  return codeFromIndexedAsset(path) ?? codeFromCompactName(path);
}

const cardImages: Record<string, string> = cardImageContext.keys().reduce(
  (acc: Record<string, string>, key: string) => {
    const code = codeFromAssetPath(key);
    if (code) {
      acc[code] = cardImageContext(key) as string;
    }
    return acc;
  },
  {}
);

export function getCardImage(code: string) {
  return cardImages[code.toUpperCase()];
}
