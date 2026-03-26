; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: ternary.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    STA r1, r6
    LDI r1, 2
    ALT RND r0, r1
    PUSH r0
    LDP r1, r15
    POP r0
    MOV r1, r0
    MOV r8, r1
    ICMP r1, 0
    JNE ternary_false_L1
    LDI r0, 69
    JMP ternary_end_L2
ternary_false_L1:
    LDI r0, 420
ternary_end_L2:
epilogue_L0:
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION

