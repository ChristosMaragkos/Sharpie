; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: arrays.c
; ----------------------------------

.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 6
    SETSP r6
    MOV r15, r6
    MOV r1, r6
    LDI r2, 3
    CALL _func_fill_array
    MOV r1, r15
    IADD r1, 4
    LDP r0, r1
epilogue_L0:
    MOV r6, r15
    IADD r6, 6
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_fill_array:
    PUSH r8
    PUSH r9
    PUSH r10
    MOV r9, r1
    MOV r10, r2
    XOR r8, r8
while_start_L2:
    CMP r8, r10
    JGE while_end_L3
    MOV r1, r8
    IMUL r1, 10
    MOV r2, r9
    MOV r3, r8
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    STA r1, r2
    INC r8
    JMP while_start_L2
while_end_L3:
epilogue_L1:
    POP r10
    POP r9
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

