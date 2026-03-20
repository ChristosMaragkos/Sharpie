.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 16
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r15
    MOV r0, r15
    IADD r0, 8
    STA r1, r0
    LDI r2, 10
    LDI r3, 20
    MOV r1, r15
    IADD r1, 4
    CALL _func_make_point
    PUSH r0
    MOV r0, r15
    IADD r0, 8
    LDP r1, r0
    POP r0
    MOV r1, r15
    IADD r1, 4
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 4
    CALL SYS_MEM_MOVE
    MOV r0, r15
    IADD r0, 8
    STA r1, r0
    MOV r0, r15
    IADD r0, 14
    STA r2, r0
    LDI r3, 100
    LDI r4, 200
    MOV r2, r3
    MOV r3, r4
    MOV r1, r15
    IADD r1, 10
    CALL _func_make_point
    PUSH r0
    MOV r0, r15
    IADD r0, 8
    LDP r1, r0
    MOV r0, r15
    IADD r0, 14
    LDP r2, r0
    POP r0
    MOV r2, r15
    IADD r2, 10
    LDP r1, r2
    MOV r8, r1
    MOV r2, r15
    IADD r2, 2
    LDP r1, r2
    MOV r2, r8
    ADD r1, r2
    MOV r0, r1
    SETSP r15
    MOV r6, r15
    LDI r7, 16
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL
.GLOBAL
_func_make_point:
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
    CALL SYS_MEM_MOVE
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
.ENDREGION
