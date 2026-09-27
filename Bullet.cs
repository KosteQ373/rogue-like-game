using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace rogue_like;

public class Bullet : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public Vector2 Velocity;
    public float Lifetime;
    public float MaxLifetime = 10f;
    public float MaxDistance = 1200f;
    public float DistanceTraveled;
    public float Damage;
    public int PierceCount = 1;

    public readonly List<ITrajectoryModifier> TrajectoryModifiers = new();
    public readonly List<IHitEffect> HitEffects = new();

    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Lifetime = 0f;
        MaxDistance = 1200f;
        DistanceTraveled = 0f;
        Damage = 10f;
        PierceCount = 1;
        TrajectoryModifiers.Clear();
        HitEffects.Clear();
    }

    public void Update(float dt, Vector2 playerPos)
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
            TrajectoryModifiers[i].Update(ref Position, ref Velocity, dt, playerPos);
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
}
