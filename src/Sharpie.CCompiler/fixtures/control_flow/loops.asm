; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: loops.c
; ----------------------------------

.REGION FIXED
.GLOBAL

Main:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r11
    PUSH r12
    PUSH r13
    XOR r11, r11
    XOR r10, r10
for_start_L1:
    ICMP r10, 10
    JGE for_end_L3
    INC r11
for_inc_L2:
    INC r10
    JMP for_start_L1
for_end_L3:
    XOR r12, r12
    XOR r8, r8
while_start_L4:
    ICMP r8, 9
    JGE while_end_L5
    ADD r12, r8
    INC r8
    JMP while_start_L4
while_end_L5:
    LDI r13, 1000
    XOR r9, r9
do_start_L6:
    SUB r13, r9
    INC r9
do_cond_L7:
    ICMP r9, 10
    JLT do_start_L6
do_end_L8:
    XOR r0, r0
epilogue_L0:
    POP r13
    POP r12
    POP r11
    POP r10
    POP r9
    POP r8
    HALT
.ENDGLOBAL

.ENDREGION

