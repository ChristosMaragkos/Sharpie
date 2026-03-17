.REGION FIXED
_func_helper:
    PUSH r15
    GETSP r15
    LDI r0, 42
    SETSP r15
    POP r15
    RET
Main:
    PUSH r15
    GETSP r15
    LDI r0, 1
    SETSP r15
    POP r15
    HALT
.ENDREGION
