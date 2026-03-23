; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: pointer_ops.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, 500
    STA r1, r15
    MOV r1, r15
    MOV r10, r1
    LDI r8, 1000
    LDI r1, 42
    STA r1, r8
    MOV r9, r1
    MOV r0, r9
epilogue_L0:
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r10
    POP r9
    POP r8
    HALT
.ENDGLOBAL
.ENDREGION

