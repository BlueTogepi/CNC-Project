using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CNCTranslator : MonoBehaviour
{
    public CNCController Controller;
    public DebugConsole DebugVR;
    [HideInInspector]
    public string[] commandList;

    private Queue<int> modalGroupQueue;
    private Queue<CNCInstruction> TargetInstrQueue;

    // Setup for CNCInstruction Class for each command
    private int line = 0;
    private bool isFirstCommand = true;
    private int gCode = 0;
    private int[] gCodeForEachGroup;
    public float prefixModifier = 0.01f; // 1 for metre, 0.01 for centimetre
    public float feedRate = 1500f;
    public float spindleSpeed = 10000f;
    public int tool = 1;
    public int miscFunc = 0;

    // Group1 Motion
    private float posX = 0f, posY = 0f, posZ = 0f;
    private float posI = 0f, posJ = 0f, posK = 0f;

    // Group2 Plane Selection
    private int plane = 1;          // 1: xy, 2: yz, 3: zx

    private void Start()
    {
        modalGroupQueue = new Queue<int>();
        gCodeForEachGroup = new int[] { -1, 0, 17, 90, -1, 93, 21, 40, 43, -1, 98, -1, 54 };

        TargetInstrQueue = Controller.InstructionQueue;
    }

    public void TranslateCommand(String inputCommandString)
    {
        if (DebugVR != null)
            DebugVR.Println("Input Command String Received");
        commandList = inputCommandString.ToUpper().Trim().Split(new char[] { '\n' });
        ReadCommandList();
    }

    public void ReadCommandList()
    {
        foreach (string commandLine in commandList)
        {
            foreach (string cmd in commandLine.Split(new char[] { ' ' }))
            {
                ReadCommand(cmd);
            }

            if (modalGroupQueue.Count == 0)
            {
                SendCNCInstruction(CNCInstruction.GCode2ModalGroup(gCode));
            }
            while (modalGroupQueue.Count != 0)
            {
                SendCNCInstruction(modalGroupQueue.Dequeue());
            }
            ResetNonModal();
        }
    }

    private void ReadCommand(string cmd)
    {
        switch (cmd[0])
        {
            case 'N':
                if (isFirstCommand)
                {
                    line++;
                    isFirstCommand = false;
                    break;
                }
                int num = int.Parse(cmd.Substring(1));
                if (++line != num)
                {
                    print("Expected line number (N" + line + ") mismatched with the input line number (" + cmd + "), changed line number to " + cmd);
                    line = num;
                }
                break;
            case 'G':
                gCode = int.Parse(cmd.Substring(1));
                int group = CNCInstruction.GCode2ModalGroup(gCode);
                modalGroupQueue.Enqueue(group);
                gCodeForEachGroup[group] = gCode;
                break;
            case 'F':
                feedRate = float.Parse(cmd.Substring(1));
                break;
            case 'S':
                spindleSpeed = float.Parse(cmd.Substring(1));
                break;
            case 'T':
                tool = int.Parse(cmd.Substring(1));
                break;
            case 'M':
                miscFunc = int.Parse(cmd.Substring(1));
                break;
            case 'X':
                posX = float.Parse(cmd.Substring(1));
                break;
            case 'Y':
                posY = float.Parse(cmd.Substring(1));
                break;
            case 'Z':
                posZ = float.Parse(cmd.Substring(1));
                break;
            case 'I':
                posI = float.Parse(cmd.Substring(1));
                break;
            case 'J':
                posJ = float.Parse(cmd.Substring(1));
                break;
            case 'K':
                posK = float.Parse(cmd.Substring(1));
                break;
            default:
                print("Unknown '" + cmd + "' command at line " + line + ", skipped this command.");
                if (DebugVR != null)
                    DebugVR.Println("Unknown '" + cmd + "' command at line " + line + ", skipped this command.");
                break;
        }
    }

    private void SendCNCInstruction(int modalGroup)
    {
        CNCInstruction instr;

        switch (modalGroup)
        {
            case 1:
                instr = new CNCInstructionMotion {
                    G = gCodeForEachGroup[1],
                    Group = modalGroup,
                    prefixModifier = this.prefixModifier,
                    FeedRate = this.feedRate,
                    SpindleSpeed = this.spindleSpeed,
                    Tool = this.tool,
                    MiscFunc = this.miscFunc,
                    posX = this.posX,
                    posY = this.posY,
                    posZ = this.posZ,
                    posI = this.posI,
                    posJ = this.posJ,
                    posK = this.posK
                };
                break;
            default:
                instr = new CNCInstruction
                {
                    G = this.gCode,
                    Group = modalGroup,
                    prefixModifier = this.prefixModifier,
                    FeedRate = this.feedRate,
                    SpindleSpeed = this.spindleSpeed,
                    Tool = this.tool,
                    MiscFunc = this.miscFunc
                };
                print("Invalid modal group Found");
                if (DebugVR != null)
                    DebugVR.Println("Invalid modal group found");
                break;
        }

        TargetInstrQueue.Enqueue(instr);
    }

    private void ResetNonModal()
    {
        posI = 0f; posJ = 0f; posK = 0f;
    }
}
