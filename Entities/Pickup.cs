using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AI_opdracht_BEE.Core;

namespace AI_opdracht_BEE.Entities;

public class Pickup
{
    public Vector2 Position { get; }
    public UpgradeType Type { get; }
    public bool IsActive { get; private set; } = true;

    private float _lifeTimer;

    public Pickup(Vector2 position, UpgradeType type)
    {
        Position = position;
        Type = type;
        _lifeTimer = GameConstants.PickupLifetime;
    }

    public Vector2 HitboxCenter => Position;
    public float HitboxRadius => GameConstants.PickupHitboxRadius;

    public void Update(GameTime gameTime)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _lifeTimer -= delta;

        if (_lifeTimer <= 0f)
            IsActive = false;
    }

    public void Collect()
    {
        IsActive = false;
    }

    public bool IsVisible
    {
        get
        {
            if (_lifeTimer > GameConstants.PickupBlinkDuration)
                return true; // still in the solid, non-blinking phase

            float timeIntoBlinkWindow = GameConstants.PickupBlinkDuration - _lifeTimer;
            int totalIntervals = GameConstants.PickupBlinkCount * 2; // each blink = one "on" + one "off"
            float intervalLength = GameConstants.PickupBlinkDuration / totalIntervals;

            int currentInterval = (int)(timeIntoBlinkWindow / intervalLength);

            // Even intervals = visible ("on"), odd intervals = hidden ("off")
            return currentInterval % 2 == 0;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible)
            return;

        var rect = new Rectangle(
            (int)(Position.X - GameConstants.PickupSize / 2f),
            (int)(Position.Y - GameConstants.PickupSize / 2f),
            GameConstants.PickupSize,
            GameConstants.PickupSize);

        spriteBatch.Draw(pixel, rect, ColorFor(Type));

        string label = LabelFor(Type);
        Vector2 labelSize = font.MeasureString(label);
        Vector2 labelPos = new Vector2(
            Position.X - labelSize.X / 2f,
            Position.Y - GameConstants.PickupSize / 2f - labelSize.Y - 2);

        spriteBatch.DrawString(font, label, labelPos, Color.White);
    }

    private static Color ColorFor(UpgradeType type) => type switch
    {
        UpgradeType.Damage => Color.OrangeRed,
        UpgradeType.FireRate => Color.Cyan,
        UpgradeType.MoveSpeed => Color.Gold,
        UpgradeType.MaxHealth => Color.HotPink,
        UpgradeType.Heal => Color.LimeGreen,
        _ => Color.White
    };

    private static string LabelFor(UpgradeType type) => type switch
    {
        UpgradeType.Damage => "DMG",
        UpgradeType.FireRate => "RATE",
        UpgradeType.MoveSpeed => "SPD",
        UpgradeType.MaxHealth => "HP+",
        UpgradeType.Heal => "HEAL",
        _ => "?"
    };
}