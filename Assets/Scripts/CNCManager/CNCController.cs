using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CNCController : MonoBehaviour
{
    public GameObject TargetKnife;                          // All knife transformation control in this script should not be relative to the piece (only CNC instructions are relative)
    public GameObject Piece;
    public GameObject PieceOrigin;
    public GameObject Home;
    public DebugConsole DebugVR;
    public float RapidMoveSpeed = 0.1f;
    public float FeedMoveSpeed = 0.01f;
    [Tooltip("Crude Threshold for Vector3 Equal Position Checking")]
    public float CrudeThreshold = 1e-4f;
    [Tooltip("Exact Threshold for Vector3 Equal Position Checking")]
    public float ExactThreshold = 1e-5f;
    [Tooltip("Allow spiral motion for G02 and G03 (Circular Motion with different radius)")]
    public bool AllowSpiral = false;
    /*[Tooltip("Empty GameObject indicating position and rotation for finished piece placement")]
    public GameObject PieceFinishedPlacement;
    [Tooltip("This is just for generating a new piece in-game, any transform measurement should be done on 'Piece'")]
    public GameObject PiecePrefab;*/

    public ParticleSystem Particles;
    public AudioSource MachineSound;
    public AudioSource CuttingSound;

    [HideInInspector]
    public Queue<CNCInstruction> InstructionQueue;

    protected Vector3 PieceInitialPosition;
    protected Quaternion PieceInitialRotation;

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

        PieceInitialPosition = pieceTransform.position;
        PieceInitialRotation = pieceTransform.rotation;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (InstructionQueue.Count != 0)
        {
            PlayMachineSound();

            CNCInstruction instr = InstructionQueue.Peek();
            if (isStartingTask)
            {
                PrintInstruction();
                if (NeedsCuttingFX(instr.G))
                {
                    PlayCuttingSound();
                    PlayParticlesFX();
                }
            }

            ReadInstruction(instr);

            isStartingTask = false;
            if (isFinishedTask)
            {
                InstructionQueue.Dequeue();
                isFinishedTask = false;
                isStartingTask = true;

                if (InstructionQueue.Count != 0)
                {
                    if (!NeedsCuttingFX(InstructionQueue.Peek().G))
                    {
                        StopCuttingSound();
                        StopParticlesFX();
                    }
                }
                else
                {
                    StopCuttingSound();
                    StopParticlesFX();
                }
            }
        }
        else
        {
            StopMachineSound();
            StopCuttingSound();
            StopParticlesFX();
        }
    }

    #region Piece Control

    /*public virtual void PieceStartActive()
    {
        print("Not implemented");
    }

    public virtual void PieceFinished()
    {
        print("Not implemented");
    }*/

    public virtual void RePiece()
    {
        print("Base class CNCController can't RePiece().");
    }

    #endregion

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

    #region Home

    public void SetHome()
    {
        Vector3 newHome = TargetKnife.transform.position;
        Home.transform.position = newHome;
        newHome = pieceOriginTransform.InverseTransformPoint(newHome);
        newHome = new Vector3(newHome.x, newHome.z, newHome.y) * 1000;
        PrintlnWithVR("Home set at " + VectorToStringLong(newHome));
    }

    public virtual void GetBackHome()
    {
        if (InstructionQueue.Count == 0)
        {
            CNCInstructionMotion tempInstr1 = new CNCInstructionMotion
            {
                G = 0,
                Group = 1,
                prefixModifier = 0.001f,
                FeedRate = 0,
                SpindleSpeed = 0,
                Tool = 1,
                MiscFunc = 0
            };
            Vector3 tempPos1 = PieceOrigin.transform.InverseTransformPoint(new Vector3(TargetKnife.transform.position.x, Home.transform.position.y, TargetKnife.transform.position.z)) * 1000f;
            tempInstr1.TargetPos = new Vector3(tempPos1.x, tempPos1.y, tempPos1.z);
            tempInstr1.PivotRelPos = Vector3.zero;

            CNCInstructionMotion tempInstr2 = new CNCInstructionMotion
            {
                G = 0,
                Group = 1,
                prefixModifier = 0.001f,
                FeedRate = 0,
                SpindleSpeed = 0,
                Tool = 1,
                MiscFunc = 0
            };
            Vector3 tempPos2 = PieceOrigin.transform.InverseTransformPoint(new Vector3(Home.transform.position.x, Home.transform.position.y, Home.transform.position.z)) * 1000f;
            tempInstr2.TargetPos = new Vector3(tempPos2.x, tempPos2.y, tempPos2.z);
            tempInstr2.PivotRelPos = Vector3.zero;

            InstructionQueue.Enqueue(tempInstr1);
            InstructionQueue.Enqueue(tempInstr2);

            PrintlnWithVR("2 G00 Instructions added to get back home.");
        }
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

    public void ClearInstrQueue()
    {
        InstructionQueue.Clear();
        isFinishedTask = false;
        isStartingTask = true;
        PrintlnWithVR("Ongoing CNC commands cleared.");
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
        print(VectorToStringLong(vector));
        if (DebugVR != null)
            DebugVR.Println(VectorToStringLong(vector));
    }

    protected void PrintlnWithVR(string str)
    {
        print(str);
        if (DebugVR != null)
            DebugVR.Println(str);
    }

    protected string VectorToStringLong(Vector3 vector)
    {
        return string.Format("({0}, {1}, {2})", vector.x, vector.y, vector.z);
    }

    #endregion

    #region Effects

    protected void PlayMachineSound()
    {
        if (MachineSound != null)
        {
            if (!MachineSound.isPlaying)
            {
                MachineSound.Play();
            }
        }
    }

    protected void StopMachineSound()
    {
        MachineSound.Stop();
    }

    protected void PlayCuttingSound()
    {
        if (CuttingSound != null)
        {
            if (!CuttingSound.isPlaying)
            {
                CuttingSound.Play();
            }
        }
    }

    protected void StopCuttingSound()
    {
        CuttingSound.Stop();
    }

    protected void PlayParticlesFX()
    {
        if (Particles != null)
        {
            if (!Particles.isPlaying)
            {
                Particles.Play();
            }
        }
    }

    protected void StopParticlesFX()
    {
        Particles.Stop();
    }

    protected bool NeedsCuttingFX(int g)
    {
        return g == 1 || g == 2 || g == 3;
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
