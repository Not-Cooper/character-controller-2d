using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController2D : MonoBehaviour
{
    public Rigidbody2D playerBody;
    private FrameInput Input;

    [SerializeField] private Bounds characterBounds;
    [SerializeField] private float topSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float decceleration;
    //allows acceleration to increase at higher speeds
    [SerializeField] private float velocityPower;
    [SerializeField] private float frictionAmount;
    [SerializeField] private float jumpStrength;
    [SerializeField] private float jumpBuffer;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float minGravityMultiplier;

    [Range(0f, 1f)]
    [SerializeField] private float jumpEndEarlyGravityModifier;

    [Range(-100f, 0f)]
    [SerializeField] private float fallClamp;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float gravityScale;

    private bool jumping = false;
    public bool isGrounded;
    private int layerMask = 1 << 3;

    private bool facingRight = true;

    private void Start()
    {
        gravityScale = playerBody.gravityScale;
        GatherInput();
    }

    // Update is called once per frame
    void Update()
    {
        GatherInput();
        CalculateGravity();

        CheckGround();

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        Run();
        ApplyFriction();
        CalculateJump();
    }

    private void FixedUpdate()
    {
        
    }


    //Get user Input
    private void GatherInput()
    {
        Input = new FrameInput
        {
            JumpDown = UnityEngine.Input.GetButtonDown("Jump"),
            JumpUp = UnityEngine.Input.GetButtonUp("Jump"),
            X = UnityEngine.Input.GetAxisRaw("Horizontal")
        };
        if (Input.JumpUp)
        {
            coyoteTimeCounter = 0;
        }
        if (Input.JumpDown)
        {
            jumpBufferCounter = jumpBuffer;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (Input.X > 0 && !facingRight)
        {
            Flip();
        }else if (Input.X < 0 && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        Vector2 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;

        facingRight = !facingRight;
    }


    //Run
    private void Run()
    {
        float targetSpeed = topSpeed * Input.X;
        //difference approaches 0 when accelerating and approaches 0 when deccelerating
        float speedDiff = targetSpeed - playerBody.velocity.x;

        //change accel rate based on above
        float accelRate;
        if (Mathf.Abs(targetSpeed) > 0.1){
            accelRate = acceleration;
        }
        else
        {
            accelRate = decceleration;
        }

        //movement
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velocityPower) * Mathf.Sign(speedDiff);

        //vector right so only applies in x direction
        playerBody.AddForce(movement * Vector2.right);
    }
    //Friction (subpart of run)
    private void ApplyFriction()
    {
        if (Mathf.Abs(topSpeed * Input.X) < 0.1)
        {
            float amount = Mathf.Min(Mathf.Abs(playerBody.velocity.x), frictionAmount);
            amount *= Mathf.Sign(playerBody.velocity.x);

            playerBody.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }
    }


    //Jumping
    private bool hasBufferedJump => isGrounded && jumpBufferCounter > 0;
    private bool hasCoyoteTime => coyoteTimeCounter > 0;
    private bool endedJumpEarly = true;

    private void CalculateJump()
    {
        if (Input.JumpDown && hasCoyoteTime || hasBufferedJump)
        {
            if (playerBody.velocity.y <= 0.01)
            {
                //playerBody.AddForce(Vector2.up * jumpStrength, ForceMode2D.Impulse);
                playerBody.velocity = new Vector2(playerBody.velocity.x, jumpStrength);
                jumpBufferCounter = 0f;
                endedJumpEarly = false;
                jumping = true;
            }
        }

        if (!isGrounded && Input.JumpUp && !endedJumpEarly && playerBody.velocity.y > 0.01)
        {
            endedJumpEarly = true;
            playerBody.AddForce(Vector2.down * playerBody.velocity.y * (1 - jumpEndEarlyGravityModifier), ForceMode2D.Impulse);
        }
    }

    private void CheckGround()
    {
        if (Physics2D.OverlapBox(transform.position, characterBounds.extents, 0, ~layerMask))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }


    //Gravity stuff
    private void CalculateGravity()
    {
        if (playerBody.velocity.y < 0.01)
        {
            playerBody.gravityScale = minGravityMultiplier * gravityScale;
            jumping = false;
        }
        else
        {
            playerBody.gravityScale = gravityScale;
        }

        if (playerBody.velocity.magnitude > Mathf.Abs(fallClamp))
        {
            playerBody.velocity = Vector2.ClampMagnitude(playerBody.velocity, Mathf.Abs(fallClamp));
        }
    }


    public class FrameInput
    {
        public bool JumpDown { get; set; }
        public bool JumpUp { get; set; }
        public float X { get; set; }
    }
}
