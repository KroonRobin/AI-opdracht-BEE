using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AI_opdracht_BEE.Core;
using AI_opdracht_BEE.Entities;

namespace AI_opdracht_BEE.Systems;

public class LevelManager
{
    private enum LevelState { WaitingToAnnounce, Announcing, Spawning }

    private LevelState _state;
    private float _timer;

    public int CurrentLevel { get; private set; }
    public string BannerText { get; private set; } = "";
    public bool ShowBanner => _state == LevelState.Announcing;
    public bool IsSpawningAllowed => _state == LevelState.Spawning;

    public LevelManager()
    {
        CurrentLevel = 0;
        _state = LevelState.WaitingToAnnounce;
        _timer = GameConstants.InitialRoundAnnouncementDelay;
    }

    public void Update(GameTime gameTime, List<Enemy> enemies, EnemySpawner spawner)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state)
        {
            case LevelState.WaitingToAnnounce:
                _timer -= delta;
                if (_timer <= 0f)
                {
                    CurrentLevel++;
                    BannerText = $"Round {CurrentLevel}";
                    _state = LevelState.Announcing;
                    _timer = GameConstants.RoundAnnouncementDuration;
                }
                break;

            case LevelState.Announcing:
                _timer -= delta;
                if (_timer <= 0f)
                {
                    StartLevel(spawner);
                    _state = LevelState.Spawning;
                }
                break;

            case LevelState.Spawning:
                if (spawner.IsDoneSpawning && enemies.Count == 0)
                {
                    _state = LevelState.WaitingToAnnounce;
                    _timer = GameConstants.RoundTransitionDelay;
                }
                break;
        }
    }

    private void StartLevel(EnemySpawner spawner)
    {
        int levelIndex = CurrentLevel - 1;

        int enemyCount = (int)Math.Round(
            GameConstants.BaseEnemiesPerLevel * Math.Pow(1 + GameConstants.EnemiesPerLevelGrowth, levelIndex));

        spawner.StartLevel(enemyCount, levelIndex);
    }
}