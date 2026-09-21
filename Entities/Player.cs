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
    public bool IsAtMaxSpeed => MoveSpeed >= GameConstants.PlayerMaxSpeedCap;
    public int MaxHealth { get; private set; } = GameConstants.PlayerMaxHealth;

    private int _currentFrame;
    private float _animationTimer;

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

        bool isMoving = input != Vector2.Zero;

        if (isMoving)
        {
            input.Normalize();
            Position += input * MoveSpeed * delta;
        }

        ClampToRoom();
        UpdateAnimation(delta, isMoving);
    }

    private void UpdateAnimation(float delta, bool isMoving)
    {
        if (!isMoving)
        {
            _currentFrame = 0;
            _animationTimer = 0f;
            return;
        }

        _animationTimer += delta;

        if (_animationTimer >= GameConstants.PlayerAnimationFrameDuration)
        {
            _animationTimer -= GameConstants.PlayerAnimationFrameDuration;
            _currentFrame = (_currentFrame + 1) % GameConstants.PlayerAnimationFrameCount;
        }
    }

    private void UpdateFacing(Vector2 input)
    {
        // Whichever axis has more movement decides which way we face.
        if (System.Math.Abs(input.Y) > System.Math.Abs(input.X))
            FacingDirection = input.Y > 0 ? Facing.Down : Facing.Up;
        else
            FacingDirection = input.X > 0 ? Facing.Right : Facing.Left;
    }

    public void FaceTowardMouse(Vector2 mouseWorldPosition)
    {
        Vector2 aimOrigin = Position - new Vector2(0, GameConstants.PlayerHeight / 2f);
        Vector2 direction = mouseWorldPosition - aimOrigin;

        if (direction == Vector2.Zero)
            return;

        if (System.Math.Abs(direction.Y) > System.Math.Abs(direction.X))
            FacingDirection = direction.Y > 0 ? Facing.Down : Facing.Up;
        else
            FacingDirection = direction.X > 0 ? Facing.Right : Facing.Left;
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
        MoveSpeed = System.Math.Min(MoveSpeed + amount, GameConstants.PlayerMaxSpeedCap);
    }

    public void IncreaseMaxHealth(int amount)
    {
        MaxHealth += amount;
        Health += amount;  // also heals by the same amount, so the pickup feels immediately rewarding
    }

    public void Heal(int amount)
    {
        Health = System.Math.Min(Health + amount, MaxHealth);
    }

    // The collision circle sits at the feet, not the whole sprite.
    // In 3/4 view your head shouldn't collide with things beside you.
    public Vector2 HitboxCenter => Position - new Vector2(0, GameConstants.PlayerHeight / 2f);
    public float HitboxRadius => GameConstants.PlayerHitboxRadius;

    public void Draw(SpriteBatch spriteBatch, Texture2D frontTexture, Texture2D backTexture, Texture2D sideTexture, Texture2D pixel)
    {
        Texture2D texture;
        SpriteEffects effects = SpriteEffects.None;

        switch (FacingDirection)
        {
            case Facing.Down:
                texture = frontTexture;
                break;

            case Facing.Up:
                texture = backTexture;
                break;

            case Facing.Left:
                texture = sideTexture;
                effects = SpriteEffects.FlipHorizontally;
                break;

            default: // Right
                texture = sideTexture;
                break;
        }

        if (texture == null)
        {
            var fallbackRect = new Rectangle(
                (int)(Position.X - GameConstants.PlayerWidth / 2f),
                (int)(Position.Y - GameConstants.PlayerHeight),
                GameConstants.PlayerWidth,
                GameConstants.PlayerHeight);

            spriteBatch.Draw(pixel, fallbackRect, Color.CornflowerBlue);
            return;
        }

        var sourceRect = new Rectangle(
            _currentFrame * GameConstants.PlayerSpriteFrameWidth,
            0,
            GameConstants.PlayerSpriteFrameWidth,
            GameConstants.PlayerSpriteFrameHeight);

        var destRect = new Rectangle(
            (int)(Position.X - GameConstants.PlayerSpriteFrameWidth * GameConstants.PlayerSpriteScale / 2f),
            (int)(Position.Y - GameConstants.PlayerSpriteFrameHeight * GameConstants.PlayerSpriteScale),
            (int)(GameConstants.PlayerSpriteFrameWidth * GameConstants.PlayerSpriteScale),
            (int)(GameConstants.PlayerSpriteFrameHeight * GameConstants.PlayerSpriteScale));

        spriteBatch.Draw(texture, destRect, sourceRect, Color.White, 0f, Vector2.Zero, effects, 0f);
    }
}