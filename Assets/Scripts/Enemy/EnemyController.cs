using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>敵の AI 状態と実行時依存を管理する中核。</summary>
public partial class EnemyController : MonoBehaviour
{
    public EnemyMovement Movement { get; private set; } public EnemyAttack Attack { get; private set; }
    public EnemySensor Sensor { get; private set; } public Health Health { get; private set; }
    public EnemyData Data { get; private set; } public Transform Target { get; private set; }
    public float AttackRange { get; private set; }
    public bool UsesContactAttackProfile { get; private set; }
    public EnemyIdleState IdleState { get; private set; } public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; } public EnemyAttackState AttackState { get; private set; }
    public EnemyDeadState DeadState { get; private set; }
    EnemyStateMachine stateMachine;
    SpriteRenderer spriteRenderer; Collider2D enemyCollider; Transform visualRoot; Sprite originalSprite;
    Vector3 originalVisualRootScale, originalVisualRootLocalPosition;
    IReadOnlyList<IEnemyKillListener> killListeners; Action<EnemyController> despawnCallback;
    bool initialized, originalSpriteCached, originalVisualRootScaleCached;
    bool warnedAboutMissingDespawnCallback, warnedAboutUnsupportedAlignmentCollider;
    EnemyMovement[] movementComponents;
    EnemyAttack[] attackComponents;
    void Awake() => CacheComponents();
    /// <summary>再利用時も安全に実行時依存と状態を再設定する。</summary>
    public bool Initialize(EnemyData data, Transform target, IBulletPool bulletPool,
        IReadOnlyList<IEnemyKillListener> injectedKillListeners,
        Action<EnemyController> injectedDespawnCallback = null, Sprite skinSprite = null,
        float skinScale = 1f, MovementProfile movementProfile = null, AttackProfile attackProfile = null)
    {
        if (data == null) { GameLog.Error(this, "EnemyData is null; cannot initialize enemy."); return false; }
        if (Health != null) Health.OnDied -= OnDied;
        initialized = false;
        Data = data; Target = target; killListeners = injectedKillListeners;
        despawnCallback = injectedDespawnCallback;
        EnsureRuntimeComponents();
        ApplySkin(skinSprite, skinScale);
        if (!SelectMovement(data, movementProfile)) return false;
        if (!SelectAttack(data, attackProfile, bulletPool, data.teamId)) return false;
        // Pool 再利用では HP、AI 状態、物理速度を前回の個体から持ち越さない。
        Health.Initialize(data.maxHp);
        ResetPhysicsState();
        var team = GetComponent<TeamAffiliation>();
        TeamId teamId = data.teamId; team.SetTeam(teamId);
        Sensor.Configure(target, data.detectionRange, data.loseSightRange);
        BuildStateMachine();
        Health.OnDied += OnDied;
        initialized = true;
        return true;
    }

    void OnDestroy() { if (Health != null) Health.OnDied -= OnDied; }
    void BuildStateMachine()
    {
        IdleState = new EnemyIdleState(this); PatrolState = new EnemyPatrolState(this); ChaseState = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this); DeadState = new EnemyDeadState(this); stateMachine = new EnemyStateMachine();
        stateMachine.ChangeState(IdleState);
    }
    void CacheComponents()
    {
        movementComponents = GetComponents<EnemyMovement>();
        attackComponents = GetComponents<EnemyAttack>();
        Movement = movementComponents.Length > 0 ? movementComponents[0] : null;
        Attack = attackComponents.Length > 0 ? attackComponents[0] : null; Sensor = GetComponent<EnemySensor>();
        enemyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        visualRoot = spriteRenderer != null ? spriteRenderer.transform : null; Health = GetComponent<Health>();
    }
    void EnsureRuntimeComponents()
    {
        CacheComponents();
        if (GetComponent<TeamAffiliation>() == null) gameObject.AddComponent<TeamAffiliation>();
        if (Health == null) Health = gameObject.AddComponent<Health>();
        if (Sensor == null) Sensor = gameObject.AddComponent<EnemySensor>();
    }
    bool SelectMovement(EnemyData data, MovementProfile profile)
    {
        if (movementComponents == null || movementComponents.Length == 0) { GameLog.Error(this, $"{data.name} prefab requires an EnemyMovement component to define movement behavior."); return false; }
        EnemyMovement selected = profile == null ? movementComponents[0] : FindMovement(profile.Kind);
        if (selected == null) { GameLog.Error(this, $"{data.name} prefab is missing {GetMovementTypeName(profile.Kind)}; using {movementComponents[0].GetType().Name}."); selected = movementComponents[0]; }
        // 再利用時は旧実装を停止・無効化してから、新しい実装だけを有効にする。
        foreach (EnemyMovement movement in movementComponents) {
            movement.Stop(); movement.enabled = movement == selected; }
        Movement = selected;
        if (profile != null && MatchesProfile(selected, profile.Kind)) selected.Configure(profile);
        else selected.Configure(data.moveSpeed);
        return true;
    }
    bool SelectAttack(EnemyData data, AttackProfile profile, IBulletPool bulletPool, TeamId teamId)
    {
        if (attackComponents == null || attackComponents.Length == 0) { GameLog.Error(this, $"{data.name} prefab requires an EnemyAttack component to define attack behavior."); return false; }
        EnemyAttack selected = profile == null ? attackComponents[0] : FindAttack(profile.Kind);
        if (selected == null) { GameLog.Error(this, $"{data.name} prefab is missing {GetAttackTypeName(profile.Kind)}; using {attackComponents[0].GetType().Name}."); selected = attackComponents[0]; }
        foreach (EnemyAttack attack in attackComponents) attack.enabled = attack == selected;
        Attack = selected;
        // プロファイル指定時は攻撃種別にかかわらず、その接敵距離を使う。未指定時だけ旧 EnemyData を使う。
        AttackRange = profile != null ? profile.EngageRange : data.attackRange;
        UsesContactAttackProfile = profile != null && profile.Kind == AttackProfile.AttackKind.Contact;
        selected.Configure(profile, bulletPool, teamId);
        return true;
    }
    EnemyMovement FindMovement(MovementProfile.MovementKind kind)
    {
        foreach (EnemyMovement movement in movementComponents)
            if (MatchesProfile(movement, kind)) return movement;
        return null;
    }
    EnemyAttack FindAttack(AttackProfile.AttackKind kind)
    {
        foreach (EnemyAttack attack in attackComponents)
            if (kind == AttackProfile.AttackKind.Shooter ? attack is EnemyShooterAttack : attack is ContactDamageAttack) return attack;
        return null;
    }
    static string GetAttackTypeName(AttackProfile.AttackKind kind) => kind switch
    {
        AttackProfile.AttackKind.Shooter => nameof(EnemyShooterAttack),
        AttackProfile.AttackKind.Contact => nameof(ContactDamageAttack), _ => kind.ToString()
    };
    static bool MatchesProfile(EnemyMovement movement, MovementProfile.MovementKind kind) => kind switch
    {
        MovementProfile.MovementKind.Ground => movement.GetType() == typeof(EnemyGroundMovement),
        MovementProfile.MovementKind.JumpingGround => movement is EnemyJumpingGroundMovement,
        MovementProfile.MovementKind.Flying => movement is EnemyFlyingMovement,
        _ => false
    };
    static string GetMovementTypeName(MovementProfile.MovementKind kind) => kind switch
    {
        MovementProfile.MovementKind.Ground => nameof(EnemyGroundMovement),
        MovementProfile.MovementKind.JumpingGround => nameof(EnemyJumpingGroundMovement),
        MovementProfile.MovementKind.Flying => nameof(EnemyFlyingMovement), _ => kind.ToString()
    };
    public void ChangeState(EnemyState next) => stateMachine?.ChangeState(next);
    public void Despawn()
    {
        initialized = false;
        Movement?.Stop();
        if (despawnCallback != null) { despawnCallback(this); return; }
        if (!warnedAboutMissingDespawnCallback)
        {
            warnedAboutMissingDespawnCallback = true;
            GameLog.Warning(this, "No despawn callback was injected; destroying scene-placed enemy.");
        }
        Destroy(gameObject);
    }
    void OnDied()
    {
        NotifyKillListeners(Data != null ? Data.scoreValue : 0);
        AudioHelper.TryPlay(Data != null ? Data.deathSound : null);
        ChangeState(DeadState);
    }
    // 静的イベントではなく、WaveSpawner から受け取った通知先だけに知らせる。
    void NotifyKillListeners(int scoreValue)
    {
        if (killListeners == null) return;
        for (int i = 0; i < killListeners.Count; i++) {
            var listener = killListeners[i]; if (listener is UnityEngine.Object unityObject && unityObject == null) continue;
            listener?.OnEnemyKilled(scoreValue); }
    }
    void ResetPhysicsState()
    {
        var body = GetComponent<Rigidbody2D>();
        if (body == null) return;
        // Pool から戻った敵が前回の移動・ノックバック速度を持ち越さないようにする。
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }
    void Update() { if (initialized) stateMachine.Tick(Time.deltaTime); }
}
