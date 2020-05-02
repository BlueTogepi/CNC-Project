using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AlertBoxController : MonoBehaviour
{
    public TextMeshProUGUI text;

    public void setMessage(string message)
    {
        text.text = "Warning: " + message;
    }

    public void close()
    {
        Destroy(gameObject);
    }
}
