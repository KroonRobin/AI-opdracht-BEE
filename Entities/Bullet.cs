using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AI_opdracht_BEE.Core;

namespace AI_opdracht_BEE.Entities;

public class Bullet
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool IsActive { get; private set; } = true;

    public Bullet(Vector2 startPosition, Vector2 direction)
    {
        Position = startPosition;
        Velocity = direction * GameConstants.BulletSpeed;
    }

    public void Update(GameTime gameTime)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * delta;

        bool offScreen =
            Position.X < -GameConstants.BulletRadius ||
            Position.X > GameConstants.RoomWidth + GameConstants.BulletRadius ||
            Position.Y < -GameConstants.BulletRadius ||
            Position.Y > GameConstants.RoomHeight + GameConstants.BulletRadius;

        if (offScreen)
            IsActive = false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var rect = new Rectangle(
            (int)(Position.X - GameConstants.BulletRadius),
            (int)(Position.Y - GameConstants.BulletRadius),
            GameConstants.BulletRadius * 2,
            GameConstants.BulletRadius * 2);

        spriteBatch.Draw(pixel, rect, Color.Gold);
    }
}