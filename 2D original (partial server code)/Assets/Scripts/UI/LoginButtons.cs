using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FinalAisle_Shared.Networking;

public class LoginButtons : MonoBehaviour
{
    public GameObject ConnectionObject;
    public LoginInputFields LoginInputFields;

    private Connection connection;
    private DataProcessor dataProcessor;

    // Relevant UI game objects
    public GameObject RegisterWindow;
    public GameObject LoginWindow;

    public Button RegisterButton; // switch to register screen
    public Button BackButton; // switch back to login screen

    public Button RegisterQueryButton; // actually registers account

    public Button VerifyEmailButton; // sends verification code for checking

    // Account registration input fields
    public InputField REmailInputField;
    public InputField RUsernameInputField;
    public InputField RPasswordInputField;

    // Login input fields
    public InputField LUsernameInputField;
    public InputField LPasswordInputField;

    // Email verify input fields
    public InputField VerifyEmailInputField;

    public Text RegisterWarningText;
    public Text VerifyEmailWarningText;

    // Transition between register screen and loading screen
    public GameObject RegisterUIWidgets;
    public GameObject VerifyEmailUIWidgets;
    public GameObject LoadingSpinner;

    void Start()
    {
        connection = ConnectionObject.GetComponent<Connection>();
        dataProcessor = ConnectionObject.GetComponent<DataProcessor>();
        dataProcessor.LoginButtons = this;
        LoginInputFields = this.gameObject.GetComponent<LoginInputFields>();

        BackToLoginScreen();

        RegisterButton.onClick.AddListener(OpenRegisterWindow);
        BackButton.onClick.AddListener(BackToLoginScreen);
        RegisterQueryButton.onClick.AddListener(TryCreateAccount);
        VerifyEmailButton.onClick.AddListener(SendEmailVerifyCode);

        LoadingSpinner.SetActive(false);
    }

    /**
     * Open the register new account window
     */
    void OpenRegisterWindow()
    {
        SetColour(REmailInputField, 1, 1, 1);
        SetColour(RUsernameInputField, 1, 1, 1);
        SetColour(RPasswordInputField, 1, 1, 1);
        RegisterWarningText.text = "";

        LUsernameInputField.text = "";
        LPasswordInputField.text = "";

        RegisterUIWidgets.SetActive(true);
        VerifyEmailUIWidgets.SetActive(false);

        RegisterWindow.SetActive(true);
        LoginWindow.SetActive(false);

        LoginInputFields.SetTabMode(LoginInputFields.RegisterMode);
    }

    /**
     * Close the register new account window
     */
    void BackToLoginScreen()
    {
        RegisterWindow.SetActive(false);
        LoginWindow.SetActive(true);

        REmailInputField.text = "";
        RUsernameInputField.text = "";
        RPasswordInputField.text = "";

        LoginInputFields.SetTabMode(LoginInputFields.LoginMode);
    }

    /**
     * Send new account creation request to server
     */
    void TryCreateAccount()
    {
        string emailText = REmailInputField.text;
        string usernameText = RUsernameInputField.text;
        string passwordText = RPasswordInputField.text;

        SetColour(REmailInputField, 1, 1, 1);
        SetColour(RUsernameInputField, 1, 1, 1);
        SetColour(RPasswordInputField, 1, 1, 1);

        if (String.IsNullOrEmpty(emailText))
        {
            SetColour(REmailInputField, 1.0f, 0.75f, 0.75f);
            return;
        }

        if (String.IsNullOrEmpty(usernameText))
        {
            SetColour(RUsernameInputField, 1.0f, 0.75f, 0.75f);
            return;
        }

        if (String.IsNullOrEmpty(passwordText) || passwordText.Length < 8)
        {
            SetColour(RPasswordInputField, 1.0f, 0.75f, 0.75f);
            RegisterWarningText.text = "Password too short!";
            return;
        }

        RegisterUIWidgets.SetActive(false);
        LoadingSpinner.SetActive(true);
        RegisterWarningText.text = "";
        LoginInputFields.DeselectAll();

        string sendString = PacketDataUtils.Condense(PacketDataUtils.TryNewAccount, emailText + " " + usernameText + " " + passwordText);
        connection.SendData(sendString);
    }

    void SendEmailVerifyCode()
    {
        SetColour(VerifyEmailInputField, 1, 1, 1);
        VerifyEmailWarningText.text = "";

        string emailCode = VerifyEmailInputField.text;
        if (String.IsNullOrEmpty(emailCode))
        {
            SetColour(VerifyEmailInputField, 1.0f, 0.75f, 0.75f);
            return;
        }

        VerifyEmailUIWidgets.SetActive(false);
        LoadingSpinner.SetActive(true);
        VerifyEmailWarningText.text = "";
        LoginInputFields.DeselectAll();

        string sendString = PacketDataUtils.Condense(PacketDataUtils.TryVerifyEmail, emailCode);
        connection.SendData(sendString);
    }

    public void SetColour(InputField field, float r, float g, float b)
    {
        ColorBlock cb = field.colors;
        cb.normalColor = new Color(r, g, b);
        field.colors = cb;
    }
}
