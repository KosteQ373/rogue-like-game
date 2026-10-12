using Microsoft.Xna.Framework;
using System;

namespace rogue_like;

public class Camera
{
    private Vector2 _position;

    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    public Camera(Vector2 initialPosition)
    {
        _position = initialPosition;
    }

    public void Update(Vector2 playerPosition, float halfW, float halfH, float arenaWidth, float arenaHeight)
    {
        // Camera dead zone (120 pixels in each direction)
        float deadZone = 120f;
        Vector2 camDiff = playerPosition - _position;
        if (Math.Abs(camDiff.X) > deadZone)
        {
            _position.X += camDiff.X > 0 ? (camDiff.X - deadZone) : (camDiff.X + deadZone);
        }
        if (Math.Abs(camDiff.Y) > deadZone)
        {
            _position.Y += camDiff.Y > 0 ? (camDiff.Y - deadZone) : (camDiff.Y + deadZone);
        }

        // Clamp camera to arena bounds
        _position.X = MathHelper.Clamp(_position.X, halfW, arenaWidth - halfW);
        _position.Y = MathHelper.Clamp(_position.Y, halfH, arenaHeight - halfH);
    }

    public Matrix GetViewMatrix(float halfW, float halfH)
    {
        return Matrix.CreateTranslation(new Vector3(-_position.X + halfW, -_position.Y + halfH, 0f));
    }

    public Vector2 GetOffset(float halfW, float halfH)
    {
        return new Vector2(halfW - _position.X, halfH - _position.Y);
    }
}
