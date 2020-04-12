using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float Movespeed = 1f;
    public float Rotatespeed = 10f;
    private float dist;
    private float angle;

    // Update is called once per frame
    public void LateUpdate()
    {
        dist = Time.deltaTime * Movespeed;
        angle = Time.deltaTime * Rotatespeed;
        transform.Translate(new Vector3(Input.GetAxis("Horizontal") * dist, Input.GetAxis("Vertical") * dist, Input.GetAxis("Horizontal2") * dist));
        transform.Rotate(new Vector3(Input.GetAxis("Rotation1") * angle, Input.GetAxis("Rotation2") * angle, 0f));
    }
}
