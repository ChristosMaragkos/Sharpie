using System.Text;
using ClangSharp.Interop;

namespace Sharpie.CCompiler;

// TODO: Intrinsics for syscalls, functions with >=5 parameters (by using the stack), structs, unions, function pointers, jump tables.
// In no particular order.
public sealed partial class SharpieEmitter
{
    private const int TempRegisterStart = 1;
    private const int TempRegisterEnd = 7;
    private const int LocalRegisterStart = 8;
    private const int LocalRegisterEnd = 15;

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
            var localCount = CountLocals(func);
            var stackBytes = localCount * 2; // 2 bytes / variable

            // because I can't be bothered to make this a two-pass compiler,
            // we're just gonna have to emit the body, scan for variables that need to be spilled,
            // then stitch the prologue and epilogue after the fact.
            var escapedVars = DetectEscapingVariables(func);
            var context = new EmissionContext(escapedVars);
            context.IsMain = (funcName == "main");

            asm.AppendLine($"{(context.IsMain ? "Main" : funcName)}:");

            var parameters = GetChildren(func)
                .Where(c => c.Kind == CXCursorKind.CXCursor_ParmDecl)
                .ToList();
            for (var i = 0; i < parameters.Count; i++)
            {
                var paramName = parameters[i].Spelling.ToString();
                var needsStack = context.EscapedVariables.Contains(paramName);
                var space = context.AllocateStorage(paramName, needsStack);

                if (i < 4)
                {
                    if (space.Type == StorageType.Register)
                    {
                        context.Emit($"MOV r{space.Value}, r{i + 1}");
                    }
                    else
                    {
                        using var offsetReg = context.AcquireTempRegister();
                        context.Emit($"LDI r{offsetReg.Value}, {space.Value}");
                        context.Emit($"STS r{i + 1}, r{offsetReg.Value}");
                    }
                }
                else
                {
                    context.PendingStackArguments.Add((i, space));
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
