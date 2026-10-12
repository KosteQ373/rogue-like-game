using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace rogue_like;

public interface ITrajectoryModifier
{
    void Update(ref Vector2 position, ref Vector2 velocity, float dt, Vector2 playerPos);
    
    // Opcjonalne — domyślnie nie robi nic. Używane przez modifikatory potrzebujące dostępu do wrogów (np. homing).
    void UpdateWithEnemies(ref Vector2 position, ref Vector2 velocity, float dt, Vector2 playerPos, IEnumerable<Enemy> enemies)
    {
        Update(ref position, ref velocity, dt, playerPos);
    }
}

public interface IHitEffect
{
    void OnHit(Enemy enemy, ref Bullet bullet);
}

public class ReturningTrajectoryModifier : ITrajectoryModifier
{
    private float _timer = 0f;
    private readonly float _returnDelay;
    private bool _returning = false;

    public bool IsReturning => _returning;

    public ReturningTrajectoryModifier(float returnDelay = 0.35f)
    {
        _returnDelay = returnDelay;
    }

    public void Update(ref Vector2 position, ref Vector2 velocity, float dt, Vector2 playerPos)
    {
        _timer += dt;
        if (!_returning && _timer >= _returnDelay)
        {
            _returning = true;
        }

        if (_returning)
        {
            Vector2 dirToPlayer = playerPos - position;
            if (dirToPlayer.LengthSquared() > 0.001f)
            {
                dirToPlayer.Normalize();
                velocity = dirToPlayer * 700f;
            }
        }
    }
}

public class FireHitEffect : IHitEffect
{
    private readonly float _duration;
    private readonly float _damagePerTick;

    public FireHitEffect(float duration = 4f, float damagePerTick = 8f)
    {
        _duration = duration;
        _damagePerTick = damagePerTick;
    }

    public void OnHit(Enemy enemy, ref Bullet bullet)
    {
        enemy.ApplyBurn(_duration, _damagePerTick);
    }
}

public class FreezeHitEffect : IHitEffect
{
    private readonly float _duration;

    public FreezeHitEffect(float duration = 3f)
    {
        _duration = duration;
    }

    public void OnHit(Enemy enemy, ref Bullet bullet)
    {
        enemy.ApplyFreeze(_duration);
    }
}

// Pocisk powoli skręca w stronę najbliższego aktywnego wroga
// Pocisk powoli skręca w stronę najbliższego aktywnego wroga w zasięgu
public class HomingTrajectoryModifier : ITrajectoryModifier
{
    // Siła skręcania — radiany na sekundę
    private readonly float _turnSpeed;
    // Próg odległości — limit szukania wrogów
    private readonly float _lockRangeSq;

    public HomingTrajectoryModifier(float turnSpeed = 4.0f, float lockRange = 400f)
    {
        _turnSpeed = turnSpeed;
        _lockRangeSq = lockRange * lockRange; // Używamy kwadratu odległości
    }

    public void Update(ref Vector2 position, ref Vector2 velocity, float dt, Vector2 playerPos)
    {
        // Puste — homing wymaga wrogów
    }

    public void UpdateWithEnemies(ref Vector2 position, ref Vector2 velocity, float dt, Vector2 playerPos, IEnumerable<Enemy> enemies)
    {
        Enemy? nearest = null;
        float nearestDistSq = _lockRangeSq; // Szuka tylko najbliższych w zasięgu _lockRange

        // Wrogowie w tym IEnumerable to teraz bardzo krótka lista z sąsiednich kafelków SpatialGrid
        foreach (var enemy in enemies)
        {
            if (!enemy.IsActive) continue;
            float distSq = Vector2.DistanceSquared(position, enemy.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = enemy;
            }
        }

        if (nearest != null)
        {
            Vector2 dirToTarget = nearest.Position - position;
            if (dirToTarget.LengthSquared() > 0.001f)
            {
                dirToTarget.Normalize();

                float speed = velocity.Length();
                if (speed < 0.001f)
                {
                    velocity = dirToTarget * 600f;
                    return;
                }

                float currentAngle = (float)Math.Atan2(velocity.Y, velocity.X);
                float targetAngle = (float)Math.Atan2(dirToTarget.Y, dirToTarget.X);

                float angleDiff = targetAngle - currentAngle;
                while (angleDiff > (float)Math.PI) angleDiff -= (float)(2 * Math.PI);
                while (angleDiff < -(float)Math.PI) angleDiff += (float)(2 * Math.PI);

                float maxTurn = _turnSpeed * dt;
                if (angleDiff > maxTurn) angleDiff = maxTurn;
                if (angleDiff < -maxTurn) angleDiff = -maxTurn;

                float newAngle = currentAngle + angleDiff;
                velocity = new Vector2((float)Math.Cos(newAngle), (float)Math.Sin(newAngle)) * speed;
            }
        }
    }
}