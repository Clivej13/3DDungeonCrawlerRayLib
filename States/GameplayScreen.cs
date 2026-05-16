using DungeonCrawler.Core;
using DungeonCrawler.Entities;
using DungeonCrawler.Input;
using DungeonCrawler.Player;
using DungeonCrawler.Rendering;
using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.States;

public sealed class GameplayScreen : IDisposable
{
    private readonly GameStateController _stateController;
    private readonly DungeonMap _map;
    private readonly PlayerController _player;
    private readonly TextureManager _textures;
    private readonly RaycastRenderer _renderer;
    private readonly WeaponRenderer _weaponRenderer;
    private bool _didHitDuringSwing;
    private float _attackCooldownTimer;

    private const float SwordCooldown = 0.35f;
    private const int SwordHitFrame = 3;

    public GameplayScreen(GameStateController stateController)
    {
        _stateController = stateController;
        _textures = new TextureManager();
        _map = new DungeonMap("Assets/Maps/test_map.json", _textures.GoblinTexture);
        _player = new PlayerController(_map);
        _renderer = new RaycastRenderer(_map, _textures.DungeonTexture);
        _weaponRenderer = new WeaponRenderer(_textures.PlayerAnimationsTexture);
    }

    public void Update(InputHandler input, float deltaTime)
    {
        _attackCooldownTimer = MathF.Max(0f, _attackCooldownTimer - deltaTime);

        _player.Update(deltaTime);
        _weaponRenderer.Update(deltaTime);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && _attackCooldownTimer <= 0f)
        {
            _weaponRenderer.TriggerAttack();
            _didHitDuringSwing = false;
            _attackCooldownTimer = SwordCooldown;
            Console.WriteLine("[Combat] Player sword swing started.");
        }

        foreach (Enemy enemy in _map.Enemies)
        {
            enemy.DistanceToPlayer = Vector2.Distance(enemy.Position, _player.Position);
            enemy.Update(deltaTime, _player.Position, _map);
        }

        HandleMeleeCombat();
        HandleEnemyCombat();
        _map.Enemies.RemoveAll(e => !e.IsAlive);

        if (input.BackPressed())
        {
            Raylib.EnableCursor();
            _stateController.ChangeState(GameState.PauseMenu);
        }
    }

    private void HandleMeleeCombat()
    {
        if (!_weaponRenderer.IsSwinging || _didHitDuringSwing || _weaponRenderer.CurrentFrame != SwordHitFrame) return;

        Vector2 forward = new(MathF.Cos(_player.Angle), MathF.Sin(_player.Angle));
        const float meleeRange = 72f;
        const float coneDot = 0.65f;

        foreach (Enemy enemy in _map.Enemies.Where(e => e.IsAlive))
        {
            Vector2 toEnemy = enemy.Position - _player.Position;
            float dist = toEnemy.Length();
            if (dist > meleeRange || dist <= 0.001f) continue;

            Vector2 dir = toEnemy / dist;
            if (Vector2.Dot(forward, dir) < coneDot) continue;

            enemy.TakeDamage(25f);
            Console.WriteLine("[Combat] Player hit goblin with sword.");
            _didHitDuringSwing = true;
            break;
        }
    }

    private void HandleEnemyCombat()
    {
        foreach (GoblinEnemy goblin in _map.Enemies.OfType<GoblinEnemy>().Where(e => e.IsAlive))
        {
            if (!goblin.CanAttackPlayer(_player.Position)) continue;

            float damage = goblin.ConsumeAttackDamage();
            if (_player.TryTakeDamage(damage))
            {
                Console.WriteLine("[Combat] Goblin attacked player.");
            }
        }
    }

    public void Draw()
    {
        Raylib.DisableCursor();
        _renderer.Draw(_player);
        _renderer.DrawMinimap(_player);

        Raylib.DrawText("WASD Move | Mouse Look | ESC Pause", 16, Raylib.GetScreenHeight() - 30, 18, Color.LightGray);
        Raylib.DrawText($"POS {_player.Position.X:0.0},{_player.Position.Y:0.0}  ANG {_player.Angle:0.00}  PITCH {_player.PitchOffset:0}",
            16, Raylib.GetScreenHeight() - 54, 18, new Color(190, 190, 190, 220));
        Raylib.DrawText($"HP: {_player.Health:0}", 16, 14, 24, _player.Health > 25 ? Color.Lime : Color.Red);

        if (_player.DamageFlashAmount > 0f)
        {
            Color flash = Raylib.ColorAlpha(Color.Red, 0.38f * _player.DamageFlashAmount);
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), flash);
        }

        _weaponRenderer.Draw();
    }

    public void Dispose() => _textures.Dispose();
}
