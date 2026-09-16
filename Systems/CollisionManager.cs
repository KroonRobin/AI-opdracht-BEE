using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AI_opdracht_BEE.Core;
using AI_opdracht_BEE.Entities;

namespace AI_opdracht_BEE.Systems;

public static class CollisionManager
{
    public static List<Vector2> CheckBulletsVsEnemies(List<Bullet> bullets, List<Enemy> enemies)
    {
        var deathPositions = new List<Vector2>();

        foreach (var bullet in bullets)
        {
            if (!bullet.IsActive)
                continue;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                    continue;

                bool overlapping = Vector2.Distance(bullet.Position, enemy.HitboxCenter)
                                    < bullet.Radius + enemy.HitboxRadius;

                if (overlapping)
                {
                    enemy.TakeDamage(bullet.Damage);
                    bullet.Deactivate();

                    if (!enemy.IsAlive)
                        deathPositions.Add(enemy.Position);

                    break;
                }
            }
        }

        return deathPositions;
    }

    public static void CheckEnemiesVsPlayer(List<Enemy> enemies, Player player)
    {
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive || !enemy.CanAttack)
                continue;

            bool overlapping = Vector2.Distance(enemy.HitboxCenter, player.HitboxCenter)
                                < enemy.HitboxRadius + player.HitboxRadius;

            if (overlapping)
            {
                player.TakeDamage(enemy.ContactDamage);
                enemy.OnHitPlayer(player.Position);
            }
        }
    }

    public static void CheckEnemyBulletsVsPlayer(List<Bullet> enemyBullets, Player player)
    {
        foreach (var bullet in enemyBullets)
        {
            if (!bullet.IsActive)
                continue;

            bool overlapping = Vector2.Distance(bullet.Position, player.HitboxCenter)
                                < bullet.Radius + player.HitboxRadius;

            if (overlapping)
            {
                player.TakeDamage(bullet.Damage);
                bullet.Deactivate();
            }
        }
    }

    public static List<UpgradeType> CheckPickupsVsPlayer(List<Pickup> pickups, Player player)
    {
        var collected = new List<UpgradeType>();

        foreach (var pickup in pickups)
        {
            if (!pickup.IsActive)
                continue;

            bool overlapping = Vector2.Distance(pickup.HitboxCenter, player.HitboxCenter)
                                < pickup.HitboxRadius + player.HitboxRadius;

            if (overlapping)
            {
                pickup.Collect();
                collected.Add(pickup.Type);
            }
        }

        return collected;
    }
}