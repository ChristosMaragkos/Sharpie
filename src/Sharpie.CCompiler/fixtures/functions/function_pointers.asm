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
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 6
    SETSP r6
    MOV r15, r6
    LDI r1, _func_add
    MOV r2, r6
    XOR r3, r3
    LDI r4, 2
    MUL r3, r4
    ADD r2, r3
    STA r1, r2
    LDI r1, _func_sub
    MOV r2, r6
    IADD r2, 2
    STA r1, r2
    LDI r2, _func_add
    LDI r3, 10
    LDI r4, 5
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_do_math
    MOV r1, r0
    MOV r0, r15
    IADD r0, 4
    STA r1, r0
    MOV r3, r15
    IADD r3, 2
    LDP r2, r3
    LDI r3, 10
    LDI r4, 5
    MOV r1, r2
    MOV r2, r3
    MOV r3, r4
    CALL _func_do_math
epilogue_L0:
    MOV r6, r15
    IADD r6, 6
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL

.GLOBAL

_func_add:
    ADD r1, r2
    MOV r0, r1
epilogue_L1:
    RET
.ENDGLOBAL

.GLOBAL

_func_sub:
    SUB r1, r2
    MOV r0, r1
epilogue_L2:
    RET
.ENDGLOBAL

.GLOBAL

_func_do_math:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 2
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    STA r1, r15
    MOV r1, r2
    MOV r2, r3
    LDP r0, r15
    ALT CALL r0
epilogue_L3:
    MOV r6, r15
    IADD r6, 2
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL

.ENDREGION

