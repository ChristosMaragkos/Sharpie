; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: structs.c
; ----------------------------------

.REGION FIXED
.GLOBAL

Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 4
    SETSP r6
    MOV r15, r6
    LDI r1, 20
    MOV r2, r6
    IADD r2, 2
    STA r1, r2
    LDI r1, 30
    STA r1, r15
    LDP r2, r2
    ADD r1, r2
    MOV r0, r1
epilogue_L0:
    MOV r6, r15
    IADD r6, 4
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL

.ENDREGION

