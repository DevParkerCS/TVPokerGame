/// <reference types="webpack-env" />

const req = require.context("./", false, /\.png$/);

export type AvatarImg = { name: string; src: string };

export const avatarImgs: AvatarImg[] = req
  .keys()
  .sort((a, b) => a.localeCompare(b, undefined, { numeric: true }))
  .map((k) => ({
    name: k.replace("./", "").replace(/\.[^/.]+$/, ""), // e.g., "cat_glass"
    src: req(k) as string, // hashed URL for <img src>
  }));

export const avatarByName: Record<string, string> = req
  .keys()
  .reduce((acc, k) => {
    const name = k.replace("./", "").replace(/\.[^/.]+$/, ""); // "cat_glass"
    acc[name] = req(k) as string; // hashed URL
    return acc;
  }, {} as Record<string, string>);
