using System;
using UnityEngine;
using Random = System.Random;

/**
 * Abstract player controller class responsible for player behaviour. Child classes provide means of control, such as player vs network.
 * 
 * All child objects to the player object, aside from the armature, should have a skinned mesh renderer.
 * Player objects should be set to the "Ignore Raycast" layer to ensure the camera isn't blocked.
 */
public abstract class PlayerController : MonoBehaviour
{
    protected GameObject playerObject;
    protected Animator playerAnimator;
    protected Rigidbody playerRigidbody;
    protected CapsuleCollider playerCollider;

    protected float playerScale; // scale of player model in real world

    protected const float horizontalTurnSpeed = 135F; // speed at which A and D buttons rotate the player
    protected const float headTurnMax = 46.6354F; // how far the head can turn before the body starts moving along, in degrees

    protected float playerHorizontalAngle; // direction the player should be facing
    protected float playerLastHorizontalAngle; // player's previous turn angle, used to determine if turn information should be sent to the server and which way the player is turning
    protected float playerBodyAngle; // direction the player's body is facing (as opposed to the head), used for turning animation calculations

    protected float nextBlinkTime = 0F; // next timeframe at which the player should blink
    protected const float minBlinkDelay = 0.2F; // minimum 0.2 seconds between blinks
    protected const float maxBlinkDelay = 1.6F; // maximum 1.2 seconds between blinks
    protected static readonly Random blinkRandom = new Random(); // universal player blink random to prevent everyone from blinking at the same time

    protected float lastJumpTime = 0F; // last timeframe at which player jumped
    protected const float jumpDelay = 0.5F; // no vertical action can occur after jump for this much time
    protected bool jumpBuffered = false; // whether a jump is scheduled to immediately follow current jump
    protected bool queueNextJump = false; // if true, will queue up a jump as soon as possible (only applies to direct player controller)

    protected float lastGroundTime = 0F; // last timeframe at which the player landed
    protected const float groundDelay = 0.1F; // no jumping can occur within this much time since landing
    protected Vector3 groundNormal = Vector3.up; // normal vector of the ground on which player is standing

    protected float lastProneTime = 0F; // last timeframe at which player toggled prone
    protected const float proneDelay = 0.5F; // prone toggle cooldown

    protected const float rollSpeedMultiplier = 1.5F; // compared to normal speed
    protected const float rollDuration = 0.66F; // # seconds needed for roll to complete
    protected float lastRollTime = 0F; // time when last roll was initiated
    protected float rollAngle; // angle at which player rolls (absolute)
    protected float rollAngle_SDVel;

    protected const float forwardMoveSpeed = 8.5F; // world space coordinates per second
    protected const float backwardMoveSpeed = 4.5F; // world space coordinates per second
    protected const float strafeSpeed = 6F; // world space coordinates per second
    protected const float jumpForce = 18F; // world space Newtons
    protected const float proneSpeedModifier = 0.5F; // prone movement is only 60% as fast as running

    protected float proneAngleX_SDVel = 0; // speed at which X prone angle is changing
    protected float proneAngleY_SDVel = 0; // speed at which Y prone angle is changing
    protected float proneAngleZ_SDVel = 0; // speed at which Z prone angle is changing
    protected float proneAngle_SmoothSpeed = 0.2F; // prone angle smooth damp modifier

    public virtual void Start()
    {
        playerObject = this.gameObject;
        playerAnimator = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

        playerScale = playerObject.transform.localScale.x; // get player scale from the in game object
        SetAngle(ref playerHorizontalAngle, playerObject.transform.rotation.y); // get player's horizontal rotation from the in game object
        SetAngle(ref playerLastHorizontalAngle, playerHorizontalAngle);
        SetAngle(ref playerBodyAngle, playerHorizontalAngle);

        Physics.queriesHitBackfaces = true; // only needs to be called once, move to static somewhere
    }

    /**
     * Player rolls in the direction facing
     * Returns true if successful
     */
    protected bool Roll(Vector2 rollInput)
    {
        if (!IsRolling() && IsGrounded() && !IsProne())
        { // conditions to roll are met
            rollInput = rollInput == Vector2.zero ? new Vector2(0, 1) : rollInput; // player rolls forward if there is no directional input
            SetAngle(ref rollAngle, playerHorizontalAngle + (rollInput.x >= 0 ? 1 : -1) * Vector2.Angle(rollInput, Vector2.up)); // determine new angle to face while rolling, adjusting for directional input (e.g. +180 if rolling backwards)

            SetRolling(true);
            lastRollTime = Time.time; // update the timestamp for last roll
            rollAngle_SDVel = 0; // reset roll angle smooth damp velocity
            return true;
        }
        return false;
    }

    /**
     * Alternate player's position between standing upright and prone on the ground
     * Returns true if successful
     */
    protected bool ToggleProne()
    {
        if (Time.time - lastProneTime >= proneDelay && IsGrounded() && !IsRolling())
        { // conditions to toggle prone are met
            playerCollider.direction = IsProne() ? 1 : 0; // change collider properties to match new prone status
            playerCollider.center = IsProne() ? new Vector3(0, 2, 0) : new Vector3(0.2F, 1, 0);

            if (!IsProne()) playerObject.transform.position = playerObject.transform.position + new Vector3(0, 0.4F, 0); // teleport player upwards a little to prevent falling through ground
            else
            { // exit prone mode
                playerObject.transform.rotation = Quaternion.Euler(new Vector3(0, playerObject.transform.rotation.y, 0)); // reset x and z angles

                proneAngleX_SDVel = 0; // reset prone angle smooth damp velocities
                proneAngleY_SDVel = 0;
                proneAngleZ_SDVel = 0;
            }
            playerAnimator.SetBool("Prone", !IsProne()); // toggle prone animation
            lastProneTime = Time.time; // update the timestamp for last prone toggle
            return true;
        }
        return false;
    }

    /**
     * Make player jump
     * Returns true if successful
     */
    protected bool Jump()
    {
        if (!IsRolling() && IsGrounded() && !IsProne() && Time.time - lastJumpTime >= jumpDelay)
        { // some conditions are met for jump (didn't jump too recently, grounded, etc.)
            if (Time.time - lastGroundTime >= groundDelay)
            { // not too recent to last landing; can jump
                if (Vector3.Angle(groundNormal, Vector3.up) <= 50F)
                { // jump is allowed if the steepness of the ground surface is less than 50 degrees (flat being 0)
                    SetGrounded(false);
                    lastJumpTime = Time.time; // update the timestamp for last jump
                    playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, jumpForce, playerRigidbody.velocity.z); // send player upwards

                    return true;
                }
            }
            else jumpBuffered = true; // last landing was too recent, but is still grounded (buffer jump)
        }
        return false;
    }

    /**
     * Analyzes player's movement input and performs actions accordingly (e.g. walk forwards or backwards?)
     */
    protected void ParseMovementInput(Vector2 movementInput)
    {
        SetMovementAnimation(movementInput); // update player animation

        if (!IsRolling())
        { // handle movement velocities (doesn't matter when rolling, since direction can't be changed)
            var moveVelocity = movementInput.normalized * (movementInput.y == 0 ? strafeSpeed : movementInput.y == 1 ? forwardMoveSpeed : backwardMoveSpeed); // velocity vector based on input
            var rotatedMoveVelocity = Quaternion.Euler(0, playerHorizontalAngle, 0) * (new Vector3(moveVelocity.y * (IsProne() ? proneSpeedModifier : 1), playerRigidbody.velocity.y, -moveVelocity.x * (IsProne() ? proneSpeedModifier : 1))); // move velocity after adjustment for player horizontal rotation

            // set up forward movement freedom check by casting a ray forwards
            var velocityRay = new Ray(playerObject.transform.position + new Vector3(0, (IsProne() ? 3.5F : 2F), 0), rotatedMoveVelocity.normalized); // how far to extend forward collision check ray, based on whether prone
            UnityEngine.Debug.DrawRay(playerObject.transform.position + new Vector3(0, (IsProne() ? 1.5F : 2F), 0), rotatedMoveVelocity.normalized * (IsProne() ? 1.5F : 1.2F));

            // restrict horizontal movement if there is a blockage
            var moveFreely = !(Physics.SphereCast(velocityRay, 0.4F, out _, 1.5F));
            if (!moveFreely) playerRigidbody.velocity = new Vector3(0, playerRigidbody.velocity.y, 0); // can't move freely, so only keep player's vertical movement component
            else playerRigidbody.velocity = rotatedMoveVelocity; // no blockage, player can move freely

            if (IsGrounded() && IsProne())
            { // re-orient player to be parallel to the ground while they are prone
                // calculate direction of forward vector (left of player) using normal and player's facing direction
                var forwardVectorX = (float)Math.Cos((playerHorizontalAngle - 90) * Math.PI / 180F);
                var forwardVectorZ = (float)-Math.Sin((playerHorizontalAngle - 90) * Math.PI / 180F);
                var forwardVectorY = (-groundNormal.x * forwardVectorX - groundNormal.z * forwardVectorZ) / groundNormal.y;

                // calculate smoothed angles, Y rotation angle remains player horizontal angle
                Vector3 targetAngle = Quaternion.LookRotation(new Vector3(forwardVectorX, forwardVectorY, forwardVectorZ), groundNormal).eulerAngles;
                var newAngleX = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.x, targetAngle.x, ref proneAngleX_SDVel, proneAngle_SmoothSpeed);
                var newAngleZ = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.z, targetAngle.z, ref proneAngleZ_SDVel, proneAngle_SmoothSpeed);

                playerObject.transform.rotation = Quaternion.Euler(newAngleX, playerHorizontalAngle, newAngleZ);
            }
        }
    }

    public virtual void FixedUpdate()
    {
        if (Time.time - lastJumpTime >= jumpDelay && !IsRolling())
        { // perform ground check and ground normal calculations only if needed, aka not right after a jump
            var checkGroundStart = playerObject.transform.position + new Vector3(0, playerCollider.height / 2, 0); // start at vertical halfway point of player
            var checkGroundDist = playerCollider.height / 2 + 0.15F; // how far to check ground, aka 0.15 units below feet
            var checkGroundRay = new Ray(checkGroundStart, Vector3.down);
            var checkGround = Physics.SphereCast(checkGroundRay, 0.5F, out RaycastHit groundHit, checkGroundDist);

            // accumulator to average all normal vectors of local region below feet (3x3 array of closely packed parallel vectors)
            groundNormal = Vector3.zero;
            for (float xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (float zOffset = -1; zOffset <= 1; zOffset++)
                {
                    RaycastHit hit;
                    bool normalExists = true;
                    if (xOffset == 0 && zOffset == 0) hit = groundHit; // don't recalculate for central ground normal vector
                    else
                    { // calculate ground normal vector
                        var ray = new Ray(checkGroundStart + new Vector3(xOffset / 5F, 0, zOffset / 5F), Vector3.down);
                        if (!Physics.Raycast(ray, out hit, checkGroundDist)) normalExists = false;
                    }

                    if (normalExists) groundNormal += hit.normal; // only add normal to calculations if the normal exists
                }
            }
            groundNormal.Normalize(); // get normal direction vector of the surface upon which the player is standing

            // draw ground check line on debug screen
            if (checkGround) UnityEngine.Debug.DrawLine(checkGroundStart, checkGroundStart + Vector3.down * checkGroundDist, Color.red);
            else UnityEngine.Debug.DrawLine(checkGroundStart, checkGroundStart + Vector3.down * checkGroundDist, Color.green);

            if (IsGrounded() && !checkGround) SetGrounded(false); // no longer on the ground, perhaps walked off a ledge?
            else if (!IsGrounded() && checkGround)
            { // landed on ground
                SetGrounded(true);
                lastGroundTime = Time.time;
            }
        }
    }

    public virtual void Update()
    {
        // check if a blink is necessary
        if (Time.time >= nextBlinkTime)
        {
            playerAnimator.SetTrigger("Blink"); // play blink animation
            nextBlinkTime = Time.time + (float)(blinkRandom.NextDouble() * (maxBlinkDelay - minBlinkDelay) + minBlinkDelay); // determine next blink time randomly
        }

        float angleDiff = GetAngleDiff(playerHorizontalAngle, playerLastHorizontalAngle); // rotation since last frame
        float headDiff = GetAngleDiff(playerHorizontalAngle, playerBodyAngle); // head rotation with respect to player body
        UnityEngine.Debug.DrawLine(playerObject.transform.position, playerObject.transform.position + new Vector3(5 * Mathf.Cos(playerHorizontalAngle * (float)Math.PI / 180), 0, -5 * Mathf.Sin(playerHorizontalAngle * (float)Math.PI / 180)), Color.red);
        UnityEngine.Debug.DrawLine(playerObject.transform.position, playerObject.transform.position + new Vector3(5 * Mathf.Cos(playerBodyAngle * (float)Math.PI / 180), 0, -5 * Mathf.Sin(playerBodyAngle * (float)Math.PI / 180)), Color.blue);

        if (!(!playerAnimator.GetBool("Idle") || !IsGrounded() || IsProne() || IsRolling()))
        { // keep stationary turning animation active
            float absHeadDiff = Math.Abs(headDiff);
            if (absHeadDiff < headTurnMax || angleDiff == 0) // angleDiff != 0 ensures that this stage is played while not still actively turning
            { // stage 1 of turn, only the head moves
                SetStage2TurnDirection(0);
                if (headDiff < 0) playerAnimator.Play("Turn Left Stage 1", 2, Mathf.Clamp(absHeadDiff / headTurnMax, 0, 1)); // layer 2 is head turn
                else playerAnimator.Play("Turn Right Stage 1", 2, Mathf.Clamp(absHeadDiff / headTurnMax, 0, 1));
            }
            else
            { // stage 2 of turn, whole body is moving
                SetStage2TurnDirection(headDiff < -headTurnMax ? -1 : 1);
                SetAngle(ref playerBodyAngle, playerHorizontalAngle + headTurnMax * (headDiff < -headTurnMax ? 1 : -1));
                playerAnimator.SetFloat("Stage 2 Turn Speed", (Math.Abs(angleDiff) / Time.deltaTime) / horizontalTurnSpeed * 2F);
            }
        }
        else
        {
            playerBodyAngle = playerHorizontalAngle; // body is aligned with head
            SetStage2TurnDirection(0);
        }

        // prone turning animation?
        if (angleDiff != 0 && IsProne()) SetProneTurning(true);
        else SetProneTurning(false);

        if (Time.time >= lastRollTime + rollDuration && IsRolling()) SetRolling(false); // end previous roll (time's up)
        if (jumpBuffered && Time.time - lastGroundTime >= groundDelay)
        {
            jumpBuffered = false;
            queueNextJump = true;
        }

        if (IsRolling())
        { // send player in correct direction if rolling (continue the roll animation)
            if (Time.time <= lastRollTime + 0.12F)
            { // just a moment after isRolling is turned to true
                var rotationY = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.y, rollAngle, ref rollAngle_SDVel, 0.04F);
                playerObject.transform.rotation = Quaternion.Euler(playerObject.transform.rotation.x, rotationY, playerObject.transform.rotation.z);
            }
            else if (Time.time >= lastRollTime + rollDuration - 0.12F)
            { // just a moment before isRolling is turned to false
                var rotationY = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.y, playerHorizontalAngle, ref rollAngle_SDVel, 0.12F);
                playerObject.transform.rotation = Quaternion.Euler(playerObject.transform.rotation.x, rotationY, playerObject.transform.rotation.z);
            }
            else rollAngle_SDVel = 0;

            var moveVelocity = new Vector2(0, forwardMoveSpeed * rollSpeedMultiplier);
            var rotatedMoveVelocity = Quaternion.Euler(0, rollAngle, 0) * (new Vector3(moveVelocity.y, playerRigidbody.velocity.y, -moveVelocity.x));

            // check if the player can move forward freely
            var velocityRay = new Ray(playerObject.transform.position + new Vector3(0, 2F, 0), rotatedMoveVelocity.normalized);
            if (Physics.SphereCast(velocityRay, 0.4F, out _, 1.5F)) playerRigidbody.velocity = new Vector3(0, playerRigidbody.velocity.y, 0); // cannot move forward due to blockage
            else playerRigidbody.velocity = rotatedMoveVelocity;
        }

        // update player rotation
        if (!IsProne()) playerObject.transform.rotation = Quaternion.Euler(playerObject.transform.rotation.x, playerHorizontalAngle, playerObject.transform.rotation.z);
        playerLastHorizontalAngle = playerHorizontalAngle;
    }

    /**
     * @return a value between -180 and 180 that corresponds to "first's" position with respect to "last"
     */
    protected float GetAngleDiff(float first, float last)
    {
        float angleDiff = first - last;
        if (angleDiff < -180) angleDiff += 360;
        else if (angleDiff >= 180) angleDiff -= 360;
        return angleDiff;
    }

    protected void SetAngle(ref float angle, float newValue)
    {
        newValue %= 360F;
        if (newValue < 0) newValue += 360;
        angle = newValue;
    }

    protected void SetGrounded(bool grounded)
    {
        if (playerAnimator.GetBool("Grounded") != grounded) playerAnimator.SetBool("Grounded", grounded);
    }

    protected void SetProneTurning(bool turning)
    {
        if (playerAnimator.GetBool("Prone Turning") != turning) playerAnimator.SetBool("Prone Turning", turning);
    }

    protected void SetRolling(bool rolling)
    {
        if (playerAnimator.GetBool("Rolling") != rolling) playerAnimator.SetBool("Rolling", rolling);
    }

    private void SetStage2TurnDirection(int direction)
    {
        if (playerAnimator.GetInteger("Stage 2 Turn Direction") != direction) playerAnimator.SetInteger("Stage 2 Turn Direction", direction);
    }

    protected void SetMovementAnimation(Vector2 movement)
    {
        if (playerAnimator.GetInteger("X Movement") != (int)movement.x) playerAnimator.SetInteger("X Movement", (int)movement.x);
        if (playerAnimator.GetInteger("Y Movement") != (int)movement.y) playerAnimator.SetInteger("Y Movement", (int)movement.y);

        if (movement == Vector2.zero && !playerAnimator.GetBool("Idle")) playerAnimator.SetBool("Idle", true);
        else if (movement != Vector2.zero && playerAnimator.GetBool("Idle")) playerAnimator.SetBool("Idle", false);
    }

    protected bool IsGrounded() => playerAnimator.GetBool("Grounded");
    protected bool IsProne() => playerAnimator.GetBool("Prone");
    protected bool IsProneTurning() => playerAnimator.GetBool("Prone Turning");
    protected bool IsRolling() => playerAnimator.GetBool("Rolling");
    protected Vector2 GetMovementAnimation() => new Vector2(playerAnimator.GetInteger("X Movement"), playerAnimator.GetInteger("Y Movement"));
}
