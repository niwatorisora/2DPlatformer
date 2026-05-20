using UnityEngine;

/// <summary>
/// Side-scrolling ground movement for enemies. Mirrors PlayerMovement's FixedUpdate
/// velocity pattern and preserves vertical velocity so gravity and jumps still work.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyGroundMovement : EnemyMovement
{
    Rigidbody2D rb;
    float moveSpeed;
    float targetVelocityX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void Configure(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    public override void MoveToward(Vector2 worldPosition)
    {
        float direction = Mathf.Sign(worldPosition.x - transform.position.x);
        targetVelocityX = direction * moveSpeed;
    }

    public override void Stop()
    {
        targetVelocityX = 0f;
    }

    void FixedUpdate()
    {
        // Preserve vertical velocity so gravity and knockback are not cancelled.
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
    }
}
