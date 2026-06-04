using UnityEngine;

/// <summary>
/// Central hub for a single enemy. Owns the state machine and exposes the
/// Movement / Attack / Sensor / Data / Target properties that states read.
/// Wiring is done through Initialize so the same Prefab works both with
/// EnemyFactory (runtime spawn) and manual scene placement.
/// </summary>
public class EnemyController : MonoBehaviour
{
    // --- Component references resolved in Awake ---
    public EnemyMovement Movement  { get; private set; }
    public EnemyAttack   Attack    { get; private set; }
    public EnemySensor   Sensor    { get; private set; }
    public Health        Health    { get; private set; }

    // --- Runtime data set by Initialize ---
    public EnemyData     Data      { get; private set; }
    public Transform     Target    { get; private set; }

    // --- State instances (created in Initialize) ---
    public EnemyIdleState   IdleState   { get; private set; }
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState  ChaseState  { get; private set; }
    public EnemyAttackState AttackState { get; private set; }
    public EnemyDeadState   DeadState   { get; private set; }

    EnemyStateMachine stateMachine;
    bool initialized;

    void Awake()
    {
        CacheComponents();
    }

    /// <summary>
    /// Called by EnemyFactory (or a scene-placement helper) to inject runtime deps.
    /// Safe to call multiple times; subsequent calls re-initialize with new data.
    /// </summary>
    public bool Initialize(EnemyData data, Transform target, IBulletPool bulletPool)
    {
        if (data == null)
        {
            GameLog.Error(this, "EnemyData is null; cannot initialize enemy.");
            return false;
        }

        if (Health != null)
            Health.OnDied -= OnDied;

        initialized = false;
        Data   = data;
        Target = target;

        EnsureRuntimeComponents();

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

        Health.Initialize(data.maxHp);

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

    void OnDestroy()
    {
        if (Health != null)
            Health.OnDied -= OnDied;
    }

    void BuildStateMachine()
    {
        IdleState   = new EnemyIdleState(this);
        PatrolState = new EnemyPatrolState(this);
        ChaseState  = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this);
        DeadState   = new EnemyDeadState(this);

        stateMachine = new EnemyStateMachine();
        stateMachine.ChangeState(IdleState);
    }

    void CacheComponents()
    {
        Movement = GetComponent<EnemyMovement>();
        Attack   = GetComponent<EnemyAttack>();
        Sensor   = GetComponent<EnemySensor>();
        Health   = GetComponent<Health>();
    }

    void EnsureRuntimeComponents()
    {
        CacheComponents();

        if (GetComponent<TeamAffiliation>() == null)
            gameObject.AddComponent<TeamAffiliation>();

        if (Health == null)
            Health = gameObject.AddComponent<Health>();

        if (Sensor == null)
            Sensor = gameObject.AddComponent<EnemySensor>();
    }

    public void ChangeState(EnemyState next)
    {
        stateMachine?.ChangeState(next);
    }

    void OnDied()
    {
        ChangeState(DeadState);
    }

    void Update()
    {
        if (!initialized) return;
        stateMachine.Tick(Time.deltaTime);
    }
}
