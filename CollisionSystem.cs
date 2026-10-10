using Microsoft.Xna.Framework;
using System;
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

    private static void ResolveGemCollisions(
        Player player,
        ObjectPool<XpGem> xpPool,
        float dt,
        ref bool leveledUpOut)
    {
        float pickupRadius = player.PickupRadius.Value;
        float pickupRadiusSq = pickupRadius * pickupRadius;
        float collectRadiusSq = 20f * 20f;
        var xps = xpPool.Items;

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
        ResolveGemCollisions(player, xpPool, dt, ref leveledUpOut);
    }

    public static void ResolveBossCollisions(
        Player player,
        ObjectPool<Bullet> bulletPool,
        Boss boss,
        ObjectPool<XpGem> xpPool,
        float dt,
        out bool leveledUpOut,
        out bool bossDiedOut)
    {
        leveledUpOut = false;
        bossDiedOut = false;

        if (boss == null || !boss.IsActive)
        {
            return;
        }

        var bullets = bulletPool.Items;

        // 1. Bullets <-> Boss
        float bossRadius = boss.Size * 0.5f;
        float radiusSum = 4f + bossRadius;
        float radiusSumSq = radiusSum * radiusSum;

        for (int i = 0; i < bullets.Count; i++)
        {
            var bullet = bullets[i];
            if (!bullet.IsActive) continue;

            float distSq = Vector2.DistanceSquared(bullet.Position, boss.Position);
            if (distSq < radiusSumSq)
            {
                boss.Health -= bullet.Damage;

                for (int k = 0; k < bullet.HitEffects.Count; k++)
                {
                    bullet.HitEffects[k].OnHit(boss, ref bullet);
                }

                bullet.PierceCount--;
                if (bullet.PierceCount <= 0)
                {
                    bullet.IsActive = false;
                }

                if (boss.Health <= 0f)
                {
                    boss.IsActive = false;
                    bossDiedOut = true;

                    for (int g = 0; g < boss.XpDropCount; g++)
                    {
                        var xp = xpPool.Get();
                        float angle = g * (MathHelper.TwoPi / boss.XpDropCount);
                        float dropRadius = 35f + (g % 3) * 15f;
                        Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dropRadius;
                        xp.Position = boss.Position + offset;
                        xp.Value = 1;
                    }

                    break;
                }
            }
        }

        // 2. Boss <-> Player
        if (boss.IsActive)
        {
            float playerRadius = 14f;
            float playerBossRadiusSum = playerRadius + bossRadius;
            float pbDistSq = Vector2.DistanceSquared(boss.Position, player.Position);

            if (pbDistSq < playerBossRadiusSum * playerBossRadiusSum)
            {
                if (boss.AttackCooldown <= 0f)
                {
                    player.TakeDamage(boss.Damage);
                    boss.AttackCooldown = 0.5f;
                }

                Vector2 delta = boss.Position - player.Position;
                if (pbDistSq > 0.0001f)
                {
                    float pushFactor = (playerBossRadiusSum * playerBossRadiusSum - pbDistSq) / (pbDistSq + 1f) * 0.2f;
                    boss.Position += delta * pushFactor;

                    float halfSize = boss.Size * 0.5f;
                    boss.Position.X = MathHelper.Clamp(boss.Position.X, halfSize, 2000f - halfSize);
                    boss.Position.Y = MathHelper.Clamp(boss.Position.Y, halfSize, 2000f - halfSize);
                }
            }
        }

        // 3. Player <-> XpGem
        ResolveGemCollisions(player, xpPool, dt, ref leveledUpOut);
    }
}
