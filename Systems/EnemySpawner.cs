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

    public EnemySpawner()
    {
        _currentInterval = GameConstants.InitialSpawnInterval;
        _timer = _currentInterval;
    }

    public void Update(GameTime gameTime, List<Enemy> enemies)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Difficulty ramp: interval slowly shrinks toward the minimum over time.
        _currentInterval = Math.Max(
            GameConstants.MinimumSpawnInterval,
            _currentInterval - GameConstants.SpawnRampPerSecond * delta);

        _timer -= delta;

        if (_timer <= 0f)
        {
            enemies.Add(new Enemy(GetRandomEdgePosition()));
            _timer = _currentInterval;
        }
    }

    private Vector2 GetRandomEdgePosition()
    {
        int edge = _random.Next(4); // 0=top, 1=bottom, 2=left, 3=right

        return edge switch
        {
            0 => new Vector2(_random.Next(0, GameConstants.RoomWidth), -GameConstants.EnemyHeight),
            1 => new Vector2(_random.Next(0, GameConstants.RoomWidth), GameConstants.RoomHeight + GameConstants.EnemyHeight),
            2 => new Vector2(-GameConstants.EnemyWidth, _random.Next(0, GameConstants.RoomHeight)),
            _ => new Vector2(GameConstants.RoomWidth + GameConstants.EnemyWidth, _random.Next(0, GameConstants.RoomHeight)),
        };
    }
}