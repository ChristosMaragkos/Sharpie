.REGION FIXED
_func_make_point:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r15
    LDI r1, 4
    CALL SYS_ALLOC_STACKFRAME
    GETSP r15
    MOV r8, r1
    MOV r9, r2
    MOV r10, r3
    MOV r1, r15
    MOV r1, r9
    MOV r2, r15
    STA r1, r2
    MOV r1, r10
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    MOV r1, r15
    PUSH r1
    MOV r1, r8
    POP r2
    LDI r3, 4
    CALL SYS_MEM_COPY
    SETSP r15
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    POP r15
    POP r10
    POP r9
    POP r8
    RET
Main:
    PUSH r8
    PUSH r15
    LDI r1, 12
    CALL SYS_ALLOC_STACKFRAME
    GETSP r15
    MOV r1, r15
    PUSH r1
    LDI r2, 10
    LDI r3, 20
    MOV r1, r15
    IADD r1, 4
    CALL _func_make_point
    POP r1
    MOV r1, r15
    IADD r1, 4
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 4
    CALL SYS_MEM_COPY
    PUSH r1
    PUSH r2
    LDI r3, 100
    LDI r4, 200
    MOV r2, r3
    MOV r3, r4
    MOV r1, r15
    IADD r1, 8
    CALL _func_make_point
    POP r2
    POP r1
    MOV r2, r15
    IADD r2, 8
    LDP r1, r2
    MOV r8, r1
    MOV r2, r15
    IADD r2, 2
    LDP r1, r2
    MOV r2, r8
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    LDI r1, 12
    CALL SYS_FREE_STACKFRAME
    POP r15
    POP r8
    HALT
.ENDREGION
