.REGION FIXED
factorial:
    LDI r0, 2
    CALL SYS_ALLOC_STACKFRAME
    LDI r3, 0
    STS r1, r3
    LDI r2, 0
    LDS r1, r2
    ICMP r1, 1
    JNE else_L1
    LDI r0, 1
    LDI r1, 2
    CALL SYS_FREE_STACKFRAME
    RET
    else_L1:
    LDI r1, 0
    LDS r0, r1
    LDI r2, 0
    LDS r1, r2
    ISUB r1, 1
    CALL factorial
    MOV r1, r0
    MUL r0, r1
    LDI r1, 2
    CALL SYS_FREE_STACKFRAME
    RET
    if_L0:
Main:
    LDI r1, 5
    CALL factorial
    HALT
.ENDREGION
