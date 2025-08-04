using UnityEngine;

public class BlindLevel
{
    public int Level { get; private set; }
    public int SmallBlind { get; private set; }
    public int BigBlind { get; private set; }
    public int Ante { get; private set; }
    public int TimeElapsedMinutes { get; private set; }

    public BlindLevel(int level, int smallBlind, int bigBlind, int Ante, int timeElapsed)
    {
        this.Level = level;
        this.SmallBlind = smallBlind;
        this.BigBlind = bigBlind;
        this.Ante = Ante;
        this.TimeElapsedMinutes = timeElapsed;
    }
}
