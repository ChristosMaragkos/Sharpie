.REGION FIXED
test_registers:
    PUSH r8
    LDI r1, 4
    CALL SYS_ALLOC_STACKFRAME
    MOV r8, r1
    LDI r6, 0
    STS r2, r6
    IADD r6, 2
    STS r3, r6
    MOV r2, r8
    GETSP r4
    LDP r3, r4
    ADD r2, r3
    MOV r1, r2
    GETSP r3
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    POP r8
    RET
test_stack:
    PUSH r8
    PUSH r9
    PUSH r10
    LDI r1, 4
    CALL SYS_ALLOC_STACKFRAME
    LDI r6, 12
    LDI r5, 0
    LDS r7, r6
    STS r7, r5
    IADD r6, 2
    IADD r5, 2
    LDS r7, r6
    STS r7, r5
    MOV r8, r1
    MOV r9, r2
    MOV r10, r3
    MOV r4, r8
    MOV r5, r9
    ADD r4, r5
    MOV r3, r4
    MOV r4, r10
    ADD r3, r4
    MOV r2, r3
    GETSP r4
    LDP r3, r4
    ADD r2, r3
    MOV r1, r2
    GETSP r3
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    POP r10
    POP r9
    POP r8
    RET
test_pointer:
    PUSH r8
    MOV r8, r1
    LDI r1, 30
    MOV r2, r8
    STA r1, r2
    RET
Main:
    PUSH r8
    PUSH r9
    LDI r1, 8
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 10
    GETSP r2
    STA r1, r2
    LDI r1, 20
    GETSP r2
    IADD r2, 2
    STA r1, r2
    PUSH r1
    LDI r2, 5
    GETSP r3
    LDP r4, r3
    IADD r3, 2
    LDP r5, r3
    MOV r1, r2
    MOV r2, r4
    MOV r3, r5
    CALL test_registers
    POP r1
    MOV r1, r0
    MOV r8, r1
    LDI r1, 100
    GETSP r2
    IADD r2, 4
    STA r1, r2
    LDI r1, 200
    GETSP r2
    IADD r2, 4
    IADD r2, 2
    STA r1, r2
    PUSH r1
    GETSP r2
    IADD r2, 4
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    LDI r2, 1
    LDI r3, 2
    LDI r4, 3
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    CALL test_stack
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    POP r1
    MOV r1, r0
    MOV r9, r1
    GETSP r1
    IADD r1, 4
    CALL test_pointer
    MOV r2, r8
    MOV r3, r9
    ADD r2, r3
    MOV r1, r2
    GETSP r3
    IADD r3, 4
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
    LDI r1, 8
    CALL SYS_FREE_STACKFRAME
    POP r9
    POP r8
    HALT
.ENDREGION
