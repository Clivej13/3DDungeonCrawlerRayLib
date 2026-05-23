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
    private readonly AudioManager _audio;
    private readonly DoorSystem _doorSystem;
    private string _doorPrompt = string.Empty;
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
        _map = new DungeonMap("Assets/Maps/level1.json", _textures.GoblinTexture, _textures.KeyTexture);
        _player = new PlayerController(_map);
        _renderer = new RaycastRenderer(
            _map,
            _textures.DungeonTexture,
            _textures.ClosedDoorTexture,
            _textures.OpenDoorTexture,
            _textures.MinimapLockTexture,
            _textures.MinimapTickTexture);
        _weaponRenderer = new WeaponRenderer(_textures.PlayerAnimationsTexture);
        _audio = new AudioManager();
        _doorSystem = new DoorSystem(_map);
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

            ResolveEnemySeparation();

            HandleMeleeCombat();
            HandleEnemyCombat();
            HandleProgression();
            _doorSystem.Update(deltaTime, _player, _map.Enemies);
            _audio.Update(deltaTime, _player, _map.Enemies.OfType<GoblinEnemy>());
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
            _player.AddKey(key.Id);
            Raylib.PlaySound(_audio.KeyPickupSound);
            Console.WriteLine($"[Progression] Picked up key '{key.Id}'.");
        }

        _doorPrompt = string.Empty;
        DoorEntity? targetedDoor = _doorSystem.GetTargetedDoor(_player);
        if (targetedDoor is not null)
        {
            bool hasKey = _player.HasKey(targetedDoor.RequiredKeyId);
            _doorPrompt = targetedDoor.State switch
            {
                DoorState.Closed => "Press E to Open Door",
                DoorState.Locked when !hasKey => "Locked Door",
                DoorState.Locked when hasKey => "Press E to Unlock Door",
                _ => string.Empty
            };

            bool interactPressed = Raylib.IsKeyPressed(KeyboardKey.E);
            if (interactPressed)
            {
                bool didInteract = _doorSystem.TryInteract(_player, targetedDoor, out string? status);
                if (didInteract)
                {
                    Raylib.PlaySound(_audio.InteractionSound);
                }
                else if (!string.IsNullOrWhiteSpace(status))
                {
                    _statusText = status!;
                    _statusTextTimer = 1.0f;
                }
            }
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

    private void ResolveEnemySeparation()
    {
        List<Enemy> aliveEnemies = _map.Enemies.Where(e => e.IsAlive).ToList();

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy a = aliveEnemies[i];
            for (int j = i + 1; j < aliveEnemies.Count; j++)
            {
                Enemy b = aliveEnemies[j];
                Vector2 delta = b.Position - a.Position;
                float distSq = delta.LengthSquared();
                float minDist = a.Radius + b.Radius;

                if (distSq >= minDist * minDist) continue;

                float dist = MathF.Sqrt(MathF.Max(distSq, 0.0001f));
                Vector2 normal = delta / dist;
                float overlap = minDist - dist;
                Vector2 correction = normal * (overlap * 0.5f);

                Vector2 newAPos = a.Position - correction;
                Vector2 newBPos = b.Position + correction;

                if (!HitsWorldCollision(a, newAPos)) a.SetPosition(newAPos);
                if (!HitsWorldCollision(b, newBPos)) b.SetPosition(newBPos);
            }
        }
    }

    private bool HitsWorldCollision(Enemy enemy, Vector2 position)
    {
        float radius = enemy.Radius;
        return _map.IsWallAtWorld(position.X - radius, position.Y - radius)
            || _map.IsWallAtWorld(position.X + radius, position.Y - radius)
            || _map.IsWallAtWorld(position.X - radius, position.Y + radius)
            || _map.IsWallAtWorld(position.X + radius, position.Y + radius);
    }

    public void Draw()
    {
        Raylib.DisableCursor();
        _renderer.Draw(_player);
        _renderer.DrawMinimap(_player);

        int hudPanelX = 14;
        int hudPanelY = Raylib.GetScreenHeight() - 158;
        int hudPanelW = 380;
        int hudPanelH = 142;
        Raylib.DrawRectangle(hudPanelX, hudPanelY, hudPanelW, hudPanelH, new Color(10, 12, 16, 190));
        Raylib.DrawRectangleLines(hudPanelX, hudPanelY, hudPanelW, hudPanelH, new Color(120, 128, 144, 220));

        Raylib.DrawText($"HP: {_player.Health:0}", hudPanelX + 12, hudPanelY + 10, 24, _player.Health > 25 ? Color.Lime : Color.Red);
        Raylib.DrawText($"Keys Collected: {_player.CollectedKeyIds.Count}", hudPanelX + 12, hudPanelY + 42, 20, _player.CollectedKeyIds.Count > 0 ? Color.Gold : Color.Gray);
        Raylib.DrawText($"Remaining Keys: {_map.Keys.Count(k => !k.IsCollected)}", hudPanelX + 12, hudPanelY + 66, 20, Color.LightGray);
        Raylib.DrawText("WASD Move | Mouse Look | ESC Pause", hudPanelX + 12, hudPanelY + 92, 18, Color.LightGray);
        Raylib.DrawText($"POS {_player.Position.X:0.0},{_player.Position.Y:0.0}  ANG {_player.Angle:0.00}  PITCH {_player.PitchOffset:0}",
            hudPanelX + 12, hudPanelY + 114, 16, new Color(190, 190, 190, 220));

        if (_statusTextTimer > 0f)
        {
            Raylib.DrawText(_statusText, hudPanelX + 12, hudPanelY - 24, 20, Color.Orange);
        }

        if (!string.IsNullOrWhiteSpace(_doorPrompt))
        {
            int promptW = Raylib.MeasureText(_doorPrompt, 24);
            int x = (Raylib.GetScreenWidth() - promptW) / 2;
            int y = Raylib.GetScreenHeight() - 68;
            Raylib.DrawRectangle(x - 12, y - 8, promptW + 24, 34, new Color(0, 0, 0, 170));
            Raylib.DrawText(_doorPrompt, x, y, 24, Color.RayWhite);
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

    public void Dispose()
    {
        _audio.Dispose();
        _textures.Dispose();
    }
}
