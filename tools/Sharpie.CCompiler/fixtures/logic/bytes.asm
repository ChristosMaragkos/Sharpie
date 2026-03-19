.REGION FIXED
Main:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, 68
    MOV r8, r1
    MOV r1, r8
    IADD r1, 1
    MOV r8, r1
    MOV r1, r15
    MOV r1, r8
    MOV r2, r15
    ALT STA r1, r2
    LDI r1, 99
    MOV r2, r15
    IADD r2, 1
    ALT STA r1, r2
    MOV r2, r15
    ALT LDP r1, r2
    MOV r3, r15
    IADD r3, 1
    ALT LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDREGION
