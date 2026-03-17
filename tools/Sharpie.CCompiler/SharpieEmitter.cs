using System.Text;
using ClangSharp.Interop;

namespace Sharpie.CCompiler;

// TODO: jump tables, inline assembly, string literal caching, bank switching impl
// In no particular order.
public sealed partial class SharpieEmitter
{
    private const int TempRegisterStart = 1;
    private const int TempRegisterEnd = 7;
    private const int LocalRegisterStart = 8;
    private const int LocalRegisterEnd = 14;

    private const int FramePointer = 15;

    public string EmitTranslationUnit(CXCursor translationUnitCursor)
    {
        var asm = new StringBuilder();
        asm.AppendLine(".REGION FIXED");

        var functions = GetChildren(translationUnitCursor)
            .Where(c => c.Kind == CXCursorKind.CXCursor_FunctionDecl)
            .ToList();

        var mainFunctions = functions.Where(func => func.Spelling.ToString() == "main");
        if (mainFunctions.Count() > 1)
            throw new InvalidOperationException(
                "Ambiguous entrypoint: more than one 'main' function found."
            );

        foreach (var func in functions)
        {
            var hasBody = GetChildren(func).Any(c => c.Kind == CXCursorKind.CXCursor_CompoundStmt);
            if (!hasBody)
                continue; // skip prototypes entirely

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
            var context = new EmissionContext(escapedVars);
            context.IsMain = (funcName == "main");

            asm.AppendLine($"{(context.IsMain ? "Main" : $"_func_{funcName}")}:");

            var parameters = GetChildren(func)
                .Where(c => c.Kind == CXCursorKind.CXCursor_ParmDecl)
                .ToList();

            int currentReg = 1;
            int currentStackArgOffset = 0;

            var retSizeBytes = func.ResultType.SizeOf;
            if (retSizeBytes > 2)
            {
                var hiddenReturn = context.AllocateStorage("__hidden_ret", false, 2);
                context.HiddenRetPtrReg = hiddenReturn.Value;
                context.Emit($"MOV r{hiddenReturn.Value}, r1");

                currentReg = 2;
            }

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
                            context.Emit($"LDI r6, {space.Value}");
                            context.Emit($"STS r{currentReg}, r6");
                        }
                    }
                    else
                    {
                        // A multi-word struct was passed in registers
                        // We must reconstruct it in its local stack home
                        context.Emit($"LDI r6, {space.Value}");

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

            foreach (var line in context.GetPrologue())
                asm.AppendLine($"    {line}");

            foreach (var line in context.Instructions)
                asm.AppendLine($"    {line}");

            // EmitReturn is responsible for the epilogue
        }

        asm.AppendLine(".ENDREGION");
        return asm.ToString();
    }

    private static void EmitFunctionBody(CXCursor compoundStmt, EmissionContext context)
    {
        foreach (var stmt in GetChildren(compoundStmt))
        {
            if (context.HasReturn)
                throw new InvalidOperationException("Dead code is not supported in this MVP");

            EmitStatement(stmt, context);
        }
    }
}
