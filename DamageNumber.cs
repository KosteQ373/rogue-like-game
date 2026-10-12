using Microsoft.Xna.Framework;

namespace rogue_like;

public class DamageNumber : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public Vector2 Velocity;
    public float Lifetime;
    public float MaxLifetime = 0.65f;
    public float DamageValue;
    public bool IsCrit;
    public Color CustomColor = Color.White;
    public bool HasCustomColor;
    public float Delay;

    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Lifetime = 0f;
        MaxLifetime = 0.65f;
        DamageValue = 0f;
        IsCrit = false;
        HasCustomColor = false;
        CustomColor = Color.White;
        Delay = 0f;
    }

    public void Update(float dt)
    {
        if (Delay > 0f)
        {
            Delay -= dt;
            return;
        }

        Lifetime += dt;
        if (Lifetime >= MaxLifetime)
        {
            IsActive = false;
            return;
        }

        Position += Velocity * dt;
        Velocity *= (1f - dt * 2.2f);
    }
}
