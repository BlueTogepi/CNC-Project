using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CNCController : MonoBehaviour
{
    public GameObject TargetKnife;                          // All knife transformation control in this script should not be relative to the piece (only CNC instructions are relative)
    public GameObject Piece;
    public GameObject PieceOrigin;
    public DebugConsole DebugVR;
    public float RapidMoveSpeed = 0.1f;
    public float FeedMoveSpeed = 0.01f;
    [Tooltip("Crude Threshold for Vector3 Equal Position Checking")]
    public float CrudeThreshold = 1e-4f;
    [Tooltip("Exact Threshold for Vector3 Equal Position Checking")]
    public float ExactThreshold = 1e-5f;
    [Tooltip("Allow spiral motion for G02 and G03")]
    public bool AllowSpiral = false;

    [HideInInspector]
    public Queue<CNCInstruction> InstructionQueue;

    protected int gCode;                                      // Current g-code
    protected bool isStartingTask;                            // True for the first frame entering a g-code task
    protected bool isFinishedTask;                            // True for the last frame finishing a g-code task
    protected Transform pieceTransform;
    protected Transform pieceOriginTransform;

    // Modal Group 1
    protected float maxDistanceRapid;
    protected float maxDistanceFeed;
    protected Vector3 scaleVector;
    protected Vector3 target;
    protected Vector3 pivot;
    protected Vector3 axis;
    protected float angleDiff;                                // Not the shortest in-between angle, but the total angle in degrees to rotate for G02, G03; [0, 360]
    protected float radiusDiff;

    protected virtual void Awake()
    {
        InstructionQueue = new Queue<CNCInstruction>();
    }
    
    protected virtual void Start()
    {
        isStartingTask = true;
        isFinishedTask = false;
        pieceTransform = Piece.transform;
        pieceOriginTransform = PieceOrigin.transform;

        maxDistanceRapid = RapidMoveSpeed * Time.deltaTime;
        maxDistanceFeed = FeedMoveSpeed * Time.deltaTime;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (InstructionQueue.Count != 0)
        {
            CNCInstruction instr = InstructionQueue.Peek();
            if (isStartingTask)
                PrintInstruction();

            ReadInstruction(instr);

            isStartingTask = false;
            if (isFinishedTask)
            {
                InstructionQueue.Dequeue();
                isFinishedTask = false;
                isStartingTask = true;
            }
        }
    }

    #region Main

    protected void ReadInstruction(CNCInstruction instr)
    {
        gCode = instr.G;
        switch (instr.Group)
        {
            case 1:
                ReadGroup1(instr);
                break;
            default:
                PrintError("Unimplemented G-Code found, cannot read instruction. Proceed to skip.\n");
                isFinishedTask = true;
                break;
        }
    }

    protected void ReadGroup1(CNCInstruction baseInstr)
    {
        if (isStartingTask)
        {
            CNCInstructionMotion instr = (CNCInstructionMotion)baseInstr;
            scaleVector = Vector3.one * instr.prefixModifier;
            target = pieceOriginTransform.TransformPoint(Vector3.Scale(instr.TargetPos, scaleVector));

            if (gCode == 2 || gCode == 3)
            {
                InitializeG02_03(instr);

                // Spiral Checking
                float currentRadius = (TargetKnife.transform.position - pivot).magnitude;
                float targetRadius = (target - pivot).magnitude;
                if (!CheckRadiusMatched(currentRadius, targetRadius) && AllowSpiral)
                {
                    PrintError("Warning: Radius Error, motion might be spiral.\n");
                }
            }
        }

        switch (gCode)
        {
            case 0:
                LinearTraverseTo(target, maxDistanceRapid);
                break;
            case 1:
                LinearTraverseTo(target, maxDistanceFeed);
                break;
            case 2:
                CircularTraverseTo(target, pivot, maxDistanceFeed, angleDiff, radiusDiff);
                if (IsAtTargetCrude(target))
                    isFinishedTask = true;
                break;
            case 3:
                CircularTraverseTo(target, pivot, maxDistanceFeed, angleDiff, radiusDiff);
                if (IsAtTargetCrude(target))
                    isFinishedTask = true;
                break;
            default:
                PrintError("Unknown G Code falling into ReadGroup1\n");
                break;
        }

        if (IsAtTarget(target))
            isFinishedTask = true;
    }

    #endregion

    #region Individual Machine-Consistent Methods

    #region Group1
    protected void LinearTraverseTo(Vector3 targetPos, float maxDistance)
    {
        Vector3 newPos = Vector3.MoveTowards(TargetKnife.transform.position, targetPos, maxDistance);
        /*float actualDistance = Vector3.Distance(newPos, TargetKnife.transform.position);
        float distanceRemainder = maxDistance - actualDistance;
        if (distanceRemainder > 0.0001)
        {
            newPos = Vector3.MoveTowards(newPos, targetPos, distanceRemainder);
        }*/
        TranslateToNewPos(newPos);
    }

    protected void CircularTraverseTo(Vector3 targetPos, Vector3 pivotPos, float maxDistance, float totalDegreeDiff, float totalRadiusDiff)
    {
        Vector3 currentDirFromPivot = TargetKnife.transform.position - pivotPos;
        Vector3 targetDirFromPivot = targetPos - pivotPos;
        float currentRadius = currentDirFromPivot.magnitude;
        float targetRadius = targetDirFromPivot.magnitude;
        if (!CheckRadiusMatched(currentRadius, targetRadius) && !AllowSpiral)
        {
            PrintError("Radius Error\n");
            isFinishedTask = true;
            return;
        }

        float maxDegree =  Mathf.Rad2Deg * (maxDistance / ((currentRadius + targetRadius) / 2));        // Estimated average radius
        maxDegree = Mathf.Min(maxDegree, Vector3.Angle(currentDirFromPivot, targetDirFromPivot));
        Vector3 newPosDir = RotateDirAroundAxis(currentDirFromPivot, maxDegree, axis);
        newPosDir += ((maxDegree / totalDegreeDiff) * totalRadiusDiff) * Vector3.Normalize(newPosDir);
        Vector3 newPos = newPosDir + pivotPos;

        if (float.IsNaN(newPos.x) || float.IsNaN(newPos.y) || float.IsNaN(newPos.z))
        {
            newPos = TargetKnife.transform.position;
        }
        TranslateToNewPos(newPos);
    }

    protected Vector3 AxisForRotation(Vector3 currentPos, Vector3 targetPos, Vector3 pivotPos)
    {
        return Vector3.Normalize(Vector3.Cross(currentPos - pivotPos, targetPos - pivotPos));
    }

    protected Vector3 RotateDirAroundAxis(Vector3 pointDir, float angle, Vector3 axis)                             // Angle in Degree, Left-hand Rule, return newPos direction
    {
        return Quaternion.AngleAxis(angle, axis) * pointDir;
    }

    protected bool CheckRadiusMatched(float radius1, float radius2)
    {
        return Mathf.Abs(radius1 - radius2) < 0.001;
    }
    #endregion

    #endregion

    #region Utilities

    protected bool IsAtTarget(Vector3 targetPos)
    {
        return Vector3.Distance(TargetKnife.transform.position, targetPos) < ExactThreshold;
    }

    protected bool IsAtTargetCrude(Vector3 targetPos)
    {
        return Vector3.Distance(TargetKnife.transform.position, targetPos) < CrudeThreshold;
    }

    protected float Arctan0360(float y, float x)
    {
        float output = Mathf.Rad2Deg * Mathf.Atan2(y, x);
        return output >= 0f ? output : output + 360f;
    }

    protected float Clamp0360(float eulerAngles)
    {
        float result = eulerAngles - Mathf.CeilToInt(eulerAngles / 360f) * 360f;
        if (result < 0)
        {
            result += 360f;
        }
        return result;
    }

    protected void PrintError(string errorMessage)
    {
        print(errorMessage + InstructionQueue.Peek().ToString());
        if (DebugVR != null)
            DebugVR.Println(errorMessage + InstructionQueue.Peek().ToStringShort());
    }

    protected void PrintInstruction()
    {
        print(InstructionQueue.Peek());
        if (DebugVR != null)
            DebugVR.Println(InstructionQueue.Peek().ToStringShort());
    }

    protected void PrintVector(Vector3 vector)
    {
        print(string.Format("({0}, {1}, {2})", vector.x, vector.y, vector.z));
    }

    #endregion

    #region Machine-dependent Methods

    protected virtual void InitializeG02_03(CNCInstructionMotion instr)
    {
        /*scaleVector = Vector3.one * instr.prefixModifier;
        target = pieceOriginTransform.TransformPoint(Vector3.Scale(instr.TargetPos, scaleVector));*/

        pivot = pieceOriginTransform.TransformDirection(Vector3.Scale(instr.PivotRelPos, scaleVector)) + TargetKnife.transform.position;
        axis = AxisForRotation(TargetKnife.transform.position, target, pivot);

        // Determining the direction of the axis
        Vector3 initialDir = TargetKnife.transform.position - pivot;
        Vector3 targetDir = target - pivot;
        radiusDiff = targetDir.magnitude - initialDir.magnitude;
        float initialAngle = Arctan0360(initialDir.z, initialDir.x);
        float targetAngle = Arctan0360(targetDir.z, targetDir.x);
        angleDiff = Clamp0360(-(targetAngle - initialAngle));
        
        // Ensuring the axis to always be Clockwise rotated as seen from top view
        if (axis.y < 0)
            axis *= -1;

        // Hardcode for 180degrees
        if (Mathf.Abs(angleDiff - 180f) < 0.1f || axis == Vector3.zero)
        {
            axis = Quaternion.FromToRotation(new Vector3(initialDir.x, 0, initialDir.z), initialDir) * Vector3.up;
        }

        // Reverse the axis for Counter-Clockwise rotation (G03)
        if (gCode == 3)
        {
            axis *= -1;
            angleDiff = Clamp0360(-angleDiff);
        }
    }

    protected virtual void TranslateToNewPos(Vector3 newPos)
    {
        TargetKnife.transform.position = newPos;
    }

    #endregion
}
