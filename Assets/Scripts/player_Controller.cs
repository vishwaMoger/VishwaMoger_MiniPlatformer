using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player_Controller : MonoBehaviour
{

    private Rigidbody2D rb;
    private BoxCollider2D BoxCol;
    private SpriteRenderer sprite;
    private Animator anim;

    [SerializeField] private LayerMask jump_Ground;

    private float Direction_X = 0f;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 14f;

    private enum MovementState { idle, running, jumping, falling }

    [SerializeField] private AudioSource jumpSFX;
    [SerializeField] private AudioSource TrampolineSFX;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        BoxCol = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Direction_X = SimpleInput.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(Direction_X * moveSpeed, rb.velocity.y);

        PlayerJump();

        UpdateAnimationState();
    }

    public void PlayerJump()
    {
        if (SimpleInput.GetButtonDown("Jump") && IsGrounded())
        {
            jumpSFX.Play();
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    //player sprite animation
    private void UpdateAnimationState()
    {
        MovementState state;

        if (Direction_X > 0f)
        {
            state = MovementState.running;
            sprite.flipX = false;
        }
        else if (Direction_X < 0f)
        {
            state = MovementState.running;
            sprite.flipX = true;
        }
        else
        {
            state = MovementState.idle;
        }

        if (rb.velocity.y > .1f)
        {
            state = MovementState.jumping;
        }
        else if (rb.velocity.y < -.1f)
        {
            state = MovementState.falling;
        }

        anim.SetInteger("state", (int)state);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Trampoline"))
        {
            rb.velocity = new Vector2(rb.velocity.x, 25f);
            TrampolineSFX.Play();
        }
    }

    // jump only when player on the ground
    private bool IsGrounded()
    {
        return Physics2D.BoxCast(BoxCol.bounds.center, BoxCol.bounds.size, 0f, Vector2.down, .1f, jump_Ground);
    }
}
