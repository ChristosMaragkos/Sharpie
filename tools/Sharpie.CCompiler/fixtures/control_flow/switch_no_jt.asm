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
    ICMP r1, 5
    JEQ case_5_L1
    ICMP r1, 10
    JEQ case_10_L2
    ICMP r1, 15
    JEQ case_15_L3
    JMP default_L4
    case_5_L1:
    LDI r9, 100
    JMP switch_end_L0
    case_10_L2:
    LDI r9, 500
    JMP switch_end_L0
    case_15_L3:
    LDI r9, 1000
    JMP switch_end_L0
    default_L4:
    LDI r9, 65535
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
    LDI r1, 20
    CALL _func_get_score
    SETSP r15
    POP r15
    HALT
.ENDREGION
