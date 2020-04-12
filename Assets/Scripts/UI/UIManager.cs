using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_InputField InputObj;
    public CNCTranslator CNCT;
    public DebugConsole DebugVR;
    private int numLine;
    [HideInInspector] public string inputString;

    private void Awake()
    {
        numLine = 1;
    }

    void Start()
    {
        
    }

    public void InputSubmit()
    {
        if (DebugVR != null)
            DebugVR.Println("Input Submitted");
        CNCT.TranslateCommand(InputObj.text);
        
    }
    public void ClearInputField()
    {
        InputObj.text = "N01";
        numLine = 1;
    }
    public void Key2InputField(string k)
    {
        if (char.IsDigit(k[0]) || k[0] == '.')
        {
            InputObj.text += k;
        }
        else
        {
            InputObj.text += " " + k;
        }
    }
    public void Enter2InputField()
    {
        numLine++;
        InputObj.text += "\nN" + numLine.ToString("d2");
    }
    public void Backspace2InputField()
    {
        InputObj.text = InputObj.text.Remove(InputObj.text.Length - 1);
    }
}
