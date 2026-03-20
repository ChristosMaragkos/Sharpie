.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r0, r15
    STA r1, r0
    LDI r2, 6
    PUSH r2
    LDI r2, 5
    PUSH r2
    LDI r2, 1
    LDI r3, 2
    LDI r4, 3
    LDI r5, 4
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    MOV r4, r5
    CALL _func_add_six_numbers
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    PUSH r0
    MOV r0, r15
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r8, r1
    LDI r1, 0
    CLS r1
    LDI r1, 10
    LDI r2, 20
    LDI r3, 5
    LDI r4, 529
    DRAW r1, r2, r3, r4
    MOV r0, r8
    SETSP r15
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL
.GLOBAL
_func_add_six_numbers:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r11
    PUSH r12
    PUSH r13
    PUSH r15
    GETSP r15
    MOV r6, r15
    IADD r6, 16
    LDS r7, r6
    MOV r12, r7
    MOV r6, r15
    IADD r6, 18
    LDS r7, r6
    MOV r13, r7
    MOV r8, r1
    MOV r9, r2
    MOV r10, r3
    MOV r11, r4
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r2, r10
    ADD r1, r2
    MOV r2, r11
    ADD r1, r2
    MOV r2, r12
    ADD r1, r2
    MOV r2, r13
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    POP r15
    POP r13
    POP r12
    POP r11
    POP r10
    POP r9
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_test_memory:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r0, r15
    STA r1, r0
    LDI r2, 20
    MOV r1, r2
    CALL SYS_ALLOC_STACKFRAME
    PUSH r0
    MOV r0, r15
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r8, r1
    MOV r1, r8
    LDI r2, 255
    LDI r3, 20
    CALL SYS_MEM_SET
    LDI r0, 1
    SETSP r15
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.ENDREGION
