.REGION FIXED
_func_fill_array:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r15
    GETSP r15
    MOV r8, r1
    MOV r9, r2
    LDI r1, 0
    MOV r10, r1
    while_start_L0:
    MOV r1, r10
    MOV r2, r9
    CMP r1, r2
    JGE while_end_L1
    MOV r1, r10
    IMUL r1, 10
    MOV r2, r8
    MOV r3, r10
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    STA r1, r2
    INC r10
    JMP while_start_L0
    while_end_L1:
    RET
Main:
    PUSH r15
    LDI r1, 6
    CALL SYS_ALLOC_STACKFRAME
    GETSP r15
    MOV r1, r15
    MOV R1, r15
    LDI r2, 3
    CALL _func_fill_array
    MOV r1, r15
    LDI r2, 2
    LDI r3, 2
    MUL r2, r3
    ADD r1, r2
    LDP r0, r1
    SETSP r15
    LDI r1, 6
    CALL SYS_FREE_STACKFRAME
    POP r15
    HALT
.ENDREGION
