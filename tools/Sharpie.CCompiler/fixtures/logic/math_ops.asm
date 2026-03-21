.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    LDI r8, 5
    LDI r9, 3
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r8, r1
    DEC r1
    MOV r8, r1
    LDI r2, 2
    MUL r1, r2
    MOV r8, r1
    IDIV r1, 3
    MOV r8, r1
    IMOD r1, 4
    MOV r8, r1
    IAND r1, 7
    MOV r8, r1
    IOR r1, 8
    MOV r8, r1
    IXOR r1, 2
    MOV r8, r1
    LDI r2, 1
    SHL r1, r2
    MOV r8, r1
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
.ENDGLOBAL
.ENDREGION
