; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: cross_bank_calls.c
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
    LDI r1, 42
    PUSH r13
    PUSH r14
    LDI r14, 1
    LDI r13, _func_fetch_enemy_sprite
    CALL SYS_FAR_CALL
    POP r14
    POP r13
    LDI r2, _func_calculate_path
    MOV r6, r15
    IADD r6, 2
    STA r2, r6
    LDI r2, 10
    LDI r3, 20
    MOV r1, r2
    MOV r2, r3
    PUSH r13
    PUSH r14
    LDI r14, 2
    MOV r6, r15
    IADD r6, 2
    LDP r13, r6
    CALL SYS_FAR_CALL
    POP r14
    POP r13
    MOV r1, r0
    STA r1, r15
    MOV r0, r15
    IADD r0, 4
    STA r2, r0
    PUSH r13
    PUSH r14
    LDI r14, 3
    LDI r13, _func_do_stuff
    CALL SYS_FAR_CALL
    POP r14
    POP r13
    LDP r1, r15
    ADD r1, r0
    MOV r0, r1
epilogue_L0:
    MOV r6, r15
    IADD r6, 6
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL

.ENDREGION
.REGION BANK_1
.GLOBAL

_func_fetch_enemy_sprite:
    IMUL r1, 3
    MOV r0, r1
epilogue_L2:
    RET
.ENDGLOBAL

.ENDREGION
.REGION BANK_2
.GLOBAL

_func_calculate_path:
    ADD r1, r2
    MOV r0, r1
epilogue_L3:
    RET
.ENDGLOBAL

.ENDREGION
.REGION BANK_3
.GLOBAL

_func_do_stuff:
    LDI r0, 42
epilogue_L1:
    RET
.ENDGLOBAL

.ENDREGION

