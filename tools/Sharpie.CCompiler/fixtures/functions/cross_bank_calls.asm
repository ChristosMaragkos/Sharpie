.REGION FIXED
.GLOBAL
Main:
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
    MOV r0, r15
    STA r1, r0
    LDI r2, 42
    MOV r1, r2
    PUSH r13
    PUSH r14
    LDI r14, 1
    LDI r13, _func_fetch_enemy_sprite
    CALL SYS_FAR_CALL
    POP r14
    POP r13
    PUSH r0
    MOV r0, r15
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r8, r1
    LDI r1, _func_calculate_path
    MOV r9, r1
    MOV r2, r9
    MOV r6, r15
    IADD r6, 2
    STA r2, r6
    MOV r0, r15
    STA r1, r0
    LDI r2, 10
    LDI r3, 20
    MOV r1, r2
    MOV r2, r3
    PUSH r13
    PUSH r14
    LDI r14, 2
    MOV r6, r15
    IADD r6, 2
    LDP r13, r6
    CALL SYS_FAR_CALL
    POP r14
    POP r13
    PUSH r0
    MOV r0, r15
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r10, r1
    MOV r1, r8
    MOV r2, r10
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
    HALT
.ENDGLOBAL
.ENDREGION
