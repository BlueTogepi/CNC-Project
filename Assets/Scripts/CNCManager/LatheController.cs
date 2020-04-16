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

    protected override void InitializeG02_03(CNCInstructionMotion instr)
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
    }
}
