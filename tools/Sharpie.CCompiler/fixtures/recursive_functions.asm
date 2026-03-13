.REGION FIXED
factorial:
    PUSH r8
    MOV r8, r1
    MOV r1, r8
    ICMP r1, 1
    JNE else_L1
    LDI r0, 1
    POP r8
    RET
    else_L1:
    MOV r1, r8
    MOV r4, r8
    ISUB r4, 1
    MOV r3, r4
    PUSH r2
    PUSH r2
    MOV r1, r3
    CALL factorial
    POP r2
    POP r2
    MOV r2, r0
    MUL r0, r2
    MOV r0, r1
    POP r8
    RET
    if_L0:
Main:
    LDI r1, 5
    CALL factorial
    HALT
.ENDREGION
