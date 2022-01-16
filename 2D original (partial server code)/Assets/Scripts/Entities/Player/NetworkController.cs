using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    private Rigidbody2D rigidBody;
    private Animator animator;
    private Transform playerTransform;

    [SerializeField] public float MoveSpeed; // speed at which player can run horizontally
    [SerializeField] public LayerMask collidableLayers; // layers with which player can collide
    [SerializeField] public Transform groundCheck; // game object that detects for ground
    [SerializeField] public float rollSpeed; // in terms of regular movement speed

    private float moveVelocity; // horizontal movement velocity

    public bool jump = false;
    public bool roll = false;

    private bool facingRight = true;
    private bool addJumpForce = false; // whether to add a jump force on next fixed update
    private bool rollRight = true; // whether the player is rolling towards the right

    private float nextBlinkTime = 0; // calculates next blink

    private float walkTime = 0; // time since last walk command
    private float rollTime = 1; // time since roll initiated
    private float walkLeanTime = 0; // time since last movement
    private float lastX; // player's previous X value

    private const float groundedRadius = 0.2F;
    private const float walkLimit = 0.1F; // minimum period of inactivity at which walk animation is stopped
    private const float rollDuration = 0.45F; // how long a roll lasts, in seconds
    private const float walkLeanLimit = 0.1F; // minimum period of inactivity at which walk lean is disactivated

    public void Initialize()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerTransform = GetComponent<Transform>();

        lastX = playerTransform.position.x;

        InitNextBlink();
    }

    public void RunOnUpdate(Vector2 moveInput) // called by network to update foreign player's character
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling") && ((moveInput.x < 0 && facingRight) || (moveInput.x > 0 && !facingRight))) // change movement direction
        {
            facingRight = !facingRight;

            Vector3 newScale = new Vector3(moveInput.x, transform.localScale.y, transform.localScale.z); // reflect the player sprite
            playerTransform.localScale = newScale;
        }

        if (Time.time >= nextBlinkTime) // determine if the player should blink
        {
            animator.SetTrigger("Blink");
            InitNextBlink();
        }

        if (roll) // roll
        {
            animator.SetTrigger("Roll");
            rollTime = 0;
            rollRight = moveInput.x == 0 ? facingRight : moveInput.x == 1; // roll to the right or left?
        }
        else if (jump) // jump
        {
            animator.SetTrigger("Jump");
            addJumpForce = true;
        }

        if (moveInput.x == 0) // no walk command detected
        {
            walkTime += Time.deltaTime;
            if (walkTime >= walkLimit && animator.GetBool("Is Walking")) animator.SetBool("Is Walking", false);
        }
        else // recent walk command
        {
            if (walkTime != 0) walkTime = 0;
            if (!animator.GetBool("Is Walking")) animator.SetBool("Is Walking", true);
        }

        if (lastX == playerTransform.position.x)  // no movement last frame
        {
            walkLeanTime += Time.deltaTime;
            if (walkLeanTime >= walkLeanLimit && animator.GetBool("Is Leaning")) animator.SetBool("Is Leaning", false);
        }
        else // recent movement
        {
            if (walkLeanTime != 0) walkLeanTime = 0;
            if (!animator.GetBool("Is Leaning")) animator.SetBool("Is Leaning", true);
        }
        lastX = playerTransform.position.x;

        moveVelocity = moveInput.x * MoveSpeed;

        jump = false;
        roll = false;
    }

    public void RunOnFixedUpdate()
    {
        if (rollTime <= rollDuration) rigidBody.velocity = new Vector2(rollSpeed * MoveSpeed * (rollRight ? 1 : -1) * Time.fixedDeltaTime, rigidBody.velocity.y); // player is rolling
        else rigidBody.velocity = new Vector2(moveVelocity * Time.fixedDeltaTime, rigidBody.velocity.y);

        rollTime += Time.fixedDeltaTime;

        rigidBody.AddForce(new Vector2(0, -100), ForceMode2D.Force); // gravity

        if (addJumpForce) // jump
        {
            addJumpForce = false;
            rigidBody.AddForce(new Vector2(0, 46), ForceMode2D.Impulse);
        }

        bool isGrounded = false;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundedRadius, collidableLayers);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                isGrounded = true;
            }
        }
        animator.SetBool("Is Grounded", isGrounded);
    }

    private void InitNextBlink()
    {
        nextBlinkTime = Time.time + Random.Range(0.3F, 3F);
    }
}
