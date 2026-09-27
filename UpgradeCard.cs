using System;

namespace rogue_like;

public enum CardRarity
{
    Common,
    Rare,
    Epic
}

public class UpgradeCard
{
    public string Title { get; set; }
    public string Description { get; set; }
    public CardRarity Rarity { get; set; }
    public Action<Player> ApplyEffect { get; set; }
    public bool CanRepeat { get; set; } = true;

    public UpgradeCard(string title, string description, CardRarity rarity, Action<Player> applyEffect, bool canRepeat = true)
    {
        Title = title;
        Description = description;
        Rarity = rarity;
        ApplyEffect = applyEffect;
        CanRepeat = canRepeat;
    }
}
