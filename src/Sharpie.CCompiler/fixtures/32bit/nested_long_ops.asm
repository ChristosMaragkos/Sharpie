; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: nested_long_ops.c
; ----------------------------------

.REGION FIXED
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 46
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r6
    LDI r2, 34464
    STA r2, r1
    IADD r1, 2
    LDI r2, 1
    STA r2, r1
    MOV r1, r6
    IADD r1, 4
    LDI r2, 3392
    STA r2, r1
    IADD r1, 2
    LDI r2, 3
    STA r2, r1
    MOV r1, r6
    IADD r1, 8
    LDI r2, 37856
    STA r2, r1
    IADD r1, 2
    LDI r2, 4
    STA r2, r1
    MOV r1, r6
    IADD r1, 12
    LDI r2, 6784
    STA r2, r1
    IADD r1, 2
    LDI r2, 6
    STA r2, r1
    MOV r1, r6
    IADD r1, 16
    MOV r0, r6
    IADD r0, 32
    STA r1, r0
    MOV r2, r6
    IADD r2, 12
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    IADD r2, 8
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    IADD r2, 4
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    LDP r3, r2
    IADD r2, 2
    LDP r4, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_nested_add
    LDI r1, 12
    CALL SYS_FREE_STACKFRAME
    MOV r1, r15
    IADD r1, 20
    MOV r0, r15
    IADD r0, 32
    STA r1, r0
    MOV r2, r15
    IADD r2, 12
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    IADD r2, 8
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    IADD r2, 4
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    LDP r3, r2
    IADD r2, 2
    LDP r4, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_nested_mul
    LDI r1, 12
    CALL SYS_FREE_STACKFRAME
    MOV r1, r15
    IADD r1, 24
    MOV r0, r15
    IADD r0, 32
    STA r1, r0
    MOV r2, r15
    IADD r2, 12
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    IADD r2, 8
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    IADD r2, 4
    MOV r1, r2
    LDI r2, 4
    CALL SYS_STACKALLOC
    MOV r2, r15
    LDP r3, r2
    IADD r2, 2
    LDP r4, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_nested_mixed
    LDI r1, 12
    CALL SYS_FREE_STACKFRAME
    MOV r1, r15
    IADD r1, 28
    MOV r0, r15
    IADD r0, 32
    STA r1, r0
    MOV r2, r15
    LDP r3, r2
    IADD r2, 2
    LDP r4, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_deep_nest
    MOV r2, r15
    IADD r2, 34
    MOV r0, r15
    IADD r0, 42
    MOV r5, r15
    IADD r5, 16
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    MOV r7, r15
    IADD r7, 20
    LDP r5, r7
    IADD r7, 2
    LDP r6, r7
    ADD r3, r5
    ALT ADD r4, r6
    STA r3, r0
    IADD r0, 2
    STA r4, r0
    MOV r0, r15
    IADD r0, 42
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    MOV r7, r15
    IADD r7, 24
    LDP r5, r7
    IADD r7, 2
    LDP r6, r7
    ADD r3, r5
    ALT ADD r4, r6
    STA r3, r0
    IADD r0, 2
    STA r4, r0
    MOV r0, r15
    IADD r0, 38
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    MOV r7, r15
    IADD r7, 28
    LDP r5, r7
    IADD r7, 2
    LDP r6, r7
    ADD r3, r5
    ALT ADD r4, r6
    STA r3, r2
    IADD r2, 2
    STA r4, r2
    MOV r0, r15
    IADD r0, 34
epilogue_L0:
    MOV r6, r15
    LDI r7, 46
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_nested_add:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 40
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 46
    MOV r5, r15
    IADD r5, 4
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r6, r15
    IADD r6, 50
    MOV r5, r15
    IADD r5, 8
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r6, r15
    IADD r6, 54
    MOV r5, r15
    IADD r5, 12
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r0, r15
    IADD r0, 20
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 4
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 24
    MOV r3, r15
    IADD r3, 8
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 12
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 20
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 24
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 32
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 8
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 36
    MOV r3, r15
    IADD r3, 4
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 12
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 32
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 36
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 16
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 28
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L4:
    MOV r6, r15
    LDI r7, 40
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_nested_mul:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 40
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 46
    MOV r5, r15
    IADD r5, 4
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r6, r15
    IADD r6, 50
    MOV r5, r15
    IADD r5, 8
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r6, r15
    IADD r6, 54
    MOV r5, r15
    IADD r5, 12
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r5, r15
    LDP r1, r5
    IADD r5, 2
    LDP r2, r5
    MOV r5, r15
    IADD r5, 4
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    MOV r5, r1
    MUL r1, r3
    MOV r0, r5
    ALT MUL r0, r3
    MUL r5, r4
    ADD r0, r5
    MOV r5, r2
    MUL r5, r3
    ADD r0, r5
    MOV r2, r0
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r5, r15
    IADD r5, 8
    LDP r1, r5
    IADD r5, 2
    LDP r2, r5
    MOV r5, r15
    IADD r5, 12
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    MOV r5, r1
    MUL r1, r3
    MOV r0, r5
    ALT MUL r0, r3
    MUL r5, r4
    ADD r0, r5
    MOV r5, r2
    MUL r5, r3
    ADD r0, r5
    MOV r2, r0
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 20
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 24
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r5, r15
    LDP r1, r5
    IADD r5, 2
    LDP r2, r5
    MOV r5, r15
    IADD r5, 8
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    MOV r5, r1
    MUL r1, r3
    MOV r0, r5
    ALT MUL r0, r3
    MUL r5, r4
    ADD r0, r5
    MOV r5, r2
    MUL r5, r3
    ADD r0, r5
    MOV r2, r0
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r5, r15
    IADD r5, 4
    LDP r1, r5
    IADD r5, 2
    LDP r2, r5
    MOV r5, r15
    IADD r5, 12
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    MOV r5, r1
    MUL r1, r3
    MOV r0, r5
    ALT MUL r0, r3
    MUL r5, r4
    ADD r0, r5
    MOV r5, r2
    MUL r5, r3
    ADD r0, r5
    MOV r2, r0
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 32
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 36
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 16
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 28
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    MOV r5, r1
    MUL r1, r3
    MOV r0, r5
    ALT MUL r0, r3
    MUL r5, r4
    ADD r0, r5
    MOV r5, r2
    MUL r5, r3
    ADD r0, r5
    MOV r2, r0
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L11:
    MOV r6, r15
    LDI r7, 40
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_nested_mixed:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 40
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 46
    MOV r5, r15
    IADD r5, 4
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r6, r15
    IADD r6, 50
    MOV r5, r15
    IADD r5, 8
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r6, r15
    IADD r6, 54
    MOV r5, r15
    IADD r5, 12
    LDP r7, r6
    STA r7, r5
    IADD r6, 2
    IADD r5, 2
    LDP r7, r6
    STA r7, r5
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r0, r15
    IADD r0, 20
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 4
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 24
    MOV r3, r15
    IADD r3, 8
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 12
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    SUB r1, r3
    ALT SUB r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 20
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 24
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    MOV r5, r1
    MUL r1, r3
    MOV r0, r5
    ALT MUL r0, r3
    MUL r5, r4
    ADD r0, r5
    MOV r5, r2
    MUL r5, r3
    ADD r0, r5
    MOV r2, r0
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 32
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 8
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    SUB r1, r3
    ALT SUB r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 36
    MOV r3, r15
    IADD r3, 4
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 12
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 32
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 36
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    MOV r5, r1
    MUL r1, r3
    MOV r0, r5
    ALT MUL r0, r3
    MUL r5, r4
    ADD r0, r5
    MOV r5, r2
    MUL r5, r3
    ADD r0, r5
    MOV r2, r0
    STA r1, r0
    IADD r0, 2
    STA r2, r0
    MOV r0, r15
    IADD r0, 16
    LDP r1, r0
    IADD r0, 2
    LDP r2, r0
    MOV r0, r15
    IADD r0, 28
    LDP r3, r0
    IADD r0, 2
    LDP r4, r0
    ADD r1, r3
    ALT ADD r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L18:
    MOV r6, r15
    LDI r7, 40
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_deep_nest:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 52
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r0, r15
    IADD r0, 20
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    LDI r4, 1
    XOR r5, r5
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 20
    LDP r2, r0
    IADD r0, 2
    LDP r3, r0
    LDI r4, 2
    XOR r5, r5
    MOV r6, r2
    MUL r2, r4
    MOV r0, r6
    ALT MUL r0, r4
    MUL r6, r5
    ADD r0, r6
    MOV r6, r3
    MUL r6, r4
    ADD r0, r6
    MOV r3, r0
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 28
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    LDI r4, 3
    XOR r5, r5
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 28
    LDP r2, r0
    IADD r0, 2
    LDP r3, r0
    LDI r4, 4
    XOR r5, r5
    MOV r6, r2
    MUL r2, r4
    MOV r0, r6
    ALT MUL r0, r4
    MUL r6, r5
    ADD r0, r6
    MOV r6, r3
    MUL r6, r4
    ADD r0, r6
    MOV r3, r0
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 16
    LDP r2, r0
    IADD r0, 2
    LDP r3, r0
    MOV r0, r15
    IADD r0, 24
    LDP r4, r0
    IADD r0, 2
    LDP r5, r0
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 40
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    LDI r4, 5
    XOR r5, r5
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 40
    LDP r2, r0
    IADD r0, 2
    LDP r3, r0
    LDI r4, 6
    XOR r5, r5
    MOV r6, r2
    MUL r2, r4
    MOV r0, r6
    ALT MUL r0, r4
    MUL r6, r5
    ADD r0, r6
    MOV r6, r3
    MUL r6, r4
    ADD r0, r6
    MOV r3, r0
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 48
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    LDI r4, 7
    XOR r5, r5
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 48
    LDP r2, r0
    IADD r0, 2
    LDP r3, r0
    LDI r4, 8
    XOR r5, r5
    MOV r6, r2
    MUL r2, r4
    MOV r0, r6
    ALT MUL r0, r4
    MUL r6, r5
    ADD r0, r6
    MOV r6, r3
    MUL r6, r4
    ADD r0, r6
    MOV r3, r0
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 36
    LDP r2, r0
    IADD r0, 2
    LDP r3, r0
    MOV r0, r15
    IADD r0, 44
    LDP r4, r0
    IADD r0, 2
    LDP r5, r0
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 12
    LDP r2, r0
    IADD r0, 2
    LDP r3, r0
    MOV r0, r15
    IADD r0, 32
    LDP r4, r0
    IADD r0, 2
    LDP r5, r0
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r0
    IADD r0, 2
    STA r3, r0
    MOV r0, r15
    IADD r0, 8
    LDP r2, r0
    IADD r0, 2
    LDP r1, r0
    XOR r3, r3
    LDI r4, 2
    XOR r0, r0
    PUSH r0
    PUSH r1
    CALL SYS_DIV_32
    POP r0
    POP r0
    MOV r1, r15
    IADD r1, 4
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    STA r2, r8
    IADD r8, 2
    STA r3, r8
epilogue_L25:
    MOV r6, r15
    LDI r7, 52
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

