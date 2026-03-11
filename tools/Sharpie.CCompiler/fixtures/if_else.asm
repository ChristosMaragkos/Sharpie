.REGION FIXED
Main:
    LDI r0, 2
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 10
    LDI r2, 0
    STS r1, r2
    LDI r2, 0
    LDS r1, r2
    ICMP r1, 10
    JNE else_L1
    LDI r0, 1
    LDI r1, 2
    CALL SYS_FREE_STACKFRAME
    HALT
    else_L1:
    LDI r0, 0
    LDI r1, 2
    CALL SYS_FREE_STACKFRAME
    HALT
    if_L0:
.ENDREGION
