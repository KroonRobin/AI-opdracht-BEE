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

    public Bullet(Vector2 startPosition, Vector2 direction, int damage, float speed, int radius, Color color)
    {
        Position = startPosition;
        Velocity = direction * speed;
        Damage = damage;
        Radius = radius;
        _color = color;
    }

    public void Update(GameTime gameTime)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * delta;

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
        var rect = new Rectangle(
            (int)(Position.X - Radius),
            (int)(Position.Y - Radius),
            Radius * 2,
            Radius * 2);

        spriteBatch.Draw(pixel, rect, _color);
    }
}