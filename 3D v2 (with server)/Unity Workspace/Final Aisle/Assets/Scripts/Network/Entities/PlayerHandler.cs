using Final_Aisle_Shared.Game.Player;
using Final_Aisle_Shared.Network;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/**
 * Class used for relaying information about player movements/actions to relevant clients
 */
public class PlayerHandler : MonoBehaviour
{
    public Dictionary<int, GameObject> OtherPlayers = new Dictionary<int, GameObject>();
    public Dictionary<int, PlayerCharacter> OtherPlayerCharacters = new Dictionary<int, PlayerCharacter>();
    public Dictionary<int, NetworkPlayerController> OtherPlayerControllers = new Dictionary<int, NetworkPlayerController>();

    /**
     * Call when a new foreign player has connected with an active character
     */
    public void PlayerConnected(int userID, PlayerCharacter character)
    {
        Thread thread = new Thread(() =>
        {
            UnityThread.ExecuteInUpdate(() =>
            {
                GameObject newTurtle = Instantiate(Connection.instance.PrefabLibrary.NetworkTurtle, new Vector3(0, 0, 0), Quaternion.identity);
                OtherPlayers.Add(userID, newTurtle);
                OtherPlayerCharacters.Add(userID, (PlayerCharacter)((INetworkSerializable)new PlayerCharacter()).Deserialize(characterString));
                OtherPlayerControllers.Add(userID, newTurtle.GetComponent<NetworkPlayerController>());
            });
        });
        thread.Start();
    }

    /**
     * A foreign player has disconnected
     */
    public void PlayerDisconnected(int userID)
    {
        Thread thread = new Thread(() =>
        {
            UnityThread.ExecuteInUpdate(() =>
            {
                if (OtherPlayers.ContainsKey(userID))
                {
                    Destroy(OtherPlayers[userID]);
                    OtherPlayers.Remove(userID);
                    OtherPlayerControllers.Remove(userID);
                }
            });
        });
        thread.Start();
    }

    public void MovementInput(int userID, float moveX, float moveY)
    {
        if (OtherPlayerControllers.ContainsKey(userID)) OtherPlayerControllers[userID].UpdateMovementInput(moveX, moveY);
    }

    public void MovementRoll(int userID, float rotation)
    {
        if (OtherPlayerControllers.ContainsKey(userID)) OtherPlayerControllers[userID].QueueMovementRoll(rotation);
    }

    public void MovementJump(int userID)
    {
        if (OtherPlayerControllers.ContainsKey(userID)) OtherPlayerControllers[userID].QueueMovementJump();
    }

    public void MovementToggleProne(int userID, bool proneState)
    {
        if (OtherPlayerControllers.ContainsKey(userID)) OtherPlayerControllers[userID].QueueMovementToggleProne(proneState);
    }

    public void TransformPosition(int userID, float posX, float posY, float posZ)
    {
        if (OtherPlayerControllers.ContainsKey(userID)) OtherPlayerControllers[userID].UpdatePosition(new Vector3(posX, posY, posZ));
    }

    public void TransformRotation(int userID, float rotation)
    {
        if (OtherPlayerControllers.ContainsKey(userID)) OtherPlayerControllers[userID].UpdateRotation(rotation);
    }
}
