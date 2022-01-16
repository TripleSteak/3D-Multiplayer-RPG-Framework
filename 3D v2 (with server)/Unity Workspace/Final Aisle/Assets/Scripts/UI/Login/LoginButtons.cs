using System;
using UnityEngine;
using UnityEngine.UI;
using Final_Aisle_Shared.Network;
using TMPro;
using System.Net.Mail;
using System.Collections.Generic;

/**
 * Contains all UI widgets necessary to the login process
 */
public class LoginButtons : MonoBehaviour
{
    public static LoginButtons instance;

    public GameObject ConnectionObject;
    [HideInInspector]
    public LoginInputFields LoginInputFields;

    // Relevant UI game objects
    public GameObject RegisterWindow;
    public GameObject LoginWindow;

    public Button GoToRegisterButton; // switch to register screen
    public Button BackButton; // switch back to login screen

    public Button LoginButton; // login button

    public Button RegisterQueryButton; // actually registers account

    public Button VerifyEmailButton; // sends verification code for checking

    // Account registration input fields
    public TMP_InputField REmailInputField;
    public TMP_InputField RUsernameInputField;
    public TMP_InputField RPasswordInputField;

    // Login input fields
    public TMP_InputField LIdentifierInputField;
    public TMP_InputField LPasswordInputField;

    // Email verify input fields
    public TMP_InputField VerifyEmailInputField;

    public TMP_Text LoginWarningText;
    public TMP_Text RegisterWarningText;
    public TMP_Text VerifyEmailWarningText;

    // Transition between register screen and loading screen
    public GameObject RegisterUIWidgets;
    public GameObject VerifyEmailUIWidgets;
    public GameObject LoadingSpinner;

    void Start()
    {
        instance = this; // for static access

        LoginInputFields = this.gameObject.GetComponent<LoginInputFields>();

        BackToLoginScreen();

        GoToRegisterButton.onClick.AddListener(OpenRegisterWindow);
        BackButton.onClick.AddListener(BackToLoginScreen);
        RegisterQueryButton.onClick.AddListener(TryCreateAccount);
        VerifyEmailButton.onClick.AddListener(SendEmailVerifyCode);
        LoginButton.onClick.AddListener(TryLogin);

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

        LIdentifierInputField.text = "";
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

        if (String.IsNullOrEmpty(emailText) || !ValidateEmail(emailText))
        {
            SetColour(REmailInputField, 1.0f, 0.75f, 0.75f);
            RegisterWarningText.text = "Invalid email";
            return;
        }

        if (String.IsNullOrEmpty(usernameText))
        {
            SetColour(RUsernameInputField, 1.0f, 0.75f, 0.75f);
            RegisterWarningText.text = "Invalid username";
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

        string sendString = PacketDataUtils.Condense(PacketDataUtils.TryNewAccount, PacketDataUtils.AbridgeStrings(new string[] { emailText, usernameText, passwordText }));
        Connection.instance.SendComposite(PacketDataUtils.TryNewAccount, new List<object> { emailText, usernameText, passwordText });
    }

    /**
     * Sends the user's inputted code to the server to check for matching code
     */
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
        Connection.instance.SendData(sendString);
    }

    /**
     * Sends user inputted login credentials to the server
     */
    void TryLogin()
    {
        string identifierText = LIdentifierInputField.text; // this can be either an email or a username
        string passwordText = LPasswordInputField.text;

        SetColour(LIdentifierInputField, 1, 1, 1);
        SetColour(LPasswordInputField, 1, 1, 1);

        if (String.IsNullOrEmpty(identifierText))
        {
            SetColour(LIdentifierInputField, 1.0f, 0.75f, 0.75f);
            LoginWarningText.text = "Invalid email/username!";
            return;
        }

        if (String.IsNullOrEmpty(passwordText) || passwordText.Length < 8)
        {
            SetColour(LPasswordInputField, 1.0f, 0.75f, 0.75f);
            LoginWarningText.text = "Password too short!";
            return;
        }

        LoginWindow.SetActive(false);
        LoadingSpinner.SetActive(true);
        LoginWarningText.text = "";
        LoginInputFields.DeselectAll();

        string sendString = PacketDataUtils.Condense(PacketDataUtils.TryLogin, PacketDataUtils.AbridgeStrings(new string[] { identifierText, passwordText }));
        Connection.instance.SendData(sendString);
    }

    /**
     * Sets the colour of an input field (e.g. to red when the input is wrong)
     */
    public void SetColour(TMP_InputField field, float r, float g, float b)
    {
        ColorBlock cb = field.colors;
        cb.normalColor = new Color(r, g, b);
        field.colors = cb;
    }

    /**
     * Determines if the email is of an appropriate format
     */
    private bool ValidateEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
