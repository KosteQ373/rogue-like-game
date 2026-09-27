using System.Collections.Generic;

namespace rogue_like;

public static class UpgradeDatabase
{
    public static List<UpgradeCard> GetAllUpgrades()
    {
        return new List<UpgradeCard>
        {
            new UpgradeCard(
                "Sharpened Blade",
                "+5 Flat Attack Damage",
                CardRarity.Common,
                player => player.AttackDamage.AddModifier(new StatModifier(5f, StatModifierType.Flat))
            ),
            new UpgradeCard(
                "Adrenaline Rush",
                "+25% Movement Speed",
                CardRarity.Common,
                player => player.MoveSpeed.AddModifier(new StatModifier(0.25f, StatModifierType.PercentAdd))
            ),
            new UpgradeCard(
                "Magnetic Core",
                "+100 Pickup Radius",
                CardRarity.Common,
                player => player.PickupRadius.AddModifier(new StatModifier(100f, StatModifierType.Flat))
            ),
            new UpgradeCard(
                "Vitality Boost",
                "+25 Max Health & Heal",
                CardRarity.Common,
                player =>
                {
                    player.MaxHealth.AddModifier(new StatModifier(25f, StatModifierType.Flat));
                    player.Heal(25f);
                }
            ),
            new UpgradeCard(
                "Rapid Fire",
                "+20% Attack Speed",
                CardRarity.Common,
                player => player.AttackSpeed.AddModifier(new StatModifier(0.2f, StatModifierType.PercentAdd))
            ),
            new UpgradeCard(
                "Sharpened Focus",
                "+10% Critical Chance",
                CardRarity.Common,
                player => player.CriticalChance.AddModifier(new StatModifier(0.1f, StatModifierType.Flat))
            ),
            new UpgradeCard(
                "Extended Barrel",
                "+250 Attack Range",
                CardRarity.Common,
                player => player.Range.AddModifier(new StatModifier(250f, StatModifierType.Flat))
            ),
            new UpgradeCard(
                "Heavy Impact",
                "+150 Knockback",
                CardRarity.Common,
                player => player.Knockback.AddModifier(new StatModifier(150f, StatModifierType.Flat))
            ),
            new UpgradeCard(
                "Berserk Pact",
                "+40% Attack Damage, \nbut -10% Move Speed",
                CardRarity.Rare,
                player =>
                {
                    player.AttackDamage.AddModifier(new StatModifier(0.4f, StatModifierType.PercentAdd));
                    player.MoveSpeed.AddModifier(new StatModifier(-0.1f, StatModifierType.PercentAdd));
                }
            ),
            new UpgradeCard(
                "Deadly Precision",
                "+50% Critical Damage Multiplier",
                CardRarity.Rare,
                player => player.CriticalDamageMultiplier.AddModifier(new StatModifier(0.5f, StatModifierType.PercentAdd))
            ),
            new UpgradeCard(
                "Piercing Spikes",
                "+1 Bullet Pierce",
                CardRarity.Rare,
                player => player.Pierce.AddModifier(new StatModifier(1f, StatModifierType.Flat))
            ),
            new UpgradeCard(
                "Shadow Reflexes",
                "+15% Dodge Chance to evade hits",
                CardRarity.Rare,
                player => player.DodgeChance.AddModifier(new StatModifier(0.15f, StatModifierType.Flat))
            ),
            new UpgradeCard(
                "Sniper Scope",
                "+25% Attack Range",
                CardRarity.Rare,
                player => player.Range.AddModifier(new StatModifier(0.25f, StatModifierType.PercentAdd))
            ),
            new UpgradeCard(
                "Shockwave",
                "+50% Knockback",
                CardRarity.Rare,
                player => player.Knockback.AddModifier(new StatModifier(0.5f, StatModifierType.PercentAdd))
            ),
            new UpgradeCard(
                "Triple Shot",
                "Fires 3 projectiles in a spread arc",
                CardRarity.Epic,
                player => player.ProjectilesCount += 2
            ),
            new UpgradeCard(
                "Boomerang Echo",
                "Projectiles return to \n the player after flying\n +1 Pierce",
                CardRarity.Epic,
                player =>
                {
                    player.HasBoomerang = true;
                    player.Pierce.AddModifier(new StatModifier(1f, StatModifierType.Flat));

                },
                canRepeat: false
            )
        };
    }
}
