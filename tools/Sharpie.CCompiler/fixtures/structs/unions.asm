.REGION FIXED
Main:
    PUSH r8
    PUSH r9
    PUSH r15
    LDI r1, 2
    CALL SYS_ALLOC_STACKFRAME
    GETSP r15
    MOV r1, r15
    LDI r1, 258
    MOV r2, r15
    STA r1, r2
    MOV r2, r15
    ALT LDP r1, r2
    MOV r8, r1
    MOV r2, r15
    IADD r2, 1
    ALT LDP r1, r2
    MOV r9, r1
    LDI r1, 10
    MOV r2, r15
    ALT STA r1, r2
    MOV r1, r15
    LDP r0, r1
    SETSP r15
    LDI r1, 2
    CALL SYS_FREE_STACKFRAME
    POP r15
    POP r9
    POP r8
    HALT
.ENDREGION
