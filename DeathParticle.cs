using Microsoft.Xna.Framework;

namespace rogue_like;

// Pojedynczy piksel rozpadającej się kostki wroga
public class DeathParticle : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public Vector2 Velocity;
    public float Lifetime;
    public float MaxLifetime = 0.5f;
    public Color Color;
    public int Size = 3;

    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Lifetime = 0f;
        MaxLifetime = 0.5f;
        Color = Color.White;
        Size = 3;
    }

    public void Update(float dt)
    {
        Lifetime += dt;
        if (Lifetime >= MaxLifetime)
        {
            IsActive = false;
            return;
        }

        Position += Velocity * dt;
        Velocity *= (1f - dt * 3f);
    }
}

// Okrągły błysk rozszerzający się dookoła zabitego wroga
public class DeathRing : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public float Radius;
    public float MaxRadius = 60f;
    public float ExpandSpeed = 200f;
    public float Lifetime;
    public float MaxLifetime = 0.35f;
    public Color Color;

    public void Reset()
    {
        Position = Vector2.Zero;
        Radius = 0f;
        MaxRadius = 60f;
        ExpandSpeed = 200f;
        Lifetime = 0f;
        MaxLifetime = 0.35f;
        Color = Color.White;
    }

    public void Update(float dt)
    {
        Lifetime += dt;
        if (Lifetime >= MaxLifetime)
        {
            IsActive = false;
            return;
        }

        Radius += ExpandSpeed * dt;
    }
}
