; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: inline_asm.c
; ----------------------------------
.REGION FIXED
; This block should exist outside of the method.
.GLOBAL
Main:
    XOR r1, r1
    XOR r0, r0
epilogue_L0:
    HALT
.ENDGLOBAL
.ENDREGION

