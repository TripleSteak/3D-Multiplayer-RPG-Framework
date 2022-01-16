using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using FinalAisle_Shared.Networking;
using FinalAisle_Shared.Networking.Packet;
using System.IO;
using System.Runtime.Versioning;
using System;

public class DataProcessor : MonoBehaviour
{
    public LoginButtons LoginButtons; // login screen scene

    public void ParseInput(PacketReceivedEventArgs args)
    {
        if (args.Packet.Data is MessagePacketData)
        {
            string prefix = PacketDataUtils.GetPrefix(((MessagePacketData)args.Packet.Data).Message);
            string details = PacketDataUtils.GetStringData(((MessagePacketData)args.Packet.Data).Message);

            if (prefix.Equals(PacketDataUtils.EmailAlreadyTaken))
            {
                Thread returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        LoginButtons.RegisterUIWidgets.SetActive(true);
                        LoginButtons.LoadingSpinner.SetActive(false);
                        LoginButtons.RegisterWarningText.text = "Email already taken!";
                        LoginButtons.SetColour(LoginButtons.REmailInputField, 1.0f, 0.75f, 0.75f);
                    });
                });
                returnThread.Start();
            }
            else if (prefix.Equals(PacketDataUtils.UsernameAlreadyTaken))
            {
                Thread returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        LoginButtons.RegisterUIWidgets.SetActive(true);
                        LoginButtons.LoadingSpinner.SetActive(false);
                        LoginButtons.RegisterWarningText.text = "Username already taken!";
                        LoginButtons.SetColour(LoginButtons.RUsernameInputField, 1.0f, 0.75f, 0.75f);
                    });
                });
                returnThread.Start();
            }
            else if (prefix.Equals(PacketDataUtils.EmailVerifySent))
            {
                Thread returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        LoginButtons.LoginInputFields.SetTabMode(LoginButtons.LoginInputFields.VerifyEmailMode);
                        LoginButtons.RegisterUIWidgets.SetActive(false);
                        LoginButtons.LoadingSpinner.SetActive(false);
                        LoginButtons.VerifyEmailInputField.text = "";
                        LoginButtons.VerifyEmailUIWidgets.SetActive(true);
                        LoginButtons.LoginInputFields.SetTabMode(LoginButtons.LoginInputFields.VerifyEmailMode);
                    });
                });
                returnThread.Start();
            }
            else if (prefix.Equals(PacketDataUtils.EmailVerifySuccess))
            {
                Thread returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        // Process successful
                    });
                });
                returnThread.Start();
            }
            else if (prefix.Equals(PacketDataUtils.EmailVerifyFail))
            {
                Thread returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        if (int.Parse(details) <= 0) // no tries left
                        {
                            LoginButtons.REmailInputField.text = "";
                            LoginButtons.RUsernameInputField.text = "";
                            LoginButtons.RPasswordInputField.text = "";
                            LoginButtons.RegisterUIWidgets.SetActive(true);
                            LoginButtons.LoadingSpinner.SetActive(false);
                            LoginButtons.LoginInputFields.SetTabMode(LoginButtons.LoginInputFields.RegisterMode);
                        }
                        else
                        {
                            LoginButtons.VerifyEmailUIWidgets.SetActive(true);
                            LoginButtons.LoadingSpinner.SetActive(false);
                            LoginButtons.VerifyEmailWarningText.text = "Wrong code! (" + details + " tries left)";
                            LoginButtons.SetColour(LoginButtons.VerifyEmailInputField, 1.0f, 0.75f, 0.75f);
                        }
                    });
                });
                returnThread.Start();
            }
            /**
            
            if (prefix.Equals(PacketDataUtils.JoinLevel)) // a player has joined a new level
            {
                UnityThread.executeInUpdate(() =>
                {
                    //otherPlayer = Instantiate(Library.NetworkRabbitPlayer, new Vector3(31, 8, 0), Quaternion.identity) as GameObject;
                    //otherPlayer.GetComponent<NetworkController>().Initialize();
                });
            }
            else if (prefix.Equals(PacketDataUtils.MovementInput))
            {
                UnityThread.executeInUpdate(() =>
                {
                    //Vector2 movement = new Vector2(float.Parse(details.Substring(0, details.IndexOf('|'))), float.Parse(details.Substring(details.IndexOf('|') + 1)));
                    //otherPlayer.GetComponent<NetworkController>().RunOnUpdate(movement);
                    //otherPlayer.GetComponent<NetworkController>().RunOnFixedUpdate();
                });
            }
            else if (prefix.Equals(PacketDataUtils.MovementJump))
            {
                UnityThread.executeInUpdate(() =>
                {
                    //otherPlayer.GetComponent<NetworkController>().jump = true;
                });
            }
            else if (prefix.Equals(PacketDataUtils.MovementRoll))
            {
                UnityThread.executeInUpdate(() =>
                {
                    //otherPlayer.GetComponent<NetworkController>().roll = true;
                });
            }*/
        }
    }
}
