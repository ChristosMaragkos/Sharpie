.REGION FIXED
Main:
    PUSH r8
    PUSH r15
    GETSP r15
    PUSH r1
    LDI r2, 2
    MOV r1, r2
    ALT RND r0, r1
    POP r1
    MOV r1, r0
    MOV r8, r1
    MOV r1, r8
    ICMP r1, 0
    JNE ternary_false_L0
    LDI r0, 69
    JMP ternary_end_L1
    ternary_false_L0:
    LDI r0, 420
    ternary_end_L1:
    SETSP r15
    POP r15
    POP r8
    HALT
.ENDREGION
