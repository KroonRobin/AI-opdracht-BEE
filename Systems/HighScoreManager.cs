using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AI_opdracht_BEE.Systems;

public static class HighScoreManager
{
    private const int MaxEntries = 5;

    private static readonly string SaveDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AI-opdracht-BEE");

    private static readonly string SaveFilePath = Path.Combine(SaveDirectory, "highscores.txt");

    public static List<int> Load()
    {
        var scores = new List<int>();

        try
        {
            if (File.Exists(SaveFilePath))
            {
                foreach (var line in File.ReadAllLines(SaveFilePath))
                {
                    if (int.TryParse(line, out int value))
                        scores.Add(value);
                }
            }
        }
        catch (IOException)
        {
            // If reading fails for any reason, just start from an empty list rather than crashing.
        }

        return scores;
    }

    // Adds a score, trims to the top 5, saves to disk, and returns the updated list.
    public static List<int> AddScore(List<int> currentScores, int newScore)
    {
        var updated = new List<int>(currentScores) { newScore };
        updated = updated.OrderByDescending(s => s).Take(MaxEntries).ToList();

        try
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllLines(SaveFilePath, updated.Select(s => s.ToString()));
        }
        catch (IOException)
        {
            // Saving is best-effort — a failed write shouldn't crash the game.
        }

        return updated;
    }
}