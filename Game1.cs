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

    private Texture2D _pixel;
    private Player _player;

    private List<Bullet> _bullets = new();
    private MouseState _previousMouseState;
    private MouseState _currentMouseState;

    private List<Enemy> _enemies = new();
    private EnemySpawner _enemySpawner;

    private float _fireCooldown = 0f;

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

        _player = new Player(new Vector2(GameConstants.RoomWidth / 2f, GameConstants.RoomHeight / 2f));

        _enemySpawner = new EnemySpawner();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

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

        _bullets.RemoveAll(b => !b.IsActive);
        // --- end mouse aim/shoot ---

        _enemySpawner.Update(gameTime, _enemies);

        foreach (var enemy in _enemies)
        {
            enemy.Update(gameTime, _player.Position);

            if (enemy.CanAttack && Vector2.Distance(enemy.HitboxCenter, _player.HitboxCenter) < enemy.HitboxRadius + _player.HitboxRadius)
            {
                _player.TakeDamage(GameConstants.EnemyContactDamage);
                enemy.OnHitPlayer(_player.Position);
            }
        }

        base.Update(gameTime);
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

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}