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
    private enum GameplayPhase
    {
        Playing,
        Victory
    }

    private readonly GameStateController _stateController;
    private readonly DungeonMap _map;
    private readonly PlayerController _player;
    private readonly TextureManager _textures;
    private readonly RaycastRenderer _renderer;
    private readonly WeaponRenderer _weaponRenderer;
    private bool _didHitDuringSwing;
    private float _attackCooldownTimer;
    private GameplayPhase _phase = GameplayPhase.Playing;
    private float _levelTimer;
    private float _statusTextTimer;
    private string _statusText = string.Empty;

    private const float SwordCooldown = 0.35f;
    private const int SwordHitFrame = 3;

    public GameplayScreen(GameStateController stateController)
    {
        _stateController = stateController;
        _textures = new TextureManager();
        _map = new DungeonMap("Assets/Maps/test_map.json", _textures.GoblinTexture, _textures.SilverKeyTexture, _textures.GoldKeyTexture);
        _player = new PlayerController(_map);
        _renderer = new RaycastRenderer(_map, _textures.DungeonTexture, _textures.ClosedDoorTexture, _textures.OpenDoorTexture);
        _weaponRenderer = new WeaponRenderer(_textures.PlayerAnimationsTexture);
    }

    public void Update(InputHandler input, float deltaTime)
    {
        _levelTimer += deltaTime;
        _attackCooldownTimer = MathF.Max(0f, _attackCooldownTimer - deltaTime);
        _statusTextTimer = MathF.Max(0f, _statusTextTimer - deltaTime);

        bool gameplayActive = _phase == GameplayPhase.Playing;
        if (gameplayActive)
        {
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
            HandleProgression();
            _map.Enemies.RemoveAll(e => !e.IsAlive);
        }
        else
        {
            _weaponRenderer.Update(deltaTime);
        }

        if (input.BackPressed())
        {
            Raylib.EnableCursor();
            _stateController.ChangeState(GameState.PauseMenu);
        }
    }

    private void HandleProgression()
    {
        foreach (KeyItem key in _map.Keys.Where(k => !k.IsCollected))
        {
            if (Vector2.DistanceSquared(_player.Position, key.Position) > 22f * 22f) continue;

            key.Collect();
            if (key.Type == KeyType.Silver) _player.HasSilverKey = true;
            if (key.Type == KeyType.Gold) _player.HasGoldKey = true;
            Raylib.PlaySound(_textures.InteractionSound);
            Console.WriteLine($"[Progression] Picked up {key.Type} key.");
        }

        foreach (DoorEntity door in _map.Doors.Where(d => d.IsLocked))
        {
            if (Vector2.DistanceSquared(_player.Position, door.Position) > 28f * 28f) continue;

            bool hasKey = door.Type == KeyType.Silver ? _player.HasSilverKey : _player.HasGoldKey;
            if (!hasKey)
            {
                _statusText = door.Type == KeyType.Silver ? "Need Silver Key" : "Need Gold Key";
                _statusTextTimer = 1.0f;
                continue;
            }

            door.Unlock();
            Raylib.PlaySound(_textures.InteractionSound);
            Console.WriteLine($"[Progression] Unlocked {door.Type} door.");
        }

        if (_map.Exit is null) return;
        if (_levelTimer < 1.0f) return;
        if (Vector2.DistanceSquared(_player.Position, new Vector2(_map.Exit.X, _map.Exit.Y)) > 40f * 40f) return;

        if (_map.Exit.RequiresAllKeys && _map.Keys.Any(k => !k.IsCollected))
        {
            _statusText = "Find all keys first";
            _statusTextTimer = 1.2f;
            return;
        }

        _phase = GameplayPhase.Victory;
        Console.WriteLine("[Progression] Player escaped dungeon.");
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
        Raylib.DrawText($"Silver Key: {(_player.HasSilverKey ? "Yes" : "No")}", 16, 42, 20, _player.HasSilverKey ? Color.SkyBlue : Color.Gray);
        Raylib.DrawText($"Gold Key: {(_player.HasGoldKey ? "Yes" : "No")}", 16, 64, 20, _player.HasGoldKey ? Color.Gold : Color.Gray);

        if (_statusTextTimer > 0f)
        {
            Raylib.DrawText(_statusText, 16, 90, 20, Color.Orange);
        }

        if (_player.DamageFlashAmount > 0f)
        {
            Color flash = Raylib.ColorAlpha(Color.Red, 0.38f * _player.DamageFlashAmount);
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), flash);
        }

        _weaponRenderer.Draw();

        if (_phase == GameplayPhase.Victory)
        {
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(0, 0, 0, 196));
            Raylib.DrawText("YOU ESCAPED", Raylib.GetScreenWidth() / 2 - 140, Raylib.GetScreenHeight() / 2 - 20, 48, Color.Lime);
            Raylib.DrawText("Press ESC to return to menu", Raylib.GetScreenWidth() / 2 - 150, Raylib.GetScreenHeight() / 2 + 34, 24, Color.White);
        }
    }

    public void Dispose() => _textures.Dispose();
}
