using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CNCInstruction
{
    public int G;
    public int Group;
    public float prefixModifier;
    public float FeedRate;
    public float SpindleSpeed;
    public int Tool;
    public int MiscFunc;

    public static int GCode2ModalGroup(int gCode)
    {
        switch (gCode)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                return 1;
            case 17:
            case 18:
            case 19:
                return 2;
            case 20:
            case 21:
                return 6;
            case 40:
            case 41:
            case 42:
                return 7;
            case 43:
            case 49:
                return 8;
            case 54:
            case 55:
            case 56:
            case 57:
            case 58:
            case 59:
                return 12;
            case 80:
            case 81:
            case 82:
            case 84:
            case 85:
            case 86:
            case 87:
            case 88:
            case 89:
                return 1;
            case 90:
            case 91:
                return 3;
            case 93:
            case 94:
                return 5;
            case 98:
            case 99:
                return 10;
            default:
                return 0;
        }
    }

    public virtual string ToStringShort()
    {
        return string.Format("G{0} F{1} S{2} T{3} M{4}",
            G, FeedRate, SpindleSpeed, Tool, MiscFunc);
    }

    public override string ToString()
    {
        return string.Format("G{0}\nprefixModifier: x{1}\nfeedRate: {2}\nspindleSpeed: {3}\ntool: {4}\nmiscFunc: {5}",
            G, prefixModifier, FeedRate, SpindleSpeed, Tool, MiscFunc);
    }
}
