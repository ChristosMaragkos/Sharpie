.REGION FIXED
Main:
    LDI r8, 0
    LDI r9, 0
    _L0:
    MOV r1, r9
    ICMP r1, 10
    JGE _L1
    INC r8
    MOV r1, r9
    INC r9
    JMP _L0
    _L1:
    LDI r10, 0
    LDI r11, 0
    _L2:
    MOV r1, r11
    ICMP r1, 9
    JGE _L3
    MOV r1, r11
    ADD r10, r1
    INC r11
    JMP _L2
    _L3:
    LDI r12, 1000
    LDI r13, 0
    _L4:
    MOV r1, r13
    SUB r12, r1
    INC r13
    MOV r1, r13
    ICMP r1, 10
    JLT _L4
        LDI r0, 0
    HALT
.ENDREGION
