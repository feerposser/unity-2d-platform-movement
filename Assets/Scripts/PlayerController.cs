using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public LayerMask groundLayer;

    Vector2 fallVectorGravity;
    Rigidbody2D rb;
    Animator anim;

    public enum JumpState { DEFAULT, PREPARETOJUMP, JUMPING, PREPARETOFALL, FALLING }
    public enum DashState { DEFAULT, PREPARETODASH, DASHING }
    public enum SideState { RIGHT, LEFT }
    public enum WallslideState { STARTSTATE, PREPARETOSLIDE, SLIDING, PREPARETOJUMP }

    [Header("Movement")]
    public SideState sideState = SideState.RIGHT;
    public float xSpeed = 5;
    public float xMoveImput;
    public bool isGrounded;

    [Header("Jump")]
    public JumpState jumpState = JumpState.FALLING;
    public bool isJumping = false;
    public float fallMultiplaier;
    public float jumpMultiplaier;
    public float jumpForce = 10;
    public float jumpCounter;
    public float jumpTime;

    [Header("Wall Sliding")]
    public WallslideState wallslideState = WallslideState.STARTSTATE;
    public float freezeWallSlidingTimer = 0.4f;
    public float maxWallSliderTime = 0.3f;
    public bool freezeWallSliding = false;
    public float wallSlidingEndsAt = 0;
    public bool isWallSliding = false;
    public float wallDistance = .55f;

    [Header("Dash")]
    public DashState dashState = DashState.DEFAULT;
    public bool isDashing = false;
    public float dashTime = 0.19f;
    public float dashSpeed = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        fallVectorGravity = new Vector2(rb.velocity.x, -Physics2D.gravity.y);
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        ComputeMovement(out xMoveImput);

        ComputeJump();

        ComputeWallSliding(xMoveImput);

        ComputeDash();
    }

    void FixedUpdate()
    {
        ExecuteMovement(xMoveImput);

        IsGrounded();

        ExecuteJump();

        ExecuteWallSlide();

        ExecuteDash();
    }

    private void IsGrounded()
    {
        Collider2D collider = Physics2D.OverlapBox(transform.position + new Vector3(0, -.5f, 0), new Vector3(.97f, .03f, 0), 0, groundLayer);

        isGrounded = collider ? true : false;
    }

    public bool HaveWallContact()
    {
        RaycastHit2D rayWallCheck;

        if (sideState == SideState.RIGHT)
        {
            rayWallCheck = Physics2D.Raycast(transform.position + new Vector3(0, -0.5f, 0), Vector2.right, wallDistance, groundLayer);
            Debug.DrawRay(transform.position + new Vector3(0, -0.5f, 0), Vector2.right, Color.magenta);
        }
        else
        {
            rayWallCheck = Physics2D.Raycast(transform.position + new Vector3(0, -0.5f, 0), Vector2.left, wallDistance, groundLayer);
            Debug.DrawRay(transform.position + new Vector3(0, -0.5f, 0), Vector2.left, Color.magenta);
        }

        return rayWallCheck;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, -.5f, 0), new Vector3(.97f, .03f, 0));
    }

    /* --- Start Dash --- */
    private void Dash(float multiplier = 1)
    {
        Vector2 vector = (sideState == SideState.RIGHT) ? 
            Vector2.right * multiplier : Vector2.left * multiplier;

        rb.AddForce(vector * multiplier, ForceMode2D.Impulse);
    }

    private IEnumerator CoroutineExecuteDash()
    {
        isDashing = true;
        Dash(dashSpeed);
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
    }

    private void ExecuteDash()
    {
        if (dashState == DashState.PREPARETODASH)
        {
            dashState = DashState.DASHING;
            StartCoroutine("CoroutineExecuteDash");
        }
    }

    protected void ComputeDash()
    {
        if(Input.GetButtonDown("Fire1"))
        {
            dashState = DashState.PREPARETODASH;
        }
    }

    /* --- Start Wall Sliding --- */
    private IEnumerator FreezeWallSliding()
    {
        freezeWallSliding = true;
        yield return new WaitForSeconds(freezeWallSlidingTimer);
        freezeWallSliding = false;
    }

    private void ExecuteWallSlide()
    {
        if (!freezeWallSliding)
        {
            if (wallslideState == WallslideState.PREPARETOSLIDE)
            {
                wallslideState = WallslideState.SLIDING;

                isJumping = false;
                isWallSliding = true;
                wallSlidingEndsAt = Time.time + maxWallSliderTime;
            }

            if (wallslideState == WallslideState.SLIDING)
            {
                if (isWallSliding && !isJumping)
                {
                    rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.9f);
                }

                if (wallSlidingEndsAt < Time.time)
                {
                    isWallSliding = false;
                    wallSlidingEndsAt = 0;
                    wallslideState = WallslideState.STARTSTATE;
                }
            }

            if (wallslideState == WallslideState.PREPARETOJUMP)
            {
                isWallSliding = false;
                jumpState = JumpState.PREPARETOJUMP;
                wallslideState = WallslideState.STARTSTATE;

                StartCoroutine("FreezeWallSliding");
            }
        }
    }

    protected void ComputeWallSliding(float move)
    {
        if (!freezeWallSliding)
        {
            if (!isGrounded && HaveWallContact() && move != 0 && wallslideState == WallslideState.STARTSTATE)
            {
                wallslideState = WallslideState.PREPARETOSLIDE;    
            }

            if (isWallSliding && Input.GetButtonDown("Jump"))
            {
                wallslideState = WallslideState.PREPARETOJUMP;
            }
        }
    }

    /* --- Start Jump --- */
    private void Jump(float jumpMultiplier = 1)
    {
        rb.AddForce(new Vector2(rb.velocity.x, jumpForce * jumpMultiplier), ForceMode2D.Impulse);
        jumpState = JumpState.JUMPING;
        isJumping = true;
        jumpCounter = 0;
    }

    private void ExecuteJump()
    {
        if (jumpState == JumpState.PREPARETOJUMP)
        {
            Jump();
        }

        if (jumpState == JumpState.PREPARETOFALL)
        {
            jumpState = JumpState.FALLING;
            isJumping = false;
            jumpCounter = 0;

            //reduce de y velocity if is going up
            if (rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.3f);
            }
        }

        // jump highier ou lower depend on jump time
        if (jumpState == JumpState.JUMPING) // if (rb.velocity.y > 0 && isJumping)
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

            jumpState = (rb.velocity.y < 0) ? JumpState.PREPARETOFALL : JumpState.JUMPING;
        }

        // if the character is falling, accelerate the fall
        if (jumpState.Equals(JumpState.FALLING))
        {
            // verify if the velocity == 0 and stop falling state. set to defaul
            if (isGrounded)
            {
                jumpState = JumpState.DEFAULT;
            } else
            {
                rb.velocity -= fallVectorGravity * fallMultiplaier * Time.deltaTime;
            }
            
        }
    }

    protected void ComputeJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isJumping)
        {
            jumpState = JumpState.PREPARETOJUMP;
        } 

        if (Input.GetButtonUp("Jump"))
        {
            jumpState = JumpState.PREPARETOFALL;
        }
    }

    /* --- Start Move --- */
    private void Move(float move, float moveMultiplier = 1)
    {
        rb.velocity = new Vector2(move * xSpeed * moveMultiplier, rb.velocity.y);
    }

    private void ExecuteMovement(float move)
    {
        if(!isDashing)
        {
            Move(move);
        }
    }

    protected void ComputeMovement(out float move)
    {
        move = Input.GetAxis("Horizontal");

        if (move > 0)
        {
            sideState = SideState.RIGHT;
        }
        else if (move < 0)
        {
            sideState = SideState.LEFT;
        }
    }
}
