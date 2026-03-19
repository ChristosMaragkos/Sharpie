.REGION FIXED
_func_get_score:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r8, r1
    LDI r1, 0
    MOV r9, r1
    MOV r1, r8
    ICMP r1, 1
    JLT default_L5
    ICMP r1, 4
    JGT default_L5
    ISUB r1, 1
    IMUL r1, 2
    LDI r2, jt_L6
    ADD r1, r2
    LDP r1, r1
    ALT JMP r1
    case_1_L1:
    LDI r9, 100
    JMP switch_end_L0
    case_2_L2:
    LDI r9, 500
    JMP switch_end_L0
    case_3_L3:
    LDI r9, 800
    JMP switch_end_L0
    case_4_L4:
    LDI r9, 1000
    JMP switch_end_L0
    default_L5:
    LDI r9, 65535
    JMP switch_end_L0
    switch_end_L0:
    MOV r0, r9
    SETSP r15
    POP r15
    POP r9
    POP r8
    RET
Main:
    PUSH r15
    GETSP r15
    LDI r1, 3
    CALL _func_get_score
    SETSP r15
    POP r15
    HALT
; Readonly Data
jt_L6:
    .DW case_1_L1
    .DW case_2_L2
    .DW case_3_L3
    .DW case_4_L4
.ENDREGION
