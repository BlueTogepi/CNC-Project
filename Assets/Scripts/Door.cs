using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject DoorLeft;
    public GameObject DoorRight;
    public float DoorSpeed = 1f;

    public Vector3 LeftClosed;
    public Vector3 LeftOpened;
    public Vector3 RightClosed;
    public Vector3 RightOpened;

    private bool isOpened;
    private bool isMoving;
    private float maxDist;

    // Start is called before the first frame update
    void Start()
    {
        isOpened = false;
        isMoving = false;
        DoorLeft.transform.position = LeftClosed;
        DoorRight.transform.position = RightClosed;
        maxDist = DoorSpeed * Time.deltaTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            if (isOpened)       // Closing Transition
            {
                DoorLeft.transform.position = Vector3.MoveTowards(LeftOpened, LeftClosed, maxDist);
                DoorRight.transform.position = Vector3.MoveTowards(RightOpened, RightClosed, maxDist);

                if (DoorLeft.transform.position == LeftClosed && DoorRight.transform.position == RightClosed)
                {
                    DoorLeft.transform.position = LeftClosed;
                    DoorRight.transform.position = RightClosed;
                    isOpened = false;
                    isMoving = false;
                }
            }
            else                // Opening Transition
            {
                DoorLeft.transform.position = Vector3.MoveTowards(LeftClosed, LeftOpened, maxDist);
                DoorRight.transform.position = Vector3.MoveTowards(RightClosed, RightOpened, maxDist);

                if (DoorLeft.transform.position == LeftOpened && DoorRight.transform.position == RightOpened)
                {
                    DoorLeft.transform.position = LeftOpened;
                    DoorRight.transform.position = RightOpened;
                    isOpened = true;
                    isMoving = false;
                }
            }
        }
    }

    public void Action()
    {
        isMoving = true;
    }
}
