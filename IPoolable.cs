namespace rogue_like;

public interface IPoolable
{
    bool IsActive { get; set; }
    void Reset();
}
