.REGION FIXED
_func_square:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r8, r1
    MOV r1, r8
    MOV r2, r8
    MUL r1, r2
    MOV r0, r1
    SETSP r15
    POP r15
    POP r8
    RET
Main:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    LDI r1, 5
    MOV r8, r1
    PUSH r1
    MOV r2, r8
    MOV r1, r2
    CALL _func_square
    POP r1
    MOV r1, r0
    MOV r9, r1
    MOV r0, r9
    SETSP r15
    POP r15
    POP r9
    POP r8
    HALT
.ENDREGION
