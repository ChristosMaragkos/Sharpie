.REGION FIXED
square:
    LDI r0, 2
    CALL SYS_ALLOC_STACKFRAME
    LDI r3, 0
    STS r1, r3
    LDI r1, 0
    LDS r0, r1
    LDI r2, 0
    LDS r1, r2
    MUL r0, r1
    LDI r1, 2
    CALL SYS_FREE_STACKFRAME
    RET
Main:
    LDI r0, 4
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 5
    LDI r2, 0
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    CALL square
    MOV r1, r0
    LDI r2, 2
    STS r1, r2
    LDI r1, 2
    LDS r0, r1
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    HALT
.ENDREGION
