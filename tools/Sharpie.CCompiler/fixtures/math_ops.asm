.REGION FIXED
Main:
    LDI r8, 5
    LDI r9, 3
    MOV r1, r9
    ADD r8, r1
    ISUB r8, 1
    IMUL r8, 2
    IDIV r8, 3
    IMOD r8, 4
    IAND r8, 7
    IOR r8, 8
    IXOR r8, 2
    LDI r1, 1
    SHL r8, r1
    LDI r1, 2
    SHR r8, r1
    MOV r0, r8
    NEG r0
    HALT
.ENDREGION
