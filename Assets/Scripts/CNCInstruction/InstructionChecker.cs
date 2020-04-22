using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstructionChecker : MonoBehaviour
{
    [HideInInspector]
    public LinkedList<CNCInstruction> instructionList;
    public LinkedList<CNCInstruction> instructionOut;
    public DebugConsole DebugVR;

    private float currentX, currentY, currentZ;
    private float currentFeedSpeed;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
