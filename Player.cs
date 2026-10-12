using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace rogue_like;

public class Player
{
    public Vector2 Position;
    public CharacterStat MoveSpeed = new CharacterStat(200f);
    public CharacterStat AttackDamage = new CharacterStat(15f);
    public CharacterStat PickupRadius = new CharacterStat(100f);
    public CharacterStat MaxHealth = new CharacterStat(100f);
    public CharacterStat AttackSpeed = new CharacterStat(1.0f);
    public CharacterStat CriticalChance = new CharacterStat(0.05f);
    public CharacterStat CriticalDamageMultiplier = new CharacterStat(1.5f);
    public CharacterStat Pierce = new CharacterStat(1f);
    public CharacterStat DodgeChance = new CharacterStat(0f);
    public CharacterStat Range = new CharacterStat(450f);
    public CharacterStat Knockback = new CharacterStat(200f);
    public int ProjectilesCount = 1;
    public bool HasBoomerang = false;
    public bool HasInfinityRange = false;
    public bool HasFireBullets = false;
    public bool HasFreezeBullets = false;
    public bool HasHomingBullets = false;
    public bool HasSpeedOnHit = false;
    public bool HasExplosiveBullets = false;
    public bool HasVampiricTouch = false;
    public bool HasSplitShot = false;
    public bool HasOvercharge = false;
    public bool HasTimeSlow = false;
    public bool HasPhoenixProtocol = false;
    public bool PhoenixProtocolUsed = false;
    public bool HasAutoAim = false;
    public float CurrentHealth;

    public float ShootTimer;
    public float ShootInterval = 0.5f;
    public float InvulnerabilityTimer;
    public float SpeedBoostTimer;
    public float OverchargeTimer;
    public float RegenerationTimer;
    public float TimeSlowTimer;

    // Zwraca aktualny mnożnik prędkości (1x lub 2x gdy buff aktywny)
    public float SpeedMultiplier => SpeedBoostTimer > 0f ? 2f : 1f;

    public int Level = 1;
    public float Xp = 0f;
    public float XpToNextLevel = 10f;

    public Player(Vector2 startPos)
    {
        Position = startPos;
        CurrentHealth = MaxHealth.Value;
    }

    public void Update(float dt)
    {
        if (InvulnerabilityTimer > 0f)
        {
            InvulnerabilityTimer -= dt;
        }
        if (SpeedBoostTimer > 0f)
        {
            SpeedBoostTimer -= dt;
        }
        if (OverchargeTimer > 0f)
        {
            OverchargeTimer -= dt;
        }
        if (RegenerationTimer > 0f)
        {
            RegenerationTimer -= dt;
        }
        if (TimeSlowTimer > 0f)
        {
            TimeSlowTimer -= dt;
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth = System.Math.Min(MaxHealth.Value, CurrentHealth + amount);
    }

    public void TakeDamage(float amount)
    {
        if (System.Random.Shared.NextDouble() < DodgeChance.Value)
        {
            InvulnerabilityTimer = 0.3f;
            return;
        }
        CurrentHealth = System.Math.Max(0f, CurrentHealth - amount);
        InvulnerabilityTimer = 0.2f;
    }

    public void AddXp(float amount, out bool leveledUp)
    {
        leveledUp = false;
        Xp += amount;
        if (Xp >= XpToNextLevel)
        {
            Xp -= XpToNextLevel;
            Level++;
            XpToNextLevel += 5f;
            leveledUp = true;
        }
    }
}
