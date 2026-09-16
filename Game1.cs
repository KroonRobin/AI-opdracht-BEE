using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AI_opdracht_BEE.Core;
using AI_opdracht_BEE.Entities;
using AI_opdracht_BEE.Systems;

namespace AI_opdracht_BEE;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;

    private Texture2D _pixel;
    private Player _player;

    private List<Bullet> _bullets = new();
    private MouseState _previousMouseState;
    private MouseState _currentMouseState;

    private List<Enemy> _enemies = new();
    private EnemySpawner _enemySpawner;

    private float _fireCooldown = 0f;
    private int _score;

    private GameState _gameState = GameState.Playing;
    private KeyboardState _previousKeyboardState;

    private Texture2D _healthBarBackground;
    private Texture2D _healthBarBorder;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = GameConstants.RoomWidth;
        _graphics.PreferredBackBufferHeight = GameConstants.RoomHeight;
        _graphics.ApplyChanges();

        ResetGame();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _healthBarBackground = TextureFactory.CreateRoundedRect(
        GraphicsDevice,
        GameConstants.HealthBarWidth,
        GameConstants.HealthBarHeight,
        GameConstants.HealthBarCornerRadius,
        Color.Black);

        int borderWidth = GameConstants.HealthBarWidth + GameConstants.HealthBarOutlineThickness * 2;
        int borderHeight = GameConstants.HealthBarHeight + GameConstants.HealthBarOutlineThickness * 2;

        _healthBarBorder = TextureFactory.CreateRoundedRectBorder(
            GraphicsDevice,
            borderWidth,
            borderHeight,
            GameConstants.HealthBarCornerRadius + GameConstants.HealthBarOutlineThickness,
            GameConstants.HealthBarOutlineThickness,
            Color.White);
    }

    private void ResetGame()
    {
        _player = new Player(new Vector2(GameConstants.RoomWidth / 2f, GameConstants.RoomHeight / 2f));
        _enemies.Clear();
        _bullets.Clear();
        _enemySpawner = new EnemySpawner();
        _score = 0;
        _fireCooldown = 0f;
        _gameState = GameState.Playing;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var currentKeyboardState = Keyboard.GetState();

        switch (_gameState)
        {
            case GameState.Playing:
                UpdatePlaying(gameTime);
                break;

            case GameState.GameOver:
                UpdateGameOver(currentKeyboardState);
                break;
        }

        _previousKeyboardState = currentKeyboardState;

        base.Update(gameTime);
    }

    private void UpdatePlaying(GameTime gameTime)
    {
        _player.Update(gameTime, Keyboard.GetState());

        // --- Mouse aim/shoot ---
        _currentMouseState = Mouse.GetState();
        _fireCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        bool wantsToShoot = _currentMouseState.LeftButton == ButtonState.Pressed;

        if (wantsToShoot && _fireCooldown <= 0f)
        {
            Vector2 mousePosition = new Vector2(_currentMouseState.X, _currentMouseState.Y);
            Vector2 shootOrigin = _player.Position - new Vector2(0, GameConstants.PlayerHeight / 2f);
            Vector2 direction = mousePosition - shootOrigin;

            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _bullets.Add(new Bullet(shootOrigin, direction));
                _fireCooldown = GameConstants.BaseFireInterval;
            }
        }

        foreach (var bullet in _bullets)
            bullet.Update(gameTime);
        // --- end mouse aim/shoot ---

        _enemySpawner.Update(gameTime, _enemies);

        foreach (var enemy in _enemies)
            enemy.Update(gameTime, _player.Position);

        _score += CollisionManager.CheckBulletsVsEnemies(_bullets, _enemies);
        CollisionManager.CheckEnemiesVsPlayer(_enemies, _player);

        _bullets.RemoveAll(b => !b.IsActive);
        _enemies.RemoveAll(e => !e.IsAlive);

        if (!_player.IsAlive)
            _gameState = GameState.GameOver;
    }

    private void UpdateGameOver(KeyboardState currentKeyboardState)
    {
        bool restartPressed =
            currentKeyboardState.IsKeyDown(Keys.Enter) &&
            _previousKeyboardState.IsKeyUp(Keys.Enter);

        if (restartPressed)
            ResetGame();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(62, 48, 40));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _player.Draw(_spriteBatch, _pixel);

        foreach (var bullet in _bullets)
            bullet.Draw(_spriteBatch, _pixel);

        foreach (var enemy in _enemies)
            enemy.Draw(_spriteBatch, _pixel);

        DrawHud();

        if (_gameState == GameState.GameOver)
            DrawGameOverScreen();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawHud()
    {
        int barX = GameConstants.RoomWidth - GameConstants.HealthBarPadding - GameConstants.HealthBarWidth;
        int barY = GameConstants.HealthBarPadding;

        var outerRect = new Rectangle(barX, barY, GameConstants.HealthBarWidth, GameConstants.HealthBarHeight);

        var borderRect = new Rectangle(
            outerRect.X - GameConstants.HealthBarOutlineThickness,
            outerRect.Y - GameConstants.HealthBarOutlineThickness,
            outerRect.Width + GameConstants.HealthBarOutlineThickness * 2,
            outerRect.Height + GameConstants.HealthBarOutlineThickness * 2);

        // Rounded dark background
        _spriteBatch.Draw(_healthBarBackground, outerRect, Color.White);

        // Green fill — plain rectangle, corners hidden by the border drawn after it
        float healthPercent = (float)_player.Health / GameConstants.PlayerMaxHealth;

        Color healthBarColor;
        if (healthPercent <= GameConstants.HealthBarRedThreshold)
            healthBarColor = Color.Red;
        else if (healthPercent <= GameConstants.HealthBarYellowThreshold)
            healthBarColor = new Color(191, 143, 0);
        else
            healthBarColor = Color.Green;

        var fillRect = new Rectangle(
            outerRect.X,
            outerRect.Y,
            (int)(outerRect.Width * healthPercent),
            outerRect.Height);

        _spriteBatch.Draw(_pixel, fillRect, healthBarColor);

        // Rounded white border, drawn on top
        _spriteBatch.Draw(_healthBarBorder, borderRect, Color.White);

        // Health number, centered in the bar
        string healthText = $"{_player.Health} HP";
        Vector2 textSize = _font.MeasureString(healthText);
        Vector2 textPosition = new Vector2(
            outerRect.X + outerRect.Width / 2f - textSize.X / 2f,
            outerRect.Y + outerRect.Height / 2f - textSize.Y / 2f);

        _spriteBatch.DrawString(_font, healthText, textPosition, Color.White);

        // Score, positioned just left of the health bar
        string scoreText = $"Score: {_score}";
        Vector2 scoreSize = _font.MeasureString(scoreText);
        Vector2 scorePosition = new Vector2(
            borderRect.X - 16 - scoreSize.X,
            outerRect.Y + outerRect.Height / 2f - scoreSize.Y / 2f);

        _spriteBatch.DrawString(_font, scoreText, scorePosition, Color.White);
    }

    private void DrawGameOverScreen()
    {
        const string title = "GAME OVER";
        const string subtitle = "Press ENTER to restart";

        Vector2 titleSize = _font.MeasureString(title);
        Vector2 subtitleSize = _font.MeasureString(subtitle);

        Vector2 titlePos = new Vector2(
            GameConstants.RoomWidth / 2f - titleSize.X / 2f,
            GameConstants.RoomHeight / 2f - titleSize.Y);

        Vector2 subtitlePos = new Vector2(
            GameConstants.RoomWidth / 2f - subtitleSize.X / 2f,
            GameConstants.RoomHeight / 2f + 10);

        _spriteBatch.DrawString(_font, title, titlePos, Color.Red);
        _spriteBatch.DrawString(_font, subtitle, subtitlePos, Color.White);
    }
}