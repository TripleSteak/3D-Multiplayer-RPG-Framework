using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/**
 * Increases smoothness of the login process by allowing the tab button to transfer focus to the next text field
 */
public class LoginInputFields : MonoBehaviour
{
    private LoginButtons loginButtons;
    private int tabMode = 0;

    public readonly int LoginMode = 0;
    public readonly int RegisterMode = 1;
    public readonly int VerifyEmailMode = 2;

    private List<TMP_InputField> loginFields;
    private List<TMP_InputField> registerFields;
    private List<TMP_InputField> verifyEmailFields;

    private EventSystem eventSystem;

    void Start()
    {
        loginButtons = this.gameObject.GetComponent<LoginButtons>();

        loginFields = new List<TMP_InputField> { loginButtons.LIdentifierInputField, loginButtons.LPasswordInputField };
        registerFields = new List<TMP_InputField> { loginButtons.REmailInputField, loginButtons.RUsernameInputField, loginButtons.RPasswordInputField };
        verifyEmailFields = new List<TMP_InputField> { };

        eventSystem = EventSystem.current;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            int fieldIndex = -1;

            if (eventSystem.currentSelectedGameObject != null)
            {
                if (tabMode == RegisterMode)
                {
                    for (int i = 0; i < registerFields.Count; i++) if (registerFields[i] == eventSystem.currentSelectedGameObject.GetComponent<TMP_InputField>())
                        {
                            fieldIndex = i;
                            break;
                        }
                }
                else if (tabMode == LoginMode)
                {
                    for (int i = 0; i < loginFields.Count; i++) if (loginFields[i] == eventSystem.currentSelectedGameObject.GetComponent<TMP_InputField>())
                        {
                            fieldIndex = i;
                            break;
                        }
                }
            }

            fieldIndex++;
            if (tabMode == RegisterMode && fieldIndex >= registerFields.Count) fieldIndex = 0;
            else if (tabMode == LoginMode && fieldIndex >= loginFields.Count) fieldIndex = 0;

            if (tabMode == RegisterMode) registerFields[fieldIndex].Select();
            else if (tabMode == LoginMode) loginFields[fieldIndex].Select();
            else if (tabMode == VerifyEmailMode) verifyEmailFields[fieldIndex].Select();
        }
    }

    public void SetTabMode(int newMode)
    {
        DeselectAll();

        tabMode = newMode;
    }

    public void DeselectAll()
    {
        try
        {
            if (tabMode == RegisterMode) foreach (TMP_InputField field in registerFields) field.OnDeselect(new BaseEventData(eventSystem));
            else if (tabMode == LoginMode) foreach (TMP_InputField field in loginFields) field.OnDeselect(new BaseEventData(eventSystem));
            else if (tabMode == VerifyEmailMode) foreach (TMP_InputField field in verifyEmailFields) field.OnDeselect(new BaseEventData(eventSystem));
        }
        catch (Exception) { }
    }
}
