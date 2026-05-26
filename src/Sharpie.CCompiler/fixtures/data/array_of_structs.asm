; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: array_of_structs.c
; ----------------------------------

.REGION FIXED

; Global Variables
.GLOBAL
_global_gverts:
    .DB 1
    .PAD 1
    .DW 10
    .DW 20
    .DB 2
    .PAD 1
    .DW -30
    .DW 40
.ENDGLOBAL

.GLOBAL

Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 12
    SETSP r6
    MOV r15, r6
    LDI r2, 3
    ALT STA r2, r15
    LDI r2, 5
    MOV r3, r6
    IADD r3, 2
    STA r2, r3
    LDI r2, 7
    MOV r3, r6
    IADD r3, 4
    STA r2, r3
    LDI r2, 4
    MOV r3, r6
    IADD r3, 6
    ALT STA r2, r3
    LDI r2, 65527
    MOV r3, r6
    IADD r3, 8
    STA r2, r3
    LDI r2, 11
    MOV r3, r6
    IADD r3, 10
    STA r2, r3
    LDI r2, _global_gverts
    XOR r3, r3
    LDI r4, 6
    MUL r3, r4
    ADD r2, r3
    ALT LDP r1, r2
    LDI r3, _global_gverts
    IADD r3, 8
    LDP r2, r3
    ADD r1, r2
    LDI r3, _global_gverts
    IADD r3, 10
    LDP r2, r3
    ADD r1, r2
    MOV r3, r6
    XOR r4, r4
    LDI r5, 6
    MUL r4, r5
    ADD r3, r4
    ALT LDP r2, r3
    ADD r1, r2
    MOV r3, r6
    XOR r4, r4
    LDI r5, 6
    MUL r4, r5
    ADD r3, r4
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r3, r6
    XOR r4, r4
    LDI r5, 6
    MUL r4, r5
    ADD r3, r4
    IADD r3, 4
    LDP r2, r3
    ADD r1, r2
    MOV r3, r6
    IADD r3, 6
    ALT LDP r2, r3
    ADD r1, r2
    MOV r3, r6
    IADD r3, 8
    LDP r2, r3
    ADD r1, r2
    MOV r3, r6
    IADD r3, 10
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
epilogue_L0:
    MOV r6, r15
    IADD r6, 12
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL

.ENDREGION

