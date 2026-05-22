; ------------------------
; Sharpie C cartridge
; ------------------------
.REGION FIXED
    JMP Main
.ENDREGION
; ----------------------------------
; SOURCE: long_test.c
; ----------------------------------

.REGION FIXED
; Global Variables
.GLOBAL
_global_global_long:
    .DW 34464
    .DW 1
.ENDGLOBAL
.GLOBAL
Main:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 134
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
    MOV r4, r6
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    MOV r6, r15
    IADD r6, 4
    LDP r4, r6
    IADD r6, 2
    LDP r5, r6
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 16
    MOV r4, r15
    IADD r4, 8
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    MOV r6, r15
    LDP r4, r6
    IADD r6, 2
    LDP r5, r6
    SUB r2, r4
    ALT SUB r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r2, r15
    IADD r2, 16
    LDP r1, r2
    IADD r2, 2
    LDP r3, r2
    INC r1
    ALT ADD r3, 0
    STA r3, r2
    IADD r2, -2
    STA r1, r2
    MOV r1, r15
    IADD r1, 8
    MOV r2, r15
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L2
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L2:
    JLE if_L1
    LDI r0, 1
    JMP epilogue_L0
if_L1:
    MOV r1, r15
    IADD r1, 60
    MOV r6, r15
    LDP r2, r6
    IADD r6, 2
    LDP r3, r6
    LDI r4, 3
    XOR r5, r5
    MOV r6, r2
    MUL r2, r4
    MOV r7, r6
    ALT MUL r7, r4
    MUL r6, r5
    ADD r7, r6
    MOV r6, r3
    MUL r6, r4
    ADD r7, r6
    STA r2, r1
    IADD r1, 2
    STA r7, r1
    MOV r1, r15
    IADD r1, 20
    LDI r2, 37856
    STA r2, r1
    IADD r1, 2
    LDI r2, 4
    STA r2, r1
    MOV r3, r15
    IADD r3, 20
    LDP r2, r3
    IADD r3, 2
    LDP r1, r3
    MOV r3, r15
    IADD r3, 4
    LDP r4, r3
    IADD r3, 2
    LDP r3, r3
    PUSH r1
    XOR r0, r0
    PUSH r0
    PUSH r2
    CALL SYS_DIV_32
    POP r0
    POP r0
    POP r1
    MOV r2, r15
    IADD r2, 116
    LDP r3, r2
    IADD r2, 2
    LDP r4, r2
    STA r3, r1
    IADD r1, 2
    STA r4, r1
    MOV r3, r15
    IADD r3, 20
    LDP r2, r3
    IADD r3, 2
    LDP r1, r3
    MOV r3, r15
    IADD r3, 4
    LDP r4, r3
    IADD r3, 2
    LDP r3, r3
    PUSH r1
    LDI r0, 1
    PUSH r0
    PUSH r2
    CALL SYS_DIV_32
    POP r0
    POP r0
    POP r1
    MOV r2, r15
    IADD r2, 120
    LDP r3, r2
    IADD r2, 2
    LDP r4, r2
    STA r3, r1
    IADD r1, 2
    STA r4, r1
    MOV r1, r15
    IADD r1, 44
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    LDI r4, 1
    MOV r5, r2
    SHL r2, r4
    SHL r3, r4
    ALT SHL r5, r4
    OR r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 72
    MOV r4, r15
    IADD r4, 44
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    LDI r4, 1
    MOV r5, r3
    SHR r3, r4
    SHR r2, r4
    ALT SHR r5, r4
    OR r2, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 76
    MOV r6, r15
    LDP r2, r6
    IADD r6, 2
    LDP r3, r6
    LDI r4, 65535
    XOR r5, r5
    AND r2, r4
    AND r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 80
    MOV r6, r15
    LDP r2, r6
    IADD r6, 2
    LDP r3, r6
    LDI r4, 255
    XOR r5, r5
    OR r2, r4
    OR r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 84
    MOV r6, r15
    LDP r2, r6
    IADD r6, 2
    LDP r3, r6
    LDI r4, 65535
    XOR r5, r5
    XOR r2, r4
    XOR r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 88
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    NOT r2
    NOT r3
    ADD r2, 1
    ALT ADD r3, 0
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 92
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    NOT r2
    NOT r3
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 50000
    XOR r5, r5
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 30000
    XOR r5, r5
    SUB r2, r4
    ALT SUB r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 2
    XOR r5, r5
    MOV r6, r2
    MUL r2, r4
    MOV r7, r6
    ALT MUL r7, r4
    MUL r6, r5
    ADD r7, r6
    MOV r6, r3
    MUL r6, r4
    ADD r7, r6
    MOV r3, r7
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 65535
    XOR r5, r5
    AND r2, r4
    AND r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 65280
    XOR r5, r5
    OR r2, r4
    OR r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 3855
    XOR r5, r5
    XOR r2, r4
    XOR r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 24
    LDI r2, 255
    STA r2, r1
    IADD r1, 2
    XOR r2, r2
    STA r2, r1
    MOV r1, r15
    IADD r1, 24
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 4
    MOV r5, r2
    SHL r2, r4
    SHL r3, r4
    ALT SHL r5, r4
    OR r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 24
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 2
    MOV r5, r3
    SHR r3, r4
    SHR r2, r4
    ALT SHR r5, r4
    OR r2, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 28
    LDI r2, 37856
    STA r2, r1
    IADD r1, 2
    LDI r2, 4
    STA r2, r1
    MOV r1, r15
    IADD r1, 28
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 18928
    LDI r5, 2
    MOV r6, r15
    IADD r6, 124
    PUSH r1
    MOV r1, r3
    MOV r3, r5
    XOR r0, r0
    PUSH r0
    PUSH r6
    CALL SYS_DIV_32
    POP r0
    POP r0
    POP r1
    MOV r6, r15
    IADD r6, 124
    LDP r2, r6
    STA r2, r1
    IADD r6, 2
    IADD r1, 2
    LDP r3, r6
    STA r3, r1
    MOV r1, r15
    IADD r1, 28
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 7
    XOR r5, r5
    MOV r6, r15
    IADD r6, 128
    PUSH r1
    MOV r1, r3
    MOV r3, r5
    LDI r0, 1
    PUSH r0
    PUSH r6
    CALL SYS_DIV_32
    POP r0
    POP r0
    POP r1
    MOV r6, r15
    IADD r6, 128
    LDP r2, r6
    STA r2, r1
    IADD r6, 2
    IADD r1, 2
    LDP r3, r6
    STA r3, r1
    MOV r1, r15
    IADD r1, 12
    XOR r2, r2
    STA r2, r1
    IADD r1, 2
    XOR r2, r2
    STA r2, r1
    MOV r1, r15
    IADD r1, 32
    LDI r2, 1
    STA r2, r1
    IADD r1, 2
    XOR r2, r2
    STA r2, r1
    MOV r1, r15
    IADD r1, 36
    LDI r2, 2
    STA r2, r1
    IADD r1, 2
    XOR r2, r2
    STA r2, r1
    MOV r1, r15
    IADD r1, 48
    LDI r2, 37856
    STA r2, r1
    IADD r1, 2
    LDI r2, 4
    STA r2, r1
    MOV r1, r15
    MOV r2, r15
    IADD r2, 4
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L8
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L8:
    JGE if_L7
if_L7:
    MOV r1, r15
    IADD r1, 4
    MOV r2, r15
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L10
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L10:
    JLE if_L9
if_L9:
    MOV r1, r15
    MOV r2, r15
    IADD r2, 4
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L12
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L12:
    JGT if_L11
if_L11:
    MOV r1, r15
    IADD r1, 4
    MOV r2, r15
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L14
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L14:
    JLT if_L13
if_L13:
    MOV r1, r15
    MOV r2, r15
    IADD r2, 4
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L16
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L16:
    JEQ if_L15
if_L15:
    MOV r1, r15
    IADD r1, 48
    MOV r2, r15
    IADD r2, 12
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L18
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L18:
    JEQ if_L17
if_L17:
    MOV r1, r15
    IADD r1, 32
    MOV r2, r15
    IADD r2, 36
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L20
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L20:
    JGE if_L19
if_L19:
    MOV r1, r15
    IADD r1, 36
    MOV r2, r15
    IADD r2, 32
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L22
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L22:
    JLE if_L21
if_L21:
    MOV r1, r15
    MOV r2, r15
    IADD r2, 12
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L24
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L24:
    JLE if_L23
if_L23:
    MOV r1, r15
    IADD r1, 12
    MOV r2, r15
    LDP r3, r1
    IADD r1, 2
    LDP r4, r1
    LDP r5, r2
    IADD r2, 2
    LDP r6, r2
    CMP r4, r6
    JNE cmp_done_L26
    LDI r1, 0x8000
    XOR r3, r1
    XOR r5, r1
    CMP r3, r5
cmp_done_L26:
    JGE if_L25
if_L25:
    MOV r1, r15
    IADD r1, 40
    LDI r2, 34464
    STA r2, r1
    IADD r1, 2
    LDI r2, 1
    STA r2, r1
    MOV r1, r15
    IADD r1, 96
    MOV r3, r15
    IADD r3, 40
    LDP r2, r3
    IADD r3, 2
    LDP r4, r3
    INC r2
    ALT ADD r4, 0
    STA r4, r3
    IADD r3, -2
    STA r2, r3
    MOV r2, r15
    IADD r2, 40
    MOV r3, r2
    MOV r4, r1
    LDP r5, r3
    STA r5, r4
    IADD r3, 2
    IADD r4, 2
    LDP r5, r3
    STA r5, r4
    MOV r1, r15
    IADD r1, 100
    MOV r2, r15
    IADD r2, 40
    MOV r3, r2
    MOV r4, r1
    LDP r5, r3
    STA r5, r4
    IADD r3, 2
    IADD r4, 2
    LDP r5, r3
    STA r5, r4
    MOV r3, r15
    IADD r3, 40
    LDP r2, r3
    IADD r3, 2
    LDP r4, r3
    DEC r2
    ALT SUB r4, 0
    STA r4, r3
    IADD r3, -2
    STA r2, r3
    MOV r1, r15
    IADD r1, 52
    MOV r4, r15
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    MOV r6, r15
    IADD r6, 4
    LDP r4, r6
    IADD r6, 2
    LDP r5, r6
    ADD r2, r4
    ALT ADD r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 56
    MOV r4, r15
    IADD r4, 8
    LDP r2, r4
    IADD r4, 2
    LDP r3, r4
    MOV r6, r15
    IADD r6, 16
    LDP r4, r6
    IADD r6, 2
    LDP r5, r6
    SUB r2, r4
    ALT SUB r3, r5
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    MOV r1, r15
    IADD r1, 104
    MOV r6, r15
    IADD r6, 52
    LDP r2, r6
    IADD r6, 2
    LDP r3, r6
    MOV r6, r15
    IADD r6, 56
    LDP r4, r6
    IADD r6, 2
    LDP r5, r6
    MOV r6, r2
    MUL r2, r4
    MOV r7, r6
    ALT MUL r7, r4
    MUL r6, r5
    ADD r7, r6
    MOV r6, r3
    MUL r6, r4
    ADD r7, r6
    STA r2, r1
    IADD r1, 2
    STA r7, r1
    MOV r1, r15
    IADD r1, 108
    MOV r0, r15
    IADD r0, 132
    STA r1, r0
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
    CALL _func_add_long
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    MOV r1, r15
    IADD r1, 112
    MOV r0, r15
    IADD r0, 132
    STA r1, r0
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
    CALL _func_mul_long
    LDI r1, 4
    CALL SYS_FREE_STACKFRAME
    MOV r1, r15
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    IADD r1, -2
    LDI r4, 2
    XOR r5, r5
    MOV r6, r2
    MUL r2, r4
    MOV r7, r6
    ALT MUL r7, r4
    MUL r6, r5
    ADD r7, r6
    MOV r6, r3
    MUL r6, r4
    ADD r7, r6
    MOV r3, r7
    STA r2, r1
    IADD r1, 2
    STA r3, r1
    XOR r0, r0
epilogue_L0:
    MOV r6, r15
    LDI r7, 134
    ADD r6, r7
    SETSP r6
    POP r15
    HALT
.ENDGLOBAL
.GLOBAL
_func_add_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
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
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L27:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_sub_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
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
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r5, r15
    IADD r5, 4
    LDP r3, r5
    IADD r5, 2
    LDP r4, r5
    SUB r1, r3
    ALT SUB r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L28:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_mul_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
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
    MOV r6, r5
    ALT MUL r6, r3
    MUL r5, r4
    ADD r6, r5
    MOV r5, r2
    MUL r5, r3
    ADD r6, r5
    STA r1, r8
    IADD r8, 2
    STA r6, r8
epilogue_L29:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_div_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 12
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 18
    MOV r5, r15
    IADD r5, 4
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
    MOV r2, r15
    LDP r2, r2
    IADD r2, 2
    LDP r1, r2
    MOV r2, r15
    IADD r2, 4
    LDP r4, r2
    IADD r2, 2
    LDP r3, r2
    XOR r0, r0
    PUSH r0
    PUSH r1
    CALL SYS_DIV_32
    POP r0
    POP r0
    MOV r1, r15
    IADD r1, 8
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    STA r2, r8
    IADD r8, 2
    STA r3, r8
epilogue_L30:
    MOV r6, r15
    LDI r7, 12
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_mod_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 12
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 18
    MOV r5, r15
    IADD r5, 4
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
    MOV r2, r15
    LDP r2, r2
    IADD r2, 2
    LDP r1, r2
    MOV r2, r15
    IADD r2, 4
    LDP r4, r2
    IADD r2, 2
    LDP r3, r2
    LDI r0, 1
    PUSH r0
    PUSH r1
    CALL SYS_DIV_32
    POP r0
    POP r0
    MOV r1, r15
    IADD r1, 8
    LDP r2, r1
    IADD r1, 2
    LDP r3, r1
    STA r2, r8
    IADD r8, 2
    STA r3, r8
epilogue_L32:
    MOV r6, r15
    LDI r7, 12
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_shl_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
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
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r3, r15
    IADD r3, 4
    MOV r4, r1
    SHL r1, r3
    SHL r2, r3
    ALT SHL r4, r3
    OR r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L34:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_shr_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
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
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    MOV r3, r15
    IADD r3, 4
    MOV r4, r2
    SHR r2, r3
    SHR r1, r3
    ALT SHR r4, r3
    OR r1, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L35:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_neg_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    NOT r1
    NOT r2
    ADD r1, 1
    ALT ADD r2, 0
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L36:
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_not_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 4
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r8, r1
    MOV r6, r15
    STA r2, r6
    IADD r6, 2
    STA r3, r6
    MOV r3, r15
    LDP r1, r3
    IADD r3, 2
    LDP r2, r3
    NOT r1
    NOT r2
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L37:
    MOV r6, r15
    LDI r7, 4
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_and_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
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
    AND r1, r3
    AND r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L38:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_or_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
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
    OR r1, r3
    OR r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L39:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_xor_long:
    PUSH r8
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    IADD r6, 14
    MOV r5, r15
    IADD r5, 4
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
    XOR r1, r3
    XOR r2, r4
    STA r1, r8
    IADD r8, 2
    STA r2, r8
epilogue_L40:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    POP r8
    RET
.ENDGLOBAL
.GLOBAL
_func_test_incdec:
    PUSH r15
    GETSP r15
    MOV r6, r15
    LDI r7, 8
    SUB r6, r7
    SETSP r6
    MOV r15, r6
    MOV r1, r6
    LDI r2, 34464
    STA r2, r1
    IADD r1, 2
    LDI r2, 1
    STA r2, r1
    MOV r2, r6
    LDP r1, r2
    IADD r2, 2
    LDP r3, r2
    INC r1
    ALT ADD r3, 0
    STA r3, r2
    IADD r2, -2
    STA r1, r2
    MOV r1, r6
    IADD r1, 4
    MOV r3, r6
    MOV r4, r1
    LDP r5, r3
    STA r5, r4
    IADD r3, 2
    IADD r4, 2
    LDP r5, r3
    STA r5, r4
epilogue_L41:
    MOV r6, r15
    LDI r7, 8
    ADD r6, r7
    SETSP r6
    POP r15
    RET
.ENDGLOBAL
.ENDREGION

