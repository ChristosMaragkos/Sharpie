.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r15
    GETSP r15
    LDI r1, 10
    ICMP r1, 10
    JNE else_L1
    LDI r0, 1
    SETSP r15
    POP r15
    POP r8
    HALT
else_L1:
    XOR r0, r0
    SETSP r15
    POP r15
    POP r8
    HALT
if_L0:
.ENDGLOBAL
.ENDREGION
