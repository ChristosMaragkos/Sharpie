; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: return_42.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    LDI r0, 42
epilogue_L0:
    HALT
.ENDGLOBAL
.ENDREGION

