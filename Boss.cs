using System;
using Microsoft.Xna.Framework;

namespace rogue_like;

public abstract class Boss : Enemy
{
    public abstract string BossTitle { get; }
}

public class ColossusBoss : Boss
{
    public override string BossTitle => "COLOSSUS";

    public override void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        Speed = 120f * speedMultiplier;
        Health = 2500f * hpMultiplier;
        MaxHealth = Health;
        Damage = 25f * damageMultiplier;
        Size = 96f;
        XpDropCount = Math.Max(1, (int)(30f * hpMultiplier));
    }

    public override void Update(float dt, Vector2 playerPos, ObjectPool<XpGem>? xpPool = null, Action<Vector2, float, bool, Color?>? onDamageNumber = null, Action<Vector2, float, Color>? onDeath = null, float speedMultiplier = 1f, List<Enemy>? neighbors = null)
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= dt;
        }

        if (KnockbackTimer > 0f)
        {
            KnockbackTimer -= dt * 2f;
        }

        if (FreezeTimer > 0f)
        {
            FreezeTimer -= dt;
        }

        if (BurnTimer > 0f)
        {
            BurnTimer -= dt;
            BurnTickTimer += dt;
            if (BurnTickTimer >= 1.0f)
            {
                BurnTickTimer -= 1.0f;
                Health -= BurnDamagePerTick;
                onDamageNumber?.Invoke(Position + new Vector2(0f, -Size * 0.35f), BurnDamagePerTick, false, new Color(255, 140, 30));

                if (Health <= 0f)
                {
                    IsActive = false;
                    onDeath?.Invoke(Position, Size, new Color(225, 25, 55));
                    return;
                }
            }
        }

        Vector2 dir = playerPos - Position;
        float distSq = dir.LengthSquared();
        if (distSq > 0.001f)
        {
            dir.Normalize();
            float currentSpeed = KnockbackTimer > 0f ? Speed * 0.4f : Speed;
            if (FreezeTimer > 0f)
            {
                currentSpeed *= 0.5f;
            }
            currentSpeed *= speedMultiplier;
            Position += dir * currentSpeed * dt;
        }

        // Clamp to arena bounds respecting boss size
        float halfSize = Size * 0.5f;
        Position.X = MathHelper.Clamp(Position.X, halfSize, 2000f - halfSize);
        Position.Y = MathHelper.Clamp(Position.Y, halfSize, 2000f - halfSize);
    }
}
