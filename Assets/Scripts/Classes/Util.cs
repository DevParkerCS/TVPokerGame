using System.Collections.Generic;
using System;
using UnityEngine;

public static class Util
{
    public static void Shuffle<T>(List<T> array)
    {
        int n = array.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1); // include n
            T value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
    }

    public static List<BlindLevel> GenerateBlindLevels(
        int numPlayers,
        int startingStack,
        int smallestChipDenom,
        int roundTimeMinutes,
        int totalTournamentMinutes)
    {
        List<BlindLevel> levels = new List<BlindLevel>();

        int totalChips = numPlayers * startingStack;
        int numLevels = totalTournamentMinutes / roundTimeMinutes;

        // Estimate a reasonable final big blind target (e.g., ~15% of total chips in play)
        int targetBigBlind = totalChips / 7;

        // Use exponential growth to determine blind levels
        double growthFactor = Math.Pow((double)targetBigBlind / (smallestChipDenom * 2), 1.0 / (numLevels - 1));

        int currentSB = smallestChipDenom;
        int currentBB = currentSB * 2;

        for (int i = 0; i < numLevels; i++)
        {
            int roundedSB = RoundToNearest(currentSB, smallestChipDenom);
            int roundedBB = RoundToNearest(currentBB, smallestChipDenom);
            int roundedAnte = roundedBB; // Big Blind Ante model

            levels.Add(new BlindLevel
            (
                i + 1,
                roundedSB,
                roundedBB,
                roundedAnte,
                i * roundTimeMinutes
            ));
            Debug.Log($"Level {i + 1}, smallBlind {roundedSB}, bigBlind {roundedBB}, Ante {roundedAnte}, timeElapsed {i * roundTimeMinutes}");

            // Apply exponential growth for next level
            currentSB = (int)(currentSB * growthFactor);
            currentBB = (int)(currentBB * growthFactor);
        }

        return levels;
    }

    private static int RoundToNearest(int value, int nearest)
    {
        return (int)Math.Round((double)value / nearest) * nearest;
    }
}
