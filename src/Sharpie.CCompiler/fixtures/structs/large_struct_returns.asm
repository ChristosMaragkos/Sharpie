; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: large_struct_returns.c
; ----------------------------------

.REGION FIXED
.GLOBAL

Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 106
    SETSP r6
    MOV r15, r6
    MOV r1, r6
    MOV r0, r6
    IADD r0, 104
    STA r1, r0
    CALL _func_create_padding
    PUSH r0
    MOV r0, r15
    IADD r0, 104
    XOR r0, r0
epilogue_L0:
    MOV r6, r15
    IADD r6, 106
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL

.GLOBAL

_func_create_padding:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    ISUB r6, 104
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r1, r6
    PUSH r1
    MOV r1, r8
    POP r2
    LDI r3, 104
    CALL SYS_MEM_MOVE
epilogue_L1:
    MOV r6, r15
    IADD r6, 104
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL

.ENDREGION

