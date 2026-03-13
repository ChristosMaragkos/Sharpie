.REGION FIXED
square:
    PUSH r8
    MOV r8, r1
    MOV r1, r8
    MOV r2, r8
    MUL r0, r2
    MOV r0, r1
    POP r8
    RET
Main:
    PUSH r8
    PUSH r9
    LDI r1, 5
    MOV r8, r1
    MOV r2, r8
    PUSH r2
    MOV r1, r2
    CALL square
    POP r2
    MOV r1, r0
    MOV r9, r1
    MOV r0, r9
    POP r9
    POP r8
    HALT
.ENDREGION
