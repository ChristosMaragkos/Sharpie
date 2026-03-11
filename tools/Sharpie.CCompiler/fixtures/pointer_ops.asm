.REGION FIXED
Main:
    LDI r0, 8
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 500
    LDI r2, 0
    STS r1, r2
    GETSP r1
    LDI r2, 2
    STS r1, r2
    LDI r1, 1000
    LDI r2, 4
    STS r1, r2
    LDI r1, 42
    LDI r3, 4
    LDS r2, r3
    STA r1, r2
    LDI r2, 4
    LDS r1, r2
    LDP r1, r1
    LDI r2, 6
    STS r1, r2
    LDI r1, 6
    LDS r0, r1
    LDI r1, 8
    CALL SYS_FREE_STACKFRAME
    HALT
.ENDREGION
