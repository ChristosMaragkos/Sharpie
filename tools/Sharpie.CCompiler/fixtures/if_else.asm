.REGION FIXED
Main:
    PUSH r8
    LDI r1, 10
    MOV r8, r1
    MOV r1, r8
    ICMP r1, 10
    JNE else_L1
    LDI r0, 1
    POP r8
    HALT
    else_L1:
    LDI r0, 0
    POP r8
    HALT
    if_L0:
.ENDREGION
