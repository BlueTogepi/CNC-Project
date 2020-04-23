using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LatheController : CNCController
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    /*public override void PieceFinished()
    {
        if (InstructionQueue.Count == 0)
        {
            StopCuttingSound();
            StopMachineSound();

            CylinderMeshGenerator cmg = Piece.GetComponent<CylinderMeshGenerator>();
            cmg.FinishCutting();
            cmg.bladeLeft = null;
            cmg.bladeRight = null;

            GameObject newPiece = Instantiate(PiecePrefab, Piece.transform.position, Piece.transform.rotation, Piece.transform.parent);
            Piece.transform.parent = null;

            newPiece.transform.position = Piece.transform.position;
            newPiece.transform.rotation = Piece.transform.rotation;
            Piece.transform.position = PieceFinishedPlacement.transform.position;
            Piece.transform.rotation = PieceFinishedPlacement.transform.rotation;

            newPiece.transform.Find("ORIGIN").position = PieceOrigin.transform.position;
            PieceOrigin = newPiece.transform.Find("ORIGIN").gameObject;

            Piece = newPiece;
        }
    }*/

    public override void RePiece()
    {
        Piece.GetComponent<CylinderMeshGenerator>().RePieceMesh();
        PrintlnWithVR("Piece Mesh Regenerated.");
    }

    public override void GetBackHome()
    {
        if (InstructionList.Count == 0)
        {
            CNCInstructionMotion tempInstr1 = new CNCInstructionMotion
            {
                G = 0,
                Group = 1,
                prefixModifier = 0.001f,
                FeedRate = 10f,
                SpindleSpeed = 0,
                Tool = 1,
                MiscFunc = 0
            };
            Vector3 tempPos1 = PieceOrigin.transform.InverseTransformPoint(new Vector3(TargetKnife.transform.position.x, TargetKnife.transform.position.y, Home.transform.position.z)) * 1000f;
            tempInstr1.TargetPos = new Vector3(tempPos1.x, tempPos1.y, tempPos1.z);
            tempInstr1.PivotRelPos = Vector3.zero;

            CNCInstructionMotion tempInstr2 = new CNCInstructionMotion
            {
                G = 0,
                Group = 1,
                prefixModifier = 0.001f,
                FeedRate = 10f,
                SpindleSpeed = 0,
                Tool = 1,
                MiscFunc = 0
            };
            Vector3 tempPos2 = PieceOrigin.transform.InverseTransformPoint(new Vector3(Home.transform.position.x, Home.transform.position.y, Home.transform.position.z)) * 1000f;
            tempInstr2.TargetPos = new Vector3(tempPos2.x, tempPos2.y, tempPos2.z);
            tempInstr2.PivotRelPos = Vector3.zero;

            InstructionList.AddLast(tempInstr1);
            InstructionList.AddLast(tempInstr2);

            PrintlnWithVR("2 G00 Instructions added to get back home.");
        }
    }

    /*protected override void InitializeG02_03(CNCInstructionMotion instr)
    {
        pivot = pieceOriginTransform.TransformDirection(Vector3.Scale(instr.PivotRelPos, scaleVector)) + TargetKnife.transform.position;
        axis = AxisForRotation(TargetKnife.transform.position, target, pivot);

        // Determining the direction of the axis
        Vector3 initialDir = TargetKnife.transform.position - pivot;
        Vector3 targetDir = target - pivot;
        radiusDiff = targetDir.magnitude - initialDir.magnitude;
        float initialAngle = Arctan0360(initialDir.y, initialDir.x);
        float targetAngle = Arctan0360(targetDir.y, targetDir.x);
        angleDiff = Clamp0360(-(targetAngle - initialAngle));

        // Ensuring the axis to always be Clockwise rotated as seen from top view
        if (axis.z > 0)
            axis *= -1;

        // Hardcode for 180degrees
        if (Mathf.Abs(angleDiff - 180f) < 0.1f || axis == Vector3.zero)
        {
            axis = Quaternion.FromToRotation(new Vector3(initialDir.x, initialDir.y, 0), initialDir) * Vector3.back;
        }

        // Reverse the axis for Counter-Clockwise rotation (G03)
        if (gCode == 3)
        {
            axis *= -1;
            angleDiff = Clamp0360(-angleDiff);
        }
    }*/
}
