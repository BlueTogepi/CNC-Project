using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestPrintUI : MonoBehaviour
{
    public TextMeshPro target;

    public void PrintText(string text)
    {
        target.text = text;
    }
}