using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugConsole : MonoBehaviour
{
    public TextMeshProUGUI DebugText;

    public void Println(string str)
    {
        DebugText.text += str + "\n";
    }
    public void Print(string str)
    {
        DebugText.text += str;
    }
    public void ClearDebug()
    {
        DebugText.text = "Debug Messages Here.\n";
    }
}
