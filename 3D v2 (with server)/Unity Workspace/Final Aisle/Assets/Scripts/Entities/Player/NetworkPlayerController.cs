using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayerController : PlayerController
{
    private Vector2 activeMovementInput = Vector2.zero;

    private Vector3 verifiedPosition = Vector3.zero; // last position verified by the server (target position)
    private Vector3 positionVelocity = Vector3.zero; // velocity referenced by the positional smooth damp function

    private float lastRotation = 0; // target player rotation
    private float rotationVelocity = 0; // velocity refernced by the rotational smooth damp function

    public Queue<string> ActionQueue = new Queue<string>(); // queue is filled by the network commands, which then execute on fixed update (does not include movement)

    public override void Start()
    {
        base.Start();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override void Update()
    {
        var targetRotation = lastRotation;
        var diff = GetAngleDiff(targetRotation, playerHorizontalAngle);
        if (diff > 0 && targetRotation < playerHorizontalAngle) targetRotation += 360;
        else if (diff < 0 && targetRotation > playerHorizontalAngle) targetRotation -= 360;

        SetAngle(ref base.playerHorizontalAngle, Mathf.SmoothDamp(playerHorizontalAngle, targetRotation, ref rotationVelocity, 0.1F)); //lastRotation; // update player rotation
        base.Update();

        ParseMovementInput(activeMovementInput);

        // adjust player's position smoothly, but only if the player is moving (so that the player doesn't slide)
        if (base.GetMovementAnimation() != Vector2.zero)
            base.gameObject.transform.position = Vector3.SmoothDamp(base.gameObject.transform.position, verifiedPosition, ref positionVelocity, 0.2F);

    
        if (ActionQueue.Count != 0)
        { // actions need to be done
            string top = ActionQueue.Peek();

            if (top.StartsWith("roll"))
            {
                float rollAngle = float.Parse(top.Substring(4));
                //UnityEngine.Debug.Log(rollAngle);

                base.playerHorizontalAngle = rollAngle;
                bool attempt = base.Roll(Vector2.zero);
                if (attempt) ActionQueue.Dequeue();
            }
            else if (top.Equals("jump"))
            {
                bool attempt = base.Jump();
                if (attempt) ActionQueue.Dequeue();
            }
            else if (top.StartsWith("prone"))
            {
                bool proneState = bool.Parse(top.Substring(6));

                if (base.IsProne() == proneState) ActionQueue.Dequeue(); // no need to toggle, already in the right state
                else
                {
                    bool attempt = base.ToggleProne();
                    if (attempt) ActionQueue.Dequeue();
                }
            }
        }

    }

    public void UpdateMovementInput(float moveX, float moveY) => activeMovementInput = new Vector2(moveX, moveY);

    public void UpdatePosition(Vector3 position) => verifiedPosition = position;

    public void UpdateRotation(float rotation) => lastRotation = rotation; // apply smoothing function later

    public void QueueMovementRoll(float rotation) => ActionQueue.Enqueue("roll" + rotation.ToString()); // append roll angle to the end of queue command

    public void QueueMovementJump() => ActionQueue.Enqueue("jump");

    public void QueueMovementToggleProne(bool proneState) => ActionQueue.Enqueue("prone:" + proneState);
}
