.REGION FIXED
Main:
    LDI r0, 12
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 0
    LDI r2, 0
    STS r1, r2
    LDI r1, 0
    LDI r2, 2
    STS r1, r2
    for-start_L0:
    LDI r2, 2
    LDS r1, r2
    ICMP r1, 10
    JGE for-end_L1
    LDI r2, 0
    LDS r1, r2
    INC r1
    STS r1, r2
    LDI r3, 2
    LDS r2, r3
    MOV r1, r2
    INC r2
    STS r2, r3
    JMP for-start_L0
    for-end_L1:
    LDI r1, 0
    LDI r2, 4
    STS r1, r2
    LDI r1, 0
    LDI r2, 6
    STS r1, r2
    while-start_L2:
    LDI r2, 6
    LDS r1, r2
    ICMP r1, 9
    JGE while-end_L3
    LDI r2, 4
    LDS r1, r2
    LDI r4, 6
    LDS r3, r4
    ADD r1, r3
    STS r1, r2
    LDI r2, 6
    LDS r1, r2
    INC r1
    STS r1, r2
    JMP while-start_L2
    while-end_L3:
    LDI r1, 1000
    LDI r2, 8
    STS r1, r2
    LDI r1, 0
    LDI r2, 10
    STS r1, r2
    do-start_L4:
    LDI r2, 8
    LDS r1, r2
    LDI r4, 10
    LDS r3, r4
    SUB r1, r3
    STS r1, r2
    LDI r2, 10
    LDS r1, r2
    INC r1
    STS r1, r2
    LDI r2, 10
    LDS r1, r2
    ICMP r1, 10
    JLT do-start_L4
        LDI r0, 0
    LDI r1, 12
    CALL SYS_FREE_STACKFRAME
    HALT
.ENDREGION
