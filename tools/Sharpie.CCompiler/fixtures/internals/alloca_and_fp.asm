.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r9, 10
    STA r1, r15
    LDI r1, 100
    CALL SYS_ALLOC_STACKFRAME
    PUSH r0
    LDP r1, r15
    POP r0
    MOV r1, r0
    MOV r8, r1
    LDI r1, 42
    STA r1, r8
    LDI r10, 20
    MOV r1, r9
    MOV r2, r10
    ADD r1, r2
    LDP r2, r8
    ADD r1, r2
    MOV r0, r1
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r10
    POP r9
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION
