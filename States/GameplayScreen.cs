using DungeonCrawler.Core;
using DungeonCrawler.Input;
using DungeonCrawler.Player;
using DungeonCrawler.Rendering;
using DungeonCrawler.World;
using Raylib_cs;

namespace DungeonCrawler.States;

public sealed class GameplayScreen : IDisposable
{
    private readonly GameStateController _stateController;
    private readonly DungeonMap _map;
    private readonly PlayerController _player;
    private readonly TextureManager _textures;
    private readonly RaycastRenderer _renderer;
    private readonly WeaponRenderer _weaponRenderer;

    public GameplayScreen(GameStateController stateController)
    {
        _stateController = stateController;
        _map = new DungeonMap();
        _player = new PlayerController(_map);
        _textures = new TextureManager();
        _renderer = new RaycastRenderer(_map, _textures.DungeonTexture);
        _weaponRenderer = new WeaponRenderer(_textures.PlayerAnimationsTexture);
    }

    public void Update(InputHandler input, float deltaTime)
    {
        _player.Update(deltaTime);
        _weaponRenderer.Update(deltaTime);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            _weaponRenderer.TriggerAttack();
        }

        if (input.BackPressed())
        {
            Raylib.EnableCursor();
            _stateController.ChangeState(GameState.PauseMenu);
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
        _weaponRenderer.Draw();
    }

    public void Dispose()
    {
        _textures.Dispose();
    }
}
