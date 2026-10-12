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
                player => player.Range.AddModifier(new StatModifier(250f, StatModifierType.Flat)),
                canRepeat: true,
                tags: UpgradeTags.Range
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
                player => player.Pierce.AddModifier(new StatModifier(1f, StatModifierType.Flat)),
                tags: UpgradeTags.Pierce
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
                player => player.Range.AddModifier(new StatModifier(0.25f, StatModifierType.PercentAdd)),
                canRepeat: true,
                tags: UpgradeTags.Range
            ),
            new UpgradeCard(
                "Shockwave",
                "+50% Knockback",
                CardRarity.Rare,
                player => player.Knockback.AddModifier(new StatModifier(0.5f, StatModifierType.PercentAdd))
            ),
            new UpgradeCard(
                "Triple Shot",
                "add two additional projectiles in a spread arc",
                CardRarity.Epic,
                player => player.ProjectilesCount += 2
            ),
            new UpgradeCard(
                "Double Shot",
                "add one additional projectile in a spread arc",
                CardRarity.Rare,
                player => player.ProjectilesCount += 1
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
                canRepeat: false,
                tags: UpgradeTags.Boomerang | UpgradeTags.Pierce,
                mutuallyExclusiveWith: UpgradeTags.InfinityRange | UpgradeTags.Explosive
            ),
            new UpgradeCard(
                "Infinity Range",
                "Projectiles fly infinitely\nuntil the edge of the map",
                CardRarity.Epic,
                player =>
                {
                    player.HasInfinityRange = true;
                },
                canRepeat: false,
                tags: UpgradeTags.InfinityRange,
                mutuallyExclusiveWith: UpgradeTags.Boomerang | UpgradeTags.Range
            ),
            new UpgradeCard(
                "Incendiary Rounds",
                "Bullets ignite enemies on hit,\ndealing burn damage every sec",
                CardRarity.Epic,
                player =>
                {
                    player.HasFireBullets = true;
                },
                canRepeat: false,
                tags: UpgradeTags.Incendiary,
                mutuallyExclusiveWith: UpgradeTags.Frostbite
            ),
            new UpgradeCard(
                "Frostbite",
                "Bullets freeze enemies on hit,\nslowing them by 50% for 3s",
                CardRarity.Epic,
                player =>
                {
                    player.HasFreezeBullets = true;
                },
                canRepeat: false,
                tags: UpgradeTags.Frostbite,
                mutuallyExclusiveWith: UpgradeTags.Incendiary
            ),
            new UpgradeCard(
                "Guided Munitions",
                "Bullets slowly home in on\nthe nearest enemy",
                CardRarity.Rare,
                player =>
                {
                    player.HasHomingBullets = true;
                },
                canRepeat: false
            ),
            new UpgradeCard(
                "Ricochet Rush",
                "30% chance to hit an enemy\ngrants 2x speed for 3 seconds",
                CardRarity.Epic,
                player =>
                {
                    player.HasSpeedOnHit = true;
                },
                canRepeat: false
            ),
            new UpgradeCard(
                "Explosive Rounds",
                "Bullets explode on hit,\ndealing 50% damage in an area",
                CardRarity.Epic,
                player =>
                {
                    player.HasExplosiveBullets = true;
                },
                canRepeat: false,
                tags: UpgradeTags.Explosive,
                mutuallyExclusiveWith: UpgradeTags.Boomerang | UpgradeTags.Pierce
            ),

            // --- Nowe karty Common ---
            new UpgradeCard(
                "Iron Skin",
                "+15% Dodge Chance to evade hits",
                CardRarity.Common,
                player => player.DodgeChance.AddModifier(new StatModifier(0.15f, StatModifierType.Flat))
            ),
            new UpgradeCard(
                "Quick Recovery",
                "Regenerate 3 HP per second\nfor 10 seconds",
                CardRarity.Common,
                player =>
                {
                    player.RegenerationTimer = 10f;
                },
                canRepeat: true
            ),
            new UpgradeCard(
                "Critical Edge",
                "+20% Critical Damage Multiplier",
                CardRarity.Common,
                player => player.CriticalDamageMultiplier.AddModifier(new StatModifier(0.2f, StatModifierType.PercentAdd))
            ),
            new UpgradeCard(
                "Thick Skin",
                "+50 Max Health & Heal",
                CardRarity.Common,
                player =>
                {
                    player.MaxHealth.AddModifier(new StatModifier(50f, StatModifierType.Flat));
                    player.Heal(50f);
                }
            ),

            // --- Nowe karty Rare ---
            new UpgradeCard(
                "Split Shot",
                "Bullets split into 2\nwhen hitting an enemy",
                CardRarity.Rare,
                player =>
                {
                    player.HasSplitShot = true;
                },
                canRepeat: false
            ),
            new UpgradeCard(
                "Overcharge",
                "20% chance to fire 3 extra\nprojectiles on shoot",
                CardRarity.Rare,
                player =>
                {
                    player.HasOvercharge = true;
                },
                canRepeat: false
            ),
            new UpgradeCard(
                "Lifesteal",
                "Heal 15% of damage\ndealt to enemies",
                CardRarity.Rare,
                player =>
                {
                    player.HasVampiricTouch = true;
                },
                canRepeat: false
            ),

            // --- Nowe karty Epic ---
            new UpgradeCard(
                "Toxic Rounds",
                "Bullets poison enemies,\ndealing damage over 4 seconds",
                CardRarity.Epic,
                player =>
                {
                    // Otruje wrogów na 4 sekundy, zadając 8 obrażeń na sekundę
                    player.HasFireBullets = true; // Reużywa systemu ognia jako DOT
                },
                canRepeat: false,
                tags: UpgradeTags.Incendiary,
                mutuallyExclusiveWith: UpgradeTags.Incendiary | UpgradeTags.Frostbite
            ),

            // --- Nowe karty Legendary ---
            new UpgradeCard(
                "Time Dilation",
                "Enemies near you are slowed\nby 40% permanently",
                CardRarity.Legendary,
                player =>
                {
                    player.HasTimeSlow = true;
                },
                canRepeat: false
            ),
            new UpgradeCard(
                "Annihilator",
                "+80% Attack Damage,\n+30% Attack Speed,\nbut -20% Move Speed",
                CardRarity.Legendary,
                player =>
                {
                    player.AttackDamage.AddModifier(new StatModifier(0.8f, StatModifierType.PercentAdd));
                    player.AttackSpeed.AddModifier(new StatModifier(0.3f, StatModifierType.PercentAdd));
                    player.MoveSpeed.AddModifier(new StatModifier(-0.2f, StatModifierType.PercentAdd));
                },
                canRepeat: false
            ),
            new UpgradeCard(
                "Phoenix Protocol",
                "Once per wave, survive\na fatal hit with 50% HP",
                CardRarity.Legendary,
                player =>
                {
                    // Dodano flagę — logika w CollisionSystem
                    player.HasPhoenixProtocol = true;
                },
                canRepeat: false
            ),
            new UpgradeCard(
                "Auto Aim",
                "Automatically target the\nnearest enemy, but -20% Damage",
                CardRarity.Epic,
                player =>
                {
                    player.HasAutoAim = true;
                    player.AttackDamage.AddModifier(new StatModifier(-0.2f, StatModifierType.PercentAdd));
                },
                canRepeat: false
            )
        };
    }
}