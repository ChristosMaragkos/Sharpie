; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: stack_frame.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 14
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r15
    LDI r2, 1
    STA r2, r1
    LDI r2, 2
    MOV r3, r1
    IADD r3, 2
    STA r2, r3
    LDI r2, 3
    MOV r3, r1
    IADD r3, 4
    STA r2, r3
    LDI r2, 4
    MOV r3, r1
    IADD r3, 6
    STA r2, r3
    LDI r2, 5
    MOV r3, r1
    IADD r3, 8
    STA r2, r3
    LDI r2, 6
    MOV r3, r1
    IADD r3, 10
    STA r2, r3
    LDI r2, 7
    MOV r3, r1
    IADD r3, 12
    STA r2, r3
    MOV r1, r15
    CALL _func_do_stuff
epilogue_L0:
    MOV r6, r15
    LDI r7, 14
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_do_stuff:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 10
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r1, r15
    LDI r2, 9
    ALT STA r2, r1
    LDI r2, 10
    MOV r3, r1
    INC r3
    ALT STA r2, r3
    LDI r2, 11
    MOV r3, r1
    IADD r3, 2
    ALT STA r2, r3
    LDI r2, 12
    MOV r3, r1
    IADD r3, 3
    ALT STA r2, r3
    LDI r2, 13
    MOV r3, r1
    IADD r3, 4
    ALT STA r2, r3
    LDI r2, 14
    MOV r3, r1
    IADD r3, 5
    ALT STA r2, r3
    LDI r2, 15
    MOV r3, r1
    IADD r3, 6
    ALT STA r2, r3
    LDI r2, 16
    MOV r3, r1
    IADD r3, 7
    ALT STA r2, r3
    MOV r0, r15
    IADD r0, 8
    STA r1, r0
    LDI r1, 8
    CALL SYS_ALLOC_STACKFRAME
    PUSH r0
    MOV r0, r15
    IADD r0, 8
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r9, r1
    LDI r1, 99
    ALT STA r1, r15
    LDP r1, r8
    MOV r3, r15
    INC r3
    ALT LDP r2, r3
    ADD r1, r2
    MOV r0, r1
epilogue_L1:
    MOV r6, r15
    LDI r7, 10
    ADD r6, r7
    SETSP r6
    POP r15
    POP r9
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

