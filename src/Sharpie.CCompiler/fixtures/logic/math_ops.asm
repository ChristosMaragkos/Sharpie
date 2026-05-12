; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: math_ops.c
; ----------------------------------

.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    LDI r8, 5
    LDI r9, 3
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    DEC r1
    LDI r2, 2
    MUL r1, r2
    IDIV r1, 3
    IMOD r1, 4
    IAND r1, 7
    IOR r1, 8
    IXOR r1, 2
    LDI r2, 1
    SHL r1, r2
    LDI r2, 2
    SHR r1, r2
    MOV r8, r1
    MOV r0, r8
    NEG r0
epilogue_L0:
    POP r9
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION

