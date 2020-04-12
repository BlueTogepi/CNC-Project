using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowGizmosPoint : MonoBehaviour
{
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;
    public float radius = 0.005f;

    private Color color;

    // Start is called before the first frame update
    void Awake()
    {
        color = inactiveColor;
    }

    private void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, radius);
    }

    public void SetActiveColor(bool isActive)
    {
        color = isActive ? activeColor : inactiveColor;
    }
}
