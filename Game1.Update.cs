using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace rogue_like;

public partial class Game1
{
    private void UpdatePlayerAndCamera(float dt)
    {
        _player.Update(dt);

        // Movement input
        var kState = Keyboard.GetState();
        Vector2 moveDir = Vector2.Zero;
        if (kState.IsKeyDown(Keys.W) || kState.IsKeyDown(Keys.Up)) moveDir.Y -= 1f;
        if (kState.IsKeyDown(Keys.S) || kState.IsKeyDown(Keys.Down)) moveDir.Y += 1f;
        if (kState.IsKeyDown(Keys.A) || kState.IsKeyDown(Keys.Left)) moveDir.X -= 1f;
        if (kState.IsKeyDown(Keys.D) || kState.IsKeyDown(Keys.Right)) moveDir.X += 1f;

        if (moveDir.LengthSquared() > 0f)
        {
            moveDir.Normalize();
            _player.Position += moveDir * _player.MoveSpeed.Value * dt;
        }

        // Clamp player to 2000x2000 arena
        _player.Position.X = MathHelper.Clamp(_player.Position.X, 20f, ArenaWidth - 20f);
        _player.Position.Y = MathHelper.Clamp(_player.Position.Y, 20f, ArenaHeight - 20f);

        // Dynamic screen center calculations
        float halfW = GraphicsDevice.Viewport.Width / 2f;
        float halfH = GraphicsDevice.Viewport.Height / 2f;

        _camera.Update(_player.Position, halfW, halfH, ArenaWidth, ArenaHeight);
    }

    private void UpdatePlayerShooting(float dt)
    {
        float halfW = GraphicsDevice.Viewport.Width / 2f;
        float halfH = GraphicsDevice.Viewport.Height / 2f;

        _player.ShootTimer += dt * _player.AttackSpeed.Value;
        var mouseState = Mouse.GetState();
        if (mouseState.LeftButton == ButtonState.Pressed && _player.ShootTimer >= _player.ShootInterval)
        {
            _player.ShootTimer = 0f;

            // Convert screen mouse position to world position based on camera dead zone position
            Vector2 screenMousePos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 cameraOffset = _camera.GetOffset(halfW, halfH);
            Vector2 worldMousePos = screenMousePos - cameraOffset;

            Vector2 shootDir = worldMousePos - _player.Position;
            if (shootDir.LengthSquared() > 0.001f)
            {
                shootDir.Normalize();
            }
            else
            {
                shootDir = new Vector2(1f, 0f);
            }

            int count = _player.ProjectilesCount;
            float spreadAngle = 0.2f;
            float baseAngle = (float)Math.Atan2(shootDir.Y, shootDir.X);

            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle;
                if (count > 1)
                {
                    angle = baseAngle + (i - (count - 1) / 2f) * spreadAngle;
                }

                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

                var bullet = _bulletPool.Get();
                bullet.Position = _player.Position;
                bullet.Velocity = dir * 600f;

                float finalDamage = _player.AttackDamage.Value;
                bool isCrit = (float)_rand.NextDouble() < _player.CriticalChance.Value;
                if (isCrit)
                {
                    finalDamage *= _player.CriticalDamageMultiplier.Value;
                }

                bullet.Damage = finalDamage;
                bullet.PierceCount = (int)_player.Pierce.Value;

                float range = _player.Range.Value;
                bullet.MaxDistance = range;
                bullet.MaxLifetime = 10f;

                if (_player.HasBoomerang)
                {
                    bullet.MaxDistance = range * 2.5f;
                    float outDistance = range * 0.5f;
                    float returnDelay = outDistance / 600f;
                    bullet.TrajectoryModifiers.Add(new ReturningTrajectoryModifier(returnDelay));
                }
            }
        }
    }

    private void UpdateBullets(float dt)
    {
        var bullets = _bulletPool.Items;
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i].IsActive)
            {
                bullets[i].Update(dt, _player.Position);
            }
        }
    }

    private void UpdatePlaying(float dt)
    {
        UpdatePlayerAndCamera(dt);
        UpdatePlayerShooting(dt);

        // Spawn enemies outside player's field of view
        _survivalTime += dt;
        _spawnTempoTime += dt;
        int intervalsPassed = (int)(_spawnTempoTime / 15f);
        _spawnInterval = Math.Max(0.3f, 2.5f - intervalsPassed * 0.15f);

        _spawnTimer += dt;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;

            double roll = _rand.NextDouble();
            Enemy enemy;
            if (roll < 0.6)
            {
                enemy = _basicEnemyPool.Get();
            }
            else if (roll < 0.8)
            {
                enemy = _fastEnemyPool.Get();
            }
            else
            {
                enemy = _tankEnemyPool.Get();
            }

            enemy.Init(_player.Level, _enemyHealthMultiplier);

            float angle = (float)(_rand.NextDouble() * Math.PI * 2);
            float dist = 750f + (float)(_rand.NextDouble() * 200f); 
            Vector2 spawnPos = _camera.Position + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dist;
            
            // Clamp spawn inside arena
            spawnPos.X = MathHelper.Clamp(spawnPos.X, 20f, ArenaWidth - 20f);
            spawnPos.Y = MathHelper.Clamp(spawnPos.Y, 20f, ArenaHeight - 20f);
            enemy.Position = spawnPos;
        }

        UpdateBullets(dt);

        // Update Enemies
        var basics = _basicEnemyPool.Items;
        for (int i = 0; i < basics.Count; i++)
        {
            if (basics[i].IsActive) basics[i].Update(dt, _player.Position);
        }

        var tanks = _tankEnemyPool.Items;
        for (int i = 0; i < tanks.Count; i++)
        {
            if (tanks[i].IsActive) tanks[i].Update(dt, _player.Position);
        }

        var fasts = _fastEnemyPool.Items;
        for (int i = 0; i < fasts.Count; i++)
        {
            if (fasts[i].IsActive) fasts[i].Update(dt, _player.Position);
        }

        // Resolve Collisions
        CollisionSystem.ResolveCollisions(_player, _bulletPool, _basicEnemyPool, _tankEnemyPool, _fastEnemyPool, _xpPool, dt, out bool leveledUp);
        if (leveledUp)
        {
            TriggerLevelUp();
        }

        if (_player.CurrentHealth <= 0f)
        {
            _currentState = GameState.GameOver;
            return;
        }

        // Check 120s boss fight interval
        _bossTimer += dt;
        if (_bossTimer >= BossInterval)
        {
            _bossTimer = 0f;
            StartBossFight();
        }
    }

    private void StartBossFight()
    {
        // Despawn all active regular enemies
        var basics = _basicEnemyPool.Items;
        for (int i = 0; i < basics.Count; i++) basics[i].IsActive = false;

        var tanks = _tankEnemyPool.Items;
        for (int i = 0; i < tanks.Count; i++) tanks[i].IsActive = false;

        var fasts = _fastEnemyPool.Items;
        for (int i = 0; i < fasts.Count; i++) fasts[i].IsActive = false;

        // Randomly spawn one of the available bosses
        _activeBoss = SpawnRandomBoss();

        _currentState = GameState.BossFight;
    }

    private Boss SpawnRandomBoss()
    {
        // Randomly select among available bosses (expandable for future bosses)
        int bossIndex = _rand.Next(1);
        Boss boss = bossIndex switch
        {
            0 => _colossusBossPool.Get(),
            _ => _colossusBossPool.Get()
        };

        boss.Init(_player.Level, _enemyHealthMultiplier);

        // Spawn outside camera / distant from player inside arena
        float angle = (float)(_rand.NextDouble() * Math.PI * 2);
        float dist = 750f;
        Vector2 spawnPos = _camera.Position + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dist;
        float margin = boss.Size;
        spawnPos.X = MathHelper.Clamp(spawnPos.X, margin, ArenaWidth - margin);
        spawnPos.Y = MathHelper.Clamp(spawnPos.Y, margin, ArenaHeight - margin);
        boss.Position = spawnPos;

        return boss;
    }

    private void UpdateBossFight(float dt)
    {
        UpdatePlayerAndCamera(dt);
        UpdatePlayerShooting(dt);
        UpdateBullets(dt);

        if (_activeBoss != null && _activeBoss.IsActive)
        {
            _activeBoss.Update(dt, _player.Position);

            CollisionSystem.ResolveBossCollisions(_player, _bulletPool, _activeBoss, _xpPool, dt, out bool leveledUp, out bool bossDied);

            if (bossDied)
            {
                OnBossDefeated();
            }

            if (leveledUp)
            {
                TriggerLevelUp();
            }
        }
        else
        {
            OnBossDefeated();
        }

        if (_player.CurrentHealth <= 0f)
        {
            _currentState = GameState.GameOver;
        }
    }

    private void OnBossDefeated()
    {
        _activeBoss = null;
        _spawnTempoTime = 0f;
        _spawnInterval = 2.5f;
        _spawnTimer = 0f;
        _enemyHealthMultiplier *= 3f;
        _currentState = GameState.Playing;
    }

    private void TriggerLevelUp()
    {
        _stateBeforeLevelUp = _currentState;
        _currentState = GameState.LevelUp;
        _currentChoices.Clear();
        var allUpgrades = UpgradeDatabase.GetAllUpgrades();
        allUpgrades.RemoveAll(u => !u.CanRepeat && _acquiredUpgrades.Contains(u.Title));

        while (_currentChoices.Count < 3 && allUpgrades.Count > 0)
        {
            CardRarity rolledRarity = RollRarity();
            var candidates = allUpgrades.FindAll(u => u.Rarity == rolledRarity && !_currentChoices.Contains(u));
            if (candidates.Count == 0)
            {
                candidates = allUpgrades.FindAll(u => !_currentChoices.Contains(u));
            }

            if (candidates.Count > 0)
            {
                int idx = _rand.Next(candidates.Count);
                _currentChoices.Add(candidates[idx]);
            }
            else
            {
                break;
            }
        }
    }

    private CardRarity RollRarity()
    {
        double roll = _rand.NextDouble();
        if (roll < 0.60) return CardRarity.Common;     
        if (roll < 0.88) return CardRarity.Rare;       
        return CardRarity.Epic;                        
    }

    private void UpdateLevelUp()
    {
        var kState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        bool clicked = mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

        int selectedCard = -1;

        if (kState.IsKeyDown(Keys.D1) || kState.IsKeyDown(Keys.NumPad1)) selectedCard = 0;
        else if (kState.IsKeyDown(Keys.D2) || kState.IsKeyDown(Keys.NumPad2)) selectedCard = 1;
        else if (kState.IsKeyDown(Keys.D3) || kState.IsKeyDown(Keys.NumPad3)) selectedCard = 2;

        if (clicked && selectedCard == -1)
        {
            int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int totalCardsWidth = 3 * 320 + 2 * 40;
            int startCardX = (screenWidth - totalCardsWidth) / 2;

            Point mousePos = new Point(mouseState.X, mouseState.Y);
            for (int i = 0; i < _currentChoices.Count; i++)
            {
                int cardX = startCardX + i * (320 + 40);
                Rectangle cardRect = new Rectangle(cardX, 150, 320, 420);
                if (cardRect.Contains(mousePos))
                {
                    selectedCard = i;
                    break;
                }
            }
        }

        if (selectedCard >= 0 && selectedCard < _currentChoices.Count)
        {
            var chosen = _currentChoices[selectedCard];
            chosen.ApplyEffect(_player);
            if (!chosen.CanRepeat)
            {
                _acquiredUpgrades.Add(chosen.Title);
            }
            _currentState = _stateBeforeLevelUp;
        }
    }

    private void UpdateGameOver()
    {
        var kState = Keyboard.GetState();
        if (kState.IsKeyDown(Keys.R))
        {
            _player = new Player(new Vector2(1000, 1000));
            _camera.Position = _player.Position;
            _survivalTime = 0f;
            _spawnTempoTime = 0f;
            _bossTimer = 0f;
            _spawnInterval = 2.5f;
            _spawnTimer = 0f;
            _enemyHealthMultiplier = 1f;
            if (_activeBoss != null) _activeBoss.IsActive = false;
            _activeBoss = null;
            _acquiredUpgrades.Clear();
            foreach (var b in _bulletPool.Items) b.IsActive = false;
            foreach (var e in _basicEnemyPool.Items) e.IsActive = false;
            foreach (var e in _tankEnemyPool.Items) e.IsActive = false;
            foreach (var e in _fastEnemyPool.Items) e.IsActive = false;
            foreach (var x in _xpPool.Items) x.IsActive = false;
            _currentState = GameState.Playing;
        }
    }
}
