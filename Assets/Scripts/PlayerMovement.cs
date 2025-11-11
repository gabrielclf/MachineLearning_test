using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Vector2 _movement;
    private Rigidbody2D _rb2d;
    private Animator _animator;
    private SpriteRenderer _spriteRender;
    private const string _MOVESPEEDX = "movSpeedX";
    private const string _MOVESPEEDY = "movSpeedY";

    private void Awake()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRender = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        _movement.Set(InputManager.Movement.x, InputManager.Movement.y);

        _rb2d.linearVelocity = _movement * moveSpeed;
        _animator.SetFloat(_MOVESPEEDX, _movement.x);
        _animator.SetFloat(_MOVESPEEDY, _movement.y);
        SpriteStatus();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "NPC")
        {
            _animator.SetTrigger("touch");
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
