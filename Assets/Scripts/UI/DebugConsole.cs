using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugConsole : MonoBehaviour
{
    public TextMeshProUGUI DebugText;

    [Header("Alert Box")]
    public GameObject AlertBoxPrefab;
    [Tooltip("(Optional) AlertBox will copy transform of the target (except Scaling)")]
    public Transform TargetAlertBoxTransform;
    public Camera EventCamera;
    public GameObject Pointer;

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

    public void Alert(string message)
    {
        if (AlertBoxPrefab != null)
        {
            GameObject obj;
            if (TargetAlertBoxTransform != null)
            {
                obj = Instantiate(AlertBoxPrefab, TargetAlertBoxTransform.position, TargetAlertBoxTransform.rotation);
            }
            else
            {
                obj = Instantiate(AlertBoxPrefab);
            }
            obj.GetComponent<AlertBoxController>().setMessage(message);
            obj.GetComponent<Canvas>().worldCamera = EventCamera;
            obj.GetComponent<OVRRaycaster>().pointer = Pointer;
        }
    }
}
