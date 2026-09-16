using System;
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
    private List<Bullet> _enemyBullets = new();
    private MouseState _currentMouseState;

    private List<Enemy> _enemies = new();
    private EnemySpawner _enemySpawner;

    private float _fireCooldown = 0f;
    private int _score;

    private GameState _gameState = GameState.Playing;
    private KeyboardState _previousKeyboardState;

    private Texture2D _healthBarBackground;
    private Texture2D _healthBarBorder;

    private LevelManager _levelManager;

    private List<Pickup> _pickups = new();
    private Random _random = new();
    private int _currentBulletDamage;
    private float _currentFireInterval;

    private int _upgradesDroppedThisRound;
    private int _lastTrackedLevel;

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
        _enemyBullets.Clear();
        _enemySpawner = new EnemySpawner();
        _levelManager = new LevelManager();
        _score = 0;
        _fireCooldown = 0f;
        _gameState = GameState.Playing;
        _currentBulletDamage = GameConstants.BulletDamage;
        _currentFireInterval = GameConstants.BaseFireInterval;
        _pickups.Clear();
        _upgradesDroppedThisRound = 0;
        _lastTrackedLevel = 0;
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
                _bullets.Add(new Bullet(
                    shootOrigin, direction, _currentBulletDamage,
                    GameConstants.BulletSpeed, GameConstants.BulletRadius, Color.Gold));
                _fireCooldown = _currentFireInterval;
            }
        }

        foreach (var bullet in _bullets)
            bullet.Update(gameTime);

        foreach (var bullet in _enemyBullets)
            bullet.Update(gameTime);
        // --- end mouse aim/shoot ---

        _levelManager.Update(gameTime, _enemies, _enemySpawner);

        if (_levelManager.CurrentLevel != _lastTrackedLevel)
        {
            _lastTrackedLevel = _levelManager.CurrentLevel;
            _upgradesDroppedThisRound = 0;
        }

        if (_levelManager.IsSpawningAllowed)
            _enemySpawner.Update(gameTime, _enemies);

        foreach (var enemy in _enemies)
            enemy.Update(gameTime, _player.Position);

        // --- Ranged enemy fire requests ---
        foreach (var enemy in _enemies)
        {
            if (!enemy.WantsToFire)
                continue;

            Vector2 direction = _player.Position - enemy.ShootOrigin;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _enemyBullets.Add(new Bullet(
                    enemy.ShootOrigin, direction, enemy.BulletDamage,
                    GameConstants.EnemyBulletSpeed, GameConstants.EnemyBulletRadius, Color.Cyan));
            }

            enemy.ConsumeFireRequest();
        }
        // --- end ranged enemy fire requests ---

        var deathPositions = CollisionManager.CheckBulletsVsEnemies(_bullets, _enemies);
        _score += deathPositions.Count;

        foreach (var deathPosition in deathPositions)
            TryDropPickup(deathPosition);

        CollisionManager.CheckEnemiesVsPlayer(_enemies, _player);
        CollisionManager.CheckEnemyBulletsVsPlayer(_enemyBullets, _player);

        var collectedUpgrades = CollisionManager.CheckPickupsVsPlayer(_pickups, _player);
        foreach (var upgrade in collectedUpgrades)
            ApplyUpgrade(upgrade);

        _bullets.RemoveAll(b => !b.IsActive);
        _enemyBullets.RemoveAll(b => !b.IsActive);
        _enemies.RemoveAll(e => !e.IsAlive);
        _pickups.RemoveAll(p => !p.IsActive);

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

    private void TryDropPickup(Vector2 position)
    {
        if (_upgradesDroppedThisRound >= GameConstants.MaxUpgradesPerRound)
            return;

        if (_random.NextDouble() > GameConstants.PickupDropChance)
            return;

        var allTypes = Enum.GetValues<UpgradeType>();
        var chosenType = allTypes[_random.Next(allTypes.Length)];

        _pickups.Add(new Pickup(position, chosenType));
        _upgradesDroppedThisRound++;
    }

    private void ApplyUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Damage:
                _currentBulletDamage += GameConstants.DamageUpgradeAmount;
                break;

            case UpgradeType.FireRate:
                _currentFireInterval = Math.Max(
                    GameConstants.MinFireInterval,
                    _currentFireInterval * GameConstants.FireRateUpgradeMultiplier);
                break;

            case UpgradeType.MoveSpeed:
                _player.IncreaseMoveSpeed(GameConstants.MoveSpeedUpgradeAmount);
                break;

            case UpgradeType.MaxHealth:
                _player.IncreaseMaxHealth(GameConstants.MaxHealthUpgradeAmount);
                break;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(62, 48, 40));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _player.Draw(_spriteBatch, _pixel);

        foreach (var bullet in _bullets)
            bullet.Draw(_spriteBatch, _pixel);

        foreach (var bullet in _enemyBullets)
            bullet.Draw(_spriteBatch, _pixel);

        foreach (var enemy in _enemies)
            enemy.Draw(_spriteBatch, _pixel);

        foreach (var pickup in _pickups)
            pickup.Draw(_spriteBatch, _pixel, _font);

        DrawHud();

        if (_levelManager.ShowBanner)
            DrawRoundBanner();

        if (_gameState == GameState.GameOver)
            DrawGameOverScreen();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawRoundBanner()
    {
        string text = _levelManager.BannerText;
        Vector2 textSize = _font.MeasureString(text);
        Vector2 position = new Vector2(
            GameConstants.RoomWidth / 2f - textSize.X / 2f,
            GameConstants.RoomHeight / 2f - textSize.Y / 2f);

        _spriteBatch.DrawString(_font, text, position, Color.White);
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

        _spriteBatch.Draw(_healthBarBackground, outerRect, Color.White);

        float healthPercent = (float)_player.Health / _player.MaxHealth;

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

        _spriteBatch.Draw(_healthBarBorder, borderRect, Color.White);

        string healthText = $"{_player.Health} HP";
        Vector2 textSize = _font.MeasureString(healthText);
        Vector2 textPosition = new Vector2(
            outerRect.X + outerRect.Width / 2f - textSize.X / 2f,
            outerRect.Y + outerRect.Height / 2f - textSize.Y / 2f);

        _spriteBatch.DrawString(_font, healthText, textPosition, Color.White);

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