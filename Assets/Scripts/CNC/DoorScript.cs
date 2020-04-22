using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    public GameObject DoorLeft;
    public GameObject DoorRight;
    public float DoorSpeed = 1f;

    [Header("Close/Open Position (Relative to the machine)")]
    public Vector3 LeftClosed;
    public Vector3 LeftOpened;
    public Vector3 RightClosed;
    public Vector3 RightOpened;

    public bool isDoorsClosed { get; private set; }

    private bool isOpened;
    private bool isMoving;
    private float maxDist;

    // Start is called before the first frame update
    void Start()
    {
        isOpened = false;
        isMoving = false;
        DoorLeft.transform.localPosition = LeftClosed;
        DoorRight.transform.localPosition = RightClosed;
        isDoorsClosed = true;
        maxDist = DoorSpeed * Time.deltaTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            if (isOpened)       // Closing Transition
            {
                DoorLeft.transform.localPosition = Vector3.MoveTowards(DoorLeft.transform.localPosition, LeftClosed, maxDist);
                DoorRight.transform.localPosition = Vector3.MoveTowards(DoorRight.transform.localPosition, RightClosed, maxDist);

                if (DoorLeft.transform.localPosition == LeftClosed && DoorRight.transform.localPosition == RightClosed)
                {
                    DoorLeft.transform.localPosition = LeftClosed;
                    DoorRight.transform.localPosition = RightClosed;
                    isOpened = false;
                    isMoving = false;
                }
            }
            else                // Opening Transition
            {
                DoorLeft.transform.localPosition = Vector3.MoveTowards(DoorLeft.transform.localPosition, LeftOpened, maxDist);
                DoorRight.transform.localPosition = Vector3.MoveTowards(DoorRight.transform.localPosition, RightOpened, maxDist);

                if (DoorLeft.transform.localPosition == LeftOpened && DoorRight.transform.localPosition == RightOpened)
                {
                    DoorLeft.transform.localPosition = LeftOpened;
                    DoorRight.transform.localPosition = RightOpened;
                    isOpened = true;
                    isMoving = false;
                }
            }
        } else if (DoorLeft.transform.localPosition == LeftClosed && DoorRight.transform.localPosition == RightClosed)
        {
            isDoorsClosed = true;
        } else
        {
            isDoorsClosed = false;
        }
    }

    public void Action()
    {
        isMoving = true;
    }
}
