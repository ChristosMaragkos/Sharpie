.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    LDI r1, 0 ; This should exist in the method.
    LDI r0, 0
    SETSP r15
    POP r15
    HALT
.ENDGLOBAL
.ENDREGION
