using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace rogue_like;

public class Bullet : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public Vector2 Velocity;
    public float Lifetime;
    public float MaxLifetime = 5f;
    public float MaxDistance = 1200f;
    public float DistanceTraveled;
    public float Damage;
    public bool IsCrit = false;
    public int PierceCount = 1;
    public bool HasSplit = false;

    public readonly List<ITrajectoryModifier> TrajectoryModifiers = new();
    public readonly List<IHitEffect> HitEffects = new();
    public readonly List<Enemy> HitEnemies = new();

    public void Update(float dt, Vector2 playerPos, IEnumerable<Enemy>? enemies = null)
    {
        Lifetime += dt;
        if (Lifetime >= MaxLifetime)
        {
            IsActive = false;
            return;
        }

        float stepDist = Velocity.Length() * dt;
        DistanceTraveled += stepDist;
        if (DistanceTraveled >= MaxDistance)
        {
            IsActive = false;
            return;
        }

        for (int i = 0; i < TrajectoryModifiers.Count; i++)
        {
            bool wasReturning = IsReturning;
            if (enemies != null)
            {
                TrajectoryModifiers[i].UpdateWithEnemies(ref Position, ref Velocity, dt, playerPos, enemies);
            }
            else
            {
                TrajectoryModifiers[i].Update(ref Position, ref Velocity, dt, playerPos);
            }
            if (!wasReturning && IsReturning)
            {
                HitEnemies.Clear();
            }

            if (TrajectoryModifiers[i] is ReturningTrajectoryModifier retMod && retMod.IsReturning)
            {
                if (Vector2.DistanceSquared(Position, playerPos) < 400f)
                {
                    IsActive = false;
                    return;
                }
            }
        }

        Position += Velocity * dt;
    }

    public bool IsReturning
    {
        get
        {
            for (int i = 0; i < TrajectoryModifiers.Count; i++)
            {
                if (TrajectoryModifiers[i] is ReturningTrajectoryModifier retMod && retMod.IsReturning)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Lifetime = 0f;
        MaxLifetime = 5f;
        MaxDistance = 1200f;
        DistanceTraveled = 0f;
        Damage = 10f;
        IsCrit = false;
        PierceCount = 1;
        HasSplit = false;
        TrajectoryModifiers.Clear();
        HitEffects.Clear();
        HitEnemies.Clear();
    }
}
