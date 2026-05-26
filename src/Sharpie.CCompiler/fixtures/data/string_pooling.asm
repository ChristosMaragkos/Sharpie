; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: string_pooling.c
; ----------------------------------

.REGION FIXED

; Global Variables
.GLOBAL
_global_str:
    .DW str_L0
.ENDGLOBAL

.GLOBAL

Main:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 16
    SETSP r6
    MOV r15, r6
    LDM r1, _global_str
    XOR r2, r2
    XOR r3, r3
    CALL SYS_PRINT
    LDI r1, str_L2
    XOR r2, r2
    LDI r3, 1
    CALL SYS_PRINT
    LDI r1, 90
    MOV r2, r8
    XOR r3, r3
    ADD r2, r3
    ALT STA r1, r2
    MOV r1, r15
    LDI r2, str_L2
    LDI r3, 16
    CALL SYS_MEM_COPY
    MOV r1, r15
    XOR r2, r2
    LDI r3, 2
    CALL SYS_PRINT
    XOR r0, r0
epilogue_L1:
    MOV r6, r15
    IADD r6, 16
    SETSP r6
    POP r15
    POP r8
    HALT
.ENDGLOBAL


; Readonly Data
.GLOBAL
str_L0:
    .DB "This should be shared", 0
str_L2:
    .DB "This should not", 0
.ENDGLOBAL
.ENDREGION

