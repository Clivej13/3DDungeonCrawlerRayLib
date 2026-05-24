using DungeonCrawler.Entities;
using DungeonCrawler.Player;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Rendering;

public sealed class AudioManager : IDisposable
{
    public Sound InteractionSound { get; }
    public Sound KeyPickupSound { get; }
    public Sound PlayerFootstepsSound { get; }
    public Sound GoblinFootstepsSound { get; }
    public Sound GoblinGrowlSound { get; }
    public Sound GoblinMutterSound { get; }

    private readonly Random _rng = new();
    private float _goblinAmbientCooldown;
    private float _playerFootstepCooldown;
    private float _goblinFootstepCooldown;
    private const float GoblinHearingDistance = 220f;

    public AudioManager()
    {
        if (!Raylib.IsAudioDeviceReady()) Raylib.InitAudioDevice();
        InteractionSound = Raylib.LoadSound("Assets/Sounds/open_door.wav");
        KeyPickupSound = Raylib.LoadSound("Assets/Sounds/pick_up_key.ogg");
        PlayerFootstepsSound = Raylib.LoadSound("Assets/Sounds/quiet_footsteps.wav");
        GoblinFootstepsSound = Raylib.LoadSound("Assets/Sounds/loud_footsteps.wav");
        GoblinGrowlSound = Raylib.LoadSound("Assets/Sounds/goblin_growl.wav");
        GoblinMutterSound = Raylib.LoadSound("Assets/Sounds/goblin_mutter.wav");

        Raylib.SetSoundVolume(PlayerFootstepsSound, 0.55f);
        Raylib.SetSoundVolume(GoblinFootstepsSound, 0.75f);
        _playerFootstepCooldown = 0f;
        _goblinFootstepCooldown = 0f;
        ResetGoblinAmbientCooldown();
    }

    public void Update(float deltaTime, PlayerController player, IEnumerable<GoblinEnemy> goblins)
    {
        _playerFootstepCooldown = MathF.Max(0f, _playerFootstepCooldown - deltaTime);
        if (player.IsMoving && _playerFootstepCooldown <= 0f && !Raylib.IsSoundPlaying(PlayerFootstepsSound))
        {
            Raylib.PlaySound(PlayerFootstepsSound);
            _playerFootstepCooldown = 0.42f;
        }

        _goblinFootstepCooldown = MathF.Max(0f, _goblinFootstepCooldown - deltaTime);
        GoblinEnemy? nearestChasing = goblins
            .Where(g => g.IsAlive && g.CombatState == EnemyCombatState.Chasing)
            .OrderBy(g => Vector2.DistanceSquared(g.Position, player.Position))
            .FirstOrDefault();

        if (nearestChasing is not null && _goblinFootstepCooldown <= 0f && !Raylib.IsSoundPlaying(GoblinFootstepsSound))
        {
            float dist = Vector2.Distance(nearestChasing.Position, player.Position);
            float attenuation = 1f - Math.Clamp(dist / GoblinHearingDistance, 0f, 1f);
            float volume = 0.20f + (attenuation * 0.75f);
            Raylib.SetSoundVolume(GoblinFootstepsSound, volume);
            Raylib.PlaySound(GoblinFootstepsSound);
            _goblinFootstepCooldown = 0.55f;
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
        if (Raylib.IsSoundPlaying(PlayerFootstepsSound)) Raylib.StopSound(PlayerFootstepsSound);
        if (Raylib.IsSoundPlaying(GoblinFootstepsSound)) Raylib.StopSound(GoblinFootstepsSound);
        Raylib.UnloadSound(InteractionSound);
        Raylib.UnloadSound(KeyPickupSound);
        Raylib.UnloadSound(PlayerFootstepsSound);
        Raylib.UnloadSound(GoblinFootstepsSound);
        Raylib.UnloadSound(GoblinGrowlSound);
        Raylib.UnloadSound(GoblinMutterSound);
    }
}
