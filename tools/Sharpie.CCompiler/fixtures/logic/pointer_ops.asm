.REGION FIXED
Main:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r15
    LDI r1, 2
    CALL SYS_ALLOC_STACKFRAME
    GETSP r15
    LDI r1, 500
    LDI r2, 0
    STS r1, r2
    MOV r1, r15
    MOV r8, r1
    LDI r1, 1000
    MOV r9, r1
    LDI r1, 42
    MOV r2, r9
    STA r1, r2
    MOV r1, r9
    LDP r1, r1
    MOV r10, r1
    MOV r0, r10
    SETSP r15
    LDI r1, 2
    CALL SYS_FREE_STACKFRAME
    POP r15
    POP r10
    POP r9
    POP r8
    HALT
.ENDREGION
