using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AI_opdracht_BEE.Core;

namespace AI_opdracht_BEE.Entities;

public enum EnemyState { Chasing, Knockback }

public class Enemy
{
    public Vector2 Position;
    public EnemyState State { get; private set; } = EnemyState.Chasing;

    public int Health { get; private set; } = GameConstants.EnemyMaxHealth;
    public bool IsAlive => Health > 0;

    private Vector2 _knockbackVelocity;
    private float _knockbackTimer;
    private float _attackCooldownTimer;

    public Enemy(Vector2 startPosition)
    {
        Position = startPosition;
    }

    // True only when the enemy is actually able to deal damage right now.
    public bool CanAttack => State == EnemyState.Chasing && _attackCooldownTimer <= 0f;

    public void Update(GameTime gameTime, Vector2 playerPosition)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer -= delta;

        switch (State)
        {
            case EnemyState.Chasing:
                ChasePlayer(playerPosition, delta);
                break;

            case EnemyState.Knockback:
                ApplyKnockback(delta);
                break;
        }
    }

    private void ChasePlayer(Vector2 playerPosition, float delta)
    {
        Vector2 direction = playerPosition - Position;
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
            Position += direction * GameConstants.EnemySpeed * delta;
        }
    }

    private void ApplyKnockback(float delta)
    {
        Position += _knockbackVelocity * delta;
        _knockbackTimer -= delta;

        if (_knockbackTimer <= 0f)
            State = EnemyState.Chasing;
    }

    // Called by collision code the instant this enemy successfully hits the player.
    public void OnHitPlayer(Vector2 playerPosition)
    {
        Vector2 knockDirection = Position - playerPosition;
        if (knockDirection == Vector2.Zero)
            knockDirection = new Vector2(1, 0); // arbitrary fallback if perfectly overlapping

        knockDirection.Normalize();

        _knockbackVelocity = knockDirection * GameConstants.EnemyKnockbackSpeed;
        _knockbackTimer = GameConstants.EnemyKnockbackDuration;
        _attackCooldownTimer = GameConstants.EnemyAttackCooldown;
        State = EnemyState.Knockback;
    }

    public void TakeDamage(int amount)
    {
        Health = System.Math.Max(0, Health - amount);
    }

    public Vector2 HitboxCenter => Position - new Vector2(0, GameConstants.EnemyHeight / 2f);
    public float HitboxRadius => GameConstants.EnemyHitboxRadius;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var rect = new Rectangle(
            (int)(Position.X - GameConstants.EnemyWidth / 2f),
            (int)(Position.Y - GameConstants.EnemyHeight),
            GameConstants.EnemyWidth,
            GameConstants.EnemyHeight);

        // Tint differently while knocked back, so you can visually confirm the state works
        Color color = State == EnemyState.Knockback ? Color.OrangeRed : Color.DarkRed;
        spriteBatch.Draw(pixel, rect, color);
    }
}