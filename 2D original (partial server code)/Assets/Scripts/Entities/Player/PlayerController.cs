using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System;
using UnityEngine;
using FinalAisle_Shared.Networking;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rigidBody;
    private Animator animator;
    private Transform playerTransform;
    private RabbitPortrait playerPortrait;

    [SerializeField] public float MoveSpeed; // speed at which player can run horizontally
    [SerializeField] public LayerMask collidableLayers; // layers with which player can collide
    [SerializeField] public Transform groundCheck; // game object that detects for ground
    [SerializeField] public float rollSpeed; // in terms of regular movement speed

    public GameObject Camera;
    public GameObject ConnectionObject;
    private Connection Connection;

    private float moveVelocity; // horizontal movement velocity

    private bool facingRight = true;
    private bool addJumpForce = false; // whether to add a jump force on next fixed update
    private bool rollRight = true; // whether the player is rolling towards the right
    private bool tappedRight = true; // if the last arrow key press was to the right
    private bool deepCrouch = false; // whether the player is deep crouching

    private float nextBlinkTime = 0; // calculates next blink

    private float walkTime = 0; // time since last walk command
    private float rollTime = 1; // time since roll initiated
    private float walkLeanTime = 0; // time since last movement
    private float crouchTime = 0; // time since last crouch
    private float lastX; // player's previous X value
    private float lastDirClickTime = 0; // time of last right or left click

    private const float groundedRadius = 0.2F;
    private const float walkLimit = 0.1F; // minimum period of inactivity at which walk animation is stopped
    private const float rollDuration = 0.45F; // how long a roll lasts, in seconds
    private const float walkLeanLimit = 0.1F; // minimum period of inactivity at which walk lean is disactivated
    private const float doubleClickWindow = 0.4F; // maximum duration between successive clicks to count as double click
    private const float deepCrouchChannelDuration = 2.0F; // time required to enter deep crouch

    void Start()
    {
        Connection = ConnectionObject.GetComponent<Connection>();

        // inform server of level join
        Connection.SendData(PacketDataUtils.Condense(PacketDataUtils.JoinLevel, ""));

        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerTransform = GetComponent<Transform>();
        playerPortrait = Camera.transform.Find("HUD").transform.Find("Rabbit Player Pastel").GetComponent<RabbitPortrait>();

        lastX = playerTransform.position.x;

        InitNextBlink();
    }

    void Update()
    {
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); // left/right for horizontal movement, up/down for climbing/rolling

        bool jumpThisFrame = false;
        bool rollThisFrame = false;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) // check for double taps (for rolls)
        {
            if (tappedRight && Time.time - lastDirClickTime <= doubleClickWindow) rollThisFrame = true;

            tappedRight = true;
            lastDirClickTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (!tappedRight && Time.time - lastDirClickTime <= doubleClickWindow) rollThisFrame = true;

            tappedRight = false;
            lastDirClickTime = Time.time;
        }

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling") && ((moveInput.x < 0 && facingRight) || (moveInput.x > 0 && !facingRight))) // change movement direction
        {
            facingRight = !facingRight;

            Vector3 newScale = new Vector3(moveInput.x, transform.localScale.y, transform.localScale.z); // reflect the player sprite
            playerTransform.localScale = newScale;
        }

        if (Time.time >= nextBlinkTime) // determine if the player should blink
        {
            animator.SetTrigger("Blink");
            playerPortrait.Blink();
            InitNextBlink();
        }

        if (rollThisFrame && animator.GetBool("Is Grounded") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling")) // roll
        {
            animator.SetTrigger("Roll");
            rollTime = 0;
            rollRight = moveInput.x == 0 ? facingRight : moveInput.x == 1; // roll to the right or left?
            deepCrouch = false; // uncrouch if rolling
            crouchTime = -rollDuration;
        }
        else rollThisFrame = false;

        if (!rollThisFrame && Input.GetKeyDown(KeyCode.Space) && animator.GetBool("Is Grounded") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling")) // jump
        {
            animator.SetTrigger("Jump");
            addJumpForce = true;
            jumpThisFrame = true;
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

        if (Math.Abs(lastX - playerTransform.position.x) < 0.05)  // no movement last frame
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

        // update player crouching status
        bool attemptCrouch = moveInput.y == -1;
        if (attemptCrouch && !animator.GetBool("Is Crouching") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Rabbit Rolling") && animator.GetBool("Is Grounded") && !jumpThisFrame)
        { // player wants to crouch, is not currently jumping or rolling
            animator.SetBool("Is Crouching", true);
            if (crouchTime != 0)
            {
                deepCrouch = false;
                crouchTime = 0;
            }
        }
        else if (!attemptCrouch && animator.GetBool("Is Crouching"))
        {
            animator.SetBool("Is Crouching", false);
        }

        // check if deep crouching should begin
        if(animator.GetBool("Is Crouching") && !deepCrouch)
        {
            crouchTime += Time.deltaTime;
            if (crouchTime > deepCrouchChannelDuration)
            {
                animator.SetTrigger("Deep Crouch");
                deepCrouch = true;
            }
        }

        if (jumpThisFrame) Connection.SendData(PacketDataUtils.Condense(PacketDataUtils.MovementJump, "")); // send jump information
        if (rollThisFrame) Connection.SendData(PacketDataUtils.Condense(PacketDataUtils.MovementRoll, ""));
    }

    void FixedUpdate()
    {
        if (rollTime <= rollDuration) rigidBody.velocity = new Vector2(rollSpeed * MoveSpeed * (rollRight ? 1 : -1) * Time.fixedDeltaTime, rigidBody.velocity.y); // player is rolling
        else rigidBody.velocity = new Vector2(moveVelocity * (animator.GetBool("Is Crouching") ? 0 : 1) * Time.fixedDeltaTime, rigidBody.velocity.y);

        // send player input data to server { MovementInput:x|y }
        Connection.SendData(PacketDataUtils.Condense(PacketDataUtils.MovementInput, moveVelocity / MoveSpeed + "|" + (animator.GetBool("Is Crouching") ? -1 : 0)));

        rollTime += Time.fixedDeltaTime;

        rigidBody.AddForce(new Vector2(0, -100), ForceMode2D.Force); // gravity

        if (addJumpForce) // jump
        {
            addJumpForce = false;
            rigidBody.AddForce(new Vector2(0, 40), ForceMode2D.Impulse);
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
        if(isGrounded && !animator.GetBool("Is Grounded"))
        {
            deepCrouch = false;
            crouchTime = 0;

            // show landing particles
            Instantiate(Camera.GetComponent<PrefabLibrary>().LandingParticles, playerTransform.position, Quaternion.identity);
        }
        animator.SetBool("Is Grounded", isGrounded);
    }

    private void InitNextBlink()
    {
        nextBlinkTime = Time.time + UnityEngine.Random.Range(0.3F, 3F);
    }
}
