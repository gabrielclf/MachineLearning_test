using UnityEngine;

[RequireComponent(typeof(AutoPlayerMove))]
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Vector2 _movement;
    private Rigidbody2D _rb2d;
    private Animator _animator;
    private SpriteRenderer _spriteRender;
    private HealthSystem health;
    private const string _MOVESPEEDX = "movSpeedX";
    private const string _MOVESPEEDY = "movSpeedY";

    private void Awake()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRender = GetComponent<SpriteRenderer>();
        health = GetComponent<HealthSystem>();
    }

    private void Update()
    {
        _movement.Set(InputManager.Movement.x, InputManager.Movement.y);

        
        
        _rb2d.linearVelocity = _movement * moveSpeed;
        _animator.SetFloat(_MOVESPEEDX, _movement.x);
        _animator.SetFloat(_MOVESPEEDY, _movement.y);
        if (!GetComponent<AutoPlayerMove>().active)
        {
            bool isMoving = Mathf.Abs(_movement.x) > 0f || Mathf.Abs(_movement.y) > 0f;
            _animator.SetBool("isMoving", isMoving);
            SpriteStatus();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("NPC"))
        {
            if(health.isActiveAndEnabled) health.TakeDamage(1);
        } 
    }

    private void SpriteStatus() //Controlar posição do sprite
    {
        if (_movement.x < 0)
        {
            _spriteRender.flipX = true;
        } else if (_movement.x > 0)
        {
            _spriteRender.flipX = false;
        }

    }
}
