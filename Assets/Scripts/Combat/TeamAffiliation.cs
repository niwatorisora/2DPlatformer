using UnityEngine;

/// <summary>
/// High-level combat side used to decide whether a bullet should ignore a target.
/// </summary>
public enum TeamId
{
    Neutral,
    Ally,
    Enemy
}

public class TeamAffiliation : MonoBehaviour
{
    [SerializeField] TeamId teamId = TeamId.Neutral;

    public TeamId TeamId => teamId;

    public void SetTeam(TeamId newTeamId)
    {
        teamId = newTeamId;
    }

    /// <summary>
    /// Neutral is intentionally never friendly so environment objects do not become allied.
    /// </summary>
    public static bool AreFriendly(TeamId a, TeamId b)
    {
        if (a == TeamId.Neutral || b == TeamId.Neutral) return false;
        return a == b;
    }
}
