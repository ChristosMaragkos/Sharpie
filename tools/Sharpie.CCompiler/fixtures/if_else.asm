.REGION FIXED
Main:
    LDI r8, 10
    MOV r1, r8
    ICMP r1, 10
    JNE _L1
    LDI r0, 1
    HALT
    _L1:
    LDI r0, 0
    HALT
    _L0:
.ENDREGION
