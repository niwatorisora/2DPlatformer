using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>敵の AI 状態と実行時依存を管理する中核。</summary>
public class EnemyController : MonoBehaviour
{
    public EnemyMovement Movement { get; private set; }
    public EnemyAttack Attack { get; private set; }
    public EnemySensor Sensor { get; private set; }
    public Health Health { get; private set; }
    public EnemyData Data { get; private set; }
    public Transform Target { get; private set; }
    public EnemyIdleState IdleState { get; private set; }
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }
    public EnemyDeadState DeadState { get; private set; }
    EnemyStateMachine stateMachine;
    SpriteRenderer spriteRenderer;
    Sprite originalSprite;
    IReadOnlyList<IEnemyKillListener> killListeners;
    Action<EnemyController> despawnCallback;
    bool initialized, originalSpriteCached;
    bool warnedAboutMissingDespawnCallback;
    void Awake() => CacheComponents();
    /// <summary>再利用時も安全に実行時依存と状態を再設定する。</summary>
    public bool Initialize(EnemyData data, Transform target, IBulletPool bulletPool,
        IReadOnlyList<IEnemyKillListener> injectedKillListeners,
        Action<EnemyController> injectedDespawnCallback = null, Sprite skinSprite = null)
    {
        if (data == null)
        {
            GameLog.Error(this, "EnemyData is null; cannot initialize enemy.");
            return false;
        }
        if (Health != null) Health.OnDied -= OnDied;
        initialized = false;
        Data   = data;
        Target = target;
        killListeners = injectedKillListeners;
        despawnCallback = injectedDespawnCallback;
        EnsureRuntimeComponents();
        ApplySkin(skinSprite);
        if (Movement == null)
        {
            GameLog.Error(this, $"{data.name} prefab requires an EnemyMovement component to define movement behavior.");
            return false;
        }
        if (Attack == null)
        {
            GameLog.Error(this, $"{data.name} prefab requires an EnemyAttack component to define attack behavior.");
            return false;
        }
        // Pool 再利用では HP、AI 状態、物理速度を前回の個体から持ち越さない。
        Health.Initialize(data.maxHp);
        ResetPhysicsState();
        var team = GetComponent<TeamAffiliation>();
        TeamId teamId = data.teamId;
        team.SetTeam(teamId);
        Movement.Configure(data.moveSpeed);
        Attack.Configure(bulletPool, teamId);
        Sensor.Configure(target, data.detectionRange, data.loseSightRange);
        BuildStateMachine();
        Health.OnDied += OnDied;
        initialized = true;
        return true;
    }

    void OnDestroy() { if (Health != null) Health.OnDied -= OnDied; }
    void BuildStateMachine()
    {
        IdleState   = new EnemyIdleState(this);
        PatrolState = new EnemyPatrolState(this);
        ChaseState  = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this);
        DeadState = new EnemyDeadState(this);
        stateMachine = new EnemyStateMachine();
        stateMachine.ChangeState(IdleState);
    }
    void CacheComponents()
    {
        Movement = GetComponent<EnemyMovement>();
        Attack   = GetComponent<EnemyAttack>();
        Sensor   = GetComponent<EnemySensor>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        Health = GetComponent<Health>();
    }
    void EnsureRuntimeComponents()
    {
        CacheComponents();
        if (GetComponent<TeamAffiliation>() == null) gameObject.AddComponent<TeamAffiliation>();
        if (Health == null) Health = gameObject.AddComponent<Health>();
        if (Sensor == null) Sensor = gameObject.AddComponent<EnemySensor>();
    }
    void ApplySkin(Sprite skinSprite)
    {
        if (spriteRenderer == null) return;
        if (!originalSpriteCached)
        {
            originalSprite = spriteRenderer.sprite;
            originalSpriteCached = true;
        }
        spriteRenderer.sprite = skinSprite != null ? skinSprite : originalSprite;
    }

    public void ChangeState(EnemyState next) => stateMachine?.ChangeState(next);

    public void Despawn()
    {
        initialized = false;
        Movement?.Stop();
        if (despawnCallback != null)
        {
            despawnCallback(this);
            return;
        }
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
        for (int i = 0; i < killListeners.Count; i++)
        {
            var listener = killListeners[i];
            if (listener is UnityEngine.Object unityObject && unityObject == null) continue;
            listener?.OnEnemyKilled(scoreValue);
        }
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
