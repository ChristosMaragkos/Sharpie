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
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r8, 5
    STA r1, r15
    MOV r2, r8
    MOV r1, r2
    CALL _func_square
    PUSH r0
    LDP r1, r15
    POP r0
    MOV r1, r0
    MOV r9, r1
    MOV r0, r9
epilogue_L0:
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r9
    POP r8
    HALT
.ENDGLOBAL
.GLOBAL
_func_square:
    PUSH r8
    MOV r8, r1
    MOV r2, r8
    MUL r1, r2
    MOV r0, r1
epilogue_L1:
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

