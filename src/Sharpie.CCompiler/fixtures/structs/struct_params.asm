; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: struct_params.c
; ----------------------------------

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
    LDI r1, 10
    MOV r2, r6
    IADD r2, 4
    STA r1, r2
    LDI r1, 20
    MOV r2, r6
    IADD r2, 6
    STA r1, r2
    MOV r0, r6
    IADD r0, 8
    STA r1, r0
    LDI r2, 5
    MOV r3, r6
    IADD r3, 4
    LDP r4, r3
    IADD r3, 2
    LDP r5, r3
    MOV r1, r2
    MOV r2, r4
    MOV r3, r5
    CALL _func_test_registers
    MOV r8, r0
    LDI r1, 100
    STA r1, r15
    LDI r1, 200
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    MOV r0, r15
    IADD r0, 8
    STA r1, r0
    MOV r1, r15
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
    MOV r9, r0
    MOV r1, r15
    CALL _func_test_pointer
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    LDP r2, r15
    ADD r1, r2
    MOV r0, r1
epilogue_L0:
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
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r1, r8
    LDP r2, r15
    ADD r1, r2
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
epilogue_L1:
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
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r8, r1
    MOV r9, r2
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r2, r3
    ADD r1, r2
    LDP r2, r15
    ADD r1, r2
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
epilogue_L2:
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r9
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_test_pointer:
    PUSH r8
    MOV r8, r1
    LDI r1, 30
    STA r1, r8
epilogue_L3:
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

