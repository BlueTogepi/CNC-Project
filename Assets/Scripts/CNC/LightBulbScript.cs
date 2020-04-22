using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightBulbScript : MonoBehaviour
{
    [HideInInspector]
    public bool isOn = false;

    private Material BulbMaterial;

    void Awake()
    {
        BulbMaterial = gameObject.GetComponent<Renderer>().material;
    }

    public void TurnOn()
    {
        isOn = true;
        BulbMaterial.EnableKeyword("_EMISSION");
    }

    public void TurnOff()
    {
        isOn = false;
        BulbMaterial.DisableKeyword("_EMISSION");
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
