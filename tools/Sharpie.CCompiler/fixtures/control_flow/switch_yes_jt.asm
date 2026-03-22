.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    LDI r1, 3
    CALL _func_get_score
    SETSP r15
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_get_score:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r9, r1
    XOR r1, r1
    MOV r8, r1
    MOV r1, r9
    ICMP r1, 1
    JLT default_L5
    ICMP r1, 4
    JGT default_L5
    DEC r1
    ADD r1, r1
    LDI r2, jt_L6
    ADD r1, r2
    LDP r1, r1
    ALT JMP r1
case_1_L1:
    LDI r8, 100
    JMP switch_end_L0
case_2_L2:
    LDI r8, 500
    JMP switch_end_L0
case_3_L3:
    LDI r8, 800
    JMP switch_end_L0
case_4_L4:
    LDI r8, 1000
    JMP switch_end_L0
default_L5:
    LDI r8, 65535
switch_end_L0:
    MOV r0, r8
    SETSP r15
    POP r15
    POP r9
    POP r8
    RET
.ENDGLOBAL
; Readonly Data
jt_L6:
    .DW case_1_L1
    .DW case_2_L2
    .DW case_3_L3
    .DW case_4_L4
.ENDREGION
