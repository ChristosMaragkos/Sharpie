; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: function_calls.c
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
    LDI r8, 5
    STA r1, r15
    MOV r2, r8
    MOV r1, r8
    CALL _func_square
epilogue_L0:
    MOV r6, r15
    IADD r6, 2
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL
.GLOBAL
_func_square:
    MOV r2, r1
    MUL r1, r2
    MOV r0, r1
epilogue_L1:
    RET
.ENDGLOBAL
.ENDREGION

