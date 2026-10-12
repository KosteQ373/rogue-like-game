using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rogue_like;

public enum PlayerState
{
    Idle,
    Walk
}

public enum PlayerDirection
{
    Down,
    Up,
    Left,
    Right
}

public class Player
{
    public Vector2 Position;
    public CharacterStat MoveSpeed = new CharacterStat(200f);

    // Animacje
    private SpriteAnimation _idleAnimation;
    private SpriteAnimation _walkAnimation;
    private SpriteAnimation _currentAnimation;

    public PlayerState State { get; private set; } = PlayerState.Idle;
    public PlayerDirection Direction { get; private set; } = PlayerDirection.Down;
    public SpriteEffects SpriteEffects { get; private set; } = SpriteEffects.None;

    private const int FramesPerRow = 4;
    private const float WalkSpeed = 140f;
    private const float DrawScale = 2.5f;
    public CharacterStat AttackDamage = new CharacterStat(5f);
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

    public void InitializeAnimations(Texture2D idleTexture, Texture2D walkTexture)
    {
        int frameWidth = idleTexture.Width / FramesPerRow;
        int frameHeight = idleTexture.Height / FramesPerRow;

        _idleAnimation = new SpriteAnimation(idleTexture, frameWidth, frameHeight, 3, FramesPerRow);
        _walkAnimation = new SpriteAnimation(walkTexture, frameWidth, frameHeight, 3, FramesPerRow);
        _currentAnimation = _idleAnimation;
    }

    public void UpdateAnimation(GameTime gameTime)
    {
        _currentAnimation.Update(gameTime);
    }

    // Obsługa ruchu — WSAD steruje tylko pozycją, nie kierunkiem korpusu
    public void HandleMovement(Vector2 moveDir, float dt)
    {
        if (moveDir != Vector2.Zero)
        {
            moveDir = Vector2.Normalize(moveDir);
            Position += moveDir * WalkSpeed * dt;
            State = PlayerState.Walk;
            _currentAnimation = _walkAnimation;
        }
        else
        {
            State = PlayerState.Idle;
            _currentAnimation = _idleAnimation;
        }
    }

    // Kierunek korpusu i wiersz animacji zależą wyłącznie od kursora myszy
    public void UpdateFacing(Vector2 worldMousePos)
    {
        Vector2 aimDir = worldMousePos - Position;

        // Określ kierunek i wiersz na podstawie wektora celowania
        if (Math.Abs(aimDir.X) > Math.Abs(aimDir.Y))
        {
            // Dominuje oś X — ruch poziomy
            if (aimDir.X >= 0)
            {
                // Prawo
                Direction = PlayerDirection.Right;
                SpriteEffects = SpriteEffects.FlipHorizontally;
                _idleAnimation.Row = 0;
                _walkAnimation.Row = 0;
            }
            else
            {
                // Lewo
                Direction = PlayerDirection.Left;
                SpriteEffects = SpriteEffects.None;
                _idleAnimation.Row = 0;
                _walkAnimation.Row = 0;
            }
        }
        else
        {
            // Dominuje oś Y — ruch pionowy
            if (aimDir.Y > 0)
            {
                // Dół
                Direction = PlayerDirection.Down;
                SpriteEffects = SpriteEffects.None;
                _idleAnimation.Row = 3;
                _walkAnimation.Row = 3;
            }
            else
            {
                // Góra
                Direction = PlayerDirection.Up;
                SpriteEffects = SpriteEffects.None;
                _idleAnimation.Row = 2;
                _walkAnimation.Row = 2;
            }
        }

        // PlayBackwards — jeśli gracz ucieka w przeciwną stronę niż celuje
        if (State == PlayerState.Walk)
        {
            // Oblicz aktualny wektor ruchu na podstawie kierunku celowania
            // Używamy aimDir jako kierunku "przodu" i sprawdzamy iloczyn skalarny
            // z wektorem ruchu — ale ruch jest sterowany przez WSAD, więc potrzebujemy
            // go przekazać. Zamiast tego: sprawdzamy czy poruszamy się w kierunku
            // przeciwnym do celowania.
            // Ponieważ nie mamy tu moveDir, ustawiamy PlayBackwards na false domyślnie.
            // Wartość ustawiana jest w Game1.Update.cs gdzie mamy oba wektory.
        }
    }

    // Ustawia PlayBackwards na animacji walki
    public void SetWalkBackwards(bool backwards)
    {
        _walkAnimation.PlayBackwards = backwards;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_currentAnimation == null) return;

        // Centrowanie sprite'a względem środka aktualnej klatki
        Vector2 origin = new Vector2(_currentAnimation.FrameWidth / 2f, _currentAnimation.FrameHeight / 2f);
        Rectangle sourceRect = _currentAnimation.SourceRectangle;

        spriteBatch.Draw(
            _currentAnimation.Texture,
            Position,
            sourceRect,
            new Color(190, 190, 200),
            0f,
            origin,
            DrawScale,
            SpriteEffects,
            0f
        );
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
