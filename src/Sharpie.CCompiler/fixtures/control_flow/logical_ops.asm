; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: logical_ops.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    LDI r1, 1
    LDI r2, 2
    LDI r3, 3
    LDI r4, 5
    CALL _func_test_logic
epilogue_L0:
    HALT
.ENDGLOBAL
.GLOBAL
_func_test_logic:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r11
    MOV r8, r1
    MOV r9, r2
    MOV r10, r3
    MOV r11, r4
    CMP r8, r9
    JEQ rel_true_L7
    XOR r1, r1
    JMP rel_end_L8
rel_true_L7:
    LDI r1, 1
rel_end_L8:
    ICMP r1, 0
    JNE logical_true_L4
    CMP r10, r11
    JLT rel_true_L12
    XOR r1, r1
    JMP rel_end_L13
rel_true_L12:
    LDI r1, 1
rel_end_L13:
    ICMP r1, 0
    JEQ logical_false_L10
    LDI r1, 1
    ICMP r1, 0
    JEQ logical_false_L10
    LDI r1, 1
    JMP logical_end_L11
logical_false_L10:
    XOR r1, r1
logical_end_L11:
    ICMP r1, 0
    JNE logical_true_L4
    XOR r1, r1
    JMP logical_end_L6
logical_true_L4:
    LDI r1, 1
logical_end_L6:
    ICMP r1, 0
    JEQ else_L3
    LDI r0, 420
    JMP epilogue_L1
else_L3:
    LDI r0, 69
    JMP epilogue_L1
if_L2:
epilogue_L1:
    POP r11
    POP r10
    POP r9
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

