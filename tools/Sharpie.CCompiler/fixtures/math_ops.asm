.REGION FIXED
Main:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    LDI r1, 5
    MOV r8, r1
    LDI r1, 3
    MOV r9, r1
    MOV r1, r9
    ADD r8, r1
    ISUB r8, 1
    LDI r1, 2
    MUL r8, r1
    IDIV r8, 3
    IMOD r8, 4
    MOV r1, r8
    IAND r1, 7
    MOV r8, r1
    MOV r1, r8
    IOR r1, 8
    MOV r8, r1
    MOV r1, r8
    IXOR r1, 2
    MOV r8, r1
    MOV r1, r8
    LDI r2, 1
    SHL r1, r2
    MOV r8, r1
    MOV r1, r8
    LDI r2, 2
    SHR r1, r2
    MOV r8, r1
    MOV r0, r8
    NEG r0
    SETSP r15
    POP r15
    POP r9
    POP r8
    HALT
.ENDREGION
