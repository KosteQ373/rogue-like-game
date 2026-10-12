using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace rogue_like;

public partial class Game1
{
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(20, 20, 26));

        int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float uiScale = Math.Min(screenWidth / 1280f, screenHeight / 720f);
        uiScale = MathHelper.Clamp(uiScale, 0.75f, 2.5f);

        MouseState mouseState = Mouse.GetState();

        // 1. Menu and Config Screens
        if (_currentState == GameState.MainMenu)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawMainMenuScreen(screenWidth, screenHeight, uiScale, mouseState);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }
        else if (_currentState == GameState.CharacterSelect)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawCharacterSelectScreen(screenWidth, screenHeight, uiScale, mouseState);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }
        else if (_currentState == GameState.Settings)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawSettingsScreen(screenWidth, screenHeight, uiScale, mouseState);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // 2. Gameplay World (with camera transform)
        float halfW = screenWidth / 2f;
        float halfH = screenHeight / 2f;
        Matrix cameraTransform = _camera.GetViewMatrix(halfW, halfH);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: cameraTransform);

        // Draw 2000x2000 arena background with grid lines
        DrawArenaBackground();

        // Draw XP Gems
        var xps = _xpPool.Items;
        for (int i = 0; i < xps.Count; i++)
        {
            if (xps[i].IsActive)
            {
                DrawRect(xps[i].Position, 8, 8, new Color(40, 220, 100));
                DrawRect(xps[i].Position, 4, 4, Color.White);
            }
        }

        // Y-sorted rysowanie wrogów i gracza (zero GC allocations)
        if (_currentState == GameState.Playing || _currentState == GameState.BossFight)
        {
            _ySortedEnemies.Clear();

            var basics = _basicEnemyPool.Items;
            for (int i = 0; i < basics.Count; i++)
                if (basics[i].IsActive) _ySortedEnemies.Add(basics[i]);

            var tanks = _tankEnemyPool.Items;
            for (int i = 0; i < tanks.Count; i++)
                if (tanks[i].IsActive) _ySortedEnemies.Add(tanks[i]);

            var fasts = _fastEnemyPool.Items;
            for (int i = 0; i < fasts.Count; i++)
                if (fasts[i].IsActive) _ySortedEnemies.Add(fasts[i]);

            _ySortedEnemies.Sort(EnemyYComparer);

            bool playerDrawn = false;
            for (int i = 0; i < _ySortedEnemies.Count; i++)
            {
                var enemy = _ySortedEnemies[i];

                // Gdy trafimy na pierwszego wroga poniżej gracza, rysujemy najpierw gracza
                if (!playerDrawn && enemy.Position.Y > _player.Position.Y)
                {
                    DrawPlayerWithBow(gameTime, mouseState);
                    playerDrawn = true;
                }

                DrawEnemyEntity(enemy, new Color(190, 190, 200), gameTime);
            }

            // Jeśli żaden wróg nie był poniżej gracza (lub brak wrogów), narysuj gracza na końcu
            if (!playerDrawn)
            {
                DrawPlayerWithBow(gameTime, mouseState);
            }
        }

        // Draw Death Effects (rings and particles)
        DrawDeathEffects();

        // Draw Explosion Effects
        DrawExplosionEffects();

        // Draw Active Boss (if in BossFight)
        if (_currentState == GameState.BossFight && _activeBoss != null && _activeBoss.IsActive)
        {
            Color bossBaseCol = new Color(180, 55, 75);
            Color bossCol = _activeBoss.KnockbackTimer > 0f ? Color.Lerp(bossBaseCol, Color.Yellow, 0.6f) : bossBaseCol;
            if (_activeBoss.IsFrozen)
            {
                bossCol = Color.Lerp(bossCol, new Color(100, 200, 255), 0.65f);
            }
            else if (_activeBoss.IsBurning)
            {
                float pulse = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 12.0) * 0.5 + 0.5);
                bossCol = Color.Lerp(bossCol, new Color(255, 95, 20), 0.45f + 0.3f * pulse);
            }

            DrawRect(_activeBoss.Position, (int)_activeBoss.Size, (int)_activeBoss.Size, bossCol);
            DrawRect(_activeBoss.Position, (int)(_activeBoss.Size * 0.7f), (int)(_activeBoss.Size * 0.7f), new Color(120, 40, 55));

            if (_activeBoss.IsFrozen)
            {
                int frostSize = (int)(_activeBoss.Size * 0.2f);
                float half = _activeBoss.Size * 0.5f;
                DrawRect(new Vector2(_activeBoss.Position.X - half + frostSize * 0.5f, _activeBoss.Position.Y - half + frostSize * 0.5f), frostSize, frostSize, new Color(180, 240, 255));
                DrawRect(new Vector2(_activeBoss.Position.X + half - frostSize * 0.5f, _activeBoss.Position.Y + half - frostSize * 0.5f), frostSize, frostSize, new Color(180, 240, 255));
                DrawRect(new Vector2(_activeBoss.Position.X - half + frostSize * 0.5f, _activeBoss.Position.Y + half - frostSize * 0.5f), frostSize, frostSize, new Color(140, 220, 255));
                DrawRect(new Vector2(_activeBoss.Position.X + half - frostSize * 0.5f, _activeBoss.Position.Y - half + frostSize * 0.5f), frostSize, frostSize, new Color(140, 220, 255));
            }

            if (_activeBoss.IsBurning)
            {
                float t = (float)gameTime.TotalGameTime.TotalSeconds * 14f;
                float half = _activeBoss.Size * 0.5f;

                for (int fi = -2; fi <= 2; fi++)
                {
                    float offsetX = fi * (half * 0.35f);
                    float fY = (float)Math.Sin(t + fi * 1.2f + _activeBoss.Position.X * 0.05f) * 6f - half - 10f;
                    DrawRect(new Vector2(_activeBoss.Position.X + offsetX, _activeBoss.Position.Y + fY), 12, 18, new Color(255, 120 + fi * 20, 20));
                    DrawRect(new Vector2(_activeBoss.Position.X + offsetX, _activeBoss.Position.Y + fY - 4f), 6, 8, new Color(255, 230, 80));
                }
            }
        }

        // Draw Bullets jako strzały z rotacją
        var bullets = _bulletPool.Items;
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i].IsActive)
            {
                Texture2D arrowTex = _player.HasFireBullets
                    ? _arrowBurnTexture
                    : _player.HasFreezeBullets
                        ? _arrowFrozenTexture
                        : _arrowTexture;

                float rotation = (float)Math.Atan2(bullets[i].Velocity.Y, bullets[i].Velocity.X);
                Vector2 arrowOrigin = new Vector2(arrowTex.Width / 2f, arrowTex.Height / 2f);

                _spriteBatch.Draw(
                    arrowTex,
                    bullets[i].Position,
                    null,
                    Color.White,
                    rotation,
                    arrowOrigin,
                    1.5f,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        // Speed boost aura — pulsujący pierścień wokół gracza
        if (_player.SpeedBoostTimer > 0f)
        {
            float t = (float)gameTime.TotalGameTime.TotalSeconds * 6f;
            float pulse = (float)Math.Sin(t) * 0.3f + 0.7f;
            float auraRadius = 24f + (float)Math.Sin(t * 0.5f) * 4f;
            int pixelCount = 24;
            Color auraColor = new Color(180, 255, 80) * pulse;
            for (int p = 0; p < pixelCount; p++)
            {
                float angle = p * (MathHelper.TwoPi / pixelCount);
                float px = _player.Position.X + (float)Math.Cos(angle) * auraRadius;
                float py = _player.Position.Y + (float)Math.Sin(angle) * auraRadius;
                DrawRect(new Vector2(px, py), 4, 4, auraColor);
            }
        }

        // Draw Damage Numbers
        var damageNumbers = _damageNumberPool.Items;
        for (int i = 0; i < damageNumbers.Count; i++)
        {
            var dn = damageNumbers[i];
            if (dn.IsActive && dn.Delay <= 0f)
            {
                string text = dn.IsCrit ? $"{Math.Round(dn.DamageValue)}!" : $"{Math.Round(dn.DamageValue)}";
                float progress = dn.Lifetime / dn.MaxLifetime;
                float alpha = 1f - progress;
                Color col = dn.HasCustomColor ? dn.CustomColor : (dn.IsCrit ? Color.Yellow : Color.White);
                col *= alpha;

                float scale = dn.IsCrit ? 1.25f : 0.9f;
                Vector2 size = MeasureText(text, scale);
                Vector2 drawPos = dn.Position - size * 0.5f;

                DrawText(text, drawPos, col, scale, shadow: true);
            }
        }

        _spriteBatch.End();

        // 3. Draw Screen Space HUD & Modals
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw Persistent HUD Elements
        DrawTopXpBar(screenWidth, uiScale);
        DrawPlayerHud(uiScale);
        DrawTimerOrBossBar(screenWidth, uiScale);
        if (_currentState != GameState.BossFight)
        {
            DrawRunStatsWidget(screenWidth, uiScale);
        }

        // Draw Boss Fight Announcement Banner (2 seconds)
        if (_bossBannerTimer > 0f)
        {
            DrawBossFightBanner(screenWidth, screenHeight, uiScale);
        }

        // Draw Modal Screens based on GameState
        switch (_currentState)
        {
            case GameState.LevelUp:
                DrawLevelUpScreen(screenWidth, screenHeight, uiScale, mouseState);
                break;
            case GameState.GameOver:
                DrawGameOverScreen(screenWidth, screenHeight, uiScale, mouseState);
                break;
            case GameState.YouWin:
                DrawYouWinScreen(screenWidth, screenHeight, uiScale, mouseState);
                break;
            case GameState.Paused:
                DrawPauseMenuScreen(screenWidth, screenHeight, uiScale, mouseState);
                break;
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Rysowanie gracza z łukiem — wywoływane z pętli Y-sorted
    private void DrawPlayerWithBow(GameTime gameTime, MouseState mouseState)
    {
        float viewHalfW = GraphicsDevice.Viewport.Width / 2f;
        float viewHalfH = GraphicsDevice.Viewport.Height / 2f;
        Vector2 cameraOffset = _camera.GetOffset(viewHalfW, viewHalfH);
        Vector2 worldMousePos = new Vector2(mouseState.X, mouseState.Y) - cameraOffset;

        Vector2 aimDir = worldMousePos - _player.Position;
        float aimAngle = (float)Math.Atan2(aimDir.Y, aimDir.X);

        // Eliptyczna pozycja łuku — spłaszczenie w osi Y dla perspektywy 2.5D
        float offsetX = (float)Math.Cos(aimAngle) * 20f;
        float offsetY = (float)Math.Sin(aimAngle) * 12f + 4f;
        Vector2 bowPos = _player.Position + new Vector2(offsetX, offsetY);

        // Pivot na środku łuku
        Vector2 bowOrigin = new Vector2(_bowTexture.Width / 2f, _bowTexture.Height / 2f);

        // Flip łuku — jeśli gracz celuje w lewo, odwróć pionowo
        SpriteEffects bowEffect = worldMousePos.X < _player.Position.X ? SpriteEffects.FlipVertically : SpriteEffects.None;

        // Z-sorting — jeśli celowanie w górę, łuk za graczem; w przeciwnym razie na wierzchu
        bool drawBowFirst = aimDir.Y < -0.1f;

        if (drawBowFirst)
        {
            // Łuk najpierw (za graczem)
            _spriteBatch.Draw(
                _bowTexture,
                bowPos,
                null,
                Color.White,
                aimAngle,
                bowOrigin,
                1.5f,
                bowEffect,
                0f
            );

            // Gracz na wierzchu
            if (_player.InvulnerabilityTimer > 0f)
            {
                int flash = (int)(gameTime.TotalGameTime.TotalMilliseconds / 80f) % 2;
                if (flash == 0)
                {
                    _player.Draw(_spriteBatch);
                }
            }
            else
            {
                _player.Draw(_spriteBatch);
            }
        }
        else
        {
            // Gracz najpierw
            if (_player.InvulnerabilityTimer > 0f)
            {
                int flash = (int)(gameTime.TotalGameTime.TotalMilliseconds / 80f) % 2;
                if (flash == 0)
                {
                    _player.Draw(_spriteBatch);
                }
            }
            else
            {
                _player.Draw(_spriteBatch);
            }

            // Łuk na wierzchu
            _spriteBatch.Draw(
                _bowTexture,
                bowPos,
                null,
                Color.White,
                aimAngle,
                bowOrigin,
                1.5f,
                bowEffect,
                0f
            );
        }
    }

    private void DrawArenaBackground()
    {
        // Kafelkowana podłoga z tekstur kamiennych płyt
        Color stoneTint = new Color(40, 42, 48);

        for (int y = 0; y < FloorRows; y++)
        {
            for (int x = 0; x < FloorCols; x++)
            {
                int texIndex = _floorMap[x, y];
                Vector2 tilePos = new Vector2(x * TileSize, y * TileSize);
                _spriteBatch.Draw(_floorTileTextures[texIndex], tilePos, stoneTint);
            }
        }

        // Arena outer border
        int borderW = 8;
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, (int)ArenaWidth, borderW), new Color(90, 95, 115));
        _spriteBatch.Draw(_pixel, new Rectangle(0, (int)ArenaHeight - borderW, (int)ArenaWidth, borderW), new Color(90, 95, 115));
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, borderW, (int)ArenaHeight), new Color(90, 95, 115));
        _spriteBatch.Draw(_pixel, new Rectangle((int)ArenaWidth - borderW, 0, borderW, (int)ArenaHeight), new Color(90, 95, 115));
    }

    private void DrawTopXpBar(int screenWidth, float uiScale)
    {
        int barHeight = Math.Max(14, (int)(16f * uiScale));
        float xpMax = _player.XpToNextLevel;
        float xpPct = xpMax > 0f ? MathHelper.Clamp(_player.Xp / xpMax, 0f, 1f) : 0f;

        // Background track
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, barHeight), new Color(14, 16, 22, 240));

        // Fill
        int fillWidth = (int)(screenWidth * xpPct);
        if (fillWidth > 0)
        {
            Color xpFillColor = new Color(35, 195, 240);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, fillWidth, barHeight), xpFillColor);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, fillWidth, Math.Max(2, (int)(4f * uiScale))), Color.White * 0.45f);
        }

        // Bottom border line
        _spriteBatch.Draw(_pixel, new Rectangle(0, barHeight - 1, screenWidth, 1), new Color(55, 65, 85));
    }

    private void DrawPlayerHud(float uiScale)
    {
        int topBarOffset = Math.Max(14, (int)(16f * uiScale));
        int panelX = (int)(20f * uiScale);
        int panelY = topBarOffset + (int)(12f * uiScale);
        int panelWidth = (int)(380f * uiScale);
        int panelHeight = (int)(76f * uiScale);

        Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        DrawPanel(panelRect, new Color(18, 20, 26, 235), new Color(50, 58, 76), Math.Max(2, (int)(2.5f * uiScale)));

        // Level Badge Box
        int badgeSize = panelHeight - (int)(14f * uiScale);
        Rectangle badgeRect = new Rectangle(panelX + (int)(8f * uiScale), panelY + (int)(7f * uiScale), (int)(64f * uiScale), badgeSize);
        DrawPanel(badgeRect, new Color(24, 32, 48), new Color(40, 185, 235), Math.Max(1, (int)(2f * uiScale)));

        float lvlLabelScale = MathHelper.Clamp(uiScale * 0.72f, 0.5f, 1.1f);
        float lvlNumScale = MathHelper.Clamp(uiScale * 1.25f, 0.85f, 1.8f);
        DrawCenteredText("LVL", new Rectangle(badgeRect.X, badgeRect.Y + (int)(3f * uiScale), badgeRect.Width, (int)(16f * uiScale)), new Color(140, 210, 255), lvlLabelScale);
        DrawCenteredText($"{_player.Level}", new Rectangle(badgeRect.X, badgeRect.Y + (int)(20f * uiScale), badgeRect.Width, badgeRect.Height - (int)(22f * uiScale)), Color.White, lvlNumScale);

        // HP Header & Values
        float maxHp = _player.MaxHealth.Value;
        float currentHp = Math.Max(0f, _player.CurrentHealth);
        float healthPct = maxHp > 0f ? MathHelper.Clamp(currentHp / maxHp, 0f, 1f) : 0f;

        int hpBarX = panelX + badgeRect.Width + (int)(16f * uiScale);
        int hpBarY = panelY + (int)(34f * uiScale);
        int hpBarWidth = panelWidth - badgeRect.Width - (int)(26f * uiScale);
        int hpBarHeight = (int)(28f * uiScale);

        float textScale = MathHelper.Clamp(uiScale * 0.85f, 0.6f, 1.2f);
        DrawText("HEALTH", new Vector2(hpBarX, panelY + (int)(10f * uiScale)), new Color(200, 205, 215), textScale);

        string hpValStr = $"{FormatNumber(currentHp)} / {FormatNumber(maxHp)}";
        Vector2 valSize = MeasureText(hpValStr, textScale);
        DrawText(hpValStr, new Vector2(hpBarX + hpBarWidth - valSize.X, panelY + (int)(10f * uiScale)), new Color(255, 110, 110), textScale);

        // HP Bar Track & Fill
        Rectangle hpBarRect = new Rectangle(hpBarX, hpBarY, hpBarWidth, hpBarHeight);
        Color hpTrackColor = new Color(38, 12, 16);
        Color hpBorderColor = new Color(85, 26, 34);
        Color hpFillColor = healthPct < 0.25f ? new Color(255, 30, 40) : new Color(225, 38, 48);

        DrawProgressBar(hpBarRect, healthPct, hpFillColor, hpTrackColor, hpBorderColor, Color.White * 0.35f, uiScale);

        // Health percentage inside bar
        string pctStr = $"{(int)(healthPct * 100)}%";
        float pctScale = MathHelper.Clamp(uiScale * 0.78f, 0.55f, 1.1f);
        DrawCenteredText(pctStr, hpBarRect, Color.White, pctScale);
    }

    private void DrawTimerOrBossBar(int screenWidth, float uiScale)
    {
        int topBarOffset = Math.Max(14, (int)(16f * uiScale));
        int minutes = (int)(_survivalTime / 60f);
        int seconds = (int)(_survivalTime % 60f);
        string timeStr = $"{minutes:D2}:{seconds:D2}";

        if (_currentState == GameState.BossFight && _activeBoss != null && _activeBoss.IsActive)
        {
            int bossBarWidth = (int)(580f * uiScale);
            int bossBarHeight = (int)(76f * uiScale);
            int bossBarX = screenWidth - bossBarWidth - (int)(20f * uiScale);
            int bossBarY = topBarOffset + (int)(12f * uiScale);

            Rectangle bossPanelRect = new Rectangle(bossBarX, bossBarY, bossBarWidth, bossBarHeight);
            DrawPanel(bossPanelRect, new Color(22, 12, 16, 240), new Color(200, 28, 48), Math.Max(2, (int)(2.5f * uiScale)));

            string bossTitleStr = (_activeBoss is Boss b) ? b.BossTitle : "BOSS";
            string bossHeader = $"[BOSS] {bossTitleStr}";
            float headerScale = MathHelper.Clamp(uiScale * 0.95f, 0.7f, 1.4f);
            DrawText(bossHeader, new Vector2(bossBarX + (int)(14f * uiScale), bossBarY + (int)(8f * uiScale)), new Color(255, 75, 85), headerScale);

            float timerScale = MathHelper.Clamp(uiScale * 0.85f, 0.6f, 1.25f);
            Vector2 timeSize = MeasureText(timeStr, timerScale);
            DrawText(timeStr, new Vector2(bossBarX + bossBarWidth - timeSize.X - (int)(14f * uiScale), bossBarY + (int)(9f * uiScale)), new Color(245, 205, 100), timerScale);

            float bossMaxHp = _activeBoss.MaxHealth;
            float bossCurHp = Math.Max(0f, _activeBoss.Health);
            float bossHpPct = bossMaxHp > 0f ? MathHelper.Clamp(bossCurHp / bossMaxHp, 0f, 1f) : 0f;

            int hpInnerX = bossBarX + (int)(14f * uiScale);
            int hpInnerY = bossBarY + (int)(34f * uiScale);
            int hpInnerW = bossBarWidth - (int)(28f * uiScale);
            int hpInnerH = (int)(28f * uiScale);

            Rectangle bossHpRect = new Rectangle(hpInnerX, hpInnerY, hpInnerW, hpInnerH);
            DrawProgressBar(bossHpRect, bossHpPct, new Color(225, 25, 45), new Color(44, 10, 14), new Color(100, 22, 34), Color.White * 0.35f, uiScale);

            string bossHpText = $"{FormatNumber(bossCurHp)} / {FormatNumber(bossMaxHp)} ({(int)(bossHpPct * 100)}%)";
            float bossPctScale = MathHelper.Clamp(uiScale * 0.78f, 0.55f, 1.1f);
            DrawCenteredText(bossHpText, bossHpRect, Color.White, bossPctScale);
        }
        else
        {
            int timerWidth = (int)(210f * uiScale);
            int timerHeight = (int)(64f * uiScale);
            int timerX = (screenWidth - timerWidth) / 2;
            int timerY = topBarOffset + (int)(12f * uiScale);

            Rectangle timerRect = new Rectangle(timerX, timerY, timerWidth, timerHeight);
            DrawPanel(timerRect, new Color(16, 20, 28, 235), new Color(60, 72, 95), Math.Max(2, (int)(2.5f * uiScale)));

            float labelScale = MathHelper.Clamp(uiScale * 0.68f, 0.48f, 0.95f);
            DrawCenteredText("SURVIVAL TIME", new Rectangle(timerX, timerY + (int)(5f * uiScale), timerWidth, (int)(16f * uiScale)), new Color(165, 180, 200), labelScale);

            float timeNumScale = MathHelper.Clamp(uiScale * 1.35f, 0.95f, 2.0f);
            DrawCenteredText(timeStr, new Rectangle(timerX, timerY + (int)(22f * uiScale), timerWidth, timerHeight - (int)(24f * uiScale)), new Color(255, 225, 100), timeNumScale);
        }
    }

    private void DrawRunStatsWidget(int screenWidth, float uiScale)
    {
        int topBarOffset = Math.Max(14, (int)(16f * uiScale));
        int widgetWidth = (int)(260f * uiScale);
        int widgetHeight = (int)(64f * uiScale);
        int widgetX = screenWidth - widgetWidth - (int)(20f * uiScale);
        int widgetY = topBarOffset + (int)(12f * uiScale);

        Rectangle widgetRect = new Rectangle(widgetX, widgetY, widgetWidth, widgetHeight);
        DrawPanel(widgetRect, new Color(16, 18, 24, 235), new Color(50, 58, 76), Math.Max(2, (int)(2.5f * uiScale)));

        float statScale = MathHelper.Clamp(uiScale * 0.78f, 0.55f, 1.15f);
        string bossStr = _isEndlessMode ? $"BOSSES: {_bossesDefeatedCount} (ENDLESS)" : $"BOSSES: {_bossesDefeatedCount} / 5";
        Color bossCol = _isEndlessMode ? new Color(255, 140, 50) : new Color(255, 110, 110);
        DrawText(bossStr, new Vector2(widgetX + (int)(12f * uiScale), widgetY + (int)(10f * uiScale)), bossCol, statScale);
        DrawText($"UPGRADES: {_acquiredUpgrades.Count}", new Vector2(widgetX + (int)(12f * uiScale), widgetY + (int)(34f * uiScale)), new Color(120, 220, 160), statScale);
    }

    private void DrawBossFightBanner(int screenWidth, int screenHeight, float uiScale)
    {
        float alpha = 1.0f;
        if (_bossBannerTimer > 1.7f)
        {
            alpha = (BossBannerDuration - _bossBannerTimer) / 0.3f;
        }
        else if (_bossBannerTimer < 0.3f)
        {
            alpha = _bossBannerTimer / 0.3f;
        }
        alpha = MathHelper.Clamp(alpha, 0f, 1f);

        int bannerHeight = (int)(130f * uiScale);
        int bannerY = (int)(screenHeight * 0.36f);

        // Banner backdrop
        _spriteBatch.Draw(_pixel, new Rectangle(0, bannerY, screenWidth, bannerHeight), new Color(14, 2, 4) * (0.92f * alpha));

        // Glowing border lines
        int borderThick = Math.Max(2, (int)(4f * uiScale));
        Color borderRed = new Color(230, 25, 45) * alpha;
        Color borderGold = new Color(255, 205, 50) * alpha;

        _spriteBatch.Draw(_pixel, new Rectangle(0, bannerY, screenWidth, borderThick), borderRed);
        _spriteBatch.Draw(_pixel, new Rectangle(0, bannerY + borderThick, screenWidth, 1), borderGold);
        _spriteBatch.Draw(_pixel, new Rectangle(0, bannerY + bannerHeight - borderThick - 1, screenWidth, 1), borderGold);
        _spriteBatch.Draw(_pixel, new Rectangle(0, bannerY + bannerHeight - borderThick, screenWidth, borderThick), borderRed);

        string mainTitle = "WARNING: BOSS FIGHT";
        float titleScale = MathHelper.Clamp(uiScale * 1.8f, 1.25f, 2.8f);
        Vector2 titleSize = MeasureText(mainTitle, titleScale);
        Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2f, bannerY + (int)(20f * uiScale));
        DrawText(mainTitle, titlePos, new Color(255, 35, 45) * alpha, titleScale);

        string subTitle = "A POWERFUL FOE HAS APPEARED - DEFEAT IT TO SURVIVE!";
        float subScale = MathHelper.Clamp(uiScale * 0.88f, 0.6f, 1.35f);
        Vector2 subSize = MeasureText(subTitle, subScale);
        Vector2 subPos = new Vector2((screenWidth - subSize.X) / 2f, bannerY + bannerHeight - subSize.Y - (int)(20f * uiScale));
        DrawText(subTitle, subPos, new Color(255, 220, 120) * alpha, subScale);
    }

    private void DrawMainMenuScreen(int screenWidth, int screenHeight, float uiScale, MouseState mouseState)
    {
        // Dark backdrop with subtle grid
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), new Color(14, 14, 20));

        // Title Header Box
        string title = "ROGUE SURVIVOR";
        float titleScale = MathHelper.Clamp(uiScale * 2.5f, 1.8f, 3.8f);
        Vector2 titleSize = MeasureText(title, titleScale);
        DrawText(title, new Vector2((screenWidth - titleSize.X) / 2f, (int)(screenHeight * 0.12f)), new Color(255, 215, 60), titleScale);

        string subtitle = "ARENA SURVIVAL ROGUELIKE";
        float subScale = MathHelper.Clamp(uiScale * 1.0f, 0.7f, 1.5f);
        Vector2 subSize = MeasureText(subtitle, subScale);
        DrawText(subtitle, new Vector2((screenWidth - subSize.X) / 2f, (int)(screenHeight * 0.23f)), new Color(120, 210, 255), subScale);

        // Buttons
        int btnW = (int)(420f * uiScale);
        int btnH = (int)(58f * uiScale);
        int btnGap = (int)(16f * uiScale);
        int startY = (int)(screenHeight * 0.35f);
        int btnX = (screenWidth - btnW) / 2;

        Rectangle playRect = new Rectangle(btnX, startY, btnW, btnH);
        Rectangle endlessRect = new Rectangle(btnX, startY + btnH + btnGap, btnW, btnH);
        Rectangle settingsRect = new Rectangle(btnX, startY + (btnH + btnGap) * 2, btnW, btnH);
        Rectangle exitRect = new Rectangle(btnX, startY + (btnH + btnGap) * 3, btnW, btnH);

        Point mousePos = new Point(mouseState.X, mouseState.Y);

        DrawMenuButton("1. PLAY", playRect, playRect.Contains(mousePos), new Color(40, 180, 230), uiScale);
        DrawMenuButton("2. ENDLESS MODE", endlessRect, endlessRect.Contains(mousePos), new Color(255, 125, 45), uiScale);
        DrawMenuButton("3. SETTINGS", settingsRect, settingsRect.Contains(mousePos), new Color(255, 195, 50), uiScale);
        DrawMenuButton("4. QUIT GAME", exitRect, exitRect.Contains(mousePos), new Color(235, 45, 60), uiScale);

        // Footer hint
        string hint = "USE MOUSE OR KEYS [1-4] TO SELECT";
        float hintScale = MathHelper.Clamp(uiScale * 0.75f, 0.5f, 1.1f);
        Vector2 hintSize = MeasureText(hint, hintScale);
        DrawText(hint, new Vector2((screenWidth - hintSize.X) / 2f, screenHeight - (int)(45f * uiScale)), new Color(120, 130, 150), hintScale);
    }

    private void DrawCharacterSelectScreen(int screenWidth, int screenHeight, float uiScale, MouseState mouseState)
    {
        // Dark backdrop
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), new Color(14, 14, 20));

        // Header
        string title = "CHOOSE YOUR HERO";
        float titleScale = MathHelper.Clamp(uiScale * 1.8f, 1.25f, 2.8f);
        Vector2 titleSize = MeasureText(title, titleScale);
        DrawText(title, new Vector2((screenWidth - titleSize.X) / 2f, (int)(screenHeight * 0.07f)), new Color(255, 215, 60), titleScale);

        string modeBadge = _isEndlessMode ? "[ ENDLESS MODE ]" : "[ STANDARD MODE ]";
        Color modeBadgeColor = _isEndlessMode ? new Color(255, 130, 45) : new Color(40, 185, 235);
        float badgeScale = MathHelper.Clamp(uiScale * 0.95f, 0.65f, 1.4f);
        Vector2 badgeSize = MeasureText(modeBadge, badgeScale);
        DrawText(modeBadge, new Vector2((screenWidth - badgeSize.X) / 2f, (int)(screenHeight * 0.13f)), modeBadgeColor, badgeScale);

        string subtitle = _isEndlessMode ? "SURVIVE UNLIMITED ENEMY WAVES & BOSS FIGHTS" : "DEFEAT 5 BOSSES TO CONQUER THE ARENA";
        float subScale = MathHelper.Clamp(uiScale * 0.8f, 0.55f, 1.2f);
        Vector2 subSize = MeasureText(subtitle, subScale);
        DrawText(subtitle, new Vector2((screenWidth - subSize.X) / 2f, (int)(screenHeight * 0.17f)), new Color(160, 175, 195), subScale);

        // 3 Cards Layout
        int cardW = (int)(340f * uiScale);
        int cardH = (int)(440f * uiScale);
        int cardGap = (int)(26f * uiScale);
        int totalW = 3 * cardW + 2 * cardGap;
        int startX = (screenWidth - totalW) / 2;
        int startY = (int)(screenHeight * 0.22f);

        Point mousePos = new Point(mouseState.X, mouseState.Y);

        // Card 1: Commando (Playable)
        Rectangle card1Rect = new Rectangle(startX, startY, cardW, cardH);
        bool card1Hovered = card1Rect.Contains(mousePos);
        DrawHeroCard(card1Rect, "COMMANDO", "BALANCED SURVIVOR", "Fires rapid plasma projectiles.\nHigh mobility and steady damage output.", true, card1Hovered, uiScale, 1);

        // Card 2: Paladin (Locked Placeholder)
        Rectangle card2Rect = new Rectangle(startX + cardW + cardGap, startY, cardW, cardH);
        DrawHeroCard(card2Rect, "PALADIN", "HEAVY DEFENDER", "Heavily armored warrior with protective energy barriers.", false, false, uiScale, 2);

        // Card 3: Assassin (Locked Placeholder)
        Rectangle card3Rect = new Rectangle(startX + (cardW + cardGap) * 2, startY, cardW, cardH);
        DrawHeroCard(card3Rect, "ASSASSIN", "CRITICAL STRIKER", "Deadly shadow fighter focused on critical strikes and speed.", false, false, uiScale, 3);

        // Back Button
        int backW = (int)(280f * uiScale);
        int backH = (int)(54f * uiScale);
        Rectangle backRect = new Rectangle((screenWidth - backW) / 2, screenHeight - (int)(80f * uiScale), backW, backH);
        bool backHover = backRect.Contains(mousePos);

        DrawMenuButton("BACK [ESC]", backRect, backHover, new Color(180, 190, 205), uiScale);
    }

    private void DrawHeroCard(Rectangle rect, string name, string role, string desc, bool isUnlocked, bool isHovered, float uiScale, int heroIndex)
    {
        Rectangle drawRect = isHovered && isUnlocked
            ? new Rectangle(rect.X - (int)(4f * uiScale), rect.Y - (int)(6f * uiScale), rect.Width + (int)(8f * uiScale), rect.Height + (int)(8f * uiScale))
            : rect;

        Color bgColor = isUnlocked ? (isHovered ? new Color(26, 32, 46) : new Color(18, 22, 32)) : new Color(16, 16, 20);
        Color borderColor = isUnlocked ? (isHovered ? new Color(60, 200, 245) : new Color(40, 140, 200)) : new Color(45, 45, 55);

        DrawPanel(drawRect, bgColor, borderColor, Math.Max(2, (int)(3f * uiScale)));

        // Hero Portrait Box
        int portraitSize = (int)(80f * uiScale);
        Rectangle portraitRect = new Rectangle(drawRect.X + (drawRect.Width - portraitSize) / 2, drawRect.Y + (int)(18f * uiScale), portraitSize, portraitSize);
        DrawPanel(portraitRect, isUnlocked ? new Color(14, 28, 40) : new Color(22, 22, 26), borderColor, Math.Max(1, (int)(2f * uiScale)));

        if (isUnlocked)
        {
            // Commando green hero icon
            DrawRect(new Vector2(portraitRect.Center.X, portraitRect.Center.Y), (int)(34f * uiScale), (int)(34f * uiScale), new Color(50, 220, 100));
            DrawRect(new Vector2(portraitRect.Center.X, portraitRect.Center.Y), (int)(18f * uiScale), (int)(18f * uiScale), Color.White);
        }
        else
        {
            // Lock icon placeholder
            DrawCenteredText("[LOCKED]", portraitRect, new Color(120, 120, 130), MathHelper.Clamp(uiScale * 0.75f, 0.52f, 1.05f));
        }

        // Hero Name & Role
        float nameScale = MathHelper.Clamp(uiScale * 1.15f, 0.8f, 1.6f);
        Color nameCol = isUnlocked ? (isHovered ? Color.White : new Color(240, 245, 255)) : new Color(120, 125, 135);
        DrawCenteredText(name, new Rectangle(drawRect.X, drawRect.Y + (int)(110f * uiScale), drawRect.Width, (int)(24f * uiScale)), nameCol, nameScale);

        float roleScale = MathHelper.Clamp(uiScale * 0.72f, 0.5f, 1.0f);
        Color roleCol = isUnlocked ? new Color(120, 210, 255) : new Color(90, 95, 105);
        DrawCenteredText(role, new Rectangle(drawRect.X, drawRect.Y + (int)(138f * uiScale), drawRect.Width, (int)(18f * uiScale)), roleCol, roleScale);

        // Divider
        _spriteBatch.Draw(_pixel, new Rectangle(drawRect.X + (int)(20f * uiScale), drawRect.Y + (int)(164f * uiScale), drawRect.Width - (int)(40f * uiScale), 1), isUnlocked ? new Color(50, 80, 120) : new Color(40, 40, 48));

        // Stats or Description
        float descScale = MathHelper.Clamp(uiScale * 0.78f, 0.55f, 1.05f);
        var wrappedLines = WrapText(desc, drawRect.Width - (int)(36f * uiScale), descScale);
        int descY = drawRect.Y + (int)(180f * uiScale);
        int lineSpacing = (int)(22f * uiScale);
        for (int i = 0; i < wrappedLines.Count; i++)
        {
            DrawText(wrappedLines[i], new Vector2(drawRect.X + (int)(18f * uiScale), descY + i * lineSpacing), isUnlocked ? new Color(190, 200, 215) : new Color(110, 115, 125), descScale);
        }

        // Action Button or Locked Badge
        if (isUnlocked)
        {
            int btnH = (int)(48f * uiScale);
            Rectangle btnRect = new Rectangle(drawRect.X + (int)(16f * uiScale), drawRect.Bottom - btnH - (int)(16f * uiScale), drawRect.Width - (int)(32f * uiScale), btnH);
            if (isHovered)
            {
                DrawPanel(btnRect, new Color(45, 190, 230), Color.White, Math.Max(2, (int)(2.5f * uiScale)));
                DrawCenteredText("PLAY [CLICK]", btnRect, new Color(15, 20, 30), MathHelper.Clamp(uiScale * 0.85f, 0.58f, 1.2f), shadow: false);
            }
            else
            {
                DrawPanel(btnRect, new Color(24, 34, 48), new Color(50, 140, 190), Math.Max(1, (int)(2f * uiScale)));
                DrawCenteredText($"SELECT [{heroIndex}]", btnRect, new Color(180, 220, 255), MathHelper.Clamp(uiScale * 0.8f, 0.55f, 1.15f));
            }
        }
        else
        {
            int badgeH = (int)(44f * uiScale);
            Rectangle badgeRect = new Rectangle(drawRect.X + (int)(16f * uiScale), drawRect.Bottom - badgeH - (int)(16f * uiScale), drawRect.Width - (int)(32f * uiScale), badgeH);
            DrawPanel(badgeRect, new Color(30, 18, 22), new Color(110, 35, 45), Math.Max(1, (int)(1.5f * uiScale)));
            DrawCenteredText("LOCKED - COMING SOON", badgeRect, new Color(200, 75, 85), MathHelper.Clamp(uiScale * 0.7f, 0.48f, 0.98f));
        }
    }

    private void DrawSettingsScreen(int screenWidth, int screenHeight, float uiScale, MouseState mouseState)
    {
        // Dark backdrop
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), new Color(14, 14, 20));

        // Header
        string title = "SETTINGS";
        float titleScale = MathHelper.Clamp(uiScale * 1.8f, 1.25f, 2.8f);
        Vector2 titleSize = MeasureText(title, titleScale);
        DrawText(title, new Vector2((screenWidth - titleSize.X) / 2f, (int)(screenHeight * 0.10f)), new Color(255, 215, 60), titleScale);

        // Center Box
        int panelW = (int)(620f * uiScale);
        int panelH = (int)(460f * uiScale);
        int panelX = (screenWidth - panelW) / 2;
        int panelY = (screenHeight - panelH) / 2;

        Rectangle panelRect = new Rectangle(panelX, panelY, panelW, panelH);
        DrawPanel(panelRect, new Color(20, 22, 30, 245), new Color(55, 65, 85), Math.Max(2, (int)(3f * uiScale)));

        Point mousePos = new Point(mouseState.X, mouseState.Y);

        // Fullscreen Option Button
        Rectangle fsRect = new Rectangle(panelX + (int)(40f * uiScale), panelY + (int)(40f * uiScale), panelW - (int)(80f * uiScale), (int)(56f * uiScale));
        bool fsHover = fsRect.Contains(mousePos);
        string fsText = _graphics.IsFullScreen ? "DISPLAY MODE: FULLSCREEN [ON]" : "DISPLAY MODE: WINDOWED [OFF]";
        DrawMenuButton(fsText, fsRect, fsHover, new Color(60, 200, 245), uiScale);

        // Sound Option Button
        Rectangle sndRect = new Rectangle(panelX + (int)(40f * uiScale), panelY + (int)(110f * uiScale), panelW - (int)(80f * uiScale), (int)(56f * uiScale));
        bool sndHover = sndRect.Contains(mousePos);
        string sndText = _soundEnabled ? "SOUND EFFECTS: [ENABLED]" : "SOUND EFFECTS: [MUTED]";
        DrawMenuButton(sndText, sndRect, sndHover, new Color(255, 205, 50), uiScale);

        // Controls Guide Box
        int ctrlBoxY = panelY + (int)(185f * uiScale);
        int ctrlBoxH = (int)(180f * uiScale);
        Rectangle ctrlRect = new Rectangle(panelX + (int)(40f * uiScale), ctrlBoxY, panelW - (int)(80f * uiScale), ctrlBoxH);
        DrawPanel(ctrlRect, new Color(14, 16, 22), new Color(40, 48, 65), Math.Max(1, (int)(1.5f * uiScale)));

        float ctrlHeaderScale = MathHelper.Clamp(uiScale * 0.85f, 0.6f, 1.2f);
        DrawCenteredText("CONTROLS GUIDE", new Rectangle(ctrlRect.X, ctrlRect.Y + (int)(10f * uiScale), ctrlRect.Width, (int)(20f * uiScale)), new Color(140, 210, 255), ctrlHeaderScale);

        float ctrlLineScale = MathHelper.Clamp(uiScale * 0.78f, 0.52f, 1.1f);
        int lineH = (int)(26f * uiScale);
        int startCtrlY = ctrlRect.Y + (int)(42f * uiScale);

        DrawText("MOVE: [W, A, S, D] OR ARROW KEYS", new Vector2(ctrlRect.X + (int)(20f * uiScale), startCtrlY), new Color(200, 205, 220), ctrlLineScale);
        DrawText("AIM & SHOOT: HOLD MOUSE LEFT BUTTON", new Vector2(ctrlRect.X + (int)(20f * uiScale), startCtrlY + lineH), new Color(200, 205, 220), ctrlLineScale);
        DrawText("CHOOSE UPGRADE: [1, 2, 3] OR CLICK", new Vector2(ctrlRect.X + (int)(20f * uiScale), startCtrlY + lineH * 2), new Color(200, 205, 220), ctrlLineScale);
        DrawText("BACK / MENU: ESCAPE KEY [ESC]", new Vector2(ctrlRect.X + (int)(20f * uiScale), startCtrlY + lineH * 3), new Color(200, 205, 220), ctrlLineScale);

        // Back Button
        Rectangle backRect = new Rectangle(panelX + (panelW - (int)(280f * uiScale)) / 2, panelY + panelH - (int)(72f * uiScale), (int)(280f * uiScale), (int)(52f * uiScale));
        bool backHover = backRect.Contains(mousePos);
        DrawMenuButton("BACK TO MAIN MENU", backRect, backHover, new Color(180, 190, 205), uiScale);
    }

    private void DrawMenuButton(string text, Rectangle rect, bool isHovered, Color accentCol, float uiScale)
    {
        Color bgColor = isHovered ? accentCol : new Color(22, 26, 36);
        Color borderColor = isHovered ? Color.White : new Color(55, 65, 85);
        Color textCol = isHovered ? (accentCol == Color.White ? Color.Black : new Color(12, 16, 24)) : Color.White;

        DrawPanel(rect, bgColor, borderColor, Math.Max(2, (int)(2.5f * uiScale)));

        float textScale = MathHelper.Clamp(uiScale * 0.92f, 0.65f, 1.35f);
        DrawCenteredText(text, rect, textCol, textScale, shadow: !isHovered);
    }

    private void DrawLevelUpScreen(int screenWidth, int screenHeight, float uiScale, MouseState mouseState)
    {
        // Dark translucent overlay
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.82f);

        // Header Title
        string titleText = "LEVEL UP! CHOOSE AN UPGRADE";
        float titleScale = MathHelper.Clamp(uiScale * 1.5f, 1.05f, 2.3f);
        Vector2 titleSize = MeasureText(titleText, titleScale);
        DrawText(titleText, new Vector2((screenWidth - titleSize.X) / 2f, (int)(screenHeight * 0.08f)), new Color(255, 215, 60), titleScale);

        string subText = "SELECT ONE REWARD TO EMPOWER YOUR SURVIVOR";
        float subScale = MathHelper.Clamp(uiScale * 0.85f, 0.6f, 1.3f);
        Vector2 subSize = MeasureText(subText, subScale);
        DrawText(subText, new Vector2((screenWidth - subSize.X) / 2f, (int)(screenHeight * 0.15f)), new Color(180, 190, 205), subScale);

        // 3 Cards Layout
        int cardWidth = (int)(340f * uiScale);
        int cardHeight = (int)(450f * uiScale);
        int cardGap = (int)(26f * uiScale);
        int totalCardsWidth = 3 * cardWidth + 2 * cardGap;
        int startCardX = (screenWidth - totalCardsWidth) / 2;
        int startCardY = (int)(screenHeight * 0.20f);

        Point mousePos = new Point(mouseState.X, mouseState.Y);

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var card = _currentChoices[i];
            int cardX = startCardX + i * (cardWidth + cardGap);
            Rectangle cardRect = new Rectangle(cardX, startCardY, cardWidth, cardHeight);

            bool isBlocked = IsUpgradeBlocked(card);
            bool isHovered = cardRect.Contains(mousePos) && !isBlocked;

            // Animate / shift hovered card slightly
            Rectangle drawRect = isHovered
                ? new Rectangle(cardRect.X - (int)(4f * uiScale), cardRect.Y - (int)(8f * uiScale), cardRect.Width + (int)(8f * uiScale), cardRect.Height + (int)(10f * uiScale))
                : cardRect;

            // Colors based on Rarity
            Color rarityColor = card.Rarity switch
            {
                CardRarity.Rare => new Color(40, 175, 245),
                CardRarity.Epic => new Color(205, 60, 245),
                CardRarity.Legendary => new Color(255, 215, 0),
                _               => new Color(130, 220, 110)
            };

            if (isBlocked)
            {
                rarityColor = new Color(110, 115, 125);
            }

            Color cardBg = isBlocked ? new Color(18, 18, 22, 245) : (isHovered ? new Color(28, 32, 44, 255) : new Color(20, 22, 30, 245));
            Color cardBorder = isBlocked ? new Color(60, 60, 70) : (isHovered ? Color.White : rarityColor);

            DrawPanel(drawRect, cardBg, cardBorder, Math.Max(2, (int)(3f * uiScale)));

            // Card Header Rarity Badge
            int headerH = (int)(44f * uiScale);
            Rectangle headerRect = new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, headerH);
            Color headerBg = isBlocked ? new Color(30, 30, 36) : (rarityColor * 0.25f);
            _spriteBatch.Draw(_pixel, headerRect, headerBg);
            _spriteBatch.Draw(_pixel, new Rectangle(drawRect.X, drawRect.Y + headerH, drawRect.Width, 2), rarityColor * 0.8f);

            string rarityStr = card.Rarity.ToString().ToUpper();
            float rarityScale = MathHelper.Clamp(uiScale * 0.85f, 0.6f, 1.15f);
            DrawCenteredText(rarityStr, headerRect, rarityColor, rarityScale);

            // Card Title
            int titleY = drawRect.Y + headerH + (int)(16f * uiScale);
            float cardTitleScale = MathHelper.Clamp(uiScale * 1.05f, 0.75f, 1.45f);
            Color titleColor = isBlocked ? new Color(125, 125, 135) : Color.White;
            DrawCenteredText(card.Title, new Rectangle(drawRect.X + (int)(12f * uiScale), titleY, drawRect.Width - (int)(24f * uiScale), (int)(32f * uiScale)), titleColor, cardTitleScale);

            // Divider Line
            int dividerY = titleY + (int)(38f * uiScale);
            _spriteBatch.Draw(_pixel, new Rectangle(drawRect.X + (int)(20f * uiScale), dividerY, drawRect.Width - (int)(40f * uiScale), 1), isBlocked ? new Color(50, 50, 60) : new Color(60, 70, 95));

            // Card Description
            int descY = dividerY + (int)(16f * uiScale);
            float descScale = MathHelper.Clamp(uiScale * 0.82f, 0.58f, 1.1f);
            Color descColor = isBlocked ? new Color(110, 110, 120) : new Color(205, 215, 230);

            var lines = WrapText(card.Description, drawRect.Width - (int)(36f * uiScale), descScale);
            int lineSpacing = (int)(24f * uiScale);
            for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
            {
                DrawText(lines[lineIdx], new Vector2(drawRect.X + (int)(18f * uiScale), descY + lineIdx * lineSpacing), descColor, descScale);
            }

            // Exclusivity warning
            if (card.MutuallyExclusiveWith != UpgradeTags.None)
            {
                int exY = descY + lines.Count * lineSpacing + (int)(8f * uiScale);
                
                // Automatycznie zamienia "Boomerang, Explosive" na "MUTUALLY EXCLUSIVE: [BOOMERANG], [EXPLOSIVE]"
                string exText = $"MUTUALLY EXCLUSIVE:\n[{card.MutuallyExclusiveWith.ToString().Replace(", ", "], [").ToUpper()}]";

                var exLines = WrapText(exText, drawRect.Width - (int)(36f * uiScale), descScale * 0.85f);
                int exLineSpacing = (int)(18f * uiScale);
                for (int l = 0; l < exLines.Count; l++)
                {
                    DrawText(exLines[l], new Vector2(drawRect.X + (int)(18f * uiScale), exY + l * exLineSpacing), new Color(255, 180, 80), descScale * 0.85f);
                }
            }

            // Card Bottom Action Prompt or Locked Badge
            if (isBlocked)
            {
                _spriteBatch.Draw(_pixel, drawRect, Color.Black * 0.45f);

                int badgeH = (int)(48f * uiScale);
                int badgeY = drawRect.Bottom - badgeH - (int)(16f * uiScale);
                Rectangle badgeRect = new Rectangle(drawRect.X + (int)(14f * uiScale), badgeY, drawRect.Width - (int)(28f * uiScale), badgeH);

                DrawPanel(badgeRect, new Color(55, 14, 18, 245), new Color(215, 38, 48), Math.Max(2, (int)(2f * uiScale)));

                string badgeText = "BLOCKED: EXCLUSIVE";
                float badgeScale = MathHelper.Clamp(uiScale * 0.78f, 0.52f, 1.1f);
                DrawCenteredText(badgeText, badgeRect, new Color(255, 90, 90), badgeScale);
            }
            else
            {
                int buttonH = (int)(44f * uiScale);
                int buttonY = drawRect.Bottom - buttonH - (int)(16f * uiScale);
                Rectangle buttonRect = new Rectangle(drawRect.X + (int)(14f * uiScale), buttonY, drawRect.Width - (int)(28f * uiScale), buttonH);

                if (isHovered)
                {
                    DrawPanel(buttonRect, new Color(45, 190, 230), Color.White, Math.Max(2, (int)(2.5f * uiScale)));
                    DrawCenteredText("CLICK TO SELECT", buttonRect, new Color(15, 20, 30), MathHelper.Clamp(uiScale * 0.82f, 0.58f, 1.15f), shadow: false);
                }
                else
                {
                    DrawPanel(buttonRect, new Color(22, 26, 34), new Color(60, 70, 90), Math.Max(1, (int)(1.5f * uiScale)));
                    DrawCenteredText($"PRESS [{i + 1}] OR CLICK", buttonRect, new Color(175, 185, 200), MathHelper.Clamp(uiScale * 0.78f, 0.52f, 1.1f));
                }
            }
        }
    }

    private void DrawGameOverScreen(int screenWidth, int screenHeight, float uiScale, MouseState mouseState)
    {
        // Backdrop
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), new Color(12, 4, 6) * 0.90f);

        int modalWidth = (int)(560f * uiScale);
        int modalHeight = (int)(430f * uiScale);
        int modalX = (screenWidth - modalWidth) / 2;
        int modalY = (screenHeight - modalHeight) / 2;

        Rectangle modalRect = new Rectangle(modalX, modalY, modalWidth, modalHeight);
        DrawPanel(modalRect, new Color(22, 16, 22, 250), new Color(220, 32, 48), Math.Max(2, (int)(3.5f * uiScale)));

        // Header
        string title = "GAME OVER";
        float titleScale = MathHelper.Clamp(uiScale * 1.9f, 1.3f, 2.9f);
        Vector2 titleSize = MeasureText(title, titleScale);
        DrawText(title, new Vector2(modalX + (modalWidth - titleSize.X) / 2f, modalY + (int)(22f * uiScale)), new Color(245, 40, 50), titleScale);

        string sub = _isEndlessMode ? "YOUR RUN HAS ENDED (ENDLESS MODE)" : "YOUR RUN HAS ENDED";
        float subScale = MathHelper.Clamp(uiScale * 0.88f, 0.6f, 1.3f);
        Vector2 subSize = MeasureText(sub, subScale);
        DrawText(sub, new Vector2(modalX + (modalWidth - subSize.X) / 2f, modalY + (int)(74f * uiScale)), new Color(190, 180, 190), subScale);

        // Stats Box
        int statsBoxY = modalY + (int)(112f * uiScale);
        int statsBoxH = (int)(180f * uiScale);
        Rectangle statsRect = new Rectangle(modalX + (int)(24f * uiScale), statsBoxY, modalWidth - (int)(48f * uiScale), statsBoxH);
        DrawPanel(statsRect, new Color(14, 10, 14, 230), new Color(65, 48, 58), Math.Max(1, (int)(1.5f * uiScale)));

        int minutes = (int)(_survivalTime / 60f);
        int seconds = (int)(_survivalTime % 60f);
        float statScale = MathHelper.Clamp(uiScale * 0.88f, 0.6f, 1.3f);
        int statPad = (int)(20f * uiScale);
        int lineH = (int)(36f * uiScale);

        DrawText("SURVIVAL TIME:", new Vector2(statsRect.X + statPad, statsRect.Y + (int)(18f * uiScale)), new Color(195, 190, 200), statScale);
        DrawText($"{minutes:D2}:{seconds:D2}", new Vector2(statsRect.Right - statPad - MeasureText($"{minutes:D2}:{seconds:D2}", statScale).X, statsRect.Y + (int)(18f * uiScale)), new Color(255, 215, 60), statScale);

        DrawText("PLAYER LEVEL:", new Vector2(statsRect.X + statPad, statsRect.Y + (int)(18f * uiScale) + lineH), new Color(195, 190, 200), statScale);
        DrawText($"LVL {_player.Level}", new Vector2(statsRect.Right - statPad - MeasureText($"LVL {_player.Level}", statScale).X, statsRect.Y + (int)(18f * uiScale) + lineH), new Color(120, 210, 255), statScale);

        DrawText("BOSSES DEFEATED:", new Vector2(statsRect.X + statPad, statsRect.Y + (int)(18f * uiScale) + lineH * 2), new Color(195, 190, 200), statScale);
        string bossText = _isEndlessMode ? $"{_bossesDefeatedCount}" : $"{_bossesDefeatedCount} / 5";
        DrawText(bossText, new Vector2(statsRect.Right - statPad - MeasureText(bossText, statScale).X, statsRect.Y + (int)(18f * uiScale) + lineH * 2), new Color(255, 100, 110), statScale);

        DrawText("UPGRADES ACQUIRED:", new Vector2(statsRect.X + statPad, statsRect.Y + (int)(18f * uiScale) + lineH * 3), new Color(195, 190, 200), statScale);
        DrawText($"{_acquiredUpgrades.Count}", new Vector2(statsRect.Right - statPad - MeasureText($"{_acquiredUpgrades.Count}", statScale).X, statsRect.Y + (int)(18f * uiScale) + lineH * 3), new Color(120, 230, 150), statScale);

        // Buttons
        int btnW = (int)(240f * uiScale);
        int btnH = (int)(54f * uiScale);
        int btnGap = (int)(20f * uiScale);
        int totalBtnW = btnW * 2 + btnGap;
        int startBtnX = modalX + (modalWidth - totalBtnW) / 2;
        int btnY = modalY + modalHeight - (int)(72f * uiScale);

        Rectangle restartBtnRect = new Rectangle(startBtnX, btnY, btnW, btnH);
        Rectangle menuBtnRect = new Rectangle(startBtnX + btnW + btnGap, btnY, btnW, btnH);

        Point mousePos = new Point(mouseState.X, mouseState.Y);

        DrawMenuButton("RESTART [R]", restartBtnRect, restartBtnRect.Contains(mousePos), new Color(240, 60, 70), uiScale);
        DrawMenuButton("MAIN MENU [M]", menuBtnRect, menuBtnRect.Contains(mousePos), new Color(180, 190, 205), uiScale);
    }

    private void DrawYouWinScreen(int screenWidth, int screenHeight, float uiScale, MouseState mouseState)
    {
        // Backdrop
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), new Color(10, 14, 24) * 0.90f);

        int modalWidth = (int)(560f * uiScale);
        int modalHeight = (int)(430f * uiScale);
        int modalX = (screenWidth - modalWidth) / 2;
        int modalY = (screenHeight - modalHeight) / 2;

        Rectangle modalRect = new Rectangle(modalX, modalY, modalWidth, modalHeight);
        DrawPanel(modalRect, new Color(18, 22, 34, 250), new Color(255, 210, 50), Math.Max(2, (int)(3.5f * uiScale)));

        // Header
        string title = "VICTORY!";
        float titleScale = MathHelper.Clamp(uiScale * 1.9f, 1.3f, 2.9f);
        Vector2 titleSize = MeasureText(title, titleScale);
        DrawText(title, new Vector2(modalX + (modalWidth - titleSize.X) / 2f, modalY + (int)(22f * uiScale)), new Color(255, 215, 50), titleScale);

        string sub = "ALL 5 BOSSES DEFEATED - ARENA CONQUERED!";
        float subScale = MathHelper.Clamp(uiScale * 0.88f, 0.6f, 1.3f);
        Vector2 subSize = MeasureText(sub, subScale);
        DrawText(sub, new Vector2(modalX + (modalWidth - subSize.X) / 2f, modalY + (int)(74f * uiScale)), new Color(140, 220, 255), subScale);

        // Stats Box
        int statsBoxY = modalY + (int)(112f * uiScale);
        int statsBoxH = (int)(180f * uiScale);
        Rectangle statsRect = new Rectangle(modalX + (int)(24f * uiScale), statsBoxY, modalWidth - (int)(48f * uiScale), statsBoxH);
        DrawPanel(statsRect, new Color(12, 14, 24, 230), new Color(48, 58, 85), Math.Max(1, (int)(1.5f * uiScale)));

        int minutes = (int)(_survivalTime / 60f);
        int seconds = (int)(_survivalTime % 60f);
        float statScale = MathHelper.Clamp(uiScale * 0.88f, 0.6f, 1.3f);
        int statPad = (int)(20f * uiScale);
        int lineH = (int)(36f * uiScale);

        DrawText("TOTAL CLEAR TIME:", new Vector2(statsRect.X + statPad, statsRect.Y + (int)(18f * uiScale)), new Color(195, 200, 215), statScale);
        DrawText($"{minutes:D2}:{seconds:D2}", new Vector2(statsRect.Right - statPad - MeasureText($"{minutes:D2}:{seconds:D2}", statScale).X, statsRect.Y + (int)(18f * uiScale)), new Color(255, 215, 60), statScale);

        DrawText("FINAL PLAYER LEVEL:", new Vector2(statsRect.X + statPad, statsRect.Y + (int)(18f * uiScale) + lineH), new Color(195, 200, 215), statScale);
        DrawText($"LVL {_player.Level}", new Vector2(statsRect.Right - statPad - MeasureText($"LVL {_player.Level}", statScale).X, statsRect.Y + (int)(18f * uiScale) + lineH), new Color(120, 210, 255), statScale);

        DrawText("BOSSES DEFEATED:", new Vector2(statsRect.X + statPad, statsRect.Y + (int)(18f * uiScale) + lineH * 2), new Color(195, 200, 215), statScale);
        DrawText("5 / 5 (100%)", new Vector2(statsRect.Right - statPad - MeasureText("5 / 5 (100%)", statScale).X, statsRect.Y + (int)(18f * uiScale) + lineH * 2), new Color(100, 255, 140), statScale);

        DrawText("TOTAL UPGRADES:", new Vector2(statsRect.X + statPad, statsRect.Y + (int)(18f * uiScale) + lineH * 3), new Color(195, 200, 215), statScale);
        DrawText($"{_acquiredUpgrades.Count}", new Vector2(statsRect.Right - statPad - MeasureText($"{_acquiredUpgrades.Count}", statScale).X, statsRect.Y + (int)(18f * uiScale) + lineH * 3), new Color(255, 140, 240), statScale);

        // Buttons
        int btnW = (int)(240f * uiScale);
        int btnH = (int)(54f * uiScale);
        int btnGap = (int)(20f * uiScale);
        int totalBtnW = btnW * 2 + btnGap;
        int startBtnX = modalX + (modalWidth - totalBtnW) / 2;
        int btnY = modalY + modalHeight - (int)(72f * uiScale);

        Rectangle restartBtnRect = new Rectangle(startBtnX, btnY, btnW, btnH);
        Rectangle menuBtnRect = new Rectangle(startBtnX + btnW + btnGap, btnY, btnW, btnH);

        Point mousePos = new Point(mouseState.X, mouseState.Y);

        DrawMenuButton("PLAY AGAIN [R]", restartBtnRect, restartBtnRect.Contains(mousePos), new Color(255, 215, 50), uiScale);
        DrawMenuButton("MAIN MENU [M]", menuBtnRect, menuBtnRect.Contains(mousePos), new Color(180, 190, 205), uiScale);
    }

    private void DrawPanel(Rectangle rect, Color bgColor, Color borderColor, int borderThickness = 2)
    {
        _spriteBatch.Draw(_pixel, rect, bgColor);

        if (borderThickness > 0)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, borderThickness), borderColor);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, borderThickness, rect.Height), borderColor);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - borderThickness, rect.Y, borderThickness, rect.Height), borderColor);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - borderThickness, rect.Width, borderThickness), borderColor);
        }
    }

    private void DrawProgressBar(Rectangle rect, float progress, Color fillColor, Color bgColor, Color borderColor, Color highlightColor, float uiScale)
    {
        DrawPanel(rect, bgColor, borderColor, Math.Max(1, (int)(1.5f * uiScale)));

        int innerX = rect.X + 1;
        int innerY = rect.Y + 1;
        int innerW = Math.Max(0, rect.Width - 2);
        int innerH = Math.Max(0, rect.Height - 2);

        int fillW = (int)(innerW * MathHelper.Clamp(progress, 0f, 1f));
        if (fillW > 0)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(innerX, innerY, fillW, innerH), fillColor);
            int hlHeight = Math.Max(2, (int)(3f * uiScale));
            _spriteBatch.Draw(_pixel, new Rectangle(innerX, innerY, fillW, hlHeight), highlightColor);
        }
    }

    private Vector2 MeasureText(string text, float scale = 1f)
    {
        if (string.IsNullOrEmpty(text)) return Vector2.Zero;

        if (_font != null)
        {
            return _font.MeasureString(text) * scale;
        }
        
        return Vector2.Zero;
    }

    private void DrawText(string text, Vector2 pos, Color color, float scale = 1f, bool shadow = true)
    {
        if (string.IsNullOrEmpty(text) || _font == null) return;

        if (shadow)
        {
            float shadowOffset = Math.Max(1f, scale * 1.5f);
            _spriteBatch.DrawString(_font, text, pos + new Vector2(shadowOffset, shadowOffset), Color.Black * 0.85f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        _spriteBatch.DrawString(_font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawCenteredText(string text, Rectangle bounds, Color color, float scale = 1f, bool shadow = true)
    {
        Vector2 size = MeasureText(text, scale);
        Vector2 pos = new Vector2(bounds.X + (bounds.Width - size.X) / 2f, bounds.Y + (bounds.Height - size.Y) / 2f);
        DrawText(text, pos, color, scale, shadow);
    }

    private List<string> WrapText(string text, float maxLineWidth, float scale)
    {
        List<string> result = new();
        if (string.IsNullOrEmpty(text)) return result;

        string[] paragraphs = text.Split('\n');
        foreach (var paragraph in paragraphs)
        {
            string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                result.Add("");
                continue;
            }

            string currentLine = words[0];
            for (int i = 1; i < words.Length; i++)
            {
                string testLine = currentLine + " " + words[i];
                if (MeasureText(testLine, scale).X <= maxLineWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    result.Add(currentLine);
                    currentLine = words[i];
                }
            }
            result.Add(currentLine);
        }
        return result;
    }

    private void DrawRect(Vector2 pos, int width, int height, Color color)
    {
        Rectangle rect = new Rectangle((int)(pos.X - width / 2), (int)(pos.Y - height / 2), width, height);
        _spriteBatch.Draw(_pixel, rect, color);
    }

    private void DrawEnemyEntity(Enemy enemy, Color baseCol, GameTime gameTime)
    {
        Color col = baseCol;
        if (enemy.KnockbackTimer > 0f)
        {
            col = Color.Lerp(col, Color.Yellow, 0.6f);
        }
        else if (enemy.IsFrozen)
        {
            col = Color.Lerp(col, new Color(100, 200, 255), 0.65f);
        }
        else if (enemy.IsBurning)
        {
            float pulse = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 12.0) * 0.5 + 0.5);
            col = Color.Lerp(col, new Color(255, 95, 20), 0.45f + 0.3f * pulse);
        }

        // Rysuj animację sprite — wybierz na podstawie stanu wroga
        SpriteAnimation? currentAnimation;
        if (enemy.State == EnemyState.Dead)
        {
            currentAnimation = enemy.DeathAnimation;
        }
        else if (enemy.State == EnemyState.Attack)
        {
            currentAnimation = enemy.AttackAnimation;
        }
        else if (enemy.KnockbackTimer > 0f)
        {
            currentAnimation = enemy.HurtAnimation;
        }
        else
        {
            currentAnimation = enemy.WalkAnimation;
        }

        if (currentAnimation != null)
        {
            Vector2 origin = new Vector2(currentAnimation.FrameWidth / 2f, currentAnimation.FrameHeight / 2f);
            float scale = enemy is TankEnemy ? 2.7f : 2.4f;

            _spriteBatch.Draw(
                currentAnimation.Texture,
                enemy.Position,
                currentAnimation.SourceRectangle,
                col,
                0f,
                origin,
                scale,
                enemy.CurrentSpriteEffects,
                0f
            );
        }
        else
        {
            // Fallback do prostokąta jeśli animacja nie jest zainicjalizowana
            DrawRect(enemy.Position, (int)enemy.Size, (int)enemy.Size, col);
        }

        // Freeze visuals: ice crystals at corners
        if (enemy.IsFrozen)
        {
            int frostSize = Math.Max(3, (int)(enemy.Size * 0.22f));
            float half = enemy.Size * 0.5f;
            DrawRect(new Vector2(enemy.Position.X - half + frostSize * 0.5f, enemy.Position.Y - half + frostSize * 0.5f), frostSize, frostSize, new Color(180, 240, 255));
            DrawRect(new Vector2(enemy.Position.X + half - frostSize * 0.5f, enemy.Position.Y + half - frostSize * 0.5f), frostSize, frostSize, new Color(180, 240, 255));
            DrawRect(new Vector2(enemy.Position.X - half + frostSize * 0.5f, enemy.Position.Y + half - frostSize * 0.5f), frostSize, frostSize, new Color(140, 220, 255));
            DrawRect(new Vector2(enemy.Position.X + half - frostSize * 0.5f, enemy.Position.Y - half + frostSize * 0.5f), frostSize, frostSize, new Color(140, 220, 255));
        }

        // Burn animation: flickering animated flames dancing on top of enemy
        if (enemy.IsBurning)
        {
            float t = (float)gameTime.TotalGameTime.TotalSeconds * 14f;
            float half = enemy.Size * 0.5f;

            // Flame 1 (left)
            float f1Y = (float)Math.Sin(t + enemy.Position.X * 0.1f) * 3.5f - half - 3f;
            DrawRect(new Vector2(enemy.Position.X - half * 0.45f, enemy.Position.Y + f1Y), 4, 6, new Color(255, 110, 15));
            DrawRect(new Vector2(enemy.Position.X - half * 0.45f, enemy.Position.Y + f1Y - 1f), 2, 3, new Color(255, 220, 60));

            // Flame 2 (center - main tallest flame)
            float f2Y = (float)Math.Sin(t * 1.3f + 1.2f + enemy.Position.Y * 0.1f) * 4.5f - half - 5f;
            DrawRect(new Vector2(enemy.Position.X, enemy.Position.Y + f2Y), 5, 8, new Color(255, 160, 20));
            DrawRect(new Vector2(enemy.Position.X, enemy.Position.Y + f2Y - 2f), 3, 4, new Color(255, 245, 90));

            // Flame 3 (right)
            float f3Y = (float)Math.Sin(t * 0.9f + 2.5f + enemy.Position.X * 0.05f) * 3.5f - half - 3f;
            DrawRect(new Vector2(enemy.Position.X + half * 0.45f, enemy.Position.Y + f3Y), 4, 6, new Color(255, 80, 10));
            DrawRect(new Vector2(enemy.Position.X + half * 0.45f, enemy.Position.Y + f3Y - 1f), 2, 3, new Color(255, 200, 40));
        }
    }

    private Color GetEnemyColor(Enemy enemy)
    {
        float t = MathHelper.Clamp((enemy.Size - 20f) / (48f - 20f), 0f, 1f);
        byte r = (byte)MathHelper.Lerp(255f, 110f, t);
        byte g = (byte)MathHelper.Lerp(70f, 0f, t);
        byte b = (byte)MathHelper.Lerp(70f, 0f, t);
        return new Color(r, g, b);
    }

    private string FormatNumber(float val)
    {
        if (val >= 1000f)
        {
            return $"{(val / 1000f):0.#}K";
        }
        return ((int)Math.Round(val)).ToString();
    }

    private void DrawDeathEffects()
    {
        // Draw death rings (okragły błysk)
        var rings = _deathRingPool.Items;
        for (int i = 0; i < rings.Count; i++)
        {
            var ring = rings[i];
            if (!ring.IsActive) continue;

            float progress = ring.Lifetime / ring.MaxLifetime;
            float alpha = 1f - progress;
            Color col = ring.Color * alpha;

            // Rysuje obręcz jako szereg pikseli tworzących okrąg (pixel-art style)
            int circumference = Math.Max(8, (int)(ring.Radius * 2));
            int pixelCount = circumference * 2;

            for (int p = 0; p < pixelCount; p++)
            {
                float angle = p * (MathHelper.TwoPi / pixelCount);
                float px = ring.Position.X + (float)Math.Cos(angle) * ring.Radius;
                float py = ring.Position.Y + (float)Math.Sin(angle) * ring.Radius;
                DrawRect(new Vector2(px, py), 3, 3, col);
            }
        }

        // Draw death particles (rozpad na piksele)
        var particles = _deathParticlePool.Items;
        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (!p.IsActive) continue;

            float progress = p.Lifetime / p.MaxLifetime;
            float alpha = 1f - progress;
            Color col = p.Color * alpha;

            DrawRect(p.Position, p.Size, p.Size, col);
        }
    }

    private void DrawExplosionEffects()
    {
        // Draw explosion rings
        var rings = _explosionRingPool.Items;
        for (int i = 0; i < rings.Count; i++)
        {
            var ring = rings[i];
            if (!ring.IsActive) continue;

            float progress = ring.Lifetime / ring.MaxLifetime;
            float alpha = 1f - progress;
            Color col = ring.Color * alpha;

            int circumference = Math.Max(8, (int)(ring.Radius * 2));
            int pixelCount = circumference * 2;

            for (int p = 0; p < pixelCount; p++)
            {
                float angle = p * (MathHelper.TwoPi / pixelCount);
                float px = ring.Position.X + (float)Math.Cos(angle) * ring.Radius;
                float py = ring.Position.Y + (float)Math.Sin(angle) * ring.Radius;
                DrawRect(new Vector2(px, py), 4, 4, col);
            }
        }

        // Draw explosion particles
        var particles = _explosionParticlePool.Items;
        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (!p.IsActive) continue;

            float progress = p.Lifetime / p.MaxLifetime;
            float alpha = 1f - progress;
            Color col = p.Color * alpha;

            DrawRect(p.Position, p.Size, p.Size, col);
        }
    }

    private void DrawPauseMenuScreen(int screenWidth, int screenHeight, float uiScale, MouseState mouseState)
    {
        // Półprzezroczyste tło przyciemniające grę
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.7f);

        int panelW = (int)(400f * uiScale);
        int panelH = (int)(320f * uiScale);
        int panelX = (screenWidth - panelW) / 2;
        int panelY = (screenHeight - panelH) / 2;

        Rectangle panelRect = new Rectangle(panelX, panelY, panelW, panelH);
        DrawPanel(panelRect, new Color(20, 22, 30, 245), new Color(55, 65, 85), Math.Max(2, (int)(3f * uiScale)));

        // Tytuł
        string title = "PAUSED";
        float titleScale = MathHelper.Clamp(uiScale * 1.6f, 1.1f, 2.5f);
        Vector2 titleSize = MeasureText(title, titleScale);
        DrawText(title, new Vector2(panelX + (panelW - (int)titleSize.X) / 2f, panelY + (int)(20f * uiScale)), new Color(255, 215, 60), titleScale);

        Point mousePos = new Point(mouseState.X, mouseState.Y);

        int btnW = (int)(320f * uiScale);
        int btnH = (int)(56f * uiScale);
        int btnGap = (int)(14f * uiScale);
        int startBtnY = panelY + (int)(80f * uiScale);

        Rectangle resumeRect = new Rectangle(panelX + (panelW - btnW) / 2, startBtnY, btnW, btnH);
        Rectangle settingsRect = new Rectangle(panelX + (panelW - btnW) / 2, startBtnY + btnH + btnGap, btnW, btnH);
        Rectangle mainMenuRect = new Rectangle(panelX + (panelW - btnW) / 2, startBtnY + (btnH + btnGap) * 2, btnW, btnH);

        bool resumeHover = resumeRect.Contains(mousePos);
        bool settingsHover = settingsRect.Contains(mousePos);
        bool mainMenuHover = mainMenuRect.Contains(mousePos);

        DrawMenuButton("1. RESUME", resumeRect, resumeHover, new Color(40, 180, 230), uiScale);
        DrawMenuButton("2. SETTINGS", settingsRect, settingsHover, new Color(255, 195, 50), uiScale);
        DrawMenuButton("3. MAIN MENU", mainMenuRect, mainMenuHover, new Color(235, 45, 60), uiScale);

        // Podpowiedź
        string hint = "PRESS [ESC] TO RESUME";
        float hintScale = MathHelper.Clamp(uiScale * 0.7f, 0.5f, 1f);
        Vector2 hintSize = MeasureText(hint, hintScale);
        DrawText(hint, new Vector2(panelX + (panelW - (int)hintSize.X) / 2f, panelY + panelH - (int)(30f * uiScale)), new Color(120, 130, 150), hintScale);
    }
}