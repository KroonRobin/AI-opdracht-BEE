using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AI_opdracht_BEE.Core;

namespace AI_opdracht_BEE.Entities;

public class Bullet
{
    public Vector2 Position;
    public Vector2 Velocity;
    public int Damage { get; }
    public int Radius { get; }
    public bool IsActive { get; private set; } = true;

    private readonly Color _color;
    private readonly Texture2D _texture;
    private readonly float _visualScale;
    private readonly float _spinSpeed;
    private readonly float _rotationOffset;
    private float _currentRotation;

    public Bullet(
        Vector2 startPosition, Vector2 direction, int damage, float speed, int radius,
        Color color, Texture2D texture, float visualScale = 1f,
        float spinSpeed = 0f, float rotationOffset = 0f)
    {
        Position = startPosition;
        Velocity = direction * speed;
        Damage = damage;
        Radius = radius;
        _color = color;
        _texture = texture;
        _visualScale = visualScale;
        _spinSpeed = spinSpeed;
        _rotationOffset = rotationOffset;
    }

    public void Update(GameTime gameTime)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * delta;

        if (_spinSpeed != 0f)
            _currentRotation += _spinSpeed * delta;

        bool offScreen =
            Position.X < -Radius ||
            Position.X > GameConstants.RoomWidth + Radius ||
            Position.Y < -Radius ||
            Position.Y > GameConstants.RoomHeight + Radius;

        if (offScreen)
            IsActive = false;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (_texture != null)
        {
            float spriteRotation;

            if (_spinSpeed != 0f)
            {
                spriteRotation = _currentRotation;
            }
            else
            {
                float velocityAngle = (float)System.Math.Atan2(Velocity.Y, Velocity.X);
                spriteRotation = velocityAngle - _rotationOffset;
            }

            Vector2 origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);

            spriteBatch.Draw(
                _texture,
                Position,
                null,
                _color,
                spriteRotation,
                origin,
                _visualScale,
                SpriteEffects.None,
                0f);
        }
        else
        {
            var rect = new Rectangle(
                (int)(Position.X - Radius),
                (int)(Position.Y - Radius),
                Radius * 2,
                Radius * 2);

            spriteBatch.Draw(pixel, rect, _color);
        }
    }
}