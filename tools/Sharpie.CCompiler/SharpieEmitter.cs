using System.Text;
using System.Text.RegularExpressions;
using ClangSharp.Interop;
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

    public SharpieEmitter(bool optimizationsEnabled)
    {
        _optimize = optimizationsEnabled;
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
                var match = Regex.Match(srcText, @"#pragma\s+bank\s+(\d+)");
                if (match.Success)
                {
                    regionName = $"BANK_{match.Groups[1].Value}";
                }
            }
            asm.AppendLine($".REGION {regionName}");
        }

        var globalNames = new HashSet<string>(StringComparer.Ordinal);

        var globalVars = GetChildren(translationUnitCursor)
            .Where(c => c.Kind == CXCursorKind.CXCursor_VarDecl)
            .ToList();

        HandleGlobals(asm, globalNames, globalVars, roData, stringPool);

        var functions = GetChildren(translationUnitCursor)
            .Where(c => c.Kind == CXCursorKind.CXCursor_FunctionDecl)
            .ToList();

        var mainFunctions = functions.Where(func => func.Spelling.ToString() == "main").ToList();
        if (mainFunctions.Count > 1)
            throw new InvalidOperationException(
                "Ambiguous entrypoint: more than one 'main' function found."
            );

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
                throw new InvalidOperationException(
                    $"Cannot define function '{funcName}'. Identifiers beginning with '__sharpie_' are reserved for hardware intrinsics."
                );

            var body = GetChildren(func).First(c => c.Kind == CXCursorKind.CXCursor_CompoundStmt);

            // because I can't be bothered to make this a two-pass compiler,
            // we're just gonna have to emit the body, scan for variables that need to be spilled,
            // then stitch the prologue and epilogue after the fact.
            var escapedVars = DetectEscapingVariables(func);
            var context = new EmissionContext(escapedVars, roData, stringPool, globalNames);
            context.IsMain = (funcName == "main");

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
                            context.Emit($"MOV r6, r15");
                            AccumulateOffset(6, space.Value, context);
                            context.Emit($"STS r{currentReg}, r6");
                        }
                    }
                    else
                    {
                        // A multi-word struct was passed in registers
                        // We must reconstruct it in its local stack home
                        context.Emit($"MOV r6, r15");
                        AccumulateOffset(6, space.Value, context);

                        for (int s = 0; s < slotsNeeded; s++)
                        {
                            context.Emit($"STS r{currentReg + s}, r6");
                            if (s < slotsNeeded - 1)
                                context.Emit($"IADD r6, 2");
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
            if (!context.HasReturn)
            {
                if (context.IsMain)
                    context.Emit("LDI r0, 0");
                context.Emit(context.ReturnInstruction);
            }

            context.Instructions.InsertRange(
                0,
                context.GetPrologue().Select(asm => Instruction.Parse(asm))
            );

            if (_optimize)
            {
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
            asm.AppendLine("; Readonly Data");
            foreach (var dataLine in roData)
                asm.AppendLine(dataLine);
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
                globalNames.Add(name);

                var linkage = clang.getCursorLinkage(global);
                bool isStatic = linkage == CXLinkageKind.CXLinkage_Internal;
                long sizeBytes = global.Type.SizeOf <= 0 ? 2 : global.Type.SizeOf;

                if (!isStatic)
                    asm.AppendLine($".GLOBAL");
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
                    var initVals = GetChildren(initListExprs.Last());
                    var typeKind = global.Type.CanonicalType.kind;

                    if (
                        typeKind == CXTypeKind.CXType_ConstantArray
                        || typeKind == CXTypeKind.CXType_IncompleteArray
                    )
                    {
                        long stride =
                            clang.getElementType(global.Type).SizeOf <= 0
                                ? 2
                                : clang.getElementType(global.Type).SizeOf;

                        foreach (var val in initVals)
                        {
                            long v = PeelExpression(val).Evaluate.AsLongLong;
                            asm.AppendLine(stride == 1 ? $"    .DB {v}" : $"    .DW {v}");
                            bytesWritten += stride;
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
                // Handle primitives and String Literals safely
                else if (normalExprs.Count > 0)
                {
                    var initExpr = PeelExpression(normalExprs.Last());

                    // --- NEW: CATCH STRING LITERALS ---
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

                            // Clang conveniently knows exactly how many bytes (including \0) the string takes!
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
                        asm.AppendLine(sizeBytes == 1 ? $"    .DB {v}" : $"    .DW {v}");
                        bytesWritten += sizeBytes;
                    }
                }

                // Zero-pad any uninitialized space (e.g. `int x;` or partially filled arrays)
                while (bytesWritten < sizeBytes)
                {
                    if (sizeBytes - bytesWritten == 1)
                    {
                        asm.AppendLine("    .DB 0");
                        bytesWritten += 1;
                    }
                    else
                    {
                        asm.AppendLine("    .DW 0");
                        bytesWritten += 2;
                    }
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
                decls.Add(current);

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
}
