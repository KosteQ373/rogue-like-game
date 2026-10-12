using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace rogue_like;

public partial class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;

    private GameState _currentState = GameState.MainMenu;
    private Player _player;
    private Camera _camera;

    private ObjectPool<Bullet> _bulletPool;
    private ObjectPool<BasicEnemy> _basicEnemyPool;
    private ObjectPool<TankEnemy> _tankEnemyPool;
    private ObjectPool<FastEnemy> _fastEnemyPool;
    private ObjectPool<ColossusBoss> _colossusBossPool;
    private ObjectPool<XpGem> _xpPool;
    private ObjectPool<DamageNumber> _damageNumberPool;
    private ObjectPool<DeathParticle> _deathParticlePool;
    private ObjectPool<DeathRing> _deathRingPool;
    private ObjectPool<ExplosionParticle> _explosionParticlePool;
    private ObjectPool<ExplosionRing> _explosionRingPool;
    
    private SpatialGrid _spatialGrid;
    private readonly List<Enemy> _nearbyEnemiesBuffer = new(100);

    private Boss _activeBoss;
    private float _bossTimer = 0f;
    private const float BossInterval = 120f;
    private float _bossBannerTimer = 0f;
    private const float BossBannerDuration = 2.0f;
    private float _spawnTempoTime = 0f;
    private float _hpMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _damageMultiplier = 1f;
    private int _bossesDefeatedCount = 0;
    private GameState _stateBeforeLevelUp = GameState.Playing;
    // [TESTOWE NARZĘDZIE] Przechowuje poprzedni stan klawiatury do wykrywania naciśnięcia "L".
    private KeyboardState _lastKState;

    private float _spawnTimer;
    private float _spawnInterval = 2.5f;
    private float _survivalTime = 0f;
    private Random _rand;

    private SpriteFont _font;
    private readonly List<UpgradeCard> _currentChoices = new();
    private readonly HashSet<string> _acquiredUpgrades = new();
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;
    private bool _soundEnabled = true;
    private bool _isEndlessMode = false;

    private const float ArenaWidth = 2000f;
    private const float ArenaHeight = 2000f;
    
    private UpgradeTags _mutuallyExclusiveTagsCache = UpgradeTags.None;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.IsFullScreen = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        _rand = new Random();
        _player = new Player(new Vector2(1000, 1000)); // Start in the center of 2000x2000 arena
        _camera = new Camera(_player.Position);

        _bulletPool = new ObjectPool<Bullet>(100);
        _basicEnemyPool = new ObjectPool<BasicEnemy>(150);
        _tankEnemyPool = new ObjectPool<TankEnemy>(50);
        _fastEnemyPool = new ObjectPool<FastEnemy>(100);
        _colossusBossPool = new ObjectPool<ColossusBoss>(1);
        _xpPool = new ObjectPool<XpGem>(300);
        _damageNumberPool = new ObjectPool<DamageNumber>(200);
        _deathParticlePool = new ObjectPool<DeathParticle>(500);
        _deathRingPool = new ObjectPool<DeathRing>(50);
        _explosionParticlePool = new ObjectPool<ExplosionParticle>(300);
        _explosionRingPool = new ObjectPool<ExplosionRing>(50);
        _spatialGrid = new SpatialGrid((int)ArenaWidth, (int)ArenaHeight, 200);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Jeśli tego nie załaduje, gra natychmiast wywali czerwony błąd i powiadomi dlaczego
        _font = Content.Load<SpriteFont>("Font"); 
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        switch (_currentState)
        {
            case GameState.MainMenu:
                UpdateMainMenu();
                break;
            case GameState.CharacterSelect:
                UpdateCharacterSelect();
                break;
            case GameState.Settings:
                UpdateSettings();
                break;
            case GameState.Playing:
                UpdatePlaying(dt);
                break;
            case GameState.BossFight:
                UpdateBossFight(dt);
                break;
            case GameState.LevelUp:
                UpdateLevelUp();
                break;
            case GameState.GameOver:
                UpdateGameOver();
                break;
            case GameState.YouWin:
                UpdateYouWin();
                break;
        }

        _previousMouseState = Mouse.GetState();
        _previousKeyboardState = Keyboard.GetState();
        base.Update(gameTime);
    }

    public void ToggleFullscreen()
    {
        _graphics.IsFullScreen = !_graphics.IsFullScreen;
        _graphics.ApplyChanges();
    }

    public void SpawnDamageNumber(Vector2 pos, float damage, bool isCrit, Color? color = null)
    {
        var num = _damageNumberPool.Get();
        float offsetX = ((float)_rand.NextDouble() - 0.5f) * 20f;
        float offsetY = ((float)_rand.NextDouble() - 0.5f) * 12f;

        // Stack vertically if close to another active damage number to avoid overlapping on rapid attacks
        float stackOffsetY = 0f;
        var items = _damageNumberPool.Items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].IsActive && Vector2.DistanceSquared(items[i].Position, pos) < 800f)
            {
                stackOffsetY -= 16f;
            }
        }

        num.Position = pos + new Vector2(offsetX, offsetY + stackOffsetY);
        float driftX = ((float)_rand.NextDouble() - 0.5f) * 36f;
        float speedY = isCrit ? -85f : -65f;
        num.Velocity = new Vector2(driftX, speedY);
        num.DamageValue = damage;
        num.IsCrit = isCrit;
        num.Lifetime = 0f;
        num.MaxLifetime = isCrit ? 0.75f : 0.6f;
        num.Delay = (float)_rand.NextDouble() * 0.09f; // Slight stagger delay for rapid attacks

        if (color.HasValue)
        {
            num.HasCustomColor = true;
            num.CustomColor = color.Value;
        }
        else
        {
            num.HasCustomColor = false;
        }
    }

    // Tworzy efekt wybuchu pocisku: pierścień + cząsteczki rozprzestrzeniające się na zewnątrz
    public void SpawnExplosionEffect(Vector2 position)
    {
        // Pierścień wybuchu
        var ring = _explosionRingPool.Get();
        ring.Position = position;
        ring.Radius = 5f;
        ring.MaxRadius = 80f;
        ring.Color = new Color(255, 200, 50);

        // Cząsteczki wybuchu — rozchodzące się promieniście
        int particleCount = 24;
        for (int i = 0; i < particleCount; i++)
        {
            var particle = _explosionParticlePool.Get();
            particle.Position = position;
            float angle = i * (MathHelper.TwoPi / particleCount) + (float)_rand.NextDouble() * 0.3f;
            float speed = 80f + (float)_rand.NextDouble() * 120f;
            particle.Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);

            // Kolory od jasnożółtego przez pomarańczowy do czerwonego
            float brightness = 0.6f + (float)_rand.NextDouble() * 0.5f;
            int colorRoll = _rand.Next(3);
            particle.Color = colorRoll switch
            {
                0 => new Color(255, 230, 80) * brightness,
                1 => new Color(255, 140, 30) * brightness,
                _ => new Color(255, 60, 20) * brightness
            };
            particle.Size = 3 + (int)((float)_rand.NextDouble() * 3);
            particle.MaxLifetime = 0.2f + (float)_rand.NextDouble() * 0.2f;
        }
    }

    // Tworzy efekt śmierci: okrągły błysk + rozpad na piksele
    public void SpawnDeathEffect(Vector2 position, float enemySize, Color enemyColor)
    {
        // Okrągły błysk
        var ring = _deathRingPool.Get();
        ring.Position = position;
        ring.Radius = enemySize * 0.5f;
        ring.MaxRadius = enemySize * 1.5f;
        ring.Color = enemyColor;

        // Rozpad na piksele – siatka wypełniająca kostkę wroga
        int gridSize = Math.Max(3, (int)(enemySize / 4f));
        float cellSize = enemySize / gridSize;
        float half = enemySize * 0.5f;

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var particle = _deathParticlePool.Get();
                particle.Position = new Vector2(
                    position.X - half + cellSize * (x + 0.5f),
                    position.Y - half + cellSize * (y + 0.5f)
                );

                // Losowy kierunek rozprzestrzenienia
                float angle = (float)_rand.NextDouble() * MathHelper.TwoPi;
                float speed = 60f + (float)_rand.NextDouble() * 120f;
                particle.Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);

                // Kolor z lekkim wariowaniem
                float brightness = 0.7f + (float)_rand.NextDouble() * 0.6f;
                particle.Color = enemyColor * brightness;
                particle.Size = Math.Max(2, (int)(cellSize * 0.6f));
                particle.MaxLifetime = 0.4f + (float)_rand.NextDouble() * 0.3f;
            }
        }
    }

    private bool IsUpgradeBlocked(UpgradeCard card)
    {
        if (card == null) return false;

        UpgradeTags currentPlayerTags = UpgradeTags.None;
        if (_player.HasBoomerang) currentPlayerTags |= UpgradeTags.Boomerang;
        if (_player.HasInfinityRange) currentPlayerTags |= UpgradeTags.InfinityRange;
        if (_player.HasFireBullets) currentPlayerTags |= UpgradeTags.Incendiary;
        if (_player.HasFreezeBullets) currentPlayerTags |= UpgradeTags.Frostbite;
        if (_player.HasExplosiveBullets) currentPlayerTags |= UpgradeTags.Explosive;

        // Jeśli karta, którą sprawdzamy jest wykluczona przez tagi gracza
        if ((card.Tags & _mutuallyExclusiveTagsCache) != 0 || (card.MutuallyExclusiveWith & currentPlayerTags) != 0)
        {
            return true;
        }

        return false;
    }
}
