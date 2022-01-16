using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GameObject playerObject;
    private SkinnedMeshRenderer playerRenderer;
    private Animator playerAnimator;
    private Rigidbody playerRigidbody;
    private CapsuleCollider playerCollider;

    public GameObject cameraObject;

    private float playerScale; // scale of player model in real world
    private bool isPlayerTransparent = false; // whether the player model's rendering mode is set to transparent

    public static float CameraMinDist = 0.5F;
    public static float CameraMaxDist = 20F;

    private const float autoCameraRotationSpeed = 720F; // speed at which camera returns to normal angle, in degrees/second

    private Vector3 followPoint; // point which is observed by camera
    private Vector2 followPointVelocity = Vector2.zero; // velocity of observation point change (horizontal)
    private float followPointVertVelocity = 0; // velocity of observation point change (vertical)

    private float horizontalPanSpeed = 270F; // speed at which player can horizontally pan camera, in degrees/second
    private float verticalPanSpeed = 135F; // speed at which player can vertically pan camera, in degrees/second
    private float horizontalTurnSpeed = 135F; // speed at which A and D buttons rotate the player

    private float verticalAngle; // angle between camera and horizontal plane (90 deg means looking straight down)
    private float horizontalAngle; // measure of the camera view angle, NOT the camera's position relative to the player (0 means looking same direction as player)
    private float playerHorizontalAngle; // direction the player should be facing

    private float curCameraDist; // how far the camera is from the player, with 0 being directly on
    private float targetCameraDist; // how far the camera SHOULD be from the player, without obstructions
    private float cameraZoomVelocity; // velocity of camera zoom change, used for smoothing
    private const float scrollZoomVelocity = 1.0F; // zoom change per mouse scroll "click"

    private float prevMouseX = 0;
    private float prevMouseY = 0; // previous mouse coordinates

    private AbsoluteMousePosition beforeDragPos; // previous mouse coordinates before dragging, in ABSOLUTE full screen coordinates (not app screen)
    private bool draggingMouse = false;

    private float nextBlinkTime = 0F; // next timeframe at which the player should blink
    private const float minBlinkDelay = 0.2F; // minimum 0.2 seconds between blinks
    private const float maxBlinkDelay = 1.6F; // maximum 1.2 seconds between blinks

    private float lastJumpTime = 0F; // last timeframe at which player jumped
    private const float jumpDelay = 0.5F; // no vertical action can occur after jump for this much time
    private bool isGrounded = false;

    private float lastProneTime = 0F; // last timeframe at which player toggled prone
    private const float proneDelay = 0.5F; // prone toggle cooldown
    private bool isProne = false;

    private float rollSpeedMultiplier = 1.5F; // compared to normal speed
    private float rollDuration = 0.8F; // # seconds needed for roll to complete
    private float lastRollTime = 0F; // time when last roll was initiated
    private float rollAngle; // angle at which player rolls (absolute)
    private float rollAngleVelocity; // keeps track of horizontal angle restoration after roll
    private bool isRolling = false;

    private Vector2 lastMovementInput = new Vector2(0, 0); // last WASD movement input, to determine if new animation needs to start

    private float forwardMoveSpeed = 8.5F; // world space coordinates per second
    private float backwardMoveSpeed = 4.5F; // world space coordinates per second
    private float strafeSpeed = 6F; // world space coordinates per second
    private float jumpForce = 18F; // world space Newtons
    private float proneSpeedModifier = 0.5F; // prone movement is only 60% as fast as running

    private float proneAngleXSpeed = 0; // speed at which X prone angle is changing
    private float proneAngleYSpeed = 0; // speed at which Y prone angle is changing
    private float proneAngleZSpeed = 0; // speed at which Z prone angle is changing
    private float proneAngleSmoothSpeed = 0.2F; // prone angle smooth damp modifier

    /*
     * Player/camera control keys
     */
    public static KeyCode MoveForward = KeyCode.W;
    public static KeyCode MoveBackward = KeyCode.S;
    public static KeyCode StrafeLeft = KeyCode.Q;
    public static KeyCode StrafeRight = KeyCode.E;
    public static KeyCode TurnLeft = KeyCode.A;
    public static KeyCode TurnRight = KeyCode.D;

    public static KeyCode Jump = KeyCode.Space;
    public static KeyCode Roll = KeyCode.V;
    public static KeyCode ToggleCrouch = KeyCode.LeftControl;

    public static KeyCode PanWithTurn = KeyCode.Mouse1;
    public static KeyCode PanNoTurn = KeyCode.Mouse0;

    void Start()
    {
        playerObject = this.gameObject;
        playerRenderer = playerObject.transform.GetChild(1).gameObject.GetComponent<SkinnedMeshRenderer>();
        playerAnimator = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

        playerScale = playerObject.transform.localScale.x;
        playerHorizontalAngle = playerObject.transform.rotation.y;

        // set starting camera viewport properties
        verticalAngle = 30F;
        horizontalAngle = 0F;

        curCameraDist = 18F;
        targetCameraDist = 18F;

        Physics.queriesHitBackfaces = true;
    }

    void FixedUpdate()
    {
        /*
         * Check for roll input
         */
        if (Time.time >= lastRollTime + rollDuration)
        { // enough time has passed since last roll toggle
            if (isGrounded && !isProne && Input.GetKey(Roll))
            {
                var rollInput = lastMovementInput == Vector2.zero ? new Vector2(0, 1) : lastMovementInput;

                // determine new angle
                rollAngle = playerHorizontalAngle;
                if (rollInput.x == 0)
                {
                    if (rollInput.y == -1) rollAngle += 180;
                }
                else if (rollInput.x == 1)
                {
                    if (rollInput.y == 1) rollAngle += 45;
                    else if (rollInput.y == 0) rollAngle += 90;
                    else rollAngle += 135;
                }
                else
                {
                    if (rollInput.y == 1) rollAngle -= 45;
                    else if (rollInput.y == 0) rollAngle -= 90;
                    else rollAngle -= 135;
                }

                playerAnimator.SetTrigger("Roll");
                isRolling = true;
                lastRollTime = Time.time;

                rollAngleVelocity = 0;
            }
            else if (isRolling)
            { // no longer rolling, end rolling here
                isRolling = false;
                lastMovementInput = Vector2.zero;
            }
        }

        if (isRolling)
        { // send player in correct direction if rolling
            if (Time.time <= lastRollTime + 0.12F)
            { // just a moment after isRolling is turned to true
                float rotationY = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.y, rollAngle, ref rollAngleVelocity, 0.04F);
                playerObject.transform.rotation = Quaternion.Euler(playerObject.transform.rotation.x, rotationY, playerObject.transform.rotation.z);
            }
            else if (Time.time >= lastRollTime + rollDuration - 0.12F)
            { // just a moment before isRolling is turned to false
                float rotationY = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.y, playerHorizontalAngle, ref rollAngleVelocity, 0.12F);
                playerObject.transform.rotation = Quaternion.Euler(playerObject.transform.rotation.x, rotationY, playerObject.transform.rotation.z);
            }
            else rollAngleVelocity = 0;

            var moveVelocity = new Vector2(0, forwardMoveSpeed * rollSpeedMultiplier);
            var rotatedMoveVelocity = Quaternion.Euler(0, rollAngle, 0) * (new Vector3(moveVelocity.y, playerRigidbody.velocity.y, -moveVelocity.x));

            // check if the player can move forward freely
            var velocityRay = new Ray(playerObject.transform.position + new Vector3(0, 2F, 0), rotatedMoveVelocity.normalized);
            if (Physics.SphereCast(velocityRay, 0.4F, out _, 1.5F))
            { // cannot move forward due to blockage
                playerRigidbody.velocity = new Vector3(0, playerRigidbody.velocity.y, 0);
            }
            else
            {
                playerRigidbody.velocity = rotatedMoveVelocity;
            }
        }

        /*
         * Check for prone
         */
        if (Time.time - lastProneTime >= proneDelay && isGrounded && !isRolling)
        { // enough time has passed since last prone toggle
            if (Input.GetKey(ToggleCrouch))
            {
                if (!isProne)
                { // enter prone mode
                    playerAnimator.ResetTrigger("Exit Prone");
                    playerAnimator.SetTrigger("Enter Prone");
                    playerCollider.direction = 0;
                    playerCollider.height = 2F;
                    playerCollider.center = new Vector3(0.2F, 1, 0);
                    playerObject.transform.position = playerObject.transform.position + new Vector3(0, 0.4F, 0); // teleport player upwards a little to prevent falling through ground
                }
                else
                { // exit prone mode
                    playerAnimator.ResetTrigger("Enter Prone");
                    playerAnimator.SetTrigger("Exit Prone");
                    playerAnimator.SetBool("Prone Moving", false);
                    playerCollider.direction = 1;
                    playerCollider.height = 3.9F;
                    playerCollider.center = new Vector3(0, 2, 0);
                    playerObject.transform.rotation = Quaternion.Euler(new Vector3(0, playerObject.transform.rotation.y, 0)); // reset x and z angleswe

                    // reset prone angle data
                    proneAngleXSpeed = 0;
                    proneAngleYSpeed = 0;
                    proneAngleZSpeed = 0;
                }
                isProne = !isProne;
                lastProneTime = Time.time;
                lastMovementInput = new Vector2(0, 0);
            }
        }

        /*
         * Check is grounded for jump
         */
        if (Time.time - lastJumpTime >= jumpDelay && !isRolling)
        { // enough time has passed since last jump for vertical motion checks
            if (isGrounded && !isProne && Input.GetKey(Jump))
            { // player wants to jump
                playerAnimator.ResetTrigger("Land");
                playerAnimator.SetTrigger("Jump");
                isGrounded = false;
                lastJumpTime = Time.time;

                playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, jumpForce, playerRigidbody.velocity.z);
            }
            else
            {
                RaycastHit groundHit;
                var checkGroundStart = playerObject.transform.position + new Vector3(0, playerCollider.height / 2, 0);
                var checkGroundDirection = new Vector3(0, -1, 0);
                var checkGroundDist = playerCollider.height / 2 + 0.15F;
                var checkGroundRay = new Ray(checkGroundStart, checkGroundDirection);
                var checkGround = Physics.SphereCast(checkGroundRay, 0.5F, out groundHit, checkGroundDist);

                // draw ground check line
                if (checkGround) UnityEngine.Debug.DrawLine(checkGroundStart, checkGroundStart + checkGroundDirection * checkGroundDist, Color.red);
                else UnityEngine.Debug.DrawLine(checkGroundStart, checkGroundStart + checkGroundDirection * checkGroundDist, Color.green);

                if (isGrounded && !checkGround)
                { // no longer on the ground
                    if (!isProne)
                    {
                        playerAnimator.ResetTrigger("Land");
                        playerAnimator.SetTrigger("Jump");
                    }
                    isGrounded = false;
                }
                else if (!isGrounded && checkGround)
                { // just hit ground
                    if (!isProne)
                    {
                        playerAnimator.SetTrigger("Land");
                        lastMovementInput = new Vector2(0, 0);
                    }
                    isGrounded = true;
                }

                if (isGrounded && isProne)
                { // re-orient player to be parallel to the ground
                    // accumulator to average all normal vectors
                    var averageNormal = Vector3.zero;
                    for (float xOffset = -1; xOffset <= 1; xOffset++)
                    {
                        for (float zOffset = -1; zOffset <= 1; zOffset++)
                        {
                            RaycastHit hit;
                            bool normalExists = true;
                            if (xOffset == 0 && zOffset == 0) hit = groundHit;
                            else
                            {
                                Ray ray = new Ray(checkGroundStart + new Vector3(xOffset / 5F, 0, zOffset / 5F), checkGroundDirection);
                                if (!Physics.Raycast(ray, out hit, checkGroundDist)) normalExists = false;
                            }

                            if (normalExists) averageNormal += hit.normal;
                        }
                    }
                    averageNormal = averageNormal.normalized; // get normal direction vector

                    // calculate direction of forward vector (left of player) using normal and player's facing direction
                    var forwardVectorX = (float)Math.Cos((playerHorizontalAngle - 90) * Math.PI / 180F);
                    var forwardVectorZ = (float)-Math.Sin((playerHorizontalAngle - 90) * Math.PI / 180F);
                    var forwardVectorY = (-averageNormal.x * forwardVectorX - averageNormal.z * forwardVectorZ) / averageNormal.y;

                    // calculate smoothed angles
                    Vector3 targetAngle = Quaternion.LookRotation(new Vector3(forwardVectorX, forwardVectorY, forwardVectorZ), averageNormal).eulerAngles;
                    var newAngleX = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.x, targetAngle.x, ref proneAngleXSpeed, proneAngleSmoothSpeed);
                    var newAngleY = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.y, targetAngle.y, ref proneAngleYSpeed, proneAngleSmoothSpeed);
                    var newAngleZ = Mathf.SmoothDampAngle(playerObject.transform.rotation.eulerAngles.z, targetAngle.z, ref proneAngleZSpeed, proneAngleSmoothSpeed);

                    playerObject.transform.rotation = Quaternion.Euler(newAngleX, newAngleY, newAngleZ);
                }
            }
        }

        PlaceCamera();
    }

    void Update()
    {
        playerHorizontalAngle %= 360F;

        // get mouse movement distance as a percentage of screen size
        var mouseInput = Input.mousePosition;
        var dxPercent = (mouseInput.x - prevMouseX) / Screen.width;
        var dyPercent = (mouseInput.y - prevMouseY) / Screen.height;

        // update mouse positions
        prevMouseX = mouseInput.x;
        prevMouseY = mouseInput.y;

        // get player rotation input from keyboard
        var keyboardRotationInput = 0F;
        if (Input.GetKey(TurnLeft)) keyboardRotationInput--;
        if (Input.GetKey(TurnRight)) keyboardRotationInput++;
        playerHorizontalAngle += keyboardRotationInput * horizontalTurnSpeed * Time.deltaTime;

        if (Input.GetKey(PanNoTurn))
        {
            horizontalAngle += dxPercent * horizontalPanSpeed; // full screen-width swipe is 180 degrees
            horizontalAngle %= 360F;

            verticalAngle += -dyPercent * verticalPanSpeed; // full screen-height swipe is 90 degrees
            verticalAngle = Mathf.Clamp(verticalAngle, -90F, 90F);

            if (!draggingMouse) // start mouse drag
            {
                draggingMouse = true;
                Cursor.visible = false;
            }
        }
        else if (Input.GetKey(PanWithTurn))
        {
            playerHorizontalAngle += dxPercent * horizontalPanSpeed;

            verticalAngle += -dyPercent * verticalPanSpeed; // full screen-height swipe is 90 degrees
            verticalAngle = Mathf.Clamp(verticalAngle, -90F, 90F);

            if (!draggingMouse) // start mouse drag
            {
                draggingMouse = true;
                Cursor.visible = false;
            }
        }
        else
        {
            if (draggingMouse)
            { // finished mouse drag
                SetCursorPos(beforeDragPos.x, beforeDragPos.y);
                draggingMouse = false;
                Cursor.visible = true;
            }

            if (lastMovementInput != Vector2.zero && !isRolling)
            { // if player is walking, reposition camera on player
                if (horizontalAngle < 0) horizontalAngle += 360F;
                if ((horizontalAngle > 0 && horizontalAngle <= autoCameraRotationSpeed * Time.deltaTime) || (horizontalAngle <= 360 && horizontalAngle >= 360 - autoCameraRotationSpeed * Time.deltaTime))
                    horizontalAngle = 0;
                else if (horizontalAngle > 0 && horizontalAngle <= 180)
                {
                    horizontalAngle -= autoCameraRotationSpeed * Time.deltaTime;
                }
                else if (horizontalAngle > 180 && horizontalAngle < 360)
                {
                    horizontalAngle += autoCameraRotationSpeed * Time.deltaTime;
                }
            }
        }

        if (!draggingMouse)
        { // update pre-drag coordinates
            GetCursorPos(out beforeDragPos);
        }

        // check mouse scroll data
        var mouseScrollDelta = Input.mouseScrollDelta.y;
        if (mouseScrollDelta != 0)
        { // zoom in/out required
            targetCameraDist -= mouseScrollDelta * scrollZoomVelocity;
            targetCameraDist = Mathf.Clamp(targetCameraDist, CameraMinDist, CameraMaxDist);
        }

        // check if a blink is necessary
        if (Time.time >= nextBlinkTime)
        {
            playerAnimator.SetTrigger("Blink"); // play blink animation

            var rand = new System.Random();
            nextBlinkTime = Time.time + (float)(rand.NextDouble() * (maxBlinkDelay - minBlinkDelay) + minBlinkDelay);
        }

        // get player movement input
        var movementInput = new Vector2(0, 0); // (right, forward)
        if (Input.GetKey(MoveForward)) movementInput.y++;
        if (Input.GetKey(MoveBackward)) movementInput.y--;
        if (Input.GetKey(StrafeLeft)) movementInput.x--;
        if (Input.GetKey(StrafeRight)) movementInput.x++;

        // handle movement animation changes
        if (movementInput != lastMovementInput && !isRolling)
        {
            if (isProne)
            { // prone animation changes
                if (movementInput == Vector2.zero) playerAnimator.SetBool("Prone Moving", false);
                else playerAnimator.SetBool("Prone Moving", true);
            }
            else
            { // standing animation changes
                if (movementInput.x == 0)
                {
                    if (movementInput.y == 0) playerAnimator.SetTrigger("Stop Walking");
                    if (movementInput.y == 1) playerAnimator.SetTrigger("Walk Forward");
                    else if (movementInput.y == -1) playerAnimator.SetTrigger("Walk Backward");
                }
                else if (movementInput.x == -1)
                { // left
                    if (movementInput.y == 0) playerAnimator.SetTrigger("Strafe Left");
                    if (movementInput.y == 1) playerAnimator.SetTrigger("Walk Forward Left");
                    else if (movementInput.y == -1) playerAnimator.SetTrigger("Walk Backward Left");
                }
                else
                { // right
                    if (movementInput.y == 0) playerAnimator.SetTrigger("Strafe Right");
                    if (movementInput.y == 1) playerAnimator.SetTrigger("Walk Forward Right");
                    else if (movementInput.y == -1) playerAnimator.SetTrigger("Walk Backward Right");
                }
            }
        }

        // handle movement velocities
        if (!isRolling)
        {
            Vector2 moveVelocity; // velocity vector before player rotation transformations
            if (movementInput.x == 0)
            {
                if (movementInput.y == 0) moveVelocity = new Vector2(0, 0);
                else if (movementInput.y == 1) moveVelocity = new Vector2(0, forwardMoveSpeed);
                else moveVelocity = new Vector2(0, -backwardMoveSpeed);
            }
            else if (movementInput.x == -1)
            { // left
                if (movementInput.y == 0) moveVelocity = new Vector2(-strafeSpeed, 0);
                else if (movementInput.y == 1) moveVelocity = new Vector2((float)(-forwardMoveSpeed * Math.Sin(Math.PI / 4)), (float)(forwardMoveSpeed * Math.Sin(Math.PI / 4)));
                else moveVelocity = new Vector2((float)(-backwardMoveSpeed * Math.Sin(Math.PI / 4)), -(float)(backwardMoveSpeed * Math.Sin(Math.PI / 4)));
            }
            else
            { // right
                if (movementInput.y == 0) moveVelocity = new Vector2(strafeSpeed, 0);
                else if (movementInput.y == 1) moveVelocity = new Vector2((float)(forwardMoveSpeed * Math.Sin(Math.PI / 4)), (float)(forwardMoveSpeed * Math.Sin(Math.PI / 4)));
                else moveVelocity = new Vector2((float)(backwardMoveSpeed * Math.Sin(Math.PI / 4)), -(float)(backwardMoveSpeed * Math.Sin(Math.PI / 4)));
            }
            var rotatedMoveVelocity = Quaternion.Euler(0, playerHorizontalAngle, 0) * (new Vector3(moveVelocity.y * (isProne ? proneSpeedModifier : 1), playerRigidbody.velocity.y, -moveVelocity.x * (isProne ? proneSpeedModifier : 1)));

            // check if the player can move forward freely
            bool moveFreely = true;
            if (!isProne)
            {
                var velocityRay = new Ray(playerObject.transform.position + new Vector3(0, 2F, 0), rotatedMoveVelocity.normalized);
                if (Physics.SphereCast(velocityRay, 0.4F, out _, 1.5F))
                { // cannot move forward due to blockage
                    playerRigidbody.velocity = new Vector3(0, playerRigidbody.velocity.y, 0);
                    moveFreely = false;
                }
            }
            else
            {
                var velocityRay = new Ray(playerObject.transform.position + new Vector3(0, 3.5F, 0), rotatedMoveVelocity.normalized);
                if (Physics.SphereCast(velocityRay, 0.4F, out _, 1.5F))
                { // cannot move forward due to blockage
                    playerRigidbody.velocity = new Vector3(0, playerRigidbody.velocity.y, 0);
                    moveFreely = false;
                }
            }

            if (moveFreely)
            { // move freely
                playerRigidbody.velocity = rotatedMoveVelocity;
            }
            lastMovementInput = movementInput;

            // update player rotation
            if (!isProne) playerObject.transform.rotation = Quaternion.Euler(playerObject.transform.rotation.x, playerHorizontalAngle, playerObject.transform.rotation.z);
        }
    }

    /**
     * Determines where the camera should be placed for the player (called in Late Update)
     */
    private void PlaceCamera()
    {
        var prevCameraDist = curCameraDist;

        // update vertical position
        RaycastHit surfaceHit;
        var checkGroundStart = playerObject.transform.position + new Vector3(0, playerCollider.height / 2, 0);
        var checkGroundDirection = new Vector3(0, -1, 0);
        var checkGroundDist = 12F;
        var checkGroundRay = new Ray(checkGroundStart, checkGroundDirection);
        var checkGround = Physics.SphereCast(checkGroundRay, 0.5F, out surfaceHit, checkGroundDist, 1 << 6);
        var playerTargetVerticalPosition = playerObject.transform.position.y;

        if (checkGround)
        {
            playerTargetVerticalPosition = surfaceHit.point.y;
            UnityEngine.Debug.DrawLine(checkGroundStart, checkGroundStart + checkGroundDirection * checkGroundDist, Color.yellow);
        }

        // player position for camera (ground underneath)
        var modifiedPlayerPos = new Vector3(playerObject.transform.position.x, playerTargetVerticalPosition, playerObject.transform.position.z);

        // adjust follow position
        var followPoint2D = Vector2.SmoothDamp(new Vector2(followPoint.x, followPoint.z), new Vector2(modifiedPlayerPos.x, modifiedPlayerPos.z), ref followPointVelocity, 0.25F);
        float followPointVert = Mathf.SmoothDamp(followPoint.y, modifiedPlayerPos.y, ref followPointVertVelocity, 0.5F);
        followPoint = new Vector3(followPoint2D.x, followPointVert, followPoint2D.y);

        // determine camera position based on camera angles
        var eyePos = new Vector3(0, 3 * playerScale, 0); // 0.135 = 3 * 0.045, adjusted for player object scale
        var cameraX = followPoint.x + CameraMaxDist * (float)-Math.Cos((horizontalAngle + playerHorizontalAngle) * Math.PI / 180.0) * (float)Math.Cos(verticalAngle * Math.PI / 180.0);
        var cameraY = followPoint.y + CameraMaxDist * (float)Math.Sin(verticalAngle * Math.PI / 180.0) + eyePos.y; // +3 to view from eyes, not feet
        var cameraZ = followPoint.z + CameraMaxDist * (float)Math.Sin((horizontalAngle + playerHorizontalAngle) * Math.PI / 180.0) * (float)Math.Cos(verticalAngle * Math.PI / 180.0);

        /*
         * Check for blocking objects
         */
        RaycastHit hit;
        var lineStart = new Vector3(cameraX, cameraY, cameraZ);
        var lineEnd = followPoint + eyePos;
        var sweepDirection = (lineStart - lineEnd).normalized;
        lineEnd += sweepDirection * 0.5F; // extra buffer distance behind player
        var sweepDist = Vector3.Distance(lineStart, lineEnd) + 1.5F; // extra buffer distance
        var nextFrameCameraDist = CameraMaxDist;
        var sweepRay = new Ray(lineEnd, sweepDirection);

        if (Physics.SphereCast(sweepRay, 0.5F, out hit, sweepDist)) //Physics.Raycast(lineEnd, sweepDirection, out hit, sweepDist))
        {
            UnityEngine.Debug.DrawLine(lineEnd, lineEnd + sweepDirection * sweepDist, Color.red);

            // calculate camera's supposed distance from player
            nextFrameCameraDist = Vector3.Distance(hit.point, lineEnd) - 1.5F; // spacing between camera and obstructing wall
        }
        else
            UnityEngine.Debug.DrawLine(lineEnd, lineEnd + sweepDirection * sweepDist, Color.green);

        nextFrameCameraDist = Mathf.Clamp(nextFrameCameraDist, CameraMinDist, targetCameraDist);

        // readjust camera rotation
        var cameraRotX = verticalAngle;
        var cameraRotY = horizontalAngle + 90;
        cameraObject.transform.rotation = Quaternion.Euler(cameraRotX, cameraRotY + playerHorizontalAngle, 0);

        // set camera position, with smoothing
        curCameraDist = Mathf.SmoothDamp(curCameraDist, nextFrameCameraDist, ref cameraZoomVelocity, 0.035F);
        var targetPos = followPoint + sweepDirection * curCameraDist + eyePos;
        cameraObject.transform.position = targetPos;

        // set player transparency, but only if camera distance has changed
        if (curCameraDist != prevCameraDist)
        {
            if (curCameraDist < 2 * playerScale && !isPlayerTransparent)
            {
                isPlayerTransparent = true;
                playerRenderer.enabled = false;
            }
            else if (curCameraDist >= 2 * playerScale && isPlayerTransparent)
            {
                isPlayerTransparent = false;
                playerRenderer.enabled = true;
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y); // sets cursor position on screen (full screen coordinates, not app screen)

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out AbsoluteMousePosition lpMousePosition); // gets cursor position from full screen coordinates (not app screen)

    [StructLayout(LayoutKind.Sequential)]
    private struct AbsoluteMousePosition // full screen-space coordinates (not app screen)
    {
        public int x;
        public int y;
    }
}
