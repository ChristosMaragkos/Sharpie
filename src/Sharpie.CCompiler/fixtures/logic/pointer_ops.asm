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
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 2
    SETSP r6
    MOV r15, r6
    LDI r1, 500
    STA r1, r15
    LDI r8, 1000
    LDI r1, 42
    STA r1, r8
    MOV r1, r8
    LDP r1, r1
    MOV r0, r1
epilogue_L0:
    MOV r6, r15
    IADD r6, 2
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL

.ENDREGION

