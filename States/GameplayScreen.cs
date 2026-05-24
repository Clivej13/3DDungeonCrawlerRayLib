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
    private enum GameplayPhase { Playing, Victory, Dead }

    private readonly GameStateController _stateController;
    private readonly DungeonMap _map;
    private readonly PlayerController _player;
    private readonly PlayerCombat _playerCombat;
    private readonly TextureManager _textures;
    private readonly RaycastRenderer _renderer;
    private readonly WeaponRenderer _weaponRenderer;
    private readonly AudioManager _audio;
    private readonly DoorSystem _doorSystem;
    private readonly GameOptions _options;
    private string _doorPrompt = string.Empty;
    private GameplayPhase _phase = GameplayPhase.Playing;
    private float _levelTimer;
    private float _statusTextTimer;
    private string _statusText = string.Empty;
    private string _lastEnemyAttackResult = "none";

    public GameplayScreen(GameStateController stateController, GameOptions options)
    {
        _stateController = stateController;
        _options = options;
        _textures = new TextureManager();
        _map = new DungeonMap("Assets/Maps/level1.json", _textures.GoblinTexture, _textures.GoblinLightWindupTexture, _textures.GoblinHeavyWindupTexture, _textures.GoblinStrikeTexture, _textures.GoblinStaggerTexture, _textures.KeyTexture);
        _player = new PlayerController(_map, _options);
        _playerCombat = new PlayerCombat();
        _renderer = new RaycastRenderer(_map, _textures.DungeonTexture, _textures.ClosedDoorTexture, _textures.OpenDoorTexture, _textures.MinimapLockTexture, _textures.MinimapTickTexture);
        _weaponRenderer = new WeaponRenderer(_textures.PlayerAnimationsTexture);
        _audio = new AudioManager();
        _doorSystem = new DoorSystem(_map, _options);
    }

    public void Update(InputHandler input, float deltaTime)
    {
        _levelTimer += deltaTime;
        _statusTextTimer = MathF.Max(0f, _statusTextTimer - deltaTime);
        if (_phase == GameplayPhase.Playing)
        {
            _player.Update(deltaTime, _map.Enemies);
            _playerCombat.Update(deltaTime, _player, _map, _map.Enemies);
            _weaponRenderer.Update(deltaTime);

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && !_playerCombat.IsBlocking && _playerCombat.TryStartAttack()) _weaponRenderer.TriggerAttack();
            if (Raylib.IsKeyPressed(KeyboardKey.Space)) _playerCombat.TryStartDodge(_player.MoveInputDirection, _player.Angle);
            if (Raylib.IsMouseButtonDown(MouseButton.Right)) _playerCombat.TryStartBlock();
            if (Raylib.IsMouseButtonReleased(MouseButton.Right)) _playerCombat.StopBlock();

            foreach (Enemy enemy in _map.Enemies)
            {
                enemy.DistanceToPlayer = Vector2.Distance(enemy.Position, _player.Position);
                enemy.Update(deltaTime, _player.Position, _map);
            }

            ResolveEnemySeparation();
            HandleMeleeCombat();
            HandleEnemyCombat();
            if (!_player.IsAlive) _phase = GameplayPhase.Dead;
            HandleProgression();
            _doorSystem.Update(deltaTime, _player, _map.Enemies);
            _audio.Update(deltaTime, _player, _map.Enemies.OfType<GoblinEnemy>());
            _map.Enemies.RemoveAll(e => !e.IsAlive);
        }
        else _weaponRenderer.Update(deltaTime);

        if (input.BackPressed()) { Raylib.EnableCursor(); _stateController.ChangeState(GameState.PauseMenu); }
    }

    private void HandleMeleeCombat()
    {
        if (!_playerCombat.IsAttackActiveFrame) return;
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
            enemy.TakeDamage(25f, dir);
            _playerCombat.MarkAttackHit();
            break;
        }
    }

    private void HandleEnemyCombat()
    {
        foreach (GoblinEnemy goblin in _map.Enemies.OfType<GoblinEnemy>().Where(g => g.IsAlive && g.CanDamagePlayerThisFrame))
        {
            if (!goblin.IsAttackValid(_player.Position, _map))
            {
                _lastEnemyAttackResult = "missed";
                continue;
            }
            if (_playerCombat.IsBlocking && _playerCombat.TryBlockHit(goblin.CurrentAttackType == GoblinAttackType.Heavy, _player))
            {
                goblin.ApplyBlockStagger(goblin.CurrentBlockStaggerDuration);
                _lastEnemyAttackResult = "blocked";
                continue;
            }
            if (_playerCombat.IsDodgeInvulnerable)
            {
                _lastEnemyAttackResult = "dodged";
                continue;
            }
            _player.TryTakeDamage(goblin.CurrentAttackDamage);
            _lastEnemyAttackResult = "hit";
        }
    }

    private void HandleProgression(){/* unchanged */
        foreach (KeyItem key in _map.Keys.Where(k => !k.IsCollected)) if (Vector2.DistanceSquared(_player.Position, key.Position) <= 22f*22f){ key.Collect(); _player.AddKey(key.Id); Raylib.PlaySound(_audio.KeyPickupSound);}        
        _doorPrompt = string.Empty; DoorEntity? targetedDoor = _doorSystem.GetTargetedDoor(_player); if (targetedDoor is not null){ bool hasKey=_player.HasKey(targetedDoor.RequiredKeyId); _doorPrompt=targetedDoor.State switch{ DoorState.Closed=>"Press E to Open Door", DoorState.Locked when !hasKey=>"Locked Door", DoorState.Locked when hasKey=>"Press E to Unlock Door", DoorState.Open=>"Press E to Close Door", _=>string.Empty}; if (Raylib.IsKeyPressed(KeyboardKey.E)){ bool didInteract=_doorSystem.TryInteract(_player,targetedDoor,out string? status); if (didInteract) Raylib.PlaySound(_audio.InteractionSound); else if(!string.IsNullOrWhiteSpace(status)){_statusText=status!; _statusTextTimer=1f;}}}
        if (_map.Exit is null || _levelTimer < 1f || Vector2.DistanceSquared(_player.Position, new Vector2(_map.Exit.X, _map.Exit.Y)) > 40f*40f) return;
        if (_map.Exit.RequiresAllKeys && _map.Keys.Any(k=>!k.IsCollected)){_statusText="Find all keys first"; _statusTextTimer=1.2f; return;} _phase=GameplayPhase.Victory;
    }

    private void ResolveEnemySeparation(){ var alive=_map.Enemies.Where(e=>e.IsAlive).ToList(); for(int i=0;i<alive.Count;i++) for(int j=i+1;j<alive.Count;j++){Enemy a=alive[i]; Enemy b=alive[j]; Vector2 d=b.Position-a.Position; float ds=d.LengthSquared(); float md=a.Radius+b.Radius; if(ds>=md*md) continue; float dist=MathF.Sqrt(MathF.Max(ds,0.0001f)); Vector2 n=d/dist; float overlap=md-dist; Vector2 corr=n*(overlap*0.5f); Vector2 na=a.Position-corr; Vector2 nb=b.Position+corr; if(!HitsWorldCollision(a,na)) a.SetPosition(na); if(!HitsWorldCollision(b,nb)) b.SetPosition(nb);} }
    private bool HitsWorldCollision(Enemy enemy, Vector2 position){ float r=enemy.Radius; return _map.IsBlockedAtWorld(position.X-r,position.Y-r)||_map.IsBlockedAtWorld(position.X+r,position.Y-r)||_map.IsBlockedAtWorld(position.X-r,position.Y+r)||_map.IsBlockedAtWorld(position.X+r,position.Y+r);}    

    public void Draw()
    {
        Raylib.DisableCursor(); _renderer.Draw(_player); _renderer.DrawMinimap(_player); _weaponRenderer.Draw();
        Raylib.DrawText($"HP: {_player.Health:0}", 18, Raylib.GetScreenHeight()-120, 24, Color.Lime);
        Raylib.DrawText($"Stamina: {_playerCombat.Stamina:0}", 18, Raylib.GetScreenHeight()-92, 22, _playerCombat.Stamina > 25f ? Color.SkyBlue : Color.Orange);
        if (_options.CombatDebugOverlayEnabled) DrawCombatDebug();
        if (_phase == GameplayPhase.Dead)
        {
            const string deathText = "You Died";
            const string hintText = "Press ESC for Pause Menu";
            int centerX = Raylib.GetScreenWidth() / 2;
            int centerY = Raylib.GetScreenHeight() / 2;
            int deathWidth = Raylib.MeasureText(deathText, 56);
            int hintWidth = Raylib.MeasureText(hintText, 24);
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(0, 0, 0, 145));
            Raylib.DrawText(deathText, centerX - deathWidth / 2, centerY - 36, 56, Color.Red);
            Raylib.DrawText(hintText, centerX - hintWidth / 2, centerY + 34, 24, Color.LightGray);
        }
    }

    private void DrawCombatDebug()
    {
        int x = 12, y = 12;
        Raylib.DrawRectangle(x, y, 560, 230, new Color(0,0,0,170));
        Raylib.DrawText($"HP: {_player.Health:0.0}", x+10, y+10, 18, Color.White);
        Raylib.DrawText($"AttackState: {_playerCombat.AttackState}", x+10, y+34, 18, Color.White);
        Raylib.DrawText($"Blocking: {_playerCombat.IsBlocking}", x+10, y+58, 18, Color.White);
        Raylib.DrawText($"DodgeCD: {_playerCombat.DodgeCooldownRemaining:0.00}  iFrames:{_playerCombat.IsDodgeInvulnerable}", x+10, y+82, 18, Color.White);
        Raylib.DrawText($"Stamina: {_playerCombat.Stamina:0.0}", x+10, y+106, 18, Color.White);
        GoblinEnemy? g = _map.Enemies.OfType<GoblinEnemy>().FirstOrDefault();
        if (g is not null){ Raylib.DrawText($"EnemyState: {g.CombatState} Timer:{g.StateTimer:0.00}", x+10, y+138, 18, Color.Yellow); Raylib.DrawText($"EnemyAttackType:{g.CurrentAttackType} LOS:{g.HasLineOfSightToPlayer}", x+10, y+162, 18, Color.Yellow);}
        Raylib.DrawText($"Last Enemy Result: {_lastEnemyAttackResult}", x+10, y+190, 18, Color.Orange);
    }

    public void Dispose(){ _audio.Dispose(); _textures.Dispose(); }
}
