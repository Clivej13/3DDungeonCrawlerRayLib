using DungeonCrawler.Entities;
using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Player;

public enum PlayerAttackState
{
    Ready,
    Windup,
    Active,
    Recovery
}

public sealed class PlayerCombat
{
    public const float MaxStamina = 100f;
    public const float DodgeStaminaCost = 25f;
    public const float AttackStaminaCost = 8f;
    public const float StaminaRegenDelay = 0.75f;
    public const float StaminaRegenPerSecond = 25f;

    public const float DodgeDuration = 0.25f;
    public const float DodgeInvulnerabilityDuration = 0.1f;
    public const float DodgeCooldown = 0.45f;
    public const float DodgeDistance = 64f;

    public const float AttackWindup = 0.15f;
    public const float AttackActive = 0.15f;
    public const float AttackRecovery = 0.35f;
    public const float LightBlockStaminaCost = 15f;
    public const float LightBlockChipDamage = 2f;
    public const float HeavyBlockStaminaCost = 40f;
    public const float HeavyBlockChipDamage = 12f;

    public float Stamina { get; private set; } = MaxStamina;
    public float DodgeCooldownRemaining => _dodgeCooldownTimer;
    public bool IsDodging => _dodgeTimer > 0f;
    public bool IsDodgeInvulnerable => _dodgeTimer > 0f && _dodgeElapsed <= DodgeInvulnerabilityDuration;
    public bool IsBlocking { get; private set; }
    public PlayerAttackState AttackState { get; private set; } = PlayerAttackState.Ready;
    public bool DidAttackHit { get; private set; }
    public bool IsAttackActiveFrame => AttackState == PlayerAttackState.Active && !DidAttackHit;

    private float _staminaRegenDelayTimer;
    private float _dodgeCooldownTimer;
    private float _dodgeTimer;
    private float _dodgeElapsed;
    private Vector2 _dodgeDirection;
    private float _attackTimer;

    public void Update(float dt, PlayerController player, DungeonMap map, IEnumerable<Enemy> enemies)
    {
        _dodgeCooldownTimer = MathF.Max(0f, _dodgeCooldownTimer - dt);
        TickStamina(dt);
        TickDodge(dt, player, map, enemies);
        TickAttack(dt);
    }

    public bool TryStartAttack()
    {
        if (IsBlocking || AttackState != PlayerAttackState.Ready || Stamina < AttackStaminaCost) return false;
        SpendStamina(AttackStaminaCost);
        AttackState = PlayerAttackState.Windup;
        _attackTimer = AttackWindup;
        DidAttackHit = false;
        return true;
    }

    public void MarkAttackHit() => DidAttackHit = true;

    public bool TryStartDodge(Vector2 inputDir, float playerAngle)
    {
        if (IsBlocking || _dodgeCooldownTimer > 0f || IsDodging || Stamina < DodgeStaminaCost) return false;

        Vector2 facing = new(MathF.Cos(playerAngle), MathF.Sin(playerAngle));
        _dodgeDirection = inputDir.LengthSquared() > 0.0001f ? Vector2.Normalize(inputDir) : -facing;
        _dodgeTimer = DodgeDuration;
        _dodgeElapsed = 0f;
        _dodgeCooldownTimer = DodgeCooldown;
        SpendStamina(DodgeStaminaCost);
        return true;
    }

    public bool TryStartBlock()
    {
        if (IsDodging || AttackState != PlayerAttackState.Ready) return false;
        IsBlocking = true;
        return true;
    }

    public void StopBlock() => IsBlocking = false;

    public bool TryBlockHit(bool isHeavyAttack, PlayerController player)
    {
        if (!IsBlocking || !player.IsAlive) return false;
        float staminaCost = isHeavyAttack ? HeavyBlockStaminaCost : LightBlockStaminaCost;
        float chipDamage = isHeavyAttack ? HeavyBlockChipDamage : LightBlockChipDamage;
        SpendStamina(staminaCost);
        player.TryTakeDamage(chipDamage);
        return true;
    }

    private void TickAttack(float dt)
    {
        if (AttackState == PlayerAttackState.Ready) return;
        _attackTimer -= dt;
        if (_attackTimer > 0f) return;

        switch (AttackState)
        {
            case PlayerAttackState.Windup:
                AttackState = PlayerAttackState.Active;
                _attackTimer = AttackActive;
                break;
            case PlayerAttackState.Active:
                AttackState = PlayerAttackState.Recovery;
                _attackTimer = AttackRecovery;
                break;
            default:
                AttackState = PlayerAttackState.Ready;
                _attackTimer = 0f;
                DidAttackHit = false;
                break;
        }
    }

    private void TickDodge(float dt, PlayerController player, DungeonMap map, IEnumerable<Enemy> enemies)
    {
        if (_dodgeTimer <= 0f) return;

        _dodgeTimer = MathF.Max(0f, _dodgeTimer - dt);
        _dodgeElapsed += dt;
        float dodgeSpeed = DodgeDistance / DodgeDuration;
        Vector2 desired = player.Position + _dodgeDirection * dodgeSpeed * dt;
        player.TryMoveTo(desired, map, enemies);
    }

    private void TickStamina(float dt)
    {
        _staminaRegenDelayTimer = MathF.Max(0f, _staminaRegenDelayTimer - dt);
        if (_staminaRegenDelayTimer > 0f || IsDodging || IsBlocking || Stamina >= MaxStamina) return;
        Stamina = MathF.Min(MaxStamina, Stamina + StaminaRegenPerSecond * dt);
    }

    private void SpendStamina(float amount)
    {
        Stamina = MathF.Max(0f, Stamina - amount);
        _staminaRegenDelayTimer = StaminaRegenDelay;
    }
}
