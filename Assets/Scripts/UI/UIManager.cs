using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_InputField InputObj;
    public CNCTranslator CNCT;
    public CNCController CNCC;
    public DoorScript Doors;
    public DebugConsole DebugVR;

    public TextAsset[] Presets;

    private int numLine;
    [HideInInspector] public string inputString;

    private void Awake()
    {
        numLine = 1;
    }

    public void InputSubmit()
    {
        if (DebugVR != null)
            DebugVR.Println("Input Submitted");
        CNCT.TranslateCommand(InputObj.text);
    }
    public void ClearInputField()
    {
        InputObj.text = "";
        numLine = 1;
    }
    public void Key2InputField(string k)
    {
        if (char.IsDigit(k[0]) || k[0] == '.' || k[0] == '-')
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
        InputObj.text += "\n";
    }
    public void Backspace2InputField()
    {
        InputObj.text = InputObj.text.Remove(InputObj.text.Length - 1);
    }

    /*public void FinishPiece()
    {
        CNCC.PieceFinished();
    }*/

    public void ClearDebugConsole()
    {
        DebugVR.ClearDebug();
    }

    public void HaultCommands()
    {
        CNCC.ClearInstrQueue();
    }

    public void Renew()
    {
        CNCC.RePiece();
    }

    public void SetHome()
    {
        CNCC.SetHome();
    }

    public void GoHome()
    {
        CNCC.GetBackHome();
    }

    public void SetPreset(int i)
    {
        InputObj.text = Presets[i - 1].text;
        string temp = string.Format("Preset {0} loaded.", i);
        print(temp);
        if (DebugVR != null)
            DebugVR.Println(temp);
    }

    public void DoorAction()
    {
        Doors.Action();
    }
}
