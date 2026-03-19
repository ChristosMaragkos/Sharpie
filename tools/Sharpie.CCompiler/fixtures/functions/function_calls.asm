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
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, 5
    MOV r8, r1
    MOV r0, r15
    STA r1, r0
    MOV r2, r8
    MOV r1, r2
    CALL _func_square
    PUSH r0
    MOV r0, r15
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r9, r1
    MOV r0, r9
    SETSP r15
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r9
    POP r8
    HALT
.ENDREGION
