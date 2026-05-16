using DungeonCrawler.Entities;
using DungeonCrawler.Player;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Rendering;

public sealed class AudioManager : IDisposable
{
    public Sound InteractionSound { get; }
    public Sound KeyPickupSound { get; }
    public Sound FootstepsLoop { get; }
    public Sound GoblinGrowlSound { get; }
    public Sound GoblinMutterSound { get; }

    private readonly Random _rng = new();
    private float _goblinAmbientCooldown;
    private const float GoblinHearingDistance = 220f;

    public AudioManager()
    {
        if (!Raylib.IsAudioDeviceReady()) Raylib.InitAudioDevice();
        InteractionSound = Raylib.LoadSound("Assets/Sounds/open_door.wav");
        KeyPickupSound = Raylib.LoadSound("Assets/Sounds/pick_up_key.ogg");
        FootstepsLoop = Raylib.LoadSound("Assets/Sounds/quiet_footsteps.wav");
        GoblinGrowlSound = Raylib.LoadSound("Assets/Sounds/goblin_growl.wav");
        GoblinMutterSound = Raylib.LoadSound("Assets/Sounds/goblin_mutter.wav");

        Raylib.SetSoundVolume(FootstepsLoop, 0.55f);
        ResetGoblinAmbientCooldown();
    }

    public void Update(float deltaTime, PlayerController player, IEnumerable<GoblinEnemy> goblins)
    {
        if (player.IsMoving)
        {
            if (!Raylib.IsSoundPlaying(FootstepsLoop))
            {
                Raylib.PlaySound(FootstepsLoop);
            }
        }
        else if (Raylib.IsSoundPlaying(FootstepsLoop))
        {
            Raylib.StopSound(FootstepsLoop);
        }

        _goblinAmbientCooldown -= deltaTime;
        if (_goblinAmbientCooldown > 0f) return;

        bool anyNearby = goblins.Any(g => g.IsAlive && Vector2.DistanceSquared(g.Position, player.Position) <= GoblinHearingDistance * GoblinHearingDistance);
        if (anyNearby)
        {
            if (_rng.NextDouble() < 0.5d) Raylib.PlaySound(GoblinGrowlSound);
            else Raylib.PlaySound(GoblinMutterSound);
        }

        ResetGoblinAmbientCooldown();
    }

    private void ResetGoblinAmbientCooldown() => _goblinAmbientCooldown = 6f + (float)_rng.NextDouble() * 9f;

    public void Dispose()
    {
        if (Raylib.IsSoundPlaying(FootstepsLoop)) Raylib.StopSound(FootstepsLoop);
        Raylib.UnloadSound(InteractionSound);
        Raylib.UnloadSound(KeyPickupSound);
        Raylib.UnloadSound(FootstepsLoop);
        Raylib.UnloadSound(GoblinGrowlSound);
        Raylib.UnloadSound(GoblinMutterSound);
    }
}
