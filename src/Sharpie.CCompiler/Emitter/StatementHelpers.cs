using ClangSharp.Interop;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static void EmitStatement(CXCursor stmt, EmissionContext context)
    {
        switch (stmt.Kind)
        {
            case CXCursorKind.CXCursor_DeclStmt:
                EmitDeclarationStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_VarDecl:
                EmitVariableDeclaration(stmt, context);
                break;

            case CXCursorKind.CXCursor_BinaryOperator:
            case CXCursorKind.CXCursor_CompoundAssignOperator:
                EmitAssignmentStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_CallExpr:
                EmitCall(stmt, context);
                break;

            case CXCursorKind.CXCursor_ReturnStmt:
                EmitReturn(stmt, context);
                break;

            case CXCursorKind.CXCursor_IfStmt:
                EmitIfStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_WhileStmt:
                EmitWhileStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_ForStmt:
                EmitForStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_DoStmt:
                EmitDoStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_CompoundStmt:
                EmitFunctionBody(stmt, context);
                break;

            case CXCursorKind.CXCursor_UnaryOperator:
            case CXCursorKind.CXCursor_UnexposedExpr:
                EmitExpression(stmt, -1, context);
                break;

            case CXCursorKind.CXCursor_ContinueStmt:
                if (context.ContinueLabels.Count == 0)
                    throw new InvalidOperationException("Unexpected 'continue' outside of loop");
                context.Emit($"JMP {context.ContinueLabels.Peek()}");
                break;

            case CXCursorKind.CXCursor_BreakStmt:
                if (context.BreakLabels.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Break statement outside of a loop or switch."
                    );
                }

                context.Emit($"JMP {context.BreakLabels.Peek()}");
                break;

            case CXCursorKind.CXCursor_CaseStmt:
                var caseBody = GetChildren(stmt).Last();
                EmitStatement(caseBody, context);
                break;

            case CXCursorKind.CXCursor_DefaultStmt:
                var defaultBody = GetChildren(stmt).First();
                EmitStatement(defaultBody, context);
                break;

            case CXCursorKind.CXCursor_SwitchStmt:
                EmitSwitchStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_AsmStmt:
                ParseAndEmitAsmString(stmt, context.Emit);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported statement kind in `main`: {stmt.Kind}"
                );
        }
    }

    private static void EmitDeclarationStatement(CXCursor declStmt, EmissionContext context)
    {
        var declarations = GetChildren(declStmt);
        if (declarations.Count == 0)
            throw new InvalidOperationException("Declaration statement contained no declarations.");

        foreach (var declaration in declarations)
        {
            if (declaration.Kind != CXCursorKind.CXCursor_VarDecl)
            {
                throw new InvalidOperationException(
                    $"Unsupported declaration kind in statement: {declaration.Kind}"
                );
            }

            EmitVariableDeclaration(declaration, context);
        }
    }
}
