using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public LayerMask groundLayer;

    Rigidbody2D rb;
    Animator anim;

    public enum SideState { right, left }

    [Header("Movement")]
    public SideState sideState = SideState.right;
    public float xSpeed = 5;

    [Header("Jump")]
    public float jumpForce = 10;
    public float fallMultiplaier;
    public float jumpTime;
    public float jumpCounter;
    public float jumpMultiplaier;
    public bool isJumping = false;

    Vector2 fallVectorGravity;

    [Header("Wall Sliding")]
    public float slidingEndsAt = 0;
    public float wallDistance = .55f;
    public float wallFriction;
    public bool isSliding = false;
    public float maxSliderTime = 0.3f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        fallVectorGravity = new Vector2(rb.velocity.x, -Physics2D.gravity.y);
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        Movement();
    }

    bool HaveWallContact()
    {
        RaycastHit2D rayWallCheck;

        if (sideState == SideState.right)
        {
            rayWallCheck = Physics2D.Raycast(transform.position + new Vector3(0, -0.5f, 0), Vector2.right, wallDistance, groundLayer);
            Debug.DrawRay(transform.position + new Vector3(0, -0.5f, 0), Vector2.right, Color.magenta);
        }
        else
        {
            rayWallCheck = Physics2D.Raycast(transform.position + Vector3.down, Vector2.left, wallDistance, groundLayer);
            Debug.DrawRay(transform.position + Vector3.down, Vector2.right, Color.magenta);
        }

        return rayWallCheck;
    }

    bool IsGrounded()
    {
        //bool rayCheckGround = Physics2D.Raycast(transform.position, Vector2.down, 1, groundLayer);
        Debug.DrawRay(transform.position, Vector2.down, Color.blue);

        //return rayCheckGround ? GroundState.grounded : GroundState.ungrounded;
        return Physics2D.Raycast(transform.position, Vector2.down, 1, groundLayer);
    }

    void Jump(float jumpMultiplier = 1)
    {
        //rb.velocity = new Vector2(rb.velocity.x, jumpForce * jumpMultiplier);
        rb.AddForce(new Vector2(rb.velocity.x, jumpForce * jumpMultiplier), ForceMode2D.Impulse);
        isJumping = true;
        jumpCounter = 0;
    }

    private void ComputeJump()
    {
        // Make the jump
        if (Input.GetButtonDown("Jump") && IsGrounded() && !isJumping)
        {
            Jump();
        }

        // bellow, compute the jump effect

        // stop jumping and start to add -y velocity to the jump final momment depend on player releasing the jump button
        if (Input.GetButtonUp("Jump"))
        {
            isJumping = false;
            jumpCounter = 0;

            if (rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.3f);
            }
        }

        // jump highier ou lower depend on jump time
        if (rb.velocity.y > 0 && isJumping)
        {
            jumpCounter += Time.deltaTime;

            if (jumpCounter > jumpTime) isJumping = true;

            float t = jumpCounter / jumpTime;
            float currentMultiplier = jumpMultiplaier;

            if (t > 0.5)
            {
                currentMultiplier = jumpMultiplaier * (1 - t);
            }

            rb.velocity += new Vector2(rb.velocity.x, jumpForce * currentMultiplier) * Time.deltaTime;
        }

        // if the character is falling, accelerate the fall
        if (rb.velocity.y < 0)
        {
            rb.velocity -= fallVectorGravity * fallMultiplaier * Time.deltaTime;
        }
    }

    private void ComputeWallSliding(float move)
    {
        if (!IsGrounded() && HaveWallContact() && move != 0)
        {
            isSliding = true;
            isJumping = false;
            slidingEndsAt = Time.time + maxSliderTime;
        }

        if (slidingEndsAt < Time.time)
        {
            isSliding = false;
            slidingEndsAt = 0;
        }

        if (isSliding && !isJumping)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.9f);
        }

        if (isSliding && Input.GetButtonDown("Jump"))
        {
            isSliding = false;
            Jump();
        }
    }

    void ComputeMovement(out float move)
    {
        move = Input.GetAxis("Horizontal");
        Move(move);
        
        if (move > 0)
        {
            sideState = SideState.right;
        } else if (move < 0)
        {
            sideState = SideState.left;
        }
    }

    void Move(float move, float moveMultiplier = 1)
    {
        rb.velocity = new Vector2(move * xSpeed * moveMultiplier, rb.velocity.y);
    }

    void Movement()
    {
        float move;
        ComputeMovement(out move);

        ComputeJump();

        ComputeWallSliding(move);
    }
}
