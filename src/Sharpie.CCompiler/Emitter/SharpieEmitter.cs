using System.Text;
using System.Text.RegularExpressions;
using ClangSharp.Interop;
using Sharpie.CCompiler.Emitter;
using Sharpie.CCompiler.Optimizations;

namespace Sharpie.CCompiler;

public sealed partial class SharpieEmitter
{
    public const int TempRegisterStart = 1;
    public const int TempRegisterEnd = 7;
    public const int LocalRegisterStart = 8;
    public const int LocalRegisterEnd = 14;

    public const int FramePointer = 15;

    private readonly bool _optimize;
    private readonly bool _allowLong;

    [GeneratedRegex(@"#pragma\s+bank\s+(\d+)")]
    private static partial Regex MyRegex();

    public SharpieEmitter(bool optimizationsEnabled, bool allowLong = false)
    {
        _optimize = optimizationsEnabled;
        _allowLong = allowLong;
    }

    public string EmitTranslationUnit(CXCursor translationUnitCursor)
    {
        var asm = new StringBuilder();
        var roData = new List<string>();
        var stringPool = new Dictionary<string, string>();

        var regionName = "FIXED";

        unsafe
        {
            var tuCursor = clang.Cursor_getTranslationUnit(translationUnitCursor);
            var tuSpelling = clang.getTranslationUnitSpelling(tuCursor).ToString();

            if (File.Exists(tuSpelling))
            {
                var srcText = File.ReadAllText(tuSpelling);
                var match = MyRegex().Match(srcText);
                if (match.Success)
                {
                    regionName = $"BANK_{match.Groups[1].Value}";
                }
            }
            asm.AppendLine($".REGION {regionName}");
        }

        if (!_allowLong)
            CheckLongTypeUsage(translationUnitCursor);

        var globalNames = new HashSet<string>(StringComparer.Ordinal);

        var globalVars = GetChildren(translationUnitCursor)
            .Where(c => c.Kind == CXCursorKind.CXCursor_VarDecl)
            .ToList();

        HandleGlobals(asm, globalNames, globalVars, roData, stringPool);

        var topLevelAsm = new List<CXCursor>();
        foreach (var child in GetChildren(translationUnitCursor))
        {
            if (child.Kind == CXCursorKind.CXCursor_AsmStmt)
            {
                topLevelAsm.Add(child);
            }
            else if (child.Kind == CXCursorKind.CXCursor_UnexposedDecl)
            {
                unsafe
                {
                    var range = clang.getCursorExtent(child);
                    var tu = clang.Cursor_getTranslationUnit(child);
                    uint numTokens = 0;
                    CXToken* tokens = null;

                    clang.tokenize(tu, range, &tokens, &numTokens);

                    if (numTokens > 0)
                    {
                        var cxString = clang.getTokenSpelling(tu, tokens[0]);
                        var firstToken = cxString.ToString();
                        clang.disposeString(cxString);

                        if (firstToken == "asm" || firstToken == "__asm__" || firstToken == "__asm")
                        {
                            topLevelAsm.Add(child);
                        }
                    }
                    clang.disposeTokens(tu, tokens, numTokens);
                }
            }
        }

        if (topLevelAsm.Count > 0)
        {
            foreach (var asmStmt in topLevelAsm)
            {
                ParseAndEmitAsmString(asmStmt, line => asm.AppendLine(line));
            }
        }

        var functions = GetChildren(translationUnitCursor)
            .Where(c => c.Kind == CXCursorKind.CXCursor_FunctionDecl)
            .ToList();

        var mainFunctions = functions.Where(func => func.Spelling.ToString() == "main").ToList();
        if (mainFunctions.Count > 1)
        {
            throw new InvalidOperationException(
                "Ambiguous entrypoint: more than one 'main' function found."
            );
        }

        var orderedFunctions = new List<CXCursor>();

        if (mainFunctions.Count == 1)
            orderedFunctions.Add(mainFunctions[0]);

        orderedFunctions.AddRange(functions.Where(func => func.Spelling.ToString() != "main"));

        foreach (var func in orderedFunctions)
        {
            var hasBody = GetChildren(func).Any(c => c.Kind == CXCursorKind.CXCursor_CompoundStmt);
            if (!hasBody)
                continue; // skip prototypes entirely

            var linkage = clang.getCursorLinkage(func);
            var isStatic = linkage == CXLinkageKind.CXLinkage_Internal; // static methods in C are file-scoped so we just don't emit .GLOBAL
            if (!isStatic)
                asm.AppendLine(".GLOBAL");

            var funcName = func.Spelling.ToString();
            if (funcName.StartsWith("__sharpie_"))
            {
                throw new InvalidOperationException(
                    $"Cannot define function '{funcName}'. Identifiers beginning with '__sharpie_' are reserved for hardware intrinsics."
                );
            }

            var body = GetChildren(func).First(c => c.Kind == CXCursorKind.CXCursor_CompoundStmt);

            // because I can't be bothered to make this a two-pass compiler,
            // we're just gonna have to emit the body, scan for variables that need to be spilled,
            // then stitch the prologue and epilogue after the fact.
            var escapedVars = DetectEscapingVariables(func);
            var context = new EmissionContext(escapedVars, roData, stringPool, globalNames)
            {
                IsMain = funcName == "main",
            };

            asm.AppendLine($"{(context.IsMain ? "Main" : $"_func_{funcName}")}:");

            var retSizeBytes = func.ResultType.SizeOf;
            if (retSizeBytes > 2)
            {
                var hiddenReturn = context.AllocateStorage("__hidden_ret", false, 2);
                context.HiddenRetPtrReg = hiddenReturn.Value;
                context.Emit($"MOV r{hiddenReturn.Value}, r1");
            }

            var usageCounts = GetVariableUsage(func);
            var methodVars = GetAllLocalDeclarations(func)
                .OrderByDescending(v => usageCounts.GetValueOrDefault(v.Spelling.ToString(), 0))
                .ToList();

            foreach (var v in methodVars)
            {
                var varName = v.Spelling.ToString();
                if (string.IsNullOrWhiteSpace(varName))
                    continue;

                var typeKind = v.Type.CanonicalType.kind;
                long sizeBytes = v.Type.SizeOf <= 0 ? 2 : v.Type.SizeOf;

                bool isRecord = typeKind is CXTypeKind.CXType_Record;
                bool isArray =
                    typeKind
                    is CXTypeKind.CXType_ConstantArray
                        or CXTypeKind.CXType_IncompleteArray;
                bool needsStack = isRecord || isArray || context.EscapedVariables.Contains(varName);

                context.AllocateStorage(varName, needsStack, (int)sizeBytes);
            }

            var parameters = GetChildren(func)
                .Where(c => c.Kind == CXCursorKind.CXCursor_ParmDecl)
                .ToList();

            var currentReg = retSizeBytes > 2 ? 2 : 1;
            var currentStackArgOffset = 0;

            for (var i = 0; i < parameters.Count; i++)
            {
                var paramDecl = parameters[i];
                var paramName = paramDecl.Spelling.ToString();

                var typeKind = paramDecl.Type.CanonicalType.kind;
                bool isRecord = typeKind == CXTypeKind.CXType_Record;
                var sizeBytes = paramDecl.Type.SizeOf;

                if (sizeBytes <= 0)
                    sizeBytes = 2; // Fallback for void*/unresolved
                var slotsNeeded = GetRegistersNeededForVariable(paramDecl.Type);

                var needsStack = isRecord || context.EscapedVariables.Contains(paramName);
                var space = context.AllocateStorage(paramName, needsStack, (int)sizeBytes);

                if (currentReg + slotsNeeded - 1 <= 4)
                {
                    // It fits in r1-r4
                    if (slotsNeeded == 1)
                    {
                        if (space.Type == StorageType.Register)
                        {
                            context.Emit($"MOV r{space.Value}, r{currentReg}");
                        }
                        else
                        {
                            context.Emit("MOV r6, r15");
                            AccumulateOffset(6, space.Value, context);
                            context.Emit($"STA r{currentReg}, r6");
                        }
                    }
                    else
                    {
                        // A multi-word struct was passed in registers
                        // We must reconstruct it in its local stack home
                        context.Emit("MOV r6, r15");
                        AccumulateOffset(6, space.Value, context);

                        for (int s = 0; s < slotsNeeded; s++)
                        {
                            context.Emit($"STA r{currentReg + s}, r6");
                            if (s < slotsNeeded - 1)
                                context.Emit("IADD r6, 2");
                        }
                    }
                    currentReg += slotsNeeded;
                }
                else
                {
                    // It was pushed to the stack so we record its byte offset and slot count
                    context.PendingStackArguments.Add((currentStackArgOffset, space, slotsNeeded));
                    currentStackArgOffset += slotsNeeded * 2;
                }
            }
            EmitFunctionBody(body, context);

            // If the function reached the end without a return,
            // emit an implicit one (HALT for main, RET for anything else)
            if (!context.HasReturn && context.IsMain)
            {
                context.Emit("LDI r0, 0");
            }

            if (_optimize)
            {
                Optimizer.Optimize(context.Instructions);
                context.UsedPreservedRegisters.RemoveAll(reg =>
                    !context.Instructions.Any(inst =>
                        inst.Arg1 == $"r{reg}" || inst.Arg2 == $"r{reg}"
                    )
                );
            }

            context.Emit($"{context.EpilogueLabel}:");
            foreach (var line in context.GetEpilogue())
                context.Emit(line);

            context.Emit(context.ReturnInstruction);

            context.Instructions.InsertRange(0, context.GetPrologue().Select(Instruction.Parse));

            if (_optimize) // second optimization pass to nuke dead code that survived the first (like redundant PUSH/POP shenanigans)
            {
                var cfg = ControlFlowGraph.Build(context.Instructions);

                Optimizer.EliminateUnreachableBlocks(cfg, roData);
                Optimizer.EliminateDeadStores(cfg);
                context.Instructions = cfg.Flatten();
                Optimizer.Optimize(context.Instructions);
            }

            foreach (var inst in context.Instructions)
            {
                if (inst.IsLabel || inst.IsComment)
                    asm.AppendLine(inst.OriginalText);
                else
                    asm.AppendLine($"    {inst.OriginalText}");
            }

            // EmitReturn is responsible for the epilogue
            if (!isStatic)
                asm.AppendLine(".ENDGLOBAL");
        }

        if (roData.Count > 0)
        {
            asm.AppendLine("");
            asm.AppendLine("; Readonly Data");
            asm.AppendLine(".GLOBAL");
            foreach (var dataLine in roData)
                asm.AppendLine(dataLine);
            asm.AppendLine(".ENDGLOBAL");
        }

        asm.AppendLine(".ENDREGION");
        return asm.ToString();
    }

    private static void HandleGlobals(
        StringBuilder asm,
        HashSet<string> globalNames,
        List<CXCursor> globalVars,
        List<string> readOnlyData,
        Dictionary<string, string> stringPool
    )
    {
        if (globalVars.Count > 0)
        {
            asm.AppendLine("; Global Variables");
            foreach (var global in globalVars)
            {
                var name = global.Spelling.ToString();

                // Skip const-qualified scalar globals. clang evaluates all value references
                // at compile time, so the global storage is never
                // accessed. This avoids emitting globals for constants which are never referenced, thus saving space.
                if (global.Type.IsConstQualified && !IsAggregateType(global.Type))
                    continue;

                globalNames.Add(name);

                var linkage = clang.getCursorLinkage(global);
                bool isStatic = linkage == CXLinkageKind.CXLinkage_Internal;
                long sizeBytes = global.Type.SizeOf <= 0 ? 2 : global.Type.SizeOf;

                if (!isStatic)
                    asm.AppendLine(".GLOBAL");
                asm.AppendLine($"_global_{name}:");

                var children = GetChildren(global);

                // grab lists, not defaults
                var initListExprs = children
                    .Where(c => c.Kind == CXCursorKind.CXCursor_InitListExpr)
                    .ToList();

                var normalExprs = children
                    .Where(c =>
                        c.Kind >= CXCursorKind.CXCursor_FirstExpr
                        && c.Kind <= CXCursorKind.CXCursor_LastExpr
                        && c.Kind != CXCursorKind.CXCursor_InitListExpr
                    )
                    .ToList();

                long bytesWritten = 0;

                // Handle arrays and structs
                if (initListExprs.Count > 0)
                {
                    var initVals = GetChildren(initListExprs[^1]);
                    var typeKind = global.Type.CanonicalType.kind;

                    if (
                        typeKind == CXTypeKind.CXType_ConstantArray
                        || typeKind == CXTypeKind.CXType_IncompleteArray
                    )
                    {
                        var elementType = clang.getElementType(global.Type);
                        long stride = elementType.SizeOf <= 0 ? 2 : elementType.SizeOf;
                        var elementCanonical = elementType.CanonicalType;
                        bool elementIsRecord =
                            elementType.kind == CXTypeKind.CXType_Record
                            || elementCanonical.kind == CXTypeKind.CXType_Record;
                        bool elementIsArray =
                            elementType.kind == CXTypeKind.CXType_ConstantArray
                            || elementType.kind == CXTypeKind.CXType_IncompleteArray;

                        if (elementIsRecord)
                        {
                            var decl = clang.getTypeDeclaration(elementCanonical);
                            var fields = GetChildren(decl)
                                .Where(c => c.Kind == CXCursorKind.CXCursor_FieldDecl)
                                .ToList();

                            foreach (var val in initVals)
                            {
                                long elementBytesWritten = 0;
                                var fieldInitVals = GetAggregateInitializerValues(val);

                                for (int i = 0; i < fields.Count; i++)
                                {
                                    long fieldOffset = clang.Cursor_getOffsetOfField(fields[i]) / 8;
                                    long fieldSize = fields[i].Type.SizeOf <= 0 ? 2 : fields[i].Type.SizeOf;

                                    var gap = fieldOffset - elementBytesWritten;
                                    if (gap > 0)
                                    {
                                        asm.AppendLine($"    .PAD {gap}");
                                        elementBytesWritten += gap;
                                    }

                                    long value = 0;
                                    if (i < fieldInitVals.Count)
                                        value = PeelExpression(fieldInitVals[i]).Evaluate.AsLongLong;

                                    asm.AppendLine(fieldSize == 1 ? $"    .DB {value}" : $"    .DW {value}");
                                    elementBytesWritten += fieldSize;
                                }

                                var tailPadding = stride - elementBytesWritten;
                                if (tailPadding > 0)
                                {
                                    asm.AppendLine($"    .PAD {tailPadding}");
                                    elementBytesWritten += tailPadding;
                                }

                                bytesWritten += elementBytesWritten;
                            }
                        }
                        else if (elementIsArray)
                        {
                            // Handle global arrays of arrays (multidimensional arrays).
                            var innerElementType = clang.getElementType(elementType);
                            long innerStride = innerElementType.SizeOf <= 0 ? 2 : innerElementType.SizeOf;

                            foreach (var rowVal in initVals)
                            {
                                foreach (var colVal in GetAggregateInitializerValues(rowVal))
                                {
                                    long v = PeelExpression(colVal).Evaluate.AsLongLong;
                                    asm.AppendLine(innerStride == 1 ? $"    .DB {v}" : $"    .DW {v}");
                                    bytesWritten += innerStride;
                                }
                            }
                        }
                        else
                        {
                            foreach (var val in initVals)
                            {
                                long v = PeelExpression(val).Evaluate.AsLongLong;
                                asm.AppendLine(stride == 1 ? $"    .DB {v}" : $"    .DW {v}");
                                bytesWritten += stride;
                            }
                        }
                    }
                    else // Structs
                    {
                        var decl = clang.getTypeDeclaration(global.Type.CanonicalType);
                        var fields = GetChildren(decl)
                            .Where(c => c.Kind == CXCursorKind.CXCursor_FieldDecl)
                            .ToList();

                        for (int i = 0; i < initVals.Count && i < fields.Count; i++)
                        {
                            long fieldSize = fields[i].Type.SizeOf;
                            long v = PeelExpression(initVals[i]).Evaluate.AsLongLong;
                            asm.AppendLine(fieldSize == 1 ? $"    .DB {v}" : $"    .DW {v}");
                            bytesWritten += fieldSize;
                        }
                    }
                }
                // Handle primitives and string literals safely
                else if (normalExprs.Count > 0)
                {
                    var initExpr = PeelExpression(normalExprs[^1]);

                    if (initExpr.Kind == CXCursorKind.CXCursor_StringLiteral)
                    {
                        string rawString = "";
                        unsafe
                        {
                            var range = clang.getCursorExtent(initExpr);
                            var tu = clang.Cursor_getTranslationUnit(initExpr);
                            uint numTokens = 0;
                            CXToken* tokens = null;

                            clang.tokenize(tu, range, &tokens, &numTokens);
                            if (numTokens > 0)
                            {
                                var cxString = clang.getTokenSpelling(tu, tokens[0]);
                                rawString = cxString.ToString();
                                clang.disposeString(cxString);
                            }
                            clang.disposeTokens(tu, tokens, numTokens);
                        }

                        var typeKind = global.Type.CanonicalType.kind;

                        // Array: char my_string[] = "Hello";
                        if (
                            typeKind == CXTypeKind.CXType_ConstantArray
                            || typeKind == CXTypeKind.CXType_IncompleteArray
                        )
                        {
                            // Dump directly into the global memory block
                            asm.AppendLine($"    .DB {rawString}, 0");

                            bytesWritten += initExpr.Type.SizeOf;
                        }
                        // Pointer: const char* my_string = "Hello";
                        else
                        {
                            if (!stringPool.TryGetValue(rawString, out var existingLabel))
                            {
                                existingLabel = EmissionContext.GenerateLabel("str");
                                stringPool[rawString] = existingLabel;

                                readOnlyData.Add($"{existingLabel}:");
                                readOnlyData.Add($"    .DB {rawString}, 0");
                            }
                            // Just save the 16-bit address pointing to the read-only string
                            asm.AppendLine($"    .DW {existingLabel}");
                            bytesWritten += 2;
                        }
                    }
                    // --- STANDARD NUMBERS ---
                    else
                    {
                        long v = initExpr.Evaluate.AsLongLong;
                        if (sizeBytes <= 2)
                        {
                            asm.AppendLine(sizeBytes == 1 ? $"    .DB {v}" : $"    .DW {v}");
                            bytesWritten += sizeBytes;
                        }
                        else
                        {
                            int wordCount = (int)((sizeBytes + 1) / 2);
                            for (int w = 0; w < wordCount; w++)
                            {
                                asm.AppendLine($"    .DW {unchecked((ushort)(v & 0xFFFF))}");
                                bytesWritten += 2;
                                v >>= 16;
                            }
                        }
                    }
                }

                // Zero-pad any uninitialized space (e.g. `int x;` or partially filled arrays)
                var diff = sizeBytes - bytesWritten;
                if (diff > 0)
                {
                    asm.AppendLine($"    .PAD {diff}");
                    bytesWritten += diff;
                }

                if (!isStatic)
                    asm.AppendLine(".ENDGLOBAL");
            }
        }
    }

    private static void EmitFunctionBody(CXCursor compoundStmt, EmissionContext context)
    {
        foreach (var stmt in GetChildren(compoundStmt))
        {
            EmitStatement(stmt, context);
        }
    }

    private static List<CXCursor> GetAllLocalDeclarations(CXCursor functionCursor)
    {
        var decls = new List<CXCursor>();
        var queue = new Queue<CXCursor>();
        queue.Enqueue(functionCursor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (
                current.Kind == CXCursorKind.CXCursor_ParmDecl
                || current.Kind == CXCursorKind.CXCursor_VarDecl
            )
            {
                decls.Add(current);
            }

            foreach (var child in GetChildren(current))
                queue.Enqueue(child);
        }
        return decls;
    }

    private static Dictionary<string, int> GetVariableUsage(CXCursor functionCursor)
    {
        var usage = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<CXCursor>();
        queue.Enqueue(functionCursor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Kind == CXCursorKind.CXCursor_DeclRefExpr)
            {
                var name = current.Spelling.ToString();
                usage[name] = usage.TryGetValue(name, out int count) ? count + 1 : 1;
            }

            foreach (var child in GetChildren(current))
                queue.Enqueue(child);
        }
        return usage;
    }

    private static bool IsLongType(CXType type)
    {
        if (type.SizeOf <= 2) return false;
        var kind = type.CanonicalType.kind;
        return kind is CXTypeKind.CXType_Long
            or CXTypeKind.CXType_ULong
            or CXTypeKind.CXType_LongLong
            or CXTypeKind.CXType_ULongLong;
    }

    private static void CheckLongTypeUsage(CXCursor rootCursor)
    {
        var queue = new Queue<CXCursor>();
        queue.Enqueue(rootCursor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (IsLongType(current.Type))
            {
                var name = current.Spelling.ToString();
                var kind = current.Kind.ToString();
                throw new InvalidOperationException(
                    "Compilation with 32-bit integers (long) is not allowed by default.\n"
                    + "Use --allow-long to enable 32-bit operations.\n"
                    + $"Found: {kind} '{(string.IsNullOrEmpty(name) ? "(anonymous)" : name)}' with type '{current.Type.Spelling}'."
                );
            }

            if (current.Kind == CXCursorKind.CXCursor_FunctionDecl && IsLongType(current.ResultType))
            {
                var name = current.Spelling.ToString();
                throw new InvalidOperationException(
                    "Compilation with 32-bit integers (long) is not allowed by default.\n"
                    + "Use --allow-long to enable 32-bit operations.\n"
                    + $"Found: function '{name}' returns 'long'."
                );
            }

            foreach (var child in GetChildren(current))
                queue.Enqueue(child);
        }
    }
}
