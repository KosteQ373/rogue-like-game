using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rogue_like;

public partial class Game1
{
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGray);

        int screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
        float halfW = screenWidth / 2f;
        float halfH = screenHeight / 2f;

        // Compute camera matrix with dead zone position
        Matrix cameraTransform = _camera.GetViewMatrix(halfW, halfH);

        // 1. Draw World (with camera transform)
        _spriteBatch.Begin(transformMatrix: cameraTransform);

        // Draw 2000x2000 black arena background
        DrawRect(new Vector2(ArenaWidth / 2f, ArenaHeight / 2f), (int)ArenaWidth, (int)ArenaHeight, Color.Black);

        // Draw XP Gems
        var xps = _xpPool.Items;
        for (int i = 0; i < xps.Count; i++)
        {
            if (xps[i].IsActive)
            {
                DrawRect(xps[i].Position, 6, 6, Color.Gold);
            }
        }

        // Draw Basic Enemies
        var basics = _basicEnemyPool.Items;
        for (int i = 0; i < basics.Count; i++)
        {
            if (basics[i].IsActive)
            {
                Color baseCol = GetEnemyColor(basics[i]);
                Color col = basics[i].KnockbackTimer > 0f ? Color.Lerp(baseCol, Color.Yellow, 0.6f) : baseCol;
                DrawRect(basics[i].Position, (int)basics[i].Size, (int)basics[i].Size, col);
            }
        }

        // Draw Tank Enemies
        var tanks = _tankEnemyPool.Items;
        for (int i = 0; i < tanks.Count; i++)
        {
            if (tanks[i].IsActive)
            {
                Color baseCol = GetEnemyColor(tanks[i]);
                Color col = tanks[i].KnockbackTimer > 0f ? Color.Lerp(baseCol, Color.Yellow, 0.6f) : baseCol;
                DrawRect(tanks[i].Position, (int)tanks[i].Size, (int)tanks[i].Size, col);
            }
        }

        // Draw Fast Enemies
        var fasts = _fastEnemyPool.Items;
        for (int i = 0; i < fasts.Count; i++)
        {
            if (fasts[i].IsActive)
            {
                Color baseCol = GetEnemyColor(fasts[i]);
                Color col = fasts[i].KnockbackTimer > 0f ? Color.Lerp(baseCol, Color.Yellow, 0.6f) : baseCol;
                DrawRect(fasts[i].Position, (int)fasts[i].Size, (int)fasts[i].Size, col);
            }
        }

        // Draw Bullets
        var bullets = _bulletPool.Items;
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i].IsActive)
            {
                DrawRect(bullets[i].Position, 8, 8, Color.Yellow);
            }
        }

        // Draw Player
        Color playerColor = _player.InvulnerabilityTimer > 0f ? Color.Cyan : Color.LimeGreen;
        DrawRect(_player.Position, 28, 28, playerColor);

        _spriteBatch.End();

        // 2. Draw UI (screen space without camera transform)
        _spriteBatch.Begin();

        float maxHp = _player.MaxHealth.Value;
        float healthPct = maxHp > 0f ? MathHelper.Clamp(_player.CurrentHealth / maxHp, 0f, 1f) : 0f;

        // Player Main HUD Health Bar (Lengthened)
        int barX = 20;
        int barY = 20;
        int barWidth = 360;
        int barHeight = 22;

        _spriteBatch.Draw(_pixel, new Rectangle(barX - 2, barY - 2, barWidth + 4, barHeight + 4), new Color(15, 15, 15));
        _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barWidth, barHeight), new Color(45, 15, 15));

        int fillWidth = (int)(barWidth * healthPct);
        if (fillWidth > 0)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, fillWidth, barHeight), new Color(220, 35, 35));
            _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, fillWidth, 4), Color.White * 0.3f);
        }

        if (_font != null)
        {
            _spriteBatch.DrawString(_font, $"HP: {(int)_player.CurrentHealth}/{(int)maxHp}", new Vector2(barX + 8, barY + 3), Color.White);
            _spriteBatch.DrawString(_font, $"Level: {_player.Level}", new Vector2(barX, barY + barHeight + 8), Color.White);
        }

        // XP Bar in top right corner
        float xpMax = _player.XpToNextLevel;
        float xpPct = xpMax > 0f ? MathHelper.Clamp(_player.Xp / xpMax, 0f, 1f) : 0f;
        int xpBarWidth = 240;
        int xpBarHeight = 22;
        int xpBarX = screenWidth - 20 - xpBarWidth;
        int xpBarY = 20;

        _spriteBatch.Draw(_pixel, new Rectangle(xpBarX - 2, xpBarY - 2, xpBarWidth + 4, xpBarHeight + 4), new Color(15, 15, 15));
        _spriteBatch.Draw(_pixel, new Rectangle(xpBarX, xpBarY, xpBarWidth, xpBarHeight), new Color(45, 40, 15));

        int xpFillWidth = (int)(xpBarWidth * xpPct);
        if (xpFillWidth > 0)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(xpBarX, xpBarY, xpFillWidth, xpBarHeight), Color.Gold);
            _spriteBatch.Draw(_pixel, new Rectangle(xpBarX, xpBarY, xpFillWidth, 4), Color.White * 0.4f);
        }

        string xpText = $"{(int)_player.Xp}/{(int)xpMax}";
        DrawPixelText(xpText, new Vector2(xpBarX - 80, xpBarY + 3), Color.White, scale: 2);

        if (_font != null)
        {
            _spriteBatch.DrawString(_font, $"Level: {_player.Level}", new Vector2(xpBarX, xpBarY + xpBarHeight + 8), Color.White);
        }
        else
        {
            DrawPixelText($"LVL {_player.Level}", new Vector2(xpBarX, xpBarY + xpBarHeight + 8), Color.White, scale: 2);
        }

        if (_currentState == GameState.LevelUp)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.75f);

            string titleText = "LEVEL UP! CHOOSE AN UPGRADE";
            if (_font != null)
            {
                Vector2 titleSize = _font.MeasureString(titleText);
                _spriteBatch.DrawString(_font, titleText, new Vector2((screenWidth - titleSize.X) / 2f, 70f), Color.Gold);
            }
            else
            {
                DrawPixelText(titleText, new Vector2((screenWidth - titleText.Length * 20) / 2f, 70f), Color.Gold, scale: 2);
            }

            int totalCardsWidth = 3 * 320 + 2 * 40;
            int startCardX = (screenWidth - totalCardsWidth) / 2;

            for (int i = 0; i < _currentChoices.Count; i++)
            {
                int cardX = startCardX + i * (320 + 40);
                Rectangle cardRect = new Rectangle(cardX, 150, 320, 420);

                _spriteBatch.Draw(_pixel, cardRect, new Color(45, 45, 45));

                var card = _currentChoices[i];
                Color borderColor = card.Rarity switch
                {
                    CardRarity.Common => Color.LightGray,
                    CardRarity.Rare => Color.LimeGreen,
                    CardRarity.Epic => Color.MediumPurple,
                    _ => Color.White
                };

                _spriteBatch.Draw(_pixel, new Rectangle(cardRect.X, cardRect.Y, cardRect.Width, 3), borderColor);
                _spriteBatch.Draw(_pixel, new Rectangle(cardRect.X, cardRect.Y, 3, cardRect.Height), borderColor);
                _spriteBatch.Draw(_pixel, new Rectangle(cardRect.X + cardRect.Width - 3, cardRect.Y, 3, cardRect.Height), borderColor);
                _spriteBatch.Draw(_pixel, new Rectangle(cardRect.X, cardRect.Y + cardRect.Height - 3, cardRect.Width, 3), borderColor);

                string shortcut = $"[{i + 1}]";
                string cardTitle = card.Title.ToUpper();
                string cardDesc = card.Description.ToUpper();
                string rarityText = card.Rarity.ToString().ToUpper();

                if (_font != null)
                {
                    _spriteBatch.DrawString(_font, shortcut, new Vector2(cardRect.X + 24, cardRect.Y + 24), Color.Cyan, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, rarityText, new Vector2(cardRect.X + 160, cardRect.Y + 28), borderColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, card.Title, new Vector2(cardRect.X + 24, cardRect.Y + 70), Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, card.Description, new Vector2(cardRect.X + 24, cardRect.Y + 140), Color.LightGray, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);
                }
                else
                {
                    DrawPixelText(shortcut, new Vector2(cardRect.X + 24, cardRect.Y + 24), Color.Cyan, scale: 3);
                    DrawPixelText(rarityText, new Vector2(cardRect.X + 160, cardRect.Y + 26), borderColor, scale: 2);
                    DrawPixelText(cardTitle, new Vector2(cardRect.X + 24, cardRect.Y + 70), Color.White, scale: 3);
                    DrawPixelText(cardDesc, new Vector2(cardRect.X + 24, cardRect.Y + 140), Color.LightGray, scale: 2);
                }
            }
        }
        else if (_currentState == GameState.GameOver)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.8f);
            if (_font != null)
            {
                string goText = "GAME OVER";
                string restartText = "PRESS 'R' TO RESTART";
                Vector2 goSize = _font.MeasureString(goText);
                Vector2 resSize = _font.MeasureString(restartText);
                _spriteBatch.DrawString(_font, goText, new Vector2((screenWidth - goSize.X) / 2f, screenHeight / 2f - 40f), Color.Red, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, restartText, new Vector2((screenWidth - resSize.X) / 2f, screenHeight / 2f + 20f), Color.White, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
            }
            else
            {
                DrawPixelText("GAME OVER", new Vector2((screenWidth - 200) / 2f, screenHeight / 2f - 40f), Color.Red, scale: 4);
                DrawPixelText("PRESS R TO RESTART", new Vector2((screenWidth - 350) / 2f, screenHeight / 2f + 20f), Color.White, scale: 2);
            }
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawRect(Vector2 pos, int width, int height, Color color)
    {
        Rectangle rect = new Rectangle((int)(pos.X - width / 2), (int)(pos.Y - height / 2), width, height);
        _spriteBatch.Draw(_pixel, rect, color);
    }

    private void DrawPixelText(string text, Vector2 pos, Color color, int scale = 2)
    {
        float cursorX = pos.X;
        float cursorY = pos.Y;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
            {
                cursorX += 4 * scale;
                continue;
            }
            DrawPixelChar(c, new Vector2(cursorX, cursorY), scale, color);
            cursorX += (c == '/' ? 3 : 4) * scale;
        }
    }

    private void DrawPixelChar(char c, Vector2 pos, int scale, Color color)
    {
        bool[] pattern = c switch
        {
            '0' => new bool[] { true,true,true, true,false,true, true,false,true, true,false,true, true,true,true },
            '1' => new bool[] { false,true,false, false,true,false, false,true,false, false,true,false, false,true,false },
            '2' => new bool[] { true,true,true, false,false,true, true,true,true, true,false,false, true,true,true },
            '3' => new bool[] { true,true,true, false,false,true, true,true,true, false,false,true, true,true,true },
            '4' => new bool[] { true,false,true, true,false,true, true,true,true, false,false,true, false,false,true },
            '5' => new bool[] { true,true,true, true,false,false, true,true,true, false,false,true, true,true,true },
            '6' => new bool[] { true,true,true, true,false,false, true,true,true, true,false,true, true,true,true },
            '7' => new bool[] { true,true,true, false,false,true, false,false,true, false,false,true, false,false,true },
            '8' => new bool[] { true,true,true, true,false,true, true,true,true, true,false,true, true,true,true },
            '9' => new bool[] { true,true,true, true,false,true, true,true,true, false,false,true, true,true,true },
            'A' => new bool[] { true,true,true, true,false,true, true,true,true, true,false,true, true,false,true },
            'B' => new bool[] { true,true,false, true,false,true, true,true,false, true,false,true, true,true,false },
            'C' => new bool[] { true,true,true, true,false,false, true,false,false, true,false,false, true,true,true },
            'D' => new bool[] { true,true,false, true,false,true, true,false,true, true,false,true, true,true,false },
            'E' => new bool[] { true,true,true, true,false,false, true,true,true, true,false,false, true,true,true },
            'F' => new bool[] { true,true,true, true,false,false, true,true,true, true,false,false, true,false,false },
            'G' => new bool[] { true,true,true, true,false,false, true,false,true, true,false,true, true,true,true },
            'H' => new bool[] { true,false,true, true,false,true, true,true,true, true,false,true, true,false,true },
            'I' => new bool[] { true,true,true, false,true,false, false,true,false, false,true,false, true,true,true },
            'J' => new bool[] { false,false,true, false,false,true, false,false,true, true,false,true, true,true,true },
            'K' => new bool[] { true,false,true, true,false,true, true,true,false, true,false,true, true,false,true },
            'L' => new bool[] { true,false,false, true,false,false, true,false,false, true,false,false, true,true,true },
            'M' => new bool[] { true,false,true, true,true,true, true,false,true, true,false,true, true,false,true },
            'N' => new bool[] { true,false,true, true,true,true, true,false,true, true,false,true, true,false,true },
            'O' => new bool[] { true,true,true, true,false,true, true,false,true, true,false,true, true,true,true },
            'P' => new bool[] { true,true,true, true,false,true, true,true,true, true,false,false, true,false,false },
            'Q' => new bool[] { true,true,true, true,false,true, true,false,true, true,true,true, false,false,true },
            'R' => new bool[] { true,true,true, true,false,true, true,true,false, true,false,true, true,false,true },
            'S' => new bool[] { true,true,true, true,false,false, true,true,true, false,false,true, true,true,true },
            'T' => new bool[] { true,true,true, false,true,false, false,true,false, false,true,false, false,true,false },
            'U' => new bool[] { true,false,true, true,false,true, true,false,true, true,false,true, true,true,true },
            'V' => new bool[] { true,false,true, true,false,true, true,false,true, true,false,true, false,true,false },
            'W' => new bool[] { true,false,true, true,false,true, true,false,true, true,true,true, true,false,true },
            'X' => new bool[] { true,false,true, true,false,true, false,true,false, true,false,true, true,false,true },
            'Y' => new bool[] { true,false,true, true,false,true, false,true,false, false,true,false, false,true,false },
            'Z' => new bool[] { true,true,true, false,false,true, false,true,false, true,false,false, true,true,true },
            '/' => new bool[] { false,false,true, false,false,true, false,true,false, true,false,false, true,false,false },
            '+' => new bool[] { false,false,false, false,true,false, true,true,true, false,true,false, false,false,false },
            '-' => new bool[] { false,false,false, false,false,false, true,true,true, false,false,false, false,false,false },
            '%' => new bool[] { true,false,true, false,false,false, false,true,false, false,false,false, true,false,true },
            '[' => new bool[] { true,true,false, true,false,false, true,false,false, true,false,false, true,true,false },
            ']' => new bool[] { false,true,true, false,false,true, false,false,true, false,false,true, false,true,true },
            ':' => new bool[] { false,false,false, false,true,false, false,false,false, false,true,false, false,false,false },
            _   => new bool[] { false,false,false, false,false,false, false,false,false, false,false,false, false,false,false }
        };

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (pattern[row * 3 + col])
                {
                    Rectangle rect = new Rectangle((int)(pos.X + col * scale), (int)(pos.Y + row * scale), scale, scale);
                    _spriteBatch.Draw(_pixel, rect, color);
                }
            }
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
}
