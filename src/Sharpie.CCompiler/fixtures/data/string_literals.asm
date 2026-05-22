; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: string_literals.c
; ----------------------------------

.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, str_L3
    XOR r2, r2
    XOR r3, r3
    CALL SYS_PRINT
    STA r1, r15
    LDI r1, 20
    CALL SYS_ALLOC_STACKFRAME
    LDI r1, 97
    MOV r2, r0
    XOR r3, r3
    ADD r2, r3
    ALT STA r1, r2
    XOR r1, r1
    MOV r2, r0
    LDI r3, 19
    ADD r2, r3
    ALT STA r1, r2
    MOV r1, r0
    XOR r2, r2
    XOR r3, r3
    CALL SYS_PRINT
    XOR r0, r0
epilogue_L0:
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL

; Readonly Data
.GLOBAL
str_L1:
    .DB "Hello from Sharpie", 0
str_L2:
    .DB "This is a compiler test", 0
str_L3:
    .DB "Something in the way", 0
.ENDGLOBAL
.ENDREGION

