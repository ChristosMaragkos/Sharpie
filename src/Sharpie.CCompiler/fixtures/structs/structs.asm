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
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, 10
    STA r1, r15
    LDI r1, 20
    MOV r2, r15
    IADD r2, 2
    STA r1, r2
    MOV r1, r15
    MOV r8, r1
    LDI r1, 30
    STA r1, r8
    LDP r1, r15
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    ADD r1, r2
    MOV r0, r1
epilogue_L0:
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION

