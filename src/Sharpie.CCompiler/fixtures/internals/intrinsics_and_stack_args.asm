; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: intrinsics_and_stack_args.c
; ----------------------------------

.REGION FIXED
.GLOBAL

Main:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 2
    SETSP r6
    MOV r15, r6
    STA r1, r15
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
    MOV r1, r0
    MOV r8, r0
    XOR r1, r1
    CLS r1
    LDI r1, 10
    LDI r2, 20
    LDI r3, 5
    LDI r4, 529
    DRAW r1, r2, r3, r4
    MOV r0, r8
epilogue_L0:
    MOV r6, r15
    IADD r6, 2
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL

.GLOBAL

_func_add_six_numbers:
    PUSH r8
    PUSH r12
    PUSH r15
    GETSP r15
    MOV r6, r15
    IADD r6, 14
    LDP r7, r6
    MOV r12, r7
    MOV r6, r15
    IADD r6, 16
    LDP r7, r6
    MOV r8, r1
    ADD r1, r2
    ADD r1, r3
    ADD r1, r4
    ADD r1, r12
    ADD r1, r7
    MOV r0, r1
epilogue_L1:
    POP r15
    POP r12
    POP r8
    RET
.ENDGLOBAL

.GLOBAL

_func_test_memory:
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 2
    SETSP r6
    MOV r15, r6
    STA r1, r15
    LDI r1, 20
    CALL SYS_ALLOC_STACKFRAME
    MOV r1, r0
    LDI r2, 255
    LDI r3, 20
    CALL SYS_MEM_SET
    LDI r0, 1
epilogue_L2:
    MOV r6, r15
    IADD r6, 2
    SETSP r6
    POP r15
    RET
.ENDGLOBAL

.ENDREGION

