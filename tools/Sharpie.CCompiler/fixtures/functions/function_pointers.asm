; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: function_pointers.c
; ----------------------------------
.REGION FIXED
.GLOBAL
Main:
    PUSH r8
    PUSH r9
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 6
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, _func_add
    MOV r2, r15
    XOR r3, r3
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    STA r1, r2
    LDI r1, _func_sub
    MOV r2, r15
    LDI r3, 2
    ADD r2, r3
    STA r1, r2
    MOV r0, r15
    IADD r0, 4
    STA r1, r0
    LDI r2, _func_add
    LDI r3, 10
    LDI r4, 5
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_do_math
    PUSH r0
    MOV r0, r15
    IADD r0, 4
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r8, r1
    MOV r0, r15
    IADD r0, 4
    STA r1, r0
    MOV r3, r15
    LDI r4, 2
    ADD r3, r4
    LDP r2, r3
    LDI r3, 10
    LDI r4, 5
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_do_math
    PUSH r0
    MOV r0, r15
    IADD r0, 4
    LDP r1, r0
    POP r0
    MOV r1, r0
    MOV r9, r1
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r0, r1
epilogue_L0:
    MOV r6, r15
    LDI r7, 6
    ADD r6, r7
    SETSP r6
    POP r15
    POP r9
    POP r8
    HALT
.ENDGLOBAL
.GLOBAL
_func_add:
    PUSH r8
    PUSH r9
    MOV r8, r1
    MOV r9, r2
    MOV r1, r8
    MOV r2, r9
    ADD r1, r2
    MOV r0, r1
epilogue_L1:
    POP r9
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_sub:
    PUSH r8
    PUSH r9
    MOV r8, r1
    MOV r9, r2
    MOV r1, r8
    MOV r2, r9
    SUB r1, r2
    MOV r0, r1
epilogue_L2:
    POP r9
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_do_math:
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
    MOV r8, r1
    MOV r9, r2
    MOV r10, r3
    MOV r1, r8
    STA r1, r15
    MOV r1, r9
    MOV r2, r10
    LDP r0, r15
    ALT CALL r0
epilogue_L3:
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    POP r10
    POP r9
    POP r8
    RET
.ENDGLOBAL
.ENDREGION

