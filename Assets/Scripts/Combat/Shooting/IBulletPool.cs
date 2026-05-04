using UnityEngine;

/// <summary>
/// Minimal shooting surface used by shooters without exposing ObjectPool details.
/// </summary>
public interface IBulletPool
{
    void Shoot(Vector2 position, Vector2 direction, BulletConfig config, GameObject owner, TeamId ownerTeam);
}
