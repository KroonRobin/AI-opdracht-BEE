using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AI_opdracht_BEE.Core;
using AI_opdracht_BEE.Systems;

namespace AI_opdracht_BEE.Entities;

public class MeleeAttackVisual
{
    private readonly Vector2 _origin;
    private readonly float _startAngle;
    private readonly float _endAngle;
    private readonly float _radius;
    private readonly int _damage;
    private readonly Vector2 _playerPosition;
    private readonly HashSet<Enemy> _hitEnemies = new();

    private float _elapsed;

    public bool IsActive => _elapsed < GameConstants.MeleeAttackDuration;

    public MeleeAttackVisual(
        Vector2 origin, float centerAngle, float halfArc, float radius,
        int damage, Vector2 playerPosition)
    {
        _origin = origin;
        _startAngle = centerAngle - halfArc;
        _endAngle = centerAngle + halfArc;
        _radius = radius;
        _damage = damage;
        _playerPosition = playerPosition;
    }

    // Advances the sweep and returns any enemies that died to a hit landed THIS frame.
    public List<EnemyDeath> Update(GameTime gameTime, List<Enemy> enemies)
    {
        var deaths = new List<EnemyDeath>();

        if (!IsActive)
            return deaths;

        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        float t = MathHelper.Clamp(_elapsed / GameConstants.MeleeAttackDuration, 0f, 1f);
        float sweptSoFar = t * (_endAngle - _startAngle); // how far the dot has rotated so far, from start

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive || _hitEnemies.Contains(enemy))
                continue;

            Vector2 toEnemy = enemy.HitboxCenter - _origin;
            float distance = toEnemy.Length();

            if (distance > _radius + enemy.HitboxRadius)
                continue;

            float angleToEnemy = (float)Math.Atan2(toEnemy.Y, toEnemy.X);
            float angleFromStart = MathHelper.WrapAngle(angleToEnemy - _startAngle);

            // Is this enemy inside the wedge at all?
            if (angleFromStart < 0f || angleFromStart > (_endAngle - _startAngle))
                continue;

            // Has the sweep actually rotated far enough to reach this enemy's angle yet?
            if (angleFromStart > sweptSoFar)
                continue;

            _hitEnemies.Add(enemy);
            enemy.TakeDamage(_damage);
            enemy.OnHitPlayer(_playerPosition);

            if (!enemy.IsAlive)
                deaths.Add(new EnemyDeath(enemy.Position, enemy.PointValue));
        }

        return deaths;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        float t = MathHelper.Clamp(_elapsed / GameConstants.MeleeAttackDuration, 0f, 1f);
        float currentAngle = MathHelper.Lerp(_startAngle, _endAngle, t);

        Vector2 dotPosition = _origin + new Vector2(
            (float)Math.Cos(currentAngle),
            (float)Math.Sin(currentAngle)) * _radius;

        var rect = new Rectangle(
            (int)(dotPosition.X - GameConstants.MeleeAttackDotSize / 2f),
            (int)(dotPosition.Y - GameConstants.MeleeAttackDotSize / 2f),
            GameConstants.MeleeAttackDotSize,
            GameConstants.MeleeAttackDotSize);

        spriteBatch.Draw(pixel, rect, Color.White);
    }
}