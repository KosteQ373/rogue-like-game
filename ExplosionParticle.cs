using Microsoft.Xna.Framework;

namespace rogue_like;

// Pojedynczy piksel wybuchu pocisku
public class ExplosionParticle : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public Vector2 Velocity;
    public float Lifetime;
    public float MaxLifetime = 0.3f;
    public Color Color;
    public int Size = 3;

    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Lifetime = 0f;
        MaxLifetime = 0.3f;
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
        Velocity *= (1f - dt * 4f);
    }
}

// Pierścień wybuchu rozszerzający się od punktu trafienia
public class ExplosionRing : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public float Radius;
    public float MaxRadius = 80f;
    public float ExpandSpeed = 300f;
    public float Lifetime;
    public float MaxLifetime = 0.25f;
    public Color Color;

    public void Reset()
    {
        Position = Vector2.Zero;
        Radius = 0f;
        MaxRadius = 80f;
        ExpandSpeed = 300f;
        Lifetime = 0f;
        MaxLifetime = 0.25f;
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
