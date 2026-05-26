; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: alloca_and_fp.c
; ----------------------------------

.REGION FIXED
.GLOBAL

Main:
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 2
    SETSP r6
    MOV r15, r6
    LDI r9, 10
    STA r1, r15
    LDI r1, 100
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 42
    STA r1, r0
    MOV r1, r9
    IADD r1, 20
    MOV r2, r0
    LDP r2, r2
    ADD r1, r2
    MOV r0, r1
epilogue_L0:
    MOV r6, r15
    IADD r6, 2
    SETSP r6
    POP r15
    POP r9
    HALT
.ENDGLOBAL

.ENDREGION

