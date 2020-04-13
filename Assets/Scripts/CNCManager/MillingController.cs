using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MillingController : CNCController
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

    protected override void TranslateToNewPos(Vector3 knifeNewPos)
    {
        Vector3 knifeMoveVec = knifeNewPos - TargetKnife.transform.position;
        Vector3 pieceNewPos = pieceTransform.position - knifeMoveVec;          // pieceMovVec = -knifeMovVec; therefore, pieceNewPos = (-knifeMovVec) + pieceOldPos
        Vector3 targetNewPos = target - knifeMoveVec;

        TargetKnife.transform.position = new Vector3(TargetKnife.transform.position.x, knifeNewPos.y, TargetKnife.transform.position.z);
        pieceTransform.position = new Vector3(pieceNewPos.x, pieceTransform.position.y, pieceNewPos.z);
        target = new Vector3(targetNewPos.x, target.y, targetNewPos.z);

        if (gCode == 2 || gCode == 3)
        {
            Vector3 pivotNewPos = pivot - knifeMoveVec;
            pivot = new Vector3(pivotNewPos.x, pivot.y, pivotNewPos.z);
        }
    }
}
