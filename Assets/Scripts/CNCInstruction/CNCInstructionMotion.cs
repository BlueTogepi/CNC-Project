using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CNCInstructionMotion : CNCInstruction
{
    public float posX;
    public float posY;              // CNC-based coordination
    public float posZ;              // CNC-based coordination
    public float posI;
    public float posJ;
    public float posK;

    public Vector3 TargetPos
    {
        get
        {
            return new Vector3(posX, posZ, posY);           // World-based coordination
        }
        set
        {
            Vector3 target = value;                         // World-based coordination
            posX = target.x;
            posY = target.z;
            posZ = target.y;
        }
    }

    public Vector3 PivotRelPos
    {
        get
        {
            return new Vector3(posI, posK, posJ);           // World-based coordination
        }
        set
        {
            Vector3 relPivot = value;                       // World-based coordination
            posI = relPivot.x;
            posJ = relPivot.z;
            posK = relPivot.y;
        }
    }

    public override string ToStringShort()
    {
        return base.ToStringShort() + string.Format(" (x{0}, y{1}, z{2}) (i{3}, j{4}, k{5})", posX, posY, posZ, posI, posJ, posK);
    }

    public override string ToString()
    {
        return base.ToString() + string.Format("\nModal Group1 Motion:\n(x, y, z) = ({0}, {1}, {2})\n(i, j, k) = ({3}, {4}, {5})", posX, posY, posZ, posI, posJ, posK);
    }
}
