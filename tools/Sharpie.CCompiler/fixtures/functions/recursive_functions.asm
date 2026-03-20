.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    LDI r1, 5
    CALL _func_factorial
    SETSP r15
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_factorial:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
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
    MOV r0, r15
    STA r1, r0
    MOV r0, r15
    IADD r0, 2
    STA r2, r0
    MOV r3, r8
    ISUB r3, 1
    MOV r1, r3
    CALL _func_factorial
    PUSH r0
    MOV r0, r15
    LDP r1, r0
    MOV r0, r15
    IADD r0, 2
    LDP r2, r0
    POP r0
    MOV r2, r0
    MUL r1, r2
    MOV r0, r1
    SETSP r15
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
    if_L0:
.ENDGLOBAL
.ENDREGION
