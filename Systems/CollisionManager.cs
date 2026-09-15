using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AI_opdracht_BEE.Core;
using AI_opdracht_BEE.Entities;

namespace AI_opdracht_BEE.Systems;

public static class CollisionManager
{
    // Returns how many enemies died this frame, so Game1 can add to the score.
    public static int CheckBulletsVsEnemies(List<Bullet> bullets, List<Enemy> enemies)
    {
        int kills = 0;

        foreach (var bullet in bullets)
        {
            if (!bullet.IsActive)
                continue;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                    continue;

                bool overlapping = Vector2.Distance(bullet.Position, enemy.HitboxCenter)
                                    < GameConstants.BulletRadius + enemy.HitboxRadius;

                if (overlapping)
                {
                    enemy.TakeDamage(GameConstants.BulletDamage);
                    bullet.Deactivate();

                    if (!enemy.IsAlive)
                        kills++;

                    break; // this bullet is spent, stop checking it against other enemies
                }
            }
        }

        return kills;
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
                player.TakeDamage(GameConstants.EnemyContactDamage);
                enemy.OnHitPlayer(player.Position);
            }
        }
    }
}