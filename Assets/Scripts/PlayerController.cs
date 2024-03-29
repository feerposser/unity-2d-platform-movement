/*
 * Created by: @feerposser
 * description: This is an unity script for handling a player controller 2D platform
 * Features included: move, dash, jump, wall slide, wall jump
 * 
 * Repository: https://github.com/feerposser/unity-2d-platform-movement
 * Gist: https://gist.github.com/feerposser/147fe370a6df710414d7c2728a96c035
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public LayerMask groundLayer;

    Vector2 fallVectorGravity;
    protected Rigidbody2D rb;

    public enum WallslideState { DEFAULT, PREPARETOSLIDE, SLIDING, PREPARETOJUMP, WALLJUMPING }
    public enum JumpState { DEFAULT, PREPARETOJUMP, JUMPING, PREPARETOFALL, FALLING }
    public enum DashState { DEFAULT, PREPARETODASH, DASHING }
    public enum SideState { RIGHT, LEFT }

    [Header("Grounded")]
    [SerializeField] Vector3 groundedCenter;
    [SerializeField] Vector2 groundBoxDetector;

    [Header("Movement")]
    [SerializeField] SideState sideState = SideState.RIGHT;
    [SerializeField] float xSpeed = 7.5f;
    [SerializeField] float xMoveImput;
    [SerializeField] bool isGrounded;
    float move;

    [Header("Jump")]
    [SerializeField] JumpState jumpState = JumpState.FALLING;
    [SerializeField] float horizontalMultiplier = .85f;
    [SerializeField] float fallMultiplier = 5.5f;
    [SerializeField] float fallingCounter = .3f;
    [SerializeField] float jumpForce = 8;
    [SerializeField] float fallingTime;

    [Header("Wall Sliding")]
    [SerializeField] WallslideState wallslideState = WallslideState.DEFAULT;
    [SerializeField] float groundCheckDistance = -.5f;
    [SerializeField] float maxWallSliderTime = .3f;
    [SerializeField] float wallSlideJumpForce = 2;
    [SerializeField] float wallSlidingEndsAt = 0;
    [SerializeField] bool isWallSliding = false;
    [SerializeField] bool isWallJumping = false;
    [SerializeField] float wallDistance = .55f;

    [Header("Dash")]
    [SerializeField] DashState dashState = DashState.DEFAULT;
    [SerializeField] bool isDashing = false;
    [SerializeField] float dashTime = 0.19f;
    [SerializeField] float dashSpeed = 4.5f;

    [Header("Gizmos colors")]
    [SerializeField] Color groundBoxDetectorColor = Color.green;
    [SerializeField] Color wallslideRayDetectorColor = Color.magenta;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        fallVectorGravity = new Vector2(rb.velocity.x, -Physics2D.gravity.y);
    }

    void Update()
    {
        ComputeMovement();

        ComputeJump();

        ComputeWallSliding();

        ComputeDash();

        if (Input.GetButtonDown("Fire3") && Time.timeScale == 1)
            Time.timeScale = 0;
        else if (Input.GetButtonDown("Fire3") && Time.timeScale == 0)
            Time.timeScale = 1;
    }

    void FixedUpdate()
    {
        float ty = Mathf.Clamp(rb.velocity.y, float.MinValue, 11);
        rb.velocity = new Vector2(rb.velocity.x, ty);
        //if (rb.velocity.y > 0) Debug.Log(rb.velocity.y);

        ExecuteMovement();

        IsGrounded();

        ExecuteJump();

        ExecuteWallSliding();

        ExecuteDash();
    }

    private void IsGrounded()
    {
        Collider2D collider = Physics2D.OverlapBox(transform.position + groundedCenter, groundBoxDetector, 0, groundLayer);
        isGrounded = collider ? true : false;
    }

    public bool HaveWallContact()
    {
        RaycastHit2D rayWallCheck;
        Vector2 direction = sideState == SideState.RIGHT ? Vector2.right : Vector2.left;
        rayWallCheck = Physics2D.Raycast(transform.position + new Vector3(0, groundCheckDistance, 0), direction, wallDistance, groundLayer);

        return rayWallCheck;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(groundBoxDetectorColor.r, groundBoxDetectorColor.g, groundBoxDetectorColor.b);
        Gizmos.DrawCube(transform.position + groundedCenter, groundBoxDetector);

        Gizmos.color = new Color(wallslideRayDetectorColor.r, wallslideRayDetectorColor.g, wallslideRayDetectorColor.b);
        Gizmos.DrawRay(transform.position + new Vector3(0, groundCheckDistance, 0),
            sideState.Equals(SideState.RIGHT) ? Vector2.right : Vector2.left);
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
        if (dashState.Equals(DashState.PREPARETODASH))
        {
            dashState = DashState.DASHING;
            StartCoroutine("CoroutineExecuteDash");
        }
    }

    protected void ComputeDash()
    {
        if (Input.GetButtonDown("Fire1")) dashState = DashState.PREPARETODASH;
    }

    /* --- Start Wall Sliding --- */
    private void WallSlidingPrepareToSlide()
    {
        wallslideState = WallslideState.SLIDING;
        wallSlidingEndsAt = Time.time + maxWallSliderTime;
    }

    private void WallSlidingSliding()
    {
        isWallSliding = true;
        if (isWallSliding && !jumpState.Equals(JumpState.JUMPING))
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.9f);
        
        if (wallSlidingEndsAt < Time.time)
        {
            isWallSliding = false;
            wallSlidingEndsAt = 0;
            wallslideState = WallslideState.DEFAULT;
        }
    }

    private void WallSlidingPrepareToJump()
    {
        isWallJumping = true;
        isWallSliding = false;
        Jump(wallSlideJumpForce);
        Debug.Log("wall jumping");
        wallslideState = WallslideState.WALLJUMPING;
    }

    private void ExecuteWallSliding()
    {
        if (wallslideState.Equals(WallslideState.DEFAULT))
            isWallJumping = false;

        if (wallslideState.Equals(WallslideState.PREPARETOSLIDE))
            WallSlidingPrepareToSlide();

        if (wallslideState.Equals(WallslideState.SLIDING))
            WallSlidingSliding();

        if (wallslideState.Equals(WallslideState.PREPARETOJUMP))
            WallSlidingPrepareToJump();
    }

    protected void ComputeWallSliding()
    {
        if (!isGrounded && HaveWallContact() && move != 0 && wallslideState.Equals(WallslideState.DEFAULT))
            wallslideState = WallslideState.PREPARETOSLIDE;

        if (isWallSliding && Input.GetButtonDown("Jump"))
            wallslideState = WallslideState.PREPARETOJUMP;

        if (Input.GetButtonUp("Jump") && isWallJumping)
            wallslideState = WallslideState.DEFAULT;
    }

    /* --- Start Jump --- */
    private void Jump(float verticalMultiplier=0)
    {
        verticalMultiplier = verticalMultiplier != 0 ? verticalMultiplier : 1;
        rb.AddForce(new Vector2(rb.velocity.x, jumpForce * verticalMultiplier), ForceMode2D.Impulse);
        jumpState = JumpState.JUMPING;
    }

    private void JumpPrepareToFall()
    {
        fallingTime = 0;
        jumpState = JumpState.FALLING;
    }

    private void JumpJumping()
    {
        rb.velocity = new Vector2(rb.velocity.x * horizontalMultiplier, rb.velocity.y);
        if (rb.velocity.y < 0) jumpState = JumpState.PREPARETOFALL;
    }

    private void JumpFalling()
    {
        fallingTime += Time.deltaTime;
        
        if (fallingTime > fallingCounter && rb.velocity.y > 0)
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.3f);
        else if (!isWallSliding)
            Falling();

        if (isGrounded) jumpState = JumpState.DEFAULT;
    }

    private void Falling() =>
        rb.velocity -= fallVectorGravity * fallMultiplier * Time.deltaTime;

    private void ExecuteJump()
    {
        if (jumpState.Equals(JumpState.PREPARETOJUMP)) Jump();

        // set preparetofall when falling from a cliff;
        if (jumpState.Equals(JumpState.DEFAULT) && rb.velocity.y < 0)
            jumpState = JumpState.PREPARETOFALL;

        if (jumpState.Equals(JumpState.PREPARETOFALL)) JumpPrepareToFall();

        // jump highier ou lower depend on jump time
        if (jumpState.Equals(JumpState.JUMPING)) JumpJumping();

        // if the character is falling, accelerate the fall
        if (jumpState.Equals(JumpState.FALLING)) JumpFalling();
    }

    protected void ComputeJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !jumpState.Equals(JumpState.JUMPING))
            jumpState = JumpState.PREPARETOJUMP;

        if (Input.GetButtonUp("Jump") && jumpState.Equals(JumpState.JUMPING))
            jumpState = JumpState.PREPARETOFALL;
    }

    /* --- Start Move --- */
    private void Move() =>
        rb.velocity = new Vector2(move * xSpeed, rb.velocity.y);

    private void ExecuteMovement()
    {
        if (!isDashing)
            Move();
    }

    protected void ComputeMovement()
    {
        move = Input.GetAxis("Horizontal");
        if (move > 0)
            sideState = SideState.RIGHT;
        else if (move < 0)
            sideState = SideState.LEFT;
    }
}
