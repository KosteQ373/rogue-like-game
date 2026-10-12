using Microsoft.Xna.Framework;

namespace rogue_like;

public class XpGem : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public int Value = 1;

    public void Reset()
    {
        Position = Vector2.Zero;
        Value = 1;
    }
}
