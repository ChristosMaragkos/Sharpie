.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    LDI r1, 1
    LDI r2, 2
    LDI r3, 3
    LDI r4, 5
    CALL _func_test_logic
    SETSP r15
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_test_logic:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r11
    PUSH r15
    GETSP r15
    MOV r8, r1
    MOV r9, r2
    MOV r10, r3
    MOV r11, r4
    CMP r8, r9
    JEQ rel_true_L5
    XOR r1, r1
    JMP rel_end_L6
rel_true_L5:
    LDI r1, 1
rel_end_L6:
    ICMP r1, 0
    JNE logical_true_L2
    CMP r10, r11
    JLT rel_true_L10
    XOR r1, r1
    JMP rel_end_L11
rel_true_L10:
    LDI r1, 1
rel_end_L11:
    ICMP r1, 0
    JEQ logical_false_L8
    LDI r1, 1
    ICMP r1, 0
    JEQ logical_false_L8
    LDI r1, 1
    JMP logical_end_L9
logical_false_L8:
    XOR r1, r1
logical_end_L9:
    ICMP r1, 0
    JNE logical_true_L2
    XOR r1, r1
    JMP logical_end_L4
logical_true_L2:
    LDI r1, 1
logical_end_L4:
    ICMP r1, 0
    JEQ else_L1
    LDI r0, 420
    SETSP r15
    POP r15
    POP r11
    POP r10
    POP r9
    POP r8
    RET
else_L1:
    LDI r0, 69
    SETSP r15
    POP r15
    POP r11
    POP r10
    POP r9
    POP r8
    RET
if_L0:
.ENDGLOBAL
.ENDREGION
