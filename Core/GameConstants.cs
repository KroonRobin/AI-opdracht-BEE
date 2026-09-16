namespace AI_opdracht_BEE.Core;

public static class GameConstants
{
    // Room / window dimensions
    public const int RoomWidth = 1280;
    public const int RoomHeight = 720;

    // Player
    public const float PlayerSpeed = 220f;      // pixels per second
    public const int PlayerWidth = 24;
    public const int PlayerHeight = 40;         // taller than wide, like a 3/4 character sprite
    public const int PlayerHitboxRadius = 10;   // collision circle at the feet
    public const int PlayerMaxHealth = 100;   // was 5 — update the existing line

    // Health bar color thresholds
    public const float HealthBarYellowThreshold = 0.5f;
    public const float HealthBarRedThreshold = 0.25f;

    // UI
    public const int HealthBarWidth = 220;
    public const int HealthBarHeight = 25;         
    public const int HealthBarPadding = 16;
    public const int HealthBarCornerRadius = 8;
    public const int HealthBarOutlineThickness = 3;

    // Enemy
    public const float EnemySpeed = 90f;
    public const int EnemyWidth = 22;
    public const int EnemyHeight = 32;
    public const int EnemyHitboxRadius = 11;

    // Enemy health
    public const int EnemyMaxHealth = 1;
    public const int BulletDamage = 1;

    // Enemy behavior
    public const float EnemyKnockbackSpeed = 350f;
    public const float EnemyKnockbackDuration = 0.25f;  // seconds spent flying backward
    public const float EnemyAttackCooldown = 0.6f;      // seconds before it can hit you again after recovering
    public const int EnemyContactDamage = 8;

    // Bullet
    public const float BulletSpeed = 800f;
    public const int BulletRadius = 4;

    // Shooting
    public const float BaseFireInterval = 1.0f;  // seconds between shots at base fire rate

    // Spawning
    public const float InitialSpawnInterval = 1.5f;   // seconds between spawns
    public const float MinimumSpawnInterval = 0.25f;
    public const float SpawnRampPerSecond = 0.01f;    // how fast difficulty climbs
}