namespace AI_opdracht_BEE.Core;

public static class GameConstants
{
    // Room / window dimensions
    public const int RoomWidth = 1600;
    public const int RoomHeight = 900;

    // Player
    public const float PlayerSpeed = 220f;      // pixels per second
    public const float PlayerMaxSpeedCap = 800f;   // hard ceiling; tune to taste once you test it
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

    // Leveling
    public const int BaseEnemiesPerLevel = 8;
    public const float EnemiesPerLevelGrowth = 0.25f;      // +25% enemy count per level
    public const float EnemyHealthGrowthPerLevel = 0.15f;  // +15% enemy health per level
    public const float EnemyDamageGrowthPerLevel = 0.15f;  // +15% enemy damage per level

    public const float InitialRoundAnnouncementDelay = 2f; // delay before "Round 1" banner at game start
    public const float RoundTransitionDelay = 5f;          // wait after last enemy dies, before announcing next round
    public const float RoundAnnouncementDuration = 3f;      // how long the round banner stays visible

    // Enemy standard
    public const float EnemySpeed = 90f;
    public const int EnemyWidth = 22;
    public const int EnemyHeight = 32;
    public const int EnemyHitboxRadius = 11;

    // Ranged enemy
    public const float RangedEnemySpeed = 70f;
    public const int RangedEnemyWidth = 22;
    public const int RangedEnemyHeight = 32;
    public const int RangedEnemyHitboxRadius = 11;
    public const int RangedEnemyBaseHealth = 6;
    public const int RangedEnemyBaseContactDamage = 6;
    public const int RangedEnemyBaseBulletDamage = 12;
    public const float RangedEnemyEngageRange = RoomWidth / 3f;
    public const float RangedEnemyAttackCooldown = 1.2f;
    public const float RangedEnemySpawnChance = 0.3f;              // 30% of spawns are ranged
    public const int RangedEnemyUnlockRound = 5;

    // Enemy bullets
    public const float EnemyBulletSpeed = 400f;
    public const int EnemyBulletRadius = 5;

    // Enemy health
    public const int EnemyMaxHealth = 10;
    public const int BulletDamage = 10;

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

    // Pickups / Upgrades
    public const float PickupDropChance = 0.1f;   // 35% chance per enemy kill
    public const int PickupSize = 16;
    public const int PickupHitboxRadius = 10;

    public const int DamageUpgradeAmount = 3;
    public const float FireRateUpgradeMultiplier = 0.85f;  // 15% faster per pickup
    public const float MinFireInterval = 0.1f;             // hard floor, prevents infinite fire rate stacking
    public const float MoveSpeedUpgradeAmount = 50f;
    public const int MaxHealthUpgradeAmount = 25;
    public const int MaxUpgradesPerRound = 2;

    public const int HealAmount = 50;
    public const float HealDropWeight = 1.0f;

    // Upgrade drop weights — higher number = more likely. Don't need to sum to any particular total.
    public const float DamageDropWeight = 1.0f;
    public const float FireRateDropWeight = 1.0f;
    public const float MoveSpeedDropWeight = 1.0f;
    public const float MaxHealthDropWeight = 0.3f;

    // Pickup lifetime
    public const float PickupLifetime = 8f;        // total seconds before disappearing
    public const float PickupBlinkDuration = 3f;   // blinking starts this many seconds before expiry
    public const int PickupBlinkCount = 3;         // number of on/off blinks during that window
}