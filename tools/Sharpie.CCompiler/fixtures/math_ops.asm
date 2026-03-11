.REGION FIXED
Main:
    LDI r0, 4
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 5
    LDI r2, 0
    STS r1, r2
    LDI r1, 3
    LDI r2, 2
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    LDI r4, 2
    LDS r3, r4
    ADD r1, r3
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    ISUB r1, 1
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    IMUL r1, 2
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    IDIV r1, 3
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    IMOD r1, 4
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    IAND r1, 7
    LDI r2, 0
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    IOR r1, 8
    LDI r2, 0
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    IXOR r1, 2
    LDI r2, 0
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    LDI r2, 1
    SHL r1, r2
    LDI r2, 0
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    LDI r2, 2
    SHR r1, r2
    LDI r2, 0
    STS r1, r2
    LDI r1, 0
    LDS r0, r1
    NEG r0
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    HALT
.ENDREGION
