namespace Sharpie.CCompiler.Optimizations;

public static class Optimizer
{
    public static void Optimize(List<Instruction> instructions)
    {
        var changed = true;

        while (changed)
        {
            changed = false;

            for (int i = 0; i < instructions.Count - 1; i++)
            {
                var current = instructions[i];
                var next = instructions[i + 1];

                if (current.IsLabel || current.IsComment)
                    continue;

                // LDI rX, 0 -> XOR rX, rX (stupid? yes, but it saves a byte.)
                if (!current.IsAlt && current.Mnemonic == "LDI" && current.Arg2 == "0")
                {
                    current.Mnemonic = "XOR";
                    current.Arg2 = current.Arg1;
                    current.RebuildText();
                    changed = true;
                    break;
                }

                // ALT IADD rX, 1 -> DINC rX
                if (current.IsAlt && current.Arg2 == "1")
                {
                    if (current.Mnemonic == "IADD")
                    {
                        current.IsAlt = false;
                        current.Mnemonic = "DINC";
                        current.Arg2 = "";
                    }
                    else if (current.Mnemonic == "ISUB")
                    {
                        current.IsAlt = false;
                        current.Mnemonic = "DDEC";
                        current.Arg2 = "";
                    }

                    if (current.Mnemonic is "DINC" or "DDEC")
                    {
                        current.RebuildText();
                        changed = true;
                        break;
                    }
                }

                // JMP to the very next line
                if (!current.IsAlt && current.Mnemonic == "JMP" && next.IsLabel)
                {
                    if (current.Arg1 == next.OriginalText.TrimEnd(':'))
                    {
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }

                // MOV rX, rX -> nothing
                if (!current.IsAlt && current.Mnemonic == "MOV" && current.Arg1 == current.Arg2)
                {
                    instructions.RemoveAt(i);
                    changed = true;
                    break;
                }

                // MOV rX, rY followed by MOV rY, rX
                if (
                    !current.IsAlt
                    && !next.IsAlt
                    && current.Mnemonic == "MOV"
                    && next.Mnemonic == "MOV"
                    && current.Arg1 == next.Arg2
                    && current.Arg2 == next.Arg1
                )
                {
                    instructions.RemoveAt(i + 1); // Remove the second MOV
                    changed = true;
                    break;
                }

                // Useless math (+0, -0, *1, /1)
                if (
                    (current.Mnemonic is "IADD" or "ISUB" && current.Arg2 == "0")
                    || (current.Mnemonic is "IMUL" or "IDIV" && current.Arg2 == "1")
                )
                {
                    instructions.RemoveAt(i);
                    changed = true;
                    break;
                }

                // Clobbered loads
                var currentWritesToArg1 =
                    current.Mnemonic is "MOV" or "LDI" or "LDP" or "LDM" or "LDS" or "POP";
                var nextOverwritesArg1 =
                    next.Mnemonic is "LDI" or "LDP" or "LDM" or "LDS" or "GETSP";

                if (currentWritesToArg1 && nextOverwritesArg1 && current.Arg1 == next.Arg1)
                {
                    // Ensure next instruction isn't reading Arg1 as its source (e.g. LDP r1, r1)
                    if (next.Arg2 != current.Arg1 && next.Arg3 != current.Arg1)
                    {
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }

                // Store followed by Load
                if (
                    current.IsAlt == next.IsAlt
                    && current.Arg1 == next.Arg1
                    && current.Arg2 == next.Arg2
                )
                {
                    if (
                        (current.Mnemonic == "STA" && next.Mnemonic == "LDP")
                        || (current.Mnemonic == "STM" && next.Mnemonic == "LDM")
                        || (current.Mnemonic == "STS" && next.Mnemonic == "LDS")
                    )
                    {
                        instructions.RemoveAt(i + 1); // remove the load
                        changed = true;
                        break;
                    }
                }

                // Load followed by Store
                if (
                    current.IsAlt == next.IsAlt
                    && current.Arg1 == next.Arg1
                    && current.Arg2 == next.Arg2
                )
                {
                    if (
                        (current.Mnemonic == "LDP" && next.Mnemonic == "STA")
                        || (current.Mnemonic == "LDM" && next.Mnemonic == "STM")
                        || (current.Mnemonic == "LDS" && next.Mnemonic == "STS")
                    )
                    {
                        instructions.RemoveAt(i + 1); // remove the store
                        changed = true;
                        break;
                    }
                }

                // PUSH followed by POP
                if (
                    current.IsAlt == next.IsAlt
                    && current.Mnemonic == "PUSH"
                    && next.Mnemonic == "POP"
                )
                {
                    if (current.Arg1 == next.Arg1)
                    {
                        instructions.RemoveAt(i);
                        instructions.RemoveAt(i);
                    }
                    else
                    {
                        current.Mnemonic = "MOV";
                        current.Arg2 = current.Arg1; // Source
                        current.Arg1 = next.Arg1; // Dest
                        current.RebuildText();
                        instructions.RemoveAt(i + 1); // remove the POP
                    }
                    changed = true;
                    break;
                }

                // IMUL rX, 2 -> ADD rX, rX
                if (!current.IsAlt && current.Mnemonic == "IMUL" && current.Arg2 == "2")
                {
                    current.Mnemonic = "ADD";
                    current.Arg2 = current.Arg1;
                    current.RebuildText();
                    changed = true;
                    break;
                }

                // accumulate math in as few instructions as needed (for example IADD rX, 2 followed by IADD rX, 10 => IADD rX, 12)
                if (current.IsAlt == next.IsAlt && current.Arg1 == next.Arg1)
                {
                    bool isAddSub =
                        current.Mnemonic is "IADD" or "ISUB" && next.Mnemonic is "IADD" or "ISUB";
                    bool isMulDiv =
                        current.Mnemonic is "IMUL" or "IDIV" && current.Mnemonic == next.Mnemonic;

                    if (isAddSub || isMulDiv)
                    {
                        if (
                            int.TryParse(current.Arg2, out int val1)
                            && int.TryParse(next.Arg2, out int val2)
                        )
                        {
                            long result = 0;

                            if (isAddSub)
                            {
                                if (current.Mnemonic == "ISUB")
                                    val1 = -val1;
                                if (next.Mnemonic == "ISUB")
                                    val2 = -val2;
                                result = val1 + val2;
                            }
                            else if (isMulDiv)
                            {
                                result = val1 * val2;
                            }

                            // Immediate math must fit in an 8-bit unsigned byte (0-255)
                            if (Math.Abs(result) <= 255)
                            {
                                if (result == 0 && isAddSub)
                                {
                                    instructions.RemoveAt(i);
                                    instructions.RemoveAt(i);
                                }
                                else
                                {
                                    if (isAddSub)
                                        current.Mnemonic = result > 0 ? "IADD" : "ISUB";
                                    current.Arg2 = Math.Abs(result).ToString();
                                    current.RebuildText();
                                    instructions.RemoveAt(i + 1); // remove the second operation
                                }
                                changed = true;
                                break;
                            }
                        }
                    }
                }

                // LDP -> Math -> STA  ==>  ALT <math>
                if (i < instructions.Count - 2)
                {
                    var nextNext = instructions[i + 2];

                    if (
                        !current.IsAlt
                        && !nextNext.IsAlt
                        && current.Mnemonic == "LDM"
                        && nextNext.Mnemonic == "STM"
                        && current.Arg1 == next.Arg1
                        && next.Arg1 == nextNext.Arg1
                        && current.Arg2 == nextNext.Arg2
                    )
                    {
                        if (next.Mnemonic is "INC" or "DEC")
                        {
                            current.Mnemonic = "LDI"; // Load the address instead of the value
                            current.RebuildText();

                            next.Mnemonic = "D" + next.Mnemonic; // DINC or DDEC
                            next.Arg1 = current.Arg1; // The pointer register
                            next.RebuildText();

                            instructions.RemoveAt(i + 2); // Remove STM
                            changed = true;
                            break;
                        }
                        else if (
                            next.Mnemonic
                            is "IADD"
                                or "ISUB"
                                or "IMUL"
                                or "IDIV"
                                or "IMOD"
                                or "IAND"
                                or "IOR"
                                or "IXOR"
                        )
                        {
                            current.Mnemonic = "LDI"; // Load the address
                            current.RebuildText();

                            next.IsAlt = true;
                            next.Arg1 = current.Arg1;
                            // Arg2 is already the correct immediate value
                            next.RebuildText();

                            instructions.RemoveAt(i + 2); // Remove STM
                            changed = true;
                            break;
                        }
                    }
                }

                // LDI rTemp, Val followed by MOV rLocal, rTemp -> LDI rLocal, Val
                if (
                    !current.IsAlt
                    && !next.IsAlt
                    && current.Mnemonic == "LDI"
                    && next.Mnemonic == "MOV"
                    && current.Arg1 == next.Arg2
                )
                {
                    current.Arg1 = next.Arg1; // Change target to the local register
                    current.RebuildText();
                    instructions.RemoveAt(i + 1); // Delete the MOV
                    changed = true;
                    break;
                }

                // MOV rTemp, rLocal followed by CMP rTemp, rOther -> CMP rLocal, rOther
                if (
                    !current.IsAlt
                    && !next.IsAlt
                    && current.Mnemonic == "MOV"
                    && next.Mnemonic == "CMP"
                )
                {
                    if (current.Arg1 == next.Arg1)
                    {
                        next.Arg1 = current.Arg2;
                        next.RebuildText();
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
                    else if (current.Arg1 == next.Arg2)
                    {
                        next.Arg2 = current.Arg2;
                        next.RebuildText();
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }

                // LDI rA, X -> LDI rB, Y -> MUL/ADD rA, rB => LDI rA, X*Y
                if (i < instructions.Count - 2)
                {
                    var nextNext = instructions[i + 2];
                    if (
                        !current.IsAlt
                        && !next.IsAlt
                        && !nextNext.IsAlt
                        && current.Mnemonic == "LDI"
                        && next.Mnemonic == "LDI"
                        && (nextNext.Mnemonic == "MUL" || nextNext.Mnemonic == "ADD")
                        && current.Arg1 == nextNext.Arg1
                        && next.Arg1 == nextNext.Arg2
                    )
                    {
                        if (
                            int.TryParse(current.Arg2, out int v1)
                            && int.TryParse(next.Arg2, out int v2)
                        )
                        {
                            int result = nextNext.Mnemonic == "MUL" ? (v1 * v2) : (v1 + v2);
                            current.Arg2 = result.ToString();
                            current.RebuildText();
                            instructions.RemoveAt(i + 2); // Remove MUL/ADD
                            instructions.RemoveAt(i + 1); // Remove second LDI
                            changed = true;
                            break;
                        }
                    }
                }

                // Dead code (anything other than a label after a JMP or RET or HALT is functionally unreachable)
                if (
                    current.Mnemonic == "JMP"
                    || current.Mnemonic == "RET"
                    || current.Mnemonic == "HALT"
                )
                {
                    if (!next.IsLabel && !next.IsDirective && !next.IsComment)
                    {
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        break;
                    }
                }

                // Folding multiple INC/DEC into one instruction
                if (!current.IsAlt && !next.IsAlt && current.Arg1 == next.Arg1)
                {
                    if (
                        (current.Mnemonic == "INC" && next.Mnemonic == "DEC")
                        || (current.Mnemonic == "DEC" && next.Mnemonic == "INC")
                    )
                    {
                        // They cancel out!
                        instructions.RemoveAt(i);
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
                    else if (current.Mnemonic == "INC" && next.Mnemonic == "INC")
                    {
                        // Two INCs = 4 bytes. IADD rX, 2 = 3 bytes. Fuse them!
                        current.Mnemonic = "IADD";
                        current.Arg2 = "2";
                        current.RebuildText();
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        break;
                    }
                    else if (current.Mnemonic == "DEC" && next.Mnemonic == "DEC")
                    {
                        // Two DECs = 4 bytes. ISUB rX, 2 = 3 bytes. Fuse them!
                        current.Mnemonic = "ISUB";
                        current.Arg2 = "2";
                        current.RebuildText();
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        break;
                    }
                }

                // IADD rX, 1 -> INC rX
                if (!current.IsAlt && current.Arg2 == "1")
                {
                    if (current.Mnemonic == "IADD")
                    {
                        current.Mnemonic = "INC";
                        current.Arg2 = "";
                    }
                    else if (current.Mnemonic == "ISUB")
                    {
                        current.Mnemonic = "DEC";
                        current.Arg2 = "";
                    }

                    if (current.Mnemonic is "INC" or "DEC")
                    {
                        current.RebuildText();
                        changed = true;
                        break;
                    }
                }

                // LDI -> STA/LDP ==> STM/LDM
                // Avoid loading an address known at compile time and dereferencing it
                if (!current.IsAlt && current.Mnemonic == "LDI")
                {
                    if (next.Mnemonic == "STA" && current.Arg1 == next.Arg2)
                    {
                        current.IsAlt = next.IsAlt; // Preserve the 8-bit/16-bit flag from the STA
                        current.Mnemonic = "STM"; // Convert to absolute store
                        current.Arg1 = next.Arg1; // The value register
                        // current.Arg2 is already the label/address
                        current.RebuildText();

                        instructions.RemoveAt(i + 1); // Delete the STA
                        changed = true;
                        break;
                    }
                    else if (next.Mnemonic == "LDP" && current.Arg1 == next.Arg2)
                    {
                        current.IsAlt = next.IsAlt; // Preserve the 8-bit/16-bit flag from the LDP
                        current.Mnemonic = "LDM"; // Convert to absolute Load
                        current.Arg1 = next.Arg1; // The destination register
                        // current.Arg2 is already the label/address
                        current.RebuildText();

                        instructions.RemoveAt(i + 1); // Delete the LDP
                        changed = true;
                        break;
                    }
                }

                // MOV rTemp, rSrc -> LDP rDest, rTemp ==> LDP rDest, rSource
                if (!current.IsAlt && current.Mnemonic == "MOV")
                {
                    var isLoadStore = next.Mnemonic is "LDP" or "STA" or "LDS" or "STS";

                    // If the NEXT instruction uses our target register as its POINTER (Arg2)
                    if (isLoadStore && current.Arg1 == next.Arg2)
                    {
                        next.Arg2 = current.Arg2; // Swap the pointer to the original source
                        next.RebuildText();

                        instructions.RemoveAt(i); // delete the MOV
                        changed = true;
                        continue;
                    }
                }
            }
        }
    }
}
