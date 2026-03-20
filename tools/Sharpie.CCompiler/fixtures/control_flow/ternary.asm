.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r0, r15
    STA r1, r0
    LDI r2, 2
    MOV r1, r2
    ALT RND r0, r1
    PUSH r0
    MOV r0, r15
    LDP r1, r0
    POP r0
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
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION
