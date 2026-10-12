using System;
using Microsoft.Xna.Framework;

namespace rogue_like;

public abstract class Enemy : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public float Speed;
    public float Health;
    public float MaxHealth;
    public float Damage;
    public float AttackCooldown = 0f;
    public float KnockbackTimer = 0f;
    public float Size = 24f;
    public int XpDropCount = 1;
    public Vector2 Velocity;

    public float BurnTimer = 0f;
    public float BurnTickTimer = 0f;
    public float BurnDamagePerTick = 8f;
    public float FreezeTimer = 0f;

    public bool IsBurning => BurnTimer > 0f;
    public bool IsFrozen => FreezeTimer > 0f;

    public void ApplyBurn(float duration = 4f, float damagePerTick = 8f)
    {
        BurnTimer = duration;
        BurnDamagePerTick = damagePerTick;
    }

    public void ApplyFreeze(float duration = 3f)
    {
        FreezeTimer = duration;
    }

    public virtual void Reset()
    {
        Position = Vector2.Zero;
        AttackCooldown = 0f;
        KnockbackTimer = 0f;
        BurnTimer = 0f;
        BurnTickTimer = 0f;
        FreezeTimer = 0f;
        Velocity = Vector2.Zero;
    }

    public abstract void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f);

    public virtual void Update(float dt, Vector2 playerPos, ObjectPool<XpGem>? xpPool = null, Action<Vector2, float, bool, Color?>? onDamageNumber = null, Action<Vector2, float, Color>? onDeath = null, float speedMultiplier = 1f,List<Enemy>? neighbors = null)
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= dt;
        }

        if (KnockbackTimer > 0f)
        {
            KnockbackTimer -= dt;
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
                onDamageNumber?.Invoke(Position + new Vector2(0f, -Size * 0.4f), BurnDamagePerTick, false, new Color(255, 140, 30));

                if (Health <= 0f)
                {
                    IsActive = false;
                    onDeath?.Invoke(Position, Size, new Color(255, 140, 30));
                    if (xpPool != null)
                    {
                        for (int g = 0; g < XpDropCount; g++)
                        {
                            var xp = xpPool.Get();
                            float angle = g * (MathHelper.TwoPi / XpDropCount);
                            Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (g > 0 ? 14f : 0f);
                            xp.Position = Position + offset;
                            xp.Value = 1;
                        }
                    }
                    return;
                }
            }
        }

        if (KnockbackTimer <= 0f)
        {
            Vector2 targetForce = playerPos - Position;
            if (targetForce.LengthSquared() > 0.0001f) targetForce.Normalize();

            Vector2 desiredDir = targetForce;

            if (neighbors != null)
            {
                Vector2 separation = Vector2.Zero;
                Vector2 alignment = Vector2.Zero;
                Vector2 centerOfMass = Vector2.Zero;
                int count = 0;

                float perceptionRadiusSq = 160f * 160f;
                float separationRadiusSq = (Size * 2f) * (Size * 2f); // 1.3 to mnożnik rozpychania

                for (int i = 0; i < neighbors.Count; i++)
                {
                    var other = neighbors[i];
                    if (other == this || !other.IsActive) continue;

                    Vector2 diff = Position - other.Position;
                    float distSq = diff.LengthSquared();

                    if (distSq < perceptionRadiusSq && distSq > 0.0001f)
                    {
                        alignment += other.Velocity;
                        centerOfMass += other.Position;
                        count++;

                        if (distSq < separationRadiusSq)
                        {
                            separation += diff * (separationRadiusSq / distSq);
                        }
                    }
                }

                if (count > 0)
                {
                    alignment /= count;
                    if (alignment.LengthSquared() > 0.0001f) alignment.Normalize();

                    centerOfMass /= count;
                    Vector2 cohesion = centerOfMass - Position;
                    if (cohesion.LengthSquared() > 0.0001f) cohesion.Normalize();
                    if (separation.LengthSquared() > 0.0001f) separation.Normalize();

                    // Wagi zachowań stadnych
                    desiredDir = (separation * 4.5f) + (alignment * 0.4f) + (cohesion * 0.1f) + (targetForce * 1.2f);
                    if (desiredDir.LengthSquared() > 0.0001f) desiredDir.Normalize();
                }
            }

            float currentSpeed = Speed * speedMultiplier;
            if (FreezeTimer > 0f) currentSpeed *= 0.5f;

            // Płynne wchodzenie w zakręty
            Velocity = Vector2.Lerp(Velocity, desiredDir * currentSpeed, dt * 2.5f);
            Position += Velocity * dt;
        }
        else
        {
            // Podczas knockbacku powoli wyhamowuje
            Velocity = Vector2.Lerp(Velocity, Vector2.Zero, dt * 8f);
        }

        // Clamp to 2000x2000 arena bounds
        Position.X = MathHelper.Clamp(Position.X, 20f, 1980f);
        Position.Y = MathHelper.Clamp(Position.Y, 20f, 1980f);
    }
}

public class BasicEnemy : Enemy
{
    public override void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        Speed = (120f + playerLevel * 2f) * speedMultiplier;
        Health = 30f * hpMultiplier;
        MaxHealth = Health;
        Damage = 5f * damageMultiplier;
        Size = 24f;
        XpDropCount = Math.Max(1, (int)(1f * hpMultiplier));
    }
}

public class TankEnemy : Enemy
{
    public override void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        // 40% slower than basic
        Speed = (120f + playerLevel * 2f) * 0.6f * speedMultiplier;
        // 5x health
        Health = 150f * hpMultiplier;
        MaxHealth = Health;
        // 2x damage
        Damage = 10f * damageMultiplier;
        // 2x size (visual & hitbox)
        Size = 40f;
        // 3 xp gems
        XpDropCount = Math.Max(1, (int)(3f * hpMultiplier));
    }
}

public class FastEnemy : Enemy
{
    public override void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        // 50% faster than basic
        Speed = (120f + playerLevel * 2f) * 1.5f * speedMultiplier;
        // half health
        Health = 15f * hpMultiplier;
        MaxHealth = Health;
        Damage = 5f * damageMultiplier;
        Size = 20f;
        XpDropCount = Math.Max(1, (int)(1f * hpMultiplier));
    }
}
