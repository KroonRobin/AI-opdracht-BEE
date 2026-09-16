using System;
using System.Collections.Generic;
using System.Linq;
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
    private MouseState _previousMouseState;

    private List<Enemy> _enemies = new();
    private EnemySpawner _enemySpawner;

    private float _fireCooldown = 0f;
    private int _score;

    private GameState _gameState = GameState.MainMenu;
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

    private Dictionary<UpgradeType, int> _dropCounts = new();

    private List<int> _highScores = new();
    private int _selectedMenuIndex;
    private static readonly string[] MenuOptions = { "Start", "High Scores", "Exit" };

    private Texture2D _menuBackgroundImage;      // used by MainMenu and HighScores
    private Texture2D _gameOverBackgroundImage;  // used by GameOver — separate, so it can differ later
    private bool _highScoresBackHovered;

    private int _selectedGameOverIndex;
    private static readonly string[] GameOverOptions = { "Play Again", "Main Menu" };

    private int _selectedPauseIndex;
    private static readonly string[] PauseOptions = { "Continue", "Main Menu" };
    private Texture2D _pauseBackgroundImage;   // separate from menu/game-over, swappable later same as the others

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

        _highScores = HighScoreManager.Load();

        ResetGame();
        _gameState = GameState.MainMenu; // override the Playing state ResetGame() sets

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

        // _menuBackgroundImage stays null until you have real art to load here, e.g.:
        // _menuBackgroundImage = Content.Load<Texture2D>("menu_background");
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
        var currentKeyboardState = Keyboard.GetState();
        var currentMouseState = Mouse.GetState();
        _currentMouseState = currentMouseState;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        bool escapeJustPressed =
            currentKeyboardState.IsKeyDown(Keys.Escape) &&
            _previousKeyboardState.IsKeyUp(Keys.Escape);

        bool escapeQuitsHere =
            _gameState == GameState.MainMenu ||
            _gameState == GameState.GameOver;

        if (escapeQuitsHere && escapeJustPressed)
            Exit();

        switch (_gameState)
        {
            case GameState.MainMenu:
                UpdateMainMenu(currentKeyboardState, currentMouseState);
                break;

            case GameState.HighScores:
                UpdateHighScoresScreen(currentKeyboardState, currentMouseState);
                break;

            case GameState.Playing:
                UpdatePlaying(gameTime, currentKeyboardState);
                break;

            case GameState.Paused:
                UpdatePauseScreen(currentKeyboardState, currentMouseState);
                break;

            case GameState.GameOver:
                UpdateGameOverScreen(currentKeyboardState, currentMouseState);
                break;
        }

        _previousKeyboardState = currentKeyboardState;
        _previousMouseState = currentMouseState;

        base.Update(gameTime);
    }

    private void UpdatePlaying(GameTime gameTime, KeyboardState currentKeyboardState)
    {
        bool pausePressed =
            currentKeyboardState.IsKeyDown(Keys.Escape) &&
            _previousKeyboardState.IsKeyUp(Keys.Escape);

        if (pausePressed)
        {
            _gameState = GameState.Paused;
            return;
        }

        _player.Update(gameTime, Keyboard.GetState());

        // --- Mouse aim/shoot ---
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

        foreach (var pickup in _pickups)
            pickup.Update(gameTime);

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
        {
            _highScores = HighScoreManager.AddScore(_highScores, _score);
            _gameState = GameState.GameOver;
        }
    }

    private void UpdatePauseScreen(KeyboardState currentKeyboardState, MouseState currentMouseState)
    {
        bool escapePressed =
            currentKeyboardState.IsKeyDown(Keys.Escape) &&
            _previousKeyboardState.IsKeyUp(Keys.Escape);

        if (escapePressed)
        {
            _gameState = GameState.Playing;
            return;
        }

        bool upPressed =
            (currentKeyboardState.IsKeyDown(Keys.W) || currentKeyboardState.IsKeyDown(Keys.Up)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.W) || _previousKeyboardState.IsKeyDown(Keys.Up));

        bool downPressed =
            (currentKeyboardState.IsKeyDown(Keys.S) || currentKeyboardState.IsKeyDown(Keys.Down)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.S) || _previousKeyboardState.IsKeyDown(Keys.Down));

        if (upPressed)
            _selectedPauseIndex = (_selectedPauseIndex - 1 + PauseOptions.Length) % PauseOptions.Length;

        if (downPressed)
            _selectedPauseIndex = (_selectedPauseIndex + 1) % PauseOptions.Length;

        Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);
        int? hoveredIndex = null;

        for (int i = 0; i < PauseOptions.Length; i++)
        {
            if (GetPauseOptionRect(PauseOptions[i], i).Contains(mousePoint))
            {
                hoveredIndex = i;
                break;
            }
        }

        if (hoveredIndex.HasValue)
            _selectedPauseIndex = hoveredIndex.Value;

        bool enterConfirmed =
            currentKeyboardState.IsKeyDown(Keys.Enter) &&
            _previousKeyboardState.IsKeyUp(Keys.Enter);

        bool mouseClicked =
            currentMouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released;

        bool confirmed = enterConfirmed || (mouseClicked && hoveredIndex.HasValue);

        if (!confirmed)
            return;

        switch (_selectedPauseIndex)
        {
            case 0: // Continue
                _gameState = GameState.Playing;
                break;

            case 1: // Main Menu
                ResetGame();
                _gameState = GameState.MainMenu;
                break;
        }
    }

    private void UpdateGameOverScreen(KeyboardState currentKeyboardState, MouseState currentMouseState)
    {
        bool upPressed =
            (currentKeyboardState.IsKeyDown(Keys.W) || currentKeyboardState.IsKeyDown(Keys.Up)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.W) || _previousKeyboardState.IsKeyDown(Keys.Up));

        bool downPressed =
            (currentKeyboardState.IsKeyDown(Keys.S) || currentKeyboardState.IsKeyDown(Keys.Down)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.S) || _previousKeyboardState.IsKeyDown(Keys.Down));

        if (upPressed)
            _selectedGameOverIndex = (_selectedGameOverIndex - 1 + GameOverOptions.Length) % GameOverOptions.Length;

        if (downPressed)
            _selectedGameOverIndex = (_selectedGameOverIndex + 1) % GameOverOptions.Length;

        Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);
        int? hoveredIndex = null;

        for (int i = 0; i < GameOverOptions.Length; i++)
        {
            if (GetGameOverOptionRect(GameOverOptions[i], i).Contains(mousePoint))
            {
                hoveredIndex = i;
                break;
            }
        }

        if (hoveredIndex.HasValue)
            _selectedGameOverIndex = hoveredIndex.Value;

        bool enterConfirmed =
            currentKeyboardState.IsKeyDown(Keys.Enter) &&
            _previousKeyboardState.IsKeyUp(Keys.Enter);

        bool mouseClicked =
            currentMouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released;

        bool confirmed = enterConfirmed || (mouseClicked && hoveredIndex.HasValue);

        if (!confirmed)
            return;

        switch (_selectedGameOverIndex)
        {
            case 0: // Play Again
                ResetGame();
                break;

            case 1: // Main Menu
                ResetGame();
                _gameState = GameState.MainMenu;
                break;
        }
    }

    private void UpdateMainMenu(KeyboardState currentKeyboardState, MouseState currentMouseState)
    {
        bool upPressed =
            (currentKeyboardState.IsKeyDown(Keys.W) || currentKeyboardState.IsKeyDown(Keys.Up)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.W) || _previousKeyboardState.IsKeyDown(Keys.Up));

        bool downPressed =
            (currentKeyboardState.IsKeyDown(Keys.S) || currentKeyboardState.IsKeyDown(Keys.Down)) &&
            !(_previousKeyboardState.IsKeyDown(Keys.S) || _previousKeyboardState.IsKeyDown(Keys.Down));

        if (upPressed)
            _selectedMenuIndex = (_selectedMenuIndex - 1 + MenuOptions.Length) % MenuOptions.Length;

        if (downPressed)
            _selectedMenuIndex = (_selectedMenuIndex + 1) % MenuOptions.Length;

        // Mouse hover overrides keyboard selection while the cursor sits over an option
        Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);
        int? hoveredIndex = null;

        for (int i = 0; i < MenuOptions.Length; i++)
        {
            if (GetMenuOptionRect(MenuOptions[i], i).Contains(mousePoint))
            {
                hoveredIndex = i;
                break;
            }
        }

        if (hoveredIndex.HasValue)
            _selectedMenuIndex = hoveredIndex.Value;

        bool enterConfirmed =
            currentKeyboardState.IsKeyDown(Keys.Enter) &&
            _previousKeyboardState.IsKeyUp(Keys.Enter);

        bool mouseClicked =
            currentMouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released;

        bool confirmed = enterConfirmed || (mouseClicked && hoveredIndex.HasValue);

        if (!confirmed)
            return;

        switch (_selectedMenuIndex)
        {
            case 0: // Start
                ResetGame();
                break;

            case 1: // High Scores
                _gameState = GameState.HighScores;
                break;

            case 2: // Exit
                Exit();
                break;
        }
    }

    private void UpdateHighScoresScreen(KeyboardState currentKeyboardState, MouseState currentMouseState)
    {
        Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);
        Rectangle backRect = GetBackButtonRect();
        _highScoresBackHovered = backRect.Contains(mousePoint);

        bool escPressed =
            currentKeyboardState.IsKeyDown(Keys.Escape) &&
            _previousKeyboardState.IsKeyUp(Keys.Escape);

        bool enterPressed =
            currentKeyboardState.IsKeyDown(Keys.Enter) &&
            _previousKeyboardState.IsKeyUp(Keys.Enter);

        bool mouseClicked =
            currentMouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released;

        bool backConfirmed = escPressed || enterPressed || (mouseClicked && _highScoresBackHovered);

        if (backConfirmed)
            _gameState = GameState.MainMenu;
    }

    private Rectangle GetMenuOptionRect(string text, int index)
    {
        Vector2 size = _font.MeasureString(text);
        Vector2 pos = new Vector2(
            GameConstants.RoomWidth / 2f - size.X / 2f,
            GameConstants.RoomHeight / 2f - 40 + index * 40);

        return new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
    }

    private Rectangle GetBackButtonRect()
    {
        const string backText = "Back";
        Vector2 size = _font.MeasureString(backText);
        Vector2 pos = new Vector2(
            GameConstants.RoomWidth / 2f - size.X / 2f,
            GameConstants.RoomHeight / 2f + 120);

        return new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
    }

    private Rectangle GetGameOverOptionRect(string text, int index)
    {
        Vector2 size = _font.MeasureString(text);
        Vector2 pos = new Vector2(
            GameConstants.RoomWidth / 2f - size.X / 2f,
            GameConstants.RoomHeight / 2f + 60 + index * 40);

        return new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
    }

    private Rectangle GetPauseOptionRect(string text, int index)
    {
        Vector2 size = _font.MeasureString(text);
        Vector2 pos = new Vector2(
            GameConstants.RoomWidth / 2f - size.X / 2f,
            GameConstants.RoomHeight / 2f - 20 + index * 40);

        return new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
    }

    private void TryDropPickup(Vector2 position)
    {
        if (_upgradesDroppedThisRound >= GameConstants.MaxUpgradesPerRound)
            return;

        if (_random.NextDouble() > GameConstants.PickupDropChance)
            return;

        var chosenType = PickWeightedUpgradeType();

        _pickups.Add(new Pickup(position, chosenType));
        _upgradesDroppedThisRound++;

        // --- debug tracking ---
        if (!_dropCounts.ContainsKey(chosenType))
            _dropCounts[chosenType] = 0;
        _dropCounts[chosenType]++;
        // --- end debug tracking ---
    }

    private UpgradeType PickWeightedUpgradeType()
    {
        var weightedOptions = new (UpgradeType type, float weight)[]
        {
            (UpgradeType.Damage, GameConstants.DamageDropWeight),
            (UpgradeType.FireRate, GameConstants.FireRateDropWeight),
            (UpgradeType.MoveSpeed, GameConstants.MoveSpeedDropWeight),
            (UpgradeType.MaxHealth, GameConstants.MaxHealthDropWeight),
        };

        float totalWeight = weightedOptions.Sum(o => o.weight);
        float roll = (float)(_random.NextDouble() * totalWeight);

        float cumulative = 0f;
        foreach (var (type, weight) in weightedOptions)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return type;
        }

        return weightedOptions[^1].type; // fallback, should never actually be reached
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

        if (_gameState == GameState.MainMenu)
            DrawMainMenu();

        if (_gameState == GameState.HighScores)
            DrawHighScoresScreen();

        if (_gameState == GameState.Paused)
            DrawPauseScreen();

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

        // --- debug: pickup drop counts ---
        string dropDebug = string.Join(" | ", _dropCounts.Select(kv => $"{kv.Key}: {kv.Value}"));
        _spriteBatch.DrawString(_font, dropDebug, new Vector2(16, 60), Color.White);
        // --- end debug ---
    }

    private void DrawMainMenu()
    {
        DrawBackgroundOverlay(_menuBackgroundImage);

        const string title = "Dungeon Locked";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new Vector2(
            GameConstants.RoomWidth / 2f - titleSize.X / 2f,
            GameConstants.RoomHeight / 2f - 140);

        _spriteBatch.DrawString(_font, title, titlePos, Color.White);

        for (int i = 0; i < MenuOptions.Length; i++)
        {
            string optionText = MenuOptions[i];
            Rectangle rect = GetMenuOptionRect(optionText, i);
            bool isSelected = i == _selectedMenuIndex;

            Color optionColor = isSelected ? Color.Red : Color.White;

            _spriteBatch.DrawString(_font, optionText, new Vector2(rect.X, rect.Y), optionColor);
        }
    }

    private void DrawHighScoresScreen()
    {
        DrawBackgroundOverlay(_menuBackgroundImage);

        const string title = "HIGH SCORES";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new Vector2(
            GameConstants.RoomWidth / 2f - titleSize.X / 2f,
            GameConstants.RoomHeight / 2f - 160);

        _spriteBatch.DrawString(_font, title, titlePos, Color.White);

        if (_highScores.Count == 0)
        {
            const string noneText = "No scores yet";
            Vector2 noneSize = _font.MeasureString(noneText);
            Vector2 nonePos = new Vector2(
                GameConstants.RoomWidth / 2f - noneSize.X / 2f,
                GameConstants.RoomHeight / 2f - 80);

            _spriteBatch.DrawString(_font, noneText, nonePos, Color.White);
        }
        else
        {
            for (int i = 0; i < _highScores.Count; i++)
            {
                string entryText = $"{i + 1}. {_highScores[i]}";
                Vector2 entrySize = _font.MeasureString(entryText);
                Vector2 entryPos = new Vector2(
                    GameConstants.RoomWidth / 2f - entrySize.X / 2f,
                    GameConstants.RoomHeight / 2f - 80 + i * 32);

                _spriteBatch.DrawString(_font, entryText, entryPos, Color.White);
            }
        }

        Rectangle backRect = GetBackButtonRect();
        Color backColor = _highScoresBackHovered ? Color.Red : Color.White;
        _spriteBatch.DrawString(_font, "Back", new Vector2(backRect.X, backRect.Y), backColor);

        const string escHint = "(or press ESC)";
        Vector2 escSize = _font.MeasureString(escHint);
        Vector2 escPos = new Vector2(
            GameConstants.RoomWidth / 2f - escSize.X / 2f,
            backRect.Y + 30);

        _spriteBatch.DrawString(_font, escHint, escPos, Color.Gray);
    }

    private void DrawPauseScreen()
    {
        DrawBackgroundOverlay(_pauseBackgroundImage);

        const string title = "PAUSED";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new Vector2(
            GameConstants.RoomWidth / 2f - titleSize.X / 2f,
            GameConstants.RoomHeight / 2f - 100);

        _spriteBatch.DrawString(_font, title, titlePos, Color.White);

        for (int i = 0; i < PauseOptions.Length; i++)
        {
            string optionText = PauseOptions[i];
            Rectangle rect = GetPauseOptionRect(optionText, i);
            bool isSelected = i == _selectedPauseIndex;

            Color optionColor = isSelected ? Color.Red : Color.White;

            _spriteBatch.DrawString(_font, optionText, new Vector2(rect.X, rect.Y), optionColor);
        }
    }

    private void DrawGameOverScreen()
    {
        DrawBackgroundOverlay(_gameOverBackgroundImage);
        const string title = "GAME OVER";
        string scoreText = $"Score: {_score}";

        bool isNewHighScore = _highScores.Count > 0 && _highScores[0] == _score;
        string highScoreText = isNewHighScore ? "New High Score!" : "";

        Vector2 titleSize = _font.MeasureString(title);
        Vector2 scoreSize = _font.MeasureString(scoreText);
        Vector2 highScoreSize = _font.MeasureString(highScoreText);

        Vector2 titlePos = new Vector2(
            GameConstants.RoomWidth / 2f - titleSize.X / 2f,
            GameConstants.RoomHeight / 2f - titleSize.Y - 40);

        Vector2 scorePos = new Vector2(
            GameConstants.RoomWidth / 2f - scoreSize.X / 2f,
            GameConstants.RoomHeight / 2f);

        Vector2 highScorePos = new Vector2(
            GameConstants.RoomWidth / 2f - highScoreSize.X / 2f,
            GameConstants.RoomHeight / 2f + 30);

        _spriteBatch.DrawString(_font, title, titlePos, Color.Red);
        _spriteBatch.DrawString(_font, scoreText, scorePos, Color.White);

        if (isNewHighScore)
            _spriteBatch.DrawString(_font, highScoreText, highScorePos, Color.Gold);

        for (int i = 0; i < GameOverOptions.Length; i++)
        {
            string optionText = GameOverOptions[i];
            Rectangle rect = GetGameOverOptionRect(optionText, i);
            bool isSelected = i == _selectedGameOverIndex;

            Color optionColor = isSelected ? Color.Red : Color.White;

            _spriteBatch.DrawString(_font, optionText, new Vector2(rect.X, rect.Y), optionColor);
        }
    }

    private void DrawBackgroundOverlay(Texture2D backgroundImage)
    {
        var rect = new Rectangle(0, 0, GameConstants.RoomWidth, GameConstants.RoomHeight);

        if (backgroundImage != null)
            _spriteBatch.Draw(backgroundImage, rect, Color.White);
        else
            _spriteBatch.Draw(_pixel, rect, Color.Black * 0.6f);
    }
}