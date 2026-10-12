using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace rogue_like;

public static class CollisionSystem
{
    // Lookup table dla XpGemów, by uniknąć wywoływania Cos/Sin w czasie śmierci wrogów
    private static readonly Vector2[] DropDirections;

    static CollisionSystem()
    {
        int count = 64; // Pokrywa 64 kierunki rozrzutu
        DropDirections = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float angle = i * (MathHelper.TwoPi / count);
            DropDirections[i] = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }

    private static void ResolveBulletEnemyCollisions<T>(
        Player player,
        List<Bullet> bullets,
        List<T> enemies,
        ObjectPool<XpGem> xpPool,
        ObjectPool<Bullet> bulletPool,
        List<Bullet> newBulletsBuffer, // <- Bufor na nowe pociski
        Action<Vector2, float, bool, Color?>? onDamage = null,
        Action<Vector2, float, Color>? onDeath = null,
        Action<Vector2>? onExplosion = null) where T : Enemy
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            var bullet = bullets[i];
            if (!bullet.IsActive) continue;

            for (int j = 0; j < enemies.Count; j++)
            {
                var enemy = enemies[j];
                if (!enemy.IsActive) continue;
                if (bullet.HitEnemies.Contains(enemy)) continue;

                float enemyRadius = enemy.Size * 0.5f;
                float radiusSum = 4f + enemyRadius;

                float distSq = Vector2.DistanceSquared(bullet.Position, enemy.Position);
                if (distSq < radiusSum * radiusSum)
                {
                    bullet.HitEnemies.Add(enemy);
                    enemy.Health -= bullet.Damage;
                    onDamage?.Invoke(enemy.Position + new Vector2(0f, -enemy.Size * 0.35f), bullet.Damage, bullet.IsCrit, null);

                    ApplyKnockback(player, bullet, enemy);
                    ApplyHitEffects(bullet, enemy);
                    HandlePlayerBuffs(player, bullet);
                    HandleSplitShot(player, bulletPool, newBulletsBuffer, bullet, enemy); // Dodajemy do bufora
                    HandleExplosions(player, bullet, enemies, xpPool, onDamage, onDeath, onExplosion);

                    bullet.PierceCount--;
                    if (bullet.PierceCount <= 0)
                    {
                        bullet.IsActive = false;
                    }

                    if (enemy.Health <= 0f)
                    {
                        HandleEnemyDeath(enemy, xpPool, onDeath);
                    }

                    if (!bullet.IsActive)
                    {
                        break;
                    }
                }
            }
        }
    }

    private static void ApplyKnockback(Player player, Bullet bullet, Enemy enemy)
    {
        if (!bullet.IsReturning)
        {
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
        }
    }

    private static void ApplyHitEffects(Bullet bullet, Enemy enemy)
    {
        for (int k = 0; k < bullet.HitEffects.Count; k++)
        {
            bullet.HitEffects[k].OnHit(enemy, ref bullet);
        }
    }

    private static void HandlePlayerBuffs(Player player, Bullet bullet)
    {
        if (player.HasSpeedOnHit && System.Random.Shared.NextDouble() < 0.3f)
        {
            player.SpeedBoostTimer = 3f;
        }

        if (player.HasVampiricTouch)
        {
            player.Heal(bullet.Damage * 0.15f);
        }
    }

    private static void HandleSplitShot(Player player, ObjectPool<Bullet> bulletPool, List<Bullet> newBulletsBuffer, Bullet bullet, Enemy enemy)
    {
        if (player.HasSplitShot && !bullet.IsReturning && bullet.PierceCount > 0 && !bullet.HasSplit)
        {
            bullet.HasSplit = true;
            float splitAngle = (float)(System.Math.PI * 0.25);
            Vector2 originalDir = bullet.Velocity;
            if (originalDir.LengthSquared() > 0.0001f)
            {
                float speed = originalDir.Length();
                originalDir.Normalize();

                Vector2 leftDir = new Vector2(
                    originalDir.X * (float)Math.Cos(-splitAngle) - originalDir.Y * (float)Math.Sin(-splitAngle),
                    originalDir.X * (float)Math.Sin(-splitAngle) + originalDir.Y * (float)Math.Cos(-splitAngle)
                );
                Vector2 rightDir = new Vector2(
                    originalDir.X * (float)Math.Cos(splitAngle) - originalDir.Y * (float)Math.Sin(splitAngle),
                    originalDir.X * (float)Math.Sin(splitAngle) + originalDir.Y * (float)Math.Cos(splitAngle)
                );

                for (int s = 0; s < 2; s++)
                {
                    var split = bulletPool.Get();
                    split.Position = bullet.Position;
                    split.Velocity = (s == 0 ? leftDir : rightDir) * speed;
                    split.Damage = bullet.Damage * 0.6f;
                    split.PierceCount = bullet.PierceCount;
                    split.MaxDistance = bullet.MaxDistance;
                    split.MaxLifetime = bullet.MaxLifetime;
                    split.DistanceTraveled = bullet.DistanceTraveled;
                    split.IsCrit = false;
                    split.IsActive = true; // Zostanie przetworzony w następnej klatce z bufora
                    split.HasSplit = true;

                    foreach (var h in bullet.HitEnemies) split.HitEnemies.Add(h);
                    split.HitEnemies.Add(enemy);
                    foreach (var mod in bullet.TrajectoryModifiers) split.TrajectoryModifiers.Add(mod);
                    foreach (var eff in bullet.HitEffects) split.HitEffects.Add(eff);

                    newBulletsBuffer.Add(split);
                }
            }
        }
    }

    private static void HandleExplosions<T>(Player player, Bullet bullet, List<T> enemies, ObjectPool<XpGem> xpPool, Action<Vector2, float, bool, Color?>? onDamage, Action<Vector2, float, Color>? onDeath, Action<Vector2>? onExplosion) where T : Enemy
    {
        if (player.HasExplosiveBullets)
        {
            onExplosion?.Invoke(bullet.Position);
            float explosionRadius = 80f;
            float explosionRadiusSq = explosionRadius * explosionRadius;
            float explosionDamage = bullet.Damage * 0.5f;

            for (int e = 0; e < enemies.Count; e++)
            {
                var other = enemies[e];
                if (!other.IsActive || bullet.HitEnemies.Contains(other)) continue;

                float otherDistSq = Vector2.DistanceSquared(bullet.Position, other.Position);
                if (otherDistSq < explosionRadiusSq)
                {
                    other.Health -= explosionDamage;
                    onDamage?.Invoke(other.Position + new Vector2(0f, -other.Size * 0.35f), explosionDamage, false, new Color(255, 180, 50));

                    if (other.Health <= 0f)
                    {
                        HandleEnemyDeath(other, xpPool, onDeath);
                    }
                }
            }
        }
    }

    private static void HandleEnemyDeath(Enemy enemy, ObjectPool<XpGem> xpPool, Action<Vector2, float, Color>? onDeath)
    {
        enemy.IsActive = false;
        onDeath?.Invoke(enemy.Position, enemy.Size, GetEnemyColorFromSize(enemy.Size));

        for (int g = 0; g < enemy.XpDropCount; g++)
        {
            var xp = xpPool.Get();
            
            // Użycie Lookup Table dla drop kierunków
            int lutIndex = (int)(((float)g / enemy.XpDropCount) * DropDirections.Length);
            lutIndex = Math.Clamp(lutIndex, 0, DropDirections.Length - 1);
            
            Vector2 offset = DropDirections[lutIndex] * (g > 0 ? 14f : 0f);
            xp.Position = enemy.Position + offset;
            xp.Value = 1;
        }
    }

    private static void ResolveEnemyPlayerCollisions<T>(Player player, List<T> enemies) where T : Enemy
    {
        float playerRadiusSq = 14f * 14f;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (!enemy.IsActive) continue;

            float enemyRadius = enemy.Size * 0.5f;
            float radiusSum = 14f + enemyRadius;

            float distSq = Vector2.DistanceSquared(enemy.Position, player.Position);
            if (distSq < radiusSum * radiusSum)
            {
                if (enemy.AttackCooldown <= 0f)
                {
                    if (player.HasPhoenixProtocol && !player.PhoenixProtocolUsed)
                    {
                        if (player.CurrentHealth - enemy.Damage <= 0f)
                        {
                            player.PhoenixProtocolUsed = true;
                            player.CurrentHealth = player.MaxHealth.Value * 0.5f;
                            player.InvulnerabilityTimer = 2f;
                            enemy.AttackCooldown = 0.5f;
                            continue;
                        }
                    }

                    player.TakeDamage(enemy.Damage);
                    enemy.AttackCooldown = 0.5f;
                }
            }
        }
    }

    private static void ResolveGemCollisions(Player player, ObjectPool<XpGem> xpPool, float dt, ref bool leveledUpOut)
    {
        float pickupRadiusSq = player.PickupRadius.Value * player.PickupRadius.Value;
        float collectRadiusSq = 400f;
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
                    if (leveledUp) leveledUpOut = true;
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
        out bool leveledUpOut,
        Action<Vector2, float, bool, Color?>? onDamage = null,
        Action<Vector2, float, Color>? onDeath = null,
        Action<Vector2>? onExplosion = null)
    {
        leveledUpOut = false;
        var newBulletsBuffer = new List<Bullet>(); // Bufor Split Shota

        var bullets = bulletPool.Items;
        ResolveBulletEnemyCollisions(player, bullets, basicPool.Items, xpPool, bulletPool, newBulletsBuffer, onDamage, onDeath, onExplosion);
        ResolveBulletEnemyCollisions(player, bullets, tankPool.Items, xpPool, bulletPool, newBulletsBuffer, onDamage, onDeath, onExplosion);
        ResolveBulletEnemyCollisions(player, bullets, fastPool.Items, xpPool, bulletPool, newBulletsBuffer, onDamage, onDeath, onExplosion);

        ResolveEnemyPlayerCollisions(player, basicPool.Items);
        ResolveEnemyPlayerCollisions(player, tankPool.Items);
        ResolveEnemyPlayerCollisions(player, fastPool.Items);

        ResolveGemCollisions(player, xpPool, dt, ref leveledUpOut);
    }

    public static void ResolveBossCollisions(
        Player player,
        ObjectPool<Bullet> bulletPool,
        Boss boss,
        ObjectPool<XpGem> xpPool,
        float dt,
        out bool leveledUpOut,
        out bool bossDiedOut,
        Action<Vector2, float, bool, Color?>? onDamage = null,
        Action<Vector2, float, Color>? onDeath = null,
        Action<Vector2>? onExplosion = null)
    {
        leveledUpOut = false;
        bossDiedOut = false;
        if (boss == null || !boss.IsActive) return;

        var newBulletsBuffer = new List<Bullet>(); // Bufor Split Shota

        var bullets = bulletPool.Items;
        float bossRadiusSumSq = (4f + boss.Size * 0.5f) * (4f + boss.Size * 0.5f);

        for (int i = 0; i < bullets.Count; i++)
        {
            var bullet = bullets[i];
            if (!bullet.IsActive || bullet.HitEnemies.Contains(boss)) continue;

            if (Vector2.DistanceSquared(bullet.Position, boss.Position) < bossRadiusSumSq)
            {
                bullet.HitEnemies.Add(boss);
                boss.Health -= bullet.Damage;
                onDamage?.Invoke(boss.Position + new Vector2(0f, -boss.Size * 0.35f), bullet.Damage, bullet.IsCrit, null);

                ApplyHitEffects(bullet, boss);
                HandleSplitShot(player, bulletPool, newBulletsBuffer, bullet, boss);
                
                if (player.HasExplosiveBullets) onExplosion?.Invoke(bullet.Position);

                bullet.PierceCount--;
                if (bullet.PierceCount <= 0) bullet.IsActive = false;

                if (boss.Health <= 0f)
                {
                    bossDiedOut = true;
                    HandleEnemyDeath(boss, xpPool, onDeath);
                    break;
                }
            }
        }

        if (boss.IsActive)
        {
            float playerBossDistSq = Vector2.DistanceSquared(boss.Position, player.Position);
            if (playerBossDistSq < (14f + boss.Size * 0.5f) * (14f + boss.Size * 0.5f) && boss.AttackCooldown <= 0f)
            {
                player.TakeDamage(boss.Damage);
                boss.AttackCooldown = 0.5f;
            }
        }

        ResolveGemCollisions(player, xpPool, dt, ref leveledUpOut);
    }

    private static Color GetEnemyColorFromSize(float size)
    {
        float t = MathHelper.Clamp((size - 20f) / (48f - 20f), 0f, 1f);
        return new Color((byte)MathHelper.Lerp(255f, 110f, t), (byte)MathHelper.Lerp(70f, 0f, t), (byte)MathHelper.Lerp(70f, 0f, t));
    }
}