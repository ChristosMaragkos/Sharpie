; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: switch_no_jt.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    LDI r1, 20
    CALL _func_get_score
epilogue_L0:
    HALT
.ENDGLOBAL
.GLOBAL
_func_get_score:
    PUSH r8
    PUSH r9
    MOV r9, r1
    XOR r1, r1
    MOV r8, r1
    MOV r1, r9
    ICMP r1, 5
    JLT default_L6
    ICMP r1, 15
    JGT default_L6
    ISUB r1, 5
    ADD r1, r1
    LDI r2, jt_L7
    ADD r1, r2
    LDP r1, r1
    ALT JMP r1
case_5_L3:
    LDI r8, 100
    JMP switch_end_L2
case_10_L4:
    LDI r8, 500
    JMP switch_end_L2
case_15_L5:
    LDI r8, 1000
    JMP switch_end_L2
default_L6:
    LDI r8, 65535
switch_end_L2:
    MOV r0, r8
epilogue_L1:
    POP r9
    POP r8
    RET
.ENDGLOBAL
; Readonly Data
jt_L7:
    .DW case_5_L3
    .DW default_L6
    .DW default_L6
    .DW default_L6
    .DW default_L6
    .DW case_10_L4
    .DW default_L6
    .DW default_L6
    .DW default_L6
    .DW default_L6
    .DW case_15_L5
.ENDREGION

