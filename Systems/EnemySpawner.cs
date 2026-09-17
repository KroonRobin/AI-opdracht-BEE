using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AI_opdracht_BEE.Core;
using AI_opdracht_BEE.Entities;

namespace AI_opdracht_BEE.Systems;

public class EnemySpawner
{
    private readonly Random _random = new();
    private float _timer;
    private float _currentInterval;

    private int _enemiesToSpawn;
    private int _enemiesSpawned;

    private int _meleeHealth;
    private int _meleeDamage;
    private int _rangedHealth;
    private int _rangedContactDamage;
    private int _rangedBulletDamage;

    private int _currentLevelIndex;

    private int _meleePoints;
    private int _rangedPoints;

    public bool IsDoneSpawning => _enemiesSpawned >= _enemiesToSpawn;

    public void StartLevel(int enemyCount, int levelIndex)
    {
        _enemiesToSpawn = enemyCount;
        _enemiesSpawned = 0;
        _currentInterval = GameConstants.InitialSpawnInterval;
        _timer = _currentInterval;
        _currentLevelIndex = levelIndex;

        double healthGrowth = Math.Pow(1 + GameConstants.EnemyHealthGrowthPerLevel, levelIndex);
        double damageGrowth = Math.Pow(1 + GameConstants.EnemyDamageGrowthPerLevel, levelIndex);
        double pointsGrowth = Math.Pow(1 + GameConstants.EnemyPointsGrowthPerLevel, levelIndex);

        _meleeHealth = (int)Math.Round(GameConstants.EnemyMaxHealth * healthGrowth);
        _meleeDamage = (int)Math.Round(GameConstants.EnemyContactDamage * damageGrowth);
        _meleePoints = (int)Math.Round(GameConstants.MeleeEnemyBasePoints * pointsGrowth);

        _rangedHealth = (int)Math.Round(GameConstants.RangedEnemyBaseHealth * healthGrowth);
        _rangedContactDamage = (int)Math.Round(GameConstants.RangedEnemyBaseContactDamage * damageGrowth);
        _rangedBulletDamage = (int)Math.Round(GameConstants.RangedEnemyBaseBulletDamage * damageGrowth);
        _rangedPoints = (int)Math.Round(GameConstants.RangedEnemyBasePoints * pointsGrowth);
    }

    public void Update(GameTime gameTime, List<Enemy> enemies)
    {
        if (IsDoneSpawning)
            return;

        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _currentInterval = Math.Max(
            GameConstants.MinimumSpawnInterval,
            _currentInterval - GameConstants.SpawnRampPerSecond * delta);

        _timer -= delta;

        if (_timer <= 0f)
        {
            enemies.Add(CreateNextEnemy());
            _enemiesSpawned++;
            _timer = _currentInterval;
        }
    }

    private Enemy CreateNextEnemy()
    {
        Vector2 position = GetRandomEdgePosition();

        int currentRound = _currentLevelIndex + 1;
        bool rangedUnlocked = currentRound >= GameConstants.RangedEnemyUnlockRound;
        bool spawnRanged = rangedUnlocked && _random.NextDouble() < GameConstants.RangedEnemySpawnChance;

        return spawnRanged
            ? Enemy.CreateRanged(position, _rangedHealth, _rangedContactDamage, _rangedBulletDamage, _rangedPoints)
            : Enemy.CreateMelee(position, _meleeHealth, _meleeDamage, _meleePoints);
    }

    private Vector2 GetRandomEdgePosition()
    {
        int edge = _random.Next(4);

        return edge switch
        {
            0 => new Vector2(_random.Next(0, GameConstants.RoomWidth), -GameConstants.EnemyHeight),
            1 => new Vector2(_random.Next(0, GameConstants.RoomWidth), GameConstants.RoomHeight + GameConstants.EnemyHeight),
            2 => new Vector2(-GameConstants.EnemyWidth, _random.Next(0, GameConstants.RoomHeight)),
            _ => new Vector2(GameConstants.RoomWidth + GameConstants.EnemyWidth, _random.Next(0, GameConstants.RoomHeight)),
        };
    }
}