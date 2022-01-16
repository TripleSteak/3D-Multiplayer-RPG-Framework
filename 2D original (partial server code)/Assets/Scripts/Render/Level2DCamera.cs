using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level2DCamera : MonoBehaviour
{
    /*
     *  Screen boundaries as a percentage of screen dimensions (always centered)
     *  If the player is within the central rectangle, no camera movement will occur
     */
    [SerializeField] public float BoundWidth;
    [SerializeField] public float BoundHeight;

    /*
     *  The dimensions of the level (rectangle), in block units
     */
    [SerializeField] public float LevelWidth;
    [SerializeField] public float LevelHeight;
    private float cameraWidth;
    private float cameraHeight;

    /*
     *  The speed at which the parallax background moves, relative to the main camera (as a decimal from 0 to 1)
     */
    [SerializeField] [Range(0, 1)] public float PanningBack1Speed;
    [SerializeField] [Range(0, 1)] public float PanningBack2Speed;
    [SerializeField] public GameObject PanningBack1;
    [SerializeField] public GameObject PanningBack2;

    private Transform cameraTransform;
    public Transform playerTransform;

    void Start()
    {
        float cameraHalfSize = GetComponent<Camera>().orthographicSize;
        cameraHeight = cameraHalfSize * 2;
        cameraWidth = cameraHalfSize * 2 * GetComponent<Camera>().aspect;

        cameraTransform = GetComponent<Transform>();
    }

    void FixedUpdate()
    {
        /*
         * Adjust camera position to follow player
         */
        float targetCameraX = -1;
        float targetCameraY = -1;
        Vector3 cameraPos = cameraTransform.position;

        float deltaX = 0, deltaY = 0; // camera movement value, in blocks

        if (playerTransform.position.x < cameraTransform.position.x - cameraWidth * BoundWidth / 2) // camera too far right
            targetCameraX = playerTransform.position.x + cameraWidth * BoundWidth / 2;
        else if (playerTransform.position.x > cameraTransform.position.x + cameraWidth * BoundWidth / 2) // camera too far left
            targetCameraX = playerTransform.position.x - cameraWidth * BoundWidth / 2;

        if (playerTransform.position.y < cameraTransform.position.y - cameraHeight * BoundHeight / 2) // camera too high
            targetCameraY = playerTransform.position.y + cameraHeight * BoundHeight / 2;
        else if (playerTransform.position.y > cameraTransform.position.y + cameraWidth * BoundHeight / 2) // camera too low
            targetCameraY = playerTransform.position.y - cameraWidth * BoundHeight / 2;

        // ensure camera repositioning stays on screen
        if (targetCameraX != -1 && targetCameraX > cameraWidth / 2 && targetCameraX < LevelWidth - cameraWidth / 2)
        {
            deltaX = targetCameraX - cameraPos.x;
            cameraPos.x = targetCameraX;
        }
        if (targetCameraY != -1 && targetCameraY > cameraHeight / 2 && targetCameraY < LevelHeight - cameraHeight / 2)
        {
            deltaY = targetCameraY - cameraPos.y;
            cameraPos.y = targetCameraY;
        }

        PanningBack1.transform.position = PanningBack1.transform.position + new Vector3(deltaX * PanningBack1Speed, deltaY * PanningBack2Speed, 0);
        PanningBack2.transform.position = PanningBack2.transform.position + new Vector3(deltaX * PanningBack2Speed, deltaY * PanningBack2Speed, 0);

        cameraTransform.position = cameraPos;
    }
}
