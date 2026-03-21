.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 10
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r15
    LDI r2, 30
    MOV r3, r1
    STA r2, r3
    LDI r2, 30
    MOV r3, r1
    IADD r3, 2
    STA r2, r3
    MOV r1, r15
    IADD r1, 4
    LDI r2, 1
    MOV r3, r1
    STA r2, r3
    LDI r2, 2
    MOV r3, r1
    IADD r3, 2
    STA r2, r3
    LDI r2, 3
    MOV r3, r1
    IADD r3, 4
    STA r2, r3
    MOV r2, r15
    LDP r1, r2
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r3, r15
    IADD r3, 4
    XOR r4, r4
    LDI r5, 2
    MUL r4, r5
    ADD r3, r4
    LDP r2, r3
    MOV r4, r15
    IADD r4, 4
    LDI r5, 2
    ADD r4, r5
    LDP r3, r4
    ADD r2, r3
    MOV r4, r15
    IADD r4, 4
    LDI r5, 4
    ADD r4, r5
    LDP r3, r4
    ADD r2, r3
    IDIV r2, 3
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    MOV r6, r15
    LDI r7, 10
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.ENDREGION
