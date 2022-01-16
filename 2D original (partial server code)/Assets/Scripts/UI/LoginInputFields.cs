using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LoginInputFields : MonoBehaviour
{
    private LoginButtons loginButtons;
    private int tabMode = 0;

    public readonly int LoginMode = 0;
    public readonly int RegisterMode = 1;
    public readonly int VerifyEmailMode = 2;

    private List<InputField> loginFields;
    private List<InputField> registerFields;
    private List<InputField> verifyEmailFields;

    void Start()
    {
        loginButtons = this.gameObject.GetComponent<LoginButtons>();

        loginFields = new List<InputField> { loginButtons.LUsernameInputField, loginButtons.LPasswordInputField };
        registerFields = new List<InputField> { loginButtons.REmailInputField, loginButtons.RUsernameInputField, loginButtons.RPasswordInputField };
        verifyEmailFields = new List<InputField> { };
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            int fieldIndex = -1;

            if (EventSystem.current.currentSelectedGameObject != null)
            {
                if (tabMode == RegisterMode)
                {
                    for (int i = 0; i < registerFields.Count; i++) if (registerFields[i] == EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
                        {
                            fieldIndex = i;
                            break;
                        }
                }
                else if (tabMode == LoginMode)
                {
                    for (int i = 0; i < loginFields.Count; i++) if (loginFields[i] == EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
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
            if (tabMode == RegisterMode) foreach (InputField field in registerFields) field.OnDeselect(new BaseEventData(EventSystem.current));
            else if (tabMode == LoginMode) foreach (InputField field in loginFields) field.OnDeselect(new BaseEventData(EventSystem.current));
            else if (tabMode == VerifyEmailMode) foreach (InputField field in verifyEmailFields) field.OnDeselect(new BaseEventData(EventSystem.current));
        }
        catch (Exception) { }
    }
}
