using UnityEngine;
using Final_Aisle_Shared.Network;
using UnityEngine.SceneManagement;
using Final_Aisle_Shared.Game.Player;

using static Final_Aisle_Shared.Network.PacketDataUtils;

/**
 * Handles how to respond to server messages
 * 
 * Selection statements are in order of frequency
 */
public class PacketProcessor : MonoBehaviour
{
    public void ParseInput(PacketReceivedEventArgs args)
    {
        var packet = args.Packet;
        var composite = packet.GetComposite(); // may be null, use only when you know the packet is of type "composite"
        var key = packet.GetKey(); // key, existence is universal across all packet types

        if (Connection.LoggedIn)
        { // already logged in
            if (key.Equals(TransformPosition)) Connection.instance.PlayerHandler.TransformPosition(composite.GetInt(0), composite.GetFloat(1), composite.GetFloat(2), composite.GetFloat(3));
            else if (key.Equals(TransformRotation)) Connection.instance.PlayerHandler.TransformRotation(composite.GetInt(0), composite.GetFloat(1));
            else if (key.Equals(MovementInput)) Connection.instance.PlayerHandler.MovementInput(composite.GetInt(0), composite.GetFloat(1), composite.GetFloat(2));
            else if (key.Equals(MovementRoll)) Connection.instance.PlayerHandler.MovementRoll(composite.GetInt(0), composite.GetFloat(1));
            else if (key.Equals(MovementJump)) Connection.instance.PlayerHandler.MovementJump(packet.GetInt());
            else if (key.Equals(MovementToggleProne)) Connection.instance.PlayerHandler.MovementToggleProne(composite.GetInt(0), composite.GetBool(1));
            else if (key.Equals(PlayerConnected)) Connection.instance.PlayerHandler.PlayerConnected(composite.GetInt(0), (PlayerCharacter)((IPacketSerializable)new PlayerCharacter()).Deserialize(composite, 1));
            else if (key.Equals(PlayerDisconnected)) Connection.instance.PlayerHandler.PlayerDisconnected(packet.GetInt());
        }
        else if (key.Equals(LoginSuccess) || key.Equals(EmailVerifySuccess))
        { // either ways of logging in, new account or returning
            UnityThread.ExecuteInUpdate(() =>
            { // login successful, move to new realm
                Connection.LoggedIn = true;
                SceneManager.LoadScene("NewRealm");
                Connection.instance.SendEmpty(PlayerPostConnect); // load in other players
            }, 500);
        }
        else if (key.Equals(LoginFail))
        {
            UnityThread.ExecuteInUpdate(() =>
            {
                LoginButtons.instance.LoginWindow.SetActive(true);
                LoginButtons.instance.LoadingSpinner.SetActive(false);
                LoginButtons.instance.LoginWarningText.text = "Invalid credentials";
            }, 500);
        }
        else if (key.Equals(EmailAlreadyTaken))
        {
            UnityThread.ExecuteInUpdate(() =>
            {
                LoginButtons.instance.RegisterUIWidgets.SetActive(true);
                LoginButtons.instance.LoadingSpinner.SetActive(false);
                LoginButtons.instance.RegisterWarningText.text = "Email already taken!";
                LoginButtons.instance.SetColour(LoginButtons.instance.REmailInputField, 1.0f, 0.75f, 0.75f);
            });
        }
        else if (key.Equals(UsernameAlreadyTaken))
        {
            UnityThread.ExecuteInUpdate(() =>
            {
                LoginButtons.instance.RegisterUIWidgets.SetActive(true);
                LoginButtons.instance.LoadingSpinner.SetActive(false);
                LoginButtons.instance.RegisterWarningText.text = "Username already taken!";
                LoginButtons.instance.SetColour(LoginButtons.instance.RUsernameInputField, 1.0f, 0.75f, 0.75f);
            });
        }
        else if (key.Equals(EmailVerifySent))
        {
            UnityThread.ExecuteInUpdate(() =>
            {
                LoginButtons.instance.LoginInputFields.SetTabMode(LoginButtons.instance.LoginInputFields.VerifyEmailMode);
                LoginButtons.instance.RegisterUIWidgets.SetActive(false);
                LoginButtons.instance.LoadingSpinner.SetActive(false);
                LoginButtons.instance.VerifyEmailInputField.text = "";
                LoginButtons.instance.VerifyEmailUIWidgets.SetActive(true);
                LoginButtons.instance.LoginInputFields.SetTabMode(LoginButtons.instance.LoginInputFields.VerifyEmailMode);
            });
        }
        else if (key.Equals(EmailVerifyFail))
        {
            UnityThread.ExecuteInUpdate(() =>
            {
                if (packet.GetInt() <= 0) // no tries left
                {
                    LoginButtons.instance.REmailInputField.text = "";
                    LoginButtons.instance.RUsernameInputField.text = "";
                    LoginButtons.instance.RPasswordInputField.text = "";
                    LoginButtons.instance.RegisterUIWidgets.SetActive(true);
                    LoginButtons.instance.LoadingSpinner.SetActive(false);
                    LoginButtons.instance.LoginInputFields.SetTabMode(LoginButtons.instance.LoginInputFields.RegisterMode);
                }
                else
                {
                    LoginButtons.instance.VerifyEmailUIWidgets.SetActive(true);
                    LoginButtons.instance.LoadingSpinner.SetActive(false);
                    LoginButtons.instance.VerifyEmailWarningText.text = "Wrong code! (" + packet.GetInt().ToString() + " tries left)";
                    LoginButtons.instance.SetColour(LoginButtons.instance.VerifyEmailInputField, 1.0f, 0.75f, 0.75f);
                }
            });
        }
    }
}
