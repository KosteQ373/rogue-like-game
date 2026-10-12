using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rogue_like;

// Stany maszyny stanów wroga
public enum EnemyState
{
    Walk,
    Attack,
    Hurt,
    Dead
}

public abstract class Enemy : IPoolable
{
    public bool IsActive { get; set; }
    public Vector2 Position;
    public float Speed;
    public float Health;
    public float MaxHealth;
    public float Damage;
    public float AttackCooldown = 0f;
    public float KnockbackTimer = 0f;
    public float Size = 24f;
    public int XpDropCount = 1;
    public Vector2 Velocity;

    // Maszyna stanów wroga
    public EnemyState State { get; set; }

    // Zasięg ataku melee — wróg musi być blisko gracza, żeby zaatakować mieczem
    public float AttackRange => Size * 0.6f;

    // Cooldown po odegraniu animacji ataku przed kolejnym atakiem
    public float AttackPostCooldown = 0f;

    // Klatka zamachu w animacji ataku (indeks klatki, na której zadajemy obrażenia)
    public int AttackDamageFrame = 4;

    // Czy obrażenia z ataku zostały już zadane w bieżącej animacji
    public bool AttackDamageDealt { get; set; }

    // Czy w tym ataku już zadano obrażenia — zapobiega wielokrotnemu zadawaniu
    private bool _hasDealtDamageThisAttack;

    // Czy w tym cyklu ataku obrażenia zostały już zużyte przez CollisionSystem
    // Blokuje ponowne ustawienie _hasDealtDamageThisAttack w kolejnych klatkach gry
    private bool _damageConsumedThisAttack;

    // Publiczna flaga — czy gracz został trafiony mieczem z przodu w tym ataku
    public bool HitPlayerThisAttack => _hasDealtDamageThisAttack;

    // Zużywa flagę trafienia — wywoływane po zadaniu obrażeń graczowi
    // Ustawia _damageConsumedThisAttack aby zablokować ponowne zadanie obrażeń w tym ataku
    public void ConsumeHit()
    {
        _hasDealtDamageThisAttack = false;
        _damageConsumedThisAttack = true;
    }

    // Obrażenia zadawane ciosem miecza
    public float AttackDamage => Damage;

    // Animacje sprite sheet
    public SpriteAnimation? WalkAnimation { get; set; }
    public SpriteAnimation? HurtAnimation { get; set; }
    public SpriteAnimation? AttackAnimation { get; set; }
    public SpriteAnimation? DeathAnimation { get; set; }
    public SpriteEffects CurrentSpriteEffects { get; protected set; } = SpriteEffects.None;

    // Statyczne tekstury — ustawiane raz z Game1
    protected static Texture2D? _basicWalkTexture;
    protected static Texture2D? _basicHurtTexture;
    protected static Texture2D? _basicAttackTexture;
    protected static Texture2D? _basicDeathTexture;
    protected static Texture2D? _fastWalkTexture;
    protected static Texture2D? _fastHurtTexture;
    protected static Texture2D? _fastAttackTexture;
    protected static Texture2D? _fastDeathTexture;
    protected static Texture2D? _tankWalkTexture;
    protected static Texture2D? _tankHurtTexture;
    protected static Texture2D? _tankAttackTexture;
    protected static Texture2D? _tankDeathTexture;

    public static void SetTextures(
        Texture2D basicWalk, Texture2D basicHurt, Texture2D basicAttack, Texture2D basicDeath,
        Texture2D fastWalk, Texture2D fastHurt, Texture2D fastAttack, Texture2D fastDeath,
        Texture2D tankWalk, Texture2D tankHurt, Texture2D tankAttack, Texture2D tankDeath)
    {
        _basicWalkTexture = basicWalk;
        _basicHurtTexture = basicHurt;
        _basicAttackTexture = basicAttack;
        _basicDeathTexture = basicDeath;
        _fastWalkTexture = fastWalk;
        _fastHurtTexture = fastHurt;
        _fastAttackTexture = fastAttack;
        _fastDeathTexture = fastDeath;
        _tankWalkTexture = tankWalk;
        _tankHurtTexture = tankHurt;
        _tankAttackTexture = tankAttack;
        _tankDeathTexture = tankDeath;
    }

    public float BurnTimer = 0f;
    public float BurnTickTimer = 0f;
    public float BurnDamagePerTick = 8f;
    public float FreezeTimer = 0f;

    public bool IsBurning => BurnTimer > 0f;
    public bool IsFrozen => FreezeTimer > 0f;

    public void ApplyBurn(float duration = 4f, float damagePerTick = 8f)
    {
        BurnTimer = duration;
        BurnDamagePerTick = damagePerTick;
    }

    public void ApplyFreeze(float duration = 3f)
    {
        FreezeTimer = duration;
    }

    public virtual void Reset()
    {
        Position = Vector2.Zero;
        State = EnemyState.Walk;
        AttackCooldown = 0f;
        AttackPostCooldown = 0f;
        KnockbackTimer = 0f;
        BurnTimer = 0f;
        BurnTickTimer = 0f;
        FreezeTimer = 0f;
        Velocity = Vector2.Zero;
        AttackDamageDealt = false;
        _hasDealtDamageThisAttack = false;
        CurrentSpriteEffects = SpriteEffects.None;
        WalkAnimation?.Reset();
        HurtAnimation?.Reset();
        AttackAnimation?.Reset();
        DeathAnimation?.Reset();
    }

    public abstract void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f);

    // Ustawia kierunek sprite na podstawie wektora do gracza
    private void SetDirection(Vector2 toPlayer)
    {
        if (Math.Abs(toPlayer.X) > Math.Abs(toPlayer.Y))
        {
            if (toPlayer.X > 0)
            {
                WalkAnimation.Row = 2;
                HurtAnimation.Row = 2;
                AttackAnimation.Row = 2;
                DeathAnimation.Row = 2;
                CurrentSpriteEffects = SpriteEffects.FlipHorizontally;
            }
            else
            {
                WalkAnimation.Row = 2;
                HurtAnimation.Row = 2;
                AttackAnimation.Row = 2;
                DeathAnimation.Row = 2;
                CurrentSpriteEffects = SpriteEffects.None;
            }
        }
        else
        {
            if (toPlayer.Y > 0)
            {
                WalkAnimation.Row = 0;
                HurtAnimation.Row = 0;
                AttackAnimation.Row = 0;
                DeathAnimation.Row = 0;
                CurrentSpriteEffects = SpriteEffects.None;
            }
            else
            {
                WalkAnimation.Row = 1;
                HurtAnimation.Row = 1;
                AttackAnimation.Row = 1;
                DeathAnimation.Row = 1;
                CurrentSpriteEffects = SpriteEffects.None;
            }
        }
    }

    // Aktualizuje maszynę stanów i animację wroga
    public void UpdateAnimations(GameTime gameTime, Vector2 playerPos)
    {
        if (WalkAnimation == null || HurtAnimation == null)
            return;

        Vector2 toPlayer = playerPos - Position;

        // Stan Dead — odtwarzaj animację śmierci
        if (State == EnemyState.Dead)
        {
            if (DeathAnimation != null)
            {
                SetDirection(toPlayer);
                DeathAnimation.Update(gameTime);
                // Po zakończeniu animacji śmierci — zwróć do puli
                if (DeathAnimation.IsFinished)
                {
                    IsActive = false;
                }
            }
            return;
        }

        // Stan Attack — odtwarzaj animację ataku i zadawanie obrażeń
        if (State == EnemyState.Attack)
        {
            if (AttackAnimation != null)
            {
                SetDirection(toPlayer);
                AttackAnimation.Update(gameTime);

                // Oznacz klatkę zamachu
                if (!AttackDamageDealt && AttackAnimation.CurrentFrame >= AttackDamageFrame)
                {
                    AttackDamageDealt = true;
                }

                // Okno trafienia — aktywne klatki miecza (3-6), nie zadajemy obrażeń więcej niż raz
                // _damageConsumedThisAttack blokuje ponowne ustawienie po zużyciu przez CollisionSystem
                if (AttackAnimation.CurrentFrame >= 3 && AttackAnimation.CurrentFrame <= 6 && !_hasDealtDamageThisAttack && !_damageConsumedThisAttack)
                {
                    float swordReach = 80f;
                    float currentDist = Vector2.Distance(Position, playerPos);

                    if (currentDist <= swordReach)
                    {
                        // Gracz w zasięgu miecza — ustaw flagę do zadania obrażeń
                        _hasDealtDamageThisAttack = true;
                    }
                }

                // Po zakończeniu animacji ataku — wróć do Walk z cooldownem
                if (AttackAnimation.IsFinished)
                {
                    State = EnemyState.Walk;
                    AttackPostCooldown = 1.0f;
                    AttackDamageDealt = false;
                    _hasDealtDamageThisAttack = false;
                    _damageConsumedThisAttack = false;
                    // Zresetuj animację chodu, aby była gotowa na kolejny ruch
                    WalkAnimation?.Reset();
                }
            }
            return;
        }

        // Stan Hurt (knockback) — odtwarzaj animację hurt
        if (KnockbackTimer > 0f)
        {
            SetDirection(toPlayer);
            HurtAnimation.Update(gameTime);
            return;
        }

        // Stan Walk — odtwarzaj animację chodu
        SetDirection(toPlayer);
        WalkAnimation.Update(gameTime);
    }

    public virtual void Update(float dt, Vector2 playerPos, ObjectPool<XpGem>? xpPool = null, Action<Vector2, float, bool, Color?>? onDamageNumber = null, Action<Vector2, float, Color>? onDeath = null, float speedMultiplier = 1f, List<Enemy>? neighbors = null)
    {
        // Stan Dead — tylko aktualizuj animację, nie rób nic więcej
        if (State == EnemyState.Dead)
        {
            return;
        }

        if (AttackCooldown > 0f)
        {
            AttackCooldown -= dt;
        }

        if (AttackPostCooldown > 0f)
        {
            AttackPostCooldown -= dt;
        }

        if (KnockbackTimer > 0f)
        {
            KnockbackTimer -= dt;
        }

        if (FreezeTimer > 0f)
        {
            FreezeTimer -= dt;
        }

        if (BurnTimer > 0f)
        {
            BurnTimer -= dt;
            BurnTickTimer += dt;
            if (BurnTickTimer >= 1.0f)
            {
                BurnTickTimer -= 1.0f;
                Health -= BurnDamagePerTick;
                onDamageNumber?.Invoke(Position + new Vector2(0f, -Size * 0.4f), BurnDamagePerTick, false, new Color(255, 140, 30));

                if (Health <= 0f)
                {
                    // Przejście w stan śmierci
                    TransitionToDeath(xpPool, onDeath);
                    return;
                }
            }
        }

        // Stan Attack — przeciwnik nie porusza się
        // Zadawanie obrażeń jest obsługiwane w UpdateAnimations(), gdzie animacja jest już zaktualizowana
        if (State == EnemyState.Attack)
        {
            // Zatrzymaj ruch podczas ataku
            Velocity = Vector2.Lerp(Velocity, Vector2.Zero, dt * 8f);
            return;
        }

        // Sprawdź czy przeciwnik powinien zaatakować
        if (State == EnemyState.Walk && KnockbackTimer <= 0f)
        {
            float attackTriggerRange = 75f;
            float distSq = Vector2.DistanceSquared(Position, playerPos);
            if (distSq <= attackTriggerRange * attackTriggerRange && AttackCooldown <= 0f && AttackPostCooldown <= 0f)
            {
                // Przełącz na stan ataku — obróć orka twarzą w stronę gracza
                State = EnemyState.Attack;
                AttackDamageDealt = false;
                _hasDealtDamageThisAttack = false;
                _damageConsumedThisAttack = false;
                if (AttackAnimation != null)
                {
                    AttackAnimation.Reset();
                    // Ustaw kierunek animacji ataku w stronę gracza
                    SetDirection(playerPos - Position);
                }
                return;
            }
        }

        // Ruch w stronę gracza (tylko gdy nie ma knockbacku)
        if (KnockbackTimer <= 0f && State == EnemyState.Walk)
        {
            Vector2 toPlayer = playerPos - Position;
            // Zabezpieczenie przed dzieleniem przez zero — użyj LengthSquared()
            Vector2 dirToPlayer = toPlayer.LengthSquared() > 0.0001f ? Vector2.Normalize(toPlayer) : Vector2.Zero;

            // Separacja — realna siła odpychania, promień dopasowany do nowego rozmiaru wrogów
            Vector2 separation = Vector2.Zero;
            float separationDistance = 100f;

            if (neighbors != null)
            {
                for (int i = 0; i < neighbors.Count; i++)
                {
                    var other = neighbors[i];
                    if (!other.IsActive || ReferenceEquals(this, other) || other.State == EnemyState.Dead) continue;

                    Vector2 diff = Position - other.Position;
                    float distSq = diff.LengthSquared();

                    if (distSq > 0.001f && distSq < separationDistance * separationDistance)
                    {
                        float dist = MathF.Sqrt(distSq);
                        // Siła rośnie gwałtownie, gdy wrogowie są bardzo blisko: (strength * strength)
                        float strength = 1f - (dist / separationDistance);
                        separation += (diff / dist) * (strength * strength);
                    }
                    else if (distSq <= 0.001f)
                    {
                        // Zabezpieczenie przed idealnym nałożeniem (zerowy dystans)
                        separation += new Vector2(
                            (float)System.Random.Shared.NextDouble() - 0.5f,
                            (float)System.Random.Shared.NextDouble() - 0.5f
                        );
                    }
                }
            }

            // Separacja musi mieć realną wagę (1.1f vs 0.8f), aby mogła zepchnąć orka na bok i ominąć tłok
            Vector2 moveDir = dirToPlayer * 0.8f + separation * 1.1f;
            if (moveDir.LengthSquared() > 0.001f)
            {
                moveDir.Normalize();
            }

            float currentSpeed = Speed * speedMultiplier;
            if (FreezeTimer > 0f) currentSpeed *= 0.5f;

            Position += moveDir * currentSpeed * dt;
            Velocity = moveDir * currentSpeed;
        }
        else
        {
            Velocity = Vector2.Lerp(Velocity, Vector2.Zero, dt * 8f);
        }

        // Clamp to 2000x2000 arena bounds
        Position.X = MathHelper.Clamp(Position.X, 20f, 1980f);
        Position.Y = MathHelper.Clamp(Position.Y, 20f, 1980f);
    }

    // Przejście w stan śmierci — wyrzuca XP i uruchamia animację
    private void TransitionToDeath(ObjectPool<XpGem>? xpPool, Action<Vector2, float, Color>? onDeath)
    {
        State = EnemyState.Dead;
        if (DeathAnimation != null)
        {
            DeathAnimation.Reset();
        }
        // Wyrzuć klejnoty XP od razu
        if (xpPool != null)
        {
            for (int g = 0; g < XpDropCount; g++)
            {
                var xp = xpPool.Get();
                float angle = g * (MathHelper.TwoPi / XpDropCount);
                float dropRadius = 35f + (g % 3) * 15f;
                Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dropRadius;
                xp.Position = Position + offset;
                xp.Value = 1;
            }
        }
        onDeath?.Invoke(Position, Size, new Color(255, 200, 50));
    }
}

public class BasicEnemy : Enemy
{
    public override void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        Speed = (120f + playerLevel * 2f) * speedMultiplier;
        Health = 30f * hpMultiplier;
        MaxHealth = Health;
        Damage = 5f * damageMultiplier;
        Size = 65f;
        XpDropCount = Math.Max(1, (int)(1f * hpMultiplier));

        // Inicjalizacja animacji z tekstur orc1 (6/8 kolumn x 4 wiersze, klatka 64x64 px)
        if (_basicWalkTexture != null && _basicHurtTexture != null && _basicAttackTexture != null && _basicDeathTexture != null)
        {
            int frameWidth = _basicWalkTexture.Width / 6;
            int frameHeight = _basicWalkTexture.Height / 4;
            WalkAnimation = new SpriteAnimation(_basicWalkTexture, frameWidth, frameHeight, 0, 6);
            HurtAnimation = new SpriteAnimation(_basicHurtTexture, frameWidth, frameHeight, 0, 6);

            int attackFrameWidth = _basicAttackTexture.Width / 8;
            int attackFrameHeight = _basicAttackTexture.Height / 4;
            AttackAnimation = new SpriteAnimation(_basicAttackTexture, attackFrameWidth, attackFrameHeight, 0, 8, 0.08f, false);
            DeathAnimation = new SpriteAnimation(_basicDeathTexture, attackFrameWidth, attackFrameHeight, 0, 8, 0.08f, false);
        }
    }
}

public class TankEnemy : Enemy
{
    public override void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        // 40% slower than basic
        Speed = (120f + playerLevel * 2f) * 0.6f * speedMultiplier;
        // 5x health
        Health = 150f * hpMultiplier;
        MaxHealth = Health;
        // 2x damage
        Damage = 10f * damageMultiplier;
        // Większy hitbox dla tanka
        Size = 80f;
        // 3 xp gems
        XpDropCount = Math.Max(1, (int)(3f * hpMultiplier));

        // Inicjalizacja animacji z tekstur orc3 (6/8 kolumn x 4 wiersze, klatka 64x64 px)
        if (_tankWalkTexture != null && _tankHurtTexture != null && _tankAttackTexture != null && _tankDeathTexture != null)
        {
            int frameWidth = _tankWalkTexture.Width / 6;
            int frameHeight = _tankWalkTexture.Height / 4;
            WalkAnimation = new SpriteAnimation(_tankWalkTexture, frameWidth, frameHeight, 0, 6);
            HurtAnimation = new SpriteAnimation(_tankHurtTexture, frameWidth, frameHeight, 0, 6);

            int attackFrameWidth = _tankAttackTexture.Width / 8;
            int attackFrameHeight = _tankAttackTexture.Height / 4;
            AttackAnimation = new SpriteAnimation(_tankAttackTexture, attackFrameWidth, attackFrameHeight, 0, 8, 0.08f, false);
            DeathAnimation = new SpriteAnimation(_tankDeathTexture, attackFrameWidth, attackFrameHeight, 0, 8, 0.08f, false);
        }
    }
}

public class FastEnemy : Enemy
{
    public override void Init(int playerLevel, float hpMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
    {
        // 50% faster than basic
        Speed = (120f + playerLevel * 2f) * 1.5f * speedMultiplier;
        // half health
        Health = 15f * hpMultiplier;
        MaxHealth = Health;
        Damage = 5f * damageMultiplier;
        Size = 55f;
        XpDropCount = Math.Max(1, (int)(1f * hpMultiplier));

        // Inicjalizacja animacji z tekstur orc2 (6/8 kolumn x 4 wiersze, klatka 64x64 px)
        if (_fastWalkTexture != null && _fastHurtTexture != null && _fastAttackTexture != null && _fastDeathTexture != null)
        {
            int frameWidth = _fastWalkTexture.Width / 6;
            int frameHeight = _fastWalkTexture.Height / 4;
            WalkAnimation = new SpriteAnimation(_fastWalkTexture, frameWidth, frameHeight, 0, 6);
            HurtAnimation = new SpriteAnimation(_fastHurtTexture, frameWidth, frameHeight, 0, 6);

            int attackFrameWidth = _fastAttackTexture.Width / 8;
            int attackFrameHeight = _fastAttackTexture.Height / 4;
            AttackAnimation = new SpriteAnimation(_fastAttackTexture, attackFrameWidth, attackFrameHeight, 0, 8, 0.08f, false);
            DeathAnimation = new SpriteAnimation(_fastDeathTexture, attackFrameWidth, attackFrameHeight, 0, 8, 0.08f, false);
        }
    }
}
