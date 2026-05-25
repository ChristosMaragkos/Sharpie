; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: vram_access.c
; ----------------------------------

.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    LDI r1, 3
    BLITMODE r1
    LDI r9, 1
    XOR r8, r8
    LDI r1, 32639
    MOV r2, r9
    STV r2, r1
while_start_L1:
    MOV r1, r8
    ICMP r1, 0
    JNE while_end_L2
    XOR r1, r1
    INPUT r1, r0
    MOV r8, r0
    IAND r8, 255
    JMP while_start_L1
while_end_L2:
    XOR r0, r0
epilogue_L0:
    POP r9
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION

