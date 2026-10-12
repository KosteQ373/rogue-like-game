using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rogue_like;

public class SpriteAnimation
{
    public Texture2D Texture { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public int Row { get; set; }
    public int FrameCount { get; }
    public float FrameTime { get; set; }
    public bool IsLooping { get; set; }
    /// <summary>Gdy true, klatki odtwarzają się od końca do początku.</summary>
    public bool PlayBackwards { get; set; } = false;

    private int _currentFrame;
    private float _elapsedTime;
    private bool _isFinished;

    // Dostęp do wewnętrznego stanu dla maszyny stanów wroga
    public int CurrentFrame => _currentFrame;
    public float ElapsedTime => _elapsedTime;
    /// <summary>True gdy animacja non-looping osiągnęła ostatnią klatkę.</summary>
    public bool IsFinished => _isFinished;

    public SpriteAnimation(Texture2D texture, int frameWidth, int frameHeight, int row, int frameCount, float frameTime = 0.15f, bool isLooping = true)
    {
        Texture = texture;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        Row = row;
        FrameCount = frameCount;
        FrameTime = frameTime;
        IsLooping = isLooping;
        _currentFrame = 0;
        _elapsedTime = 0f;
    }

    public void Update(GameTime gameTime)
    {
        _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        while (_elapsedTime >= FrameTime)
        {
            _elapsedTime -= FrameTime;

            if (PlayBackwards)
            {
                _currentFrame--;
                if (_currentFrame < 0)
                {
                    if (IsLooping)
                    {
                        _currentFrame = FrameCount - 1;
                        _isFinished = false;
                    }
                    else
                    {
                        _currentFrame = 0;
                        _isFinished = true;
                    }
                }
            }
            else
            {
                _currentFrame++;
                if (_currentFrame >= FrameCount)
                {
                    if (IsLooping)
                    {
                        _currentFrame = 0;
                        _isFinished = false;
                    }
                    else
                    {
                        _currentFrame = FrameCount - 1;
                        _isFinished = true;
                    }
                }
            }
        }
    }

    public Rectangle SourceRectangle => new Rectangle(
        _currentFrame * FrameWidth,
        Row * FrameHeight,
        FrameWidth,
        FrameHeight
    );

    public void Reset()
    {
        _currentFrame = 0;
        _elapsedTime = 0f;
        _isFinished = false;
    }
}
