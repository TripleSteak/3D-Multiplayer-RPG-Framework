using Final_Aisle_Shared.Network;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

/**
 * Extension of the player controller class that corresponds to the local player-controlled character
 */
public class DirectPlayerController : PlayerController
{
    public GameObject cameraObject; // the player camera

    public static float CameraMinDist = 0.5F; // closest zoom to player
    public static float CameraMaxDist = 20F; // furthest zoom to player

    private const float autoCameraRotationSpeed = 720F; // speed at which camera returns to normal angle, in degrees/second

    private Vector3 followPoint; // point which is observed by camera
    private Vector2 followPoint_SDVel; // velocity of observation point change (horizontal)
    private float followPointVert_SDVel; // velocity of observation point change (vertical)

    private readonly float horizontalPanSpeed = 270F; // speed at which player can horizontally pan camera, in degrees/second
    private readonly float verticalPanSpeed = 135F; // speed at which player can vertically pan camera, in degrees/second

    private float verticalAngle; // angle between camera and horizontal plane (90 deg means looking straight down)
    private float horizontalAngleOffset; // measure of the camera view angle, NOT the camera's position relative to the player (0 means looking same direction as player)

    private float curCameraDist; // how far the camera is from the player, with 0 being directly on
    private float targetCameraDist; // how far the camera SHOULD be from the player, without obstructions
    private float cameraZoom_SDVel; // velocity of camera zoom change, used for smoothing
    private const float scrollZoomVelocity = 1.0F; // zoom change per mouse scroll "click"

    private float prevMouseX = 0;
    private float prevMouseY = 0; // previous mouse coordinates

    private AbsoluteMousePosition beforeDragPos; // previous mouse coordinates before dragging, in ABSOLUTE full screen coordinates (not app screen)
    private bool draggingMouse = false;

    private Vector2 lastServerMovementInput = Vector2.zero; // last movement input sent to server

    private long fixedUpdateFrameCount = 0; // increments by one during every fixed update

    /*
     * Player/camera control keys
     */
    public static KeyCode MoveForwardKey = KeyCode.W;
    public static KeyCode MoveBackwardKey = KeyCode.S;
    public static KeyCode StrafeLeftKey = KeyCode.Q;
    public static KeyCode StrafeRightKey = KeyCode.E;
    public static KeyCode TurnLeftKey = KeyCode.A;
    public static KeyCode TurnRightKey = KeyCode.D;

    public static KeyCode JumpKey = KeyCode.Space;
    public static KeyCode RollKey = KeyCode.V;
    public static KeyCode ToggleProneKey = KeyCode.LeftControl;

    public static KeyCode PanWithTurnKey = KeyCode.Mouse1;
    public static KeyCode PanNoTurnKey = KeyCode.Mouse0;

    public override void Start()
    {
        base.Start();

        // set starting camera viewport properties
        verticalAngle = 30F;
        horizontalAngleOffset = 0F;

        curCameraDist = 18F;
        targetCameraDist = 18F;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // send periodic positional update to server
        fixedUpdateFrameCount++;
        if (fixedUpdateFrameCount % 10 == 0)
        { // every 10 fixed updates, or 10 * 0.02 = 0.2 seconds OR 5 Hz
            string sendString = PacketDataUtils.Condense(PacketDataUtils.TransformPosition, PacketDataUtils.AbridgeStrings(new string[] { transform.position.x.ToString(), transform.position.y.ToString(), transform.position.z.ToString() }));
            Connection.instance.SendData(sendString); // update server with player's position
        }

        // send player turning rotation data to the server
        if (playerHorizontalAngle != playerLastHorizontalAngle)
        { // only send data if the rotation has changed
            string sendString = PacketDataUtils.Condense(PacketDataUtils.TransformRotation, base.playerHorizontalAngle.ToString());
            Connection.instance.SendData(sendString); // tell server that player rotation has changed
        }
    }

    public override void Update()
    {
        base.Update();

        // check mouse scroll data
        var mouseScrollDelta = Input.mouseScrollDelta.y;
        if (mouseScrollDelta != 0)
        { // zoom in/out required
            targetCameraDist -= mouseScrollDelta * scrollZoomVelocity;
            targetCameraDist = Mathf.Clamp(targetCameraDist, CameraMinDist, CameraMaxDist);
        }

        // get player movement input (directional keys)
        var movementInput = Vector2.zero; // (right, forward)
        if (Input.GetKey(MoveForwardKey)) movementInput.y++;
        if (Input.GetKey(MoveBackwardKey)) movementInput.y--;
        if (Input.GetKey(StrafeLeftKey)) movementInput.x--;
        if (Input.GetKey(StrafeRightKey)) movementInput.x++;

        // relay change in movement to the server
        if (movementInput != lastServerMovementInput)
        {
            string sendString = PacketDataUtils.Condense(PacketDataUtils.MovementInput, PacketDataUtils.AbridgeStrings(new string[] { movementInput.x.ToString(), movementInput.y.ToString() }));
            Connection.instance.SendData(sendString); // communicate change in player movement to the server
            lastServerMovementInput = movementInput;
        }        

        // check for roll input
        if (Input.GetKey(RollKey))
        {
            /*
             * Get movement keys to determine which way to roll, fetching them again here (rather than at the parse movement section)
             * seems to be the most precise in avoiding rolling the wrong direction
            */
            var rollInput = Vector2.zero; // (right, forward)
            if (Input.GetKey(MoveForwardKey)) rollInput.y++;
            if (Input.GetKey(MoveBackwardKey)) rollInput.y--;
            if (Input.GetKey(StrafeLeftKey)) rollInput.x--;
            if (Input.GetKey(StrafeRightKey)) rollInput.x++;

            bool attempt = base.Roll(rollInput);
            if (attempt)
            {
                //UnityEngine.Debug.Log("sending roll info");
                string sendString = PacketDataUtils.Condense(PacketDataUtils.MovementRoll, base.rollAngle.ToString());
                Connection.instance.SendData(sendString); // tell server that the player rolled
            }
        }

        // check for toggle prone input
        if (Input.GetKey(ToggleProneKey))
        {
            bool attempt = base.ToggleProne();
            if (attempt)
            {
                //UnityEngine.Debug.Log("sending toggle prone info");
                string sendString = PacketDataUtils.Condense(PacketDataUtils.MovementToggleProne, base.IsProne().ToString());
                Connection.instance.SendData(sendString); // tell server that the player toggled prone
            }
        }

        // check for jump
        if (Input.GetKey(JumpKey) || base.queueNextJump)
        {
            base.queueNextJump = false;

            bool attempt = base.Jump();
            if (attempt)
            {
                string sendString = PacketDataUtils.Condense(PacketDataUtils.MovementJump, "");
                Connection.instance.SendData(sendString); // tell server that the jumped
            }
        }

        // get mouse movement distance as a percentage of screen size
        var mouseInput = Input.mousePosition;
        var dxPercent = (mouseInput.x - prevMouseX) / Screen.width;
        var dyPercent = (mouseInput.y - prevMouseY) / Screen.height;

        // update mouse positions
        prevMouseX = mouseInput.x;
        prevMouseY = mouseInput.y;

        // get player rotation input from keyboard
        var keyboardRotationInput = 0F;
        if (Input.GetKey(TurnLeftKey)) keyboardRotationInput--;
        if (Input.GetKey(TurnRightKey)) keyboardRotationInput++;
        SetAngle(ref playerHorizontalAngle, playerHorizontalAngle + keyboardRotationInput * horizontalTurnSpeed * Time.deltaTime);

        if (Input.GetKey(PanNoTurnKey))
        {
            horizontalAngleOffset += dxPercent * horizontalPanSpeed; // full screen-width swipe is 180 degrees
            horizontalAngleOffset %= 360F;

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
            if (Input.GetKey(PanWithTurnKey))
            {
                SetAngle(ref playerHorizontalAngle, playerHorizontalAngle + dxPercent * horizontalPanSpeed);

                verticalAngle += -dyPercent * verticalPanSpeed; // full screen-height swipe is 90 degrees
                verticalAngle = Mathf.Clamp(verticalAngle, -90F, 90F);

                if (!draggingMouse) // start mouse drag
                {
                    draggingMouse = true;
                    Cursor.visible = false;
                }
            }
            else if (draggingMouse)
            { // finished mouse drag
                SetCursorPos(beforeDragPos.x, beforeDragPos.y);
                draggingMouse = false;
                Cursor.visible = true;
            }

            if (base.GetMovementAnimation() != Vector2.zero && !IsRolling())
            { // if player is walking, reposition camera on player
                if (horizontalAngleOffset < 0) horizontalAngleOffset += 360F;
                if ((horizontalAngleOffset > 0 && horizontalAngleOffset <= autoCameraRotationSpeed * Time.deltaTime) || (horizontalAngleOffset <= 360 && horizontalAngleOffset >= 360 - autoCameraRotationSpeed * Time.deltaTime))
                    horizontalAngleOffset = 0;
                else if (horizontalAngleOffset > 0 && horizontalAngleOffset <= 180)
                {
                    horizontalAngleOffset -= autoCameraRotationSpeed * Time.deltaTime;
                }
                else if (horizontalAngleOffset > 180 && horizontalAngleOffset < 360)
                {
                    horizontalAngleOffset += autoCameraRotationSpeed * Time.deltaTime;
                }
            }
        }

        if (!draggingMouse)
        { // update pre-drag coordinates
            GetCursorPos(out beforeDragPos);
        }

        ParseMovementInput(movementInput); // make changes to player movement and animation according to movement input (call after movement input/comparison handling)
        
    }

    void LateUpdate()
    {
        PlaceCamera(); // re-adjust the camera's position at the end of every fixed update
    }

    /**
     * Determines where the camera should be placed for the player (called in Late Update)
     */
    private void PlaceCamera()
    {
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
        var followPoint2D = Vector2.SmoothDamp(new Vector2(followPoint.x, followPoint.z), new Vector2(modifiedPlayerPos.x, modifiedPlayerPos.z), ref followPoint_SDVel, 0.25F);
        float followPointVert = Mathf.SmoothDamp(followPoint.y, modifiedPlayerPos.y, ref followPointVert_SDVel, 0.5F);
        followPoint = new Vector3(followPoint2D.x, followPointVert, followPoint2D.y);

        // determine camera position based on camera angles
        var eyePos = new Vector3(0, 3 * playerScale, 0); // 0.135 = 3 * 0.045, adjusted for player object scale
        var cameraX = followPoint.x + CameraMaxDist * (float)-Math.Cos((horizontalAngleOffset + playerHorizontalAngle) * Math.PI / 180.0) * (float)Math.Cos(verticalAngle * Math.PI / 180.0);
        var cameraY = followPoint.y + CameraMaxDist * (float)Math.Sin(verticalAngle * Math.PI / 180.0) + eyePos.y; // +3 to view from eyes, not feet
        var cameraZ = followPoint.z + CameraMaxDist * (float)Math.Sin((horizontalAngleOffset + playerHorizontalAngle) * Math.PI / 180.0) * (float)Math.Cos(verticalAngle * Math.PI / 180.0);

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
            nextFrameCameraDist = Vector3.Distance(hit.point, lineEnd) - 0.5F; // spacing between camera and obstructing wall
        }
        else
            UnityEngine.Debug.DrawLine(lineEnd, lineEnd + sweepDirection * sweepDist, Color.green);

        nextFrameCameraDist = Mathf.Clamp(nextFrameCameraDist, CameraMinDist, targetCameraDist);

        // readjust camera rotation
        var cameraRotX = verticalAngle;
        var cameraRotY = horizontalAngleOffset + 90;
        cameraObject.transform.rotation = Quaternion.Euler(cameraRotX, cameraRotY + playerHorizontalAngle, 0);

        // set camera position, with smoothing
        curCameraDist = Mathf.SmoothDamp(curCameraDist, nextFrameCameraDist, ref cameraZoom_SDVel, 0.035F);
        var targetPos = followPoint + sweepDirection * curCameraDist + eyePos;
        cameraObject.transform.position = targetPos;

        // hide all necessary entities based on clipping
        foreach (EntityClipper clipper in EntityClipper.EntityClippers)
        {
            Transform entityPos = clipper.gameObject.transform;
            float distance = Vector3.Distance(entityPos.position + new Vector3(0, clipper.HalfHeight, 0), cameraObject.transform.position);

            if (distance <= clipper.CameraClipRadius && !clipper.IsTransparent)
            { // hide the entity
                foreach (SkinnedMeshRenderer renderer in clipper.Renderers) renderer.enabled = false;
                clipper.IsTransparent = true;
            }
            else if (distance > clipper.CameraClipRadius && clipper.IsTransparent)
            { // show the entity
                foreach (SkinnedMeshRenderer renderer in clipper.Renderers) renderer.enabled = true;
                clipper.IsTransparent = false;
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
