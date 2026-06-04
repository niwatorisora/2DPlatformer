# AGENTS.md

This file is the shared instruction source for coding AI agents working on this project. Keep it concise and project-specific. Longer explanations belong in `docs/`.

## Project Overview

This is a Unity 6000.4.5f1 2D platformer prototype. The current gameplay focus is player movement, mouse-aimed shooting, pooled bullets, ScriptableObject-driven bullet/weapon data, team-based friendly-fire filtering, and a State/Factory-based enemy AI system.

Primary scene:
- `Assets/Scenes/SampleScene.unity`

Documentation index and detailed notes:
- `docs/project-overview.md` — index and quick summary
- `docs/architecture-overview.md` — directory layout, data flow, type-to-file map
- `docs/shooting.md` — BulletData / WeaponData / ShooterCore / BulletPool / Bullet
- `docs/combat.md` — Health / IDamageable / TeamAffiliation / hit rules
- `docs/enemy-ai.md` — EnemyData / EnemyController / states / movement / attack
- `docs/data-assets.md` — existing assets, naming conventions, how to add new ones

## Tech Stack

- Engine: Unity 6000.4.5f1
- Language: C#
- Render Pipeline: URP 2D
- Physics: `Rigidbody2D`, `Collider2D`, trigger callbacks
- Input: Unity legacy input APIs currently used by scripts
- Data authoring: ScriptableObject assets

## Architecture

- `Assets/Scripts/Player`
  - `PlayerMovement`: horizontal movement and jump.
  - `PlayerShooter`: mouse aiming, cooldown, spread, burst/sequence firing via `ShooterCore`.
- `Assets/Scripts/Combat`
  - `Health.cs` contains `DamageContext` (struct), `IDamageable` (interface), and `Health` (MonoBehaviour).
  - `TeamAffiliation.cs` contains `TeamId` (enum: `Neutral`, `Ally`, `Enemy`) and `TeamAffiliation` (MonoBehaviour).
- `Assets/Scripts/Combat/BulletDatas`
  - `BulletData.cs` contains `BulletData` (SO) and `BulletConfig` (readonly struct snapshot).
- `Assets/Scripts/Combat/WeaponDatas`
  - `WeaponData`: firing behavior — cooldown, simultaneous shot count, spread, sequence count, interval.
- `Assets/Scripts/Combat/Shooting`
  - `BulletPool.cs` contains `IBulletPool` (interface) and `BulletPool` (MonoBehaviour, `ObjectPool<Bullet>`).
  - `Bullet`: runtime movement, lifetime, collision, team checks, and damage application.
  - `ShooterCore`: internal static class — shared spread and sequence coroutine logic used by both `PlayerShooter` and `EnemyShooterAttack`.
- `Assets/Scripts/Enemy`
  - `EnemyController`: AI hub; drives `EnemyStateMachine` each frame. `Initialize` guarantees runtime-only `Health`, `TeamAffiliation`, and `EnemySensor` when missing, then subscribes to `Health.OnDied` to transition to `EnemyDeadState`.
  - `EnemySensor`: detection range checks (`TryDetectTarget`, `IsInAttackRange`, `HasLostSight`).
  - `EnemyFactory`: `Create(EnemyData, Vector2)` — Instantiate + ensure `EnemyController` + Initialize.
  - `EnemyData` (SO): per-type shared gameplay parameters (team, HP, speed, detection/attack ranges, patrol settings). Does not include movement/attack-specific config — those live on the Prefab's movement/attack components.
- `Assets/Scripts/Enemy/Movement`
  - `EnemyMovement` (abstract): `Configure / MoveToward / Stop`.
  - `EnemyGroundMovement`: ground horizontal movement via `Rigidbody2D.linearVelocity.x`.
  - `EnemyJumpingGroundMovement`: extends `EnemyGroundMovement` with auto-jump when target is above.
  - `EnemyFlyingMovement`: full 2D movement, `gravityScale=0`, Lerp steering.
- `Assets/Scripts/Enemy/Attack`
  - `EnemyAttack` (abstract): `Configure / CanAttack / TryAttack`.
  - `EnemyShooterAttack`: shoots toward target using `ShooterCore`. `WeaponData` is a `[SerializeField]` set in the Prefab Inspector; `IBulletPool` is injected via `Configure` at spawn time.
- `Assets/Scripts/Enemy/States`
  - `EnemyStateMachine.cs` contains `EnemyState` (abstract) and `EnemyStateMachine`.
  - State implementations: `EnemyIdleState`, `EnemyPatrolState`, `EnemyChaseState`, `EnemyAttackState`, `EnemyDeadState`.
- `Assets/Scripts/Diagnostics`
  - `GameLog`: standardized Unity Console logging with `[Level:ClassName]` prefixes.
  - `CombatDamageLog`: optional `Health.OnDamaged` / `Health.OnDied` logger (console via `GameLog`).

Core data flow:
1. A shooter (`PlayerShooter` / `EnemyShooterAttack`) reads `WeaponData`.
2. `WeaponData` points to `BulletData`.
3. `BulletConfig` snapshots `BulletData` values at fire time.
4. `ShooterCore` computes spread/sequence and calls `IBulletPool.Shoot`.
5. `BulletPool` launches pooled `Bullet` instances.
6. `Bullet` uses `hitMask` and owner/team checks, then calls `IDamageable.TakeDamage` on `Health`.

## Development Setup

- Open the project in Unity 6000.4.5f1.
- Use `Assets/Scenes/SampleScene.unity` for the current prototype scene.
- If Unity is already open, do not start a second batchmode Unity instance for the same project.
- Generated folders such as `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and `Recordings/` are not source files and should remain ignored.
- Generated project files such as `*.csproj`, `*.sln`, and `*.slnx` should not be committed.

Useful validation:
- Prefer Unity compile/play testing when the Editor is available.
- If the Editor is already open and batchmode is blocked, Roslyn/csc checks are acceptable for C# syntax validation, but they do not replace Play Mode behavior checks.

## Project-Specific Rules

- Keep bullet behavior and weapon firing behavior separate:
  - Bullet behavior belongs in `BulletData`.
  - Firing behavior belongs in `WeaponData`.
- Do not reintroduce per-fire-mode strategy classes for single/shotgun/burst unless the generic `simultaneousShotCount + spreadAngle + sequenceShotCount + sequenceInterval` model is no longer sufficient.
- Enemy authoring source of truth:
  - Shared values such as team, HP, movement speed, detection range, attack range, and patrol settings belong in `EnemyData`.
  - Movement/attack behavior selection and behavior-specific Inspector fields belong on Prefab components (`EnemyMovement` / `EnemyAttack` implementations).
  - `Health`, `TeamAffiliation`, `EnemySensor`, and `EnemyController` are runtime wiring components for enemies; do not use Prefab `Health.maxHp` or Prefab `TeamAffiliation.teamId` as enemy tuning data.
- Friendly-fire logic requires `TeamAffiliation`. Do not infer team identity from Layer names alone.
- Layers and `BulletData.hitMask` are broad collision filters. They decide what a bullet can collide with, not which side owns an object.
- Objects in `hitMask` without `IDamageable` stop bullets without taking damage. Use this for terrain and non-damageable blockers.
- Same-team targets and the owner are ignored by bullets; bullets should pass through them.
- `TeamId` values are `Neutral`, `Ally`, and `Enemy`. There is no `Player` value — the player uses `Ally`.
- `AreFriendly(a, b)` returns true only when `a == b` and neither is `Neutral`. `Ally` and `Enemy` are not friendly to each other.
- Use `Health` for actors or mobs that have HP. Death behavior belongs in listeners on `Health.OnDied`, not in `Health` itself.
- Use `GameLog` for gameplay/debug logging so console prefixes stay consistent.
- Keep `Assets/**/*.meta` files tracked with their assets. Never delete or regenerate `.meta` files casually.
- Avoid editing generated Unity folders (`Library/`, `Temp/`, `Logs/`, `UserSettings/`) unless explicitly troubleshooting local Editor state.
- Preserve user-made scene or asset changes. Unity YAML edits should be minimal and based on current file contents.
- Add comments only where they clarify gameplay rules, data ownership, pooling behavior, or non-obvious Unity/physics decisions.

## Git Workflow

### Branch Strategy

Use branches as rollback checkpoints.

| Branch | Purpose |
|--------|---------|
| `main` | Stable working state. |
| `feature/<name>` | New features. |
| `fix/<name>` | Bug fixes. |
| `chore/<name>` | Documentation, tooling, cleanup. |

Before large or risky changes, create a branch. If an approach fails after a few attempts, stop and explain the options instead of piling on unrelated fixes.

### Commit Messages

Use Conventional Commits:

`<type>(<scope>): <subject>`

Common types:
- `feat`
- `fix`
- `docs`
- `refactor`
- `chore`
- `test`
- `style`

Examples:
- `feat(combat): add team-based bullet filtering`
- `docs(project): document shooting architecture`
- `chore(git): ignore unity generated files`

## Custom Commands

### `/docs`

Review `docs/` against the actual codebase. Update outdated or missing project overview, architecture, key components, and setup notes.

### `/checkpoint`

Summarize changes since the last commit, then commit with a Conventional Commit message. Do not include ignored Unity generated files.

### `/status`

Summarize:
- Implemented systems
- In-progress work
- Known TODO/FIXME comments
- Known blockers or manual Unity checks still needed

## General Principles

- Prefer small, reviewable changes.
- Match existing Unity/C# style and serialized asset patterns.
- State assumptions when behavior is ambiguous.
- Do not add dependencies unless the benefit is clear for this prototype.
- Keep documentation and `AGENTS.md` updated when architecture or workflow changes.


<!-- headroom:rtk-instructions -->
# RTK (Rust Token Killer) - Token-Optimized Commands

When running shell commands, **always prefix with `rtk`**. This reduces context
usage by 60-90% with zero behavior change. If rtk has no filter for a command,
it passes through unchanged — so it is always safe to use.

## Key Commands
```bash
# Git (59-80% savings)
rtk git status          rtk git diff            rtk git log

# Files & Search (60-75% savings)
rtk ls <path>           rtk read <file>         rtk grep <pattern>
rtk find <pattern>      rtk diff <file>

# Test (90-99% savings) — shows failures only
rtk pytest tests/       rtk cargo test          rtk test <cmd>

# Build & Lint (80-90% savings) — shows errors only
rtk tsc                 rtk lint                rtk cargo build
rtk prettier --check    rtk mypy                rtk ruff check

# Analysis (70-90% savings)
rtk err <cmd>           rtk log <file>          rtk json <file>
rtk summary <cmd>       rtk deps                rtk env

# GitHub (26-87% savings)
rtk gh pr view <n>      rtk gh run list         rtk gh issue list

# Infrastructure (85% savings)
rtk docker ps           rtk kubectl get         rtk docker logs <c>

# Package managers (70-90% savings)
rtk pip list            rtk pnpm install        rtk npm run <script>
```

## Rules
- In command chains, prefix each segment: `rtk git add . && rtk git commit -m "msg"`
- For debugging, use raw command without rtk prefix
- `rtk proxy <cmd>` runs command without filtering but tracks usage
<!-- /headroom:rtk-instructions -->
