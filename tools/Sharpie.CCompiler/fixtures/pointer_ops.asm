.REGION FIXED
Main:
    LDI r8, 1000
    LDI r1, 42
    MOV r2, r8
    STA r1, r2
    MOV r9, r8
    LDP r9, r9
    MOV r0, r9
    HALT
.ENDREGION
