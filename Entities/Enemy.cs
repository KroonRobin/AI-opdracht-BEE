using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AI_opdracht_BEE.Core;

namespace AI_opdracht_BEE.Entities;

public enum EnemyType { Melee, Ranged, Brawler }
public enum EnemyState { Chasing, Attacking, Knockback }

public class Enemy
{
    public Vector2 Position;
    public EnemyType Type { get; }
    public EnemyState State { get; private set; } = EnemyState.Chasing;

    public int Health { get; private set; }
    public int ContactDamage { get; }
    public int BulletDamage { get; }
    public bool IsAlive => Health > 0;
    public bool WantsToFire { get; private set; }
    public int PointValue { get; }

    private readonly float _moveSpeed;
    private readonly int _width;
    private readonly int _height;
    private readonly float _hitboxRadius;
    private readonly float _engageRange;
    private readonly float _attackCooldownDuration;
    private float _attackTimer;

    private Vector2 _knockbackVelocity;
    private float _knockbackTimer;
    private float _contactCooldownTimer;

    private Enemy(
    EnemyType type, Vector2 startPosition, int maxHealth, int contactDamage,
    float moveSpeed, int width, int height, float hitboxRadius,
    float engageRange, float attackCooldownDuration, int bulletDamage, int pointValue)
    {
        Type = type;
        Position = startPosition;
        Health = maxHealth;
        ContactDamage = contactDamage;
        _moveSpeed = moveSpeed;
        _width = width;
        _height = height;
        _hitboxRadius = hitboxRadius;
        _engageRange = engageRange;
        _attackCooldownDuration = attackCooldownDuration;
        _attackTimer = attackCooldownDuration;
        BulletDamage = bulletDamage;
        PointValue = pointValue;
    }

    public static Enemy CreateMelee(Vector2 startPosition, int maxHealth, int contactDamage, int pointValue)
    {
        return new Enemy(
            EnemyType.Melee, startPosition, maxHealth, contactDamage,
            GameConstants.EnemySpeed, GameConstants.EnemyWidth, GameConstants.EnemyHeight,
            GameConstants.EnemyHitboxRadius, engageRange: 0f, attackCooldownDuration: 0f,
            bulletDamage: 0, pointValue);
    }

    public static Enemy CreateBrawler(Vector2 startPosition, int maxHealth, int contactDamage, int pointValue)
    {
        return new Enemy(
            EnemyType.Brawler, startPosition, maxHealth, contactDamage,
            GameConstants.BrawlerEnemySpeed, GameConstants.BrawlerEnemyWidth, GameConstants.BrawlerEnemyHeight,
            GameConstants.BrawlerEnemyHitboxRadius, engageRange: 0f, attackCooldownDuration: 0f,
            bulletDamage: 0, pointValue);
    }

    public static Enemy CreateRanged(Vector2 startPosition, int maxHealth, int contactDamage, int bulletDamage, int pointValue)
    {
        return new Enemy(
            EnemyType.Ranged, startPosition, maxHealth, contactDamage,
            GameConstants.RangedEnemySpeed, GameConstants.RangedEnemyWidth, GameConstants.RangedEnemyHeight,
            GameConstants.RangedEnemyHitboxRadius, GameConstants.RangedEnemyEngageRange,
            GameConstants.RangedEnemyAttackCooldown, bulletDamage, pointValue);
    }

    public bool CanAttack => State != EnemyState.Knockback && _contactCooldownTimer <= 0f;
    public bool DodgesBullets => Type == EnemyType.Brawler;
    public Vector2 ShootOrigin => Position - new Vector2(0, _height / 2f);

    public void Update(GameTime gameTime, Vector2 playerPosition)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_contactCooldownTimer > 0f)
            _contactCooldownTimer -= delta;

        switch (State)
        {
            case EnemyState.Chasing:
                ChaseOrEngage(playerPosition, delta);
                break;
            case EnemyState.Attacking:
                HandleAttacking(playerPosition, delta);
                break;
            case EnemyState.Knockback:
                ApplyKnockback(delta);
                break;
        }
    }

    private void ChaseOrEngage(Vector2 playerPosition, float delta)
    {
        bool insideRoom =
            Position.X >= 0 && Position.X <= GameConstants.RoomWidth &&
            Position.Y >= 0 && Position.Y <= GameConstants.RoomHeight;

        float distance = Vector2.Distance(Position, playerPosition);

        if (Type == EnemyType.Ranged && insideRoom && distance <= _engageRange)
        {
            State = EnemyState.Attacking;
            return;
        }

        Vector2 direction = playerPosition - Position;
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
            Position += direction * _moveSpeed * delta;
        }
    }

    private void HandleAttacking(Vector2 playerPosition, float delta)
    {
        float distance = Vector2.Distance(Position, playerPosition);

        if (distance > _engageRange)
        {
            State = EnemyState.Chasing;
            return;
        }

        _attackTimer -= delta;
        if (_attackTimer <= 0f)
        {
            WantsToFire = true;
            _attackTimer = _attackCooldownDuration;
        }
    }

    public void ConsumeFireRequest()
    {
        WantsToFire = false;
    }

    private void ApplyKnockback(float delta)
    {
        Position += _knockbackVelocity * delta;
        _knockbackTimer -= delta;

        if (_knockbackTimer <= 0f)
            State = EnemyState.Chasing;
    }

    public void TakeDamage(int amount)
    {
        Health = System.Math.Max(0, Health - amount);
    }

    public void OnHitPlayer(Vector2 playerPosition)
    {
        Vector2 knockDirection = Position - playerPosition;
        if (knockDirection == Vector2.Zero)
            knockDirection = new Vector2(1, 0);

        knockDirection.Normalize();

        _knockbackVelocity = knockDirection * GameConstants.EnemyKnockbackSpeed;
        _knockbackTimer = GameConstants.EnemyKnockbackDuration;
        _contactCooldownTimer = GameConstants.EnemyAttackCooldown;
        State = EnemyState.Knockback;
    }

    public void OnDodgeBullet(Vector2 bulletPosition)
    {
        Vector2 pushDirection = Position - bulletPosition;
        if (pushDirection == Vector2.Zero)
            pushDirection = new Vector2(1, 0);

        pushDirection.Normalize();

        _knockbackVelocity = pushDirection * GameConstants.BrawlerDodgeSpeed;
        _knockbackTimer = GameConstants.BrawlerDodgeDuration;
        State = EnemyState.Knockback;
    }

    public Vector2 HitboxCenter => Position - new Vector2(0, _height / 2f);
    public float HitboxRadius => _hitboxRadius;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var rect = new Rectangle(
            (int)(Position.X - _width / 2f),
            (int)(Position.Y - _height),
            _width, _height);

        Color color = Type switch
        {
            EnemyType.Ranged => State == EnemyState.Knockback ? Color.Plum : Color.Purple,
            EnemyType.Brawler => State == EnemyState.Knockback ? Color.Yellow : Color.DarkOrange,
            _ => State == EnemyState.Knockback ? Color.OrangeRed : Color.DarkRed,
        };

        spriteBatch.Draw(pixel, rect, color);
    }
}