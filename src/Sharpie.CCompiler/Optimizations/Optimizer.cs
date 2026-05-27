using Sharpie.CCompiler.Emitter;

namespace Sharpie.CCompiler.Optimizations;

public static class Optimizer
{
    private static readonly HashSet<string> DefMnemonics = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOV",
        "LDI",
        "LDP",
        "LDM",
        "LDS",
        "POP",
        "GETSP",
        "RND",
        "INC",
        "DEC",
        "DINC",
        "DDEC",
        "ADD",
        "SUB",
        "MUL",
        "DIV",
        "MOD",
        "SHL",
        "SHR",
        "AND",
        "OR",
        "XOR",
        "NOT",
        "NEG",
        "IADD",
        "ISUB",
        "IMUL",
        "IDIV",
        "IMOD",
        "IAND",
        "IOR",
        "IXOR",
        "LDV"
    };

    private static readonly HashSet<string> UseMnemonics = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADD",
        "SUB",
        "MUL",
        "DIV",
        "MOD",
        "SHL",
        "SHR",
        "AND",
        "OR",
        "XOR",
        "NOT",
        "NEG",
        "IADD",
        "ISUB",
        "IMUL",
        "IDIV",
        "IMOD",
        "IAND",
        "IOR",
        "IXOR",
        "INC",
        "DEC",
        "DINC",
        "DDEC",
        "STA",
        "STM",
        "STP",
        "STS",
        "CMP",
        "ICMP",
        "PUSH",
        "OUT_R",
        "PRNT",
        "DRAW",
        "CLS",
        "SWC",
        "BANK",
        "PLAY",
        "SONG",
        "CAM",
        "INPUT",
        "COL",
        "OAMTAG",
        "SETOAM",
        "MUTE",
        "SETSP",
        "STV",
        "LDV",
        "BLITMODE"
    };

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

                // XOR rX, rX; MOV rY, rX -> XOR rY, rY (zero propagation)
                if (
                    !current.IsAlt && !next.IsAlt
                    && current.Mnemonic == "XOR" && current.Arg1 == current.Arg2
                    && next.Mnemonic == "MOV" && next.Arg2 == current.Arg1
                    && next.Arg1 != current.Arg1
                )
                {
                    current.Arg1 = next.Arg1;
                    current.Arg2 = next.Arg1;
                    current.RebuildText();
                    instructions.RemoveAt(i + 1);
                    changed = true;
                    break;
                }

                if (
                    !current.IsAlt && !next.IsAlt
                    && current.Mnemonic is "XOR"
                    && current.Arg1 == current.Arg2
                    && current.Arg1 == next.Arg2
                        )
                {
                    // XOR rX, rX; ADD/SUB rY, rX -> remove both
                    if (next.Mnemonic is "ADD" or "SUB")
                    {
                        instructions.RemoveAt(i + 1);
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
                    // XOR rX, rX; MUL rY, rX -> XOR rY, rY
                    else if (next.Mnemonic is "MUL")
                    {
                        next.Mnemonic = "XOR";
                        next.Arg2 = next.Arg1;
                        next.RebuildText();
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
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
                if (!current.IsAlt && current.Mnemonic == "JMP" && next.IsLabel && current.Arg1 == next.OriginalText.TrimEnd(':'))
                {
                    instructions.RemoveAt(i);
                    changed = true;
                    break;
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

                // IAND rX, 255 before ALT STA rX, rY -> remove IAND (byte store truncates)
                if (
                    !current.IsAlt && current.Mnemonic == "IAND" && current.Arg2 == "255"
                    && next.IsAlt && next.Mnemonic == "STA" && current.Arg1 == next.Arg1
                )
                {
                    instructions.RemoveAt(i);
                    changed = true;
                    break;
                }

                // XOR rX, rX; MUL rX, rY -> remove MUL (rX is already 0)
                if (
                    !current.IsAlt && !next.IsAlt
                    && current.Mnemonic == "XOR" && current.Arg1 == current.Arg2
                    && next.Mnemonic == "MUL" && next.Arg1 == current.Arg1
                )
                {
                    instructions.RemoveAt(i + 1);
                    changed = true;
                    break;
                }

                // XOR rX, rX; MUL rY, rX -> replace MUL with XOR rY, rY
                if (
                    !current.IsAlt && !next.IsAlt
                    && current.Mnemonic == "XOR" && current.Arg1 == current.Arg2
                    && next.Mnemonic == "MUL" && next.Arg2 == current.Arg1
                    && next.Arg1 != current.Arg1
                )
                {
                    next.Mnemonic = "XOR";
                    next.Arg2 = next.Arg1;
                    next.RebuildText();
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

                // Wider IADD/ISUB accumulation — scan forward across neutral instructions
                if (
                    !current.IsAlt
                    && current.Mnemonic is "IADD" or "ISUB"
                    && int.TryParse(current.Arg2, out int _)
                )
                {
                    int total = current.Mnemonic == "IADD" ? int.Parse(current.Arg2) : -int.Parse(current.Arg2);
                    var matchIndices = new List<int>();
                    bool blocked = false;

                    for (int scan = 1; i + scan < instructions.Count; scan++)
                    {
                        var mid = instructions[i + scan];

                        if (mid.IsLabel || mid.IsComment || mid.IsDirective)
                            continue;

                        if (
                            !mid.IsAlt
                            && mid.Arg1 == current.Arg1
                        )
                        {
                            if (mid.Mnemonic is "IADD" or "ISUB" && int.TryParse(mid.Arg2, out int midVal))
                            {
                                total += mid.Mnemonic == "IADD" ? midVal : -midVal;
                                matchIndices.Add(i + scan);
                                continue;
                            }
                            if (mid.Mnemonic == "INC")
                            {
                                total++;
                                matchIndices.Add(i + scan);
                                continue;
                            }
                            if (mid.Mnemonic == "DEC")
                            {
                                total--;
                                matchIndices.Add(i + scan);
                                continue;
                            }
                        }

                        var midDefs = GetDefs(mid);
                        var midUses = GetUses(mid);

                        if (midDefs.Contains(current.Arg1) || midUses.Contains(current.Arg1))
                        {
                            if (matchIndices.Count > 0)
                                break;
                            blocked = true;
                            break;
                        }

                        if (mid.Mnemonic is "JMP" or "RET" or "HALT")
                        {
                            if (matchIndices.Count > 0)
                                break;
                            blocked = true;
                            break;
                        }
                    }

                    if (!blocked && matchIndices.Count > 0 && Math.Abs(total) <= 255)
                    {
                        for (int m = matchIndices.Count - 1; m >= 0; m--)
                            instructions.RemoveAt(matchIndices[m]);

                        if (total == 0)
                        {
                            instructions.RemoveAt(i);
                        }
                        else
                        {
                            current.Mnemonic = total > 0 ? "IADD" : "ISUB";
                            current.Arg2 = Math.Abs(total).ToString();
                            current.RebuildText();
                        }

                        changed = true;
                        break;
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

                // LDI rX, N; ADD rY, rX -> IADD rY, N  (remove LDI)
                if (
                    !current.IsAlt && !next.IsAlt
                    && current.Mnemonic == "LDI"
                    && next.Mnemonic == "ADD"
                    && next.Arg2 == current.Arg1
                    && next.Arg1 != current.Arg1
                    && int.TryParse(current.Arg2, out int ldiVal)
                    && ldiVal >= 0 && ldiVal <= 255
                )
                {
                    current.Mnemonic = "IADD";
                    current.Arg1 = next.Arg1;
                    current.RebuildText();
                    instructions.RemoveAt(i + 1);
                    changed = true;
                    break;
                }

                // LDI rX, N; SUB rY, rX -> ISUB rY, N  (remove LDI)
                if (
                    !current.IsAlt && !next.IsAlt
                    && current.Mnemonic == "LDI"
                    && next.Mnemonic == "SUB"
                    && next.Arg2 == current.Arg1
                    && next.Arg1 != current.Arg1
                    && int.TryParse(current.Arg2, out int ldiValSub)
                    && ldiValSub >= 0 && ldiValSub <= 255
                )
                {
                    current.Mnemonic = "ISUB";
                    current.Arg1 = next.Arg1;
                    current.RebuildText();
                    instructions.RemoveAt(i + 1);
                    changed = true;
                    break;
                }

                // LDI rX, N (N<=255); ...neutral...; ADD rY, rX -> ...; IADD rY, N
                // Also handles SUB -> ISUB.  Neutral instructions (e.g. XOR rZ, rZ for 32-bit zero
                // extension) stay in place.  The LDI is removed and the ADD/SUB is transformed into
                // IADD/ISUB so that flag-setting happens at the original ADD/SUB position (important
                // when neutral instructions like XOR clear the carry flag that ALT ADD/SUB uses).
                if (
                    !current.IsAlt && current.Mnemonic == "LDI"
                    && int.TryParse(current.Arg2, out int ldiScanVal)
                    && ldiScanVal >= 0 && ldiScanVal <= 255
                )
                {
                    bool foundTarget = false;
                    for (int scan = 1; i + scan < instructions.Count; scan++)
                    {
                        var mid = instructions[i + scan];
                        if (mid.IsLabel || mid.IsComment || mid.IsDirective)
                            continue;

                        if (!mid.IsAlt && mid.Arg2 == current.Arg1 && mid.Arg1 != current.Arg1)
                        {
                            if (mid.Mnemonic == "ADD")
                            {
                                instructions.RemoveAt(i);              // remove LDI
                                instructions[i + scan - 1].Mnemonic = "IADD";
                                instructions[i + scan - 1].Arg2 = ldiScanVal.ToString();
                                instructions[i + scan - 1].RebuildText();
                                foundTarget = true;
                                break;
                            }
                            if (mid.Mnemonic == "SUB")
                            {
                                instructions.RemoveAt(i);              // remove LDI
                                instructions[i + scan - 1].Mnemonic = "ISUB";
                                instructions[i + scan - 1].Arg2 = ldiScanVal.ToString();
                                instructions[i + scan - 1].RebuildText();
                                foundTarget = true;
                                break;
                            }
                        }

                        var midDefs = GetDefs(mid);
                        var midUses = GetUses(mid);
                        if (midDefs.Contains(current.Arg1) || midUses.Contains(current.Arg1))
                            break;

                        if (mid.Mnemonic is "JMP" or "RET" or "HALT")
                            break;
                    }
                    if (foundTarget) { changed = true; break; }
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

                // MOV rA, rB followed by ADD/SUB/AND/OR/XOR rC, rA -> use rB directly
                // Also remove redundant trailing MOV rB, rA (store-back) when rA==rC
                if (
                    !current.IsAlt && !next.IsAlt
                    && current.Mnemonic == "MOV"
                    && next.Mnemonic is "ADD" or "SUB" or "AND" or "OR" or "XOR"
                )
                {
                    if (current.Arg1 == next.Arg1)
                    {
                        next.Arg1 = current.Arg2;
                        next.RebuildText();

                        if (
                            i + 2 < instructions.Count
                            && !instructions[i + 2].IsLabel && !instructions[i + 2].IsAlt
                            && instructions[i + 2].Mnemonic == "MOV"
                            && instructions[i + 2].Arg1 == current.Arg2
                            && instructions[i + 2].Arg2 == current.Arg1
                        )
                        {
                            instructions.RemoveAt(i + 2);
                        }

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
&& int.TryParse(current.Arg2, out int v1)
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
                        instructions.RemoveAt(i);
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
                    else if (current.Mnemonic == "INC" && next.Mnemonic == "INC")
                    {
                        current.Mnemonic = "IADD";
                        current.Arg2 = "2";
                        current.RebuildText();
                        instructions.RemoveAt(i + 1);
                        changed = true;
                        break;
                    }
                    else if (current.Mnemonic == "DEC" && next.Mnemonic == "DEC")
                    {
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

                // MOV rTemp, rSrc -> LDP/STA/LDS/STS rX, rTemp ==> use rSrc directly
                // but only when rTemp is not read again before being redefined.
                if (!current.IsAlt && current.Mnemonic == "MOV")
                {
                    var isLoadStore = next.Mnemonic is "LDP" or "STA" or "LDS" or "STS";
                    if (isLoadStore && current.Arg1 == next.Arg2)
                    {
                        if (IsRegisterUsedBeforeRedefined(instructions, i + 2, current.Arg1))
                            continue;

                        next.Arg2 = current.Arg2;
                        next.RebuildText();
                        instructions.RemoveAt(i);
                        changed = true;
                        continue;
                    }
                }

                // MOV rA, rB -> ... -> MOV rC, rA  =>  MOV rC, rB
                if (!current.IsAlt && current.Mnemonic == "MOV" && current.Arg1.StartsWith('r') && current.Arg2.StartsWith('r'))
                {
                    bool sawLabel = false;
                    for (int scan = 1; i + scan < instructions.Count; scan++)
                    {
                        var mid = instructions[i + scan];

                        if (mid.IsLabel)
                        {
                            sawLabel = true;
                            continue;
                        }

                        if (mid.IsComment || mid.IsDirective)
                            continue;

                        if (!mid.IsAlt && mid.Mnemonic == "MOV" && mid.Arg2 == current.Arg1)
                        {
                            // Don't propagate across label boundaries when the source
                            // register is volatile across calls (r0–r7). The label may be
                            // a loop header — rB gets clobbered by CALLs in the loop body
                            // and won't equal rA on the next iteration.
                            if (sawLabel && IsCallVolatile(current.Arg2))
                                break;

                            // Don't substitute if the target writes to the source register
                            // (e.g. MOV rA, rB; MOV rB, rA — swap that also depends on rA's value)
                            if (mid.Arg1 == current.Arg2)
                                break;

                            mid.Arg2 = current.Arg2;
                            mid.RebuildText();
                            changed = true;
                            break;
                        }

                        var midDefs = GetDefs(mid);
                        if (midDefs.Contains(current.Arg1) || midDefs.Contains(current.Arg2))
                            break;

                        if (mid.Mnemonic is "JMP" or "RET" or "HALT")
                            break;
                    }

                    if (changed)
                        break;
                }

                // JMP to the next line => Remove the JMP
                if (!current.IsAlt && current.Mnemonic == "JMP" && next.IsLabel && current.Arg1 == next.OriginalText.Trim(':'))
                {
                    instructions.RemoveAt(i);
                    changed = true;
                    break;
                }

                // PUSH rX -> ... (no use/def of rX) -> POP rX  =>  remove both
                if (current.Mnemonic == "PUSH")
                {
                    int popScan = -1;

                    for (int scan = 1; i + scan < instructions.Count; scan++)
                    {
                        var mid = instructions[i + scan];

                        if (mid.IsLabel || mid.IsComment || mid.IsDirective)
                            continue;

                        if (!mid.IsAlt && mid.Mnemonic == "POP" && mid.Arg1 == current.Arg1)
                        {
                            popScan = scan;
                            break;
                        }

                        var midDefs = GetDefs(mid);
                        var midUses = GetUses(mid);

                        if (midDefs.Contains(current.Arg1) || midUses.Contains(current.Arg1))
                            break;

                        if (mid.Mnemonic is "JMP" or "RET" or "HALT")
                            break;
                    }

                    if (popScan > 0)
                    {
                        instructions.RemoveAt(i + popScan);
                        instructions.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }
        }
    }

    public static void EliminateUnreachableBlocks(
        ControlFlowGraph cfg,
        List<string>? readOnlyData = null
    )
    {
        var addressTakenLabels = new HashSet<string>();
        if (readOnlyData != null)
        {
            foreach (var line in readOnlyData)
            {
                foreach (var token in line.Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries))
                {
                    addressTakenLabels.Add(token);
                }
            }
        }

        bool changed = true;

        while (changed)
        {
            changed = false;

            for (int i = 1; i < cfg.Blocks.Count; i++)
            {
                var block = cfg.Blocks[i];

                if (block.Predecessors.Count == 0 && !addressTakenLabels.Contains(block.Name))
                {
                    foreach (var successor in block.Successors)
                    {
                        successor.Predecessors.Remove(block);
                    }

                    cfg.Blocks.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }
    }

    private static HashSet<string> GetDefs(Instruction inst)
    {
        var defs = new HashSet<string>();

        if (DefMnemonics.Contains(inst.Mnemonic))
        {
            if (!string.IsNullOrEmpty(inst.Arg1) && inst.Arg1.StartsWith('r'))
                defs.Add(inst.Arg1);
        }
        else if (inst.Mnemonic == "CALL")
        {
            defs.Add("r0"); // return
            defs.Add("r1");
            defs.Add("r2");
            defs.Add("r3");
            defs.Add("r4"); // args
            defs.Add("r5");
            defs.Add("r6");
            defs.Add("r7"); // temps
        }

        return defs;
    }

    private static HashSet<string> GetUses(Instruction inst)
    {
        var uses = new HashSet<string>();

        if (!string.IsNullOrEmpty(inst.Arg2) && inst.Arg2.StartsWith('r'))
            uses.Add(inst.Arg2);
        if (!string.IsNullOrEmpty(inst.Arg3) && inst.Arg3.StartsWith('r'))
            uses.Add(inst.Arg3);
        if (!string.IsNullOrEmpty(inst.Arg4) && inst.Arg4.StartsWith('r'))
            uses.Add(inst.Arg4);

        // arg 1 is used as a source in math, stores, compares, pushes etc
        if (ReadsFirstArg(inst) && !string.IsNullOrEmpty(inst.Arg1) && inst.Arg1.StartsWith('r'))
        {
            uses.Add(inst.Arg1);
        }

        // CALL uses argument registers (r1-r4)
        if (inst.Mnemonic == "CALL")
        {
            uses.Add("r1");
            uses.Add("r2");
            uses.Add("r3");
            uses.Add("r4");

            if (inst.Arg1 == "SYS_FAR_CALL")
            {
                uses.Add("r13");
                uses.Add("r14");
            }

            // indirect calls
            if (!string.IsNullOrEmpty(inst.Arg1) && inst.Arg1.StartsWith('r'))
            {
                uses.Add(inst.Arg1);
            }
        }

        if (inst.Mnemonic is "RET" or "HALT")
        {
            uses.Add("r0");
        }

        return uses;
    }

    private static bool ReadsFirstArg(Instruction inst)
    {
        return UseMnemonics.Contains(inst.Mnemonic)
            || (inst.Mnemonic.StartsWith('J') && inst.Mnemonic.Length == 3);
    }

    private static bool IsCallVolatile(string register)
    {
        return register is "r0" or "r1" or "r2" or "r3" or "r4" or "r5" or "r6" or "r7";
    }

    private static bool IsRegisterUsedBeforeRedefined(
        List<Instruction> instructions,
        int startIndex,
        string register
    )
    {
        for (int i = startIndex; i < instructions.Count; i++)
        {
            var inst = instructions[i];
            if (inst.IsDirective || inst.IsComment)
                continue;

            var uses = GetUses(inst);
            if (uses.Contains(register))
                return true;

            var defs = GetDefs(inst);
            if (defs.Contains(register))
                return false;
        }

        return false;
    }

    public static Dictionary<BasicBlock, HashSet<string>> ComputeLiveOut(ControlFlowGraph cfg)
    {
        var liveIn = new Dictionary<BasicBlock, HashSet<string>>();
        var liveOut = new Dictionary<BasicBlock, HashSet<string>>();
        var def = new Dictionary<BasicBlock, HashSet<string>>();
        var use = new Dictionary<BasicBlock, HashSet<string>>();

        foreach (var block in cfg.Blocks)
        {
            liveIn[block] = [];
            liveOut[block] = [];
            def[block] = [];
            use[block] = [];

            foreach (var inst in block.Instructions)
            {
                var instUses = GetUses(inst);
                var instDefs = GetDefs(inst);

                // If it's used before being defined in this block add to use
                foreach (var u in instUses)
                {
                    if (!def[block].Contains(u))
                        use[block].Add(u);
                }

                // If defined add to def
                foreach (var d in instDefs)
                {
                    def[block].Add(d);
                }
            }
        }

        bool changed = true;
        while (changed)
        {
            changed = false;

            for (int i = cfg.Blocks.Count - 1; i >= 0; i--)
            {
                var block = cfg.Blocks[i];

                // LiveOut is the union of LiveIn of all successors
                var newLiveOut = new HashSet<string>();
                foreach (var succ in block.Successors)
                {
                    newLiveOut.UnionWith(liveIn[succ]);
                }

                if (!liveOut[block].SetEquals(newLiveOut))
                {
                    liveOut[block] = newLiveOut;
                    changed = true;
                }

                var newLiveIn = new HashSet<string>(liveOut[block]);
                newLiveIn.ExceptWith(def[block]);
                newLiveIn.UnionWith(use[block]);

                if (!liveIn[block].SetEquals(newLiveIn))
                {
                    liveIn[block] = newLiveIn;
                    changed = true;
                }
            }
        }

        return liveOut;
    }

    public static void EliminateDeadStores(ControlFlowGraph cfg)
    {
        var blockLiveOut = ComputeLiveOut(cfg);

        foreach (var block in cfg.Blocks)
        {
            // the registers needed by successor blocks
            var live = new HashSet<string>(blockLiveOut[block]);

            for (int i = block.Instructions.Count - 1; i >= 0; i--)
            {
                var inst = block.Instructions[i];

                if (inst.IsLabel || inst.IsDirective || inst.IsComment)
                    continue;

                var defs = GetDefs(inst);
                var uses = GetUses(inst);

                bool hasSideEffects =
                    inst.Mnemonic
                        is "CALL"
                            or "RET"
                            or "STA"
                            or "STM"
                            or "STS"
                            or "STP"
                            or "STV"
                            or "ALT"
                            or "HALT"
                            or "PUSH"
                            or "POP"
                            or "SETSP"
                            or "DINC"
                            or "DDEC"
                            or "BLITMODE"
                    || (inst.IsAlt && inst.Mnemonic.StartsWith('I') && inst.Mnemonic.Length == 4);

                if (defs.Count > 0 && !hasSideEffects)
                {
                    bool isAnyDefLive = false;
                    foreach (var d in defs)
                    {
                        if (live.Contains(d))
                            isAnyDefLive = true;
                    }

                    if (!isAnyDefLive)
                    {
                        // The register this writes to is never read again, so we nuke it from orbit
                        block.Instructions.RemoveAt(i);
                        continue;
                    }
                }

                live.ExceptWith(defs);
                live.UnionWith(uses);
            }
        }
    }

    public static void EliminateDeadStackStores(ControlFlowGraph cfg)
    {
        foreach (var block in cfg.Blocks)
        {
            var regToOffset = new Dictionary<string, int>();
            var prevStaIdx = new Dictionary<int, int>();
            var toRemove = new HashSet<int>();

            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var inst = block.Instructions[i];
                if (inst.IsLabel || inst.IsComment || inst.IsDirective)
                    continue;

                var defs = GetDefs(inst);
                foreach (var d in defs)
                {
                    if (d.StartsWith('r') && regToOffset.ContainsKey(d) && inst.Mnemonic != "MOV" && inst.Mnemonic is not ("IADD" or "ISUB"))
                    {
                        regToOffset.Remove(d);
                    }
                }

                if (defs.Contains("r15"))
                    regToOffset["r15"] = 0;

                if (!inst.IsAlt && inst.Mnemonic == "MOV" && inst.Arg2 == "r15")
                {
                    regToOffset[inst.Arg1] = 0;
                }
                else if (!inst.IsAlt && inst.Mnemonic is "IADD" or "ISUB"
                         && regToOffset.ContainsKey(inst.Arg1)
                         && int.TryParse(inst.Arg2, out int iaddVal))
                {
                    if (inst.Mnemonic == "IADD")
                        regToOffset[inst.Arg1] += iaddVal;
                    else
                        regToOffset[inst.Arg1] -= iaddVal;
                }
                else if (!inst.IsAlt && inst.Mnemonic == "MOV"
                         && regToOffset.TryGetValue(inst.Arg2, out int srcOff))
                {
                    regToOffset[inst.Arg1] = srcOff;
                }

                // LDP from a known stack offset invalidates any STA before it
                if (!inst.IsAlt && inst.Mnemonic == "LDP"
                    && regToOffset.TryGetValue(inst.Arg2, out int ldpOff))
                {
                    prevStaIdx.Remove(ldpOff);
                }

                // STA to a known stack offset: previous STA to the same offset is dead
                if (!inst.IsAlt && inst.Mnemonic == "STA"
                    && regToOffset.TryGetValue(inst.Arg2, out int staOff))
                {
                    if (prevStaIdx.TryGetValue(staOff, out int prev))
                        toRemove.Add(prev);
                    prevStaIdx[staOff] = i;
                }
            }

            foreach (var idx in toRemove.OrderByDescending(i => i))
                block.Instructions.RemoveAt(idx);
        }
    }

    public static void CseStackAddresses(ControlFlowGraph cfg)
    {
        foreach (var block in cfg.Blocks)
        {
            var offsetRegs = new Dictionary<int, (string Reg, int AtIndex)>();
            var invalidAfter = new Dictionary<string, int>();
            var toRemove = new HashSet<int>();

            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var inst = block.Instructions[i];
                if (inst.IsLabel || inst.IsComment || inst.IsDirective)
                    continue;

                foreach (var d in GetDefs(inst))
                {
                    invalidAfter[d] = i;
                    if (d == "r15")
                        offsetRegs.Clear();
                }

                if (!inst.IsAlt && inst.Mnemonic == "MOV" && inst.Arg2 == "r15")
                {
                    string dest = inst.Arg1;
                    if (i + 1 < block.Instructions.Count
                        && !block.Instructions[i + 1].IsAlt
                        && block.Instructions[i + 1].Mnemonic is "IADD" or "ISUB"
                        && block.Instructions[i + 1].Arg1 == dest
                        && int.TryParse(block.Instructions[i + 1].Arg2, out int N))
                    {
                        int offset = block.Instructions[i + 1].Mnemonic == "IADD" ? N : -N;

                        if (offsetRegs.TryGetValue(offset, out var cached)
                            && cached.Reg != dest
                            && (!invalidAfter.ContainsKey(cached.Reg)
                                || invalidAfter[cached.Reg] <= cached.AtIndex + 1))
                        {
                            inst.Arg2 = cached.Reg;
                            inst.RebuildText();
                            toRemove.Add(i + 1);
                            offsetRegs[offset] = (dest, i);
                        }
                        else
                        {
                            offsetRegs[offset] = (dest, i);
                        }
                    }
                }
            }

            foreach (var idx in toRemove.OrderByDescending(i => i))
                block.Instructions.RemoveAt(idx);
        }
    }
}

