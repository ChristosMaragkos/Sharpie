.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    LDI r1, 20
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
    MOV r8, r1
    XOR r1, r1
    MOV r9, r1
    MOV r1, r8
    ICMP r1, 5
    JLT default_L4
    ICMP r1, 15
    JGT default_L4
    ISUB r1, 5
    ADD r1, r1
    LDI r2, jt_L5
    ADD r1, r2
    LDP r1, r1
    ALT JMP r1
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
.ENDGLOBAL
; Readonly Data
jt_L5:
    .DW case_5_L1
    .DW default_L4
    .DW default_L4
    .DW default_L4
    .DW default_L4
    .DW case_10_L2
    .DW default_L4
    .DW default_L4
    .DW default_L4
    .DW default_L4
    .DW case_15_L3
.ENDREGION
