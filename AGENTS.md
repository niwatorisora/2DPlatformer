# AGENTS.md

This file is the shared instruction source for coding AI agents working on this project. Keep it concise and project-specific. Longer explanations belong in `docs/`.

## Project Overview

This is a Unity 6000.4.5f1 2D platformer prototype. The current gameplay focus is player movement, mouse-aimed shooting, pooled bullets, ScriptableObject-driven bullet/weapon data, and team-based friendly-fire filtering.

Primary scene:
- `Assets/Scenes/SampleScene.unity`

Detailed project notes:
- `docs/project-overview.md`

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
  - `PlayerShooter`: mouse aiming, trigger cooldown, spread, burst/sequence firing.
- `Assets/Scripts/Combat`
  - `IDamageable`: damage receiver contract using `DamageContext`.
  - `Health`: shared HP, damage intake checks, and death/damaged events.
  - `TeamAffiliation`: combat side used for friendly-fire filtering.
- `Assets/Scripts/Combat/Shooting`
  - `BulletData`: bullet behavior data such as speed, lifetime, damage, gravity, and hit mask.
  - `WeaponData`: firing behavior such as cooldown, simultaneous shot count, spread, sequence count, and interval.
  - `BulletPool`: pooled bullet spawning.
  - `Bullet`: runtime bullet movement, lifetime, collision, team checks, and damage application.
- `Assets/Scripts/Diagnostics`
  - `GameLog`: standardized Unity Console logging with `[Level:ClassName]` prefixes.
- `Assets/Scripts/Dev`
  - Prototype-only helpers such as `DummyTarget`.

Core data flow:
1. A shooter reads `WeaponData`.
2. `WeaponData` points to `BulletData`.
3. `BulletConfig` snapshots `BulletData` at fire time.
4. `BulletPool` launches pooled `Bullet` instances.
5. `Bullet` uses `hitMask` and owner checks, then asks `IDamageable`/`Health` whether damage can be received.

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
- Friendly-fire logic requires `TeamAffiliation`. Do not infer team identity from Layer names alone.
- Layers and `BulletData.hitMask` are broad collision filters. They decide what a bullet can collide with, not which side owns an object.
- Objects in `hitMask` without `IDamageable` stop bullets without taking damage. Use this for terrain and non-damageable blockers.
- Same-team targets and the owner are ignored by bullets; bullets should pass through them.
- `Player` and `Ally` are friendly to each other. `Enemy` is friendly to `Enemy`. `Neutral` is not friendly to anyone.
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
