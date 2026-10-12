using System.Collections.Generic;

namespace rogue_like;

public class StatModifier
{
    public float Value;
    public StatModifierType Type;

    public StatModifier(float value, StatModifierType type)
    {
        Value = value;
        Type = type;
    }
}

public enum StatModifierType
{
    Flat = 0,
    PercentAdd = 1,
    PercentMult = 2
}

public class CharacterStat
{
    private readonly float _baseValue;
    private float _cachedValue;
    private bool _isDirty = true;
    private readonly List<StatModifier> _modifiers = new();

    public float Value
    {
        get
        {
            if (_isDirty)
            {
                _cachedValue = CalculateFinalValue();
                _isDirty = false;
            }
            return _cachedValue;
        }
    }

    public CharacterStat(float baseValue)
    {
        _baseValue = baseValue;
        _cachedValue = baseValue;
    }

    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        _isDirty = true;
    }

    public void RemoveModifier(StatModifier mod)
    {
        _modifiers.Remove(mod);
        _isDirty = true;
    }

    private float CalculateFinalValue()
    {
        float sumFlat = 0f;
        float sumPercentAdd = 0f;
        float finalMult = 1f;

        for (int i = 0; i < _modifiers.Count; i++)
        {
            var mod = _modifiers[i];
            if (mod.Type == StatModifierType.Flat)
            {
                sumFlat += mod.Value;
            }
            else if (mod.Type == StatModifierType.PercentAdd)
            {
                sumPercentAdd += mod.Value;
            }
            else if (mod.Type == StatModifierType.PercentMult)
            {
                finalMult *= (1f + mod.Value);
            }
        }

        float basePlusFlat = _baseValue + sumFlat;
        return (basePlusFlat * (1f + sumPercentAdd)) * finalMult;
    }
}
