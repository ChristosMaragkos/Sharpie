.REGION FIXED
square:
    MOV r0, r1
    MUL r0, r1
    RET
Main:
    LDI r8, 5
    MOV r1, r8
    CALL square
    MOV r9, r0
    MOV r0, r9
    HALT
.ENDREGION
