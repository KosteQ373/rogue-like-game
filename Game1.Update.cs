using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

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
            _player.Position += moveDir * _player.MoveSpeed.Value * _player.SpeedMultiplier * dt;
        }

        // [TESTOWE NARZĘDZIE] Naciśnij "L", aby natychmiast awansować na następny poziom.
        // USUŃ lub zakomentuj przed wydaniem finalnej wersji gry.
        if (kState.IsKeyDown(Keys.L) && !_lastKState.IsKeyDown(Keys.L))
        {
            _player.Xp = _player.XpToNextLevel;
            _player.AddXp(1f, out bool leveledUp);
            if (leveledUp)
            {
                TriggerLevelUp();
            }
        }

        // Clamp player to 2000x2000 arena
        _player.Position.X = MathHelper.Clamp(_player.Position.X, 20f, ArenaWidth - 20f);
        _player.Position.Y = MathHelper.Clamp(_player.Position.Y, 20f, ArenaHeight - 20f);

        // [TESTOWE NARZĘDZIE] Zapisz obecny stan klawiatury na kolejną klatkę
        _lastKState = kState;

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
        // Auto Aim strzela automatycznie; bez karty wymagane jest kliknięcie myszy
        bool canShoot = _player.HasAutoAim || mouseState.LeftButton == ButtonState.Pressed;
        if (canShoot && _player.ShootTimer >= _player.ShootInterval)
        {
            _player.ShootTimer = 0f;

            // Convert screen mouse position to world position based on camera dead zone position
            Vector2 screenMousePos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 cameraOffset = _camera.GetOffset(halfW, halfH);
            Vector2 worldMousePos = screenMousePos - cameraOffset;

            Vector2 shootDir;
            if (_player.HasAutoAim)
            {
                // Auto Aim — celuj w najbliższego wroga
                shootDir = FindNearestEnemyDirection(_player.Position);
            }
            else
            {
                shootDir = worldMousePos - _player.Position;
            }

            if (shootDir.LengthSquared() > 0.001f)
            {
                shootDir.Normalize();
            }
            else
            {
                shootDir = new Vector2(1f, 0f);
            }

            // Overcharge — 20% szans na 3 dodatkowe pociski
            int overchargeExtra = 0;
            if (_player.HasOvercharge && (float)_rand.NextDouble() < 0.2f)
            {
                overchargeExtra = 3;
            }

            int count = _player.ProjectilesCount + overchargeExtra;
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

                if (_player.HasFireBullets)
                {
                    bullet.HitEffects.Add(new FireHitEffect(4f, 8f));
                }

                if (_player.HasFreezeBullets)
                {
                    bullet.HitEffects.Add(new FreezeHitEffect(3f));
                }

                if (_player.HasInfinityRange)
                {
                    bullet.MaxDistance = float.MaxValue;
                    bullet.MaxLifetime = float.MaxValue;
                }
                else if (_player.HasBoomerang)
                {
                    bullet.MaxDistance = range * 2.5f;
                    float outDistance = range * 0.5f;
                    float returnDelay = outDistance / 600f;
                    bullet.TrajectoryModifiers.Add(new ReturningTrajectoryModifier(returnDelay));
                }

                if (_player.HasHomingBullets)
                {
                    bullet.TrajectoryModifiers.Add(new HomingTrajectoryModifier());
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
                // Wyciągnij wrogów w pobliżu pocisku
                _spatialGrid.GetNearby(bullets[i].Position, _nearbyEnemiesBuffer);
                
                bullets[i].Update(dt, _player.Position, _nearbyEnemiesBuffer);

                if (bullets[i].Position.X < 0f || bullets[i].Position.X > ArenaWidth ||
                    bullets[i].Position.Y < 0f || bullets[i].Position.Y > ArenaHeight)
                {
                    bullets[i].IsActive = false;
                }
            }
        }
    }

    private void UpdateDamageNumbers(float dt)
    {
        var numbers = _damageNumberPool.Items;
        for (int i = 0; i < numbers.Count; i++)
        {
            if (numbers[i].IsActive)
            {
                numbers[i].Update(dt);
            }
        }
    }

    private void UpdateDeathEffects(float dt)
    {
        var particles = _deathParticlePool.Items;
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i].IsActive)
            {
                particles[i].Update(dt);
            }
        }

        var rings = _deathRingPool.Items;
        for (int i = 0; i < rings.Count; i++)
        {
            if (rings[i].IsActive)
            {
                rings[i].Update(dt);
            }
        }
    }

    private void UpdateExplosionEffects(float dt)
    {
        var particles = _explosionParticlePool.Items;
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i].IsActive)
            {
                particles[i].Update(dt);
            }
        }

        var rings = _explosionRingPool.Items;
        for (int i = 0; i < rings.Count; i++)
        {
            if (rings[i].IsActive)
            {
                rings[i].Update(dt);
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

        enemy.Init(_player.Level, _hpMultiplier, _speedMultiplier, _damageMultiplier);

        float angle = (float)(_rand.NextDouble() * Math.PI * 2);
        float dist = 750f + (float)(_rand.NextDouble() * 200f); 
        Vector2 spawnPos = _camera.Position + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dist;
        
        // Clamp spawn inside arena
        spawnPos.X = MathHelper.Clamp(spawnPos.X, 20f, ArenaWidth - 20f);
        spawnPos.Y = MathHelper.Clamp(spawnPos.Y, 20f, ArenaHeight - 20f);
        enemy.Position = spawnPos;
    }

    // Zbierz aktywnych wrogów dla homingu
    var basics = _basicEnemyPool.Items;
    var tanks = _tankEnemyPool.Items;
    var fasts = _fastEnemyPool.Items;
    UpdateBullets(dt);
    UpdateDamageNumbers(dt);
    UpdateDeathEffects(dt);
    UpdateExplosionEffects(dt);

    // Time Dilation — spowolnienie wrogów w zasięgu gracza
    float enemySpeedMultiplier = 1f;
    if (_player.HasTimeSlow)
    {
        enemySpeedMultiplier = 0.6f; // 40% spowolnienie
    }

    // Regeneration — leczenie 3 HP na sekundę
    if (_player.RegenerationTimer > 0f)
    {
        _player.Heal(3f * dt);
    }

    // --- ZMIANY DLA HORDY ZACZYNAJĄ SIĘ TUTAJ ---

    // 1. Czyścimy siatkę i wrzucamy do niej wszystkich aktywnych wrogów
    _spatialGrid.Clear();
    for (int i = 0; i < basics.Count; i++) if (basics[i].IsActive) _spatialGrid.Add(basics[i]);
    for (int i = 0; i < tanks.Count; i++) if (tanks[i].IsActive) _spatialGrid.Add(tanks[i]);
    for (int i = 0; i < fasts.Count; i++) if (fasts[i].IsActive) _spatialGrid.Add(fasts[i]);

    // 2. Update Basics
    for (int i = 0; i < basics.Count; i++)
    {
        var e = basics[i];
        if (!e.IsActive) continue;
        float sm = enemySpeedMultiplier;
        if (_player.HasTimeSlow)
        {
            float distSq = Vector2.DistanceSquared(e.Position, _player.Position);
            if (distSq > 300f * 300f) sm = 1f;
        }
        
        // Pobieramy sąsiadów z siatki i przekazujemy do Update
        _spatialGrid.GetNearby(e, _nearbyEnemiesBuffer);
        e.Update(dt, _player.Position, _xpPool, SpawnDamageNumber, SpawnDeathEffect, sm, _nearbyEnemiesBuffer);
    }

    // 3. Update Tanks
    for (int i = 0; i < tanks.Count; i++)
    {
        var e = tanks[i];
        if (!e.IsActive) continue;
        float sm = enemySpeedMultiplier;
        if (_player.HasTimeSlow)
        {
            float distSq = Vector2.DistanceSquared(e.Position, _player.Position);
            if (distSq > 300f * 300f) sm = 1f;
        }
        
        // Pobieramy sąsiadów z siatki i przekazujemy do Update
        _spatialGrid.GetNearby(e, _nearbyEnemiesBuffer);
        e.Update(dt, _player.Position, _xpPool, SpawnDamageNumber, SpawnDeathEffect, sm, _nearbyEnemiesBuffer);
    }

    // 4. Update Fasts
    for (int i = 0; i < fasts.Count; i++)
    {
        var e = fasts[i];
        if (!e.IsActive) continue;
        float sm = enemySpeedMultiplier;
        if (_player.HasTimeSlow)
        {
            float distSq = Vector2.DistanceSquared(e.Position, _player.Position);
            if (distSq > 300f * 300f) sm = 1f;
        }
        
        // Pobieramy sąsiadów z siatki i przekazujemy do Update
        _spatialGrid.GetNearby(e, _nearbyEnemiesBuffer);
        e.Update(dt, _player.Position, _xpPool, SpawnDamageNumber, SpawnDeathEffect, sm, _nearbyEnemiesBuffer);
    }

    // --- KONIEC ZMIAN DLA HORDY ---

    // Resolve Collisions
    CollisionSystem.ResolveCollisions(_player, _bulletPool, _basicEnemyPool, _tankEnemyPool, _fastEnemyPool, _xpPool, dt, out bool leveledUp, SpawnDamageNumber, SpawnDeathEffect, SpawnExplosionEffect);
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

        _bossBannerTimer = BossBannerDuration;
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

        boss.Init(_player.Level, _hpMultiplier, _speedMultiplier, _damageMultiplier);

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
        if (_bossBannerTimer > 0f)
        {
            _bossBannerTimer = Math.Max(0f, _bossBannerTimer - dt);
        }

        UpdatePlayerAndCamera(dt);
        UpdatePlayerShooting(dt);

        // Czyścimy siatkę i wrzucamy do niej bossa, żeby pociski mogły go namierzyć (np. dla Homing)
        _spatialGrid.Clear();
        if (_activeBoss != null && _activeBoss.IsActive)
        {
            _spatialGrid.Add(_activeBoss);
        }

        // Teraz wywołujemy UpdateBullets poprawnie, tylko z jednym argumentem
        UpdateBullets(dt);
        
        UpdateDamageNumbers(dt);
        UpdateDeathEffects(dt);
        UpdateExplosionEffects(dt);

        if (_activeBoss != null && _activeBoss.IsActive)
        {
            float bossSm = 1f;
            if (_player.HasTimeSlow)
            {
                float distSq = Vector2.DistanceSquared(_activeBoss.Position, _player.Position);
                if (distSq <= 300f * 300f) bossSm = 0.6f;
            }
            _activeBoss.Update(dt, _player.Position, _xpPool, SpawnDamageNumber, SpawnDeathEffect, bossSm);

            if (!_activeBoss.IsActive || _activeBoss.Health <= 0f)
            {
                _activeBoss.IsActive = false;
                for (int g = 0; g < _activeBoss.XpDropCount; g++)
                {
                    var xp = _xpPool.Get();
                    float angle = g * (MathHelper.TwoPi / _activeBoss.XpDropCount);
                    float dropRadius = 35f + (g % 3) * 15f;
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dropRadius;
                    xp.Position = _activeBoss.Position + offset;
                    xp.Value = 1;
                }
                OnBossDefeated();
                return;
            }

            CollisionSystem.ResolveBossCollisions(_player, _bulletPool, _activeBoss, _xpPool, dt, out bool leveledUp, out bool bossDied, SpawnDamageNumber, SpawnDeathEffect, SpawnExplosionEffect);

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
            _activeBoss = null;
            _spawnTempoTime = 0f;
            _spawnInterval = 2.5f;
            _spawnTimer = 0f;
            _currentState = GameState.Playing;
        }

        if (_player.CurrentHealth <= 0f)
        {
            _currentState = GameState.GameOver;
        }
    }

    private void OnBossDefeated()
    {
        _activeBoss = null;
        _bossesDefeatedCount++;
        _hpMultiplier *= (_bossesDefeatedCount + 1);
        _speedMultiplier += _speedMultiplier * 0.15f;
        _damageMultiplier += _damageMultiplier * 0.25f;

        if (!_isEndlessMode && _bossesDefeatedCount >= 5)
        {
            _currentState = GameState.YouWin;
            return;
        }
        _spawnTempoTime = 0f;
        _spawnInterval = 2.5f;
        _spawnTimer = 0f;
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
        if (roll < 0.55) return CardRarity.Common;     
        if (roll < 0.80) return CardRarity.Rare;       
        if (roll < 0.95) return CardRarity.Epic;       
        return CardRarity.Legendary;                   
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
            int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

            float uiScale = Math.Min(screenWidth / 1280f, screenHeight / 720f);
            uiScale = MathHelper.Clamp(uiScale, 0.7f, 2.5f);

            int cardWidth = (int)(300f * uiScale);
            int cardHeight = (int)(410f * uiScale);
            int cardGap = (int)(30f * uiScale);
            int totalCardsWidth = 3 * cardWidth + 2 * cardGap;
            int startCardX = (screenWidth - totalCardsWidth) / 2;
            int startCardY = (int)(screenHeight * 0.22f);

            Point mousePos = new Point(mouseState.X, mouseState.Y);
            for (int i = 0; i < _currentChoices.Count; i++)
            {
                int cardX = startCardX + i * (cardWidth + cardGap);
                Rectangle cardRect = new Rectangle(cardX, startCardY, cardWidth, cardHeight);
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
            if (!IsUpgradeBlocked(chosen))
            {
                chosen.ApplyEffect(_player);
                _acquiredUpgrades.Add(chosen.Title);
                
                // Miejsca gdzie dodajemy cache wykluczeń:
                _mutuallyExclusiveTagsCache |= chosen.MutuallyExclusiveWith;
                
                _currentState = _stateBeforeLevelUp;
            }
        }
    }

    private void ResetGame()
    {
        _player = new Player(new Vector2(1000, 1000));
        _camera.Position = _player.Position;
        _survivalTime = 0f;
        _spawnTempoTime = 0f;
        _bossTimer = 0f;
        _bossBannerTimer = 0f;
        _spawnInterval = 2.5f;
        _spawnTimer = 0f;
        _hpMultiplier = 1f;
        _speedMultiplier = 1f;
        _damageMultiplier = 1f;
        _bossesDefeatedCount = 0;
        if (_activeBoss != null) _activeBoss.IsActive = false;
        _activeBoss = null;
        _acquiredUpgrades.Clear();
        foreach (var b in _bulletPool.Items) b.IsActive = false;
        foreach (var e in _basicEnemyPool.Items) e.IsActive = false;
        foreach (var e in _tankEnemyPool.Items) e.IsActive = false;
        foreach (var e in _fastEnemyPool.Items) e.IsActive = false;
        foreach (var x in _xpPool.Items) x.IsActive = false;
        foreach (var p in _deathParticlePool.Items) p.IsActive = false;
        foreach (var r in _deathRingPool.Items) r.IsActive = false;
        foreach (var p in _explosionParticlePool.Items) p.IsActive = false;
        foreach (var r in _explosionRingPool.Items) r.IsActive = false;
        _currentState = GameState.Playing;
    }

    private bool IsKeyJustPressed(Keys key)
    {
        return Keyboard.GetState().IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
    }

    private bool IsMouseClicked()
    {
        return Mouse.GetState().LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;
    }

    private void UpdateMainMenu()
    {
        var kState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        bool clicked = IsMouseClicked();
        Point mousePos = new Point(mouseState.X, mouseState.Y);

        int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float uiScale = Math.Min(screenWidth / 1280f, screenHeight / 720f);
        uiScale = MathHelper.Clamp(uiScale, 0.75f, 2.5f);

        int btnW = (int)(420f * uiScale);
        int btnH = (int)(58f * uiScale);
        int btnGap = (int)(16f * uiScale);
        int startY = (int)(screenHeight * 0.35f);
        int btnX = (screenWidth - btnW) / 2;

        Rectangle playRect = new Rectangle(btnX, startY, btnW, btnH);
        Rectangle endlessRect = new Rectangle(btnX, startY + btnH + btnGap, btnW, btnH);
        Rectangle settingsRect = new Rectangle(btnX, startY + (btnH + btnGap) * 2, btnW, btnH);
        Rectangle exitRect = new Rectangle(btnX, startY + (btnH + btnGap) * 3, btnW, btnH);

        if (IsKeyJustPressed(Keys.D1) || IsKeyJustPressed(Keys.NumPad1) || (clicked && playRect.Contains(mousePos)))
        {
            _isEndlessMode = false;
            _currentState = GameState.CharacterSelect;
            return;
        }

        if (IsKeyJustPressed(Keys.D2) || IsKeyJustPressed(Keys.NumPad2) || IsKeyJustPressed(Keys.E) || (clicked && endlessRect.Contains(mousePos)))
        {
            _isEndlessMode = true;
            _currentState = GameState.CharacterSelect;
            return;
        }

        if (IsKeyJustPressed(Keys.D3) || IsKeyJustPressed(Keys.NumPad3) || IsKeyJustPressed(Keys.S) || (clicked && settingsRect.Contains(mousePos)))
        {
            _currentState = GameState.Settings;
            return;
        }

        if (IsKeyJustPressed(Keys.Escape) || IsKeyJustPressed(Keys.D4) || IsKeyJustPressed(Keys.NumPad4) || IsKeyJustPressed(Keys.Q) || (clicked && exitRect.Contains(mousePos)))
        {
            Exit();
        }
    }

    private void UpdateCharacterSelect()
    {
        var kState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        bool clicked = IsMouseClicked();
        Point mousePos = new Point(mouseState.X, mouseState.Y);

        int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float uiScale = Math.Min(screenWidth / 1280f, screenHeight / 720f);
        uiScale = MathHelper.Clamp(uiScale, 0.75f, 2.5f);

        int cardW = (int)(340f * uiScale);
        int cardH = (int)(440f * uiScale);
        int cardGap = (int)(26f * uiScale);
        int totalW = 3 * cardW + 2 * cardGap;
        int startX = (screenWidth - totalW) / 2;
        int startY = (int)(screenHeight * 0.22f);

        Rectangle hero1Rect = new Rectangle(startX, startY, cardW, cardH);
        Rectangle backBtnRect = new Rectangle((screenWidth - (int)(280f * uiScale)) / 2, screenHeight - (int)(80f * uiScale), (int)(280f * uiScale), (int)(54f * uiScale));

        // Hero 1 (Commando) selected -> start game
        if (IsKeyJustPressed(Keys.D1) || IsKeyJustPressed(Keys.NumPad1) || IsKeyJustPressed(Keys.Enter) || IsKeyJustPressed(Keys.Space) || (clicked && hero1Rect.Contains(mousePos)))
        {
            ResetGame();
            return;
        }

        // Back to main menu
        if (IsKeyJustPressed(Keys.Escape) || IsKeyJustPressed(Keys.B) || (clicked && backBtnRect.Contains(mousePos)))
        {
            _currentState = GameState.MainMenu;
        }
    }

    private void UpdateSettings()
    {
        var kState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        bool clicked = IsMouseClicked();
        Point mousePos = new Point(mouseState.X, mouseState.Y);

        int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float uiScale = Math.Min(screenWidth / 1280f, screenHeight / 720f);
        uiScale = MathHelper.Clamp(uiScale, 0.75f, 2.5f);

        int panelW = (int)(620f * uiScale);
        int panelH = (int)(460f * uiScale);
        int panelX = (screenWidth - panelW) / 2;
        int panelY = (screenHeight - panelH) / 2;

        Rectangle fullscreenBtnRect = new Rectangle(panelX + (int)(40f * uiScale), panelY + (int)(110f * uiScale), panelW - (int)(80f * uiScale), (int)(56f * uiScale));
        Rectangle soundBtnRect = new Rectangle(panelX + (int)(40f * uiScale), panelY + (int)(180f * uiScale), panelW - (int)(80f * uiScale), (int)(56f * uiScale));
        Rectangle backBtnRect = new Rectangle(panelX + (panelW - (int)(280f * uiScale)) / 2, panelY + panelH - (int)(74f * uiScale), (int)(280f * uiScale), (int)(54f * uiScale));

        if (IsKeyJustPressed(Keys.F11) || IsKeyJustPressed(Keys.F) || (clicked && fullscreenBtnRect.Contains(mousePos)))
        {
            ToggleFullscreen();
        }

        if (IsKeyJustPressed(Keys.M) || (clicked && soundBtnRect.Contains(mousePos)))
        {
            _soundEnabled = !_soundEnabled;
        }

        if (IsKeyJustPressed(Keys.Escape) || IsKeyJustPressed(Keys.B) || (clicked && backBtnRect.Contains(mousePos)))
        {
            _currentState = GameState.MainMenu;
        }
    }

    private void UpdateGameOver()
    {
        var kState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        bool clicked = IsMouseClicked();
        Point mousePos = new Point(mouseState.X, mouseState.Y);

        int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float uiScale = Math.Min(screenWidth / 1280f, screenHeight / 720f);
        uiScale = MathHelper.Clamp(uiScale, 0.75f, 2.5f);

        int modalW = (int)(560f * uiScale);
        int modalH = (int)(430f * uiScale);
        int modalX = (screenWidth - modalW) / 2;
        int modalY = (screenHeight - modalH) / 2;

        int btnW = (int)(240f * uiScale);
        int btnH = (int)(54f * uiScale);
        int btnGap = (int)(20f * uiScale);
        int totalBtnW = btnW * 2 + btnGap;
        int startBtnX = modalX + (modalW - totalBtnW) / 2;
        int btnY = modalY + modalH - (int)(72f * uiScale);

        Rectangle restartBtnRect = new Rectangle(startBtnX, btnY, btnW, btnH);
        Rectangle menuBtnRect = new Rectangle(startBtnX + btnW + btnGap, btnY, btnW, btnH);

        if (IsKeyJustPressed(Keys.R) || (clicked && restartBtnRect.Contains(mousePos)))
        {
            ResetGame();
            return;
        }

        if (IsKeyJustPressed(Keys.Escape) || IsKeyJustPressed(Keys.M) || (clicked && menuBtnRect.Contains(mousePos)))
        {
            _currentState = GameState.MainMenu;
        }
    }

    private void UpdateYouWin()
    {
        var kState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        bool clicked = IsMouseClicked();
        Point mousePos = new Point(mouseState.X, mouseState.Y);

        int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float uiScale = Math.Min(screenWidth / 1280f, screenHeight / 720f);
        uiScale = MathHelper.Clamp(uiScale, 0.75f, 2.5f);

        int modalW = (int)(560f * uiScale);
        int modalH = (int)(430f * uiScale);
        int modalX = (screenWidth - modalW) / 2;
        int modalY = (screenHeight - modalH) / 2;

        int btnW = (int)(240f * uiScale);
        int btnH = (int)(54f * uiScale);
        int btnGap = (int)(20f * uiScale);
        int totalBtnW = btnW * 2 + btnGap;
        int startBtnX = modalX + (modalW - totalBtnW) / 2;
        int btnY = modalY + modalH - (int)(72f * uiScale);

        Rectangle restartBtnRect = new Rectangle(startBtnX, btnY, btnW, btnH);
        Rectangle menuBtnRect = new Rectangle(startBtnX + btnW + btnGap, btnY, btnW, btnH);

        if (IsKeyJustPressed(Keys.R) || (clicked && restartBtnRect.Contains(mousePos)))
        {
            ResetGame();
            return;
        }

        if (IsKeyJustPressed(Keys.Escape) || IsKeyJustPressed(Keys.M) || (clicked && menuBtnRect.Contains(mousePos)))
        {
            _currentState = GameState.MainMenu;
        }
    }
    
    // Zwraca listę z bossiem jeśli jest aktywny, pustą listę w przeciwnym razie
    private IEnumerable<Enemy> CollectBossEnemy()
    {
        if (_activeBoss != null && _activeBoss.IsActive)
        {
            return new[] { _activeBoss };
        }
        return Array.Empty<Enemy>();
    }

    // Znajduje kierunek do najbliższego wroga (dla Auto Aim)
    private Vector2 FindNearestEnemyDirection(Vector2 playerPos)
    {
        float nearestDistSq = float.MaxValue;
        Vector2 direction = Vector2.Zero;

        var basics = _basicEnemyPool.Items;
        var tanks = _tankEnemyPool.Items;
        var fasts = _fastEnemyPool.Items;

        foreach (var e in basics)
        {
            if (!e.IsActive) continue;
            float distSq = Vector2.DistanceSquared(playerPos, e.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                direction = e.Position - playerPos;
            }
        }

        foreach (var e in tanks)
        {
            if (!e.IsActive) continue;
            float distSq = Vector2.DistanceSquared(playerPos, e.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                direction = e.Position - playerPos;
            }
        }

        foreach (var e in fasts)
        {
            if (!e.IsActive) continue;
            float distSq = Vector2.DistanceSquared(playerPos, e.Position);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                direction = e.Position - playerPos;
            }
        }

        // Boss ma priorytet jeśli jest bliski
        if (_activeBoss != null && _activeBoss.IsActive)
        {
            float distSq = Vector2.DistanceSquared(playerPos, _activeBoss.Position);
            if (distSq < nearestDistSq)
            {
                direction = _activeBoss.Position - playerPos;
            }
        }

        return direction;
    }
}
