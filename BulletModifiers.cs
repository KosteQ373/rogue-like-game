using Microsoft.Xna.Framework;

namespace rogue_like;

public interface ITrajectoryModifier
{
    void Update(ref Vector2 position, ref Vector2 velocity, float dt, Vector2 playerPos);
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
