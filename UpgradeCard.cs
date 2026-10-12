using System;

namespace rogue_like;

public enum CardRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

[Flags]
public enum UpgradeTags
{
    None = 0,
    Boomerang = 1 << 0,
    InfinityRange = 1 << 1,
    Incendiary = 1 << 2,
    Frostbite = 1 << 3,
    Explosive = 1 << 4,
    Pierce = 1 << 5,
    Range = 1 << 6
}

public class UpgradeCard
{
    public string Title { get; set; }
    public string Description { get; set; }
    public CardRarity Rarity { get; set; }
    public Action<Player> ApplyEffect { get; set; }
    public bool CanRepeat { get; set; }
    public UpgradeTags Tags { get; set; }
    public UpgradeTags MutuallyExclusiveWith { get; set; }

    public UpgradeCard(
        string title,
        string description,
        CardRarity rarity,
        Action<Player> applyEffect,
        bool canRepeat = true,
        UpgradeTags tags = UpgradeTags.None,
        UpgradeTags mutuallyExclusiveWith = UpgradeTags.None)
    {
        Title = title;
        Description = description;
        Rarity = rarity;
        ApplyEffect = applyEffect;
        CanRepeat = canRepeat;
        Tags = tags;
        MutuallyExclusiveWith = mutuallyExclusiveWith;
    }
}