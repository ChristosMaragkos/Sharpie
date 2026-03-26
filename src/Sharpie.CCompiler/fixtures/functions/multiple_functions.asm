; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: multiple_functions.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    LDI r0, 1
epilogue_L0:
    HALT
.ENDGLOBAL
.GLOBAL
_func_helper:
    LDI r0, 42
epilogue_L1:
    RET
.ENDGLOBAL
.ENDREGION

