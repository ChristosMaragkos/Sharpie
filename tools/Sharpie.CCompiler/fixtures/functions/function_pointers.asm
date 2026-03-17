.REGION FIXED
_func_add:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r8, r1
    MOV r9, r2
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    POP r15
    POP r9
    POP r8
    RET
_func_sub:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r8, r1
    MOV r9, r2
    MOV r1, r8
    MOV r2, r9
    SUB r1, r2
    MOV r0, r1
    SETSP r15
    POP r15
    POP r9
    POP r8
    RET
_func_do_math:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r15
    GETSP r15
    MOV r8, r1
    MOV r9, r2
    MOV r10, r3
    MOV r1, r8
    PUSH r1
    MOV r1, r9
    MOV r2, r10
    POP r0
    ALT CALL r0
    SETSP r15
    POP r15
    POP r10
    POP r9
    POP r8
    RET
Main:
    PUSH r8
    PUSH r9
    PUSH r15
    LDI r1, 4
    CALL SYS_ALLOC_STACKFRAME
    GETSP r15
    MOV r1, r15
    LDI r1, _func_add
    MOV r2, r15
    LDI r3, 0
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    STA r1, r2
    LDI r1, _func_sub
    MOV r2, r15
    LDI r3, 1
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    STA r1, r2
    PUSH r1
    LDI r2, _func_add
    LDI r3, 10
    LDI r4, 5
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_do_math
    POP r1
    MOV r1, r0
    MOV r8, r1
    PUSH r1
    MOV r3, r15
    LDI r4, 1
    LDI r5, 2
    MUL r4, r5
    ADD r3, r4
    LDP r2, r3
    LDI r3, 10
    LDI r4, 5
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_do_math
    POP r1
    MOV r1, r0
    MOV r9, r1
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    POP r15
    POP r9
    POP r8
    HALT
.ENDREGION
