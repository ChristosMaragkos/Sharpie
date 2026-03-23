; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: global_variables.c
; ----------------------------------
.REGION FIXED
; Global Variables
.GLOBAL
_global_g_score:
    .DW 0
.ENDGLOBAL
.GLOBAL
_global_g_lives:
    .DW 3
.ENDGLOBAL
.GLOBAL
_global_g_map:
    .DW 10
    .DW 20
    .DW 30
.ENDGLOBAL
_global_g_p1:
    .DW 100
    .DB 5
    .DB 0
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    LDI r1, 50
    STM r1, _global_g_score
    LDI r1, _global_g_lives
    DINC r1
    LDI r1, 200
    STM r1, _global_g_p1
    LDI r1, _global_g_p1
    MOV r2, r15
    PUSH r1
    MOV r1, r2
    POP r2
    LDI r3, 4
    CALL SYS_MEM_MOVE
    LDM r1, _global_g_score
    LDM r2, _global_g_lives
    ADD r1, r2
    LDI r3, _global_g_map
    LDI r4, 2
    ADD r3, r4
    LDP r2, r3
    ADD r1, r2
    LDP r2, r15
    ADD r1, r2
    MOV r0, r1
epilogue_L0:
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.ENDREGION

