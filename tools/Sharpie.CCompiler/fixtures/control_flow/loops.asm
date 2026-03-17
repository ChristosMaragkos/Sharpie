.REGION FIXED
Main:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r11
    PUSH r12
    PUSH r13
    PUSH r15
    GETSP r15
    LDI r1, 0
    MOV r8, r1
    LDI r1, 0
    MOV r9, r1
    for_start_L0:
    MOV r1, r9
    ICMP r1, 10
    JGE for_end_L1
    INC r8
    MOV r1, r9
    INC r9
    JMP for_start_L0
    for_end_L1:
    LDI r1, 0
    MOV r10, r1
    LDI r1, 0
    MOV r11, r1
    while_start_L2:
    MOV r1, r11
    ICMP r1, 9
    JGE while_end_L3
    MOV r1, r11
    ADD r10, r1
    INC r11
    JMP while_start_L2
    while_end_L3:
    LDI r1, 1000
    MOV r12, r1
    LDI r1, 0
    MOV r13, r1
    do_start_L4:
    MOV r1, r13
    SUB r12, r1
    INC r13
    MOV r1, r13
    ICMP r1, 10
    JLT do_start_L4
    LDI r0, 0
    HALT
.ENDREGION
