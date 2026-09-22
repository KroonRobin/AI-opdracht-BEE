using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    private bool _shootingUnlocked;

    private List<Bullet> _bullets = new();
    private List<Bullet> _enemyBullets = new();
    private MouseState _currentMouseState;
    private MouseState _previousMouseState;

    private MeleeAttackVisual _meleeAttackVisual;
    private float _meleeCooldown;

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

    private List<HighScoreEntry> _highScores = new();
    private StringBuilder _nameInput = new StringBuilder();
    private const int MaxNameLength = 12;
    private float _cursorBlinkTimer;
    private bool _showCursor = true;

    private RenderTarget2D _renderTarget;
    private Rectangle _renderDestinationRect;
    private float _renderScale;
    private int _renderOffsetX;
    private int _renderOffsetY;
    private Vector2 _virtualMousePosition;

    private Texture2D _playerFrontTexture;
    private Texture2D _playerBackTexture;
    private Texture2D _playerSideTexture;

    private Texture2D _arrowTexture;
    private Texture2D _enemyOrbTexture;

    private Texture2D _speedPickupTexture;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Window.TextInput += OnTextInput;

        var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _graphics.PreferredBackBufferWidth = displayMode.Width;
        _graphics.PreferredBackBufferHeight = displayMode.Height;
        _graphics.IsFullScreen = true;
        _graphics.ApplyChanges();

        _highScores = HighScoreManager.Load();

        ResetGame();
        _gameState = GameState.MainMenu;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _arrowTexture = Content.Load<Texture2D>("Sprites/arrow");
        _enemyOrbTexture = Content.Load<Texture2D>("Sprites/enemy_projectile");

        _playerFrontTexture = Content.Load<Texture2D>("Sprites/character_front");
        _playerBackTexture = Content.Load<Texture2D>("Sprites/character_back");
        _playerSideTexture = Content.Load<Texture2D>("Sprites/character_side");

        _speedPickupTexture = Content.Load<Texture2D>("Sprites/speed_pwrup");

        _renderTarget = new RenderTarget2D(GraphicsDevice, GameConstants.RoomWidth, GameConstants.RoomHeight);

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
        _meleeAttackVisual = null;
        _meleeCooldown = 0f;
        _gameState = GameState.Playing;
        _currentBulletDamage = GameConstants.BulletDamage;
        _currentFireInterval = GameConstants.BaseFireInterval;
        _pickups.Clear();
        _upgradesDroppedThisRound = 0;
        _lastTrackedLevel = 0;
        _shootingUnlocked = false;
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateRenderTransform();

        var currentKeyboardState = Keyboard.GetState();
        var currentMouseState = Mouse.GetState();
        _currentMouseState = currentMouseState;
        _virtualMousePosition = ScreenToVirtualMouse(currentMouseState);

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

            case GameState.EnteringName:
                UpdateNameEntry(gameTime, currentKeyboardState);
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
        _player.FaceTowardMouse(_virtualMousePosition);

        // --- Mouse aim/shoot ---
        _fireCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!_shootingUnlocked && _currentMouseState.LeftButton == ButtonState.Released)
            _shootingUnlocked = true;

        bool wantsToShoot = _shootingUnlocked && _currentMouseState.LeftButton == ButtonState.Pressed;

        if (wantsToShoot && _fireCooldown <= 0f)
        {
            Vector2 mousePosition = _virtualMousePosition;
            Vector2 shootOrigin = _player.Position - new Vector2(0, GameConstants.PlayerHeight / 2f);
            Vector2 direction = mousePosition - shootOrigin;

            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _bullets.Add(new Bullet(
                    shootOrigin, direction, _currentBulletDamage,
                    GameConstants.BulletSpeed, GameConstants.BulletRadius, Color.White, _arrowTexture,
                    GameConstants.ArrowSpriteScale, spinSpeed: 0f, rotationOffset: MathHelper.ToRadians(45f)));
                _fireCooldown = _currentFireInterval;
            }
        }

        // --- Melee attack ---
        _meleeCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        bool rightClicked =
            _currentMouseState.RightButton == ButtonState.Pressed &&
            _previousMouseState.RightButton == ButtonState.Released;

        if (rightClicked && _meleeCooldown <= 0f)
        {
            TriggerMeleeAttack();
            _meleeCooldown = GameConstants.MeleeAttackCooldown;
        }

        if (_meleeAttackVisual != null)
        {
            var meleeDeaths = _meleeAttackVisual.Update(gameTime, _enemies);

            foreach (var death in meleeDeaths)
            {
                _score += death.Points;
                TryDropPickup(death.Position);
            }

            if (!_meleeAttackVisual.IsActive)
                _meleeAttackVisual = null;
        }
        // --- end melee attack ---

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
                        GameConstants.EnemyBulletSpeed, GameConstants.EnemyBulletRadius, Color.White, _enemyOrbTexture,
                        GameConstants.EnemyOrbSpriteScale, spinSpeed: GameConstants.EnemyOrbSpinSpeed));
            }

            enemy.ConsumeFireRequest();
        }
        // --- end ranged enemy fire requests ---

        var deaths = CollisionManager.CheckBulletsVsEnemies(_bullets, _enemies);

        foreach (var death in deaths)
        {
            _score += death.Points;
            TryDropPickup(death.Position);
        }

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
            if (HighScoreManager.Qualifies(_highScores, _score))
            {
                _nameInput.Clear();
                _gameState = GameState.EnteringName;
            }
            else
            {
                _gameState = GameState.GameOver;
            }
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

        Point mousePoint = new Point((int)_virtualMousePosition.X, (int)_virtualMousePosition.Y);
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

        Point mousePoint = new Point((int)_virtualMousePosition.X, (int)_virtualMousePosition.Y);
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
        Point mousePoint = new Point((int)_virtualMousePosition.X, (int)_virtualMousePosition.Y);
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
        Point mousePoint = new Point((int)_virtualMousePosition.X, (int)_virtualMousePosition.Y);
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

        Texture2D icon = chosenType == UpgradeType.MoveSpeed ? _speedPickupTexture : null;
        _pickups.Add(new Pickup(position, chosenType, icon));
        _upgradesDroppedThisRound++;
    }

    private void TriggerMeleeAttack()
    {
        Vector2 attackOrigin = _player.Position - new Vector2(0, GameConstants.PlayerHeight / 2f);
        Vector2 aimDirection = _virtualMousePosition - attackOrigin;

        if (aimDirection == Vector2.Zero)
            aimDirection = new Vector2(0, 1);

        float rawAngle = (float)Math.Atan2(aimDirection.Y, aimDirection.X);
        float centerAngle = SnapAngleToCompass(rawAngle);
        float halfArc = MathHelper.ToRadians(GameConstants.MeleeAttackArcDegrees / 2f);

        _meleeAttackVisual = new MeleeAttackVisual(
            attackOrigin, centerAngle, halfArc, GameConstants.MeleeAttackRadius,
            _currentBulletDamage, _player.Position);
    }

    private float SnapAngleToCompass(float angle)
    {
        float step = MathHelper.PiOver4; // 45° in radians
        return (float)Math.Round(angle / step) * step;
    }

    private UpgradeType PickWeightedUpgradeType()
    {
        var weightedOptions = new List<(UpgradeType type, float weight)>
        {
            (UpgradeType.Damage, GameConstants.DamageDropWeight),
            (UpgradeType.FireRate, GameConstants.FireRateDropWeight),
            (UpgradeType.MaxHealth, GameConstants.MaxHealthDropWeight),
        };

        if (!_player.IsAtMaxSpeed)
            weightedOptions.Add((UpgradeType.MoveSpeed, GameConstants.MoveSpeedDropWeight));

        if (_player.Health < _player.MaxHealth)
            weightedOptions.Add((UpgradeType.Heal, GameConstants.HealDropWeight));

        float totalWeight = weightedOptions.Sum(o => o.weight);
        float roll = (float)(_random.NextDouble() * totalWeight);

        float cumulative = 0f;
        foreach (var (type, weight) in weightedOptions)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return type;
        }

        return weightedOptions[^1].type;
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

            case UpgradeType.Heal:
                _player.Heal(GameConstants.HealAmount);
                break;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        // --- Pass 1: draw the whole game at a fixed 1920x1080, into the render target ---
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(62, 48, 40));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _player.Draw(_spriteBatch, _playerFrontTexture, _playerBackTexture, _playerSideTexture, _pixel);

        foreach (var bullet in _bullets)
            bullet.Draw(_spriteBatch, _pixel);

        foreach (var bullet in _enemyBullets)
            bullet.Draw(_spriteBatch, _pixel);

        foreach (var enemy in _enemies)
            enemy.Draw(_spriteBatch, _pixel);

        _meleeAttackVisual?.Draw(_spriteBatch, _pixel);

        foreach (var pickup in _pickups)
            pickup.Draw(_spriteBatch, _pixel, _font);

        DrawHud();

        if (_gameState == GameState.MainMenu)
            DrawMainMenu();

        if (_gameState == GameState.HighScores)
            DrawHighScoresScreen();

        if (_gameState == GameState.Paused)
            DrawPauseScreen();

        if (_gameState == GameState.EnteringName)
            DrawNameEntryScreen();

        if (_gameState == GameState.GameOver)
            DrawGameOverScreen();

        if (_levelManager.ShowBanner)
            DrawRoundBanner();

        _spriteBatch.End();

        // --- Pass 2: scale that canvas onto the real screen, preserving aspect ratio ---
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_renderTarget, _renderDestinationRect, Color.White);
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
                string entryText = $"{i + 1}. {_highScores[i].Name} - {_highScores[i].Score}";
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

        bool isNewHighScore = _highScores.Count > 0 && _highScores[0].Score == _score;
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

    private void OnTextInput(object sender, TextInputEventArgs e)
    {
        if (_gameState != GameState.EnteringName)
            return;

        if (e.Key == Keys.Back)
        {
            if (_nameInput.Length > 0)
                _nameInput.Remove(_nameInput.Length - 1, 1);
            return;
        }

        char typedChar = e.Character;

        if (char.IsControl(typedChar) || typedChar == '|')
            return;

        if (_nameInput.Length < MaxNameLength)
            _nameInput.Append(typedChar);
    }

    private void UpdateNameEntry(GameTime gameTime, KeyboardState currentKeyboardState)
    {
        _cursorBlinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_cursorBlinkTimer >= 0.5f)
        {
            _cursorBlinkTimer = 0f;
            _showCursor = !_showCursor;
        }

        bool enterPressed =
            currentKeyboardState.IsKeyDown(Keys.Enter) &&
            _previousKeyboardState.IsKeyUp(Keys.Enter);

        if (!enterPressed)
            return;

        string finalName = _nameInput.Length > 0 ? _nameInput.ToString() : "Player";
        _highScores = HighScoreManager.AddScore(_highScores, new HighScoreEntry(finalName, _score));
        _gameState = GameState.GameOver;
    }

    private void DrawNameEntryScreen()
    {
        DrawBackgroundOverlay(_gameOverBackgroundImage);

        const string title = "NEW HIGH SCORE!";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new Vector2(
            GameConstants.RoomWidth / 2f - titleSize.X / 2f,
            GameConstants.RoomHeight / 2f - 160);

        _spriteBatch.DrawString(_font, title, titlePos, Color.Gold);

        string scoreText = $"Score: {_score}";
        Vector2 scoreSize = _font.MeasureString(scoreText);
        Vector2 scorePos = new Vector2(
            GameConstants.RoomWidth / 2f - scoreSize.X / 2f,
            GameConstants.RoomHeight / 2f - 110);

        _spriteBatch.DrawString(_font, scoreText, scorePos, Color.White);

        const string prompt = "Enter your name:";
        Vector2 promptSize = _font.MeasureString(prompt);
        Vector2 promptPos = new Vector2(
            GameConstants.RoomWidth / 2f - promptSize.X / 2f,
            GameConstants.RoomHeight / 2f - 60);

        _spriteBatch.DrawString(_font, prompt, promptPos, Color.White);

        const int boxWidth = 320;
        const int boxHeight = 40;
        var boxRect = new Rectangle(
            (int)(GameConstants.RoomWidth / 2f - boxWidth / 2f),
            (int)(GameConstants.RoomHeight / 2f - 20),
            boxWidth, boxHeight);

        const int borderThickness = 2;
        var borderRect = new Rectangle(
            boxRect.X - borderThickness,
            boxRect.Y - borderThickness,
            boxRect.Width + borderThickness * 2,
            boxRect.Height + borderThickness * 2);

        _spriteBatch.Draw(_pixel, borderRect, Color.White);
        _spriteBatch.Draw(_pixel, boxRect, Color.Black);

        string displayText = _nameInput.ToString();
        if (_showCursor)
            displayText += "|";

        Vector2 textSize = _font.MeasureString(displayText);
        Vector2 textPos = new Vector2(
            boxRect.X + 10,
            boxRect.Y + boxRect.Height / 2f - textSize.Y / 2f);

        _spriteBatch.DrawString(_font, displayText, textPos, Color.White);

        const string confirmHint = "Press ENTER to confirm";
        Vector2 hintSize = _font.MeasureString(confirmHint);
        Vector2 hintPos = new Vector2(
            GameConstants.RoomWidth / 2f - hintSize.X / 2f,
            GameConstants.RoomHeight / 2f + 60);

        _spriteBatch.DrawString(_font, confirmHint, hintPos, Color.Gray);
    }

    private void DrawBackgroundOverlay(Texture2D backgroundImage)
    {
        var rect = new Rectangle(0, 0, GameConstants.RoomWidth, GameConstants.RoomHeight);

        if (backgroundImage != null)
            _spriteBatch.Draw(backgroundImage, rect, Color.White);
        else
            _spriteBatch.Draw(_pixel, rect, Color.Black * 0.6f);
    }

    private void UpdateRenderTransform()
    {
        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;

        float scaleX = screenWidth / (float)GameConstants.RoomWidth;
        float scaleY = screenHeight / (float)GameConstants.RoomHeight;
        _renderScale = Math.Min(scaleX, scaleY);

        int destWidth = (int)(GameConstants.RoomWidth * _renderScale);
        int destHeight = (int)(GameConstants.RoomHeight * _renderScale);

        _renderOffsetX = (screenWidth - destWidth) / 2;
        _renderOffsetY = (screenHeight - destHeight) / 2;

        _renderDestinationRect = new Rectangle(_renderOffsetX, _renderOffsetY, destWidth, destHeight);
    }

    private Vector2 ScreenToVirtualMouse(MouseState mouseState)
    {
        return new Vector2(
            (mouseState.X - _renderOffsetX) / _renderScale,
            (mouseState.Y - _renderOffsetY) / _renderScale);
    }
}