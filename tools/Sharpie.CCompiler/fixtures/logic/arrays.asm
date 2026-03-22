.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 6
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r15
    MOV r1, r15
    LDI r2, 3
    CALL _func_fill_array
    MOV r1, r15
    LDI r2, 4
    ADD r1, r2
    LDP r0, r1
    MOV r6, r15
    LDI r7, 6
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_fill_array:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r15
    GETSP r15
    MOV r9, r1
    MOV r10, r2
    XOR r1, r1
    MOV r8, r1
while_start_L0:
    CMP r8, r10
    JGE while_end_L1
    MOV r1, r8
    IMUL r1, 10
    MOV r2, r9
    MOV r3, r8
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    STA r1, r2
    INC r8
    JMP while_start_L0
while_end_L1:
    RET
.ENDGLOBAL
.ENDREGION
