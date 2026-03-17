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
    JGE for_end_L2
    INC r8
    for_inc_L1:
    MOV r1, r9
    INC r9
    JMP for_start_L0
    for_end_L2:
    LDI r1, 0
    MOV r10, r1
    LDI r1, 0
    MOV r11, r1
    while_start_L3:
    MOV r1, r11
    ICMP r1, 9
    JGE while_end_L4
    MOV r1, r11
    ADD r10, r1
    INC r11
    JMP while_start_L3
    while_end_L4:
    LDI r1, 1000
    MOV r12, r1
    LDI r1, 0
    MOV r13, r1
    do_start_L5:
    MOV r1, r13
    SUB r12, r1
    INC r13
    do_cond_L6:
    MOV r1, r13
    ICMP r1, 10
    JLT do_start_L5
    do_end_L7:
    LDI r0, 0
    HALT
.ENDREGION
