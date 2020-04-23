using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightBulbScript : MonoBehaviour
{
    public Color ColorOn = new Color(255, 0, 0);
    public Color ColorOff = new Color(10, 8, 8);
    [HideInInspector]
    public bool isOn = false;

    private Material BulbMaterial;

    void Awake()
    {
        BulbMaterial = gameObject.GetComponent<Renderer>().material;
    }

    public void TurnOn()
    {
        if (!isOn)
        {
            isOn = true;
            BulbMaterial.SetColor("_Color", ColorOn);
        }
    }

    public void TurnOff()
    {
        if (isOn)
        {
            isOn = false;
            BulbMaterial.SetColor("_Color", ColorOff);
        }
    }

    public void ToggleOnOff()
    {
        if (isOn)
        {
            TurnOff();
        }
        else
        {
            TurnOn();
        }
    }
}
