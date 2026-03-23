; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: if_else.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    LDI r1, 10
    ICMP r1, 10
    JNE else_L2
    LDI r0, 1
    JMP epilogue_L0
else_L2:
    XOR r0, r0
    JMP epilogue_L0
if_L1:
epilogue_L0:
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION

