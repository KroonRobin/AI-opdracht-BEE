using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AI_opdracht_BEE.Core;
using AI_opdracht_BEE.Systems;

namespace AI_opdracht_BEE.Entities;

public class MeleeAttackVisual
{
    private Vector2 _origin;
    private readonly float _startAngle;
    private readonly float _endAngle;
    private readonly float _radius;
    private readonly int _damage;
    private Vector2 _playerPosition;
    private readonly HashSet<Enemy> _hitEnemies = new();

    private readonly Texture2D _texture;
    private readonly float _visualScale;
    private readonly float _rotationOffset;

    private float _elapsed;

    public bool IsActive => _elapsed < GameConstants.MeleeAttackDuration;

    // True while this swing is aimed straight up — Game1 uses this to draw the sword behind the player.
    public bool IsBehindPlayer { get; }

    public MeleeAttackVisual(
    Vector2 origin, float centerAngle, float halfArc, float radius,
    int damage, Vector2 playerPosition, bool renderBehindPlayer,
    Texture2D texture = null, float visualScale = 1f, float rotationOffset = 0f)
    {
        _origin = origin;
        _startAngle = centerAngle - halfArc;
        _endAngle = centerAngle + halfArc;
        _radius = radius;
        _damage = damage;
        _playerPosition = playerPosition;
        _texture = texture;
        _visualScale = visualScale;
        _rotationOffset = rotationOffset;

        IsBehindPlayer = renderBehindPlayer;
    }

    private static bool IsFacingUp(float angle)
    {
        const float upAngle = -MathHelper.PiOver2; // straight up, in atan2 terms
        const float tolerance = 0.01f;
        return Math.Abs(MathHelper.WrapAngle(angle - upAngle)) < tolerance;
    }

    public List<EnemyDeath> Update(GameTime gameTime, List<Enemy> enemies)
    {
        var deaths = new List<EnemyDeath>();

        if (!IsActive)
            return deaths;

        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        float t = MathHelper.Clamp(_elapsed / GameConstants.MeleeAttackDuration, 0f, 1f);
        float sweptSoFar = t * (_endAngle - _startAngle);

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

            if (angleFromStart < 0f || angleFromStart > (_endAngle - _startAngle))
                continue;

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

        if (_texture != null)
        {
            // Pivot at the bottom-left corner (the hilt), so the blade sweeps outward
            // from the player's hand instead of spinning around the texture's own center.
            Vector2 pivot = new Vector2(0, _texture.Height);
            float rotation = currentAngle - _rotationOffset;

            spriteBatch.Draw(
                _texture,
                _origin,
                null,
                Color.White,
                rotation,
                pivot,
                _visualScale,
                SpriteEffects.None,
                0f);

            return;
        }

        // Fallback: the original dot, if no sword texture was supplied.
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

    public void FollowPlayer(Vector2 newOrigin, Vector2 newPlayerPosition)
    {
        _origin = newOrigin;
        _playerPosition = newPlayerPosition;
    }
}