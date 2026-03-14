.REGION FIXED
Main:
    PUSH r8
    LDI r1, 4
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 10
    GETSP r2
    STA r1, r2
    LDI r1, 20
    GETSP r2
    IADD r2, 2
    STA r1, r2
    GETSP r1
    MOV r8, r1
    LDI r1, 30
    MOV r2, r8
    STA r1, r2
    GETSP r2
    LDP r1, r2
    GETSP r3
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    POP r8
    HALT
.ENDREGION
