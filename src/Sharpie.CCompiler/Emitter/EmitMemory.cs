using ClangSharp.Interop;
using Sharpie.CCompiler.Emitter;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static void EmitLValueAddress(CXCursor lvalue, int targetReg, EmissionContext context)
    {
        var peeled = PeelExpression(lvalue);

        if (peeled.Kind == CXCursorKind.CXCursor_DeclRefExpr)
        {
            var name = peeled.Spelling.ToString();

            if (context.Locals.TryGetValue(name, out var loc))
            {
                context.Emit($"MOV r{targetReg}, r15");
                AccumulateOffset(targetReg, loc.Value, context);
            }
            else if (context.Globals.Contains(name))
            {
                context.Emit($"LDI r{targetReg}, _global_{name}");
            }
            else
            {
                throw new InvalidOperationException($"Unknown variable '{name}'");
            }
        }
        else if (
            peeled.Kind == CXCursorKind.CXCursor_UnaryOperator
            && GetUnaryOperatorKind(peeled) == CXUnaryOperatorKind.CXUnaryOperator_Deref
        )
        {
            var ptrExpr = GetChildren(peeled).First();
            EmitExpression(ptrExpr, targetReg, context);
        }
        else if (
            peeled.Kind
            is CXCursorKind.CXCursor_MemberRefExpr
                or CXCursorKind.CXCursor_MemberRef
        )
        {
            var children = GetChildren(peeled);
            if (children.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot compute address for {peeled.Kind} ({peeled.Spelling}): no base expression"
                );
            }

            var baseExpr = children.First();
            bool isPointer = baseExpr.Type.CanonicalType.kind == CXTypeKind.CXType_Pointer;

            if (isPointer)
                EmitExpression(baseExpr, targetReg, context);
            else
                EmitLValueAddress(baseExpr, targetReg, context);

            var fieldDecl = clang.getCursorReferenced(peeled);

            long offsetBits = clang.Cursor_getOffsetOfField(fieldDecl);
            if (offsetBits < 0)
            {
                throw new InvalidOperationException(
                    $"Could not determine offset for struct field '{peeled.Spelling}'"
                );
            }

            long offsetBytes = offsetBits / 8;
            AccumulateOffset(targetReg, (int)offsetBytes, context);
        }
        else if (peeled.Kind == CXCursorKind.CXCursor_CallExpr)
        {
            throw new InvalidOperationException(
                "Call expressions are not lvalues. Aggregate call results must be consumed as expressions."
            );
        }
        else if (peeled.Kind == CXCursorKind.CXCursor_ArraySubscriptExpr)
        {
            var children = GetChildren(peeled);
            var baseExpr = PeelExpression(children[0]);
            var indexExpr = PeelExpression(children[1]);

            if (baseExpr.Type.CanonicalType.kind == CXTypeKind.CXType_Pointer)
                EmitExpression(baseExpr, targetReg, context);
            else
                EmitLValueAddress(baseExpr, targetReg, context);

            using var indexReg = context.AcquireTempRegister();
            EmitExpression(indexExpr, indexReg.Value, context);

            var stride = peeled.Type.SizeOf;
            if (stride > 1)
            {
                using var strideReg = context.AcquireTempRegister();
                context.Emit($"LDI r{strideReg.Value}, {stride}");
                context.Emit($"MUL r{indexReg.Value}, r{strideReg.Value}");
            }

            context.Emit($"ADD r{targetReg}, r{indexReg.Value}");
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot compute memory address for expression kind: {peeled.Kind}"
            );
        }
    }

    private static void AccumulateOffset(int targetReg, int offset, EmissionContext context)
    {
        while (offset > 0)
        {
            var chunk = Math.Min(offset, 255);
            context.Emit($"IADD r{targetReg}, {chunk}");
            offset -= chunk;
        }
    }

    private static bool IsAggregateType(CXType type)
    {
        var canonicalKind = type.CanonicalType.kind;
        return canonicalKind == CXTypeKind.CXType_Record
            || canonicalKind == CXTypeKind.CXType_ConstantArray
            || canonicalKind == CXTypeKind.CXType_IncompleteArray
            || canonicalKind == CXTypeKind.CXType_VariableArray;
    }

    private static void EmitAllocStackframe(int byteCount, EmissionContext context)
    {
        if (byteCount < 0 || byteCount > 255)
            throw new InvalidOperationException($"SYS_ALLOC_STACKFRAME expects a byte count 0-255, got {byteCount}");

        context.Emit($"LDI r1, {byteCount}");
        context.Emit("CALL SYS_ALLOC_STACKFRAME");
    }

    private static void EmitFreeStackframe(int byteCount, EmissionContext context)
    {
        if (byteCount < 0 || byteCount > 255)
            throw new InvalidOperationException($"SYS_FREE_STACKFRAME expects a byte count 0-255, got {byteCount}");

        context.Emit($"LDI r1, {byteCount}");
        context.Emit("CALL SYS_FREE_STACKFRAME");
    }

    private static string GetOrAddStringLiteral(CXCursor stringLiteralNode, EmissionContext context)
    {
        string rawString = "";
        unsafe
        {
            var range = clang.getCursorExtent(stringLiteralNode);
            var tu = clang.Cursor_getTranslationUnit(stringLiteralNode);
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

        if (!context.StringPool.TryGetValue(rawString, out var existingLabel))
        {
            existingLabel = EmissionContext.GenerateLabel("str");
            context.StringPool[rawString] = existingLabel;

            context.ReadOnlyData.Add($"{existingLabel}:");
            context.ReadOnlyData.Add($"    .DB {rawString}, 0");
        }

        return existingLabel;
    }

    private static void EmitCompoundLiteralIntoAddress(
        CXCursor compoundLiteralExpr,
        int destinationAddressRegister,
        EmissionContext context
    )
    {
        var initList = GetChildren(compoundLiteralExpr)
            .FirstOrDefault(c => c.Kind == CXCursorKind.CXCursor_InitListExpr);
        if (initList.Kind != CXCursorKind.CXCursor_InitListExpr)
            return;

        var rawChildren = GetChildren(initList);
        var decl = clang.getTypeDeclaration(compoundLiteralExpr.Type.CanonicalType);
        var fields = GetChildren(decl).Where(c => c.Kind == CXCursorKind.CXCursor_FieldDecl).ToList();

        int fieldIdx = 0;
        foreach (var rawChild in rawChildren)
        {
            var peeled = PeelExpression(rawChild);
            if (peeled.Kind == CXCursorKind.CXCursor_MemberRef)
            {
                var fieldDecl = clang.getCursorReferenced(peeled);
                if (fieldDecl.Kind == CXCursorKind.CXCursor_FieldDecl)
                {
                    long offsetBytes = clang.Cursor_getOffsetOfField(fieldDecl) / 8;
                    long fieldSize = fieldDecl.Type.SizeOf;

                    var initChildren = GetChildren(rawChild);
                    var valExpr = initChildren.FirstOrDefault(
                        c => c.Kind != CXCursorKind.CXCursor_MemberRef
                    );
                    if (valExpr.Kind != CXCursorKind.CXCursor_NoDeclFound)
                    {
                        EmitCompoundLiteralStoreField(valExpr, fieldSize, offsetBytes, destinationAddressRegister, context);
                    }
                }
            }
            else
            {
                if (fieldIdx < fields.Count)
                {
                    long offsetBytes = clang.Cursor_getOffsetOfField(fields[fieldIdx]) / 8;
                    long fieldSize = fields[fieldIdx].Type.SizeOf;
                    EmitCompoundLiteralStoreField(rawChild, fieldSize, offsetBytes, destinationAddressRegister, context);
                }
                fieldIdx++;
            }
        }
    }

    private static void EmitCompoundLiteralStoreField(
        CXCursor valExpr,
        long fieldSize,
        long offsetBytes,
        int destinationAddressRegister,
        EmissionContext context
    )
    {
        using var valReg = context.AcquireTempRegister();
        EmitExpression(valExpr, valReg.Value, context);

        using var addrReg = context.AcquireTempRegister();
        context.Emit($"MOV r{addrReg.Value}, r{destinationAddressRegister}");
        AccumulateOffset(addrReg.Value, (int)offsetBytes, context);

        string altPrefix = (fieldSize == 1) ? "ALT " : "";
        context.Emit($"{altPrefix}STA r{valReg.Value}, r{addrReg.Value}");
    }

    private static void EmitCompoundLiteral(CXCursor expr, int targetReg, EmissionContext context)
    {
        long size = expr.Type.SizeOf;
        var space = context.AllocateStorage(EmissionContext.GenerateLabel("anon_lit"), true, (int)size);

        using var baseReg = context.AcquireTempRegister();
        context.Emit($"MOV r{baseReg.Value}, r15");
        AccumulateOffset(baseReg.Value, space.Value, context);
        EmitCompoundLiteralIntoAddress(expr, baseReg.Value, context);

        if (targetReg >= 0)
        {
            context.Emit($"MOV r{targetReg}, r15");
            AccumulateOffset(targetReg, space.Value, context);
        }
    }
}
