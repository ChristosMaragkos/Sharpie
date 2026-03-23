; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: unions.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, 258
    STA r1, r15
    ALT LDP r1, r15
    MOV r8, r1
    MOV r2, r15
    INC r2
    ALT LDP r1, r2
    MOV r9, r1
    LDI r1, 10
    ALT STA r1, r15
    LDP r0, r15
epilogue_L0:
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r9
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION

