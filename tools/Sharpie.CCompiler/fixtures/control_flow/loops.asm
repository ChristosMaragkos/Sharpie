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
    XOR r1, r1
    MOV r11, r1
    XOR r1, r1
    MOV r10, r1
for_start_L1:
    MOV r1, r10
    ICMP r1, 10
    JGE for_end_L3
    INC r11
for_inc_L2:
    MOV r1, r10
    INC r10
    JMP for_start_L1
for_end_L3:
    XOR r1, r1
    MOV r12, r1
    XOR r1, r1
    MOV r8, r1
while_start_L4:
    MOV r1, r8
    ICMP r1, 9
    JGE while_end_L5
    MOV r1, r12
    MOV r2, r8
    ADD r1, r2
    MOV r12, r1
    INC r8
    JMP while_start_L4
while_end_L5:
    LDI r13, 1000
    XOR r1, r1
    MOV r9, r1
do_start_L6:
    MOV r1, r13
    MOV r2, r9
    SUB r1, r2
    MOV r13, r1
    INC r9
do_cond_L7:
    MOV r1, r9
    ICMP r1, 10
    JLT do_start_L6
do_end_L8:
    XOR r0, r0
    HALT
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

