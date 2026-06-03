using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5.0f;
    [SerializeField] float jumpForce = 5.0f;

    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb;
    float horizontalInput;
    bool isGrounded;
    bool hasJumped;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        // Ground check stays in Update so jump input and grounded state are sampled together.
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (!isGrounded) hasJumped = false;

        if (isGrounded && !hasJumped && Input.GetButtonDown("Jump"))
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            hasJumped = true;
        }
    }

    void FixedUpdate()
    {
        // Physics velocity is applied in FixedUpdate while preserving vertical movement.
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }
}
