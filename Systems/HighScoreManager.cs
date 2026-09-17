using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AI_opdracht_BEE.Systems;

public readonly struct HighScoreEntry
{
    public readonly string Name;
    public readonly int Score;

    public HighScoreEntry(string name, int score)
    {
        Name = name;
        Score = score;
    }
}

public static class HighScoreManager
{
    private const int MaxEntries = 5;

    private static readonly string SaveDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AI-opdracht-BEE");

    private static readonly string SaveFilePath = Path.Combine(SaveDirectory, "highscores.txt");

    public static List<HighScoreEntry> Load()
    {
        var entries = new List<HighScoreEntry>();

        try
        {
            if (File.Exists(SaveFilePath))
            {
                foreach (var line in File.ReadAllLines(SaveFilePath))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int score))
                        entries.Add(new HighScoreEntry(parts[1], score));
                }
            }
        }
        catch (IOException)
        {
        }

        return entries;
    }

    // True if this score would actually earn a spot on the list (top 5, or fewer than 5 saved so far).
    public static bool Qualifies(List<HighScoreEntry> currentEntries, int score)
    {
        if (currentEntries.Count < MaxEntries)
            return true;

        return score >= currentEntries.Min(e => e.Score);
    }

    public static List<HighScoreEntry> AddScore(List<HighScoreEntry> currentEntries, HighScoreEntry newEntry)
    {
        var updated = new List<HighScoreEntry>(currentEntries) { newEntry };
        updated = updated.OrderByDescending(e => e.Score).Take(MaxEntries).ToList();

        try
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllLines(SaveFilePath, updated.Select(e => $"{e.Score}|{e.Name}"));
        }
        catch (IOException)
        {
        }

        return updated;
    }
}