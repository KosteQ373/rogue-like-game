using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace rogue_like;

public static class CollisionSystem
{
    private static void ResolveBulletEnemyCollisions<T>(
        Player player,
        List<Bullet> bullets,
        List<T> enemies,
        ObjectPool<XpGem> xpPool) where T : Enemy
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            var bullet = bullets[i];
            if (!bullet.IsActive) continue;

            for (int j = 0; j < enemies.Count; j++)
            {
                var enemy = enemies[j];
                if (!enemy.IsActive) continue;

                float enemyRadius = enemy.Size * 0.5f;
                float radiusSum = 4f + enemyRadius;

                float distSq = Vector2.DistanceSquared(bullet.Position, enemy.Position);
                if (distSq < radiusSum * radiusSum)
                {
                    enemy.Health -= bullet.Damage;

                    // Apply knockback: knockback / size (of enemy) in px, in the direction the bullet was flying
                    Vector2 kbDir = bullet.Velocity;
                    if (kbDir.LengthSquared() > 0.0001f)
                    {
                        kbDir.Normalize();
                    }
                    else
                    {
                        kbDir = enemy.Position - bullet.Position;
                        if (kbDir.LengthSquared() > 0.0001f) kbDir.Normalize();
                        else kbDir = new Vector2(1f, 0f);
                    }

                    float kbDistance = player.Knockback.Value / enemy.Size;
                    enemy.Position += kbDir * kbDistance;
                    enemy.KnockbackTimer = 0.25f;
                    enemy.Position.X = MathHelper.Clamp(enemy.Position.X, 20f, 1980f);
                    enemy.Position.Y = MathHelper.Clamp(enemy.Position.Y, 20f, 1980f);

                    for (int k = 0; k < bullet.HitEffects.Count; k++)
                    {
                        bullet.HitEffects[k].OnHit(enemy, ref bullet);
                    }

                    bullet.PierceCount--;
                    if (bullet.PierceCount <= 0)
                    {
                        bullet.IsActive = false;
                    }

                    if (enemy.Health <= 0f)
                    {
                        enemy.IsActive = false;
                        for (int g = 0; g < enemy.XpDropCount; g++)
                        {
                            var xp = xpPool.Get();
                            float angle = g * (MathHelper.TwoPi / enemy.XpDropCount);
                            Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (g > 0 ? 14f : 0f);
                            xp.Position = enemy.Position + offset;
                            xp.Value = 1;
                        }
                    }

                    break;
                }
            }
        }
    }

    private static void ResolveEnemyPlayerCollisions<T>(
        Player player,
        List<T> enemies) where T : Enemy
    {
        float playerRadius = 14f;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (!enemy.IsActive) continue;

            float enemyRadius = enemy.Size * 0.5f;
            float radiusSum = playerRadius + enemyRadius;

            float distSq = Vector2.DistanceSquared(enemy.Position, player.Position);
            if (distSq < radiusSum * radiusSum)
            {
                if (enemy.AttackCooldown <= 0f)
                {
                    player.TakeDamage(enemy.Damage);
                    enemy.AttackCooldown = 0.5f;
                }

                Vector2 delta = enemy.Position - player.Position;
                if (distSq > 0.0001f)
                {
                    float pushFactor = (radiusSum * radiusSum - distSq) / (distSq + 1f) * 0.4f;
                    enemy.Position += delta * pushFactor;

                    enemy.Position.X = MathHelper.Clamp(enemy.Position.X, 20f, 1980f);
                    enemy.Position.Y = MathHelper.Clamp(enemy.Position.Y, 20f, 1980f);
                }
                else
                {
                    enemy.Position += new Vector2(1f, 0f) * 2f;
                }
            }
        }
    }

    private static void ResolveEnemyEnemyCollisions<T1, T2>(
        List<T1> list1,
        List<T2> list2,
        bool sameList) where T1 : Enemy where T2 : Enemy
    {
        int count1 = list1.Count;
        int count2 = list2.Count;

        for (int i = 0; i < count1; i++)
        {
            var e1 = list1[i];
            if (!e1.IsActive) continue;

            int startJ = sameList ? i + 1 : 0;
            for (int j = startJ; j < count2; j++)
            {
                var e2 = list2[j];
                if (!e2.IsActive) continue;
                if (sameList && i == j) continue;

                Vector2 delta = e1.Position - e2.Position;
                float distSq = delta.LengthSquared();
                float minDist = (e1.Size * 0.5f) + (e2.Size * 0.5f) + 4f;

                if (distSq < minDist * minDist && distSq > 0.0001f)
                {
                    float pushFactor = (minDist * minDist - distSq) / (distSq + 1f) * 0.1f;
                    Vector2 push = delta * pushFactor;
                    e1.Position += push;
                    e2.Position -= push;
                }
            }
        }
    }

    public static void ResolveCollisions(
        Player player,
        ObjectPool<Bullet> bulletPool,
        ObjectPool<BasicEnemy> basicPool,
        ObjectPool<TankEnemy> tankPool,
        ObjectPool<FastEnemy> fastPool,
        ObjectPool<XpGem> xpPool,
        float dt,
        out bool leveledUpOut)
    {
        leveledUpOut = false;

        var bullets = bulletPool.Items;
        var basics = basicPool.Items;
        var tanks = tankPool.Items;
        var fasts = fastPool.Items;
        var xps = xpPool.Items;

        // 1. Bullet <-> Enemy
        ResolveBulletEnemyCollisions(player, bullets, basics, xpPool);
        ResolveBulletEnemyCollisions(player, bullets, tanks, xpPool);
        ResolveBulletEnemyCollisions(player, bullets, fasts, xpPool);

        // 2. Enemy <-> Player
        ResolveEnemyPlayerCollisions(player, basics);
        ResolveEnemyPlayerCollisions(player, tanks);
        ResolveEnemyPlayerCollisions(player, fasts);

        // 3. Enemy <-> Enemy (within and between pools)
        ResolveEnemyEnemyCollisions(basics, basics, true);
        ResolveEnemyEnemyCollisions(tanks, tanks, true);
        ResolveEnemyEnemyCollisions(fasts, fasts, true);
        ResolveEnemyEnemyCollisions(basics, tanks, false);
        ResolveEnemyEnemyCollisions(basics, fasts, false);
        ResolveEnemyEnemyCollisions(tanks, fasts, false);

        // 4. Player <-> XpGem
        float pickupRadius = player.PickupRadius.Value;
        float pickupRadiusSq = pickupRadius * pickupRadius;
        float collectRadiusSq = 20f * 20f;

        for (int i = 0; i < xps.Count; i++)
        {
            var xp = xps[i];
            if (!xp.IsActive) continue;

            float distSq = Vector2.DistanceSquared(xp.Position, player.Position);
            if (distSq < pickupRadiusSq)
            {
                Vector2 dir = player.Position - xp.Position;
                if (dir.LengthSquared() > 0.001f)
                {
                    dir.Normalize();
                    xp.Position += dir * 350f * dt;
                }

                if (distSq < collectRadiusSq)
                {
                    xp.IsActive = false;
                    player.AddXp(xp.Value, out bool leveledUp);
                    if (leveledUp)
                    {
                        leveledUpOut = true;
                    }
                }
            }
        }
    }
}
