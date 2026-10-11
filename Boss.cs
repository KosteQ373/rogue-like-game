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

    public override void Update(float dt, Vector2 playerPos)
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= dt;
        }

        if (KnockbackTimer > 0f)
        {
            KnockbackTimer -= dt * 2f;
        }

        Vector2 dir = playerPos - Position;
        float distSq = dir.LengthSquared();
        if (distSq > 0.001f)
        {
            dir.Normalize();
            float currentSpeed = KnockbackTimer > 0f ? Speed * 0.4f : Speed;
            Position += dir * currentSpeed * dt;
        }

        // Clamp to arena bounds respecting boss size
        float halfSize = Size * 0.5f;
        Position.X = MathHelper.Clamp(Position.X, halfSize, 2000f - halfSize);
        Position.Y = MathHelper.Clamp(Position.Y, halfSize, 2000f - halfSize);
    }
}
