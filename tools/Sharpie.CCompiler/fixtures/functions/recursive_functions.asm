.REGION FIXED
_func_factorial:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r8, r1
    MOV r1, r8
    ICMP r1, 1
    JNE else_L1
    LDI r0, 1
    SETSP r15
    POP r15
    POP r8
    RET
    else_L1:
    MOV r1, r8
    PUSH r1
    PUSH r2
    MOV r3, r8
    ISUB r3, 1
    MOV r1, r3
    CALL _func_factorial
    POP r2
    POP r1
    MOV r2, r0
    MUL r1, r2
    MOV r0, r1
    SETSP r15
    POP r15
    POP r8
    RET
    if_L0:
Main:
    PUSH r15
    GETSP r15
    LDI r1, 5
    CALL _func_factorial
    SETSP r15
    POP r15
    HALT
.ENDREGION
