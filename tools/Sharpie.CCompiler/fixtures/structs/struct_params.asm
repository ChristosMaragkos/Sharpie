.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 10
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r15
    LDI r1, 10
    MOV r2, r15
    STA r1, r2
    LDI r1, 20
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    MOV r0, r15
    IADD r0, 4
    STA r1, r0
    LDI r2, 5
    MOV r3, r15
    LDP r4, r3
    IADD r3, 2
    LDP r5, r3
    MOV r1, r2
    MOV r2, r4
    MOV r3, r5
    CALL _func_test_registers
    PUSH r0
    MOV r0, r15
    IADD r0, 4
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r8, r1
    MOV r1, r15
    IADD r1, 6
    LDI r1, 100
    MOV r2, r15
    IADD r2, 6
    STA r1, r2
    LDI r1, 200
    MOV r2, r15
    IADD r2, 6
    IADD r2, 2
    STA r1, r2
    MOV r0, r15
    IADD r0, 4
    STA r1, r0
    MOV r2, r15
    IADD r2, 6
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    LDI r2, 1
    LDI r3, 2
    LDI r4, 3
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_test_stack
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    PUSH r0
    MOV r0, r15
    IADD r0, 4
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r9, r1
    MOV r1, r15
    IADD r1, 6
    CALL _func_test_pointer
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r3, r15
    IADD r3, 6
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    MOV r6, r15
    LDI r7, 10
    ADD r6, r7
    SETSP r6
    POP r15
    POP r9
    POP r8
    HALT
.ENDGLOBAL
.GLOBAL
_func_test_registers:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r6, r15
    STS r2, r6
    IADD r6, 2
    STS r3, r6
    MOV r1, r8
    MOV r3, r15
    LDP r2, r3
    ADD r1, r2
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_test_stack:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r6, r15
    IADD r6, 14
    MOV r5, r15
    IADD r5, 0
    LDS r7, r6
    STS r7, r5
    IADD r6, 2
    IADD r5, 2
    LDS r7, r6
    STS r7, r5
    MOV r8, r1
    MOV r9, r2
    MOV r10, r3
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r2, r10
    ADD r1, r2
    MOV r3, r15
    LDP r2, r3
    ADD r1, r2
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r10
    POP r9
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_test_pointer:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r8, r1
    LDI r1, 30
    MOV r2, r8
    STA r1, r2
    RET
.ENDGLOBAL
.ENDREGION
