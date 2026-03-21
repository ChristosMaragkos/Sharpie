.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    PUSH r10
    PUSH r11
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 2
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r8, str_L0
    LDI r9, str_L1
    LDI r10, str_L0
    LDI r1, str_L2
    XOR r2, r2
    XOR r3, r3
    CALL SYS_PRINT
    MOV r0, r15
    STA r1, r0
    LDI r1, 20
    CALL SYS_ALLOC_STACKFRAME
    PUSH r0
    MOV r0, r15
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r11, r1
    LDI r1, 97
    MOV r2, r11
    XOR r3, r3
    ADD r2, r3
    ALT STA r1, r2
    XOR r1, r1
    MOV r2, r11
    LDI r3, 19
    ADD r2, r3
    ALT STA r1, r2
    MOV r1, r11
    XOR r2, r2
    XOR r3, r3
    CALL SYS_PRINT
    XOR r0, r0
    SETSP r15
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r11
    POP r10
    POP r9
    POP r8
    HALT
.ENDGLOBAL
; Readonly Data
str_L0:
    .DB "Hello from Sharpie", 0
str_L1:
    .DB "This is a compiler test", 0
str_L2:
    .DB "Something in the way", 0
.ENDREGION
