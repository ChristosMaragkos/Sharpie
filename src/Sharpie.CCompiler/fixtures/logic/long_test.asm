; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: long_test.c
; ----------------------------------

.REGION FIXED
; Global Variables
.GLOBAL
_global_global_long:
    .DW 34464
    .DW 1
.ENDGLOBAL
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 16
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r15
    LDI r2, 34464
    STA r2, r1
    IADD r1, 2
    LDI r2, 1
    STA r2, r1
    MOV r1, r15
    IADD r1, 8
    LDI r2, 3392
    STA r2, r1
    IADD r1, 2
    LDI r2, 3
    STA r2, r1
    MOV r1, r15
    IADD r1, 4
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    MOV r6, r15
    IADD r6, 8
    LDP r4, r6
    IADD r6, 2
    LDP r5, r6
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 12
    MOV r4, r15
    IADD r4, 4
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    MOV r6, r15
    LDP r4, r6
    IADD r6, 2
    LDP r5, r6
    SUB r2, r4
    ALT SUB r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r2, r15
    IADD r2, 12
    LDP r1, r2
    IADD r2, 2
    LDP r3, r2
    INC r1
    ALT ADD r3, 0
    STA r3, r2
    IADD r2, -2
    STA r1, r2
    MOV r1, r15
    IADD r1, 4
    MOV r2, r15
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L2
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L2:
    JLE if_L1
    LDI r0, 1
    JMP epilogue_L0
if_L1:
    MOV r1, r15
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 2
    XOR r5, r5
    MOV r6, r2
    MUL r2, r4
    MOV r7, r6
    ALT MUL r7, r4
    MUL r6, r5
    ADD r7, r6
    MOV r6, r3
    MUL r6, r4
    ADD r7, r6
    MOV r3, r7
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    XOR r0, r0
epilogue_L0:
    MOV r6, r15
    LDI r7, 16
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_add_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 4
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L3:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_sub_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 4
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    SUB r1, r3
    ALT SUB r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L4:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_mul_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r5, r15
    LDP r1, r5
    IADD r5, 2
    LDP r2, r5
    MOV r5, r15
    IADD r5, 4
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    MOV r5, r1
    MUL r1, r3
    MOV r6, r5
    ALT MUL r6, r3
    MUL r5, r4
    ADD r6, r5
    MOV r5, r2
    MUL r5, r3
    ADD r6, r5
    STA r1, r8
    IADD r8, 2
    STA r6, r8
epilogue_L5:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_test_incdec:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r15
    LDI r2, 34464
    STA r2, r1
    IADD r1, 2
    LDI r2, 1
    STA r2, r1
    MOV r2, r15
    LDP r1, r2
    IADD r2, 2
    LDP r3, r2
    INC r1
    ALT ADD r3, 0
    STA r3, r2
    IADD r2, -2
    STA r1, r2
    MOV r1, r15
    IADD r1, 4
    MOV r2, r15
    MOV r3, r2
    MOV r4, r1
    LDP r5, r3
    STA r5, r4
    IADD r3, 2
    IADD r4, 2
    LDP r5, r3
    STA r5, r4
epilogue_L6:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    RET
.ENDGLOBAL
.ENDREGION

