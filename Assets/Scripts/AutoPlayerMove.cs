using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class AutoPlayerMove : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    public Transform target;      // o objeto que o player vai seguir
    public float moveSpeed = 5f;  // velocidade do movimento

    private Rigidbody2D rb;
    public bool active = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (target == null) return;
        if (!active) return;

        // direção do player até o alvo
        Vector2 dir = ((Vector2)target.position - rb.position).normalized;

        // aplica velocidade
        rb.linearVelocity = dir * moveSpeed;
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0f || Mathf.Abs(rb.linearVelocity.y) > 0f;
        animator.SetBool("isMoving", isMoving);
        if(rb.linearVelocity.x < 0) spriteRenderer.flipX = true;
        else spriteRenderer.flipX = false;
    }
}
