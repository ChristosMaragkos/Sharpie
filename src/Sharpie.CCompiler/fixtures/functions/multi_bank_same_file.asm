; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: multi_bank_same_file.c
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
    STA r1, r15
    LDI r1, 10
    PUSH r13
    PUSH r14
    LDI r14, 1
    LDI r13, _func_bank1_func
    CALL SYS_FAR_CALL
    POP r14
    POP r13
    MOV r1, r0
    STA r1, r15
    MOV r2, r0
    MOV r1, r0
    PUSH r13
    PUSH r14
    LDI r14, 2
    LDI r13, _func_bank2_func
    CALL SYS_FAR_CALL
    POP r14
    POP r13
    MOV r1, r0
    CALL _func_fixed_func
epilogue_L0:
    MOV r6, r15
    LDI r7, 2
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_fixed_func:
    ISUB r1, 5
    MOV r0, r1
epilogue_L3:
    RET
.ENDGLOBAL
.ENDREGION
.REGION BANK_1
.GLOBAL
_func_bank1_func:
    IADD r1, 100
    MOV r0, r1
epilogue_L1:
    RET
.ENDGLOBAL
.ENDREGION
.REGION BANK_2
.GLOBAL
_func_bank2_func:
    ADD r1, r1
    MOV r0, r1
epilogue_L2:
    RET
.ENDGLOBAL
.ENDREGION

