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

    private GameState _currentState = GameState.Playing;
    private Player _player;
    private Camera _camera;

    private ObjectPool<Bullet> _bulletPool;
    private ObjectPool<BasicEnemy> _basicEnemyPool;
    private ObjectPool<TankEnemy> _tankEnemyPool;
    private ObjectPool<FastEnemy> _fastEnemyPool;
    private ObjectPool<XpGem> _xpPool;

    private float _spawnTimer;
    private float _spawnInterval = 2.5f;
    private float _survivalTime = 0f;
    private Random _rand;

    private SpriteFont _font;
    private readonly List<UpgradeCard> _currentChoices = new();
    private readonly HashSet<string> _acquiredUpgrades = new();
    private MouseState _previousMouseState;

    private const float ArenaWidth = 2000f;
    private const float ArenaHeight = 2000f;

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
        _xpPool = new ObjectPool<XpGem>(300);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            _font = Content.Load<SpriteFont>("Font");
        }
        catch
        {
            _font = null!;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        switch (_currentState)
        {
            case GameState.Playing:
                UpdatePlaying(dt);
                break;
            case GameState.LevelUp:
                UpdateLevelUp();
                break;
            case GameState.GameOver:
                UpdateGameOver();
                break;
        }

        _previousMouseState = Mouse.GetState();
        base.Update(gameTime);
    }
}
