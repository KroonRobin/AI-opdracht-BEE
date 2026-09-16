using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AI_opdracht_BEE.Core;

namespace AI_opdracht_BEE.Entities;

public enum Facing { Down, Up, Left, Right }

public class Player
{
    // Position is the player's FEET (bottom-center of the sprite).
    // This is what makes 3/4 depth sorting work later.
    public Vector2 Position;
    public Facing FacingDirection { get; private set; } = Facing.Down;
    public int Health { get; private set; } = GameConstants.PlayerMaxHealth;
    public bool IsAlive => Health > 0;
    public float MoveSpeed { get; private set; } = GameConstants.PlayerSpeed;
    public int MaxHealth { get; private set; } = GameConstants.PlayerMaxHealth;

    public Player(Vector2 startPosition)
    {
        Position = startPosition;
    }

    public void Update(GameTime gameTime, KeyboardState keyboard)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Build a direction vector from input
        Vector2 input = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) input.Y -= 1;
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) input.Y += 1;
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) input.X -= 1;
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) input.X += 1;

        if (input != Vector2.Zero)
        {
            // Normalize so diagonal movement isn't faster than straight movement.
            // Without this, holding W+D gives you ~1.41x speed.
            input.Normalize();
            Position += input * MoveSpeed * delta;

            UpdateFacing(input);
        }

        ClampToRoom();
    }

    private void UpdateFacing(Vector2 input)
    {
        // Whichever axis has more movement decides which way we face.
        if (System.Math.Abs(input.Y) > System.Math.Abs(input.X))
            FacingDirection = input.Y > 0 ? Facing.Down : Facing.Up;
        else
            FacingDirection = input.X > 0 ? Facing.Right : Facing.Left;
    }

    private void ClampToRoom()
    {
        float halfWidth = GameConstants.PlayerWidth / 2f;
        Position.X = MathHelper.Clamp(Position.X, halfWidth, GameConstants.RoomWidth - halfWidth);
        // Feet can't go above the sprite height, or below the room floor
        Position.Y = MathHelper.Clamp(Position.Y, GameConstants.PlayerHeight, GameConstants.RoomHeight);
    }

    public void TakeDamage(int amount)
    {
        Health = System.Math.Max(0, Health - amount);
    }

    public void IncreaseMoveSpeed(float amount)
    {
        MoveSpeed += amount;
    }

    public void IncreaseMaxHealth(int amount)
    {
        MaxHealth += amount;
        Health += amount;  // also heals by the same amount, so the pickup feels immediately rewarding
    }

    // The collision circle sits at the feet, not the whole sprite.
    // In 3/4 view your head shouldn't collide with things beside you.
    public Vector2 HitboxCenter => Position - new Vector2(0, GameConstants.PlayerHeight / 2f);
    public float HitboxRadius => GameConstants.PlayerHitboxRadius;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Placeholder: a rectangle drawn UPWARD from the feet position.
        var rect = new Rectangle(
            (int)(Position.X - GameConstants.PlayerWidth / 2f),
            (int)(Position.Y - GameConstants.PlayerHeight),
            GameConstants.PlayerWidth,
            GameConstants.PlayerHeight);

        spriteBatch.Draw(pixel, rect, Color.CornflowerBlue);

        // Small marker showing which way you're facing (helps while testing)
        var facingOffset = FacingDirection switch
        {
            Facing.Up => new Point(0, -6),
            Facing.Down => new Point(0, 6),
            Facing.Left => new Point(-8, 0),
            Facing.Right => new Point(8, 0),
            _ => Point.Zero
        };

        var marker = new Rectangle(
            (int)Position.X - 3 + facingOffset.X,
            (int)(Position.Y - GameConstants.PlayerHeight / 2f) - 3 + facingOffset.Y,
            6, 6);

        spriteBatch.Draw(pixel, marker, Color.White);
    }
}