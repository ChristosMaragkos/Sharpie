.REGION FIXED
Main:
    PUSH r8
    PUSH r15
    LDI r1, 4
    CALL SYS_ALLOC_STACKFRAME
    GETSP r15
    LDI r1, 10
    MOV r2, r15
    STA r1, r2
    LDI r1, 20
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    MOV r1, r15
    MOV r8, r1
    LDI r1, 30
    MOV r2, r8
    STA r1, r2
    MOV r2, r15
    LDP r1, r2
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    POP r15
    POP r8
    HALT
.ENDREGION
