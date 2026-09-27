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

    public virtual void Reset()
    {
        Position = Vector2.Zero;
        AttackCooldown = 0f;
        KnockbackTimer = 0f;
    }

    public abstract void Init(int playerLevel);

    public virtual void Update(float dt, Vector2 playerPos)
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= dt;
        }

        if (KnockbackTimer > 0f)
        {
            KnockbackTimer -= dt;
        }
        else
        {
            Vector2 dir = playerPos - Position;
            float distSq = dir.LengthSquared();
            if (distSq > 0.001f)
            {
                dir.Normalize();
                Position += dir * Speed * dt;
            }
        }

        // Clamp to 2000x2000 arena bounds
        Position.X = MathHelper.Clamp(Position.X, 20f, 1980f);
        Position.Y = MathHelper.Clamp(Position.Y, 20f, 1980f);
    }
}

public class BasicEnemy : Enemy
{
    public override void Init(int playerLevel)
    {
        Speed = 120f + playerLevel * 2f;
        Health = 30f + playerLevel * 5f;
        MaxHealth = Health;
        Damage = 5f;
        Size = 24f;
        XpDropCount = 1;
    }
}

public class TankEnemy : Enemy
{
    public override void Init(int playerLevel)
    {
        // 40% slower than basic
        Speed = (120f + playerLevel * 2f) * 0.6f;
        // 5x health
        Health = (30f + playerLevel * 5f) * 5f;
        MaxHealth = Health;
        // 2x damage
        Damage = 10f;
        // 2x size (visual & hitbox)
        Size = 40f;
        // 3 xp gems
        XpDropCount = 3;
    }
}

public class FastEnemy : Enemy
{
    public override void Init(int playerLevel)
    {
        // 50% faster than basic
        Speed = (120f + playerLevel * 2f) * 1.5f;
        // half health
        Health = (30f + playerLevel * 5f) * 0.5f;
        MaxHealth = Health;
        Damage = 5f;
        Size = 20f;
        XpDropCount = 1;
    }
}
